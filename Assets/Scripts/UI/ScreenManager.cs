using System;
using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class ScreenManager : MonoBehaviour
    {
        private GameObject _menuGroup;
        private GameObject _gameGroup;
        private GameObject _endScreenGroup;

        public AppScreen CurrentScreen { get; private set; } = AppScreen.Menu;

        public event Action<AppScreen> ScreenChanged;

        public void SetGroups(GameObject menu, GameObject game, GameObject endScreen)
        {
            _menuGroup = menu;
            _gameGroup = game;
            _endScreenGroup = endScreen;
        }

        public void Show(AppScreen screen)
        {
            CurrentScreen = screen;

            if (_menuGroup != null) _menuGroup.SetActive(screen == AppScreen.Menu);
            if (_gameGroup != null) _gameGroup.SetActive(screen == AppScreen.Game);
            if (_endScreenGroup != null) _endScreenGroup.SetActive(screen == AppScreen.EndScreen);

            ScreenChanged?.Invoke(screen);
        }

        public void ShowMenu() => Show(AppScreen.Menu);
        public void ShowGame() => Show(AppScreen.Game);
        public void ShowEndScreen() => Show(AppScreen.EndScreen);
    }
}
