using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Handles menu-level keyboard navigation that is not covered by the grid input
    /// in PrototypeRunScreenController: Escape to go back/close panels, Tab/Shift+Tab
    /// for UI button focus, Space/Enter to activate focused button, Space to skip
    /// XP animation.
    /// </summary>
    public sealed class KeyboardNavigationController : MonoBehaviour
    {
        private Selectable _currentFocus;
        private GameObject _focusIndicator;
        private RectTransform _focusRect;

        private void Update()
        {
            HandleEscape();
            HandleTabNavigation();
            HandleActivation();
        }

        private void HandleEscape()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            if (!UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) return;
#else
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
#endif
            // Find the top-most active panel and try to close it
            // Look for common panel close buttons
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Try to find and click a "back" or "close" button on any active panel
            if (TryClickButton(canvas, "BackButton")) return;
            if (TryClickButton(canvas, "CloseButton")) return;
            if (TryClickButton(canvas, "CancelButton")) return;
            if (TryClickButton(canvas, "SaveQuitSudoku")) return;
            TryClickButton(canvas, "SaveQuitPath");
        }

        private static bool TryClickButton(Canvas canvas, string name)
        {
            var t = canvas.transform.FindDeepChild(name);
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            var btn = t.GetComponent<Button>();
            if (btn == null || !btn.interactable) return false;
            btn.onClick.Invoke();
            return true;
        }

        private void HandleTabNavigation()
        {
            bool tabPressed;
            bool shiftHeld;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            tabPressed = kb.tabKey.wasPressedThisFrame;
            shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
#else
            tabPressed = Input.GetKeyDown(KeyCode.Tab);
            shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
            if (!tabPressed) return;

            var allSelectables = Selectable.allSelectablesArray;
            if (allSelectables.Length == 0) return;

            // Find current index
            var currentIdx = -1;
            for (var i = 0; i < allSelectables.Length; i++)
            {
                if (allSelectables[i] == _currentFocus && allSelectables[i].gameObject.activeInHierarchy)
                {
                    currentIdx = i;
                    break;
                }
            }

            // Move to next/previous
            var direction = shiftHeld ? -1 : 1;
            var startIdx = currentIdx < 0 ? 0 : currentIdx + direction;
            var count = allSelectables.Length;

            for (var attempt = 0; attempt < count; attempt++)
            {
                var idx = ((startIdx + attempt * direction) % count + count) % count;
                var candidate = allSelectables[idx];
                if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.interactable)
                {
                    SetFocus(candidate);
                    return;
                }
            }
        }

        private void HandleActivation()
        {
            if (_currentFocus == null) return;

            bool activated;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            activated = kb.enterKey.wasPressedThisFrame ||
                        kb.numpadEnterKey.wasPressedThisFrame ||
                        kb.spaceKey.wasPressedThisFrame;
#else
            activated = Input.GetKeyDown(KeyCode.Return) ||
                        Input.GetKeyDown(KeyCode.KeypadEnter) ||
                        Input.GetKeyDown(KeyCode.Space);
#endif
            if (!activated) return;

            if (_currentFocus is Button btn && btn.interactable)
            {
                btn.onClick.Invoke();
            }
            else if (_currentFocus is Toggle toggle && toggle.interactable)
            {
                toggle.isOn = !toggle.isOn;
            }
        }

        private void SetFocus(Selectable selectable)
        {
            _currentFocus = selectable;
            selectable.Select();
            UpdateFocusIndicator(selectable);
        }

        private void UpdateFocusIndicator(Selectable selectable)
        {
            if (_focusIndicator == null)
            {
                _focusIndicator = new GameObject("KeyboardFocusIndicator", typeof(RectTransform), typeof(Image));
                _focusIndicator.transform.SetParent(FindFirstObjectByType<Canvas>()?.transform, false);
                var img = _focusIndicator.GetComponent<Image>();
                img.color = new Color(1f, 1f, 0f, 0.5f); // Yellow highlight
                img.raycastTarget = false;
                _focusRect = _focusIndicator.GetComponent<RectTransform>();
            }

            _focusIndicator.SetActive(true);

            // Position the indicator around the focused element
            var targetRect = selectable.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                _focusIndicator.SetActive(false);
                return;
            }

            // Match the focused element's position and size with a small border
            _focusRect.SetParent(targetRect, false);
            _focusRect.anchorMin = Vector2.zero;
            _focusRect.anchorMax = Vector2.one;
            _focusRect.offsetMin = new Vector2(-2f, -2f);
            _focusRect.offsetMax = new Vector2(2f, 2f);

            // Ensure indicator renders behind the content but visible
            _focusIndicator.transform.SetAsFirstSibling();
        }

        public void ClearFocus()
        {
            _currentFocus = null;
            if (_focusIndicator != null)
                _focusIndicator.SetActive(false);
        }
    }

    internal static class TransformExtensions
    {
        /// <summary>
        /// Recursively find a child by name.
        /// </summary>
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var result = child.FindDeepChild(name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
