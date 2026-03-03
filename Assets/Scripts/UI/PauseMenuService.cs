using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    public sealed class PauseMenuService
    {
        public bool CanRestartLevel(RunState runState)
        {
            return runState != null && runState.TutorialMode;
        }

        public bool TryRestartLevel(RunState runState)
        {
            if (!CanRestartLevel(runState))
            {
                return false;
            }

            return true;
        }

        public void AbandonRun(MenuFlowService menu)
        {
            menu.Session.HasRunInProgress = false;
            menu.QuitToMain();
        }
    }
}
