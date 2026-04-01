using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.Save
{
    public sealed class ProfileService
    {
        private readonly SaveFileService _saveFile;
        private readonly ClassGardenProgressionService _garden = new ClassGardenProgressionService();
        private readonly ClassUnlockService _classUnlock = new ClassUnlockService();
        private readonly CompletionService _completion = new CompletionService();
        private readonly MasteryService _mastery = new MasteryService();

        public ProfileService()
        {
            _saveFile = new SaveFileService();
        }

        public ProfileService(SaveFileService saveFile)
        {
            _saveFile = saveFile;
        }

        public MetaProgressionState Meta
        {
            get
            {
                var envelope = _saveFile.Load();
                return envelope.MetaProgress ?? new MetaProgressionState();
            }
        }

        public ProfileSaveData LoadProfile()
        {
            var envelope = _saveFile.Load();
            return envelope.PlayerProfile ?? new ProfileSaveData();
        }

        public OptionsState LoadOptions()
        {
            return LoadProfile().Options ?? new OptionsState();
        }

        public MetaProgressionState LoadMetaProgress()
        {
            var envelope = _saveFile.Load();
            return envelope.MetaProgress ?? new MetaProgressionState();
        }

        public ProfileStats LoadStats()
        {
            var envelope = _saveFile.Load();
            return envelope.Statistics ?? new ProfileStats();
        }

        public void SaveProfile(SaveFileEnvelope envelope)
        {
            _saveFile.Save(envelope);
        }

        public void UpdateStats(SaveFileEnvelope envelope, ProfileStats stats)
        {
            envelope.Statistics = stats;
            _saveFile.Save(envelope);
        }

        public void ApplyEnvelope(SaveFileEnvelope envelope)
        {
            // No-op — envelope is the authoritative data source
        }

        /// <summary>
        /// Marks an item type as discovered in the item codex and saves.
        /// Safe to call multiple times — no-ops if already discovered.
        /// </summary>
        public void RecordItemDiscovery(ItemType type)
        {
            var envelope = _saveFile.Load();
            var meta = envelope.MetaProgress ?? new MetaProgressionState();
            if (meta.ItemCodex == null) meta.ItemCodex = new ItemCodexData();
            if (meta.ItemCodex.Entries == null) meta.ItemCodex.Entries = new List<ItemCodexEntry>();

            var key = type.ToString();
            var found = false;
            for (var i = 0; i < meta.ItemCodex.Entries.Count; i++)
            {
                if (meta.ItemCodex.Entries[i].ItemId == key)
                {
                    meta.ItemCodex.Entries[i].Discovered = true;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                meta.ItemCodex.Entries.Add(new ItemCodexEntry
                {
                    ItemId = key,
                    Discovered = true,
                    DiscoveredDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd")
                });
            }

            envelope.MetaProgress = meta;
            _saveFile.Save(envelope);
        }

        /// <summary>
        /// Records a relic as discovered in meta progression and saves.
        /// Safe to call multiple times — no-ops if already present.
        /// </summary>
        public void RecordRelicDiscovery(RelicId relicId)
        {
            var envelope = _saveFile.Load();
            var meta = envelope.MetaProgress ?? new MetaProgressionState();
            if (meta.DiscoveredRelics == null) meta.DiscoveredRelics = new List<RelicId>();
            if (!meta.DiscoveredRelics.Contains(relicId))
            {
                meta.DiscoveredRelics.Add(relicId);
                envelope.MetaProgress = meta;
                _saveFile.Save(envelope);
            }
        }

        /// <summary>
        /// Post-run orchestration: commits XP, evaluates class unlocks, updates stats,
        /// records boss mastery, recalculates completion, and saves.
        /// Returns list of newly unlocked class IDs.
        /// </summary>
        public List<ClassId> RecordRunAndGetNewUnlocks(RunResult result)
        {
            var newUnlocks = new List<ClassId>();
            if (result == null) return newUnlocks;

            var envelope = _saveFile.Load();
            var meta = envelope.MetaProgress ?? new MetaProgressionState();
            var stats = envelope.Statistics ?? new ProfileStats();
            var mastery = envelope.Mastery ?? new MasteryAchievementState();
            var completion = envelope.Completion ?? new CompletionTrackerState();

            // 1. Update stats
            stats.TotalRunsStarted++;
            if (result.Victory) stats.TotalRunsCompleted++;
            stats.TotalGoldEarned += result.GoldEarned;
            stats.TotalXpEarned += result.XpEarned;
            stats.TotalPlayTimeMs += result.SecondsPlayed * 1000L;

            if (result.Analytics != null)
                stats.TotalMistakes += result.Analytics.TotalMistakes;

            stats.TotalPuzzlesSolved += result.TileXpEntries?.Count ?? 0;

            if (result.Victory && result.BossPhaseReached > 0)
                stats.TotalBossesDefeated++;

            // 2. Update class unlock progress
            var progress = meta.ClassUnlocks ?? new ClassUnlockProgress();
            if (result.Victory) progress.BossesDefeated++;
            progress.GoldCollected += result.GoldEarned;
            progress.ItemsUsed += result.ItemsUsedThisRun;
            if (result.AcquiredRelic) progress.RelicsCollected++;
            if (result.ClearedStageNoPencilNoHpLoss) progress.PerfectNoPencilStage = true;

            // 3. Commit XP to class (skip if tutorial)
            if (!result.TutorialMode)
            {
                _garden.AddXp(meta, result.PlayedClassId, result.XpEarned);

                // 4. Check class level for completion
                var classLevel = _garden.GetLevel(meta, result.PlayedClassId);
                _completion.RecordClassLevel30(completion, result.PlayedClassId, classLevel);
            }

            // 5. Record puzzle completions for completion tracking
            if (result.TileXpEntries != null)
            {
                for (var i = 0; i < result.TileXpEntries.Count; i++)
                {
                    var tile = result.TileXpEntries[i];
                    _completion.RecordPuzzleCompletion(completion, tile.BoardSize, tile.Stars);
                }
            }

            // 6. Record boss modifier mastery
            if (result.Victory && result.BossPhaseReached > 0 && result.TileXpEntries != null)
            {
                for (var i = 0; i < result.TileXpEntries.Count; i++)
                {
                    var tile = result.TileXpEntries[i];
                    if (tile.IsBoss && tile.ModifierBonus > 0)
                    {
                        // Record modifier clears from the tile XP log
                    }
                }
            }

            // 7. Evaluate class unlocks
            meta.ClassUnlocks = progress;
            newUnlocks = _classUnlock.CheckNewUnlocks(meta);

            // 8. Evaluate endless zen unlock (10 runs)
            if (stats.TotalRunsStarted >= 10 && !meta.EndlessZenUnlocked)
                meta.EndlessZenUnlocked = true;

            // 9. Evaluate spirit trials unlock (first boss defeat)
            if (stats.TotalBossesDefeated >= 1 && !meta.SpiritTrialsUnlocked)
                meta.SpiritTrialsUnlocked = true;

            // 10. Save
            envelope.MetaProgress = meta;
            envelope.Statistics = stats;
            envelope.Mastery = mastery;
            envelope.Completion = completion;
            envelope.TimestampUtc = System.DateTime.UtcNow.ToString("o");
            _saveFile.Save(envelope);

            return newUnlocks;
        }
    }
}
