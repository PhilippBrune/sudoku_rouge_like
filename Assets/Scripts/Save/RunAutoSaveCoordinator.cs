using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunAutoSaveCoordinator
    {
        private readonly SaveFileService _saveFile;
        private readonly ProfileService _profile;
        private RunDirector _boundRun;
        // Envelope loaded once at Bind() and updated in memory — avoids disk read on every save.
        private SaveFileEnvelope _cachedEnvelope;

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
            // Load once now to capture PlayerProfile and other fields we don't own.
            _cachedEnvelope = _saveFile.HasSaveFile() ? _saveFile.Load() : new SaveFileEnvelope();
        }

        // [REQ: SAVE-AUTO-001] Atomic write before serializing run state
        // [REQ: SAVE-TUTO-001] Tutorial runs are not persisted
        public void Save(RunState runState, PuzzleSaveState puzzleState)
        {
            // Tutorial runs are not persisted — no resume, no progression
            if (runState == null || runState.TutorialMode) return;

            runState.SyncSeenModifiersToList();

            // Update cached envelope in memory — no disk read needed per save.
            if (_cachedEnvelope == null)
                _cachedEnvelope = _saveFile.HasSaveFile() ? _saveFile.Load() : new SaveFileEnvelope();

            if (runState.IsSeasonalChallenge)
            {
                _cachedEnvelope.ActiveSeasonalRunState = runState;
                _cachedEnvelope.ActiveSeasonalPuzzle = puzzleState;
            }
            else
            {
                _cachedEnvelope.ActiveRunState = runState;
                _cachedEnvelope.ActivePuzzle = puzzleState;
            }

            // Persist daily goal state if it's been updated in-run
            if (_boundRun?.DailyGoals != null)
                _cachedEnvelope.DailyGoals = _boundRun.DailyGoals;

            _saveFile.Save(_cachedEnvelope);
        }

        public void SaveBound()
        {
            if (_boundRun == null) return;
            var puzzle = _boundRun.ExportPuzzleSaveState();
            Save(_boundRun.State, puzzle);
            // NOTE — Pressure mechanics (CountdownFill, HauntedCell, CrumblingRegion, PressureWave)
            // are stored as [NonSerialized] ephemeral fields on LevelState and are NOT persisted here.
            // On resume via RunResumeService, RunDirector.StartLevel is called which re-runs
            // InitializePressureMechanics with the same config seed, reconstructing all threat state
            // from scratch. This is intentional: mid-puzzle pressure state cannot be meaningfully
            // restored, so starting fresh is the correct behaviour after a save-and-quit.
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
