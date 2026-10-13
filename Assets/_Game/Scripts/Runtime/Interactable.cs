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
        /// <summary>True when this object is the next destination for the active mission.</summary>
        public virtual bool MatchesCurrentObjective(GameContext ctx) { return false; }
        /// <summary>True when the guide arrow / 3D marker should point at this object.</summary>
        public virtual bool IsGuideTarget(GameContext ctx) { return MatchesCurrentObjective(ctx); }
        /// <summary>Short logical Persian name shown next to the guide arrow.</summary>
        public virtual string GuideName { get { return ""; } }
        public virtual string GetPrompt(GameContext ctx) { return promptFa; }
        public abstract void Interact(GameSceneController scene);

        /// <summary>Finds the closest usable object that advances the current objective.</summary>
        public static Interactable FindObjectiveTarget(Vector3 position, GameContext ctx)
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var it = All[i];
                if (it == null || !it.isActiveAndEnabled || !it.IsGuideTarget(ctx)) continue;
                Vector3 delta = it.transform.position - position;
                delta.y = 0f;
                float distance = delta.sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = it; }
            }
            return best;
        }

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
