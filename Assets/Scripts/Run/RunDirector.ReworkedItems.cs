using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    // [REQ: ITEM-REWORK-002] The owner's picks after the 0.4.3 playtest: four items that did what another item did now do something of
    // their own (Chime of Notes, River Bell, Stone Oath, Moth Light). [REQ: RELIC-THREAD-001] the Unbroken Thread.
    public sealed partial class RunDirector
    {
        public const int StoneOathHp = 1;
        public const float StoneOathGold = 1.5f;
        public const int RiverBellGoldPerFiveCombo = 2;

        /// <summary>Chime of Notes: every digit its row, column and box still allow, as a free note in every empty cell without
        /// notes (the player's own notes stay). Returns the number of cells written.</summary>
        public int WriteAllNotes()
        {
            var board = CurrentBoard;
            if (board == null) return 0;
            var written = 0;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (board.Cells[r, c] != 0 || board.IsGiven(r, c) || board.HasAnyPencilMark(r, c)) continue;
                foreach (var digit in GetValidCandidates(r, c)) board.AddPencilMark(r, c, digit);
                written++;
            }
            return written;
        }

        /// <summary>Moth Light: the fog of the chosen cell's box is gone for good; digits waiting under it are judged now.
        /// Returns the number of cells cleared.</summary>
        public int LiftFogFromBox(int row, int col)
        {
            var fog = CurrentOverlay?.FogCells;
            if (fog == null || fog.Count == 0 || CurrentBoard == null) return 0;
            var before = BuildFogSet();
            var region = CurrentBoard.RegionMap[row, col];
            var lifted = fog.RemoveAll(cell => CurrentBoard.RegionMap[cell.Row, cell.Col] == region);
            if (lifted > 0) EvaluateFogPendingCells(before);
            return lifted;
        }

        public bool BoardHasFog => CurrentOverlay?.FogCells?.Count > 0;

        private void PayStoneOath()
        {
            State.StoneOathActive = true;
            State.CurrentHP -= StoneOathHp;
            if (CurrentLevelState != null) CurrentLevelState.HpLost += StoneOathHp;
            RelicService.OnHpChanged(State);
        }

        private int RiverBellGold() => State.RiverBellActive ? RiverBellGoldPerFiveCombo * (State.ComboStreak / 5) : 0;

        /// <summary>After a wrong digit: a shield or the River Bell keeps the combo, the Unbroken Thread halves it, else it ends.</summary>
        private void BreakCombo()
        {
            if (State.LastMistakeShieldAbsorbed || State.RiverBellActive) return;
            State.ComboStreak = RelicService.HasRelicOfType(State, RelicId.UnbrokenThread) ? State.ComboStreak / 2 : 0;
        }
    }
}
