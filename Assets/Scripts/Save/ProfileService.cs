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

        /// <summary>[REQ: META-COMPLETE-001] The profile's completion in whole percent (the class garden's totals, UI-STYLE-019).</summary>
        public int LoadCompletionPercent() =>
            // The share is a sum of float quarters; the small epsilon keeps 0.29 from flooring to 28 % (review).
            (int)Math.Floor(_completion.GetCompletionPercent(_saveFile.Load().Completion) * 100f + 0.001f);

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

        /// <summary>[REQ: DAILY-WALK-003] The day's scored walk is over: its XP joins the XP waiting for a class. Returns that XP.</summary>
        public int SettleDailyWalk(DailyGoalState liveGoals, int walkXp)
        {
            var envelope = _saveFile.Load();
            var goals = liveGoals ?? envelope.DailyGoals ?? new DailyGoalState();
            var xp = DailyGoalService.SettleWalk(goals, walkXp);
            envelope.DailyGoals = goals;
            _saveFile.Save(envelope);
            return xp;
        }

        /// <summary>[REQ: DAILY-WALK-003] Gives the waiting XP (walks and the weekly scroll) to an unlocked class.</summary>
        public bool GiveWaitingXp(ClassId classId)
        {
            var envelope = _saveFile.Load();
            var goals = envelope.DailyGoals;
            var meta = envelope.MetaProgress ?? new MetaProgressionState();
            if (goals == null || goals.PendingWalkXp <= 0 || !ClassUnlockService.GetProgress(meta, classId).Unlocked) return false;
            _garden.AddXp(meta, classId, goals.PendingWalkXp);
            var completion = envelope.Completion ?? new CompletionTrackerState();
            _completion.RecordClassLevel30(completion, classId, _garden.GetLevel(meta, classId));
            goals.PendingWalkXp = 0;
            envelope.MetaProgress = meta;
            envelope.Completion = completion;
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
                    // META-CODEX-009: an item found before the counters existed (0) was found at least once already.
                    var codexEntry = meta.ItemCodex.Entries[i];
                    var before = codexEntry.Discovered ? Math.Max(1, codexEntry.TimesFound) : codexEntry.TimesFound;
                    codexEntry.TimesFound = CodexCount(before + 1L);
                    codexEntry.Discovered = true;
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
                    DiscoveredDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    TimesFound = 1
                });
            }

            envelope.MetaProgress = meta;
            _saveFile.Save(envelope);
        }

        // [REQ: META-CODEX-009] Adds each use to the item's Codex entry; an item the Codex has not recorded as found
        // (a starting item, the tutorial's) gets no entry, so the count never reveals an unknown item.
        internal static void CountItemUses(MetaProgressionState meta, IEnumerable<string> itemTypesUsed)
        {
            if (meta?.ItemCodex?.Entries == null || itemTypesUsed == null) return;
            var found = new Dictionary<string, ItemCodexEntry>();
            foreach (var entry in meta.ItemCodex.Entries)
                if (entry != null && entry.Discovered && !string.IsNullOrEmpty(entry.ItemId) && !found.ContainsKey(entry.ItemId))
                    found.Add(entry.ItemId, entry);
            foreach (var used in itemTypesUsed)
                if (used != null && found.TryGetValue(used, out var entry))
                    entry.TimesUsed = CodexCount(entry.TimesUsed + 1L);
        }

        // A Codex counter stays between 0 and MaxCodexCount, also with an edited save (no wrap to negative numbers).
        private const int MaxCodexCount = 999_999;
        private static int CodexCount(long value) => (int)Math.Clamp(value, 0L, MaxCodexCount);

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
            // [REQ: INCENSE-XP-001] An Incense Walk is no run toward the 10 that open Endless Zen (owner 2026-10-27).
            if (result.Mode != GameMode.IncenseWalk) stats.TotalRunsStarted++;
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
            if (!progress.ItemCodexComplete && CodexProgress.RequiredItems(meta).Complete)
                progress.ItemCodexComplete = true;

            // 3. Commit XP to class (skip if tutorial)
            // [REQ: XP-COMMIT-001] XP committed to class at run end only; no mid-run leveling
            // [HARMONY-XP-001] XP is scaled by the harmony multiplier before commit, on every run end (victory or
            // defeat), with no flat bonus (SPEC-CONF-024, decided 2026-10-06; spec §4.7).
            // [REQ: META-STATS-001] Garden Runs and the best floor per class (the class garden, UI-STYLE-019).
            // [REQ: DAILY-WALK-003] The daily walk has no class: its XP waits for one (SettleDailyWalk, GiveWaitingXp).
            if (result.Mode == GameMode.GardenRun && !result.TutorialMode && !result.DisableProgressionRewards && !result.DailyWalk)
                _garden.RecordGardenRun(meta, result.PlayedClassId, result.FloorReached);

            if (!result.TutorialMode && !result.DailyWalk)
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
            // [REQ: META-CODEX-008] Curses met in this run join the Codex.
            ModifierDiscoveryService.MergeCursesIntoMeta(meta, liveRunState?.CursesSeenThisRun);
            // [REQ: META-CODEX-009] This run's item uses join the Codex's counts (only here, so autosaves never count twice).
            CountItemUses(meta, liveRunState?.ItemTypesUsedThisRun);
            // 9d. The other achievements, once XP, unlocks and Codex finds are counted [ACHIEVE-RUN-001].
            var maxClassLevel = 0;
            foreach (ClassId id in Enum.GetValues(typeof(ClassId))) maxClassLevel = Math.Max(maxClassLevel, _garden.GetLevel(meta, id));
            SteamAchievementService.EvaluateProgressAchievements(meta, stats, mastery, result, liveRunState?.CurrentHP ?? -1,
                maxClassLevel, envelope.DailyGoals?.CurrentStreak ?? 0);

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
