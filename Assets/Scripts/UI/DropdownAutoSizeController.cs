using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class DropdownAutoSizeController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
    {
        [SerializeField] private int maxVisibleItems = 5;
        [SerializeField] private float itemHeight = 30f;
        [SerializeField] private float padding = 8f;
        private const float InteractionHoldSeconds = 1.25f;

        private static float _lastInteractionUnscaledTime = -10f;

        public static bool HasRecentGlobalInteraction
        {
            get
            {
                var now = Time.unscaledTime;
                return (now - _lastInteractionUnscaledTime) <= InteractionHoldSeconds;
            }
        }

        private Dropdown _dropdown;
        private RectTransform _template;
        private ScrollRect _scroll;
        private RectTransform _content;

        private void Awake()
        {
            _dropdown = GetComponent<Dropdown>();
            _template = _dropdown != null ? _dropdown.template : null;
            _scroll = _template != null ? _template.GetComponent<ScrollRect>() : null;
            _content = _scroll != null ? _scroll.content : null;

            if (_dropdown != null)
            {
                _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
            {
                _dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }

        private void LateUpdate()
        {
            if (_dropdown == null || _template == null)
            {
                return;
            }

            var count = _dropdown.options != null ? _dropdown.options.Count : 0;
            if (count <= 0)
            {
                count = 1;
            }

            var visible = Mathf.Min(maxVisibleItems, count);
            var height = (visible * itemHeight) + (padding * 2f);
            _template.sizeDelta = new Vector2(_template.sizeDelta.x, height);

            if (_scroll != null)
            {
                _scroll.vertical = count > maxVisibleItems;
                _scroll.movementType = ScrollRect.MovementType.Clamped;
            }

            if (_content != null)
            {
                var contentHeight = (count * itemHeight) + (padding * 2f);
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, contentHeight);
            }

            if (_template.gameObject.activeInHierarchy)
            {
                MarkInteraction();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MarkInteraction();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            MarkInteraction();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            MarkInteraction();
        }

        public void OnSelect(BaseEventData eventData)
        {
            MarkInteraction();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            MarkInteraction();
        }

        private void OnDropdownValueChanged(int _)
        {
            MarkInteraction();
        }

        private void MarkInteraction()
        {
            var now = Time.unscaledTime;
            _lastInteractionUnscaledTime = now;
        }
    }
}
