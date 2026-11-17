using UnityEngine;
using UnityEngine.EventSystems;

namespace Deadzone.UI
{
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 60f;

        public Vector2 InputDirection { get; private set; } = Vector2.zero;
        public bool IsPressed { get; private set; } = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            Vector2 position = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, background.position);
            Vector2 radius = background.sizeDelta / 2f;
            Vector2 rawDirection = (eventData.position - position) / (radius * handleRange / 100f);

            InputDirection = (rawDirection.magnitude > 1f) ? rawDirection.normalized : rawDirection;
            handle.anchoredPosition = InputDirection * (radius * handleRange / 100f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            InputDirection = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
