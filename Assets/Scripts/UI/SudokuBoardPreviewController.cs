using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class SudokuBoardPreviewController : MonoBehaviour
    {
        private RunMapController _runMapController;
        private Text _boardText;
        private Text _statusText;
        private float _nextRefreshTime;

        public void Configure(RunMapController runMap, Text board, Text status)
        {
            _runMapController = runMap;
            _boardText = board;
            _statusText = status;
            RenderNow();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime) return;
            _nextRefreshTime = Time.unscaledTime + 0.2f;
            RenderNow();
        }

        private void RenderNow()
        {
            if (_runMapController == null)
                _runMapController = FindFirstObjectByType<RunMapController>();

            var run = _runMapController?.Run;
            var board = run?.CurrentBoard;
            if (board == null)
            {
                if (_boardText != null)
                    _boardText.text = "Sudoku board not initialized yet.";
                if (_statusText != null)
                    _statusText.text = "Hint: Start a run from Main Menu.";
                return;
            }

            var builder = new StringBuilder();
            var size = board.Size;
            for (var row = 0; row < size; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    var value = board.Cells[row, col];
                    builder.Append(value == 0 ? "." : value.ToString());
                    if (col < size - 1) builder.Append(' ');
                }
                if (row < size - 1) builder.AppendLine();
            }

            if (_boardText != null)
                _boardText.text = builder.ToString();

            if (_statusText != null)
            {
                var solved = run.IsLevelComplete;
                _statusText.text =
                    $"Board: {size}x{size} | Stars: {run.CurrentLevelConfig?.Stars ?? 0} | Solved: {(solved ? "Yes" : "No")}";
            }
        }
    }
}
