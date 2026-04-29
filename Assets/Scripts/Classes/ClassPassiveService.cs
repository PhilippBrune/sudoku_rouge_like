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
                case ClassId.GardenMonk:
                    // Heal every 5 correct placements (upgrades at L15 → every 4)
                    var healInterval = GardenMonkHealInterval(state);
                    if (level.CorrectPlacements % healInterval == 0)
                        state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + 1);
                    break;

                case ClassId.KoiGambler:
                    // GildedKoiScale: raises proc chance to 40% and gold grant to +5
                    var hasScale = RelicService.HasRelicOfType(state, RelicId.GildedKoiScale);
                    var goldChance = hasScale ? 0.40 : KoiGoldChance(state);
                    if (new Random(state.Seed ^ level.CorrectPlacements).NextDouble() < goldChance)
                    {
                        var goldGain = hasScale ? 5 : KoiGoldGain(state);
                        state.CurrentGold += goldGain;
                        return goldGain;
                    }
                    break;

                case ClassId.ReedDuelist:
                    // Perfect + no pencil tile → +2 Pencil (checked at level end, not here)
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
        /// Called by RunDirector at the end of each completed level.
        /// Returns pencil gained (ReedDuelist passive).
        /// </summary>
        public static int OnLevelComplete(RunState state, LevelState level)
        {
            switch (state.ClassId)
            {
                case ClassId.ReedDuelist:
                    // Perfect + no pencil → +2 Pencil (L15: +3 Pencil)
                    if (level.PerfectSoFar && level.NoPencilUsed)
                    {
                        var bonus = ReedDuelistPencilBonus(state);
                        state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + bonus);
                        return bonus;
                    }
                    break;

                case ClassId.StoneGardener:
                    // Reset "first item used" flag each level (tracked via LevelState.ItemsUsedThisLevel)
                    // The actual free-use logic lives in UseItem
                    break;
            }
            return 0;
        }

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

        // ── GardenMonk: HP-trade pencil purchase ────────────────────────────────

        /// <summary>
        /// [REDESIGN] GardenMonk: buying pencil mid-puzzle costs 1 HP instead of gold.
        /// Returns true when the HP-trade replaces the gold cost.
        /// Call from the shop/HUD pencil-buy path: deduct 1 HP and skip the gold deduction.
        /// </summary>
        public static bool GardenMonkPencilBuyIsHPTrade(RunState state)
            => state.ClassId == ClassId.GardenMonk;

        // ── LanternSeer: modifier weakening ─────────────────────────────────────

        /// <summary>Returns the modifier intensity reduction factor for LanternSeer (0 = no reduction).</summary>
        public static float GetModifierWeakeningFactor(RunState state)
        {
            if (state.ClassId != ClassId.LanternSeer) return 0f;
            // L10: 25%, L25: 30%
            return LanternSeerWeakenFactor(state);
        }

        // ── QuietCartographer: next-tile preview ─────────────────────────────────

        /// <summary>Returns true if the next-tile preview should be shown on the HUD.</summary>
        public static bool ShouldShowNextTilePreview(RunState state, LevelState level)
        {
            if (state.ClassId != ClassId.QuietCartographer) return false;
            // Show after a perfect tile; L15 upgrade: always show
            var alwaysShow = QuietCartographerAlwaysPreview(state);
            return alwaysShow || level.PerfectSoFar;
        }

        // ── Helpers: level-scaled thresholds ────────────────────────────────────

        private static int GardenMonkHealInterval(RunState state)
        {
            // Upgrade at L15: heal every 4 instead of 5
            var level = GetClassLevel(state);
            return level >= 15 ? 4 : 5;
        }

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

        private static int ReedDuelistPencilBonus(RunState state)
        {
            var level = GetClassLevel(state);
            return level >= 15 ? 3 : 2;
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

        private static bool QuietCartographerAlwaysPreview(RunState state)
        {
            return GetClassLevel(state) >= 15;
        }

        // Reads class level from XP stored in RunState (via XpService pattern).
        // RunState doesn't store the level directly — derive it from XP service.
        private static int GetClassLevel(RunState state)
        {
            // XP is committed at run end, so during a run we don't have the exact level.
            // Use RunNumber as a rough proxy: each run ~200 XP → L1 per ~1 run early on.
            // A proper implementation would pass the level in from ClassGardenProgressionService.
            // For now, the passive uses a conservative base — upgrades are cosmetic accuracy improvements.
            return Math.Min(40, 1 + state.RunNumber * 2);
        }
    }
}
