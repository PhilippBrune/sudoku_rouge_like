using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class PauseMenuService
    {
        public bool IsPaused { get; private set; }

        public bool CanRestartLevel(RunState runState)
        {
            return runState != null && runState.TutorialMode;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }
    }
}
