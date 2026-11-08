using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: EVENT-CHALLENGE-001] The puzzle challenge: a clock on the board that counts down; solved gives the prize, out of time goes
    // straight back to the path without one. The clock runs on while the Options are open and is saved with the run.
    public sealed partial class InRunController
    {
        private Text _challengeClock;

        private void StartChallengePuzzle()
        {
            var config = _map?.Run?.TakePendingChallenge();
            if (config == null) return;
            _map.StartPuzzleAsync(config);
            _completionHandled = false;
            _boardView.MarkOverlayDirty();
            _selectedRow = _selectedCol = -1;
            _highlightValue = 0;
            CancelPendingItemInteraction(false);
            _bagHighlightCells.Clear();
            _bagHighlightEndTime = 0f;
            _pendingAsyncLevel = config;
            ShowPath();
            ShowBossAwakeningPanel("Creating Puzzle");
        }

        private void TickChallenge(RunDirector run)
        {
            if (run.State.ChallengeKind == 0 || run.CurrentBoard == null)
            {
                if (_challengeClock != null) _challengeClock.gameObject.SetActive(false);
                return;
            }
            if (_challengeClock == null)
            {
                _challengeClock = ScrollUiKit.Label(_sudokuPanel.transform, "ChallengeClock", string.Empty, 44, TextAnchor.MiddleCenter, ScrollUiKit.Vermilion,
                    new Vector2(0.38f, 0.905f), new Vector2(0.62f, 0.955f), 24);
                _challengeClock.gameObject.AddComponent<Outline>().effectColor = new Color(0.95f, 0.91f, 0.82f, 0.9f);
            }
            _challengeClock.gameObject.SetActive(true);
            run.State.ChallengeSecondsLeft -= Time.unscaledDeltaTime;
            var left = Mathf.Max(0, Mathf.CeilToInt(run.State.ChallengeSecondsLeft));
            _challengeClock.text = $"{left / 60}:{left % 60:00}";
            if (run.State.ChallengeSecondsLeft <= 0f) EndChallenge(run, false);
        }

        private void EndChallenge(RunDirector run, bool solved)
        {
            if (solved)
            {
                _audio?.PlayPuzzleSolved();
                _boardView?.FlashBoardSolved();
            }
            if (run.State != null) run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
            _hudView?.StopPuzzleTimer();
            run.State.PuzzleClockSeconds = 0f;
            CancelPendingItemInteraction(false);
            var line = run.FinishChallenge(solved);
            if (_challengeClock != null) _challengeClock.gameObject.SetActive(false);
            _map?.SaveNow();
            _pathMessage = line;
            ShowPath();
        }
    }
}
