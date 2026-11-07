using System;
using SudokuRoguelike.Data;

namespace SudokuRoguelike.Run
{
    // [REQ: RUN-FLOORS-001] Playtest 0.4.1 on a Legion Go (owner's pick T1, 2026-11-05): the Garden Run is three floors, the
    // gardens of Floors 1, 3 and 5 (the Bamboo Courtyard, the Koi Terrace, the Shrine Summit). Each is denser than before:
    // bigger boards, more gold and XP per puzzle, the skipped garden's warden in the middle (RunGraphService) and a shop
    // after each boss. A run saved before keeps its five floors (RunState.ThreeFloors false).
    public sealed partial class RunDirector
    {
        public const int ThreeFloorTotal = 3;
        public const float ThreeFloorGold = 1.4f;
        public const float ThreeFloorXp = 1.5f;

        private void BeginThreeFloors()
        {
            State.ThreeFloors = true;
            State.TotalFloors = ThreeFloorTotal;
            State.GlobalGoldMultiplier *= ThreeFloorGold;
            State.AllNodesRevealed = Classes.ClassPassiveService.RevealsFloor(State);   // the first floor too (CLASS-PASSIVE-002)
        }

        /// <summary>The board sizes of a garden: 6×6–7×7, 7×7–9×9 and 9×9 on a three-floor run, else the garden's own.</summary>
        public static (int min, int max) BoardSizesFor(int floor, bool threeFloors)
        {
            if (!threeFloors) return (FloorThemeData.GetMinBoardSize(floor), FloorThemeData.GetMaxBoardSize(floor));
            return floor <= 0 ? (6, 7) : floor <= 2 ? (7, 9) : (9, 9);
        }

        // Each solved puzzle of a three-floor run gives half again its XP (there are about half as many).
        private void ScaleThreeFloorXp()
        {
            if (!State.ThreeFloors || TileXpLog.Count == 0) return;
            var entry = TileXpLog[TileXpLog.Count - 1];
            entry.TotalXp = (int)Math.Round(entry.TotalXp * ThreeFloorXp, MidpointRounding.AwayFromZero);
        }
    }
}
