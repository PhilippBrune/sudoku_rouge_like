using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class RelicService
    {
        private readonly Random _random;

        // Tier weights per floor: [floor][T1, T2, T3, T4, Legendary]
        private static readonly float[][] TierWeights =
        {
            new[] { 0.50f, 0.30f, 0.15f, 0.05f, 0.00f },
            new[] { 0.30f, 0.35f, 0.25f, 0.08f, 0.02f },
            new[] { 0.15f, 0.30f, 0.30f, 0.18f, 0.07f },
            new[] { 0.05f, 0.20f, 0.30f, 0.30f, 0.15f },
            new[] { 0.00f, 0.10f, 0.25f, 0.35f, 0.30f }
        };

        private static readonly Dictionary<RelicTier, RelicId[]> TierPool = new()
        {
            [RelicTier.Tier1] = new[] { RelicId.SmoothPebble, RelicId.CrackedTeacup, RelicId.WoodenComb, RelicId.MossToken },
            [RelicTier.Tier2] = new[] { RelicId.KoiReflectionRelic, RelicId.MonkCharm, RelicId.CopperTortoise, RelicId.JadeHairpin, RelicId.StoneSundial },
            [RelicTier.Tier3] = new[] { RelicId.SakuraSeal, RelicId.CrimsonFan, RelicId.WisteriaBranch, RelicId.PaperCrane, RelicId.PorcelainMask },
            [RelicTier.Tier4] = new[] { RelicId.TransmutedSigil, RelicId.PhoenixFeather, RelicId.SpiritLantern, RelicId.MoonstoneCompass },
            [RelicTier.Legendary] = new[] { RelicId.GoldenRoot, RelicId.SilentGrid, RelicId.ShiftingGarden, RelicId.EternalLotus, RelicId.DragonsEye }
        };

        public RelicService(int seed)
        {
            _random = new Random(seed);
        }

        public RelicInstance RollRelic(int floorIndex, int tierBonus = 0)
        {
            var tier = RollTier(floorIndex + tierBonus);
            var pool = TierPool[tier];
            var id = pool[_random.Next(pool.Length)];

            return new RelicInstance
            {
                Id = id,
                Tier = tier,
                UsesRemaining = GetDefaultUses(id)
            };
        }

        /// <summary>Roll <paramref name="count"/> distinct relics for a choice panel.
        /// Returns up to <paramref name="count"/> unique relics (may be fewer if the tier pool is small).</summary>
        public List<RelicInstance> RollRelicChoices(int floorIndex, int count = 3, int tierBonus = 0)
        {
            var choices = new List<RelicInstance>(count);
            var usedIds = new HashSet<RelicId>();

            for (var attempt = 0; attempt < count * 4 && choices.Count < count; attempt++)
            {
                var tier = RollTier(floorIndex + tierBonus);
                var pool = TierPool[tier];
                var id = pool[_random.Next(pool.Length)];
                if (usedIds.Contains(id)) continue;
                usedIds.Add(id);
                choices.Add(new RelicInstance { Id = id, Tier = tier, UsesRemaining = GetDefaultUses(id) });
            }

            return choices;
        }

        public bool RollEliteRelicDrop(int floorIndex)
        {
            var chance = 0.15f + floorIndex * 0.0625f;
            return _random.NextDouble() < chance;
        }

        private RelicTier RollTier(int effectiveFloor)
        {
            var fi = Math.Clamp(effectiveFloor, 0, TierWeights.Length - 1);
            var weights = TierWeights[fi];
            var roll = (float)_random.NextDouble();
            var cumulative = 0f;

            for (var i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return (RelicTier)i;
            }

            return RelicTier.Tier1;
        }

        private static int GetDefaultUses(RelicId id)
        {
            return id switch
            {
                RelicId.CrackedTeacup => 1,
                RelicId.SilentGrid => 3,
                RelicId.PhoenixFeather => 1,
                _ => -1 // passive
            };
        }

        // ── Relic Queries ──

        public static float GetShopRerollCostMultiplier(RunState state) =>
            state.HasRelic && state.HeldRelic.Id == RelicId.MossToken ? 0.75f : 1.0f;

        public static float GetShopPriceMultiplier(RunState state) =>
            state.HasRelic && state.HeldRelic.Id == RelicId.SpiritLantern ? 0.80f : 1.0f;

        public static int GetBonusRewardSlots(RunState state) =>
            state.HasRelic && state.HeldRelic.Id == RelicId.StoneSundial ? 1 : 0;

        public static int GetRelicNodeTierBonus(RunState state) =>
            state.HasRelic && state.HeldRelic.Id == RelicId.MoonstoneCompass ? 1 : 0;

        public static bool HasInfiniteItems(RunState state) =>
            state.HasRelic && state.HeldRelic.Id == RelicId.EternalLotus;

        public static bool TryPreventDeath(RunState state)
        {
            if (!state.HasRelic || state.HeldRelic.Id != RelicId.PhoenixFeather) return false;
            if (state.HeldRelic.UsesRemaining <= 0) return false;
            state.HeldRelic.UsesRemaining--;
            state.CurrentHP = 1;
            return true;
        }

        public static bool TryAbsorbMistake(RunState state)
        {
            if (!state.HasRelic) return false;
            if (state.HeldRelic.Id == RelicId.CrackedTeacup && state.HeldRelic.UsesRemaining > 0)
            {
                state.HeldRelic.UsesRemaining--;
                return true;
            }
            return false;
        }

        // ── Name & Description ──

        public static string GetRelicName(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "Smooth Pebble",
                RelicId.CrackedTeacup => "Cracked Teacup",
                RelicId.WoodenComb => "Wooden Comb",
                RelicId.MossToken => "Moss Token",
                RelicId.KoiReflectionRelic => "Koi Reflection",
                RelicId.MonkCharm => "Monk's Charm",
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

        public static string GetIconName(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble      => "smooth_pebble",
                RelicId.CrackedTeacup     => "cracked_teacup",
                RelicId.WoodenComb        => "wooden_comb",
                RelicId.MossToken         => "moss_token",
                RelicId.KoiReflectionRelic=> "koi_reflection_relic",
                RelicId.MonkCharm         => "monk_charm",
                RelicId.CopperTortoise    => "copper_tortoise",
                RelicId.JadeHairpin       => "jade_hairpin",
                RelicId.StoneSundial      => "stone_sundial",
                RelicId.SakuraSeal        => "sakura_seal",
                RelicId.CrimsonFan        => "crimson_fan",
                RelicId.WisteriaBranch    => "wisteria_branch",
                RelicId.PaperCrane        => "paper_crane",
                RelicId.PorcelainMask     => "porcelain_mask",
                RelicId.TransmutedSigil   => "transmuted_sigil",
                RelicId.PhoenixFeather    => "phoenix_feather",
                RelicId.SpiritLantern     => "spirit_lantern",
                RelicId.MoonstoneCompass  => "moonstone_compass",
                RelicId.GoldenRoot        => "golden_root",
                RelicId.SilentGrid        => "silent_grid",
                RelicId.ShiftingGarden    => "shifting_garden",
                RelicId.EternalLotus      => "eternal_lotus",
                RelicId.DragonsEye        => "dragons_eye",
                _ => ""
            };
        }

        public static string GetRelicDescription(RelicId id)
        {
            return id switch
            {
                RelicId.SmoothPebble => "Start each puzzle with +2 pencil marks.",
                RelicId.CrackedTeacup => "Absorb the first mistake each puzzle (no HP loss).",
                RelicId.WoodenComb => "+1 Max HP.",
                RelicId.MossToken => "Shop rerolls cost 25% less.",
                RelicId.KoiReflectionRelic => "Reveal one candidate per puzzle start.",
                RelicId.MonkCharm => "5 correct placements in a row grants +2 gold.",
                RelicId.CopperTortoise => "Perfect puzzle completion grants +15 gold.",
                RelicId.JadeHairpin => "+1 item slot capacity.",
                RelicId.StoneSundial => "+1 reward slot after each puzzle.",
                RelicId.SakuraSeal => "No-hit puzzle streak: heal 1 HP after 3 puzzles.",
                RelicId.CrimsonFan => "Reduce boss modifier intensity by one step.",
                RelicId.WisteriaBranch => "Heal 2 HP at the start of each floor.",
                RelicId.PaperCrane => "First item use each puzzle is free (no charge cost).",
                RelicId.PorcelainMask => "Elite reward rarity upgraded by one tier.",
                RelicId.TransmutedSigil => "+25% gold from puzzle completion.",
                RelicId.PhoenixFeather => "Prevent death once per run (heal to 1 HP).",
                RelicId.SpiritLantern => "Shop prices reduced by 20%.",
                RelicId.MoonstoneCompass => "Relic node tier boosted by +1.",
                RelicId.GoldenRoot => "+50% gold interest at floor end.",
                RelicId.SilentGrid => "Next 3 mistakes are silent (no HP loss).",
                RelicId.ShiftingGarden => "Puzzles occasionally reshuffle given digits.",
                RelicId.EternalLotus => "Items are never consumed.",
                RelicId.DragonsEye => "See one move into the future.",
                _ => ""
            };
        }
    }
}
