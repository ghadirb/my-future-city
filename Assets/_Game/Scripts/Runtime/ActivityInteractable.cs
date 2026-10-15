using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>
    /// One real job activity spot. The child has to make the decisions of the job (see activities.json);
    /// simply touching the spot never advances the mission. Wrong answers cost nothing: the child gets friendly
    /// feedback, may ask for a hint and may try again or come back later.
    /// </summary>
    public sealed class ActivityInteractable : Interactable
    {
        public string missionId;
        public string targetId;
        public int order;
        public string guideNameFa = "";
        public GameObject pendingVisual;
        public GameObject completedVisual;

        private GameContext ctx;
        private bool done;

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
            var status = ctx.Missions.GetStatus(missionId);
            done = status == MissionStatus.Completed ||
                   (status == MissionStatus.Active && order < ctx.Missions.GetCountByTarget(missionId, targetId));
            if (pendingVisual != null) pendingVisual.SetActive(!done);
            if (completedVisual != null) completedVisual.SetActive(done);
            UpdateBeacons();
        }

        /// <summary>The yellow pole/ball only shows on the spot the child should go to next.</summary>
        private void UpdateBeacons()
        {
            if (pendingVisual == null) return;
            bool current = IsAvailable(ctx);
            var all = pendingVisual.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "Beacon" || all[i].name == "Marker") all[i].gameObject.SetActive(current);
        }

        public override bool IsAvailable(GameContext context)
        {
            return context != null &&
                   context.Missions.GetStatus(missionId) == MissionStatus.Active &&
                   context.Missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId) &&
                   order == context.Missions.GetCountByTarget(missionId, targetId);
        }

        public override bool MatchesCurrentObjective(GameContext context) { return IsAvailable(context); }

        public override string GuideName { get { return guideNameFa; } }

        public override void Interact(GameSceneController scene)
        {
            if (scene == null || scene.Ctx == null) return;
            var session = scene.Ctx.Activities.Begin(missionId, targetId, order);
            if (session == null) return;
            ShowStep(scene, session);
        }

        private void ShowStep(GameSceneController scene, ActivitySession session)
        {
            var step = session.CurrentStep;
            if (step == null) return;

            string title = session.Scene.titleFa + "  (" + (session.StepIndex + 1) + "/" + session.StepCount + ")";
            int n = step.options.Count;
            var options = new DialogOption[n];
            for (int i = 0; i < n; i++)
            {
                int index = i;
                options[i] = new DialogOption(step.options[i].textFa, () => OnChoose(scene, session, index));
            }
            scene.Hud.ShowDialog(title, step.promptFa, options);
        }

        private void OnChoose(GameSceneController scene, ActivitySession session, int index)
        {
            var result = session.Choose(index);
            if (!result.Accepted)
            {
                ShowMistake(scene, session, result.FeedbackFa);
                return;
            }

            if (result.SceneFinished)
            {
                string summary = session.FirstTryCorrect + " از " + session.StepsDone
                                 + " تصمیم را در تلاش اول درست گرفتی.";
                scene.Hud.ShowDialog("کار تمام شد!", result.FeedbackFa + "\n" + summary,
                    new DialogOption("ادامه", null));
                Refresh();
                return;
            }

            scene.Hud.ShowDialog(session.Scene.titleFa, result.FeedbackFa,
                new DialogOption("ادامه", () => ShowStep(scene, session)));
        }

        private void ShowMistake(GameSceneController scene, ActivitySession session, string feedback)
        {
            scene.Hud.ShowDialog("یک بار دیگر فکر کنیم", feedback,
                new DialogOption("دوباره تلاش می‌کنم", () => ShowStep(scene, session)),
                new DialogOption("راهنما می‌خواهم", () =>
                {
                    string hint = session.UseHint();
                    scene.Hud.ShowDialog("راهنما", string.IsNullOrEmpty(hint) ? "آرام فکر کن؛ می‌توانی!" : hint,
                        new DialogOption("متوجه شدم", () => ShowStep(scene, session)));
                }),
                new DialogOption("بعداً برمی‌گردم", () => session.Abandon()));
        }
    }
}
