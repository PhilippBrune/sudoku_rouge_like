using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    // Serializable: kept in RunState so a resumed run keeps its analytics (GAP-038).
    [System.Serializable]
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
        public bool PencilEverUsed;
        public List<TileXpEntry> TileXpLog = new List<TileXpEntry>();
        [UnityEngine.SerializeField] private List<int> _mistakesPerPuzzle = new List<int>();

        public void RecordPuzzleSolved(int mistakes, int goldEarned)
        {
            TotalPuzzlesSolved++;
            TotalMistakes += mistakes;
            TotalGoldEarned += goldEarned;
            _mistakesPerPuzzle.Add(mistakes);
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

        public void RecordPencilPlaced()
        {
            PencilEverUsed = true;
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
                var mistakes = i < _mistakesPerPuzzle.Count ? _mistakesPerPuzzle[i] : 0;
                if (mistakes > highestMistakes) highestMistakes = mistakes;
                result.MistakesPerPuzzle.Add(mistakes);
            }

            result.HardestPuzzleStars = highestStars;
            result.HighestSinglePuzzleMistakes = highestMistakes;
            result.PerfectPuzzleCount = PerfectPuzzleCount;
            result.CursedPuzzlesCompleted = CursedPuzzlesCompleted;
            result.BaseXp = GetTotalXp();
            result.RunCompleted = RunCompleted;
            result.RunScore = CalculateRunScore(TileXpLog, PerfectPuzzleCount, CursedPuzzlesCompleted,
                TotalMistakes, RunCompleted);

            if (TotalMistakes > 5)
                result.ImprovementSuggestions.Add("Try using pencil marks to reduce mistakes.");
            if (TotalItemsUsed == 0)
                result.ImprovementSuggestions.Add("Use items to gain an advantage in harder puzzles.");

            return result;
        }

        /// <summary>Run score (GameDesignSpec §9c): the base XP plus each bonus and the penalty as a share of it (GAP-053).</summary>
        public static int CalculateRunScore(List<TileXpEntry> tiles, int perfectPuzzles,
            int cursedCompleted, int totalMistakes, bool victory)
        {
            var totalXp = 0;
            for (var i = 0; i < tiles.Count; i++) totalXp += tiles[i].TotalXp;
            return RunScoreBreakdown.Calculate(totalXp, perfectPuzzles, cursedCompleted, totalMistakes, victory).Total;
        }
    }
}
