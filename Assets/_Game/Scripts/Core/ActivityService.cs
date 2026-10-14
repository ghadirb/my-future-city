using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    public class StepResult
    {
        /// <summary>The choice was accepted (correct, or any option of a free step) and the session moved on.</summary>
        public bool Accepted;
        public bool Correct;
        public string FeedbackFa;
        public bool SceneFinished;
    }

    /// <summary>One run through one scene. Created by <see cref="ActivityService.Begin"/>.</summary>
    public class ActivitySession
    {
        private readonly ActivityService service;
        public ActivityDefinition Definition { get; private set; }
        public ActivityScene Scene { get; private set; }
        public int Order { get; private set; }
        public int SceneIndex { get; private set; }
        public int StepIndex { get; private set; }
        public bool Finished { get; private set; }
        public bool Abandoned { get; private set; }
        /// <summary>True when the mission had already been completed before this run (a voluntary repeat).</summary>
        public bool IsReplay { get; private set; }

        public int StepsDone { get; private set; }
        public int FirstTryCorrect { get; private set; }
        public int WrongChoices { get; private set; }
        public int HintsUsed { get; private set; }

        private bool stepHadMistake;

        internal ActivitySession(ActivityService service, ActivityDefinition def, int order, bool replay)
        {
            this.service = service;
            Definition = def;
            Order = order;
            SceneIndex = def.SceneIndexFor(order);
            Scene = def.scenes[SceneIndex];
            IsReplay = replay;
        }

        public int StepCount { get { return Scene.steps.Count; } }

        public ActivityStep CurrentStep
        {
            get { return (Finished || Abandoned || StepIndex >= Scene.steps.Count) ? null : Scene.steps[StepIndex]; }
        }

        /// <summary>The child picked an option. A wrong pick changes nothing except the log; the same step is tried again.</summary>
        public StepResult Choose(int optionIndex)
        {
            var step = CurrentStep;
            var res = new StepResult();
            if (step == null || optionIndex < 0 || optionIndex >= step.options.Count) return res;

            var opt = step.options[optionIndex];
            bool ok = step.IsAccepted(optionIndex);
            res.Correct = ok;
            res.FeedbackFa = opt.feedbackFa;
            service.LogChoice(this, StepIndex, optionIndex, ok);

            if (!ok)
            {
                WrongChoices++;
                stepHadMistake = true;
                service.Stats(Definition.jobId).wrongChoices++;
                return res;
            }

            res.Accepted = true;
            StepsDone++;
            var js = service.Stats(Definition.jobId);
            js.stepsDone++;
            if (!stepHadMistake) { FirstTryCorrect++; js.firstTry++; }
            stepHadMistake = false;
            StepIndex++;

            if (StepIndex >= Scene.steps.Count)
            {
                Finished = true;
                res.SceneFinished = true;
                service.FinishSession(this);
            }
            return res;
        }

        /// <summary>The child asked for help on the current step. Counted in the log, never punished.</summary>
        public string UseHint()
        {
            var step = CurrentStep;
            if (step == null) return "";
            HintsUsed++;
            stepHadMistake = true;      // a hinted step no longer counts as "first try"
            service.Stats(Definition.jobId).hints++;
            return step.hintFa ?? "";
        }

        /// <summary>The child walked away before the scene was finished.</summary>
        public void Abandon()
        {
            if (Finished || Abandoned) return;
            Abandoned = true;
            service.Stats(Definition.jobId).abandoned++;
        }
    }

    /// <summary>
    /// Runs real job activities and records what the child actually did. It only talks to the interest log
    /// (SaveData.activity*); rewards are given exclusively by <see cref="MissionService"/> when the objective advances.
    /// Pure C# so it is fully unit-tested.
    /// </summary>
    public class ActivityService
    {
        private readonly ActivityDatabase acts;
        private readonly SaveData save;
        private readonly MissionService missions;

        public event Action<ActivitySession> SceneCompleted;

        public ActivityService(ActivityDatabase acts, SaveData save, MissionService missions)
        {
            if (acts == null) throw new ArgumentNullException("acts");
            if (save == null) throw new ArgumentNullException("save");
            if (missions == null) throw new ArgumentNullException("missions");
            this.acts = acts;
            this.save = save;
            this.missions = missions;
        }

        public ActivityDatabase Database { get { return acts; } }

        public bool HasActivity(string missionId, string targetId) { return acts.Has(missionId, targetId); }

        /// <summary>
        /// Starts spot number <paramref name="order"/> of an activity objective. Returns null unless this exact spot is
        /// the next unfinished one of the ACTIVE mission, so a spot can never be rewarded twice in one run.
        /// </summary>
        public ActivitySession Begin(string missionId, string targetId, int order)
        {
            var def = acts.Find(missionId, targetId);
            if (def == null) return null;
            var active = missions.ActiveMission;
            if (active == null || active.id != missionId) return null;
            if (!missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId)) return null;
            if (missions.GetCountByTarget(missionId, targetId) != order) return null;
            if (def.SceneFor(order) == null) return null;

            bool replay = missions.TimesCompleted(missionId) > 0;
            Stats(def.jobId).started++;
            return new ActivitySession(this, def, order, replay);
        }

        // ----- internal bookkeeping used by ActivitySession -----

        internal ActivityJobStats Stats(string jobId)
        {
            return save.GetActivityStats(jobId, true);
        }

        internal void LogChoice(ActivitySession s, int step, int option, bool correct)
        {
            save.activitySeq++;
            save.activityChoices.Add(new ActivityChoice
            {
                seq = save.activitySeq,
                missionId = s.Definition.missionId,
                targetId = s.Definition.targetId,
                scene = s.SceneIndex,
                step = step,
                option = option,
                correct = correct
            });
            if (save.activityChoices.Count > SaveData.MaxActivityChoices)
                save.activityChoices.RemoveRange(0, save.activityChoices.Count - SaveData.MaxActivityChoices);
        }

        internal void FinishSession(ActivitySession s)
        {
            var js = Stats(s.Definition.jobId);
            js.completed++;
            if (s.IsReplay) js.optionalRuns++;
            var rec = save.GetActivityRecord(s.Definition.missionId, s.Definition.targetId, s.SceneIndex, true);
            rec.completions++;
            rec.jobId = s.Definition.jobId;

            // Stats are written BEFORE the mission advances, so the save made by the mission events already contains them.
            var h = SceneCompleted;
            if (h != null) h(s);
            missions.Notify(ObjectiveType.Interact, s.Definition.targetId);
        }
    }
}
