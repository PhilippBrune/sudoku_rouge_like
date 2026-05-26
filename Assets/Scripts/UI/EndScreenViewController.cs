using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Classes;
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
        public void ShowBossCleared(RunDirector run, Action onContinue = null, string continueLabel = null)
        {
            ClearBossClearedDynamicContent();
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

                // 9.1 — hide the Back button; it belongs to game-over context only
                if (_gameOverBackBtn != null)
                    _gameOverBackBtn.gameObject.SetActive(false);

                // 9.2 — hide the details info block (class/floor/XP), belongs to game-over context only
                SetGameOverDetailsScrimActive(false);
                if (_gameOverDetails != null)
                {
                    _gameOverDetails.text = string.Empty;
                    _gameOverDetails.gameObject.SetActive(false);
                }

                // Add a "Continue" button so the player controls when to advance, not a timer.
                var existingContinue = _gameOverPanel.transform.Find("BossClearedContinueBtn");
                if (existingContinue != null) UnityEngine.Object.Destroy(existingContinue.gameObject);

                // 9.3 — per-floor stats panel
                var existingStats = _gameOverPanel.transform.Find("BossClearedStats");
                if (existingStats != null) UnityEngine.Object.Destroy(existingStats.gameObject);
                if (run != null)
                {
                    var statsStr = BuildBossClearedStatsText(run);
                    var statsGo = new GameObject("BossClearedStats", typeof(RectTransform), typeof(Text));
                    statsGo.transform.SetParent(_gameOverPanel.transform, false);
                    var statsRt = statsGo.GetComponent<RectTransform>();
                    statsRt.anchorMin = new Vector2(0.30f, 0.36f);
                    statsRt.anchorMax = new Vector2(0.70f, 0.58f);
                    statsRt.offsetMin = statsRt.offsetMax = Vector2.zero;
                    var statsTxt = statsGo.GetComponent<Text>();
                    statsTxt.text = statsStr;
                    statsTxt.alignment = TextAnchor.MiddleCenter;
                    statsTxt.fontSize = 22;
                    statsTxt.color = GamePalette.WinGold;
                    statsTxt.font = FontAssetService.GetFont();
                    var statsShadow = statsGo.AddComponent<Shadow>();
                    statsShadow.effectColor = new Color(0f, 0f, 0f, 0.90f);
                    statsShadow.effectDistance = new Vector2(2f, -2f);
                }

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
                    lblTxt.text = continueLabel ?? T("InRun.End.BossCleared.Continue");
                    lblTxt.alignment = TextAnchor.MiddleCenter;
                    lblTxt.fontSize = 16;
                    lblTxt.color = GamePalette.WinGold;
                    lblTxt.font = FontAssetService.GetFont();
                    var btn = btnGo.GetComponent<Button>();
                    btn.onClick.AddListener(() => onContinue?.Invoke());
                }
            }
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = T("InRun.End.BossCleared.Title");
                _gameOverSummary.color = GamePalette.WinGold;
            }
        }

        public readonly struct BossClearedStatsSnapshot
        {
            public BossClearedStatsSnapshot(int mistakes, int hpLost, int itemsUsed, int pencilMarks)
            {
                Mistakes = mistakes;
                HpLost = hpLost;
                ItemsUsed = itemsUsed;
                PencilMarks = pencilMarks;
            }

            public int Mistakes { get; }
            public int HpLost { get; }
            public int ItemsUsed { get; }
            public int PencilMarks { get; }
        }

        public static BossClearedStatsSnapshot BuildBossClearedStats(RunDirector run)
        {
            var level = run?.CurrentLevelState;
            return new BossClearedStatsSnapshot(
                level?.Mistakes ?? 0,
                level?.HpLost ?? 0,
                level?.ItemsUsedThisLevel ?? 0,
                level?.PencilMarksUsed ?? 0);
        }

        public static string BuildBossClearedStatsText(RunDirector run)
        {
            var stats = BuildBossClearedStats(run);
            return F(
                "InRun.End.BossCleared.Stats",
                stats.Mistakes,
                stats.HpLost,
                stats.ItemsUsed,
                stats.PencilMarks);
        }

        /// <summary>Hides the boss-cleared interstitial panel. Only call from a player-triggered action.</summary>
        public void HideBossCleared()
        {
            ClearBossClearedDynamicContent();
            // Restore elements hidden during ShowBossCleared before deactivating the panel
            if (_gameOverBackBtn != null) _gameOverBackBtn.gameObject.SetActive(true);
            if (_gameOverDetails != null) _gameOverDetails.gameObject.SetActive(true);
            SetGameOverDetailsScrimActive(true);
            if (_gameOverPanel != null && _gameOverPanel.activeSelf)
                _gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(RunDirector run, bool victory)
        {
            // Seasonal challenge: show score result instead of normal end screen
            if (run?.State?.IsSeasonalChallenge == true)
            {
                ShowSeasonalResult(run, victory);
                return;
            }

            ClearBossClearedDynamicContent();

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

            // Restore elements that may have been hidden by ShowBossCleared
            if (_gameOverBackBtn != null) _gameOverBackBtn.gameObject.SetActive(true);
            if (_gameOverDetails != null) _gameOverDetails.gameObject.SetActive(true);
            SetGameOverDetailsScrimActive(true);
            ConfigureOutcomeButton(victory);

            // Dynamic title
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = victory ? T("InRun.End.Title.Victory") : T("InRun.End.Title.Defeat");
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
                var className = ClassLabel(classId);
                sb.AppendLine(ParkNarrativeService.GetRunEndFlavor(victory, classId));
                sb.AppendLine();
                sb.AppendLine(F("InRun.End.Detail.ClassFloor", className, run.State.Depth));
                sb.AppendLine(F("InRun.End.Detail.HpGold", run.State.CurrentHP, run.State.MaxHP, run.State.CurrentGold));

                // XP earned this run
                var xpEarned = result?.XpEarned ?? 0;
                sb.AppendLine(F("InRun.End.Detail.XpEarned", xpEarned));
                sb.AppendLine();

                // Level-up notification
                if (preLevel != postLevel)
                    sb.AppendLine(F("InRun.End.Detail.LevelUp", preLevel, postLevel));

                // XP progress bar toward next level
                if (postLevel < 40)
                {
                    var barLen = 16;
                    var filled = postXpToNext > 0
                        ? Math.Min(barLen, (int)(postXpInto * barLen / (float)postXpToNext))
                        : 0;
                    var bar = new string('=', filled) + new string('-', barLen - filled);
                    sb.AppendLine(F("InRun.End.Detail.ClassLevel", className, postLevel));
                    sb.AppendLine(F("InRun.End.Detail.XpBar", bar, postXpInto, postXpToNext));
                    sb.AppendLine(F("InRun.End.Detail.ToLevel", postLevel + 1));
                }
                else
                {
                    sb.AppendLine(F("InRun.End.Detail.LevelMax", className));
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
                    sb.AppendLine(T("InRun.End.Detail.NewUnlockHeader"));
                    foreach (var cid in newUnlocks)
                        sb.AppendLine(F("InRun.End.Detail.ClassUnlocked", ClassLabel(cid)));
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
                _gameOverSummary.text = F("InRun.End.Seasonal.Title", theme);
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

            // Award Ink Stamps: +2 for S grade, +1 for first-ever monthly completion (regardless of grade).
            // [REQ: SEASONAL-STAMP-001]
            if (completed)
            {
                if (envelope.DailyGoals == null) envelope.DailyGoals = new DailyGoalState();
                var isFirstCompletion = prevBest == 0;
                if (isFirstCompletion)
                    envelope.DailyGoals.TotalInkStamps += 1;
                if (grade == "S")
                    envelope.DailyGoals.TotalInkStamps += 2;
            }

            envelope.ActiveSeasonalRunState = null;
            envelope.ActiveSeasonalPuzzle = null;
            save.Save(envelope);

            if (_gameOverDetails != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine(F("InRun.End.Seasonal.Grade", grade));
                sb.AppendLine(F("InRun.End.Seasonal.Score", score));
                sb.AppendLine();
                sb.AppendLine(F("InRun.End.Seasonal.CellsCorrect", cellsCorrect));
                sb.AppendLine(F("InRun.End.Seasonal.Mistakes", mistakes));
                sb.AppendLine(F("InRun.End.Seasonal.PencilLeft", pencilLeft, state.MaxPencil));
                sb.AppendLine();
                if (completed && score > prevBest && prevBest > 0)
                    sb.AppendLine(F("InRun.End.Seasonal.NewPersonalBest", prevBest));
                else if (prevBest > 0)
                    sb.AppendLine(F("InRun.End.Seasonal.PersonalBest", prevBest));
                else
                    sb.AppendLine(T("InRun.End.Seasonal.FirstAttempt"));
                if (completed && prevBest == 0)
                    sb.AppendLine(T("InRun.End.Seasonal.InkStampFirst"));
                if (completed && grade == "S")
                    sb.AppendLine(T("InRun.End.Seasonal.InkStampS"));
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
            var tierName   = SpiritTierLabel(tier);
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
                    ? F("InRun.End.Trials.TitleComplete", tierName)
                    : F("InRun.End.Trials.TitleFailed", tierName);
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
                    sb.AppendLine(F("InRun.End.Trials.Score", score));
                    sb.AppendLine(F("InRun.End.Trials.Time", mins, secs));
                    sb.AppendLine(F("InRun.End.Trials.CellsFilled", cells));
                    sb.AppendLine(F("InRun.End.Trials.PencilMarks", pencilUsed));
                    sb.AppendLine(F("InRun.End.Trials.Mistakes", mistakes));
                    if (modCount > 0) sb.AppendLine(F("InRun.End.Trials.Modifiers", modCount));
                    sb.AppendLine();
                    if (score > prevBest && prevBest > 0)
                        sb.AppendLine(F("InRun.End.Trials.NewPersonalBest", prevBest));
                    else if (prevBest > 0)
                        sb.AppendLine(F("InRun.End.Trials.PersonalBest", prevBest));
                    else
                        sb.AppendLine(T("InRun.End.Trials.FirstCompletion"));
                }
                else
                {
                    sb.AppendLine(T("InRun.End.Trials.AwaitAgain"));
                    sb.AppendLine();
                    sb.AppendLine(F("InRun.End.Trials.CellsFilled", cells));
                    sb.AppendLine(F("InRun.End.Trials.Mistakes", mistakes));
                    if (prevBest > 0) sb.AppendLine(F("InRun.End.Trials.BestScore", prevBest));
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
                _gameOverSummary.text  = F("InRun.End.Zen.Title", depth);
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
                sb.AppendLine(F("InRun.End.Zen.DepthReached", depth));
                sb.AppendLine(F("InRun.End.Zen.SessionTime", mins, secs));
                sb.AppendLine(F("InRun.End.Zen.Mistakes", mistakes));
                sb.AppendLine();
                if (isNewBest && prevBest > 0)
                    sb.AppendLine(F("InRun.End.Zen.NewPersonalBest", prevBest));
                else if (prevBest > 0)
                    sb.AppendLine(F("InRun.End.Zen.PersonalBest", prevBest));
                else
                    sb.AppendLine(T("InRun.End.Zen.FirstSession"));
                sb.AppendLine();
                sb.AppendLine(F("InRun.End.Zen.TotalSessions", stats.TotalZenSessions));
                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        private static string T(string key) => LocalizationService.T(key);

        private void SetGameOverDetailsScrimActive(bool active)
        {
            var scrim = _gameOverPanel != null ? _gameOverPanel.transform.Find("GameOverDetailsScrim") : null;
            if (scrim != null) scrim.gameObject.SetActive(active);
        }

        private void ClearBossClearedDynamicContent()
        {
            if (_gameOverPanel == null) return;
            RemoveDynamicChild("BossClearedContinueBtn");
            RemoveDynamicChild("BossClearedStats");
        }

        private void RemoveDynamicChild(string name)
        {
            var child = _gameOverPanel.transform.Find(name);
            if (child == null) return;
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }

        private void ConfigureOutcomeButton(bool victory)
        {
            if (_gameOverBackBtn == null) return;
            InRunUiFactory.ApplyActionIcon(_gameOverBackBtn, victory ? UiAction.Confirm : UiAction.Back, compact: true);
            var label = _gameOverBackBtn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
                label.text = victory ? T("InRun.End.ClaimReward") : T("Back");
        }

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);

        private static string ClassLabel(ClassId classId)
        {
            var def = ClassCatalog.GetDefinition(classId);
            return def != null ? LocalizationService.T(def.Name, def.Name) : classId.ToString();
        }

        private static string SpiritTierLabel(SpiritTrialsTier tier) =>
            LocalizationService.T($"SpiritTrials.Tier.{tier}", tier.ToString());

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
