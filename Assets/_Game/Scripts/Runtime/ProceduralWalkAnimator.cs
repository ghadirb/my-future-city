using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Placeholder limb swing for primitive characters (real animations come with real models).</summary>
    public class ProceduralWalkAnimator : MonoBehaviour
    {
        public PlayerController source;
        public Transform leftArm, rightArm, leftLeg, rightLeg;
        public float swing = 45f;
        private float phase;

        private void Update()
        {
            float s = source != null ? source.Speed01 : 0f;
            phase += Time.deltaTime * Mathf.Lerp(0f, 14f, s);
            float a = Mathf.Sin(phase) * swing * Mathf.Clamp01(s * 2f);
            Set(leftLeg, a);
            Set(rightLeg, -a);
            Set(leftArm, -a);
            Set(rightArm, a);
        }

        private static void Set(Transform t, float angle)
        {
            if (t != null) t.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
