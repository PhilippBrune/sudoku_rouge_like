using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class AchievementDefinition
    {
        public string Id;
        public string Name;
        public AchievementTier Tier;
        public bool Hidden;
    }

    public sealed class SteamAchievementService
    {
        private readonly List<AchievementDefinition> _definitions = new();

        public SteamAchievementService()
        {
            SeedDefinitions();
        }

        public IReadOnlyList<AchievementDefinition> GetDefinitions() => _definitions;

        public List<string> EvaluateUnlocks(RunResult result, MetaProgressionState meta)
        {
            var unlocked = new List<string>();

            TryUnlock(meta, unlocked, "first_puzzle", result.GardenDepthReached >= 1);
            TryUnlock(meta, unlocked, "first_boss", result.ClearedBoss);
            TryUnlock(meta, unlocked, "combo_20", result.PeakCombo >= 20);
            TryUnlock(meta, unlocked, "dual_modifier_clear", result.ClearedMultiStageBoss);
            TryUnlock(meta, unlocked, "perfect_boss", result.ClearedBoss && result.PerfectClear);
            TryUnlock(meta, unlocked, "heat_master", result.PeakHeatScore >= 6.0f);
            TryUnlock(meta, unlocked, "endless_depth_20", result.Mode == GameMode.EndlessZen && result.GardenDepthReached >= 20);
            TryUnlock(meta, unlocked, "no_hp_loss_run", result.Victory && !result.WonWithUnderThreeHp && result.MistakesMade == 0);
            TryUnlock(meta, unlocked, "hidden_dual_boss", result.ClearedMultiStageBoss);
            TryUnlock(meta, unlocked, "chaos_monk_unlock", result.ClearedMultiStageBoss && result.WonWithOneHp);

            return unlocked;
        }

        private static void TryUnlock(MetaProgressionState meta, List<string> unlocked, string id, bool condition)
        {
            if (!condition || meta.UnlockedAchievements.Contains(id))
            {
                return;
            }

            meta.UnlockedAchievements.Add(id);
            unlocked.Add(id);
        }

        private void SeedDefinitions()
        {
            for (var i = 1; i <= 52; i++)
            {
                _definitions.Add(new AchievementDefinition
                {
                    Id = $"ach_{i:00}",
                    Name = $"Achievement {i}",
                    Tier = i <= 12 ? AchievementTier.Beginner : i <= 28 ? AchievementTier.Intermediate : i <= 42 ? AchievementTier.Advanced : i <= 50 ? AchievementTier.Expert : AchievementTier.Hidden,
                    Hidden = i > 50
                });
            }

            _definitions.Add(new AchievementDefinition { Id = "first_puzzle", Name = "First Puzzle", Tier = AchievementTier.Beginner, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "first_boss", Name = "First Boss", Tier = AchievementTier.Beginner, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "combo_20", Name = "20 Streak", Tier = AchievementTier.Intermediate, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "dual_modifier_clear", Name = "Dual Modifier Clear", Tier = AchievementTier.Intermediate, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "perfect_boss", Name = "Perfect Boss", Tier = AchievementTier.Advanced, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "heat_master", Name = "Max Heat", Tier = AchievementTier.Expert, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "endless_depth_20", Name = "Depth 20 Endless", Tier = AchievementTier.Expert, Hidden = false });
            _definitions.Add(new AchievementDefinition { Id = "no_hp_loss_run", Name = "No HP Loss Run", Tier = AchievementTier.Hidden, Hidden = true });
            _definitions.Add(new AchievementDefinition { Id = "hidden_dual_boss", Name = "Hidden Dual Boss", Tier = AchievementTier.Hidden, Hidden = true });
            _definitions.Add(new AchievementDefinition { Id = "chaos_monk_unlock", Name = "Chaos Monk", Tier = AchievementTier.Hidden, Hidden = true });
        }
    }
}
