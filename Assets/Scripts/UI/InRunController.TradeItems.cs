using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.UI
{
    // [REQ: ITEM-PACK-001] [REQ: ITEM-TRADE-001] RM-104's items in the bag (owner's picks 2026-11-04): the Seed Pouch is
    // planted, the three trades pay HP, pencil marks or Max HP first. An item whose price cannot be paid stays in the bag.
    public sealed partial class InRunController
    {
        public const int BloodInkHp = 2, BloodInkCells = 3, InkPactPencil = 6;

        /// <summary>True when the item was one of RM-104's (used or refused with a status line).</summary>
        private bool UseTradeItem(int slotIndex, ItemInstance item, RunDirector run)
        {
            var s = run.State;
            var board = run.CurrentBoard;
            switch (item.Type)
            {
                case ItemType.SeedPouch:
                    if (!run.PlantSeedPouch())
                    {
                        SetStatus(LocalizationService.Format("Item.Status.SeedPouchFull", "{0} Seed Pouches are already growing.",
                            RunDirector.SeedPouchesAtOnce));
                        return true;
                    }
                    run.TryUseItem(slotIndex);
                    SetStatus(LocalizationService.Format("Item.Status.SeedPouch", "Seed Pouch planted: a Rare item after {0} solved puzzles.",
                        RunDirector.SeedPouchPuzzles));
                    return true;
                case ItemType.BloodInk:
                    if (board == null) { SetStatus(T("No active board.")); return true; }
                    if (s.CurrentHP <= BloodInkHp)
                    {
                        SetStatus(LocalizationService.Format("Item.Status.NeedHp", "{0} needs more than {1} HP.", ItemService.GetItemName(item.Type), BloodInkHp));
                        return true;
                    }
                    var filled = FillRandomEmptyCells(board, run, BloodInkCells);
                    if (filled == 0) { SetStatus(T("No incomplete unit found.")); return true; }
                    s.CurrentHP -= BloodInkHp;
                    run.TryUseItem(slotIndex);
                    SetStatus(LocalizationService.Format("Item.Status.BloodInk", "Blood Ink: -{0} HP, {1} cell(s) written.", BloodInkHp, filled));
                    RenderBoard();
                    return true;
                case ItemType.InkPact:
                    if (board == null) { SetStatus(T("No active board.")); return true; }
                    if (s.CurrentPencil < InkPactPencil)
                    {
                        SetStatus(LocalizationService.Format("Item.Status.NeedPencil", "{0} needs {1} pencil marks.", ItemService.GetItemName(item.Type), InkPactPencil));
                        return true;
                    }
                    var count = FillMostCompleteUnit(board, run);
                    if (count == 0) { SetStatus(T("No incomplete unit found.")); return true; }
                    s.CurrentPencil -= InkPactPencil;
                    run.TryUseItem(slotIndex);
                    SetStatus(LocalizationService.Format("Item.Status.InkPact", "Ink Pact: -{0} pencil marks, {1} cell(s) written.", InkPactPencil, count));
                    RenderBoard();
                    return true;
                case ItemType.RedThread:
                    if (s.MaxHP <= 1)
                    {
                        SetStatus(LocalizationService.Format("Item.Status.NeedMaxHp", "{0} needs more than 1 Max HP.", ItemService.GetItemName(item.Type)));
                        return true;
                    }
                    s.MaxHP--;
                    s.CurrentHP = Math.Min(s.CurrentHP, s.MaxHP);
                    run.TryUseItem(slotIndex);
                    var relic = run.TakeRandomRelic();
                    SetStatus(LocalizationService.Format("Item.Status.RedThread", "Red Thread: -1 Max HP, +{0}.",
                        relic != null ? RelicService.GetRelicName(relic.Id) : "-"));
                    return true;
                default:
                    return false;
            }
        }

        // Blood Ink: the correct digit in up to `count` empty cells, picked by the run's seed and the cells still open.
        private static int FillRandomEmptyCells(SudokuBoard board, RunDirector run, int count)
        {
            var open = new List<(int r, int c)>();
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (!board.GivenMask[r, c] && board.Cells[r, c] == 0) open.Add((r, c));
            var rng = new Random(run.State.Seed ^ (run.State.Depth * 7919) ^ (open.Count * 104729));
            var filled = 0;
            while (filled < count && open.Count > 0)
            {
                var pick = rng.Next(open.Count);
                var (row, col) = open[pick];
                open.RemoveAt(pick);
                if (run.PlaceNumber(row, col, board.Solution[row, col]) == PlaceResult.Correct) filled++;   // a locked cell is skipped
            }
            return filled;
        }
    }
}
