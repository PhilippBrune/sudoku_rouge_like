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

        /// <summary>
        /// F22: Shows the boss-cleared interstitial panel using bg_boss_cleared.png as the background.
        /// Call from InRunController immediately after a non-final boss puzzle is solved, before
        /// advancing to the next floor. The panel uses the same _gameOverPanel structure so it
        /// auto-hides when the next level loads.
        /// </summary>
        public void ShowBossCleared(RunDirector run, Action onContinue = null)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
            if (_gameOverPanel != null)
            {
                var bgImg = _gameOverPanel.transform.Find("PanelBackground")?.GetComponent<RawImage>();
                if (bgImg != null)
                {
                    var tex = Resources.Load<Texture2D>("background/bg_boss_cleared");
                    bgImg.texture = tex;
                    bgImg.color   = new Color(1f, 1f, 1f, tex != null ? 0.85f : 0f);
                }

                // Add a "Continue" button so the player controls when to advance, not a timer.
                var existingContinue = _gameOverPanel.transform.Find("BossClearedContinueBtn");
                if (existingContinue != null) UnityEngine.Object.Destroy(existingContinue.gameObject);
                if (onContinue != null)
                {
                    var btnGo = new GameObject("BossClearedContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGo.transform.SetParent(_gameOverPanel.transform, false);
                    var rt = btnGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.35f, 0.08f);
                    rt.anchorMax = new Vector2(0.65f, 0.18f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    btnGo.GetComponent<Image>().color = new Color(0.20f, 0.16f, 0.10f, 0.90f);
                    var lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    lbl.transform.SetParent(btnGo.transform, false);
                    var lblRt = lbl.GetComponent<RectTransform>();
                    lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
                    lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
                    var lblTxt = lbl.GetComponent<Text>();
                    lblTxt.text = "Continue →";
                    lblTxt.alignment = TextAnchor.MiddleCenter;
                    lblTxt.fontSize = 16;
                    lblTxt.color = GamePalette.WinGold;
                    var btn = btnGo.GetComponent<Button>();
                    btn.onClick.AddListener(() => onContinue?.Invoke());
                }
            }
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = "Boss Defeated!";
                _gameOverSummary.color = GamePalette.WinGold;
            }
        }

        /// <summary>Hides the boss-cleared interstitial panel — called after the timed pause.</summary>
        public void HideBossCleared()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
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
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                InRunUiFactory.SelectFirstInteractable(_gameOverPanel);
            }

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

            if (victory && result?.Mode == SudokuRoguelike.Core.GameMode.GardenRun && result.XpEarned > 0)
                SudokuRoguelike.Meta.SteamLeaderboardService.SubmitRunScore(result.XpEarned);

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

        public void ShowSpiritTrialsResult(RunDirector run, bool completed)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            var bgImg = _gameOverPanel?.transform.Find("PanelBackground")?.GetComponent<RawImage>();
            if (bgImg != null)
            {
                var tex = Resources.Load<Texture2D>("background/bg_trials_menu");
                bgImg.texture = tex;
                bgImg.color   = new Color(1f, 1f, 1f, tex != null ? 0.85f : 0f);
            }

            var state = run?.State;
            var level = run?.CurrentLevelState;
            if (state == null) return;

            var tier       = state.SpiritTrialsTier;
            var tierName   = tier.ToString();
            var seconds    = state.TotalRunSeconds;
            var modCount   = run?.CurrentLevelConfig?.ActiveModifiers?.Count ?? 0;
            var cells      = level?.CorrectPlacements ?? 0;
            var pencilUsed = level?.PencilMarksUsed   ?? 0;
            var mistakes   = level?.Mistakes          ?? 0;

            var score = completed
                ? SpiritTrialsService.CalculateScore(tier, seconds, modCount, cells, pencilUsed, mistakes)
                : 0;

            if (_gameOverSummary != null)
            {
                _gameOverSummary.text  = completed
                    ? $"Spirit Trials\n{tierName} — Complete!"
                    : $"Spirit Trials\n{tierName} — Failed";
                _gameOverSummary.color = completed ? GamePalette.WinGold : InRunUiFactory.CursedTitleRed;
            }

            // Persist personal best per tier
            var save     = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            if (envelope.Statistics == null) envelope.Statistics = new ProfileStats();
            var stats = envelope.Statistics;

            int prevBest = tier switch
            {
                SpiritTrialsTier.Apprentice  => stats.TrialsBestScore_Apprentice,
                SpiritTrialsTier.Adept       => stats.TrialsBestScore_Adept,
                SpiritTrialsTier.Master      => stats.TrialsBestScore_Master,
                _                            => stats.TrialsBestScore_Grandmaster,
            };

            if (completed && score > prevBest)
            {
                switch (tier)
                {
                    case SpiritTrialsTier.Apprentice:  stats.TrialsBestScore_Apprentice  = score; break;
                    case SpiritTrialsTier.Adept:       stats.TrialsBestScore_Adept       = score; break;
                    case SpiritTrialsTier.Master:      stats.TrialsBestScore_Master      = score; break;
                    default:                           stats.TrialsBestScore_Grandmaster = score; break;
                }
                new ProfileService(save).UpdateStats(envelope, stats);
            }

            if (_gameOverDetails != null)
            {
                var mins = (int)(seconds / 60);
                var secs = (int)(seconds % 60);
                var sb   = new StringBuilder();
                if (completed)
                {
                    sb.AppendLine($"Score:          {score:N0}");
                    sb.AppendLine($"Time:           {mins}:{secs:D2}");
                    sb.AppendLine($"Cells Filled:   {cells}");
                    sb.AppendLine($"Pencil Marks:   {pencilUsed}");
                    sb.AppendLine($"Mistakes:       {mistakes}");
                    if (modCount > 0) sb.AppendLine($"Modifiers:      +{modCount}");
                    sb.AppendLine();
                    if (score > prevBest && prevBest > 0)
                        sb.AppendLine($"New Personal Best!  (prev: {prevBest:N0})");
                    else if (prevBest > 0)
                        sb.AppendLine($"Personal Best:  {prevBest:N0}");
                    else
                        sb.AppendLine("First completion!");
                }
                else
                {
                    sb.AppendLine("The spirit trials await again.");
                    sb.AppendLine();
                    sb.AppendLine($"Cells Filled:   {cells}");
                    sb.AppendLine($"Mistakes:       {mistakes}");
                    if (prevBest > 0) sb.AppendLine($"Best Score:     {prevBest:N0}");
                }
                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        public void ShowEndlessZenResult(RunDirector run)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            var bgImg = _gameOverPanel?.transform.Find("PanelBackground")?.GetComponent<RawImage>();
            if (bgImg != null)
            {
                var tex = Resources.Load<Texture2D>("background/bg_zen_menu");
                bgImg.texture = tex;
                bgImg.color   = new Color(1f, 1f, 1f, tex != null ? 0.85f : 0f);
            }

            var state   = run?.State;
            var level   = run?.CurrentLevelState;
            var depth   = state?.Depth ?? 0;
            var seconds = state?.TotalRunSeconds ?? 0f;
            var mistakes = run?.GetAnalytics()?.TotalMistakes ?? level?.Mistakes ?? 0;

            if (_gameOverSummary != null)
            {
                _gameOverSummary.text  = $"Endless Zen\nDepth {depth}";
                _gameOverSummary.color = GamePalette.WinGold;
            }

            // Persist depth + session count
            var save     = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            if (envelope.Statistics == null) envelope.Statistics = new ProfileStats();
            var stats   = envelope.Statistics;
            var prevBest = stats.HighestEndlessDepth;
            var isNewBest = depth > prevBest;

            if (isNewBest) stats.HighestEndlessDepth = depth;
            stats.TotalZenSessions += 1;
            new ProfileService(save).UpdateStats(envelope, stats);

            if (_gameOverDetails != null)
            {
                var mins = (int)(seconds / 60);
                var secs = (int)(seconds % 60);
                var sb   = new StringBuilder();
                sb.AppendLine($"Depth Reached:  {depth}");
                sb.AppendLine($"Session Time:   {mins}:{secs:D2}");
                sb.AppendLine($"Mistakes:       {mistakes}");
                sb.AppendLine();
                if (isNewBest && prevBest > 0)
                    sb.AppendLine($"New Personal Best!  (prev: {prevBest})");
                else if (prevBest > 0)
                    sb.AppendLine($"Personal Best:  {prevBest}");
                else
                    sb.AppendLine("First Endless Zen session!");
                sb.AppendLine();
                sb.AppendLine($"Total Sessions: {stats.TotalZenSessions}");
                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        public struct BossClearedStats
        {
            public int Mistakes;
            public int HpLost;
            public int ItemsUsed;
            public int PencilMarks;
        }

        public static BossClearedStats BuildBossClearedStats(RunDirector run)
        {
            var level = run?.CurrentLevelState;
            return new BossClearedStats
            {
                Mistakes    = level?.Mistakes             ?? 0,
                HpLost      = level?.HpLost               ?? 0,
                ItemsUsed   = level?.ItemsUsedThisLevel   ?? 0,
                PencilMarks = level?.PencilMarksUsed      ?? 0,
            };
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
            if (result.DisableProgressionRewards || result.Mode != GameMode.GardenRun) return null;

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
            var newUnlocks = profile.RecordRunAndGetNewUnlocks(result, run?.State);
            // ActiveRunState is cleared inside RecordRunAndGetNewUnlocks to avoid a race condition
            // where a second async save would overwrite the meta progress from the first save.

            return newUnlocks;
        }
    }
}
