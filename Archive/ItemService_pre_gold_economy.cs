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

        // ── Slot count by star rating ──

        public static int GetSlotCount(int stars, int bonusSlots)
        {
            var baseSlots = stars switch
            {
                <= 1 => 2,
                2 => 3,
                3 => 3,
                4 => 4,
                _ => 5,
            };
            return baseSlots + bonusSlots;
        }

        // ── Nothing chance by star rating ──

        private static double GetNothingChance(int stars)
        {
            return stars switch
            {
                <= 1 => 0.25,
                2 => 0.22,
                3 => 0.18,
                4 => 0.15,
                _ => 0.12
            };
        }

        // ── Nothing gold bonus by star rating ──

        private static int GetNothingGoldBonus(int stars)
        {
            return stars switch
            {
                <= 1 => 8,
                2 => 10,
                3 => 12,
                4 => 14,
                _ => 16
            };
        }

        // ── Roll reward slots ──

        public List<ItemRollSlot> RollSlots(int stars, int classLevel, int bonusSlots)
        {
            var slotCount = GetSlotCount(stars, bonusSlots);
            var nothingChance = GetNothingChance(stars);
            var nothingGold = GetNothingGoldBonus(stars);

            var slots = new List<ItemRollSlot>(slotCount);
            for (var i = 0; i < slotCount; i++)
            {
                if (_random.NextDouble() < nothingChance)
                {
                    slots.Add(new ItemRollSlot
                    {
                        IsNothing = true,
                        IsLocked = false,
                        NothingGoldBonus = nothingGold
                    });
                }
                else
                {
                    slots.Add(RollItemSlot(stars, classLevel));
                }
            }

            // Guarantee at least 1 real item
            var hasItem = false;
            for (var i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsNothing) { hasItem = true; break; }
            }
            if (!hasItem && slots.Count > 0)
            {
                slots[0] = RollItemSlot(stars, classLevel);
            }

            return slots;
        }

        /// <summary>Reroll eligible slots (not locked, not Nothing).</summary>
        public void RerollEligibleSlots(List<ItemRollSlot> slots, int stars, int classLevel)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsLocked || slots[i].IsNothing) continue;
                slots[i] = RollItemSlot(stars, classLevel);
            }
        }

        // ── Item catalog: all items with their fixed rarity (for unique items) ──

        private static readonly ItemType[] TieredItems =
        {
            ItemType.Solver, ItemType.Finder, ItemType.InkWell, ItemType.MeditationStone,
            ItemType.WindChime, ItemType.PatternScroll, ItemType.KoiReflection, ItemType.LanternOfClarity
        };

        private static readonly ItemType[] UniqueNormalItems =
        {
            ItemType.GardenRake, ItemType.OfferingBowl, ItemType.PruningShears, ItemType.ZenSandSifter
        };

        private static readonly ItemType[] UniqueRareItems =
        {
            ItemType.GinkgoLeaf, ItemType.RicePaperUmbrella, ItemType.TempleIncense
        };

        private static readonly ItemType[] UniqueEpicItems =
        {
            ItemType.KoiDragonScale, ItemType.GoldenKintsugiJar, ItemType.SilkFan
        };

        public static bool IsTiered(ItemType type)
        {
            return type <= ItemType.LanternOfClarity;
        }

        public static ItemRarity GetFixedRarity(ItemType type)
        {
            return type switch
            {
                ItemType.GardenRake or ItemType.OfferingBowl or ItemType.PruningShears or ItemType.ZenSandSifter
                    => ItemRarity.Normal,
                ItemType.GinkgoLeaf or ItemType.RicePaperUmbrella or ItemType.TempleIncense
                    => ItemRarity.Rare,
                ItemType.KoiDragonScale or ItemType.GoldenKintsugiJar or ItemType.SilkFan
                    => ItemRarity.Epic,
                _ => ItemRarity.Normal // tiered items get rarity from roll
            };
        }

        // ── Roll a single item slot ──

        private ItemRollSlot RollItemSlot(int stars, int classLevel)
        {
            var rarity = RollRarity(stars, classLevel);
            var type = RollItemType(rarity, classLevel);

            return new ItemRollSlot
            {
                IsNothing = false,
                IsLocked = false,
                RolledItem = new ItemInstance
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Type = type,
                    Rarity = IsTiered(type) ? rarity : GetFixedRarity(type),
                    Charges = 1
                }
            };
        }

        private ItemRarity RollRarity(int stars, int classLevel)
        {
            // Epic items only available at class Level 15+
            if (classLevel >= 15)
            {
                // ~2% at L15, rising to ~7% at L40
                var epicChance = 0.02 + (classLevel - 15) * 0.002;
                if (_random.NextDouble() < epicChance)
                    return ItemRarity.Epic;
            }

            var rareChance = 0.15 + Math.Max(0, stars - 2) * 0.05;
            return _random.NextDouble() < rareChance ? ItemRarity.Rare : ItemRarity.Normal;
        }

        private ItemType RollItemType(ItemRarity rarity, int classLevel)
        {
            // Epic rarity → chance for unique epic items
            if (rarity == ItemRarity.Epic && _random.NextDouble() < 0.35)
                return UniqueEpicItems[_random.Next(UniqueEpicItems.Length)];

            // Rare rarity → chance for unique rare items
            if (rarity == ItemRarity.Rare && _random.NextDouble() < 0.25)
                return UniqueRareItems[_random.Next(UniqueRareItems.Length)];

            // Normal rarity → chance for unique normal items
            if (rarity == ItemRarity.Normal && _random.NextDouble() < 0.20)
                return UniqueNormalItems[_random.Next(UniqueNormalItems.Length)];

            // Default: tiered items
            return TieredItems[_random.Next(TieredItems.Length)];
        }

        // ── Item effect implementations ──

        public static bool TryUseSolver(SudokuBoard board, ItemRarity rarity, int row, int col)
        {
            if (!board.IsEmpty(row, col)) return false;
            board.SetCell(row, col, board.Solution[row, col]);

            var neighborCount = rarity switch
            {
                ItemRarity.Normal => 0,
                ItemRarity.Rare => 1,
                ItemRarity.Epic => 2,
                _ => 0
            };
            FillNeighbors(board, row, col, neighborCount);
            return true;
        }

        public static List<(int Row, int Col)> UseFinder(SudokuBoard board, ItemRarity rarity, int row, int col)
        {
            var target = board.GetCell(row, col);
            var needed = rarity switch
            {
                ItemRarity.Normal => 1,
                ItemRarity.Rare => 3,
                ItemRarity.Epic => 2, // Epic trades breadth for accuracy
                _ => 1
            };

            var matches = new List<(int Row, int Col)>();
            if (target == 0) return matches;

            for (var r = 0; r < board.Size; r++)
            {
                for (var c = 0; c < board.Size; c++)
                {
                    if (r == row && c == col) continue;
                    if (!board.IsEmpty(r, c) || board.IsGiven(r, c)) continue;
                    if (board.Solution[r, c] != target) continue;

                    board.GetPencilSet(r, c).Add(target);
                    matches.Add((r, c));
                    if (matches.Count >= needed) return matches;
                }
            }
            return matches;
        }

        public static int GetInkWellAmount(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 3,
                ItemRarity.Rare => 6,
                ItemRarity.Epic => 10,
                _ => 3
            };
        }

        public static int GetMeditationStoneAmount(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 1,
                ItemRarity.Rare => 2,
                ItemRarity.Epic => 3,
                _ => 1
            };
        }

        public static int GetLanternOfClarityMoves(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 3,
                ItemRarity.Rare => 6,
                ItemRarity.Epic => 10,
                _ => 3
            };
        }

        public static int GetKoiReflectionCells(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 1,
                ItemRarity.Rare => 2,
                ItemRarity.Epic => 3,
                _ => 1
            };
        }

        public static int GetPatternScrollZones(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 1,
                ItemRarity.Rare => 2,
                ItemRarity.Epic => -1, // -1 = full conflict web
                _ => 1
            };
        }

        // ── Item descriptions ──

        public static string GetItemName(ItemType type)
        {
            return type switch
            {
                ItemType.Solver => "Solver",
                ItemType.Finder => "Finder",
                ItemType.InkWell => "Ink Well",
                ItemType.MeditationStone => "Meditation Stone",
                ItemType.WindChime => "Wind Chime",
                ItemType.PatternScroll => "Pattern Scroll",
                ItemType.KoiReflection => "Koi Reflection",
                ItemType.LanternOfClarity => "Lantern of Clarity",
                ItemType.GardenRake => "Garden Rake",
                ItemType.OfferingBowl => "Offering Bowl",
                ItemType.PruningShears => "Pruning Shears",
                ItemType.ZenSandSifter => "Zen Sand Sifter",
                ItemType.GinkgoLeaf => "Ginkgo Leaf",
                ItemType.RicePaperUmbrella => "Rice Paper Umbrella",
                ItemType.TempleIncense => "Temple Incense",
                ItemType.KoiDragonScale => "Koi Dragon Scale",
                ItemType.GoldenKintsugiJar => "Golden Kintsugi Jar",
                ItemType.SilkFan => "Silk Fan",
                _ => type.ToString()
            };
        }

        public static string GetItemDescription(ItemType type, ItemRarity rarity)
        {
            return type switch
            {
                ItemType.Solver => rarity switch
                {
                    ItemRarity.Normal => "Fill 1 correct cell.",
                    ItemRarity.Rare => "Fill 1 correct cell + 1 valid neighbor.",
                    ItemRarity.Epic => "Fill 1 correct cell + 2 valid neighbors.",
                    _ => "Fill a correct cell."
                },
                ItemType.Finder => rarity switch
                {
                    ItemRarity.Normal => "Highlight 1 matching cell.",
                    ItemRarity.Rare => "Highlight 3 matching cells.",
                    ItemRarity.Epic => "Highlight 2 matching cells (guaranteed accuracy).",
                    _ => "Highlight matching cells."
                },
                ItemType.InkWell => $"+{GetInkWellAmount(rarity)} Pencil charges.",
                ItemType.MeditationStone => $"+{GetMeditationStoneAmount(rarity)} HP.",
                ItemType.WindChime => rarity switch
                {
                    ItemRarity.Normal => "Undo 1 wrong input.",
                    ItemRarity.Rare => "Undo 1 wrong input + restore 1 HP.",
                    ItemRarity.Epic => "Undo 1 wrong input + restore 1 HP + reveal 1 correct cell.",
                    _ => "Undo a wrong input."
                },
                ItemType.PatternScroll => rarity switch
                {
                    ItemRarity.Normal => "Highlight conflicts in 1 zone.",
                    ItemRarity.Rare => "Highlight conflicts in 2 zones.",
                    ItemRarity.Epic => "Highlight full conflict web.",
                    _ => "Highlight constraint conflicts."
                },
                ItemType.KoiReflection => $"Reveal candidates for {GetKoiReflectionCells(rarity)} cell(s) (no Pencil cost).",
                ItemType.LanternOfClarity => $"Disable Fog of War for {GetLanternOfClarityMoves(rarity)} moves.",
                ItemType.GardenRake => "Highlights cells with only 2 candidates in current row/column.",
                ItemType.OfferingBowl => "Spend 5 HP to reveal the correct number for one cell.",
                ItemType.PruningShears => "Removes 1 impossible candidate from a 3x3 box.",
                ItemType.ZenSandSifter => "Highlights Hidden Pairs in the current row.",
                ItemType.GinkgoLeaf => "Highlights all instances of a chosen number until placed.",
                ItemType.RicePaperUmbrella => "Protects HP from the next 2 mistakes.",
                ItemType.TempleIncense => "Correct cells for a specific number pulse for 5 moves.",
                ItemType.KoiDragonScale => "Instantly completes the most-filled line or box.",
                ItemType.GoldenKintsugiJar => "Highlights all current mistakes on the board in red.",
                ItemType.SilkFan => "Swap the positions of two placed numbers.",
                _ => "Unknown item."
            };
        }

        // ── Shop prices ──

        public static int GetBasePrice(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Normal => 15,
                ItemRarity.Rare => 30,
                ItemRarity.Epic => 60,
                _ => 15
            };
        }

        public static int GetShopPrice(ItemRarity rarity, int floorIndex)
        {
            var basePrice = GetBasePrice(rarity);
            return (int)Math.Round(basePrice * (1f + floorIndex * 0.5f));
        }

        // ── Private helpers ──

        private static void FillNeighbors(SudokuBoard board, int row, int col, int count)
        {
            if (count <= 0) return;
            var filled = 0;
            for (var r = Math.Max(0, row - 1); r <= Math.Min(board.Size - 1, row + 1); r++)
            {
                for (var c = Math.Max(0, col - 1); c <= Math.Min(board.Size - 1, col + 1); c++)
                {
                    if ((r == row && c == col) || !board.IsEmpty(r, c)) continue;
                    board.SetCell(r, c, board.Solution[r, c]);
                    filled++;
                    if (filled >= count) return;
                }
            }
        }
    }
}
