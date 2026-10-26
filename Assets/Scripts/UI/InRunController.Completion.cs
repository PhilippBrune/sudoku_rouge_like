using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Puzzle completion, game over, rewards, boss-cleared interstitial and floor advance
    /// (GAP-002). Split out of InRunController.cs per docs/controller-extraction-plan.md.
    /// </summary>
    public sealed partial class InRunController
    {
        // ─────────────────────────── Completion / Game Over ───────────────────────────

        private void HandleCompletion()
        {
            var run = _map?.Run;
            if (run == null || !run.IsLevelComplete) { _completionHandled = false; return; }
            if (_completionHandled) return;
            if (run.State == null) { _completionHandled = false; return; }
            _completionHandled = true;

            if (run.State.TutorialMode)
            {
                run.CompleteLevelAndGrantRewards();
                if (run.State.RunNumber == 0)
                {
                    // Basics tutorial: play stinger and return after a short pause
                    _audio?.SetContext(RunAudioController.Context.Path);
                    _audio?.PlayPuzzleSolved();
                    RumbleService.PulsePuzzleSolved(this);
                    _boardView?.FlashBoardSolved();
                    SetStatus(T("Puzzle solved! Well done!"));
                    StartCoroutine(ReturnToMenuAfterDelay(2.5f));
                }
                else
                {
                    SetStatus(T("Tutorial solved!"));
                    SaveAndQuit();
                }
                return;
            }

            if (run.State.IsSeasonalChallenge)
            {
                // Accumulate this puzzle's elapsed time into the run total before stopping the timer
                if (run.State != null)
                    run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
                _hudView?.StopPuzzleTimer();

                SetActive(_sudokuPanel, false);
                SetActive(_pathPanel, false);
                CloseRunScenes();
                _audio?.SetContext(RunAudioController.Context.Path);
                _audio?.PlayPuzzleSolved();
                RumbleService.PulsePuzzleSolved(this);
                _boardView?.FlashBoardSolved();
                _endScreenView.ShowGameOver(run, true);
                return;
            }

            if (run.State.Mode == GameMode.EndlessZen)
            {
                run.CompleteLevelAndGrantRewards();
                if (run.State != null)
                    run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
                _hudView?.StopPuzzleTimer();

                run.AdvanceEndlessZenLevel();
                SetStatus(Tf("Endless Zen  ·  Depth {0}", run.State.Depth)); // GAP-019: the key was double-encoded
                _audio?.PlayPuzzleSolved();
                RumbleService.PulsePuzzleSolved(this);
                _boardView?.FlashBoardSolved();
                _selectedRow = _selectedCol = -1;
                _highlightValue = 0;
                ShowSudoku();
                RebuildBoard();
                return;
            }

            if (run.State.Mode == GameMode.SpiritTrials)
            {
                if (run.State != null)
                    run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
                _hudView?.StopPuzzleTimer();

                SetActive(_sudokuPanel, false);
                SetActive(_pathPanel, false);
                CloseRunScenes();
                _audio?.SetContext(RunAudioController.Context.Path);
                _audio?.PlayPuzzleSolved();
                RumbleService.PulsePuzzleSolved(this);
                _boardView?.FlashBoardSolved();
                _endScreenView.ShowSpiritTrialsResult(run, true);
                return;
            }

            // _justCompletedNodeIndex is set in OnPostRewardReady after reward screen dismissed
            var completingNode = _map?.Run?.GetCurrentNode();
            var completingIsBoss = completingNode?.Type == NodeType.Boss;
            _justCompletedNodeIndex = -1;
            if (!completingIsBoss) ShowPath();
            _audio?.PlayPuzzleSolved();
            RumbleService.PulsePuzzleSolved(this);
            _boardView?.FlashBoardSolved();
            StartCoroutine(DelayedShowReward());
        }

        private System.Collections.IEnumerator DelayedShowReward()
        {
            // Store completed node index now so OnPostRewardReady can fire the gold ring after dismissal
            _justCompletedNodeIndex = _map?.Run?.State?.CurrentNodeIndex ?? -1;
            yield return new WaitForSecondsRealtime(0.35f);

            var node = _map?.Run?.GetCurrentNode();
            var isBoss = node?.Type == NodeType.Boss;
            var runState = _map?.Run?.State;
            var isFinalBoss = isBoss && runState != null && runState.CurrentFloor >= runState.TotalFloors - 1;

            // Boss nodes do not show normal item rewards, but they must still grant
            // completion rewards and record boss defeat/statistics before end-screen persistence.
            if (isBoss)
                _map.TryClaimCurrentPuzzleRewards(out _, out _, buildItemSlots: false);

            if (isFinalBoss)
            {
                // Final boss: skip reward — OnPostRewardReady handles victory sequence directly
                OnPostRewardReady();
                yield break;
            }

            if (isBoss)
            {
                // [REQ: REQ-BOSS-107] [REQ: UI-STYLE-005] Non-final boss: the offering table shows the fight's numbers
                // and the relic choice on one screen; taking or leaving the offerings loads the next floor. The solved
                // board is hidden first, so its keys and controller shortcuts are off while the screen is open.
                // Normal item rewards are never shown after a boss.
                var choices = _map?.Run?.RollBossOfferings();   // [REQ: RELIC-KEY-001] keystones after the first boss
                SetActive(_sudokuPanel, false);
                _endScreenView.ShowBossCleared(_map.Run, choices,
                    relic =>
                    {
                        _rewardView.AcceptRelic(relic);
                        OnPostRewardReady();
                    },
                    () =>
                    {
                        if (choices != null && choices.Count > 0) _rewardView.LeaveRelics();
                        OnPostRewardReady();
                    });
                yield break;
            }

            _rewardView.ShowRewardScreen();
        }

        private void CheckGameOver()
        {
            if (_gameOverPanel != null && _gameOverPanel.activeSelf) return;
            var run = _map?.Run;
            if (run == null || !run.IsPlayerDead) return;
            ShowDefeatScreen(run);
        }

        // Defeat end screen: records the run (XP, unlocks) and clears the save. Also used by Abandon Run (GAP-020).
        private void ShowDefeatScreen(RunDirector run)
        {
            // Accumulate this puzzle's elapsed time into the run total before stopping the timer
            if (run.State != null)
                run.State.TotalRunSeconds += _hudView?.GetPuzzleElapsed() ?? 0f;
            _hudView?.StopPuzzleTimer();

            SetActive(_sudokuPanel, false);
            SetActive(_pathPanel, false);
            CloseRunScenes();
            _audio?.SetContext(RunAudioController.Context.Path); // stop puzzle loop before stinger
            _audio?.PlayGameOver();
            _audio?.PlayRunEndMusic(false, 1.5f); // AUDIO-MUSIC-006: defeat track after the stinger
            if (run.State?.Mode == GameMode.SpiritTrials)
                _endScreenView.ShowSpiritTrialsResult(run, false);
            else if (run.State?.Mode == GameMode.EndlessZen)
                _endScreenView.ShowEndlessZenResult(run);
            else
                _endScreenView.ShowGameOver(run, false);
        }

        // The reward, relic shrine and shop are scenes beside the path panel (UI-STYLE-007/008/009); hiding the path
        // does not hide them, so a run that ends closes them.
        private void CloseRunScenes()
        {
            _rewardView?.CloseScenes();
            _shopView?.CloseScenes();
        }

        // ─────────────────────────── Post-reward callback ───────────────────────────

        private void OnPostRewardReady()
        {
            var node = _map?.Run?.GetCurrentNode();
            if (node != null && node.Type == NodeType.Boss)
            {
                var s = _map.Run.State;
                if (s.CurrentFloor >= s.TotalFloors - 1)
                {
                    SetActive(_sudokuPanel, false);
                    SetActive(_pathPanel, false);
                    CloseRunScenes();
                    // Fire run-won hooks
                    RelicService.OnRunVictory(s);
                    var run = _map.Run;
                    run.GetAnalytics()?.RecordRunComplete();
                    if (run.DailyGoals != null && !s.IsSeasonalChallenge)
                        DailyGoalService.EvaluateInRun(run.DailyGoals, s, run.CurrentLevelState, DailyGoalService.TriggerRunWon);
                    _audio?.SetContext(RunAudioController.Context.Path); // stop puzzle loop before stinger
                    _audio?.PlayRunVictoryFanfare();
                    _audio?.PlayRunEndMusic(true, 4.5f); // AUDIO-MUSIC-006: victory track after the fanfare
                    RumbleService.PulseVictory(this);
                    _boardView?.TriggerDigitRain();
                    StartCoroutine(VictoryScreenFlash());
                    _endScreenView.ShowGameOver(run, true);
                    return;
                }
                // Boss-cleared screen was already shown before relic choice; advance the floor now.
                StartCoroutine(AdvanceFloorAfterBossCleared());
                return;
            }
            else
            {
                _completedRingArmed = true; // arm so RebuildPathCanvas fires the gold ring
            }
            ShowPath();
            _completedRingArmed = false;
        }

        /// <summary>
        /// F11: Hides the boss-cleared interstitial and advances to the next floor.
        /// Called by the "Continue" button — no auto-timer.
        /// </summary>
        private System.Collections.IEnumerator AdvanceFloorAfterBossCleared()
        {
            yield return null; // one frame to let the button click settle
            _endScreenView?.HideBossCleared();
            _map.AdvanceToNextFloor();
            var newFloor = _map.Run.State.CurrentFloor;
            _audio?.SetFloorIndex(newFloor); // A1 — prime new floor BGM before switching to path context
            var floorFlavor = SudokuRoguelike.Run.ParkNarrativeService.GetFloorEntryFlavor(newFloor);
            _pathMessage = floorFlavor;
            StartCoroutine(ClearPathMessageAfterDelay(5f));
            SetStatus(T("Garden cleared! Next floor..."));
            _justCompletedNodeIndex = -1; // floor advance: no ring (node is gone)
            ShowPath();
            _completedRingArmed = false;
        }
    }
}
