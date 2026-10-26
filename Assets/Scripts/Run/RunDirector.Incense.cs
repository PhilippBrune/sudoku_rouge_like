using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    // [REQ: INCENSE-CLOCK-003] [REQ: INCENSE-PUZZLE-001] The Incense Walk (IncenseWalkMode.md): puzzle after puzzle on one
    // clock; a wrong digit costs time instead of HP, pencil marks are free, there is no path, bag or relic.
    public sealed partial class RunDirector
    {
        /// <summary>The walk's clock; null outside the Incense Walk.</summary>
        public IncenseWalkService Incense { get; private set; }

        private void StartIncenseRun(LaunchRequest request, int seed)
        {
            State = IncenseWalkService.CreateRunState(request.ClassId, seed, request.ClassLevel);
            Incense = new IncenseWalkService(request.ClassId, seed);
            StartIncenseLevel();
        }

        /// <summary>The walk's next puzzle; Depth counts the puzzles solved.</summary>
        public void AdvanceIncenseLevel()
        {
            if (Incense == null || State?.Mode != GameMode.IncenseWalk) return;
            State.Depth++;
            StartIncenseLevel();
        }

        private void StartIncenseLevel()
        {
            CurrentLevelConfig = Incense.BuildLevel(State.Depth);
            StartLevel(CurrentLevelConfig);
            Incense.OnPuzzleStart();
        }

        // True when the walk took the mistake: the clock pays, not the HP.
        private bool IncenseMistake()
        {
            if (Incense == null || State?.Mode != GameMode.IncenseWalk) return false;
            Incense.OnMistake();
            State.LastMistakeHpLost = 0;
            return true;
        }
    }
}
