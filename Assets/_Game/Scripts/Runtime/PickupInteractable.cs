using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>A world item that counts toward a Collect objective. Hidden once collected in the current run.</summary>
    public class PickupInteractable : Interactable
    {
        public string missionId;
        public string targetId;
        public string displayNameFa;
        public int order;               // index among pickups sharing the same targetId
        public Transform spin;

        private GameContext ctx;
        private Renderer[] renderers;
        private bool visible = true;

        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            ctx = GameContext.Instance;
            ctx.Missions.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ctx != null) ctx.Missions.Changed -= Refresh;
        }

        private void Update()
        {
            if (spin != null && visible) spin.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        private bool ShouldShow()
        {
            var def = ctx.Database.FindMission(missionId);
            var status = ctx.Missions.GetStatus(missionId);
            if (status == MissionStatus.Active)
                return order >= ctx.Missions.GetCountByTarget(missionId, targetId);
            if (status == MissionStatus.Completed && def != null && !def.repeatable) return false;
            return true;
        }

        private void Refresh()
        {
            bool show = ShouldShow();
            if (show == visible && renderers != null) { /* still apply once after Start */ }
            visible = show;
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = show;
        }

        public override bool IsAvailable(GameContext c)
        {
            return visible && c.Missions.IsObjectiveCurrent(ObjectiveType.Collect, targetId);
        }

        public override bool MatchesCurrentObjective(GameContext c)
        {
            return IsAvailable(c);
        }

        public override string GetPrompt(GameContext c)
        {
            return "برداشتن " + displayNameFa;
        }

        public override void Interact(GameSceneController scene)
        {
            scene.Ctx.Missions.Notify(ObjectiveType.Collect, targetId);
        }
    }
}
