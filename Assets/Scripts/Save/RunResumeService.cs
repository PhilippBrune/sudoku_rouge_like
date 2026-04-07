using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunResumeService
    {
        private readonly SaveFileService _saveFile;

        public RunResumeService()
        {
            _saveFile = new SaveFileService();
        }

        public RunResumeService(SaveFileService saveFile)
        {
            _saveFile = saveFile;
        }

        public bool HasActiveRun()
        {
            return _saveFile.HasActiveRun();
        }

        public bool TryResumeFromSave(out RunState runState, out PuzzleSaveState puzzleState)
        {
            runState = null;
            puzzleState = null;

            if (!_saveFile.HasSaveFile()) return false;

            var envelope = _saveFile.Load();
            if (envelope.ActiveRunState == null) return false;

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

        private static void RestoreTransientState(RunState runState)
        {
            runState.SyncSeenModifiersFromList();
        }
    }
}
