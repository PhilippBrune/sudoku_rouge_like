using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class PauseRunController : MonoBehaviour
    {
        private PauseMenuService _pauseService;
        private System.Action _onQuitRequested;

        public void Bind(PauseMenuService pauseService, System.Action onQuitRequested)
        {
            _pauseService = pauseService;
            _onQuitRequested = onQuitRequested;
        }

        public void Pause()
        {
            _pauseService?.SetPaused(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            _pauseService?.SetPaused(false);
            Time.timeScale = 1f;
        }

        public void QuitToMenu()
        {
            Resume();
            _onQuitRequested?.Invoke();
        }
    }
}
