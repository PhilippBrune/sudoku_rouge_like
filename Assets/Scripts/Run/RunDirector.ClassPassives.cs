using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Run
{
    // The owner's picks after the 0.4.2 playtest (2026-11-07): the Lantern Seer's lights (LS1) and the Shrine Archivist's
    // find (SA1) [REQ: CLASS-PASSIVE-003], and the reworked items [REQ: ITEM-REWORK-001] (GAP-122, GAP-125).
    public sealed partial class RunDirector
    {
        /// <summary>What the last caught light gave (for the status line); null when none was caught since.</summary>
        public string LastLanternReward { get; set; }

        /// <summary>The cell the Shrine Archivist's last find wrote; null when none.</summary>
        public (int row, int col)? LastArchiveFind { get; set; }

        /// <summary>Double or Quits at the end of the last puzzle: the gold won, -1 for the HP lost, 0 when not used.</summary>
        public int LastDoubleOrQuits { get; set; }

        // ── the Lantern Seer's lights ──

        // The puzzle's own shuffled order of its cells: the lights are the first empty ones, so the board and the level
        // seed give them back after a Save & Quit and a caught light moves on to the next.
        private int[] LanternOrder()
        {
            var size = CurrentBoard.Size;
            var order = new int[size * size];
            for (var i = 0; i < order.Length; i++) order[i] = i;
            var rng = new Random((CurrentLevelConfig?.Seed ?? 0) ^ 0x1A7E);
            for (var i = order.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        /// <summary>The cells the Lantern Seer's lights float over (none for any other class).</summary>
        public List<(int row, int col)> LanternLightCells()
        {
            var lights = new List<(int row, int col)>();
            var want = ClassPassiveService.LanternLights(State);
            if (want == 0 || CurrentBoard == null || CurrentLevelState == null) return lights;
            var size = CurrentBoard.Size;
            foreach (var index in LanternOrder())
            {
                var (r, c) = (index / size, index % size);
                if (CurrentBoard.Cells[r, c] != 0 || CurrentBoard.IsGiven(r, c)) continue;
                lights.Add((r, c));
                if (lights.Count == want) break;
            }
            return lights;
        }

        /// <summary>A new puzzle: no light caught yet, and the lights lift their fog.</summary>
        private void StartLevelPassives()
        {
            State.LanternCaught = 0;
            LiftLanternFog();
        }

        /// <summary>Each light lifts the fog around itself.</summary>
        private void LiftLanternFog()
        {
            if (CurrentOverlay?.FogCells == null || CurrentOverlay.FogCells.Count == 0) return;
            foreach (var (r, c) in LanternLightCells())
                ModifierGeometryGenerator.RevealAdjacentFog(CurrentOverlay, r, c, CurrentBoard.Size);
        }

        // A correct digit was just placed at (row, col): it caught a light when fewer empty cells than lights came before it
        // in the order (only this cell changed).
        private void OnLanternPlacement(int row, int col)
        {
            var want = ClassPassiveService.LanternLights(State);
            if (want == 0) return;
            var size = CurrentBoard.Size;
            var before = 0;
            foreach (var index in LanternOrder())
            {
                var (r, c) = (index / size, index % size);
                if (r == row && c == col) break;
                if (CurrentBoard.Cells[r, c] == 0 && !CurrentBoard.IsGiven(r, c) && ++before >= want) return;
            }
            CatchLantern();
        }

        private void CatchLantern()
        {
            State.LanternCaught++;
            var rng = new Random((CurrentLevelConfig?.Seed ?? 0) * 31 + State.LanternCaught);
            switch (rng.Next(3))
            {
                case 0:
                    State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + ClassPassiveService.LanternHp);
                    LastLanternReward = LocalizationService.Format("Class.Lantern.Hp", "The light gives +{0} HP.", ClassPassiveService.LanternHp);
                    break;
                case 1:
                    State.GainGold(ClassPassiveService.LanternGold);
                    LastLanternReward = LocalizationService.Format("Class.Lantern.Gold", "The light gives +{0} gold.", ClassPassiveService.LanternGold);
                    break;
                default:
                    State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + ClassPassiveService.LanternPencil);
                    LastLanternReward = LocalizationService.Format("Class.Lantern.Pencil", "The light gives +{0} pencil.", ClassPassiveService.LanternPencil);
                    break;
            }
            if (State.LanternCaught % ClassPassiveService.LanternRevealEvery(State) == 0 && WriteRandomDigit(rng))
                LastLanternReward += " " + LocalizationService.T("Class.Lantern.Digit", "It writes a digit too.");
            LiftLanternFog();
        }

        // A random empty cell gets its digit, as an item writes it (no combo, no light caught).
        private bool WriteRandomDigit(Random rng)
        {
            var size = CurrentBoard.Size;
            var empty = new List<(int r, int c)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (CurrentBoard.Cells[r, c] == 0 && !CurrentBoard.IsGiven(r, c)) empty.Add((r, c));
            if (empty.Count == 0) return false;
            var (row, col) = empty[rng.Next(empty.Count)];
            var digit = CurrentBoard.Solution[row, col];
            CurrentBoard.PlaceValue(row, col, digit);
            CurrentLevelState.CorrectPlacements++;
            if (AutoPencilCleanup)   // as a placed digit does (OPT-GAME-002)
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    if (r == row || c == col || CurrentBoard.RegionMap[r, c] == CurrentBoard.RegionMap[row, col])
                        CurrentBoard.RemovePencilMark(r, c, digit);
            if (CurrentOverlay?.FogCells?.Count > 0) ModifierGeometryGenerator.RevealAdjacentFog(CurrentOverlay, row, col, size);
            return true;
        }

        // ── the Shrine Archivist's find ──

        // After a pencil mark changed: a cell with one or two notes, the right one among them, rolls once per puzzle.
        private void TryArchiveFind(int row, int col)
        {
            if (State.ClassId != ClassId.ShrineArchivist || CurrentBoard.Cells[row, col] != 0 || IsCellInFog(row, col)) return;
            var marks = CurrentBoard.GetPencilMarks(row, col);
            var solution = CurrentBoard.Solution[row, col];
            if (marks.Count == 0 || marks.Count > 2 || !marks.Contains(solution)) return;
            if (!CurrentLevelState.ArchiveRolled.Add((row, col))) return;
            var roll = new Random((CurrentLevelConfig?.Seed ?? 0) ^ (row * 97 + col * 13 + 7)).NextDouble();
            if (roll >= ClassPassiveService.ArchiveChance) return;
            PlaceNumber(row, col, solution);
            LastArchiveFind = (row, col);
        }

        // ── the reworked items ──

        // The Golden Kintsugi Jar: a wrong digit becomes the right one while charges last.
        private int KintsugiMend(int row, int col, int value)
        {
            if (State.KintsugiCharges <= 0 || IsPlacementCorrect(row, col, value)) return value;
            State.KintsugiCharges--;
            return CurrentBoard.Solution[row, col];
        }

        public const int KintsugiMends = 3;

        // At a solved puzzle's end, after its gold: Double or Quits, and the per-puzzle effects end.
        private void ResolvePuzzleItems(int puzzleGold)
        {
            State.KintsugiCharges = 0;
            State.LanternCaught = 0;
            if (State.SurveyEase > 0) State.SurveyEase--;
            LastDoubleOrQuits = 0;
            if (!State.DoubleOrQuitsActive) return;
            State.DoubleOrQuitsActive = false;
            if (new Random((CurrentLevelConfig?.Seed ?? 0) * 7919 + State.Depth).NextDouble() < 0.5)
            {
                State.GainGold(puzzleGold);
                LastDoubleOrQuits = puzzleGold;
            }
            else
            {
                State.CurrentHP = Math.Max(1, State.CurrentHP - 1);
                LastDoubleOrQuits = -1;
            }
        }

        /// <summary>Survey Notes: the puzzle after the one it was used in is a whole star easier.</summary>
        public static float SurveyEasedMissingPercent(RunState state, int stars, float missingPercent) =>
            state?.SurveyEase == 1 && stars > 1 ? StarDensityService.MissingPercentForStars(stars - 1) : missingPercent;

        /// <summary>A class item used from the bag; returns the status line. The Loaded Coin works from the bag and is
        /// not used up here.</summary>
        public string ApplyClassItem(ItemInstance item)
        {
            switch (item.Type)
            {
                case ItemType.MonksBeads:
                    State.MonksBeadsCountdown = 5;
                    return LocalizationService.T("Item.Status.MonksBeads", "Monk's Beads: your next 5 correct digits each heal 1 HP.");
                case ItemType.AnnotatedFolio:
                    return LocalizationService.Format("Item.Status.AnnotatedFolio", "Annotated Folio: notes written in {0} cell(s).",
                        WriteTwinCandidates(int.MaxValue));
                case ItemType.DoubleOrQuits:
                    State.DoubleOrQuitsActive = true;
                    return LocalizationService.T("Item.Status.DoubleOrQuits", "Double or Quits: when this puzzle ends, its gold doubles or you lose 1 HP.");
                case ItemType.WornChisel:
                    State.WornChiselActive = true;
                    return LocalizationService.T("Item.Status.WornChisel", "Worn Chisel: 30 % off in every shop of this floor.");
                case ItemType.DimLantern:
                    State.DimLanternUsed = true;
                    return LocalizationService.T("Item.Status.DimLantern", "Dim Lantern: this floor's boss rules are shown on the map.");
                case ItemType.ReedPledge:
                    State.PledgeActive = true;
                    return LocalizationService.T("Item.Status.ReedPledge", "Reed Pledge: solve this puzzle without pencil marks for +4 pencil, +30 gold and +25 XP.");
                case ItemType.SurveyNotes:
                    State.SurveyEase = 2;
                    return LocalizationService.T("Item.Status.SurveyNotes", "Survey Notes: your next puzzle is a whole star easier.");
                default:
                    return LocalizationService.T("Item.Status.LoadedCoin", "The Loaded Coin works from your bag: the next reward fills every Nothing slot.");
            }
        }

        /// <summary>Writes the two possible digits as notes into empty cells that have exactly two (at most <paramref name="max"/>
        /// cells); returns how many. The Annotated Folio does every such cell, the Zen Sand Sifter four.</summary>
        public int WriteTwinCandidates(int max)
        {
            var written = 0;
            var size = CurrentBoard.Size;
            for (var r = 0; r < size && written < max; r++)
            for (var c = 0; c < size && written < max; c++)
            {
                if (CurrentBoard.Cells[r, c] != 0 || CurrentBoard.IsGiven(r, c)) continue;
                var candidates = GetValidCandidates(r, c);
                if (candidates.Count != 2) continue;
                CurrentBoard.AddPencilMark(r, c, candidates[0]);
                CurrentBoard.AddPencilMark(r, c, candidates[1]);
                written++;
            }
            return written;
        }

        /// <summary>The Garden Rake: removes every note the digits in its row, column or box already rule out; returns how
        /// many went and gives 1 pencil for every 3.</summary>
        public int RakeImpossibleNotes()
        {
            var removed = 0;
            var size = CurrentBoard.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                if (CurrentBoard.Cells[r, c] != 0) continue;
                var allowed = GetValidCandidates(r, c);
                foreach (var mark in new List<int>(CurrentBoard.GetPencilMarks(r, c)))
                    if (!allowed.Contains(mark)) { CurrentBoard.RemovePencilMark(r, c, mark); removed++; }
            }
            State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + removed / 3);
            return removed;
        }

        /// <summary>Lifts the fog that is left when the puzzle is solved (a digit written into fog keeps its cell fogged).</summary>
        public void LiftRemainingFog()
        {
            CurrentOverlay?.FogCells?.Clear();
            CurrentLevelState?.FogPendingCells?.Clear();
        }
    }
}
