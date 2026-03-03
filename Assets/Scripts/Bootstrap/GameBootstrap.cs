using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField] private ClassId startingClass = ClassId.NumberFreak;
        [SerializeField] private int runNumber = 1;
        [SerializeField] private bool resumeRunIfAvailable = true;

        private readonly SaveFileService _saveFileService = new();
        private readonly ProfileService _profileService = new();
        private readonly RunResumeService _resumeService = new();

        private RunAutoSaveCoordinator _autoSave;
        private RunDirector _run;

        private void Start()
        {
            _autoSave = new RunAutoSaveCoordinator(_saveFileService, _profileService);

            if (resumeRunIfAvailable && _saveFileService.TryLoadRun(out var runEnvelope))
            {
                _profileService.ApplyEnvelope(runEnvelope);
                _run = new RunDirector(seed);
                if (_resumeService.TryResumeFromSave(_run, runEnvelope))
                {
                    _autoSave.Bind(_run);
                    Debug.Log($"Run resumed for {_run.RunState.ClassId}. HP={_run.RunState.CurrentHP}, Pencil={_run.RunState.CurrentPencil}");
                    return;
                }
            }

            _run = new RunDirector(seed);
            _run.StartRun(startingClass, runNumber: runNumber, meta: _profileService.Meta);
            _autoSave.Bind(_run);

            var levelConfig = _run.BuildLevelConfig(runNumber, depth: 1);
            _run.StartLevel(levelConfig);

            Debug.Log($"Run started with {_run.RunState.ClassId}. HP={_run.RunState.CurrentHP}, Pencil={_run.RunState.CurrentPencil}");
            Debug.Log($"Level size={levelConfig.BoardSize}, stars={levelConfig.Stars}, missing={levelConfig.MissingPercent:P0}");
        }

        public void DebugCompleteLevel()
        {
            _run.CompleteLevelAndGrantRewards();
            Debug.Log($"Rewards applied. Gold={_run.RunState.CurrentGold}, XP={_run.RunState.CurrentXP}, Level={_run.RunState.Level}");
        }
    }
}
