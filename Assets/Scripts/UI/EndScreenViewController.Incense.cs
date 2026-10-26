using System;
using System.Globalization;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: INCENSE-SCORE-002] [REQ: INCENSE-BOARD-001] [REQ: INCENSE-XP-001] The Incense Walk's result in the mode-result
    // family (UI-STYLE-026): the walk's numbers, the score large against the best, the class's XP, and on the week's seed
    // the week's board (a practice walk has none).
    public sealed partial class EndScreenViewController
    {
        public static string IncenseBoard(DateTime day) =>
            $"IncenseWalk_{ISOWeek.GetYear(day):0000}-W{ISOWeek.GetWeekOfYear(day):00}";

        private Text _incenseNotes;
        private string _incenseNotesBase;

        public void ShowIncenseResult(RunDirector run)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
            var state = run.State;
            var walk = run.Incense;
            var score = walk?.Score ?? 0;
            var puzzles = walk?.PuzzlesSolved ?? 0;
            var largest = walk?.LargestBoard ?? 0;
            var weekly = state.WeeklySeedRun;

            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var profile = new ProfileService(save);
            var result = _map.BuildRunResult(false, 0, 0);
            if (result != null) result.GoldEarned = 0;   // the walk's puzzles pay no gold toward class unlocks
            if (result != null) profile.RecordRunAndGetNewUnlocks(result, state);   // the class keeps each puzzle's XP
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            var stats = envelope.Statistics ??= new ProfileStats();
            var prevBest = stats.IncenseBestScore;
            stats.IncenseWalks++;
            stats.IncenseBestScore = Math.Max(prevBest, score);
            stats.IncenseMostPuzzles = Math.Max(stats.IncenseMostPuzzles, puzzles);
            stats.IncenseLargestBoard = Math.Max(stats.IncenseLargestBoard, largest);
            profile.UpdateStats(envelope, stats);
            RunLengthLog.Record(save, state, false, puzzles, Time.realtimeSinceStartup);   // [REQ: RUN-LENGTH-001]

            var isNewBest = score > prevBest;
            var end = new ModeEndSummary
            {
                Painting = "bg_garden_grove",
                Good = isNewBest,
                Eyebrow = weekly
                    ? F("Incense.Result.EyebrowWeek", T("Incense Walk"), ISOWeek.GetWeekOfYear(DateTime.Today))
                    : F("Incense.Result.EyebrowPractice", T("Incense Walk")),
                Title = T("Incense.Result.Title"),
                Closing = T("Incense.Result.Closing"),
                RecordHeader = T("Incense.Result.Score"),
                RecordValue = Number(score),
                RecordLine = prevBest <= 0 ? T("Incense.Result.First")
                    : isNewBest ? F("InRun.End.Mode.NewBestLine", Number(prevBest)) : F("Incense.Result.Best", Number(prevBest)),
                NewBest = isNewBest && prevBest > 0,
                Again = RunAgain.Kind.None
            };
            end.Rows.Add((T("Incense.Result.Puzzles"), Number(puzzles)));
            end.Rows.Add((T("Incense.Result.Largest"), largest > 0 ? $"{largest}×{largest}" : "–"));
            end.Rows.Add((T("InRun.End.Mode.Mistakes"), Number(run.GetAnalytics()?.TotalMistakes ?? 0)));
            end.Rows.Add((T("Incense.Result.Xp"), F("InRun.End.Scroll.XpGain", result?.XpEarned ?? 0)));
            _incenseNotesBase = weekly ? T("DailyWalk.Board.Loading") : T("Incense.Result.Practice");
            end.Notes.Add(_incenseNotesBase);
            ShowModeEndScroll(end);
            _incenseNotes = _runEndScroll != null ? _runEndScroll.transform.Find("ModeEndNotes/Text")?.GetComponent<Text>() : null;
            if (weekly) SteamLeaderboardService.SubmitToBoard(IncenseBoard(DateTime.Today), score, ShowIncenseStanding);
        }

        private void ShowIncenseStanding(BoardStanding standing)
        {
            if (_incenseNotes == null) return;   // the result was closed before Steam answered
            _incenseNotes.text = standing == null
                ? T("DailyWalk.Board.Offline")
                : F("Incense.Result.Board", F("DailyWalk.Board.Place", standing.Rank, standing.Entries),
                    standing.Top.Count > 0 ? Number(standing.Top[0].Score) : "–");
        }
    }
}
