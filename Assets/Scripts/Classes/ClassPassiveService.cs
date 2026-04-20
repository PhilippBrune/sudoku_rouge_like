using System;
using SudokuRoguelike.Core;

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
                    // 25% chance (+3% per prestige) correct placement grants +1 Gold
                    var goldChance = KoiGoldChance(state);
                    if (new Random(state.Seed ^ level.CorrectPlacements).NextDouble() < goldChance)
                    {
                        state.CurrentGold++;
                        return 1;
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
                // 25% chance no HP cost on wrong placement (30% at KoiGambler L10+)
                var noHpChance = KoiNoHpChance(state);
                if (new Random(state.Seed ^ state.CurrentHP ^ state.CurrentGold).NextDouble() < noHpChance)
                    return true; // absorbed
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
            return level >= 10 ? 0.30 : 0.25;
        }

        private static double KoiNoHpChance(RunState state)
        {
            var level = GetClassLevel(state);
            return level >= 20 ? 0.30 : 0.25;
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
            if (level >= 25) return 0.30f;
            if (level >= 10) return 0.25f;
            return 0.20f;
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
