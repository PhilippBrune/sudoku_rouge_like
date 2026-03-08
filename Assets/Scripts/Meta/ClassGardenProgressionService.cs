using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Meta
{
    public sealed class ClassGardenProgressionService
    {
        private const int MaxLevel = 40;
        private const int MaxPrestigeTier = 9;

        public int XpToNextLevel(int level)
        {
            var safeLevel = Math.Clamp(level, 1, MaxLevel);
            if (safeLevel <= 20)
            {
                return 120 + ((safeLevel - 1) * 20);
            }

            var delta = safeLevel - 20;
            return 500 + (delta * delta * 10);
        }

        public int CalculateRunXp(int boardSize, int stars, int depthReached, bool victory, bool clearedBoss, bool perfectClear, int mistakesMade, int activeModifierCount)
        {
            var baseXp = 60 + (boardSize * 8) + (stars * 30) + (depthReached * 6);
            var modifierBonus = activeModifierCount >= 2 ? 35 : activeModifierCount == 1 ? 15 : 0;
            var victoryBonus = victory ? 40 : 0;
            var bossBonus = clearedBoss ? 55 : 0;
            var perfectBonus = perfectClear ? 30 : 0;
            var mistakePenalty = Math.Max(0, mistakesMade) * 5;
            return Math.Max(0, baseXp + modifierBonus + victoryBonus + bossBonus + perfectBonus - mistakePenalty);
        }

        public GardenProgressionUpdate ApplyRun(MetaProgressionState meta, ClassId playedClass, RunResult result)
        {
            var state = meta.GardenProgression ??= new GardenClassProgressionState();
            var entry = GetOrCreateClassEntry(state, playedClass);

            var runXp = result.XpEarned;

            state.ArchiveRunCount++;
            if (result.Victory)
            {
                state.ArchiveSeedsBloomed++;
            }

            if (result.ClearedBoss)
            {
                state.ArchiveBossesDefeated++;
            }

            if (result.PerfectClear)
            {
                state.ArchivePerfectRuns++;
            }

            state.TotalXpEarned += runXp;
            state.CurrentXp += runXp;

            entry.TotalXpEarned += runXp;
            entry.CurrentXp += runXp;

            var levelsGained = 0;
            while (state.CurrentLevel < MaxLevel)
            {
                var needed = XpToNextLevel(state.CurrentLevel);
                if (state.CurrentXp < needed)
                {
                    break;
                }

                state.CurrentXp -= needed;
                state.CurrentLevel++;
                levelsGained++;

                if (state.CurrentLevel % 5 == 0)
                {
                    state.PassiveTier++;
                }
            }

            while (entry.Level < state.CurrentLevel)
            {
                var needed = XpToNextLevel(entry.Level);
                if (entry.CurrentXp < needed)
                {
                    break;
                }

                entry.CurrentXp -= needed;
                entry.Level++;
            }

            var prestigeGranted = false;
            if (CanPrestige(state))
            {
                state.PrestigeTier++;
                state.CurrentLevel = 1;
                state.CurrentXp = 0;
                state.PassiveTier++;
                prestigeGranted = true;
            }

            entry.PrestigeTier = Math.Max(entry.PrestigeTier, state.PrestigeTier);

            return new GardenProgressionUpdate
            {
                XpAwarded = runXp,
                LevelsGained = levelsGained,
                PrestigeGranted = prestigeGranted,
                NewLevel = state.CurrentLevel,
                NewPrestigeTier = state.PrestigeTier
            };
        }

        private static ClassGardenProgressEntry GetOrCreateClassEntry(GardenClassProgressionState state, ClassId classId)
        {
            for (var i = 0; i < state.ClassEntries.Count; i++)
            {
                if (state.ClassEntries[i].ClassId == classId)
                {
                    return state.ClassEntries[i];
                }
            }

            var created = new ClassGardenProgressEntry { ClassId = classId, Level = 1 };
            state.ClassEntries.Add(created);
            return created;
        }

        private static bool CanPrestige(GardenClassProgressionState state)
        {
            if (state.PrestigeTier >= MaxPrestigeTier)
            {
                return false;
            }

            return state.CurrentLevel >= MaxLevel && state.ArchiveBossesDefeated >= (state.PrestigeTier + 1) * 3;
        }
    }

    public sealed class GardenProgressionUpdate
    {
        public int XpAwarded;
        public int LevelsGained;
        public bool PrestigeGranted;
        public int NewLevel;
        public int NewPrestigeTier;
    }
}
