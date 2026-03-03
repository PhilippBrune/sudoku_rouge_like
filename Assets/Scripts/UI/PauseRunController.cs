using SudokuRoguelike.Run;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class PauseRunController : MonoBehaviour
    {
        private RunDirector _run;

        public void Bind(RunDirector run)
        {
            _run = run;
        }

        public void Pause()
        {
            _run?.OnPauseRequested();
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            Time.timeScale = 1f;
        }

        public void QuitToMenu()
        {
            _run?.OnQuitRequested();
        }
    }
}
