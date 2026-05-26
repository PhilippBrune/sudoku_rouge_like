using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Extension helpers for <see cref="Button"/> to guard against null references
    /// and destroyed-object invocations on dynamically-built UI panels.
    /// </summary>
    public static class ButtonExtensions
    {
        /// <summary>
        /// Invokes <see cref="Button.onClick"/> only if the button reference is
        /// non-null and its <see cref="GameObject"/> is active in the hierarchy.
        /// Logs a warning when the invocation is skipped so silent no-ops are diagnosable.
        /// </summary>
        /// <param name="btn">The button to invoke (may be null).</param>
        /// <param name="callerTag">Optional label included in the warning message.</param>
        public static void SafeInvoke(this Button btn, string callerTag = "")
        {
            if (btn != null && btn.gameObject.activeInHierarchy)
            {
                btn.onClick.Invoke();
            }
            else
            {
                var tag = string.IsNullOrEmpty(callerTag) ? "" : $" ({callerTag})";
                Debug.LogWarning($"[ButtonExtensions] SafeInvoke skipped{tag} — button is null or its GameObject is inactive.");
            }
        }
    }
}
