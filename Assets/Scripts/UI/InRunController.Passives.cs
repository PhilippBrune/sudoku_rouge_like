using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    public sealed partial class InRunController
    {
        // [REQ: CLASS-PASSIVE-003] What a caught light or an archive find just did, on the status line.
        private void ShowPassiveNews(RunDirector run)
        {
            if (run == null) return;
            if (run.LastArchiveFind.HasValue)
            {
                var (r, c) = run.LastArchiveFind.Value;
                run.LastArchiveFind = null;
                _boardView?.PlayDigitPlaced(r, c);
                SetStatus(LocalizationService.Format("Class.Archive.Find", "Archive find: the {0} writes itself.", run.CurrentBoard.Cells[r, c]));
                _numpadView?.UpdateButtonStates(run.CurrentBoard.Size, run.CurrentBoard);
                _map?.SaveNow();
            }
            if (run.LastLanternReward == null) return;
            SetStatus(run.LastLanternReward);
            run.LastLanternReward = null;
        }

        // [REQ: ITEM-REWORK-001] The Golden Kintsugi Jar turned the typed digit into the right one.
        private void ShowKintsugiMend(int typed, int written)
        {
            if (typed == written) return;
            SetStatus(LocalizationService.Format("Item.Status.KintsugiMended", "Golden Kintsugi Jar: the {0} became a {1}.", typed, written));
        }
    }
}
