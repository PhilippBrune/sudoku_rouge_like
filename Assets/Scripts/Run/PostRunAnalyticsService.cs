using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class PostRunAnalyticsService
    {
        public int TotalPuzzlesSolved;
        public int TotalMistakes;
        public int TotalGoldEarned;
        public int TotalItemsUsed;
        public int FloorsCleared;
        public bool RunCompleted;
        public int PerfectPuzzleCount;
        public int CursedPuzzlesCompleted;
        public List<TileXpEntry> TileXpLog = new List<TileXpEntry>();

        public void RecordPuzzleSolved(int mistakes, int goldEarned)
        {
            TotalPuzzlesSolved++;
            TotalMistakes += mistakes;
            TotalGoldEarned += goldEarned;
        }

        public void RecordItemUsed()
        {
            TotalItemsUsed++;
        }

        public void RecordFloorCleared()
        {
            FloorsCleared++;
        }

        public void RecordRunComplete()
        {
            RunCompleted = true;
        }

        public void RecordPerfectPuzzle()
        {
            PerfectPuzzleCount++;
        }

        public void RecordCursedPuzzleCompleted()
        {
            CursedPuzzlesCompleted++;
        }

        public void RecordTileXp(TileXpEntry entry)
        {
            TileXpLog.Add(entry);
        }

        public int GetTotalXp()
        {
            var total = 0;
            for (var i = 0; i < TileXpLog.Count; i++)
                total += TileXpLog[i].TotalXp;
            return total;
        }

        public PostRunAnalytics Build()
        {
            var result = new PostRunAnalytics
            {
                TotalMistakes = TotalMistakes
            };

            var highestMistakes = 0;
            var highestStars = 0;
            for (var i = 0; i < TileXpLog.Count; i++)
            {
                if (TileXpLog[i].Stars > highestStars)
                    highestStars = TileXpLog[i].Stars;
            }

            for (var i = 0; i < TileXpLog.Count; i++)
            {
                var mistakes = i < TileXpLog.Count ? 0 : 0;
                result.MistakesPerPuzzle.Add(mistakes);
            }

            result.HardestPuzzleStars = highestStars;
            result.HighestSinglePuzzleMistakes = highestMistakes;
            result.PerfectPuzzleCount = PerfectPuzzleCount;
            result.CursedPuzzlesCompleted = CursedPuzzlesCompleted;
            result.RunScore = CalculateRunScore(TileXpLog, PerfectPuzzleCount, CursedPuzzlesCompleted,
                TotalMistakes, RunCompleted);

            if (TotalMistakes > 5)
                result.ImprovementSuggestions.Add("Try using pencil marks to reduce mistakes.");
            if (TotalItemsUsed == 0)
                result.ImprovementSuggestions.Add("Use items to gain an advantage in harder puzzles.");

            return result;
        }

        /// <summary>Run score: base XP × completion bonus × perfect bonus × cursed bonus, minus mistake penalty.</summary>
        public static int CalculateRunScore(List<TileXpEntry> tiles, int perfectPuzzles,
            int cursedCompleted, int totalMistakes, bool victory)
        {
            var totalXp = 0;
            for (var i = 0; i < tiles.Count; i++) totalXp += tiles[i].TotalXp;

            var score = totalXp;

            // Perfect puzzle bonus: +5% per perfect puzzle (additive)
            score = (int)(score * (1f + perfectPuzzles * 0.05f));

            // Cursed completion bonus: +10% per cursed puzzle accepted and cleared
            score = (int)(score * (1f + cursedCompleted * 0.10f));

            // Victory bonus: +25%
            if (victory) score = (int)(score * 1.25f);

            // Mistake penalty: -2% per mistake, floor at 50% of base
            var misPenalty = System.Math.Min(0.50f, totalMistakes * 0.02f);
            score = (int)(score * (1f - misPenalty));

            return System.Math.Max(0, score);
        }
    }
}
