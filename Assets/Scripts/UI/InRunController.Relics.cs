using System.Linq;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    // The signature relics that act on the puzzle screen (RELIC-SIG-002).
    public sealed partial class InRunController
    {
        // Ink Stone: erasing pencil marks until one is left writes that digit, through the normal digit path (a wrong one
        // is a mistake like any other).
        private void InkStoneAfterPencil(int toggled)
        {
            var run = _map?.Run;
            var board = run?.CurrentBoard;
            if (run == null || board == null || _selectedRow < 0 || _selectedCol < 0) return;
            if (!RelicService.HasRelicOfType(run.State, RelicId.InkStone)) return;
            var marks = board.GetPencilMarks(_selectedRow, _selectedCol);
            if (marks.Contains(toggled) || marks.Count != 1) return;
            // The erase was the first press: with Double Tap Confirm on, the relic's digit is the second (OPT-GAME-005).
            _armedDigit = marks.First();
            _armedCell = (_selectedRow, _selectedCol);
            _armedTime = UnityEngine.Time.realtimeSinceStartup;
            _pencilMode = false;
            try { EnterNumber(marks.First()); }
            finally { _pencilMode = true; }
        }

        // The status line after a digit that was not placed: already there, or taken back by Single Stroke.
        private string PlacementNote(PlaceResult result, int value) => result == PlaceResult.TakenBack
            ? Tf("InRun.SingleStroke", value)
            : Tf("{0} is already there.", value);
    }
}
