using UnityEngine;

namespace MyFutureCity
{
    public class MenuBackdropSpin : MonoBehaviour
    {
        public float degreesPerSecond = 8f;
        private void Update() { transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World); }
    }
}
