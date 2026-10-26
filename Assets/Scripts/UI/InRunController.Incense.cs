using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    // [REQ: INCENSE-CLOCK-004] [REQ: INCENSE-CLOCK-005] The Incense Walk on the puzzle screen: the stick burns while a
    // puzzle is open and the run menu is shut; a frame longer than a quarter second (drawing the next garden) does not
    // burn; at 0:00 the walk ends at once, also in the middle of a puzzle.
    public sealed partial class InRunController
    {
        private const float IncenseMaxFrame = 0.25f;
        private int _incenseBest = -1;

        // [REQ: REQ-UI-101] The header over the board: "Floor 2  Depth 5  7×7 3★"; in the Incense Walk the puzzle's number.
        private static string LevelInfoText(RunDirector run, Sudoku.SudokuBoard board)
        {
            var stars = run.CurrentLevelConfig?.Stars ?? 0;
            return run.State.Mode == GameMode.IncenseWalk
                ? LocalizationService.Format("Incense.Hud.Header", "Incense Walk · puzzle {0}  {1}×{1}  {2}★", run.State.Depth + 1, board.Size, stars)
                : LocalizationService.Format("InRun.LevelInfo", "Floor {0}  Depth {1}  {2}×{2}  {3}★",
                    run.State.CurrentFloor + 1, run.State.Depth, board.Size, stars);
        }

        private void TickIncense(RunDirector run)
        {
            var walk = run?.Incense;
            if (walk == null || run.State?.Mode != GameMode.IncenseWalk)
            {
                _incenseBest = -1;
                _hudView?.ShowIncense(null, null, null, 0, default);
                return;
            }
            if (_incenseBest < 0)
            {
                var save = new SaveFileService(SaveProfileService.ActiveSlot);
                _incenseBest = save.HasSaveFile() ? save.Load().Statistics?.IncenseBestScore ?? 0 : 0;
                SetStatus(T("Incense.Status.Start"));
            }
            var playing = _sudokuPanel != null && _sudokuPanel.activeSelf && !run.IsLevelComplete
                          && (_gameOverPanel == null || !_gameOverPanel.activeSelf)
                          && (_inGameOptionsPanel == null || !_inGameOptionsPanel.activeSelf);
            if (playing) walk.Tick(Mathf.Min(Time.unscaledDeltaTime, IncenseMaxFrame));
            _hudView?.ShowIncense(walk, run.CurrentLevelConfig, run.CurrentLevelState, _incenseBest, run.State.ClassId);
            if (playing && walk.Over) EndIncenseWalk(run);
        }

        // The run's play time, as the other modes add it at each puzzle's end.
        private void AddPuzzleTime(RunDirector run)
        {
            if (run.State != null) run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
        }

        // A wrong digit's line: its cost on the clock in the walk, the HP left elsewhere.
        private string MistakeNote(RunDirector run, int value) => run.State.Mode == GameMode.IncenseWalk
            ? Tf("Incense.Status.Mistake", value)
            : Tf("{0} is wrong here. HP: {1}", value, run.State.CurrentHP);

        // [REQ: INCENSE-CLOCK-002] [REQ: INCENSE-XP-001] A solved puzzle: its XP, its seconds on the clock, the next puzzle.
        private void HandleIncenseSolved(RunDirector run)
        {
            if (run.Incense.Over)   // [REQ: INCENSE-CLOCK-004] solved after 0:00: nothing counts
            {
                EndIncenseWalk(run);
                return;
            }
            AddPuzzleTime(run);
            var config = run.CurrentLevelConfig;
            var level = run.CurrentLevelState;
            var mistakes = level?.Mistakes ?? 0;
            var usedPencil = level != null && level.PencilMarksUsed > 0;   // the walk's marks are free, but they count
            run.CompleteLevelAndGrantRewards();
            var added = run.Incense.OnPuzzleSolved(config?.BoardSize ?? 6, config?.Stars ?? 1, mistakes, usedPencil);
            _hudView?.FloatIncenseBonus(added);
            SetStatus(Tf("Incense.Status.Solved", "+" + $"{added / 60}:{added % 60:00}"));
            _audio?.PlayPuzzleSolved();
            RumbleService.PulsePuzzleSolved(this);
            _boardView?.FlashBoardSolved();
            run.AdvanceIncenseLevel();
            _selectedRow = _selectedCol = -1;
            _highlightValue = 0;
            ShowSudoku();
            RebuildBoard();
        }

        private void EndIncenseWalk(RunDirector run)
        {
            _incenseBest = -1;
            AddPuzzleTime(run);
            _hudView?.StopPuzzleTimer();
            SetActive(_sudokuPanel, false);
            SetActive(_pathPanel, false);
            CloseRunScenes();
            _audio?.SetContext(RunAudioController.Context.Path);
            _audio?.PlayGameOver();
            _endScreenView.ShowIncenseResult(run);
        }
    }
}
