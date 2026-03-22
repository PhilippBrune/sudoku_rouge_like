using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Renders a bright dot as the player's position indicator on the garden path canvas.
    /// Will be swapped for per-class sprites in a future pass.
    /// </summary>
    public sealed class PlayerIconController
    {
        private GameObject _iconGo;
        private RectTransform _iconRect;
        private Image _iconImage;
        private Vector2 _currentPos;
        private Vector2 _targetPos;
        private bool _animating;
        private float _animT;
        private const float AnimDuration = 0.4f;

        private static Sprite _dotSprite;

        public void EnsureCreated(RectTransform parent)
        {
            if (_iconGo != null) return;

            _iconGo = new GameObject("PlayerIcon", typeof(RectTransform), typeof(Image));
            _iconGo.transform.SetParent(parent, false);
            _iconRect = _iconGo.GetComponent<RectTransform>();
            _iconRect.anchorMin = new Vector2(0f, 1f);
            _iconRect.anchorMax = new Vector2(0f, 1f);
            _iconRect.pivot = new Vector2(0.5f, 0.5f);
            _iconRect.sizeDelta = new Vector2(24f, 24f);
            _iconImage = _iconGo.GetComponent<Image>();
            _iconImage.sprite = GetDotSprite();
            _iconImage.color = new Color(1f, 0.92f, 0.5f, 1f);
            _iconImage.raycastTarget = false;
            _iconGo.transform.SetAsLastSibling();
        }

        public void SetPosition(Vector2 canvasPos, RectTransform canvasRect)
        {
            if (_iconRect == null) return;
            var pos = CanvasToAnchored(canvasPos, canvasRect);
            _iconRect.anchoredPosition = pos;
            _currentPos = pos;
            _targetPos = pos;
            _animating = false;
        }

        public void AnimateTo(Vector2 canvasPos, RectTransform canvasRect)
        {
            if (_iconRect == null) return;
            _currentPos = _iconRect.anchoredPosition;
            _targetPos = CanvasToAnchored(canvasPos, canvasRect);
            _animT = 0f;
            _animating = true;
        }

        public void Update()
        {
            if (!_animating || _iconRect == null) return;
            _animT += Time.deltaTime / AnimDuration;
            if (_animT >= 1f)
            {
                _animT = 1f;
                _animating = false;
            }

            var t = Mathf.SmoothStep(0f, 1f, _animT);
            _iconRect.anchoredPosition = Vector2.Lerp(_currentPos, _targetPos, t);
        }

        public void SetVisible(bool visible)
        {
            if (_iconGo != null) _iconGo.SetActive(visible);
        }

        public void BringToFront()
        {
            if (_iconGo != null) _iconGo.transform.SetAsLastSibling();
        }

        private static Vector2 CanvasToAnchored(Vector2 normalised, RectTransform canvasRect)
        {
            // anchor is top-left (0,1), so x goes right and y goes down (negative)
            var w = canvasRect.rect.width;
            var h = canvasRect.rect.height;
            return new Vector2(normalised.x * w, -normalised.y * h);
        }

        private static Sprite GetDotSprite()
        {
            if (_dotSprite != null) return _dotSprite;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var center = size * 0.5f - 0.5f;
            var r = center;
            for (var py = 0; py < size; py++)
            {
                for (var px = 0; px < size; px++)
                {
                    var dist = Mathf.Sqrt((px - center) * (px - center) + (py - center) * (py - center));
                    var alpha = Mathf.Clamp01(r - dist + 0.5f);
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            _dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _dotSprite;
        }
    }
}
