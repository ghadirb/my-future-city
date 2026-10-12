using UnityEngine;
using UnityEngine.EventSystems;

namespace MyFutureCity
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        public float distance = 7f;
        public float minDistance = 1.5f;
        public float minPitch = 8f;
        public float maxPitch = 60f;
        public float collisionRadius = 0.3f;
        public float followSmoothTime = 0.08f;
        public float degreesPerInch = 110f;

        private float yaw;
        private float pitch = 28f;
        private int lookFinger = -1;
        private Vector3 smoothedPivot;
        private Vector3 pivotVelocity;
        private Vector3 lastMouse;

        private void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                smoothedPivot = target.position + pivotOffset;
            }
        }

        private float Sensitivity()
        {
            return GameContext.Instance.Save.settings.cameraSensitivity;
        }

        private void ReadLook()
        {
            float dpi = Screen.dpi > 1f ? Screen.dpi : 160f;
            float k = degreesPerInch / dpi * Sensitivity();

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
                    if (lookFinger < 0 && !overUi && t.position.x > Screen.width * 0.35f) lookFinger = t.fingerId;
                }
                else if (t.fingerId == lookFinger)
                {
                    if (t.phase == TouchPhase.Moved)
                    {
                        yaw += t.deltaPosition.x * k;
                        pitch -= t.deltaPosition.y * k;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        lookFinger = -1;
                    }
                }
            }

            // Desktop / editor testing: hold right mouse button and drag.
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(1)) lastMouse = Input.mousePosition;
                if (Input.GetMouseButton(1))
                {
                    Vector3 d = Input.mousePosition - lastMouse;
                    lastMouse = Input.mousePosition;
                    yaw += d.x * 0.2f * Sensitivity();
                    pitch -= d.y * 0.2f * Sensitivity();
                }
                lookFinger = -1;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (!GameInput.Blocked) ReadLook();
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Vector3 wantedPivot = target.position + pivotOffset;
            smoothedPivot = Vector3.SmoothDamp(smoothedPivot, wantedPivot, ref pivotVelocity, followSmoothTime);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rot * Vector3.back;
            float dist = distance;
            RaycastHit hit;
            if (Physics.SphereCast(smoothedPivot, collisionRadius, back, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(minDistance, hit.distance);

            transform.position = smoothedPivot + back * dist;
            transform.rotation = rot;
        }
    }
}
