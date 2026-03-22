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
        private readonly ClassGardenProgressionService _gardenProgressionService = new();

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

        public bool TryDiscoverRelic(RelicId relicId)
        {
            if (Meta.DiscoveredRelics.Contains(relicId))
            {
                return false;
            }

            Meta.DiscoveredRelics.Add(relicId);
            return true;
        }

        public List<ClassId> RecordRunAndGetNewUnlocks(RunResult result)
        {
            var newUnlocks = new List<ClassId>();

            if (result.TutorialMode || result.Mode == GameMode.Tutorial)
            {
                return newUnlocks;
            }

            _gardenProgressionService.ApplyRun(Meta, result.PlayedClassId, result);

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

            if (!Meta.EndlessZenUnlocked && Stats.TotalRuns >= 10)
            {
                Meta.EndlessZenUnlocked = true;
            }

            var oldAverage = Stats.AverageMistakes;
            Stats.AverageMistakes = ((oldAverage * (Stats.TotalRuns - 1)) + result.MistakesMade) / Stats.TotalRuns;

            if (Stats.FastestSeconds == 0 || result.SecondsPlayed < Stats.FastestSeconds)
            {
                Stats.FastestSeconds = result.SecondsPlayed;
            }

            if (result.GardenDepthReached > Stats.HighestEndlessDepth)
            {
                Stats.HighestEndlessDepth = result.GardenDepthReached;
            }

            if (result.GoldEarned > Stats.HighestGoldInSingleRun)
                Stats.HighestGoldInSingleRun = result.GoldEarned;

            if (result.PeakCombo > Stats.HighestCombo)
                Stats.HighestCombo = result.PeakCombo;

            if (result.GardenDepthReached > Stats.HighestFloorReached)
                Stats.HighestFloorReached = result.GardenDepthReached;

            if (result.RiskRoutesChosen > Stats.RiskRoutesChosenInBestRun)
                Stats.RiskRoutesChosenInBestRun = result.RiskRoutesChosen;

            if (result.UsedAnyItem) Stats.UsedItem = true;
            if (result.AcquiredRelic) Stats.AcquiredRelic = true;
            if (result.BoughtFromShop) Stats.BoughtFromShop = true;
            if (result.SwappedRelic) Stats.SwappedRelic = true;
            if (result.AcquiredEpicItem) Stats.AcquiredEpicItem = true;
            if (result.FlawlessFloor) Stats.CompletedFlawlessFloor = true;
            if (result.Victory && result.Mode == GameMode.GardenRun) Stats.CompletedFullRun = true;
            if (result.Mode == GameMode.Tutorial) Stats.CompletedTutorialPuzzle = true;
            if (result.Victory && !result.UsedAnyItem && result.Mode == GameMode.GardenRun)
                Stats.CompletedNoItemRun = true;
            if (result.SpiritTrialScore > Stats.HighestSpiritTrialScore)
                Stats.HighestSpiritTrialScore = result.SpiritTrialScore;

            // Track size×star combos for completion
            RecordSizeStarCombo(result);

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

            if (result.SolvedEightByEightFourStar)
            {
                _masteryService.RecordNineByNineFiveStarClear(Mastery);
            }

            if (result.Victory && result.PerfectClear)
            {
                _masteryService.RecordNoItemRun(Mastery);
            }

            _completionService.Recalculate(Completion, Meta, Mastery, Stats);
            _achievementService.EvaluateUnlocks(result, Meta, Stats, Mastery);
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

        private void RecordSizeStarCombo(RunResult result)
        {
            if (result.TileXpEntries == null) return;
            for (var i = 0; i < result.TileXpEntries.Count; i++)
            {
                var entry = result.TileXpEntries[i];
                var key = $"{entry.BoardSize}x{entry.Stars}";
                if (Stats.ClearedSizeStarKeys.Add(key))
                    Stats.SizeStarCombosCleared++;
            }
        }

        private static void CopyMeta(MetaProgressionState from, MetaProgressionState to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.EndlessZenUnlocked = from.EndlessZenUnlocked;
            to.SpiritTrialsUnlocked = from.SpiritTrialsUnlocked;
            to.MaxStarCap = from.MaxStarCap;
            to.ClassUnlocks = from.ClassUnlocks ?? new ClassUnlockProgress();
            to.GardenProgression ??= new GardenClassProgressionState();

            if (from.GardenProgression != null)
            {
                to.GardenProgression.TotalXpEarned = from.GardenProgression.TotalXpEarned;
                to.GardenProgression.ArchiveRunCount = from.GardenProgression.ArchiveRunCount;
                to.GardenProgression.ArchiveSeedsBloomed = from.GardenProgression.ArchiveSeedsBloomed;
                to.GardenProgression.ArchiveBossesDefeated = from.GardenProgression.ArchiveBossesDefeated;
                to.GardenProgression.ArchivePerfectRuns = from.GardenProgression.ArchivePerfectRuns;

                to.GardenProgression.ClassEntries.Clear();
                for (var i = 0; i < from.GardenProgression.ClassEntries.Count; i++)
                {
                    var source = from.GardenProgression.ClassEntries[i];
                    to.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
                    {
                        ClassId = source.ClassId,
                        TotalXp = source.TotalXp,
                        PrestigeTier = source.PrestigeTier
                    });
                }
            }

            to.UnlockedClasses.Clear();
            for (var i = 0; i < from.UnlockedClasses.Count; i++)
            {
                to.UnlockedClasses.Add(from.UnlockedClasses[i]);
            }

            to.DiscoveredRelics.Clear();
            for (var i = 0; i < from.DiscoveredRelics.Count; i++)
            {
                to.DiscoveredRelics.Add(from.DiscoveredRelics[i]);
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
            to.HighestEndlessDepth = from.HighestEndlessDepth;
            to.TotalAchievementsUnlocked = from.TotalAchievementsUnlocked;
            to.SizeStarCombosCleared = from.SizeStarCombosCleared;
            to.HighestGoldInSingleRun = from.HighestGoldInSingleRun;
            to.RiskRoutesChosenInBestRun = from.RiskRoutesChosenInBestRun;
            to.HighestCombo = from.HighestCombo;
            to.HighestFloorReached = from.HighestFloorReached;
            to.ModifiersEncountered = from.ModifiersEncountered;
            to.CompletedFullRun = from.CompletedFullRun;
            to.UsedItem = from.UsedItem;
            to.AcquiredRelic = from.AcquiredRelic;
            to.BoughtFromShop = from.BoughtFromShop;
            to.SwappedRelic = from.SwappedRelic;
            to.CompletedTutorialPuzzle = from.CompletedTutorialPuzzle;
            to.CompletedNoItemRun = from.CompletedNoItemRun;
            to.CompletedFlawlessFloor = from.CompletedFlawlessFloor;
            to.AcquiredEpicItem = from.AcquiredEpicItem;
            to.HighestSpiritTrialScore = from.HighestSpiritTrialScore;

            to.ClearedSizeStarKeys.Clear();
            foreach (var key in from.ClearedSizeStarKeys)
                to.ClearedSizeStarKeys.Add(key);
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
        }
    }
}
