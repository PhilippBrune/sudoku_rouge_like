using System;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Top-level screen manager for the single-scene architecture.
    /// Manages three panel groups: Menu, Game, and EndScreen.
    /// Sub-screen navigation within each group is handled by
    /// MenuFlowService (menus) and InRunUiFlowController (game).
    /// </summary>
    public sealed class ScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject menuGroup;
        [SerializeField] private GameObject gameGroup;
        [SerializeField] private GameObject endScreenGroup;

        public AppScreen CurrentScreen { get; private set; } = AppScreen.Menu;

        public event Action<AppScreen> ScreenChanged;

        public void Show(AppScreen screen)
        {
            CurrentScreen = screen;

            if (menuGroup != null) menuGroup.SetActive(screen == AppScreen.Menu);
            if (gameGroup != null) gameGroup.SetActive(screen == AppScreen.Game);
            if (endScreenGroup != null) endScreenGroup.SetActive(screen == AppScreen.EndScreen);

            ScreenChanged?.Invoke(screen);
        }

        public void ShowMenu() => Show(AppScreen.Menu);
        public void ShowGame() => Show(AppScreen.Game);
        public void ShowEndScreen() => Show(AppScreen.EndScreen);

        /// <summary>
        /// Assigns panel group references at runtime (used by blueprint builders).
        /// </summary>
        public void SetGroups(GameObject menu, GameObject game, GameObject endScreen)
        {
            menuGroup = menu;
            gameGroup = game;
            endScreenGroup = endScreen;
        }
    }

    public enum AppScreen
    {
        Menu,
        Game,
        EndScreen
    }
}
