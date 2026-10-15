using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Gently bobs a UI element up and down (uses unscaled time so it also works while paused).</summary>
    public class UIBob : MonoBehaviour
    {
        public float amplitude = 6f;
        public float speed = 2f;
        public float phase;

        private RectTransform rt;
        private Vector2 basePos;

        private void Awake()
        {
            rt = transform as RectTransform;
            if (rt != null) basePos = rt.anchoredPosition;
        }

        private void Update()
        {
            if (rt == null) return;
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * speed + phase) * amplitude);
        }
    }
}
