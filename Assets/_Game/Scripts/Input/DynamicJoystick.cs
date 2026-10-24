using UnityEngine;
using UnityEngine.EventSystems;

namespace KillThemAll.Input
{
    public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Visual Components")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("Configuration")]
        [SerializeField] private float handleRange = 1f;
        [SerializeField] private float deadZone = 0.05f;
        [SerializeField] private bool hideOnRelease = true;

        private Canvas _canvas;
        private Camera _camera;
        private Vector2 _input = Vector2.zero;
        private Vector2 _defaultBackgroundPosition;
        private bool _isPressed = false;

        public Vector2 Direction => _input;
        public float Horizontal => _input.x;
        public float Vertical => _input.y;
        public bool IsPressed => _isPressed;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("[DynamicJoystick] Canvas bulunamadı!");
            }

            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _camera = _canvas.worldCamera;
            }

            if (background != null)
            {
                _defaultBackgroundPosition = background.anchoredPosition;
                if (hideOnRelease)
                {
                    background.gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            if (background != null)
            {
                if (hideOnRelease)
                {
                    background.gameObject.SetActive(true);
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform,
                    eventData.position,
                    _camera,
                    out Vector2 localPoint
                );
                background.anchoredPosition = localPoint;
                if (handle != null)
                {
                    handle.anchoredPosition = Vector2.zero;
                }
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            Vector2 position = RectTransformUtility.WorldToScreenPoint(_camera, background.position);
            Vector2 radius = background.sizeDelta / 2f;
            Vector2 rawInput = (eventData.position - position) / (radius * (_canvas != null ? _canvas.scaleFactor : 1f));

            FormatInput(rawInput);
            handle.anchoredPosition = _input * radius * handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _input = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (background != null && hideOnRelease)
            {
                background.gameObject.SetActive(false);
            }
        }

        private void FormatInput(Vector2 rawInput)
        {
            if (rawInput.magnitude > 1f)
            {
                _input = rawInput.normalized;
            }
            else
            {
                _input = rawInput;
            }

            if (_input.magnitude < deadZone)
            {
                _input = Vector2.zero;
            }
        }
    }
}
