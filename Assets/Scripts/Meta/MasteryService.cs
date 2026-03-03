using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class MasteryService
    {
        public void RecordBossClear(MasteryAchievementState mastery, BossModifierId modifier, int stars, bool noHpLoss, bool dualModifier)
        {
            if (!mastery.BossClearsByModifier.Contains(modifier))
            {
                mastery.BossClearsByModifier.Add(modifier);
            }

            if (noHpLoss && !mastery.PerfectBossClearsByModifier.Contains(modifier))
            {
                mastery.PerfectBossClearsByModifier.Add(modifier);
            }

            if (dualModifier)
            {
                mastery.DualModifierClears++;
            }
        }

        public void RecordNineByNineFiveStarClear(MasteryAchievementState mastery)
        {
            mastery.NineByNineFiveStarClears++;
        }

        public void RecordNoItemRun(MasteryAchievementState mastery)
        {
            mastery.NoItemRuns++;
        }

        public List<ModifierMasteryEntry> BuildModifierBadges(MasteryAchievementState mastery)
        {
            var output = new List<ModifierMasteryEntry>();
            var all = (BossModifierId[])System.Enum.GetValues(typeof(BossModifierId));

            for (var i = 0; i < all.Length; i++)
            {
                var badge = ModifierBadgeTier.None;
                if (mastery.BossClearsByModifier.Contains(all[i]))
                {
                    badge = ModifierBadgeTier.Bronze;
                }

                if (mastery.PerfectBossClearsByModifier.Contains(all[i]))
                {
                    badge = ModifierBadgeTier.Gold;
                }

                if (mastery.DualModifierClears > 0 && mastery.BossClearsByModifier.Contains(all[i]))
                {
                    badge = ModifierBadgeTier.Spirit;
                }

                output.Add(new ModifierMasteryEntry
                {
                    Modifier = all[i],
                    BadgeTier = badge
                });
            }

            return output;
        }
    }
}
