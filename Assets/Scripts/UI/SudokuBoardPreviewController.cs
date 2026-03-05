using System.Text;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class SudokuBoardPreviewController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private Text boardText;
        [SerializeField] private Text statusText;

        private float _nextRefreshTime;

        public void Configure(RunMapController runMap, Text board, Text status)
        {
            runMapController = runMap;
            boardText = board;
            statusText = status;
            RenderNow();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + 0.2f;
            RenderNow();
        }

        private void RenderNow()
        {
            if (runMapController == null)
            {
                runMapController = FindFirstObjectByType<RunMapController>();
            }

            var run = runMapController?.Run;
            var board = run?.CurrentBoard;
            if (board == null)
            {
                if (boardText != null)
                {
                    boardText.text = "Sudoku board not initialized yet.";
                }

                if (statusText != null)
                {
                    statusText.text = "Hint: Start a run from Main Menu.";
                }

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
                    if (col < size - 1)
                    {
                        builder.Append(' ');
                    }
                }

                if (row < size - 1)
                {
                    builder.AppendLine();
                }
            }

            if (boardText != null)
            {
                boardText.text = builder.ToString();
            }

            if (statusText != null)
            {
                var solved = run.CurrentLevelState != null && run.CurrentLevelState.PuzzleComplete;
                statusText.text =
                    $"Board: {size}x{size} | Stars: {run.CurrentLevelConfig?.Stars ?? 0} | Solved: {(solved ? "Yes" : "No")}. " +
                    "Use your existing input flow to place numbers.";
            }
        }
    }
}
