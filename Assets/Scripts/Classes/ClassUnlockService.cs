using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassUnlockService
    {
        public IReadOnlyList<ClassId> EvaluateUnlocks(MetaProgressionState meta)
        {
            var newlyUnlocked = new List<ClassId>();
            var progress = meta.ClassUnlocks;

            UnlockIfEligible(meta, ClassId.GardenMonk,
                progress.ClearedTier1Or2Boss && progress.NonTutorialRunCount >= 3,
                newlyUnlocked);

            UnlockIfEligible(meta, ClassId.ShrineArchivist,
                progress.ClearedTier3Boss && progress.SolvedEightByEightFourStar,
                newlyUnlocked);

            UnlockIfEligible(meta, ClassId.KoiGambler,
                progress.WonWithUnderThreeHp || progress.CompletedKoiPath,
                newlyUnlocked);

            UnlockIfEligible(meta, ClassId.StoneGardener,
                progress.ClearedTier4Boss && progress.ReachedHeatFive,
                newlyUnlocked);

            UnlockIfEligible(meta, ClassId.LanternSeer,
                progress.ClearedGermanWhispersBoss && progress.ClearedMultiStageBoss,
                newlyUnlocked);

            return newlyUnlocked;
        }

        public void UpdateProgressFromRunResult(MetaProgressionState meta, RunResult result)
        {
            if (result.TutorialMode || result.Mode == GameMode.Tutorial)
            {
                return;
            }

            var progress = meta.ClassUnlocks;
            progress.NonTutorialRunCount++;

            if (result.ClearedBoss)
            {
                if (result.ClearedBossTier <= BossModifierTier.Tier2)
                {
                    progress.ClearedTier1Or2Boss = true;
                }

                if (result.ClearedBossTier >= BossModifierTier.Tier3)
                {
                    progress.ClearedTier3Boss = true;
                }

                if (result.ClearedBossTier >= BossModifierTier.Tier4)
                {
                    progress.ClearedTier4Boss = true;
                }
            }

            if (result.SolvedEightByEightFourStar)
            {
                progress.SolvedEightByEightFourStar = true;
            }

            if (result.CompletedKoiPathRoute)
            {
                progress.CompletedKoiPath = true;
            }

            if (result.WonWithUnderThreeHp)
            {
                progress.WonWithUnderThreeHp = true;
            }

            if (result.PeakHeatScore >= 5f)
            {
                progress.ReachedHeatFive = true;
            }

            if (result.ClearedGermanWhispersBoss)
            {
                progress.ClearedGermanWhispersBoss = true;
            }

            if (result.ClearedMultiStageBoss)
            {
                progress.ClearedMultiStageBoss = true;
            }
        }

        public string GetUnlockRequirementText(ClassId classId)
        {
            return classId switch
            {
                ClassId.NumberFreak => "Default unlocked",
                ClassId.GardenMonk => "Clear Tier 1/2 boss and reach Run 3",
                ClassId.ShrineArchivist => "Clear Tier 3 boss and solve one 8x8 4★",
                ClassId.KoiGambler => "Win with <3 HP remaining or complete Koi Path route",
                ClassId.StoneGardener => "Defeat Tier 4 boss and reach Heat Score 5.0",
                ClassId.LanternSeer => "Clear German Whispers boss and beat multi-stage boss",
                ClassId.ZenMaster => "Locked preview class",
                _ => "Unknown unlock requirement"
            };
        }

        private static void UnlockIfEligible(MetaProgressionState meta, ClassId classId, bool condition, List<ClassId> newlyUnlocked)
        {
            if (!condition || meta.UnlockedClasses.Contains(classId))
            {
                return;
            }

            meta.UnlockedClasses.Add(classId);
            newlyUnlocked.Add(classId);
        }
    }
}
