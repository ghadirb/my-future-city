using UnityEngine;
using UnityEngine.EventSystems;

namespace MyFutureCity
{
    public class JoystickView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform baseRect;
        public RectTransform knob;
        public float radius = 80f;

        public void OnPointerDown(PointerEventData e) { OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, e.pressEventCamera, out local)) return;
            Vector2 v = Vector2.ClampMagnitude(local, radius);
            if (knob != null) knob.anchoredPosition = v;
            GameInput.Move = v / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            GameInput.Move = Vector2.zero;
        }

        private void OnDisable()
        {
            GameInput.Move = Vector2.zero;
        }
    }
}
