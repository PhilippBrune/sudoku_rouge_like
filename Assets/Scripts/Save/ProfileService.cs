using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class ProfileService
    {
        private readonly SaveFileService _saveFile;
        private readonly ClassGardenProgressionService _garden = new ClassGardenProgressionService();
        private readonly ClassUnlockService _classUnlock = new ClassUnlockService();
        private readonly CompletionService _completion = new CompletionService();

        public ProfileService()
        {
            _saveFile = new SaveFileService(SaveProfileService.ActiveSlot);
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
        // [REQ: META-CODEX-001] RecordItemDiscovery + RecordRelicDiscovery: item & relic codex discovery tracking
        // [REQ: META-CODEX-002] Entry is marked Discovered on first acquisition only; subsequent calls are no-ops
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
        // [REQ: META-UNLOCK-002] [REQ: META-XP-004] RecordRunAndGetNewUnlocks: XP committed at run end only (no mid-run leveling); evaluates all 8 class unlock conditions
        public List<ClassId> RecordRunAndGetNewUnlocks(RunResult result, RunState liveRunState = null)
        {
            var newUnlocks = new List<ClassId>();
            if (result == null) return newUnlocks;

            var envelope = _saveFile.Load();
            var meta = envelope.MetaProgress ?? new MetaProgressionState();
            var stats = envelope.Statistics ?? new ProfileStats();
            var mastery = envelope.Mastery ?? new MasteryAchievementState();
            var completion = envelope.Completion ?? new CompletionTrackerState();
            ModifierDiscoveryService.SanitizeMetaProgress(meta);

            // 1. Update stats
            stats.TotalRunsStarted++;
            if (result.Victory) stats.TotalRunsCompleted++;
            stats.TotalGoldEarned += result.GoldEarned;
            stats.TotalXpEarned += result.XpEarned;
            stats.TotalPlayTimeMs += result.SecondsPlayed * 1000L;

            if (result.Analytics != null)
                stats.TotalMistakes += result.Analytics.TotalMistakes;

            stats.TotalPuzzlesSolved += result.TileXpEntries?.Count ?? 0;

            stats.TotalBossesDefeated += result.BossesDefeatedThisRun;

            // 2. Update class unlock progress
            var progress = meta.ClassUnlocks ?? new ClassUnlockProgress();
            progress.BossesDefeated += result.BossesDefeatedThisRun;
            progress.GoldCollected += result.GoldEarned;
            progress.ItemsUsed += result.ItemsUsedThisRun;
            progress.RelicsCollected += result.RelicsCollectedThisRun;
            if (result.ClearedStageNoPencilNoHpLoss) progress.PerfectNoPencilStage = true;
            // [BUG-FIX] Perfect full run: victory with zero mistakes across all 5 floors.
            if (result.Victory && result.MistakesMade == 0) progress.PerfectFullRunAllFloors = true;

            // Check if item codex is now complete (all 26 item types discovered)
            if (!progress.ItemCodexComplete && meta.ItemCodex?.Entries != null)
            {
                var allItems = System.Enum.GetValues(typeof(Core.ItemType));
                var discovered = 0;
                for (var ci = 0; ci < meta.ItemCodex.Entries.Count; ci++)
                    if (meta.ItemCodex.Entries[ci].Discovered) discovered++;
                if (discovered >= allItems.Length)
                    progress.ItemCodexComplete = true;
            }

            // 3. Commit XP to class (skip if tutorial)
            // [REQ: XP-COMMIT-001] XP committed to class at run end only; no mid-run leveling
            // [HARMONY-XP-001] XP is scaled by the harmony multiplier before commit.
            // [HARMONY-XP-001] XP multiplier applies only on victory; defeats use ×1.0.
            // [L4-B] On victory add flat HarmonyLevel × 50 bonus XP on top.
            if (!result.TutorialMode)
            {
                int xpToCommit;
                if (result.Victory)
                {
                    xpToCommit = (int)Math.Round(result.XpEarned * HarmonyDifficultyService.GetXpMultiplier(result.HarmonyLevel));
                    xpToCommit += result.HarmonyLevel * 50;
                }
                else
                {
                    xpToCommit = result.XpEarned;
                }
                _garden.AddXp(meta, result.PlayedClassId, xpToCommit);

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

            // 7. Evaluate class unlocks
            meta.ClassUnlocks = progress;
            newUnlocks = _classUnlock.CheckNewUnlocks(meta);

            // 8. Evaluate endless zen unlock (10 runs)
            // [REQ: ZENMODE-UNLOCK-001] Unlocked when TotalRuns ≥ 10
            if (stats.TotalRunsStarted >= 10 && !meta.EndlessZenUnlocked)
                meta.EndlessZenUnlocked = true;

            // 8b. Evaluate Harmony level unlock [HARMONY-UNLOCK-001]
            // Win at current max level (H0–H9) to unlock the next level (H1–H10).
            // Requires at least 1 boss defeated during the run.
            if (result.Victory
                && result.Mode == GameMode.GardenRun
                && result.HarmonyLevel == meta.MaxUnlockedHarmonyLevel
                && result.BossesDefeatedThisRun >= 1
                && meta.MaxUnlockedHarmonyLevel < 10)
            {
                meta.MaxUnlockedHarmonyLevel++;
            }

            // 9. Evaluate spirit trials unlock (first boss defeat)
            // [REQ: TRIAL-UNLOCK-001] Unlocked after first boss defeat (full run completion)
            if (stats.TotalBossesDefeated >= 1 && !meta.SpiritTrialsUnlocked)
                meta.SpiritTrialsUnlocked = true;

            // 9b. Evaluate Harmony achievements [HARMONY-ACHIEVE-001]
            SteamAchievementService.EvaluateHarmonyAchievements(meta, mastery, result);

            // 9c. Mirror cumulative stats to Steam (no-op if SDK not present)
            SteamStatsService.SyncFromLocalStats(stats);

            var discoveryAdded = ModifierDiscoveryService.MergeIntoMeta(meta, liveRunState?.SeenBossModifiers);
            if (liveRunState?.SeenBossModifiers != null)
            {
                UnityEngine.Debug.Log(
                    $"[ModifierDiscovery] End-of-run meta merge: added={discoveryAdded}, " +
                    $"runSeen={ModifierDiscoveryService.Describe(liveRunState.SeenBossModifiers)}, " +
                    $"metaNow={ModifierDiscoveryService.Describe(meta.DiscoveredBossModifiers)}");
            }

            // 10. Save — also clear active run state here so no second save races against this one
            envelope.MetaProgress = meta;
            envelope.Statistics = stats;
            envelope.Mastery = mastery;
            envelope.Completion = completion;
            envelope.TimestampUtc = System.DateTime.UtcNow.ToString("o");
            if (!result.TutorialMode)
            {
                envelope.ActiveRunState = null;
                envelope.ActivePuzzle = null;
            }
            _saveFile.Save(envelope);

            return newUnlocks;
        }
    }
}
