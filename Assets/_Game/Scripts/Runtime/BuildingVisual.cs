using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Makes a building's look follow its persisted restoration state.</summary>
    public class BuildingVisual : MonoBehaviour
    {
        public string buildingId;
        public Renderer[] bodyRenderers;
        public Material abandonedMaterial, renovatingMaterial, restoredMaterial;
        public GameObject[] abandonedObjects, renovatingObjects, restoredObjects;

        private GameContext ctx;

        private void Start()
        {
            ctx = GameContext.Instance;
            ctx.City.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ctx != null) ctx.City.Changed -= Refresh;
        }

        private static void SetAll(GameObject[] objs, bool on)
        {
            if (objs == null) return;
            for (int i = 0; i < objs.Length; i++) if (objs[i] != null) objs[i].SetActive(on);
        }

        private void Refresh()
        {
            var state = ctx.City.GetState(buildingId);
            Material m = state == BuildingState.Abandoned ? abandonedMaterial
                       : state == BuildingState.Renovating ? renovatingMaterial
                       : restoredMaterial;
            if (bodyRenderers != null && m != null)
                for (int i = 0; i < bodyRenderers.Length; i++) bodyRenderers[i].sharedMaterial = m;
            SetAll(abandonedObjects, state == BuildingState.Abandoned);
            SetAll(renovatingObjects, state == BuildingState.Renovating);
            SetAll(restoredObjects, state == BuildingState.Restored || state == BuildingState.Active);
        }
    }
}
