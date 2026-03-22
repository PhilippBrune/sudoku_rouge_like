using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class RelicService
    {
        private readonly Random _random;

        public RelicService(int seed)
        {
            _random = new Random(seed);
        }

        // ── Tier weights by floor (Tier1, Tier2, Tier3, Tier4, Legendary) ──

        private static readonly int[][] FloorWeights =
        {
            new[] { 60, 30, 10,  0,  0 }, // Floor 1
            new[] { 40, 35, 20,  5,  0 }, // Floor 2
            new[] { 20, 35, 30, 13,  2 }, // Floor 3
            new[] { 10, 25, 35, 25,  5 }, // Floor 4
            new[] {  5, 15, 35, 35, 10 }, // Floor 5
        };

        // All relics grouped by tier for rolling
        private static readonly RelicId[] Tier1Relics = { RelicId.SmoothPebble, RelicId.CrackedTeacup, RelicId.WoodenComb, RelicId.MossToken };
        private static readonly RelicId[] Tier2Relics = { RelicId.KoiReflectionRelic, RelicId.MonkCharm, RelicId.CopperTortoise, RelicId.JadeHairpin, RelicId.StoneSundial };
        private static readonly RelicId[] Tier3Relics = { RelicId.SakuraSeal, RelicId.CrimsonFan, RelicId.WisteriaBranch, RelicId.PaperCrane, RelicId.PorcelainMask };
        private static readonly RelicId[] Tier4Relics = { RelicId.TransmutedSigil, RelicId.PhoenixFeather, RelicId.SpiritLantern, RelicId.MoonstoneCompass };
        private static readonly RelicId[] LegendaryRelics = { RelicId.GoldenRoot, RelicId.SilentGrid, RelicId.ShiftingGarden, RelicId.EternalLotus, RelicId.DragonsEye };

        /// <summary>Roll a random relic based on floor and risk route.</summary>
        public RelicInstance RollRelic(int floor, bool isRiskRoute)
        {
            var effectiveFloor = isRiskRoute ? Math.Min(floor + 1, 5) : floor;
            var tier = RollTier(effectiveFloor);
            var id = PickRelicFromTier(tier);
            return new RelicInstance
            {
                Id = id,
                Tier = tier,
                UsesRemaining = GetInitialUses(id)
            };
        }

        /// <summary>Check if elite tile drops a relic this floor.</summary>
        public bool RollEliteRelicDrop(int floor)
        {
            var chance = floor switch
            {
                1 => 0.15,
                2 => 0.20,
                3 => 0.25,
                4 => 0.30,
                _ => 0.40
            };
            return _random.NextDouble() < chance;
        }

        /// <summary>Offer a relic to the player. Returns true if choice screen needed.</summary>
        public bool OfferRelic(RunState runState, RelicInstance offered)
        {
            if (!runState.HasRelic)
            {
                AcceptRelic(runState, offered);
                return false; // auto-accept, no choice needed
            }
            return true; // caller should show choice screen
        }

        /// <summary>Accept a relic into the single slot (replaces any existing).</summary>
        public void AcceptRelic(RunState runState, RelicInstance relic)
        {
            runState.HasRelic = true;
            runState.HeldRelic = relic;
            ApplyOnAcquire(runState, relic);
        }

        /// <summary>Apply one-time effects when a relic is first acquired.</summary>
        private static void ApplyOnAcquire(RunState runState, RelicInstance relic)
        {
            switch (relic.Id)
            {
                case RelicId.TransmutedSigil:
                    runState.MaxHP += 1;
                    runState.CurrentHP += 1;
                    break;
                case RelicId.WisteriaBranch:
                    runState.CurrentHP = Math.Min(runState.CurrentHP + 2, runState.MaxHP + 2);
                    runState.MaxHP += 2;
                    break;
                case RelicId.EternalLotus:
                    for (var i = 0; i < runState.Inventory.Count; i++)
                        runState.Inventory[i].IsInfinite = true;
                    break;
                case RelicId.PaperCrane:
                    runState.PaperCraneUsesRemaining = 2;
                    break;
                case RelicId.PhoenixFeather:
                    runState.PhoenixFeatherUsed = false;
                    break;
            }
        }

        /// <summary>Called at the start of each puzzle — applies per-puzzle relic effects.</summary>
        public static void ApplyPuzzleStart(RunState runState)
        {
            if (!runState.HasRelic) return;
            var relic = runState.HeldRelic;

            // Reset per-puzzle state
            runState.CrackedTeacupUsedThisPuzzle = false;
            runState.MonkCharmStreakCount = 0;
            runState.DragonsEyeUsedThisFloor = false; // reset per floor is handled elsewhere

            switch (relic.Id)
            {
                case RelicId.SmoothPebble:
                    runState.CurrentPencil = Math.Min(runState.CurrentPencil + 5, runState.MaxPencil + 5);
                    if (runState.CurrentPencil > runState.MaxPencil)
                        runState.MaxPencil = runState.CurrentPencil;
                    break;
                case RelicId.SilentGrid:
                    runState.MistakeShieldCharges += 2;
                    break;
                case RelicId.SakuraSeal:
                    if (runState.SakuraSealPerfectLastPuzzle)
                    {
                        runState.CurrentHP = Math.Min(runState.CurrentHP + 3, runState.MaxHP);
                        runState.SakuraSealPerfectLastPuzzle = false;
                    }
                    break;
            }
        }

        /// <summary>Called at the start of each floor — applies per-floor relic effects.</summary>
        public static void ApplyFloorStart(RunState runState)
        {
            if (!runState.HasRelic) return;

            runState.DragonsEyeUsedThisFloor = false;

            switch (runState.HeldRelic.Id)
            {
                case RelicId.WisteriaBranch:
                    runState.CurrentHP = Math.Min(runState.CurrentHP + 2, runState.MaxHP);
                    break;
            }
        }

        /// <summary>Called when a puzzle is completed — applies completion relic effects.</summary>
        public static void ApplyPuzzleComplete(RunState runState, bool perfectClear)
        {
            if (!runState.HasRelic) return;

            switch (runState.HeldRelic.Id)
            {
                case RelicId.TransmutedSigil:
                    runState.CurrentGold += 3;
                    break;
                case RelicId.CopperTortoise:
                    if (perfectClear) runState.CurrentGold += 5;
                    break;
                case RelicId.SakuraSeal:
                    runState.SakuraSealPerfectLastPuzzle = perfectClear;
                    break;
            }
        }

        /// <summary>Called on a wrong placement — returns true if the mistake was absorbed (no HP loss).</summary>
        public static bool TryAbsorbMistake(RunState runState)
        {
            if (!runState.HasRelic) return false;

            switch (runState.HeldRelic.Id)
            {
                case RelicId.CrackedTeacup:
                    if (!runState.CrackedTeacupUsedThisPuzzle)
                    {
                        runState.CrackedTeacupUsedThisPuzzle = true;
                        return true;
                    }
                    break;
            }

            // Umbrella shield (from Rice Paper Umbrella item, tracked on RunState)
            if (runState.UmbrellaShieldCharges > 0)
            {
                runState.UmbrellaShieldCharges--;
                return true;
            }

            return false;
        }

        /// <summary>Called when HP reaches 0 — returns true if death was prevented.</summary>
        public static bool TryPreventDeath(RunState runState)
        {
            if (!runState.HasRelic) return false;
            if (runState.HeldRelic.Id != RelicId.PhoenixFeather) return false;
            if (runState.PhoenixFeatherUsed) return false;

            runState.PhoenixFeatherUsed = true;
            runState.CurrentHP = runState.MaxHP;
            runState.CurrentPencil = runState.MaxPencil;
            return true;
        }

        /// <summary>Get the shop reroll cost multiplier from relic.</summary>
        public static float GetShopRerollCostMultiplier(RunState runState)
        {
            if (!runState.HasRelic) return 1f;
            return runState.HeldRelic.Id == RelicId.MossToken ? 0.75f : 1f;
        }

        /// <summary>Get the shop price multiplier from relic.</summary>
        public static float GetShopPriceMultiplier(RunState runState)
        {
            if (!runState.HasRelic) return 1f;
            return runState.HeldRelic.Id == RelicId.SpiritLantern ? 0.80f : 1f;
        }

        /// <summary>Get boss modifier intensity reduction from relic (0 = no reduction, 1 = one step down).</summary>
        public static int GetModifierIntensityReduction(RunState runState)
        {
            if (!runState.HasRelic) return 0;
            return runState.HeldRelic.Id == RelicId.CrimsonFan ? 1 : 0;
        }

        /// <summary>Get extra reward slots from relic.</summary>
        public static int GetBonusRewardSlots(RunState runState)
        {
            if (!runState.HasRelic) return 0;
            return runState.HeldRelic.Id == RelicId.StoneSundial ? 1 : 0;
        }

        /// <summary>Get relic tier upgrade for relic nodes (Moonstone Compass).</summary>
        public static int GetRelicNodeTierBonus(RunState runState)
        {
            if (!runState.HasRelic) return 0;
            return runState.HeldRelic.Id == RelicId.MoonstoneCompass ? 1 : 0;
        }

        /// <summary>Get elite item rarity upgrade (Porcelain Mask).</summary>
        public static bool GetEliteRarityUpgrade(RunState runState)
        {
            if (!runState.HasRelic) return false;
            return runState.HeldRelic.Id == RelicId.PorcelainMask;
        }

        /// <summary>Check if items should have infinite uses (Eternal Lotus).</summary>
        public static bool HasInfiniteItems(RunState runState)
        {
            if (!runState.HasRelic) return false;
            return runState.HeldRelic.Id == RelicId.EternalLotus;
        }

        /// <summary>Apply gold interest between stages (Golden Root: 50% interest).</summary>
        public static void ApplyGoldInterest(RunState runState)
        {
            if (!runState.HasRelic) return;
            if (runState.HeldRelic.Id == RelicId.GoldenRoot)
            {
                var interest = runState.CurrentGold / 2;
                runState.CurrentGold += interest;
            }
        }

        /// <summary>Called on each correct placement for streak tracking (Monk Charm).</summary>
        public static int OnCorrectPlacement(RunState runState)
        {
            if (!runState.HasRelic) return 0;
            if (runState.HeldRelic.Id != RelicId.MonkCharm) return 0;

            runState.MonkCharmStreakCount++;
            if (runState.MonkCharmStreakCount >= 5)
            {
                runState.MonkCharmStreakCount = 0;
                runState.CurrentGold += 2;
                return 2; // gold earned
            }
            return 0;
        }

        /// <summary>Called on a wrong placement to reset streak (Monk Charm).</summary>
        public static void OnWrongPlacement(RunState runState)
        {
            runState.MonkCharmStreakCount = 0;
        }

        // ── Tier info ──

        public static RelicTier GetTier(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble or RelicId.CrackedTeacup or RelicId.WoodenComb or RelicId.MossToken
                    => RelicTier.Tier1,
                RelicId.KoiReflectionRelic or RelicId.MonkCharm or RelicId.CopperTortoise or RelicId.JadeHairpin or RelicId.StoneSundial
                    => RelicTier.Tier2,
                RelicId.SakuraSeal or RelicId.CrimsonFan or RelicId.WisteriaBranch or RelicId.PaperCrane or RelicId.PorcelainMask
                    => RelicTier.Tier3,
                RelicId.TransmutedSigil or RelicId.PhoenixFeather or RelicId.SpiritLantern or RelicId.MoonstoneCompass
                    => RelicTier.Tier4,
                _ => RelicTier.Legendary
            };
        }

        public static string GetName(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "Smooth Pebble",
                RelicId.CrackedTeacup => "Cracked Teacup",
                RelicId.WoodenComb => "Wooden Comb",
                RelicId.MossToken => "Moss Token",
                RelicId.KoiReflectionRelic => "Koi Reflection",
                RelicId.MonkCharm => "Monk Charm",
                RelicId.CopperTortoise => "Copper Tortoise",
                RelicId.JadeHairpin => "Jade Hairpin",
                RelicId.StoneSundial => "Stone Sundial",
                RelicId.SakuraSeal => "Sakura Seal",
                RelicId.CrimsonFan => "Crimson Fan",
                RelicId.WisteriaBranch => "Wisteria Branch",
                RelicId.PaperCrane => "Paper Crane",
                RelicId.PorcelainMask => "Porcelain Mask",
                RelicId.TransmutedSigil => "Transmuted Sigil",
                RelicId.PhoenixFeather => "Phoenix Feather",
                RelicId.SpiritLantern => "Spirit Lantern",
                RelicId.MoonstoneCompass => "Moonstone Compass",
                RelicId.GoldenRoot => "Golden Root",
                RelicId.SilentGrid => "Silent Grid",
                RelicId.ShiftingGarden => "Shifting Garden",
                RelicId.EternalLotus => "Eternal Lotus",
                RelicId.DragonsEye => "Dragon's Eye",
                _ => id.ToString()
            };
        }

        public static string GetDescription(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "+5 Pencil at the start of each puzzle.",
                RelicId.CrackedTeacup => "First wrong placement each puzzle costs 0 HP.",
                RelicId.WoodenComb => "Nothing-slot gold bonus doubled.",
                RelicId.MossToken => "Shop reroll costs reduced by 25%.",
                RelicId.KoiReflectionRelic => "+1 combo grace (combo doesn't break on first pause).",
                RelicId.MonkCharm => "Every 5 correct placements in a row grants +2 Gold.",
                RelicId.CopperTortoise => "+5 Gold bonus when completing a puzzle with no mistakes.",
                RelicId.JadeHairpin => "Reveal all candidates in one row for free (1 use/puzzle).",
                RelicId.StoneSundial => "+1 reward slot on puzzle completion.",
                RelicId.SakuraSeal => "After a perfect puzzle, next puzzle starts with +3 HP.",
                RelicId.CrimsonFan => "Boss modifier intensity reduced by one step.",
                RelicId.WisteriaBranch => "+2 HP at the start of each floor.",
                RelicId.PaperCrane => "Skip one puzzle tile on the path (2 uses/run).",
                RelicId.PorcelainMask => "Elite tiles drop items one rarity tier higher.",
                RelicId.TransmutedSigil => "+1 Max HP, +3 Gold per puzzle completed.",
                RelicId.PhoenixFeather => "When HP reaches 0, prevent death: fully restore HP and Pencil.",
                RelicId.SpiritLantern => "All shop items cost 20% less gold.",
                RelicId.MoonstoneCompass => "Relic node tiles always offer one tier higher than floor default.",
                RelicId.GoldenRoot => "Gold earned carries 50% interest between stages.",
                RelicId.SilentGrid => "+2 mistake shield charges per puzzle.",
                RelicId.ShiftingGarden => "Extra branching tile added to each floor.",
                RelicId.EternalLotus => "All items have infinite uses for the rest of the run.",
                RelicId.DragonsEye => "Reveal the full solution for 3 seconds (1 use/floor).",
                _ => "Unknown relic."
            };
        }

        public static int GetBasePrice(RelicTier tier)
        {
            return tier switch
            {
                RelicTier.Tier1 => 25,
                RelicTier.Tier2 => 45,
                RelicTier.Tier3 => 75,
                RelicTier.Tier4 => 120,
                RelicTier.Legendary => 200,
                _ => 25
            };
        }

        // ── Private helpers ──

        private RelicTier RollTier(int floor)
        {
            var floorIndex = Math.Clamp(floor - 1, 0, FloorWeights.Length - 1);
            var weights = FloorWeights[floorIndex];
            var total = 0;
            for (var i = 0; i < weights.Length; i++) total += weights[i];

            var roll = _random.Next(total);
            var cumulative = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return (RelicTier)i;
            }
            return RelicTier.Tier1;
        }

        private RelicId PickRelicFromTier(RelicTier tier)
        {
            var pool = tier switch
            {
                RelicTier.Tier1 => Tier1Relics,
                RelicTier.Tier2 => Tier2Relics,
                RelicTier.Tier3 => Tier3Relics,
                RelicTier.Tier4 => Tier4Relics,
                RelicTier.Legendary => LegendaryRelics,
                _ => Tier1Relics
            };
            return pool[_random.Next(pool.Length)];
        }

        private static int GetInitialUses(RelicId id)
        {
            return id switch
            {
                RelicId.PaperCrane => 2,
                RelicId.PhoenixFeather => 1,
                RelicId.DragonsEye => 1,
                RelicId.JadeHairpin => 1,
                _ => -1 // passive
            };
        }
    }
}
