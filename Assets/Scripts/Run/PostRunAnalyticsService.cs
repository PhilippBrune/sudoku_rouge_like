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
        // Floor of each solved puzzle, parallel to _mistakesPerPuzzle (SCORE-GRADE-003); shorter in older saves.
        [UnityEngine.SerializeField] private List<int> _floorPerPuzzle = new List<int>();

        public void RecordPuzzleSolved(int mistakes, int goldEarned, int floor = -1)
        {
            TotalPuzzlesSolved++;
            TotalGoldEarned += goldEarned;
            // A save from before floors were recorded has fewer floors than puzzles; pad so the two lists stay parallel.
            while (_floorPerPuzzle.Count < _mistakesPerPuzzle.Count) _floorPerPuzzle.Add(-1);
            _mistakesPerPuzzle.Add(mistakes);
            _floorPerPuzzle.Add(floor);
        }

        /// <summary>
        /// Counts one mistake when it happens. The total used to grow only when a puzzle was solved, so the mistakes of
        /// the puzzle that ended a run (or an Endless Zen session) never counted (GAP-059).
        /// </summary>
        public void RecordMistake()
        {
            TotalMistakes++;
        }

        /// <summary>An item took the last mistake back (Ginkgo Leaf): it no longer counts.</summary>
        public void RecordMistakeUndone()
        {
            if (TotalMistakes > 0) TotalMistakes--;
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

        /// <summary>
        /// The run's analytics and score. <paramref name="goldLeft"/> is the gold still held (SCORE-GOLD-001);
        /// <paramref name="lostPuzzleFloor"/> is the floor of the unsolved puzzle that ended the run, or -1 (SCORE-GRADE-002).
        /// </summary>
        public PostRunAnalytics Build(int goldLeft = 0, int lostPuzzleFloor = -1, int lostPuzzleMistakes = 0, int weeklySeconds = -1)
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
            result.GoldLeft = System.Math.Max(0, goldLeft);
            result.WeeklySeconds = weeklySeconds;
            result.PuzzleGrades = BuildPuzzleGrades(lostPuzzleFloor, lostPuzzleMistakes);
            result.RunScore = RunScoreBreakdown.Calculate(result.BaseXp, RunScoreBreakdown.GradesOf(result.PuzzleGrades),
                CursedPuzzlesCompleted, RunCompleted, result.GoldLeft, weeklySeconds).Total;

            if (TotalMistakes > 5)
                result.ImprovementSuggestions.Add("Try using pencil marks to reduce mistakes.");
            if (TotalItemsUsed == 0)
                result.ImprovementSuggestions.Add("Use items to gain an advantage in harder puzzles.");

            return result;
        }

        // [REQ: SCORE-GRADE-001] [REQ: SCORE-GRADE-002] One grade per solved puzzle in the order played, then the puzzle
        // that ended the run as Messy.
        private List<PuzzleGradeEntry> BuildPuzzleGrades(int lostPuzzleFloor, int lostPuzzleMistakes)
        {
            var grades = new List<PuzzleGradeEntry>(_mistakesPerPuzzle.Count + 1);
            for (var i = 0; i < _mistakesPerPuzzle.Count; i++)
                grades.Add(new PuzzleGradeEntry
                {
                    Floor = i < _floorPerPuzzle.Count ? _floorPerPuzzle[i] : -1,
                    Mistakes = _mistakesPerPuzzle[i],
                    Grade = RunScoreBreakdown.GradeFor(_mistakesPerPuzzle[i])
                });
            if (lostPuzzleFloor >= 0)
                grades.Add(new PuzzleGradeEntry
                {
                    Floor = lostPuzzleFloor,
                    Mistakes = lostPuzzleMistakes,
                    EndedRun = true,
                    Grade = PuzzleGrade.Messy
                });
            return grades;
        }
    }
}
