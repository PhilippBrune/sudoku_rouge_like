using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunResumeService
    {
        private readonly SaveFileService _saveFile;

        public RunResumeService()
        {
            _saveFile = new SaveFileService(SaveProfileService.ActiveSlot);
        }

        public RunResumeService(SaveFileService saveFile)
        {
            _saveFile = saveFile;
        }

        /// <summary>
        /// Ends the saved Garden Run as a defeat before a new run replaces it: its XP and progress are recorded and
        /// the save is cleared. Returns the recorded result, or null when there is no saved run (GAP-032).
        /// </summary>
        // [REQ: REQ-RUN-101] Abandoning a run records its result like any other defeat.
        public RunResult AbandonSavedRun(ProfileService profile)
        {
            if (profile == null || !TryResumeFromSave(out var runState, out var puzzleState)) return null;
            var abandoned = new RunDirector();
            // The open puzzle is restored too, so a run left inside it grades that puzzle Messy (SCORE-GRADE-002).
            ApplyToRunDirector(abandoned, runState, puzzleState);
            var result = RunResultFactory.Build(abandoned, victory: false, bossPhaseReached: 0, secondsPlayed: 0);
            profile.RecordRunAndGetNewUnlocks(result, abandoned.State);
            if (abandoned.State.DailyWalkScored) profile.SettleDailyWalk(null, result.XpEarned);   // [REQ: DAILY-WALK-003]
            return result;
        }

        public bool HasActiveRun()
        {
            return _saveFile.HasActiveRun();
        }

        public bool HasActiveSeasonalRun(int year, int month)
        {
            return _saveFile.HasActiveSeasonalRun(year, month);
        }

        /// <summary>
        /// Attempts to restore a saved Seasonal Challenge puzzle for the given month.
        /// Returns false if no matching save exists.
        /// </summary>
        public bool TryResumeSeasonalFromSave(int year, int month,
            out RunState runState, out PuzzleSaveState puzzleState)
        {
            runState = null;
            puzzleState = null;

            if (!_saveFile.HasSaveFile()) return false;
            var envelope = _saveFile.Load();
            var saved = envelope.ActiveSeasonalRunState;
            if (!SaveFileService.IsResumableSeasonalRunState(saved) || saved.Seed != year * 100 + month) return false;

            runState = saved;
            puzzleState = SaveFileService.IsUsablePuzzleSave(envelope.ActiveSeasonalPuzzle)
                ? envelope.ActiveSeasonalPuzzle
                : null;
            RestoreTransientState(runState);
            return true;
        }

        // [REQ: SAVE-RESUME-001] Restores full run state including boss modifier, AllowIrregularPuzzles
        public bool TryResumeFromSave(out RunState runState, out PuzzleSaveState puzzleState)
        {
            runState = null;
            puzzleState = null;

            if (!_saveFile.HasSaveFile()) return false;

            var envelope = _saveFile.Load();
            if (!SaveFileService.IsResumableRunState(envelope.ActiveRunState)) return false;

            runState = envelope.ActiveRunState;
            puzzleState = SaveFileService.IsUsablePuzzleSave(envelope.ActivePuzzle)
                ? envelope.ActivePuzzle
                : null;

            RestoreTransientState(runState);
            return true;
        }

        /// <summary>
        /// Resume a run from a save envelope into an existing RunDirector.
        /// Used by RunMapController.ResumeFromEnvelope().
        /// </summary>
        public bool TryResumeFromSave(RunDirector run, SaveFileEnvelope envelope)
        {
            if (run == null || envelope == null) return false;
            if (!SaveFileService.IsResumableRunState(envelope.ActiveRunState)) return false;

            var runState = envelope.ActiveRunState;
            RestoreTransientState(runState);

            var puzzle = SaveFileService.IsUsablePuzzleSave(envelope.ActivePuzzle) ? envelope.ActivePuzzle : null;
            ApplyToRunDirector(run, runState, puzzle);
            return true;
        }

        /// <summary>
        /// Rebuilds a RunDirector from a loaded run state and optional mid-puzzle save. This is the one
        /// resume sequence used by GameBootstrap.LaunchResume, so tests can drive the exact game path.
        /// </summary>
        // [REQ: SAVE-RESUME-001] Services re-seeded, state restored, floor graph and node position rebuilt,
        // then the puzzle (board, rules, overlay, counters, pressure) restored (GAP-001)
        public static void ApplyToRunDirector(RunDirector run, RunState runState, PuzzleSaveState puzzleState)
        {
            var request = new LaunchRequest
            {
                ClassId = runState.ClassId,
                Mode = runState.Mode,
                AllowIrregularPuzzles = runState.AllowIrregularPuzzles,
                HarmonyLevel = runState.HarmonyLevel,
                HarmonyPerk = runState.HarmonyPerk,
                SelectedTier = runState.Mode == GameMode.SpiritTrials ? runState.SpiritTrialsTier : null
            };

            // Re-initialize services with the run's seed, then overlay saved progress
            // (HP, floor, items, relics, curses, NodePath, etc.)
            run.StartRun(request, runState.Seed);
            run.RestoreState(runState);
            var savedNodeIndex = runState.CurrentNodeIndex;
            run.RebuildFloorGraph(); // rebuild for the restored floor/path state
            run.State.CurrentNodeIndex = savedNodeIndex;

            if (puzzleState != null)
                run.TryRestorePuzzleSaveState(puzzleState);
        }

        // [REQ: RELIC-SLOT-003] Legacy migration: if HasRelic + HeldRelic present but HeldRelics empty, migrate single relic into list
        private static void RestoreTransientState(RunState runState)
        {
            runState.ActiveFloorModifiers ??= new System.Collections.Generic.List<BossModifierId>();
            runState.ChosenBossModifiers  ??= new System.Collections.Generic.List<BossModifierId>();
            runState.HeldRelics           ??= new System.Collections.Generic.List<RelicInstance>();
            runState.FailedGenerationSeeds ??= new System.Collections.Generic.List<int>();

            if (runState.SeenBossModifierList == null)
                runState.SeenBossModifierList = new System.Collections.Generic.List<BossModifierId>();
            if (runState.SeenBossModifiers == null)
                runState.SeenBossModifiers = new System.Collections.Generic.HashSet<BossModifierId>();
            runState.SyncSeenModifiersFromList();
            runState.HarmonyConfig = HarmonyDifficultyService.BuildConfig(runState.HarmonyLevel);

            // Migrate legacy single-relic save data to HeldRelics list
            if (runState.HasRelic && runState.HeldRelic != null
                && (runState.HeldRelics == null || runState.HeldRelics.Count == 0))
            {
                if (runState.HeldRelics == null) runState.HeldRelics = new System.Collections.Generic.List<RelicInstance>();
                runState.HeldRelics.Add(runState.HeldRelic);
            }

            // Migrate legacy single boss modifier → new list.
            // Old saves used HasChosenBossModifier + ChosenBossModifierId; new code reads ChosenBossModifiers list.
            if (runState.HasChosenBossModifier
                && (runState.ChosenBossModifiers == null || runState.ChosenBossModifiers.Count == 0))
            {
                if (runState.ChosenBossModifiers == null)
                    runState.ChosenBossModifiers = new System.Collections.Generic.List<BossModifierId>();
                runState.ChosenBossModifiers.Add(runState.ChosenBossModifierId);
            }
        }
    }
}
