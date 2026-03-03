using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.Save
{
    public sealed class ProfileService
    {
        private readonly ClassUnlockService _classUnlockService = new();
        private readonly MasteryService _masteryService = new();
        private readonly CompletionService _completionService = new();
        private readonly AscensionService _ascensionService = new();
        private readonly SteamAchievementService _achievementService = new();

        public MetaProgressionState Meta { get; } = new();
        public ProfileStats Stats { get; } = new();
        public OptionsState Options { get; } = new();
        public TutorialProgressState TutorialProgress { get; } = new();
        public MasteryAchievementState Mastery { get; } = new();
        public CompletionTrackerState Completion { get; } = new();

        public ProfileService()
        {
            if (!Meta.UnlockedClasses.Contains(ClassId.NumberFreak))
            {
                Meta.UnlockedClasses.Add(ClassId.NumberFreak);
            }
        }

        public bool IsClassUnlocked(ClassId classId)
        {
            return Meta.UnlockedClasses.Contains(classId);
        }

        public void UnlockClass(ClassId classId)
        {
            if (!Meta.UnlockedClasses.Contains(classId))
            {
                Meta.UnlockedClasses.Add(classId);
            }
        }

        public void AddEssence(int value)
        {
            if (value <= 0)
            {
                return;
            }

            Meta.GardenEssence += value;
        }

        public bool TryUnlockRelic(string relicId, int cost)
        {
            if (Meta.UnlockedRelics.Contains(relicId) || Meta.GardenEssence < cost)
            {
                return false;
            }

            Meta.GardenEssence -= cost;
            Meta.UnlockedRelics.Add(relicId);
            return true;
        }

        public List<ClassId> RecordRunAndGetNewUnlocks(RunResult result)
        {
            var newUnlocks = new List<ClassId>();

            if (result.TutorialMode || result.Mode == GameMode.Tutorial)
            {
                return newUnlocks;
            }

            _classUnlockService.UpdateProgressFromRunResult(Meta, result);
            var evaluated = _classUnlockService.EvaluateUnlocks(Meta);
            for (var i = 0; i < evaluated.Count; i++)
            {
                newUnlocks.Add(evaluated[i]);
            }

            Stats.TotalRuns++;
            if (result.Victory)
            {
                Stats.BossClears++;
            }

            var oldAverage = Stats.AverageMistakes;
            Stats.AverageMistakes = ((oldAverage * (Stats.TotalRuns - 1)) + result.MistakesMade) / Stats.TotalRuns;

            if (Stats.FastestSeconds == 0 || result.SecondsPlayed < Stats.FastestSeconds)
            {
                Stats.FastestSeconds = result.SecondsPlayed;
            }

            if (result.PeakHeatScore > Stats.HighestHeatScore)
            {
                Stats.HighestHeatScore = result.PeakHeatScore;
            }

            if (result.GardenDepthReached > Stats.HighestEndlessDepth)
            {
                Stats.HighestEndlessDepth = result.GardenDepthReached;
            }

            if (result.ClearedBoss)
            {
                var modifier = result.ClearedGermanWhispersBoss ? BossModifierId.GermanWhispers : BossModifierId.ParityLines;
                _masteryService.RecordBossClear(
                    Mastery,
                    modifier,
                    stars: result.ClearedBossTier >= BossModifierTier.Tier4 ? 5 : 4,
                    noHpLoss: result.Victory && result.WonWithUnderThreeHp == false,
                    dualModifier: result.ClearedMultiStageBoss);
            }

            if (Mastery.BossClearsByModifier.Count >= 9)
            {
                Meta.HiddenDualModifierBossUnlocked = true;
            }

            if (Meta.HiddenDualModifierBossUnlocked && result.ClearedMultiStageBoss && result.WonWithOneHp)
            {
                Meta.ChaosMonkUnlocked = true;
                UnlockClass(ClassId.ChaosMonk);
            }

            if (result.SolvedEightByEightFourStar)
            {
                _masteryService.RecordNineByNineFiveStarClear(Mastery);
            }

            if (result.Victory && result.PerfectClear)
            {
                _masteryService.RecordNoItemRun(Mastery);
            }

            _completionService.Recalculate(Completion, Meta, Mastery, Stats);
            _achievementService.EvaluateUnlocks(result, Meta);
            Stats.TotalAchievementsUnlocked = Meta.UnlockedAchievements.Count;

            return newUnlocks;
        }

        public void RecordRun(RunResult result)
        {
            RecordRunAndGetNewUnlocks(result);
        }

        public void ApplyAscension()
        {
            _ascensionService.ApplyAscension(Meta);
        }

        public bool TryPrestigeReset()
        {
            return _ascensionService.TryPrestigeReset(Meta);
        }

        public int GetSeasonalChallengeSeed(int year, int month)
        {
            return _ascensionService.BuildMonthlySeed(year, month);
        }

        public void ApplyEnvelope(SaveFileEnvelope envelope)
        {
            if (envelope == null)
            {
                return;
            }

            CopyMeta(envelope.MetaProgress, Meta);
            CopyOptions(envelope.PlayerProfile?.Options, Options);
            CopyStats(envelope.Statistics, Stats);
            CopyTutorial(envelope.TutorialProgress, TutorialProgress);
            CopyMastery(envelope.Mastery, Mastery);
            CopyCompletion(envelope.Completion, Completion);

            if (!Meta.UnlockedClasses.Contains(ClassId.NumberFreak))
            {
                Meta.UnlockedClasses.Add(ClassId.NumberFreak);
            }
        }

        private static void CopyMeta(MetaProgressionState from, MetaProgressionState to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.GardenEssence = from.GardenEssence;
            to.EndlessZenUnlocked = from.EndlessZenUnlocked;
            to.SpiritTrialsUnlocked = from.SpiritTrialsUnlocked;
            to.MaxStarCap = from.MaxStarCap;
            to.ClassUnlocks = from.ClassUnlocks ?? new ClassUnlockProgress();

            to.UnlockedClasses.Clear();
            for (var i = 0; i < from.UnlockedClasses.Count; i++)
            {
                to.UnlockedClasses.Add(from.UnlockedClasses[i]);
            }

            to.UnlockedRelics.Clear();
            for (var i = 0; i < from.UnlockedRelics.Count; i++)
            {
                to.UnlockedRelics.Add(from.UnlockedRelics[i]);
            }

            to.PurchasedPermanentUpgrades.Clear();
            for (var i = 0; i < from.PurchasedPermanentUpgrades.Count; i++)
            {
                to.PurchasedPermanentUpgrades.Add(from.PurchasedPermanentUpgrades[i]);
            }

            to.UnlockedAchievements.Clear();
            for (var i = 0; i < from.UnlockedAchievements.Count; i++)
            {
                to.UnlockedAchievements.Add(from.UnlockedAchievements[i]);
            }
        }

        private static void CopyOptions(OptionsState from, OptionsState to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.Language = from.Language;
            to.Audio = from.Audio ?? new AudioSettingsModel();
            to.Graphics = from.Graphics ?? new GraphicsSettingsModel();
            to.Gameplay = from.Gameplay ?? new GameplaySettings();
            to.Accessibility = from.Accessibility ?? new AccessibilitySettings();
        }

        private static void CopyStats(ProfileStats from, ProfileStats to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.TotalRuns = from.TotalRuns;
            to.BossClears = from.BossClears;
            to.AverageMistakes = from.AverageMistakes;
            to.FastestSeconds = from.FastestSeconds;
            to.HighestHeatScore = from.HighestHeatScore;
            to.HighestEndlessDepth = from.HighestEndlessDepth;
            to.TotalAchievementsUnlocked = from.TotalAchievementsUnlocked;
        }

        private static void CopyTutorial(TutorialProgressState from, TutorialProgressState to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.CompletedConfigurationKeys.Clear();
            for (var i = 0; i < from.CompletedConfigurationKeys.Count; i++)
            {
                to.CompletedConfigurationKeys.Add(from.CompletedConfigurationKeys[i]);
            }

            to.CompletedSingleModifiers.Clear();
            for (var i = 0; i < from.CompletedSingleModifiers.Count; i++)
            {
                to.CompletedSingleModifiers.Add(from.CompletedSingleModifiers[i]);
            }
        }

        private static void CopyMastery(MasteryAchievementState from, MasteryAchievementState to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.BossClearsByModifier.Clear();
            for (var i = 0; i < from.BossClearsByModifier.Count; i++)
            {
                to.BossClearsByModifier.Add(from.BossClearsByModifier[i]);
            }

            to.PerfectBossClearsByModifier.Clear();
            for (var i = 0; i < from.PerfectBossClearsByModifier.Count; i++)
            {
                to.PerfectBossClearsByModifier.Add(from.PerfectBossClearsByModifier[i]);
            }

            to.NineByNineFiveStarClears = from.NineByNineFiveStarClears;
            to.DualModifierClears = from.DualModifierClears;
            to.NoItemRuns = from.NoItemRuns;
        }

        private static void CopyCompletion(CompletionTrackerState from, CompletionTrackerState to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.GlobalCompletionPercent = from.GlobalCompletionPercent;
            to.AllSizesAllStarsCleared = from.AllSizesAllStarsCleared;
            to.AllModifiersCleared = from.AllModifiersCleared;
            to.AllClassesLevelThirty = from.AllClassesLevelThirty;
            to.AllRelicsUnlocked = from.AllRelicsUnlocked;
            to.MultiStageBossHighHeatClear = from.MultiStageBossHighHeatClear;
        }
    }
}
