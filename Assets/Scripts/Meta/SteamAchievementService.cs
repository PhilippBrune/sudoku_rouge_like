using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Meta
{
    public sealed class AchievementDefinition
    {
        public string Id;
        public string Name;
        public AchievementTier Tier;
        public bool Hidden;
    }

    /// <summary>
    /// Full 52-achievement system per MetaProgressionSystem spec.
    /// Evaluates unlock conditions after each run using RunResult + MetaProgressionState + ProfileStats.
    /// </summary>
    public sealed class SteamAchievementService
    {
        private readonly List<AchievementDefinition> _definitions = new();

        public SteamAchievementService()
        {
            SeedDefinitions();
        }

        public IReadOnlyList<AchievementDefinition> GetDefinitions() => _definitions;

        public List<string> EvaluateUnlocks(RunResult result, MetaProgressionState meta,
            ProfileStats stats, MasteryAchievementState mastery)
        {
            var unlocked = new List<string>();

            // ── Beginner (1-12) ──
            TryUnlock(meta, unlocked, "first_puzzle", result.GardenDepthReached >= 1);
            TryUnlock(meta, unlocked, "first_boss", result.ClearedBoss);
            TryUnlock(meta, unlocked, "first_floor", result.GardenDepthReached >= 1 && result.Victory);
            TryUnlock(meta, unlocked, "first_item", stats.UsedItem || result.UsedAnyItem);
            TryUnlock(meta, unlocked, "first_relic", stats.AcquiredRelic || result.AcquiredRelic);
            TryUnlock(meta, unlocked, "gold_100", result.GoldEarned >= 100);
            TryUnlock(meta, unlocked, "pencil_saver", result.ClearedStageNoPencilNoHpLoss);
            TryUnlock(meta, unlocked, "class_unlock_1", meta.UnlockedClasses.Count >= 2);
            TryUnlock(meta, unlocked, "risk_route", result.RiskRoutesChosen >= 1);
            TryUnlock(meta, unlocked, "shop_purchase", stats.BoughtFromShop || result.BoughtFromShop);
            TryUnlock(meta, unlocked, "combo_5", result.PeakCombo >= 5);
            TryUnlock(meta, unlocked, "tutorial_complete_1",
                stats.CompletedTutorialPuzzle || result.Mode == GameMode.Tutorial);

            // ── Intermediate (13-28) ──
            TryUnlock(meta, unlocked, "combo_20", result.PeakCombo >= 20);
            TryUnlock(meta, unlocked, "dual_modifier_clear", result.ClearedMultiStageBoss);
            TryUnlock(meta, unlocked, "floor_3", result.GardenDepthReached >= 3);
            TryUnlock(meta, unlocked, "floor_5", result.GardenDepthReached >= 5);
            TryUnlock(meta, unlocked, "first_victory", result.Victory && result.Mode == GameMode.GardenRun);
            TryUnlock(meta, unlocked, "gold_1000", result.GoldEarned >= 1000);
            TryUnlock(meta, unlocked, "class_unlock_4", meta.UnlockedClasses.Count >= 4);
            TryUnlock(meta, unlocked, "level_10", CheckAnyClassAtLevel(meta, 10));
            TryUnlock(meta, unlocked, "relic_swap", stats.SwappedRelic || result.SwappedRelic);
            TryUnlock(meta, unlocked, "star_4_clear", result.HighestStarCleared >= 4);
            TryUnlock(meta, unlocked, "star_5_clear", result.HighestStarCleared >= 5);
            TryUnlock(meta, unlocked, "9x9_clear", result.HighestBoardSize >= 9);
            TryUnlock(meta, unlocked, "modifier_5_seen", stats.ModifiersEncountered >= 5);
            TryUnlock(meta, unlocked, "all_sizes_played", stats.SizeStarCombosCleared >= 5);
            TryUnlock(meta, unlocked, "risk_3_floors", result.RiskRoutesChosen >= 3);
            TryUnlock(meta, unlocked, "epic_item", stats.AcquiredEpicItem || result.AcquiredEpicItem);

            // ── Advanced (29-42) ──
            TryUnlock(meta, unlocked, "perfect_boss",
                result.ClearedBoss && result.MistakesMade == 0);
            TryUnlock(meta, unlocked, "endless_depth_20",
                result.Mode == GameMode.EndlessZen && result.GardenDepthReached >= 20);
            TryUnlock(meta, unlocked, "level_20", CheckAnyClassAtLevel(meta, 20));
            TryUnlock(meta, unlocked, "level_30", CheckAnyClassAtLevel(meta, 30));
            TryUnlock(meta, unlocked, "all_modifiers_seen",
                mastery.BossClearsByModifier.Count >= 15);
            TryUnlock(meta, unlocked, "star_6_clear", result.HighestStarCleared >= 6);
            TryUnlock(meta, unlocked, "gold_5000_run", result.GoldEarned >= 5000);
            TryUnlock(meta, unlocked, "no_item_victory",
                result.Victory && !result.UsedAnyItem && result.Mode == GameMode.GardenRun);
            TryUnlock(meta, unlocked, "class_unlock_8", meta.UnlockedClasses.Count >= 8);
            TryUnlock(meta, unlocked, "triple_modifier", result.SimultaneousModifiersOnBoss >= 3);
            TryUnlock(meta, unlocked, "all_stars_all_sizes",
                stats.SizeStarCombosCleared >= 30);
            TryUnlock(meta, unlocked, "perfect_floor", result.FlawlessFloor);
            TryUnlock(meta, unlocked, "codex_50", CheckCodexPercent(meta, 0.50f));
            TryUnlock(meta, unlocked, "prestige_1", CheckAnyClassPrestige(meta, 1));

            // ── Expert (43-50) ──
            TryUnlock(meta, unlocked, "no_hp_loss_run",
                result.Victory && result.MistakesMade == 0 && result.Mode == GameMode.GardenRun);
            TryUnlock(meta, unlocked, "level_40", CheckAnyClassAtLevel(meta, 40));
            TryUnlock(meta, unlocked, "all_classes_30", CheckAllClassesAtLevel(meta, 30));
            TryUnlock(meta, unlocked, "endless_depth_50",
                result.Mode == GameMode.EndlessZen && result.GardenDepthReached >= 50);
            TryUnlock(meta, unlocked, "five_modifier_boss", result.SimultaneousModifiersOnBoss >= 5);
            TryUnlock(meta, unlocked, "full_codex", CheckCodexPercent(meta, 1.0f));
            TryUnlock(meta, unlocked, "spirit_grandmaster",
                result.Mode == GameMode.SpiritTrials && result.SpiritTrialScore >= 1500);
            TryUnlock(meta, unlocked, "prestige_5", CheckAnyClassPrestige(meta, 5));

            // ── Hidden (51-52) ──
            TryUnlock(meta, unlocked, "hidden_dual_boss", result.ClearedMultiStageBoss);
            TryUnlock(meta, unlocked, "chaos_monk_unlock",
                result.ClearedMultiStageBoss && result.WonWithOneHp);

            return unlocked;
        }

        private static bool CheckAnyClassAtLevel(MetaProgressionState meta, int level)
        {
            if (meta.GardenProgression?.ClassEntries == null) return false;
            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                var (lv, _, _) = XpService.DeriveLevel(meta.GardenProgression.ClassEntries[i].TotalXp);
                if (lv >= level) return true;
            }
            return false;
        }

        private static bool CheckAllClassesAtLevel(MetaProgressionState meta, int level)
        {
            if (meta.GardenProgression?.ClassEntries == null) return false;
            if (meta.GardenProgression.ClassEntries.Count < 8) return false;
            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                var (lv, _, _) = XpService.DeriveLevel(meta.GardenProgression.ClassEntries[i].TotalXp);
                if (lv < level) return false;
            }
            return true;
        }

        private static bool CheckAnyClassPrestige(MetaProgressionState meta, int tier)
        {
            if (meta.GardenProgression?.ClassEntries == null) return false;
            for (var i = 0; i < meta.GardenProgression.ClassEntries.Count; i++)
            {
                if (meta.GardenProgression.ClassEntries[i].PrestigeTier >= tier) return true;
            }
            return false;
        }

        private static bool CheckCodexPercent(MetaProgressionState meta, float threshold)
        {
            if (meta.ItemCodex?.Entries == null || meta.ItemCodex.Entries.Count == 0) return false;
            var discovered = 0;
            for (var i = 0; i < meta.ItemCodex.Entries.Count; i++)
            {
                if (meta.ItemCodex.Entries[i].Discovered) discovered++;
            }
            return (float)discovered / meta.ItemCodex.Entries.Count >= threshold;
        }

        private static void TryUnlock(MetaProgressionState meta, List<string> unlocked, string id, bool condition)
        {
            if (!condition || meta.UnlockedAchievements.Contains(id))
                return;

            meta.UnlockedAchievements.Add(id);
            unlocked.Add(id);
        }

        private void SeedDefinitions()
        {
            // Beginner (1-12)
            Add("first_puzzle", "First Steps", AchievementTier.Beginner);
            Add("first_boss", "Garden Guardian", AchievementTier.Beginner);
            Add("first_floor", "Bamboo Gate", AchievementTier.Beginner);
            Add("first_item", "Tool of the Trade", AchievementTier.Beginner);
            Add("first_relic", "Keepsake", AchievementTier.Beginner);
            Add("gold_100", "Pocket Change", AchievementTier.Beginner);
            Add("pencil_saver", "Ink Miser", AchievementTier.Beginner);
            Add("class_unlock_1", "New Perspective", AchievementTier.Beginner);
            Add("risk_route", "Brave Path", AchievementTier.Beginner);
            Add("shop_purchase", "Window Shopping", AchievementTier.Beginner);
            Add("combo_5", "Getting Started", AchievementTier.Beginner);
            Add("tutorial_complete_1", "Student", AchievementTier.Beginner);

            // Intermediate (13-28)
            Add("combo_20", "Flow State", AchievementTier.Intermediate);
            Add("dual_modifier_clear", "Double Trouble", AchievementTier.Intermediate);
            Add("floor_3", "Deep Garden", AchievementTier.Intermediate);
            Add("floor_5", "Summit Seeker", AchievementTier.Intermediate);
            Add("first_victory", "Garden Cleared", AchievementTier.Intermediate);
            Add("gold_1000", "Prosperous", AchievementTier.Intermediate);
            Add("class_unlock_4", "Versatile", AchievementTier.Intermediate);
            Add("level_10", "Apprentice", AchievementTier.Intermediate);
            Add("relic_swap", "Hard Choice", AchievementTier.Intermediate);
            Add("star_4_clear", "Stargazer", AchievementTier.Intermediate);
            Add("star_5_clear", "Constellation", AchievementTier.Intermediate);
            Add("9x9_clear", "Full Grid", AchievementTier.Intermediate);
            Add("modifier_5_seen", "Well Traveled", AchievementTier.Intermediate);
            Add("all_sizes_played", "Size Explorer", AchievementTier.Intermediate);
            Add("risk_3_floors", "Daredevil", AchievementTier.Intermediate);
            Add("epic_item", "Purple Glow", AchievementTier.Intermediate);

            // Advanced (29-42)
            Add("perfect_boss", "Flawless Victory", AchievementTier.Advanced);
            Add("endless_depth_20", "Deep Meditation", AchievementTier.Advanced);
            Add("level_20", "Journeyman", AchievementTier.Advanced);
            Add("level_30", "Master", AchievementTier.Advanced);
            Add("all_modifiers_seen", "Modifier Scholar", AchievementTier.Advanced);
            Add("star_6_clear", "Void Walker", AchievementTier.Advanced);
            Add("gold_5000_run", "Golden Garden", AchievementTier.Advanced);
            Add("no_item_victory", "Purist", AchievementTier.Advanced);
            Add("class_unlock_8", "Full Roster", AchievementTier.Advanced);
            Add("triple_modifier", "Triple Threat", AchievementTier.Advanced);
            Add("all_stars_all_sizes", "Completionist", AchievementTier.Advanced);
            Add("perfect_floor", "Flawless Floor", AchievementTier.Advanced);
            Add("codex_50", "Collector", AchievementTier.Advanced);
            Add("prestige_1", "Reborn", AchievementTier.Advanced);

            // Expert (43-50)
            Add("no_hp_loss_run", "Untouched", AchievementTier.Expert);
            Add("level_40", "Enlightened", AchievementTier.Expert);
            Add("all_classes_30", "Garden Council", AchievementTier.Expert);
            Add("endless_depth_50", "Eternal Patience", AchievementTier.Expert);
            Add("five_modifier_boss", "Quintuple", AchievementTier.Expert);
            Add("full_codex", "Encyclopedist", AchievementTier.Expert);
            Add("spirit_grandmaster", "Trial Champion", AchievementTier.Expert);
            Add("prestige_5", "Ascended", AchievementTier.Expert);

            // Hidden (51-52)
            Add("hidden_dual_boss", "Inner Harmony", AchievementTier.Hidden, hidden: true);
            Add("chaos_monk_unlock", "Last Stand", AchievementTier.Hidden, hidden: true);
        }

        private void Add(string id, string name, AchievementTier tier, bool hidden = false)
        {
            _definitions.Add(new AchievementDefinition
            {
                Id = id,
                Name = name,
                Tier = tier,
                Hidden = hidden
            });
        }
    }
}
