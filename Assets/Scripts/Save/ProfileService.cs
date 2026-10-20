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

        public bool CanPrestigeClass(ClassId classId) => _garden.CanPrestige(LoadMetaProgress(), classId);

        /// <summary>Prestiges the class when it is eligible and saves the profile (GAP-017).</summary>
        // [REQ: META-PRESTIGE-004] Called from the Meta Progression panel after the player confirms.
        public bool TryPrestigeClass(ClassId classId)
        {
            var envelope = _saveFile.Load();
            if (envelope.MetaProgress == null || !_garden.TryPrestige(envelope.MetaProgress, classId))
                return false;
            _saveFile.Save(envelope);
            return true;
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

            // Check if item codex is now complete: every item type discovered, except the counter items (ITEM-COUNTER-007),
            // which only shops sell and only for the floor's rules, so they would make the unlock depend on luck.
            if (!progress.ItemCodexComplete && meta.ItemCodex?.Entries != null)
            {
                var required = 0;
                foreach (Core.ItemType type in System.Enum.GetValues(typeof(Core.ItemType)))
                    if (!Items.CounterItems.IsCounterItem(type)) required++;
                var discovered = 0;
                for (var ci = 0; ci < meta.ItemCodex.Entries.Count; ci++)
                    if (meta.ItemCodex.Entries[ci].Discovered
                        && !(System.Enum.TryParse<Core.ItemType>(meta.ItemCodex.Entries[ci].ItemId, out var found)
                             && Items.CounterItems.IsCounterItem(found)))
                        discovered++;
                if (discovered >= required)
                    progress.ItemCodexComplete = true;
            }

            // 3. Commit XP to class (skip if tutorial)
            // [REQ: XP-COMMIT-001] XP committed to class at run end only; no mid-run leveling
            // [HARMONY-XP-001] XP is scaled by the harmony multiplier before commit, on every run end (victory or
            // defeat), with no flat bonus (SPEC-CONF-024, decided 2026-10-06; spec §4.7).
            if (!result.TutorialMode)
            {
                var xpToCommit = (int)Math.Round(result.XpEarned * HarmonyDifficultyService.GetXpMultiplier(result.HarmonyLevel));
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

            // 6. [REQ: META-COMPLETE-001] Record the modifiers of defeated bosses (GAP-009: this had no caller).
            if (result.ClearedBossModifiers != null)
                foreach (var modifier in result.ClearedBossModifiers)
                    _completion.RecordModifierCleared(completion, modifier);

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
