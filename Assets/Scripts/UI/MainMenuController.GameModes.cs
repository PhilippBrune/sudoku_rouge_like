using System;
using System.Collections.Generic;
using UnityEngine;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-025] Game Modes · Five paths: each curtain's state strip and each slip's text (GameModesScreen).
    // Harmony is set here and on Select Class (owner 2026-10-21); a change on either screen refreshes both.
    public sealed partial class MainMenuController
    {
        private const string AccentColor = "#9E2A2B";
        private const int EndlessZenRunStarts = 10;   // ZENMODE-UNLOCK-001 (ProfileService)
        private GameModesScreen _gameModesScreen;

        public void SetGameModesScreen(GameModesScreen screen) => _gameModesScreen = screen;

        private void RefreshGameModesPanel()
        {
            var meta = Profile.LoadMetaProgress();
            _gameModesController?.LoadFromMeta(meta);
            if (_gameModesScreen == null) return;
            _gameModesScreen.Show(GameModesScreen.Mode.Garden);

            var stats = Profile.LoadStats() ?? new ProfileStats();
            var envelope = _save.HasSaveFile() ? _save.Load() : new SaveFileEnvelope();
            var endlessUnlocked = _debugEnableAllFeatures || (meta?.EndlessZenUnlocked ?? false);
            var trialsUnlocked = _debugEnableAllFeatures || (meta?.SpiritTrialsUnlocked ?? false);
            _gameModesScreen.Refresh(new Dictionary<GameModesScreen.Mode, GameModesScreen.ModeView>
            {
                [GameModesScreen.Mode.Endless] = EndlessView(endlessUnlocked, stats),
                [GameModesScreen.Mode.Trials] = TrialsView(trialsUnlocked, stats, out var tierLabels),
                [GameModesScreen.Mode.Daily] = DailyView(envelope.DailyGoals ?? new DailyGoalState()),
                [GameModesScreen.Mode.Monthly] = MonthlyView(envelope.SeasonalChallenge)
            });
            _gameModesScreen.ShowTiers(tierLabels, trialsUnlocked);
            var records = _gameModesPanel != null ? _gameModesPanel.transform.Find("BtnZenRecords") : null;
            if (records != null) records.GetComponent<UnityEngine.UI.Button>().interactable = endlessUnlocked;
            RefreshGameModesHarmony(meta);
        }

        // The Garden Run slip and curtain: Harmony name, its rule, the next level's condition and XP, the perk.
        private void RefreshGameModesHarmony(MetaProgressionState meta)
        {
            if (_gameModesScreen == null || _gameModesController == null) return;
            var level = _gameModesController.SelectedHarmonyLevel;
            var maxUnlocked = _gameModesController.GetMaxUnlockedHarmonyLevel(meta);
            var perk = _gameModesController.SelectedHarmonyPerk;
            var rule = LocalizationService.T("Harmony.Description." + level, HarmonyDifficultyService.GetDescription(level));
            var next = level == maxUnlocked && maxUnlocked < 10
                ? LocalizationService.Format("GameModes.Harmony.Next", "Win at {0} to open {1}.", HarmonyName(level), HarmonyName(level + 1))
                : LocalizationService.Format("GameModes.Harmony.Open", "Open up to {0}.", HarmonyName(maxUnlocked));
            var xp = LocalizationService.Format("GameModes.Harmony.Xp", "XP ×{0:0.0}", HarmonyDifficultyService.GetXpMultiplier(level));
            var perkUnlocked = level >= 4;
            _gameModesScreen.ShowHarmony(new GameModesScreen.HarmonyView(
                level,
                maxUnlocked,
                HarmonyName(level),
                $"{rule}\n{next}  {xp}",
                LocalizationService.Format("GameModes.State.Garden", "Harmony {0} of {1}", Accent(GameModesScreen.Numeral(level)),
                    GameModesScreen.Numeral(maxUnlocked)),
                perkUnlocked ? HarmonyPerkName(perk) : LocalizationService.T("ClassSelect.PerkLocked", "from Harmony IV"),
                perkUnlocked
                    ? HarmonyPerkDescription(perk)
                    : LocalizationService.T("MainMenu.Perks.UnlockHint", "Perks unlock at H4, H6, H8, and H10."),
                perkUnlocked));
        }

        private static GameModesScreen.ModeView EndlessView(bool unlocked, ProfileStats stats)
        {
            if (!unlocked)
                return new GameModesScreen.ModeView(false,
                    LocalizationService.Format("GameModes.Locked.Endless", "Runs started {0} / 10",
                        Math.Min(stats.TotalRunsStarted, EndlessZenRunStarts)),
                    LocalizationService.T("MainMenu.Status.EndlessZenLocked", "Endless Zen unlocks after 10 run starts."));
            return new GameModesScreen.ModeView(true,
                LocalizationService.Format("GameModes.State.Endless", "Best depth {0}", Accent(stats.HighestEndlessDepth.ToString())),
                LocalizationService.T("GameModes.Body.Endless",
                    "One 9×9 after another until your HP runs out. Each solved puzzle takes you one step deeper.")
                + "\n" + LocalizationService.Format("GameModes.Body.EndlessBest", "Best depth {0} · Zen sessions {1}",
                    stats.HighestEndlessDepth, stats.TotalZenSessions));
        }

        private static GameModesScreen.ModeView TrialsView(bool unlocked, ProfileStats stats, out List<string> tierLabels)
        {
            var tiers = new (string Key, string Name, int Best)[]
            {
                ("Apprentice", "Apprentice", stats.TrialsBestScore_Apprentice),
                ("Adept", "Adept", stats.TrialsBestScore_Adept),
                ("Master", "Master", stats.TrialsBestScore_Master),
                ("Grandmaster", "Grandmaster", stats.TrialsBestScore_Grandmaster)
            };
            var best = 0;
            tierLabels = new List<string>();
            foreach (var tier in tiers)
            {
                best = Math.Max(best, tier.Best);
                var score = tier.Best > 0 ? tier.Best.ToString("N0", LocalizationService.Culture) : "\u2013";
                tierLabels.Add(LocalizationService.T("SpiritTrials.Tier." + tier.Key, tier.Name) + "\n" + score);
            }
            if (!unlocked)
                return new GameModesScreen.ModeView(false,
                    LocalizationService.T("GameModes.Locked.Trials", "Defeat your first boss"),
                    LocalizationService.T("MainMenu.Status.SpiritTrialsLocked", "Spirit Trials unlock after your first boss defeat."));
            var state = best > 0
                ? LocalizationService.Format("GameModes.State.Trials", "Best score {0}", Accent(best.ToString("N0", LocalizationService.Culture)))
                : LocalizationService.T("GameModes.State.TrialsNew", "Four tiers wait");
            return new GameModesScreen.ModeView(true, state,
                LocalizationService.T("GameModes.Body.Trials", "One timed puzzle under rising pressure. Choose a tier, then a class."));
        }

        private static GameModesScreen.ModeView DailyView(DailyGoalState goals)
        {
            DailyGoalService.RefreshIfNewDay(goals, DateTime.Today);
            var defs = DailyGoalService.GetTodayGoals(goals);
            var done = 0;
            var lines = new List<string>();
            for (var i = 0; i < defs.Length && i < goals.TodayGoalCompleted.Length; i++)
            {
                var text = DailyGoalService.GetGoalDescription(defs[i]);
                if (goals.TodayGoalCompleted[i])
                {
                    done++;
                    lines.Add($"<color=#7A6E5A>• {text}  ({LocalizationService.T("GameModes.Daily.Done", "done")})</color>");
                }
                else lines.Add($"• {text}");
            }
            var resetIn = DateTime.Today.AddDays(1) - DateTime.Now;
            lines.Add(LocalizationService.Format("GameModes.Body.DailyStamps", "Ink stamps {0} · new goals in {1:D2}:{2:D2}",
                goals.TotalInkStamps, (int)resetIn.TotalHours, resetIn.Minutes));
            return new GameModesScreen.ModeView(true,
                LocalizationService.Format("GameModes.State.Daily", "{0} of 3 · streak {1}", done, Accent(goals.CurrentStreak.ToString())),
                string.Join("\n", lines));
        }

        private static GameModesScreen.ModeView MonthlyView(SeasonalChallengeState seasonal)
        {
            var today = DateTime.Today;
            var theme = SeasonalChallengeService.GetThemeName(today.Year, today.Month);
            var daysLeft = (new DateTime(today.Year, today.Month, 1).AddMonths(1) - today).Days;
            var days = LocalizationService.FormatPlural(daysLeft, "GameModes.Days.One", "GameModes.Days.Many", "{0} day", "{0} days");
            var best = seasonal?.GetBest(today.Year, today.Month) ?? 0;
            var bestLine = best > 0
                ? LocalizationService.Format("Challenge.Monthly.PersonalBest", "Personal Best: {0:N0}", best)
                : LocalizationService.T("Challenge.Monthly.PersonalBest.None", "Personal Best: -");
            var cfg = SeasonalChallengeService.BuildChallengeConfig(today.Year, today.Month);
            return new GameModesScreen.ModeView(true,
                LocalizationService.Format("GameModes.State.Monthly", "{0} · {1}", theme, Accent(days)),
                $"{theme}: {SeasonalChallengeService.GetPanelSubtitle(cfg)}\n{bestLine}\n{SeasonalChallengeService.GetCountdownLabel(today)}");
        }

        private static string Accent(string text) => $"<color={AccentColor}>{text}</color>";

    }
}
