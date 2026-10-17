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
        /// <summary>Ordered step only: the item went to the right slot but the step is not finished yet.</summary>
        public bool Placed;
        /// <summary>What the student says/does in answer to this choice (empty when the activity has no reactions).</summary>
        public string ReactionFa = "";
        /// <summary>Wrong picks discouraged the student: a support step now comes before the original step.</summary>
        public bool SupportStarted;
        /// <summary>The support step was answered well; the original step is tried again.</summary>
        public bool SupportDone;
        /// <summary>Set together with SceneFinished: how the scene ended for the student.</summary>
        public string EndingFa = "";
    }

    public enum StudentMood { Calm, Unsure, Discouraged }

    /// <summary>One run through one scene. Created by <see cref="ActivityService.Begin"/>.</summary>
    public class ActivitySession
    {
        private readonly ActivityService service;
        public ActivityDefinition Definition { get; private set; }
        public ActivityScene Scene { get; private set; }
        public int Order { get; private set; }
        public int SceneIndex { get; private set; }
        /// <summary>Index of the variant played (-1 = default scenes). Part of the log key so variants never collide.</summary>
        public int VariantIndex { get; private set; }
        public int SceneKey { get { return VariantIndex < 0 ? SceneIndex : 100 * (VariantIndex + 1) + SceneIndex; } }
        public int StepIndex { get; private set; }
        public bool Finished { get; private set; }
        public bool Abandoned { get; private set; }
        /// <summary>True when the mission had already been completed before this run (a voluntary repeat).</summary>
        public bool IsReplay { get; private set; }
        /// <summary>True for an extra activity that no mission asks for (see ActivityDefinition.optional).</summary>
        public bool IsOptional { get { return Definition.optional; } }

        public int StepsDone { get; private set; }
        public int FirstTryCorrect { get; private set; }
        public int WrongChoices { get; private set; }
        public int HintsUsed { get; private set; }

        private bool stepHadMistake;
        private int stepWrong;          // wrong picks in the current step (drives the student's mood)

        /// <summary>Wrong picks in one step that discourage the student and start the support step.</summary>
        public const int WrongsBeforeSupport = 2;
        /// <summary>Log index of the support step (real steps use their own index).</summary>
        public const int SupportLogStep = 90;

        /// <summary>True while the support (encouragement) step is the one being played.</summary>
        public bool InSupport { get; private set; }
        public bool SupportUsed { get; private set; }
        public StudentMood Mood { get { return stepWrong >= WrongsBeforeSupport ? StudentMood.Discouraged : (stepWrong == 1 ? StudentMood.Unsure : StudentMood.Calm); } }
        public string StudentName { get { return string.IsNullOrEmpty(Scene.studentFa) ? "دانش‌آموز" : Scene.studentFa; } }

        private string Pick(List<string> pool, int salt)
        {
            if (pool == null || pool.Count == 0) return "";
            int i = (SceneKey * 7 + StepIndex * 3 + salt + WrongChoices) % pool.Count;
            if (i < 0) i = -i;
            return pool[i].Replace("{name}", StudentName);
        }

        /// <summary>Ordered step: how many items are already in place (items 0..PlacedCount-1 of the authored order).</summary>
        public int PlacedCount { get; private set; }

        public const string OrderWrongFa = "این کار هنوز زود است. فکر کن اول کدام کار باید انجام شود.";

        /// <summary>The option index a correct play would pick now (first accepted option, or the next item of an ordered step).</summary>
        public int NextAcceptedIndex()
        {
            var step = CurrentStep;
            if (step == null) return -1;
            if (step.ordered) return PlacedCount;
            for (int i = 0; i < step.options.Count; i++) if (step.IsAccepted(i)) return i;
            return -1;
        }

        /// <summary>An option index that this step would NOT accept now, or -1 when none exists.</summary>
        public int WrongIndex()
        {
            var step = CurrentStep;
            if (step == null) return -1;
            for (int i = 0; i < step.options.Count; i++)
            {
                if (step.ordered ? i > PlacedCount : !step.IsAccepted(i)) return i;
            }
            return -1;
        }

        internal ActivitySession(ActivityService service, ActivityDefinition def, int order, bool replay, int variantIndex)
        {
            this.service = service;
            Definition = def;
            Order = order;
            VariantIndex = variantIndex;
            var list = variantIndex >= 0 ? def.variants[variantIndex].scenes : def.scenes;
            SceneIndex = order < 0 ? 0 : order % list.Count;
            Scene = list[SceneIndex];
            IsReplay = replay;
        }

        public int StepCount { get { return Scene.steps.Count; } }

        public ActivityStep CurrentStep
        {
            get
            {
                if (Finished || Abandoned || StepIndex >= Scene.steps.Count) return null;
                return InSupport ? Definition.supportStep : Scene.steps[StepIndex];
            }
        }

        /// <summary>The child picked an option. A wrong pick changes nothing except the log; the same step is tried again.</summary>
        public StepResult Choose(int optionIndex)
        {
            var step = CurrentStep;
            var res = new StepResult();
            if (step == null || optionIndex < 0 || optionIndex >= step.options.Count) return res;

            var opt = step.options[optionIndex];
            bool ok;
            if (step.ordered)
            {
                if (optionIndex < PlacedCount) return res;        // already placed: not a decision
                ok = optionIndex == PlacedCount;
            }
            else ok = step.IsAccepted(optionIndex);
            res.Correct = ok;
            res.FeedbackFa = (step.ordered && !ok) ? OrderWrongFa : opt.feedbackFa;
            service.LogChoice(this, InSupport ? SupportLogStep : StepIndex, optionIndex, ok);

            var rx = Definition.HasReactions ? Definition.reactions : null;
            if (!ok)
            {
                WrongChoices++;
                stepHadMistake = true;
                service.Stats(Definition.jobId).wrongChoices++;
                if (!step.ordered && rx != null)
                {
                    stepWrong++;
                    if (!InSupport && stepWrong >= WrongsBeforeSupport && Definition.HasSupport && !SupportUsed)
                    {
                        InSupport = true;               // the lesson takes a detour: first encourage the student
                        SupportUsed = true;
                        res.SupportStarted = true;
                    }
                    res.ReactionFa = Pick(Mood == StudentMood.Discouraged ? rx.discouragedFa : rx.unsureFa, stepWrong);
                }
                return res;
            }

            if (InSupport)
            {
                // The detour is over: the student calms down and the same step is tried again.
                InSupport = false;
                stepWrong = 0;
                res.Accepted = true;
                res.SupportDone = true;
                res.ReactionFa = Pick(rx != null ? rx.reliefFa : null, 1);
                return res;
            }

            if (step.ordered)
            {
                PlacedCount++;
                if (PlacedCount < step.options.Count) { res.Placed = true; return res; }
                PlacedCount = 0;                                   // the last item completes the step
            }

            res.Accepted = true;
            if (!step.ordered && rx != null) res.ReactionFa = Pick(stepHadMistake ? rx.reliefFa : rx.confidentFa, 2);
            stepWrong = 0;
            if (!string.IsNullOrEmpty(opt.setsTag)) service.RememberChoice(opt.setsTag);
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
                if (rx != null)
                    res.EndingFa = Pick((SupportUsed || WrongChoices >= 3) ? rx.endingReviewFa : rx.endingGoodFa, 3);
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
            if (def == null || def.optional) return null;
            var active = missions.ActiveMission;
            if (active == null || active.id != missionId) return null;
            if (!missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId)) return null;
            if (missions.GetCountByTarget(missionId, targetId) != order) return null;
            if (def.SceneFor(order) == null) return null;

            bool replay = missions.TimesCompleted(missionId) > 0;
            Stats(def.jobId).started++;
            return new ActivitySession(this, def, order, replay, def.VariantIndexFor(save.choiceTags));
        }

        /// <summary>An optional activity is offered once the stage named by its missionId has been completed once.</summary>
        public bool IsOptionalAvailable(string missionId, string targetId)
        {
            var def = acts.Find(missionId, targetId);
            return def != null && def.optional && def.scenes != null && def.scenes.Count > 0 && missions.TimesCompleted(missionId) > 0;
        }

        /// <summary>
        /// Starts an optional activity. It needs no active mission, can be played again and again, and never touches
        /// missions or rewards: the only thing it changes is the interest log.
        /// </summary>
        public ActivitySession BeginOptional(string missionId, string targetId, int order)
        {
            if (!IsOptionalAvailable(missionId, targetId)) return null;
            var def = acts.Find(missionId, targetId);
            if (def.SceneFor(order) == null) return null;
            Stats(def.jobId).started++;
            return new ActivitySession(this, def, order, false, def.VariantIndexFor(save.choiceTags));
        }

        // ----- internal bookkeeping used by ActivitySession -----

        internal void RememberChoice(string tag) { save.SetChoiceTag(tag); }

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
                scene = s.SceneKey,
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
            if (s.IsOptional) js.optionalPicked++;
            else if (s.IsReplay) js.optionalRuns++;
            var rec = save.GetActivityRecord(s.Definition.missionId, s.Definition.targetId, s.SceneKey, true);
            rec.completions++;
            rec.jobId = s.Definition.jobId;

            // Stats are written BEFORE the mission advances, so the save made by the mission events already contains them.
            var h = SceneCompleted;
            if (h != null) h(s);
            if (!s.IsOptional) missions.Notify(ObjectiveType.Interact, s.Definition.targetId);   // optional: no mission, no reward
        }
    }
}
