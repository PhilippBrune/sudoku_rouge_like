using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Lightweight accessibility caption bar used as a fallback for important status updates.
    /// This mirrors announcements on-screen without requiring OS-level screen reader APIs.
    /// </summary>
    public sealed class AccessibilityAnnouncementOverlay : MonoBehaviour
    {
        private static AccessibilityAnnouncementOverlay _instance;

        private readonly List<string> _history = new List<string>();
        private Text _label;
        private CanvasGroup _canvasGroup;
        private float _hideAtRealtime;
        private bool _enabled = true;

        public static void Announce(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var overlay = EnsureInstance();
            if (overlay == null || !overlay._enabled) return;

            overlay.Show(text);
        }

        public static void SetEnabled(bool enabled)
        {
            var overlay = EnsureInstance();
            if (overlay == null) return;

            overlay._enabled = enabled;
            if (!enabled)
                overlay.HideImmediate();
        }

        public static IReadOnlyList<string> GetHistory()
        {
            return _instance?._history;
        }

        private static AccessibilityAnnouncementOverlay EnsureInstance()
        {
            if (_instance != null) return _instance;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;

            var root = new GameObject("AccessibilityAnnouncementOverlay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            _instance = root.AddComponent<AccessibilityAnnouncementOverlay>();
            _instance.Initialize();
            return _instance;
        }

        private void Initialize()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.14f, 0.02f);
            rect.anchorMax = new Vector2(0.86f, 0.10f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.06f, 0.09f, 0.90f);
            background.raycastTarget = false;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);

            _label = textGo.GetComponent<Text>();
            _label.font = FontAssetService.GetFont();
            _label.fontSize = 14;
            _label.alignment = TextAnchor.MiddleLeft;
            _label.color = GamePalette.TextPrimary;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.raycastTarget = false;
        }

        private void Update()
        {
            if (_canvasGroup == null || _canvasGroup.alpha <= 0f) return;
            if (Time.unscaledTime < _hideAtRealtime) return;

            HideImmediate();
        }

        private void Show(string text)
        {
            if (_label == null || _canvasGroup == null) return;

            _label.text = text;
            _canvasGroup.alpha = 1f;
            _hideAtRealtime = Time.unscaledTime + 3f;

            _history.Add(text);
            if (_history.Count > 30)
                _history.RemoveAt(0);
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _hideAtRealtime = 0f;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
