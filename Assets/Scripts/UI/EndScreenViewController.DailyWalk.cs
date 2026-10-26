using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-028] The Daily Walk's result (owner 2026-10-26): the day's tally and goals on the left, the XP that
    // waits for a class in the middle (DailyWalkGiveView), the day's board on the right, the streak as a stamp. The walk
    // has no class, so its XP goes to the class the player gives it to; Back leaves it waiting.
    public sealed partial class EndScreenViewController
    {
        private Text _dailyBoard;

        private void ShowDailyWalkResult(RunDirector run, bool victory)
        {
            ClearBossClearedDynamicContent();
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
            var state = run.State;
            var result = _map.BuildRunResult(victory, 0, 0);
            PersistResult(result, run);

            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var profile = new ProfileService(save);
            var walkXp = result?.XpEarned ?? 0;
            var settled = state.DailyWalkScored ? profile.SettleDailyWalk(run.DailyGoals, walkXp) : 0;
            var floorReached = victory ? state.TotalFloors : state.CurrentFloor + 1;
            RunLengthLog.Record(save, state, victory, floorReached, Time.realtimeSinceStartup);   // [REQ: RUN-LENGTH-001]
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            var goals = envelope.DailyGoals ?? new DailyGoalState();
            var score = result != null ? RunScoreBreakdown.For(result).Total : 0;

            UseRunEndScroll(true);
            var screen = ScrollUiKit.Area(_gameOverPanel.transform, "DailyWalkResult", Vector2.zero, Vector2.one);
            _runEndScroll = screen.gameObject;
            ScrollUiKit.Painting(screen, "DailyPainting", "bg_daily_walk", new Color(0.80f, 0.79f, 0.78f, 1f));
            var eyebrow = T("Daily Walk") + " · " + DateTime.Today.ToString("dddd d MMMM", LocalizationService.Culture);
            ScrollUiKit.Banner(screen, "DailyBanner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "DailyEyebrow", eyebrow, "DailyTitle", victory ? T("DailyWalk.Result.Victory") : F("DailyWalk.Result.Defeat", floorReached));
            BuildDailyTally(screen, state, result, goals, floorReached, score);
            DailyWalkGiveView.Build(screen, new Vector2(0.333f, 0.296f), new Vector2(0.667f, 0.815f),
                new Vector2(0.40f, 0.206f), new Vector2(0.60f, 0.259f), goals.PendingWalkXp,
                DailySum(state.DailyWalkScored, walkXp, settled, goals), envelope.MetaProgress,
                profile.GiveWaitingXp, () => OnReturnToMenuPressed?.Invoke());
            var back = ScrollUiKit.PillButton(screen, "BtnDailyBack", new Vector2(0.29f, 0.206f), new Vector2(0.39f, 0.259f),
                T("Back"), 22, new Color(0.24f, 0.20f, 0.16f, 1f), ScrollUiKit.PaperText, 30f);
            back.onClick.AddListener(() => OnReturnToMenuPressed?.Invoke());
            var give = screen.Find("BtnGiveXp")?.GetComponent<Button>();
            if (give != null)
            {
                back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = give };
                var nav = give.navigation;
                nav.selectOnLeft = back;
                give.navigation = nav;
            }
            BuildDailyBoard(screen);
            BuildStreakStamp(screen, goals, state.DailyWalkScored);
            var hint = ScrollUiKit.Label(screen, "DailyHint", T("DailyWalk.Result.Hint"), 18, TextAnchor.MiddleCenter,
                new Color(0.88f, 0.84f, 0.74f, 1f), new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.09f), 12);
            Shade(hint);

            // The board of the day the walk began on (a walk past midnight stays on its day).
            var day = DateTime.TryParseExact(goals.LastScoredWalkDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var walked) && state.DailyWalkScored ? walked : DateTime.Today;
            var board = SteamLeaderboardService.DailyWalkBoard(day);
            SteamLeaderboardService.SubmitToBoard(board, state.DailyWalkScored ? score : 0, ShowDailyStanding);
        }

        // [REQ: DAILY-WALK-003] How the XP adds up: the walk ×1.5, the goals, the streak; XP that waited from before.
        private static string DailySum(bool scored, int walkXp, int settled, DailyGoalState goals)
        {
            string sum;
            if (!scored) sum = T("DailyWalk.Result.Replay");
            else
            {
                var day = DailyGoalService.StreakDay(goals);
                var streak = DailyGoalService.StreakBonusPercent(day);
                var goalXp = 0;
                foreach (var tier in DailyGoalService.GoalsMet(goals)) goalXp += DailyGoalService.GoalXp(tier);
                var dawn = (int)Math.Round(walkXp * DailyGoalService.DawnBonus, MidpointRounding.AwayFromZero);
                sum = streak > 0
                    ? F("DailyWalk.Result.Sum", walkXp, dawn, goalXp, day, streak, settled)
                    : F("DailyWalk.Result.SumNoStreak", walkXp, dawn, goalXp, settled);
            }
            var before = goals.PendingWalkXp - settled;
            return before > 0 ? sum + "\n" + F("DailyWalk.Result.Waiting", before) : sum;
        }

        // The day's garden: floors, HP, puzzles, perfect puzzles and the score; under it the three goals with their XP.
        private static void BuildDailyTally(Transform screen, RunState state, RunResult result, DailyGoalState goals, int floorReached,
            int score)
        {
            var paper = ScrollUiKit.Fill(screen, "DailyTally", new Vector2(0.078f, 0.296f), new Vector2(0.307f, 0.815f), ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var t = paper.rectTransform;
            ScrollUiKit.BrocadeBand(t, "BrocadeTop", new Vector2(0f, 0.968f), Vector2.one, true);
            ScrollUiKit.BrocadeBand(t, "BrocadeBottom", Vector2.zero, new Vector2(1f, 0.032f), true);
            ScrollUiKit.Label(t, "TallyHeader", T("DailyWalk.Result.Garden").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.10f, 0.875f), new Vector2(0.90f, 0.915f), 11);
            var rows = new (string Label, string Value)[]
            {
                (T("DailyWalk.Result.Floors"), F("InRun.End.Scroll.FloorOf", floorReached, state.TotalFloors)),
                (T("InRun.Reward.Row.Hp"), $"{state.CurrentHP} / {state.MaxHP}"),
                (T("InRun.End.Scroll.PuzzlesSolved"), (result?.TileXpEntries?.Count ?? 0).ToString()),
                (T("DailyWalk.Result.Perfect"), (result?.PerfectPuzzleCount ?? 0).ToString()),
                (T("InRun.End.Scroll.Score"), Number(score))
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var centre = 0.82f - i * 0.072f;
                ScrollUiKit.ValueRow(t, $"Tally_{i}", new Vector2(0.10f, centre - 0.03f), new Vector2(0.90f, centre + 0.03f),
                    rows[i].Label, rows[i].Value, 21, ScrollUiKit.Ink);
            }
            ScrollUiKit.Label(t, "GoalsHeader", T("DailyWalk.Result.Goals").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.10f, 0.40f), new Vector2(0.90f, 0.44f), 11);
            var defs = DailyGoalService.GetTodayGoals(goals);
            for (var i = 0; i < defs.Length; i++)
            {
                var done = state.DailyWalkScored && goals.TodayGoalCompleted[i];
                var centre = 0.35f - i * 0.09f;
                var mark = done ? "<color=#2F6B3A>✓</color> " : "<color=#9E2A2B>○</color> ";
                var line = ScrollUiKit.Label(t, $"Goal_{i}", mark + DailyGoalService.GetGoalDescription(defs[i]), 17, TextAnchor.MiddleLeft,
                    ScrollUiKit.Ink, new Vector2(0.10f, centre - 0.04f), new Vector2(0.74f, centre + 0.04f), 11);
                line.supportRichText = true;
                ScrollUiKit.Label(t, $"GoalXp_{i}", DailyGoalService.GetRewardDescription(defs[i]), 17, TextAnchor.MiddleRight,
                    done ? ScrollUiKit.Ink : ScrollUiKit.InkSoft, new Vector2(0.74f, centre - 0.04f), new Vector2(0.90f, centre + 0.04f), 11);
            }
        }

        // [REQ: DAILY-WALK-008] The day's board: filled when Steam answers (ShowDailyStanding).
        private void BuildDailyBoard(Transform screen)
        {
            var paper = ScrollUiKit.Fill(screen, "DailyBoard", new Vector2(0.693f, 0.426f), new Vector2(0.922f, 0.815f), ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -10f);
            _dailyBoard = ScrollUiKit.Label(paper.transform, "BoardText", T("DailyWalk.Board.Loading"), 19, TextAnchor.UpperLeft,
                ScrollUiKit.Ink, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.93f), 12);
            _dailyBoard.supportRichText = true;
        }

        private void ShowDailyStanding(BoardStanding standing)
        {
            if (_dailyBoard == null) return;   // the result was closed before Steam answered
            _dailyBoard.text = DailyBoardText(standing);
        }

        /// <summary>The board's text: the header with the walkers, the place (and among friends), the top three and the
        /// player's own row when it is not among them; without Steam a line that says so.</summary>
        public static string DailyBoardText(BoardStanding standing)
        {
            const string soft = "<size=16><color=#6B5F4C>{0}</color></size>";
            if (standing == null)
                return string.Format(soft, T("GameModes.Daily.BoardHeader").ToUpperInvariant()) + "\n\n" + T("DailyWalk.Board.Offline");
            var header = string.Format(soft, F("DailyWalk.Board.Header", standing.Entries).ToUpperInvariant());
            var lines = new System.Collections.Generic.List<string> { header };
            if (standing.Rank > 0)
            {
                lines.Add($"<size=40>{F("DailyWalk.Board.Place", standing.Rank, standing.Entries)}</size>");
                if (standing.Friends > 1) lines.Add(F("DailyWalk.Board.Friends", standing.FriendRank, standing.Friends));
            }
            else lines.Add(T("DailyWalk.Board.NotOnIt"));
            for (var i = 0; i < standing.Top.Count; i++)
                lines.Add($"{i + 1}. {standing.Top[i].Name}  <color=#3A3228>{Number(standing.Top[i].Score)}</color>");
            if (standing.Rank > standing.Top.Count)
                lines.Add($"<color=#9E2A2B>{standing.Rank}. {T("DailyWalk.Board.You")}</color>");
            return string.Join("\n", lines);
        }

        // The streak as a vermilion stamp: the day of the streak, its XP bonus, the ink stamp of all three goals.
        private static void BuildStreakStamp(Transform screen, DailyGoalState goals, bool scored)
        {
            var day = DailyGoalService.StreakDay(goals);
            var bonus = DailyGoalService.StreakBonusPercent(day);
            var text = F("DailyWalk.Result.Streak", day);
            if (bonus > 0) text += "\n" + F("DailyWalk.Result.StreakBonus", bonus);
            if (scored && goals.TodayGoalCompleted[0] && goals.TodayGoalCompleted[1] && goals.TodayGoalCompleted[2])
                text += "\n" + T("DailyWalk.Result.Stamp");
            // A paper disc under the vermilion ring, so the stamp reads on the painting.
            var stamp = ScrollUiKit.Spot(screen, "StreakStamp", new Vector2(0.805f, 0.30f), new Vector2(184f, 184f),
                new Color(0.94f, 0.89f, 0.78f, 0.92f), ScrollUiKit.Disc());
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            ScrollUiKit.Fill(stamp.transform, "Ring", Vector2.zero, Vector2.one, ScrollUiKit.Vermilion, ScrollUiKit.Ring());
            ScrollUiKit.Label(stamp.transform, "Text", text, 22, TextAnchor.MiddleCenter, ScrollUiKit.Vermilion,
                new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.84f), 13);
        }
    }
}
