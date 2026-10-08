using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunAutoSaveCoordinator
    {
        private readonly SaveFileService _saveFile;
        private RunDirector _boundRun;

        public RunAutoSaveCoordinator(SaveFileService saveFile)
        {
            _saveFile = saveFile;
        }

        public RunAutoSaveCoordinator(SaveFileService saveFile, ProfileService profile)
        {
            _saveFile = saveFile;
        }

        public void Bind(RunDirector run)
        {
            _boundRun = run;
        }

        // [REQ: SAVE-AUTO-001] Atomic write before serializing run state
        // [REQ: SAVE-TUTO-001] Tutorial runs are not persisted
        public void Save(RunState runState, PuzzleSaveState puzzleState)
        {
            if (runState == null || runState.TutorialMode) return;

            // Seasonal challenge saves go to a separate slot — do NOT block on DisableProgressionRewards.
            // For all other modes (e.g. SpiritTrials which also sets DisableProgressionRewards), skip.
            if (!runState.IsSeasonalChallenge && runState.DisableProgressionRewards) return;

            runState.SyncSeenModifiersToList();

            // [REQ: SAVE-AUTO-001] [REQ: META-CODEX-001] Reload the file before every autosave. Other services write it
            // during a run (codex discoveries, options, profile); writing a copy loaded earlier erased their changes
            // (GAP-037). The coordinator only replaces the fields it owns: active run, puzzle and in-run daily goals.
            var envelope = _saveFile.HasSaveFile() ? _saveFile.Load() : new SaveFileEnvelope();

            // Accumulate cross-run modifier discovery into meta progression
            if (envelope.MetaProgress != null)
            {
                foreach (var mod in runState.SeenBossModifiers)
                {
                    if (!envelope.MetaProgress.DiscoveredBossModifiers.Contains(mod))
                        envelope.MetaProgress.DiscoveredBossModifiers.Add(mod);
                }
            }

            if (runState.IsSeasonalChallenge)
            {
                envelope.ActiveSeasonalRunState = runState;
                envelope.ActiveSeasonalPuzzle = puzzleState;
            }
            else
            {
                envelope.ActiveRunState = runState;
                envelope.ActivePuzzle = puzzleState;
            }

            // Persist daily goal state if it's been updated in-run
            if (_boundRun?.DailyGoals != null)
                envelope.DailyGoals = _boundRun.DailyGoals;

            UnityEngine.Debug.Log(
                $"[AutoSave] HP={runState.CurrentHP} Pencil={runState.CurrentPencil} " +
                $"Items={runState.HeldItems?.Count ?? 0} NodeIdx={runState.CurrentNodeIndex} " +
                $"Floor={runState.CurrentFloor} RunNum={runState.RunNumber}");
            _saveFile.Save(envelope);
        }

        public void SaveBound()
        {
            if (_boundRun == null) return;
            var puzzle = _boundRun.ExportPuzzleSaveState();
            Save(_boundRun.State, puzzle);
            // [REQ: PRESSURE-SAVE-003] Pressure mechanics (CountdownFill, HauntedCell, CrumblingRegion, PressureWave)
            // are [NonSerialized] fields on LevelState and are not persisted. On resume,
            // RunDirector.RestoreLevelRulesAfterResume re-runs InitializePressureMechanics with the level seed,
            // so the threats start fresh after a Save & Quit (PRESSURE-SAVE-002).
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
