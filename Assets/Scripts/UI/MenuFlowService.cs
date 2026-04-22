using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class MenuFlowService
    {
        private readonly Stack<MenuScreen> _history = new Stack<MenuScreen>();
        private readonly Dictionary<MenuScreen, GameObject> _panels = new Dictionary<MenuScreen, GameObject>();

        /// <summary>Fired with the newly-active panel immediately after it is shown.</summary>
        public Action<GameObject> OnPanelShown;

        public MenuScreen CurrentScreen { get; private set; } = MenuScreen.MainMenu;

        public GameObject CurrentPanel =>
            _panels.TryGetValue(CurrentScreen, out var p) ? p : null;

        public void RegisterPanel(MenuScreen screen, GameObject panel)
        {
            _panels[screen] = panel;
        }

        public void Show(MenuScreen screen)
        {
            if (CurrentScreen != screen)
                _history.Push(CurrentScreen);

            SetActive(CurrentScreen, false);
            CurrentScreen = screen;
            SetActive(CurrentScreen, true);
        }

        public void Back()
        {
            if (_history.Count == 0) return;

            SetActive(CurrentScreen, false);
            CurrentScreen = _history.Pop();
            SetActive(CurrentScreen, true);
        }

        public void ShowMainMenu()
        {
            _history.Clear();
            HideAll();
            CurrentScreen = MenuScreen.MainMenu;
            SetActive(MenuScreen.MainMenu, true);
        }

        private void SetActive(MenuScreen screen, bool active)
        {
            if (_panels.TryGetValue(screen, out var panel) && panel != null)
            {
                panel.SetActive(active);
                if (active) OnPanelShown?.Invoke(panel);
            }
        }

        private void HideAll()
        {
            foreach (var kvp in _panels)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(false);
            }
        }
    }
}
