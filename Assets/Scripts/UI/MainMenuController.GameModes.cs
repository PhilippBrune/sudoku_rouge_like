using System;
using System.Collections.Generic;
using UnityEngine;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
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
            // [REQ: UI-STYLE-027] The Daily Walk's caption carries the day; its board fills the side box when Steam answers.
            var caption = _gameModesPanel != null ? _gameModesPanel.transform.Find("ModeSlip/Slip_Daily/Caption") : null;
            if (caption != null)
                caption.GetComponent<UnityEngine.UI.Text>().text = (LocalizationService.T("Daily Walk") + " · "
                    + DateTime.Today.ToString("dddd d MMMM", LocalizationService.Culture)).ToUpperInvariant();
            SteamLeaderboardService.SubmitToBoard(SteamLeaderboardService.DailyWalkBoard(DateTime.Today), 0, ShowDailySide);
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

        // [REQ: UI-STYLE-027] [REQ: DAILY-WALK-001] The Daily Walk's slip: the rules, today's goals, and in the side box the
        // streak, the ink stamps and the day's board (filled when Steam answers). XP still waiting turns the way in into
        // "Give … XP"; a saved run into "Abandon & walk".
        private GameModesScreen.ModeView DailyView(DailyGoalState goals)
        {
            DailyGoalService.RefreshIfNewDay(goals, DateTime.Today);
            var defs = DailyGoalService.GetTodayGoals(goals);
            var walked = goals.LastScoredWalkDate == DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var done = 0;
            var marks = new List<string>();
            for (var i = 0; i < defs.Length; i++)
            {
                var met = goals.TodayGoalCompleted[i];
                if (met) done++;
                marks.Add((met ? "<color=#2F6B3A>✓</color> " : "○ ") + DailyGoalService.GetGoalDescription(defs[i]));
            }
            var body = LocalizationService.T("GameModes.Body.Daily") + "\n" + LocalizationService.T("GameModes.Daily.Goals") + " "
                       + string.Join(" · ", marks);
            var state = walked
                ? LocalizationService.Format("GameModes.State.DailyWalked", "Walked · {0} of 3 · streak {1}", done, Accent(goals.CurrentStreak.ToString()))
                : LocalizationService.Format("GameModes.State.DailyOpen", "Not walked · streak {0}", Accent(goals.CurrentStreak.ToString()));
            _dailySide = DailySide(goals);
            string enter = null;
            if (goals.PendingWalkXp > 0)
                enter = LocalizationService.Format("GameModes.Daily.Give", "Give {0} XP", goals.PendingWalkXp);
            else if (_gameBootstrap != null && _gameBootstrap.HasResumableRun())
                enter = LocalizationService.T("GameModes.Daily.AbandonWalk");
            return new GameModesScreen.ModeView(true, state, body, enter, _dailySide + "\n" + LocalizationService.T("DailyWalk.Board.Loading"));
        }

        private string _dailySide;

        private static string DailySide(DailyGoalState goals)
        {
            var day = DailyGoalService.StreakDay(goals);
            const string soft = "<size=14><color=#6B5F4C>{0}</color></size>";
            return string.Format(soft, LocalizationService.T("GameModes.Daily.StreakHeader").ToUpperInvariant()) + "\n"
                   + LocalizationService.Format("GameModes.Daily.Streak", "Day {0}: +{1} % XP", day, DailyGoalService.StreakBonusPercent(day)) + "\n"
                   + LocalizationService.Format("GameModes.Daily.Stamps", "Ink stamps {0} · this week {1} of 7", goals.TotalInkStamps,
                       ThisWeek(goals)) + "\n"
                   + string.Format(soft, LocalizationService.T("GameModes.Daily.BoardHeader").ToUpperInvariant());
        }

        private static int ThisWeek(DailyGoalState goals)
        {
            var today = DateTime.Today;
            var week = $"{System.Globalization.ISOWeek.GetYear(today):0000}-W{System.Globalization.ISOWeek.GetWeekOfYear(today):00}";
            return goals.StampWeek == week ? goals.StampsThisWeek : 0;
        }

        // [REQ: DAILY-WALK-008] The day's board in the side box: the player's place (or "not walked"), the walkers, the best.
        private void ShowDailySide(BoardStanding standing)
        {
            if (_gameModesScreen == null || _dailySide == null) return;
            string line;
            if (standing == null) line = LocalizationService.T("DailyWalk.Board.Offline");
            else
            {
                var place = standing.Rank > 0
                    ? LocalizationService.Format("DailyWalk.Board.Place", "Place {0} of {1}", standing.Rank, standing.Entries)
                    : LocalizationService.T("GameModes.Daily.NotWalked");
                var best = standing.Top.Count > 0 ? standing.Top[0].Score.ToString("N0", LocalizationService.Culture) : "\u2013";
                line = LocalizationService.Format("GameModes.Daily.BoardLine", "{0} · {1} walkers · best {2}", place, standing.Entries, Accent(best));
            }
            _gameModesScreen.SetSide(GameModesScreen.Mode.Daily, _dailySide + "\n" + line);
        }

        /// <summary>The Daily Walk's way in: XP still waiting is given first; then the day's walk (a saved run is abandoned).</summary>
        public void EnterDailyWalk()
        {
            var envelope = _save.HasSaveFile() ? _save.Load() : new SaveFileEnvelope();
            if ((envelope.DailyGoals?.PendingWalkXp ?? 0) > 0)
            {
                ShowWaitingXp(envelope);
                return;
            }
            if (_gameBootstrap == null) return;
            if (_gameBootstrap.HasResumableRun()) _gameBootstrap.AbandonSavedRun();
            ClearSeed();
            _gameBootstrap.LaunchRun(new LaunchRequest
            {
                Mode = GameMode.GardenRun, DailyWalk = true, HasSeed = true, Seed = DailyGoalService.WalkSeed(DateTime.Today),
                AllowIrregularPuzzles = true
            });
        }

        private GameObject _waitingXp;

        // [REQ: UI-STYLE-028] The XP waiting from a walk left on its result (or the weekly scroll), given on the menu.
        private void ShowWaitingXp(SaveFileEnvelope envelope)
        {
            if (_gameModesPanel == null) return;
            ScrollUiKit.DestroyNow(_waitingXp);
            var screen = ScrollUiKit.Fill(_gameModesPanel.transform, "WaitingXp", Vector2.zero, Vector2.one, Color.black);
            screen.raycastTarget = true;
            _waitingXp = screen.gameObject;
            ScrollUiKit.Painting(screen.transform, "Painting", "bg_daily_walk", new Color(0.62f, 0.62f, 0.62f, 1f));
            ScrollUiKit.Banner(screen.transform, "Banner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "Eyebrow", LocalizationService.T("Daily Walk"), "Title", LocalizationService.T("DailyWalk.Give.WaitingTitle"));
            void Close()
            {
                ScrollUiKit.DestroyNow(_waitingXp);
                _waitingXp = null;
                RefreshGameModesPanel();
                _gameModesScreen?.Show(GameModesScreen.Mode.Daily);
                var curtain = _gameModesPanel.transform.Find("BtnDailyWalk");
                var events = UnityEngine.EventSystems.EventSystem.current;
                if (curtain != null && events != null) events.SetSelectedGameObject(curtain.gameObject);
            }
            var goals = envelope.DailyGoals;
            DailyWalkGiveView.Build(screen.transform, new Vector2(0.333f, 0.30f), new Vector2(0.667f, 0.80f),
                new Vector2(0.40f, 0.20f), new Vector2(0.60f, 0.255f), goals.PendingWalkXp,
                LocalizationService.Format("DailyWalk.Result.Waiting", "+{0} waiting from before", goals.PendingWalkXp),
                envelope.MetaProgress, Profile.GiveWaitingXp, Close);
            var back = ScrollUiKit.PillButton(screen.transform, "BtnWaitingBack", new Vector2(0.29f, 0.20f), new Vector2(0.39f, 0.255f),
                LocalizationService.T("Back"), 22, new Color(0.24f, 0.20f, 0.16f, 1f), ScrollUiKit.PaperText, 30f);
            back.onClick.AddListener(Close);
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
