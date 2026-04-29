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

        public bool HasActiveRun()
        {
            return _saveFile.HasActiveRun();
        }

        // [REQ: SAVE-RESUME-001] Restores full run state including boss modifier, AllowIrregularPuzzles
        public bool TryResumeFromSave(out RunState runState, out PuzzleSaveState puzzleState)
        {
            runState = null;
            puzzleState = null;

            if (!_saveFile.HasSaveFile()) return false;

            var envelope = _saveFile.Load();
            if (envelope.ActiveRunState == null) return false;
            if (envelope.ActiveRunState.DisableProgressionRewards) return false;

            runState = envelope.ActiveRunState;
            puzzleState = envelope.ActivePuzzle;

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
            if (envelope.ActiveRunState == null) return false;
            if (envelope.ActiveRunState.DisableProgressionRewards) return false;

            var runState = envelope.ActiveRunState;
            RestoreTransientState(runState);

            // Initialize services via a throwaway StartRun, then replace state
            var request = new LaunchRequest
            {
                ClassId = runState.ClassId,
                Mode = runState.Mode,
                AllowIrregularPuzzles = runState.AllowIrregularPuzzles
            };
            run.StartRun(request, runState.Seed);
            run.RestoreState(runState);

            // Rebuild floor graph for current floor
            run.RebuildFloorGraph();

            // Restore puzzle if available
            if (envelope.ActivePuzzle != null)
                run.TryRestorePuzzleSaveState(envelope.ActivePuzzle);

            return true;
        }

        // [REQ: RELIC-SLOT-003] Legacy migration: if HasRelic + HeldRelic present but HeldRelics empty, migrate single relic into list
        private static void RestoreTransientState(RunState runState)
        {
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
