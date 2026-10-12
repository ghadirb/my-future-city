using System.Collections.Generic;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Base class for everything the player can use with the interact button.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public string promptFa = Strings.Interact;
        public float radius = 2.5f;

        private static readonly List<Interactable> All = new List<Interactable>();

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public virtual bool IsAvailable(GameContext ctx) { return true; }
        public virtual string GetPrompt(GameContext ctx) { return promptFa; }
        public abstract void Interact(GameSceneController scene);

        /// <summary>Nearest available interactable in range that the player is not turned away from.</summary>
        public static Interactable FindBest(Vector3 position, Vector3 forward, GameContext ctx)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var it = All[i];
                if (it == null || !it.isActiveAndEnabled) continue;
                Vector3 d = it.transform.position - position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > it.radius) continue;
                if (!it.IsAvailable(ctx)) continue;
                float facing = dist < 0.8f ? 1f : Vector3.Dot(forward, d / dist);
                if (facing < -0.25f) continue;
                float score = dist - facing * 0.8f;
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }
    }
}
