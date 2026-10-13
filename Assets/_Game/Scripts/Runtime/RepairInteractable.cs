using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>One broken spot of a building. Counts toward an Interact objective.</summary>
    public class RepairInteractable : Interactable
    {
        public string missionId;
        public string targetId;
        public int order;
        public GameObject brokenVisual;
        public GameObject fixedVisual;

        private GameContext ctx;
        private bool isFixed;

        private void Start()
        {
            ctx = GameContext.Instance;
            ctx.Missions.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ctx != null) ctx.Missions.Changed -= Refresh;
        }

        private void Refresh()
        {
            var status = ctx.Missions.GetStatus(missionId);
            if (status == MissionStatus.Completed) isFixed = true;
            else if (status == MissionStatus.Active) isFixed = order < ctx.Missions.GetCountByTarget(missionId, targetId);
            else isFixed = false;
            if (brokenVisual != null) brokenVisual.SetActive(!isFixed);
            if (fixedVisual != null) fixedVisual.SetActive(isFixed);
        }

        public override bool IsAvailable(GameContext c)
        {
            return !isFixed && c.Missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId);
        }

        public override bool MatchesCurrentObjective(GameContext c)
        {
            return IsAvailable(c);
        }

        public override void Interact(GameSceneController scene)
        {
            scene.Ctx.Missions.Notify(ObjectiveType.Interact, targetId);
        }
    }
}
