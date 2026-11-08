using System;
using System.Globalization;
using System.Text;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// [REQ: RUN-HISTORY-001] The last five finished Garden Runs and walks for the main menu's slip (RM-230, the owner's pick
    /// C): seed, class, result, items, relics and the board the run ended on with its last wrong digit. Kept apart from the
    /// 200-entry <see cref="RunHistoryEntry"/> log, so only these five carry a board.
    /// </summary>
    public static class RecentRuns
    {
        public const int MaxEntries = 5;

        public static RecentRun Capture(RunDirector run, bool victory, int floorReached, DateTime utcNow)
        {
            var state = run.State;
            var entry = new RecentRun
            {
                EndedUtc = utcNow.ToString("o", CultureInfo.InvariantCulture),
                ClassId = state.ClassId,
                DailyWalk = state.DailyWalk,
                MonthlyWalk = state.IsSeasonalChallenge,
                HarmonyLevel = state.HarmonyLevel,
                Victory = victory,
                FloorReached = floorReached,
                ActiveSeconds = Mathf.RoundToInt(state.ActiveSeconds),
                Seed = state.Seed,
                Garden = state.CurrentFloor,
                Tile = state.CurrentNodeIndex
            };
            state.HeldItems?.ForEach(i => { if (i != null) entry.Items.Add(i.Type); });
            state.HeldRelics?.ForEach(r => { if (r != null) entry.Relics.Add(r.Id); });

            var board = run.CurrentBoard;
            var config = run.CurrentLevelConfig;
            if (board == null || config == null) return entry;
            int size = board.Size, wrongRow = state.LastWrongRow, wrongCol = state.LastWrongCol;
            var digits = new StringBuilder(size * size);
            var givens = new StringBuilder(size * size);
            var regions = new StringBuilder(size * size);
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                digits.Append((char)('0' + board.Cells[r, c]));
                givens.Append(board.GivenMask[r, c] ? '1' : '0');
                regions.Append((char)('0' + board.RegionMap[r, c]));
            }
            entry.BoardSize = size;
            entry.Stars = config.Stars;
            entry.Digits = digits.ToString();
            entry.Givens = givens.ToString();
            entry.Regions = regions.ToString();
            entry.Rules.AddRange(config.ActiveModifiers);
            if (wrongRow >= 0 && wrongRow < size && wrongCol >= 0 && wrongCol < size)
            {
                entry.WrongRow = wrongRow;
                entry.WrongCol = wrongCol;
                entry.WrongDigit = state.LastWrongDigit;
                entry.RightDigit = board.Solution[wrongRow, wrongCol];
            }
            return entry;
        }

        /// <summary>Puts the run first and drops the oldest beyond <see cref="MaxEntries"/>.</summary>
        public static void Add(SaveFileEnvelope envelope, RecentRun entry)
        {
            if (envelope == null || entry == null) return;
            envelope.RecentRuns ??= new System.Collections.Generic.List<RecentRun>();
            envelope.RecentRuns.Insert(0, entry);
            if (envelope.RecentRuns.Count > MaxEntries)
                envelope.RecentRuns.RemoveRange(MaxEntries, envelope.RecentRuns.Count - MaxEntries);
        }
    }
}
