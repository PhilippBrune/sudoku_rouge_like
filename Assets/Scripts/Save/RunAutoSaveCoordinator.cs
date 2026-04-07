using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunAutoSaveCoordinator
    {
        private readonly SaveFileService _saveFile;
        private readonly ProfileService _profile;
        private RunDirector _boundRun;

        public RunAutoSaveCoordinator(SaveFileService saveFile)
        {
            _saveFile = saveFile;
        }

        public RunAutoSaveCoordinator(SaveFileService saveFile, ProfileService profile)
        {
            _saveFile = saveFile;
            _profile = profile;
        }

        public void Bind(RunDirector run)
        {
            _boundRun = run;
        }

        public void Save(RunState runState, PuzzleSaveState puzzleState)
        {
            // Tutorial runs are not persisted — no resume, no progression
            if (runState == null || runState.TutorialMode) return;

            runState.SyncSeenModifiersToList();

            var envelope = _saveFile.Load();
            envelope.ActiveRunState = runState;
            envelope.ActivePuzzle = puzzleState;
            _saveFile.Save(envelope);
        }

        public void SaveBound()
        {
            if (_boundRun == null) return;
            var puzzle = _boundRun.ExportPuzzleSaveState();
            Save(_boundRun.State, puzzle);
        }

        public void ClearActiveRun()
        {
            var envelope = _saveFile.Load();
            envelope.ActiveRunState = null;
            envelope.ActivePuzzle = null;
            _saveFile.Save(envelope);
        }

        public void SaveOnTrigger(RunSaveTrigger trigger, RunState runState, PuzzleSaveState puzzleState)
        {
            Save(runState, puzzleState);
        }
    }
}
