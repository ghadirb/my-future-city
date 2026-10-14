using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>
    /// A spot in the school where the child does real teaching work (choose a lesson, show and explain a
    /// concept, answer a student, manage the class, run a whole lesson). Touching it never counts by itself:
    /// the objective only advances once <see cref="TeachingFlow"/> reports a finished teaching decision.
    /// </summary>
    public sealed class TeacherLessonInteractable : Interactable
    {
        public const string TargetTopic = "lesson_topic";
        public const string TargetBoard = "teach_board";
        public const string TargetStudents = "teach_students";
        public const string TargetHelp = "teach_help";
        public const string TargetActivity = "teach_activity";
        public const string TargetFull = "teach_full";

        public string missionId;
        public string targetId;
        public int order;
        public GameObject pendingVisual;
        public GameObject completedVisual;

        private GameContext ctx;

        public static bool IsTeachingTarget(string target)
        {
            return target == TargetTopic || target == TargetBoard || target == TargetStudents ||
                   target == TargetHelp || target == TargetActivity || target == TargetFull;
        }

        private void Start()
        {
            ctx = GameContext.Instance;
            if (ctx != null) ctx.Missions.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ctx != null) ctx.Missions.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (ctx == null) return;
            // Spots of different stages share places in the school yard, so only the running stage shows its spots.
            bool running = ctx.Missions.GetStatus(missionId) == MissionStatus.Active;
            bool done = running && order < ctx.Missions.GetCountByTarget(missionId, targetId);
            if (pendingVisual != null) pendingVisual.SetActive(running && !done);
            if (completedVisual != null) completedVisual.SetActive(running && done);
        }

        public override bool IsAvailable(GameContext context)
        {
            return context != null &&
                   context.Missions.GetStatus(missionId) == MissionStatus.Active &&
                   context.Missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId) &&
                   order == context.Missions.GetCountByTarget(missionId, targetId);
        }

        public override bool MatchesCurrentObjective(GameContext context)
        {
            return IsAvailable(context);
        }

        public override string GuideName
        {
            get
            {
                switch (targetId)
                {
                    case TargetTopic: return "تختهٔ انتخاب درس";
                    case TargetBoard: return "تختهٔ کلاس";
                    case TargetStudents: return "دانش‌آموز کلاس";
                    case TargetHelp: return "دانش‌آموز نیازمند کمک";
                    case TargetActivity: return "موقعیت کلاس";
                    default: return "تختهٔ جلسهٔ کامل";
                }
            }
        }

        public override string GetPrompt(GameContext context)
        {
            switch (targetId)
            {
                case TargetTopic: return "انتخاب موضوع درس";
                case TargetBoard: return order == 0 ? "نشان دادن مفهوم" : "توضیح ساده";
                case TargetStudents: return "گوش دادن به دانش‌آموز";
                case TargetHelp: return "کمک به دانش‌آموز";
                case TargetActivity: return "انتخاب فعالیت کلاس";
                default: return "شروع جلسهٔ کامل";
            }
        }

        public override void Interact(GameSceneController scene)
        {
            if (scene == null || !IsAvailable(scene.Ctx)) return;
            switch (targetId)
            {
                case TargetTopic:
                    TeachingFlow.ChooseTopic(scene, missionId, () => Advance(scene));
                    break;
                case TargetBoard:
                    TeachingFlow.RunStep(scene, order == 0 ? StepKinds.Present : StepKinds.Explain, missionId, s => Advance(scene));
                    break;
                case TargetStudents:
                    TeachingFlow.RunStep(scene, StepKinds.Check, missionId, s => Advance(scene));
                    break;
                case TargetHelp:
                    TeachingFlow.RunStep(scene, StepKinds.Help, missionId, s => Advance(scene));
                    break;
                case TargetActivity:
                    TeachingFlow.RunStep(scene, StepKinds.Manage, missionId, s => Advance(scene));
                    break;
                default:
                    TeachingFlow.RunFullLesson(scene, missionId, () => Advance(scene));
                    break;
            }
        }

        /// <summary>Counts the finished teaching action for the mission (only once per spot).</summary>
        private void Advance(GameSceneController scene)
        {
            if (!IsAvailable(scene.Ctx)) return;
            scene.Ctx.Missions.Notify(ObjectiveType.Interact, targetId);
            Refresh();
        }
    }
}
