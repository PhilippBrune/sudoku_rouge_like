using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SudokuRoguelike.UI
{
    /// <summary>Calls <see cref="OnFocus"/> when the controller's (or the mouse's) selection lands on this object: the Codex's
    /// tiles (UI-STYLE-018) and the class garden's plaques (UI-STYLE-019) show their entry this way.</summary>
    public sealed class FocusNotifier : MonoBehaviour, ISelectHandler
    {
        public Action OnFocus;
        public void OnSelect(BaseEventData eventData) => OnFocus?.Invoke();
    }
}
