using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the game-over / victory screen.
    /// </summary>
    public sealed class EndScreenViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _gameOverPanel;
        private Text _gameOverSummary;
        private Text _gameOverDetails;
        private Button _gameOverBackBtn;
        private bool _gameOverShown;

        public Action OnReturnToMenuPressed;

        public bool IsShown => _gameOverShown;
        public void ResetShown() => _gameOverShown = false;

        public void Configure(RunMapController map, GameObject gameOverPanel,
            Text goSummary, Text goDetails, Button goBack)
        {
            _map = map;
            _gameOverPanel = gameOverPanel;
            _gameOverSummary = goSummary;
            _gameOverDetails = goDetails;
            _gameOverBackBtn = goBack;

            if (_gameOverBackBtn != null)
            {
                _gameOverBackBtn.onClick.RemoveAllListeners();
                _gameOverBackBtn.onClick.AddListener(() => OnReturnToMenuPressed?.Invoke());
            }
        }

        public void CheckAndShow(RunDirector run)
        {
            if (_gameOverShown) return;
            if (run == null || !run.IsPlayerDead) return;
            _gameOverShown = true;
            ShowGameOver(run, false);
        }

        public void ShowGameOver(RunDirector run, bool victory)
        {
            // Seasonal challenge: show score result instead of normal end screen
            if (run?.State?.IsSeasonalChallenge == true)
            {
                ShowSeasonalResult(run, victory);
                return;
            }

            // Panels hidden by InRunController before calling ShowGameOver
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            // Load the correct background art (placeholder added in BuildGameOverPanel)
            if (_gameOverPanel != null)
            {
                var bgImg = _gameOverPanel.transform.Find("PanelBackground")?.GetComponent<RawImage>();
                if (bgImg != null)
                {
                    var tex = Resources.Load<Texture2D>(victory ? "background/bg_victory" : "background/bg_defeat");
                    bgImg.texture = tex;
                    bgImg.color   = new Color(1f, 1f, 1f, tex != null ? 0.85f : 0f);
                }
            }

            // Dynamic title
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = victory ? "Victory!" : "Defeat";
                _gameOverSummary.color = victory
                    ? GamePalette.WinGold
                    : InRunUiFactory.CursedTitleRed;
            }

            // Snapshot XP before persisting so we can show level-up
            var classId = run.State.ClassId;
            var snapshotSave = new SaveFileService(SaveProfileService.ActiveSlot);
            var preXp = snapshotSave.HasSaveFile()
                ? GetClassTotalXpFromSave(snapshotSave.Load(), classId) : 0;
            var preLevel = XpTable.DeriveLevel(preXp);

            var result = _map.BuildRunResult(victory, 0, 0);
            var newUnlocks = PersistResult(result, run);

            // Load updated XP after persist; fallback to in-memory calculation
            var postSave = new SaveFileService(SaveProfileService.ActiveSlot);
            var postXp = postSave.HasSaveFile()
                ? GetClassTotalXpFromSave(postSave.Load(), classId) : preXp + (result?.XpEarned ?? 0);
            var postLevel = XpTable.DeriveLevel(postXp);
            var postXpInto = postXp - XpTable.CumulativeXpForLevel(postLevel);
            var postXpToNext = postLevel < 40 ? XpTable.XpToNextLevel(postLevel) : 0;

            if (_gameOverDetails != null)
            {
                var sb = new StringBuilder();

                // Run summary
                sb.AppendLine($"Class: {classId}   Floor: {run.State.Depth}");
                sb.AppendLine($"HP: {run.State.CurrentHP}/{run.State.MaxHP}   Gold: {run.State.CurrentGold}");

                // XP earned this run
                var xpEarned = result?.XpEarned ?? 0;
                sb.AppendLine($"XP Earned This Run: +{xpEarned}");
                sb.AppendLine();

                // Level-up notification
                if (preLevel != postLevel)
                    sb.AppendLine($"  Level Up!  {preLevel}  \u2192  {postLevel}");

                // XP progress bar toward next level
                if (postLevel < 40)
                {
                    var barLen = 16;
                    var filled = postXpToNext > 0
                        ? Math.Min(barLen, (int)(postXpInto * barLen / (float)postXpToNext))
                        : 0;
                    var bar = new string('=', filled) + new string('-', barLen - filled);
                    sb.AppendLine($"{classId}  Lv {postLevel}");
                    sb.AppendLine($"[{bar}]  {postXpInto} / {postXpToNext} XP");
                    sb.AppendLine($"(to Level {postLevel + 1})");
                }
                else
                {
                    sb.AppendLine($"{classId}  Level MAX  (Lv 40)");
                }

                // Run score
                if (result != null && result.RunScore > 0)
                {
                    sb.AppendLine();
                    var presenter = new EndScreenPresenter();
                    sb.AppendLine(presenter.BuildRunScoreBreakdown(result));
                }

                // New class unlocks
                if (newUnlocks != null && newUnlocks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("NEW UNLOCK\u00a0—");
                    foreach (var cid in newUnlocks)
                        sb.AppendLine($"  \u2605 {cid} is now available!");
                }

                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        private void ShowSeasonalResult(RunDirector run, bool completed)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            var now = DateTime.Today;
            var level = run.CurrentLevelState;
            var state = run.State;

            var cellsCorrect = level?.CorrectPlacements ?? 0;
            var mistakes = level?.Mistakes ?? 0;
            var pencilLeft = state.CurrentPencil;
            var score = SeasonalChallengeService.CalculateScore(cellsCorrect, mistakes, pencilLeft, state.MaxPencil);
            var grade = SeasonalChallengeService.GetGrade(score, completed);
            var theme = SeasonalChallengeService.GetThemeName(now.Year, now.Month);

            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = $"Monthly Walk\n{theme}";
                _gameOverSummary.color = GamePalette.WinGold;
            }

            // Persist personal best
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            if (envelope.SeasonalChallenge == null) envelope.SeasonalChallenge = new SeasonalChallengeState();
            var prevBest = envelope.SeasonalChallenge.GetBest(now.Year, now.Month);

            var hpRemaining = state.CurrentHP;
            var pencilUsed = Math.Max(0, state.MaxPencil - state.CurrentPencil);
            var timeSeconds = Mathf.RoundToInt(state.TotalRunSeconds);

            if (completed)
                envelope.SeasonalChallenge.SetBest(now.Year, now.Month, score, hpRemaining, pencilUsed, timeSeconds);

            envelope.ActiveSeasonalRunState = null;
            envelope.ActiveSeasonalPuzzle = null;
            save.Save(envelope);

            if (_gameOverDetails != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Grade:  {grade}");
                sb.AppendLine($"Score:  {score:N0}");
                sb.AppendLine();
                sb.AppendLine($"Cells Correct:  {cellsCorrect}");
                sb.AppendLine($"Mistakes:       {mistakes}");
                sb.AppendLine($"Pencil Left:    {pencilLeft} / {state.MaxPencil}");
                sb.AppendLine();
                if (completed && score > prevBest && prevBest > 0)
                    sb.AppendLine($"New Personal Best!  (prev: {prevBest:N0})");
                else if (prevBest > 0)
                    sb.AppendLine($"Personal Best:  {prevBest:N0}");
                else
                    sb.AppendLine("First attempt this month!");
                sb.AppendLine();
                sb.AppendLine(SeasonalChallengeService.GetCountdownLabel(DateTime.Today));
                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        private static int GetClassTotalXpFromSave(SaveFileEnvelope envelope, ClassId classId)
        {
            var entries = envelope?.MetaProgress?.GardenProgression?.ClassEntries;
            if (entries == null) return 0;
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].ClassId == classId) return entries[i].TotalXp;
            return 0;
        }

        private List<ClassId> PersistResult(RunResult result, RunDirector run)
        {
            if (result == null || result.TutorialMode) return null;
            if (run?.State?.IsSeasonalChallenge == true) return null; // seasonal: no progression

            if (run != null)
            {
                var analytics = run.GetAnalytics();
                result.ItemsUsedThisRun = analytics?.TotalItemsUsed
                    ?? run.CurrentLevelState?.ItemsUsedThisLevel ?? 0;
                result.AcquiredRelic = run.State.HasRelic;
                if (analytics != null)
                    result.ClearedStageNoPencilNoHpLoss = analytics.TotalMistakes == 0 && !analytics.PencilEverUsed;
            }

            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var profile = new ProfileService(save);
            var newUnlocks = profile.RecordRunAndGetNewUnlocks(result);
            // ActiveRunState is cleared inside RecordRunAndGetNewUnlocks to avoid a race condition
            // where a second async save would overwrite the meta progress from the first save.

            return newUnlocks;
        }
    }
}
