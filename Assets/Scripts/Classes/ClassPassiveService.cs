using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Classes
{
    /// <summary>
    /// Applies class-specific passive effects at the correct game hooks.
    /// Call the appropriate method from RunDirector at each event point.
    /// </summary>
    public static class ClassPassiveService
    {
        // ── On correct placement ─────────────────────────────────────────────────

        /// <summary>Called by RunDirector after every correct digit placement.</summary>
        /// <returns>Gold gained by KoiGambler passive (0 if none).</returns>
        public static int OnCorrectPlacement(RunState state, LevelState level)
        {
            switch (state.ClassId)
            {
                case ClassId.KoiGambler:
                    // GildedKoiScale: raises proc chance to 40% and gold grant to +5
                    var hasScale = RelicService.HasRelicOfType(state, RelicId.GildedKoiScale);
                    var goldChance = hasScale ? 0.40 : KoiGoldChance(state);
                    if (new Random(state.Seed ^ level.CorrectPlacements).NextDouble() < goldChance)
                    {
                        var goldGain = hasScale ? 5 : KoiGoldGain(state);
                        state.GainGold(goldGain);
                        return goldGain;
                    }
                    break;

            }
            return 0;
        }

        // ── On wrong placement (before HP loss) ─────────────────────────────────

        /// <summary>
        /// Called before HP is deducted. Returns true if the passive absorbs the HP loss entirely.
        /// </summary>
        public static bool OnMistake(RunState state)
        {
            if (state.ClassId == ClassId.KoiGambler)
            {
                // GildedKoiScale: 40% no-HP chance on wrong placement; also gives +1 Reroll Token as consolation
                var hasScale = RelicService.HasRelicOfType(state, RelicId.GildedKoiScale);
                var noHpChance = hasScale ? 0.40 : KoiNoHpChance(state);
                if (new Random(state.Seed ^ state.CurrentHP ^ state.CurrentGold).NextDouble() < noHpChance)
                {
                    if (hasScale) state.RerollTokens++; // consolation token on bad-proc absorb
                    return true; // absorbed
                }
            }
            return false;
        }

        // ── On pencil mark added ─────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the pencil mark should cost 0 (ShrineArchivist: first mark per cell free).
        /// </summary>
        public static bool IsPencilFree(RunState state, LevelState level, int row, int col, bool cellAlreadyHasMark)
        {
            if (state.ClassId != ClassId.ShrineArchivist) return false;
            // Free on first mark per cell; upgrades at L20 → first 2 marks per cell free
            var freeMarks = ShrineArchivistFreeMarks(state);
            return !cellAlreadyHasMark; // first mark is always free; L20 upgrade tracked separately
        }

        /// <summary>
        /// [REDESIGN] ShrineArchivist: each pencil mark has a 25% chance to auto-reveal the correct digit.
        /// Call after a pencil mark is placed. Returns true if the cell should be auto-filled with its solution value.
        /// </summary>
        public static bool TryArchivistReveal(RunState state, LevelState level, int row, int col)
        {
            if (state.ClassId != ClassId.ShrineArchivist) return false;
            // 25% chance; seeded so reveals are deterministic on resume
            return new Random(state.Seed ^ row * 100 + col ^ level.PencilMarksUsed).NextDouble() < 0.25;
        }

        // ── On level complete ────────────────────────────────────────────────────

        /// <summary>
        /// [REQ: CLASS-PASSIVE-002] Called by RunDirector at the end of each completed level, after the puzzle's gold.
        /// Playtest 0.4.1 (owner's picks M2, P-NF, P-RD, P-QC, 2026-11-05); GAP-120: three of these did nothing before.
        /// </summary>
        public static void OnLevelComplete(RunState state, LevelState level, int puzzleGold)
        {
            var clean = level.Mistakes == 0;
            switch (state.ClassId)
            {
                case ClassId.NumberFreak:   // no mistake: +1 Reroll Token and +10 gold
                    if (!clean) break;
                    state.RerollTokens++;
                    state.GainGold(NumberFreakGold);
                    break;
                case ClassId.GardenMonk:    // no mistake: +2 HP (Level 15: +3)
                    if (clean) state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + GardenMonkHeal(state));
                    break;
                case ClassId.ReedDuelist:   // no pencil mark: +50 % of the puzzle's gold and +1 HP (Level 15: +2)
                    if (!level.NoPencilUsed) break;
                    state.GainGold(puzzleGold / 2);
                    state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + (GetClassLevel(state) >= 15 ? 2 : 1));
                    break;
                case ClassId.QuietCartographer:   // a perfect puzzle eases the next one
                    state.CartographerEase = level.PerfectSoFar && clean;
                    break;
            }
        }

        public const int NumberFreakGold = 10;

        /// <summary>[REQ: CLASS-PASSIVE-002] The Quiet Cartographer's eased puzzle: its share of empty cells lies half a star
        /// (Level 15: a whole star) lower; nothing for any other class or puzzle.</summary>
        public static float EasedMissingPercent(RunState state, int stars, float missingPercent)
        {
            if (state?.ClassId != ClassId.QuietCartographer || !state.CartographerEase || stars <= 1) return missingPercent;
            var easier = StarDensityService.MissingPercentForStars(stars - 1);
            return GetClassLevel(state) >= 15 ? easier : (missingPercent + easier) / 2f;
        }

        /// <summary>The Quiet Cartographer sees every tile's size and stars from the floor's start.</summary>
        public static bool RevealsFloor(RunState state) => state?.ClassId == ClassId.QuietCartographer;

        // ── On item use ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the item use should not consume a charge (StoneGardener: first item/level free;
        /// PaperCrane relic also covers this — checked separately).
        /// </summary>
        public static bool IsItemUseFree(RunState state, LevelState level)
        {
            if (state.ClassId != ClassId.StoneGardener) return false;
            // First item each level is free; L20 upgrade: first 2 items free
            var freeUses = StoneGardenerFreeItems(state);
            return level.ItemsUsedThisLevel < freeUses;
        }

        // ── GardenMonk: pencil purchase restriction ─────────────────────────────

        /// <summary>
        /// [REQ: REQ-ECON-100] Garden Monk cannot buy pencil mid-level (GDS §6.1); every other class can.
        /// </summary>
        public static bool CanBuyPencilMidLevel(RunState state)
            => state.ClassId != ClassId.GardenMonk;

        // ── LanternSeer: modifier weakening ─────────────────────────────────────

        /// <summary>Returns the modifier intensity reduction factor for LanternSeer (0 = no reduction).</summary>
        public static float GetModifierWeakeningFactor(RunState state)
        {
            if (state.ClassId != ClassId.LanternSeer) return 0f;
            // L10: 25%, L25: 30%
            return LanternSeerWeakenFactor(state);
        }

        // ── Helpers: level-scaled thresholds ────────────────────────────────────

        public static int GardenMonkHeal(RunState state) => GetClassLevel(state) >= 15 ? 3 : 2;

        private static double KoiGoldChance(RunState state)
        {
            var level = GetClassLevel(state);
            // [REDESIGN] Base 33%; L10 upgrade kept for parity with old milestone
            return level >= 10 ? 0.35 : 0.33;
        }

        private static int KoiGoldGain(RunState state) => 3; // [REDESIGN] +3 gold (was +1)

        private static double KoiNoHpChance(RunState state)
        {
            var level = GetClassLevel(state);
            // [REDESIGN] Base 33%; L20 upgrade kept for parity
            return level >= 20 ? 0.35 : 0.33;
        }

        private static int ShrineArchivistFreeMarks(RunState state)
        {
            var level = GetClassLevel(state);
            return level >= 20 ? 2 : 1;
        }

        private static int StoneGardenerFreeItems(RunState state)
        {
            var level = GetClassLevel(state);
            return level >= 20 ? 2 : 1;
        }

        private static float LanternSeerWeakenFactor(RunState state)
        {
            var level = GetClassLevel(state);
            // [REDESIGN] Base 30% (was 20%); L10: 35%; L25: 40%
            if (level >= 25) return 0.40f;
            if (level >= 10) return 0.35f;
            return 0.30f;
        }

        private static int GetClassLevel(RunState state)
        {
            return Math.Clamp(state?.ClassLevel ?? 1, 1, 40);
        }
    }
}
