using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class MasteryService
    {
        public void RecordModifierClear(MasteryAchievementState state, BossModifierId modifier,
            int stars, bool noHpLoss)
        {
            if (state == null) return;

            var record = FindOrCreateRecord(state, modifier);
            record.ClearCount++;
            if (stars >= 4) record.HighStarClears++;
            if (noHpLoss) record.NoHpLossClears++;
        }

        public int GetClearCount(MasteryAchievementState state, BossModifierId modifier)
        {
            var record = FindRecord(state, modifier);
            return record?.ClearCount ?? 0;
        }

        public bool IsMastered(MasteryAchievementState state, BossModifierId modifier)
        {
            var record = FindRecord(state, modifier);
            return record != null && record.ClearCount >= 10 && record.HighStarClears >= 3 && record.NoHpLossClears >= 1;
        }

        public void UnlockAchievement(MasteryAchievementState state, string achievementId)
        {
            if (state == null) return;
            if (!state.UnlockedAchievements.Contains(achievementId))
                state.UnlockedAchievements.Add(achievementId);
        }

        private static MasteryRecord FindRecord(MasteryAchievementState state, BossModifierId modifier)
        {
            if (state?.Records == null) return null;
            for (var i = 0; i < state.Records.Count; i++)
            {
                if (state.Records[i].ModifierId == modifier)
                    return state.Records[i];
            }
            return null;
        }

        private static MasteryRecord FindOrCreateRecord(MasteryAchievementState state, BossModifierId modifier)
        {
            var record = FindRecord(state, modifier);
            if (record != null) return record;

            record = new MasteryRecord { ModifierId = modifier };
            state.Records.Add(record);
            return record;
        }
    }
}
