using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Items
{
    public sealed class ItemService
    {
        private readonly Random _random;

        public ItemService(int seed)
        {
            _random = new Random(seed);
        }

        public List<ItemRollSlot> RollSlots(DifficultyTier difficulty, int stars)
        {
            var slotCount = stars switch
            {
                <= 1 => 2,
                2 => 3,
                3 => 3,
                4 => 4,
                _ => 5,
            };

            var nothingGoldBonus = stars switch
            {
                <= 1 => 8,
                2 => 10,
                3 => 12,
                4 => 14,
                _ => 16
            };

            var slots = new List<ItemRollSlot>(slotCount);
            for (var i = 0; i < slotCount; i++)
            {
                slots.Add(RollSingleSlot(difficulty, stars));
                if (slots[i].IsNothing)
                {
                    slots[i].NothingGoldBonus = nothingGoldBonus;
                }
            }

            EnforceGuarantees(slots, difficulty, stars);
            return slots;
        }

        public void RerollEligibleSlots(List<ItemRollSlot> slots, DifficultyTier difficulty, int stars)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsLocked || slots[i].IsNothing)
                {
                    continue;
                }

                slots[i] = RollSingleSlot(difficulty, stars);
            }

            EnforceGuarantees(slots, difficulty, stars);
        }

        public bool TryUseSolver(SudokuBoard board, ItemRarity rarity, int row, int col)
        {
            if (!board.IsEmpty(row, col))
            {
                return false;
            }

            board.SetCell(row, col, board.Solution[row, col]);
            var bonus = rarity switch
            {
                ItemRarity.Normal => 0,
                ItemRarity.Rare => 1,
                ItemRarity.Epic => 2,
                _ => 0
            };

            ApplyNeighborSolve(board, row, col, bonus);
            return true;
        }

        public List<(int Row, int Col)> UseFinder(SudokuBoard board, ItemRarity rarity, int row, int col)
        {
            var target = board.GetCell(row, col);
            var needed = rarity switch
            {
                ItemRarity.Normal => 1,
                ItemRarity.Rare => 3,
                ItemRarity.Epic => 2,
                _ => 1
            };

            var matches = new List<(int Row, int Col)>();
            if (target == 0)
            {
                return matches;
            }

            for (var r = 0; r < board.Size; r++)
            {
                for (var c = 0; c < board.Size; c++)
                {
                    if ((r == row && c == col) || board.GetCell(r, c) != target)
                    {
                        continue;
                    }

                    matches.Add((r, c));
                    if (matches.Count >= needed)
                    {
                        return matches;
                    }
                }
            }

            return matches;
        }

        private ItemRollSlot RollSingleSlot(DifficultyTier difficulty, int stars)
        {
            var nothingChance = 0.25;
            var roll = _random.NextDouble();

            if (roll < nothingChance)
            {
                return new ItemRollSlot { IsNothing = true, IsLocked = false, NothingGoldBonus = 10 };
            }

            var rarity = RollRarity(difficulty, stars);
            var type = _random.NextDouble() < 0.5 ? ItemType.Solver : ItemType.Finder;

            return new ItemRollSlot
            {
                IsNothing = false,
                IsLocked = false,
                RolledItem = new ItemInstance
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Type = type,
                    Rarity = rarity,
                    Charges = 1
                }
            };
        }

        private ItemRarity RollRarity(DifficultyTier difficulty, int stars)
        {
            var rareChance = 0.15 + ((int)difficulty - 1) * 0.08;
            var epicChance = 0.03 + Math.Max(0, stars - 3) * 0.05;

            if (difficulty == DifficultyTier.Diff5 && stars >= 4 && _random.NextDouble() < epicChance)
            {
                return ItemRarity.Epic;
            }

            return _random.NextDouble() < rareChance ? ItemRarity.Rare : ItemRarity.Normal;
        }

        private void EnforceGuarantees(List<ItemRollSlot> slots, DifficultyTier difficulty, int stars)
        {
            var requiredItems = difficulty == DifficultyTier.Diff1 ? 1 : 2;
            var currentItems = CountItems(slots);

            while (currentItems < requiredItems)
            {
                var index = FindUnlockedNothingSlot(slots);
                if (index < 0)
                {
                    break;
                }

                slots[index] = RollSingleSlot(difficulty, stars);
                currentItems = CountItems(slots);
            }

            if (difficulty >= DifficultyTier.Diff4)
            {
                var hasRareOrHigher = false;
                for (var i = 0; i < slots.Count; i++)
                {
                    var item = slots[i].RolledItem;
                    if (!slots[i].IsNothing && item != null && item.Rarity != ItemRarity.Normal)
                    {
                        hasRareOrHigher = true;
                        break;
                    }
                }

                if (!hasRareOrHigher)
                {
                    var index = FindUnlockedItemSlot(slots);
                    if (index >= 0)
                    {
                        slots[index].RolledItem.Rarity = difficulty == DifficultyTier.Diff5 && stars >= 4
                            ? ItemRarity.Epic
                            : ItemRarity.Rare;
                    }
                }
            }
        }

        private static int CountItems(List<ItemRollSlot> slots)
        {
            var count = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsNothing)
                {
                    count++;
                }
            }

            return count;
        }

        private static int FindUnlockedNothingSlot(List<ItemRollSlot> slots)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsLocked && slots[i].IsNothing)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindUnlockedItemSlot(List<ItemRollSlot> slots)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsLocked && !slots[i].IsNothing && slots[i].RolledItem != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void ApplyNeighborSolve(SudokuBoard board, int row, int col, int count)
        {
            if (count <= 0)
            {
                return;
            }

            var filled = 0;
            for (var r = Math.Max(0, row - 1); r <= Math.Min(board.Size - 1, row + 1); r++)
            {
                for (var c = Math.Max(0, col - 1); c <= Math.Min(board.Size - 1, col + 1); c++)
                {
                    if ((r == row && c == col) || !board.IsEmpty(r, c))
                    {
                        continue;
                    }

                    board.SetCell(r, c, board.Solution[r, c]);
                    filled++;
                    if (filled >= count)
                    {
                        return;
                    }
                }
            }
        }
    }
}
