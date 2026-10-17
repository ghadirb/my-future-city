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
        /// <summary>An extra activity no mission asks for: shown once its stage is done, repeatable, never rewarded.</summary>
        public bool optional;

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
            if (optional)
            {
                // Hidden until the stage is done; never "done" (it can be played again); no yellow guide beacon.
                if (pendingVisual != null)
                {
                    pendingVisual.SetActive(IsAvailable(ctx));
                    var marks = pendingVisual.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < marks.Length; i++)
                        if (marks[i].name == "Beacon" || marks[i].name == "Marker") marks[i].gameObject.SetActive(false);
                }
                if (completedVisual != null) completedVisual.SetActive(false);
                return;
            }
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
            if (optional) return context != null && context.Activities.IsOptionalAvailable(missionId, targetId);
            return context != null &&
                   context.Missions.GetStatus(missionId) == MissionStatus.Active &&
                   context.Missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId) &&
                   order == context.Missions.GetCountByTarget(missionId, targetId);
        }

        public override bool MatchesCurrentObjective(GameContext context) { return !optional && IsAvailable(context); }

        public override string GuideName { get { return guideNameFa; } }

        public override void Interact(GameSceneController scene)
        {
            if (scene == null || scene.Ctx == null) return;
            var session = optional
                ? scene.Ctx.Activities.BeginOptional(missionId, targetId, order)
                : scene.Ctx.Activities.Begin(missionId, targetId, order);
            if (session == null) return;
            ShowStep(scene, session);
        }

        private void ShowStep(GameSceneController scene, ActivitySession session)
        {
            var step = session.CurrentStep;
            if (step == null) return;

            string title = session.Scene.titleFa + "  (" + (session.StepIndex + 1) + "/" + session.StepCount + ")";
            if (step.ordered) { ShowOrderedStep(scene, session, step, title); return; }

            int n = step.options.Count;
            var options = new DialogOption[n];
            for (int i = 0; i < n; i++)
            {
                int index = i;
                options[i] = new DialogOption(step.options[i].textFa, () => OnChoose(scene, session, index));
            }
            scene.Hud.ShowDialog(title, step.promptFa, options);
        }

        /// <summary>
        /// Direct "doing" step: items already placed are listed in the text, the remaining ones are buttons shown in a
        /// scrambled (but fixed) order; the child taps them one by one in the order the work is really done.
        /// </summary>
        private void ShowOrderedStep(GameSceneController scene, ActivitySession session, ActivityStep step, string title)
        {
            var body = new System.Text.StringBuilder(step.promptFa);
            if (session.PlacedCount > 0)
            {
                body.Append("\n\n");
                for (int i = 0; i < session.PlacedCount; i++)
                    body.Append(i + 1).Append(") ").Append(step.options[i].textFa).Append("\n");
            }

            var remaining = new System.Collections.Generic.List<int>();
            for (int i = session.PlacedCount; i < step.options.Count; i++) remaining.Add(i);
            if (remaining.Count > 1)
            {
                // rotate so the correct (first) remaining item is never the first button
                int shift = 1 + (session.SceneKey + session.StepIndex + session.PlacedCount) % (remaining.Count - 1);
                var rotated = new System.Collections.Generic.List<int>();
                for (int i = 0; i < remaining.Count; i++) rotated.Add(remaining[(i + shift) % remaining.Count]);
                remaining = rotated;
            }

            var options = new DialogOption[remaining.Count];
            for (int i = 0; i < remaining.Count; i++)
            {
                int index = remaining[i];
                options[i] = new DialogOption(step.options[index].textFa, () => OnChoose(scene, session, index));
            }
            scene.Hud.ShowDialog(title, body.ToString(), options);
        }

        private void OnChoose(GameSceneController scene, ActivitySession session, int index)
        {
            var result = session.Choose(index);
            if (result.Placed)
            {
                scene.Hud.ShowDialog(session.Scene.titleFa, result.FeedbackFa,
                    new DialogOption("ادامه", () => ShowStep(scene, session)));
                return;
            }
            if (!result.Accepted)
            {
                ShowMistake(scene, session, result.FeedbackFa);
                return;
            }

            if (result.SceneFinished)
            {
                string summary = session.FirstTryCorrect + " از " + session.StepsDone
                                 + " تصمیم را در تلاش اول درست گرفتی."
                                 + (session.IsOptional ? "\nاین یک فعالیت اختیاری بود؛ هر وقت خواستی می‌توانی دوباره انجامش بدهی." : "");
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
