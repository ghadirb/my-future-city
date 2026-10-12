using UnityEngine;

namespace MyFutureCity
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 3.2f;
        public float runSpeed = 5.5f;
        public float turnSpeed = 540f;
        public float gravity = -22f;
        public Transform cameraTransform;
        public Vector3 respawnPosition;

        private CharacterController controller;
        private float verticalVelocity;

        /// <summary>0..1 fraction of run speed; drives the walk animation.</summary>
        public float Speed01 { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (respawnPosition == Vector3.zero) respawnPosition = transform.position;
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private static Vector2 ReadInput()
        {
            Vector2 v = GameInput.Move;
            float kx = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                     - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float ky = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                     - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            Vector2 k = new Vector2(kx, ky);
            if (k.sqrMagnitude > v.sqrMagnitude) v = Vector2.ClampMagnitude(k, 1f);
            return v;
        }

        private void Update()
        {
            Vector2 input = GameInput.Blocked ? Vector2.zero : ReadInput();
            float mag = Mathf.Clamp01(input.magnitude);
            Vector3 move = Vector3.zero;

            if (mag > 0.05f)
            {
                Vector3 fwd = cameraTransform != null
                    ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)
                    : Vector3.forward;
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                fwd.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, fwd);
                Vector3 dir = (fwd * input.y + right * input.x).normalized;

                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);

                // Small stick deflection = walk, full deflection = run.
                float speed = mag < 0.6f
                    ? Mathf.Lerp(0f, walkSpeed, mag / 0.6f)
                    : Mathf.Lerp(walkSpeed, runSpeed, (mag - 0.6f) / 0.4f);
                move = dir * speed;
            }

            Speed01 = Mathf.Clamp01(move.magnitude / runSpeed);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (transform.position.y < -10f) Respawn();
        }

        public void Respawn()
        {
            controller.enabled = false;
            transform.position = respawnPosition;
            controller.enabled = true;
            verticalVelocity = 0f;
        }
    }
}
