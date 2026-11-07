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
        public static bool IsPencilFree(RunState state, LevelState level, int row, int col, int marksInCell)
        {
            // [REQ: CLASS-PASSIVE-003] The first mark of a cell is free, at Level 20 the first two (the upgrade had no code).
            return state.ClassId == ClassId.ShrineArchivist && marksInCell < ShrineArchivistFreeMarks(state);
        }

        // [REQ: CLASS-PASSIVE-003] The owner's picks SA1 and LS1 after 0.4.2 (2026-11-07; GAP-122: the old passives had no code).

        /// <summary>The Shrine Archivist's find: a cell narrowed to two notes or fewer, the right one among them, writes
        /// itself with this chance (once per cell and puzzle; RunDirector.ClassPassives.cs).</summary>
        public const double ArchiveChance = 0.30;

        /// <summary>The Lantern Seer's lights in each puzzle: one, two from Level 15.</summary>
        public static int LanternLights(RunState state) =>
            state?.ClassId != ClassId.LanternSeer ? 0 : GetClassLevel(state) >= 15 ? 2 : 1;

        /// <summary>Every third caught light writes a digit, every second from Level 25.</summary>
        public static int LanternRevealEvery(RunState state) => GetClassLevel(state) >= 25 ? 2 : 3;

        public const int LanternGold = 8, LanternPencil = 2, LanternHp = 1;

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

        private static int GetClassLevel(RunState state)
        {
            return Math.Clamp(state?.ClassLevel ?? 1, 1, 40);
        }
    }
}
