using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;
using SudokuRoguelike.UI;
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

            if (_saveFileService.TryLoadProfile(out var profileEnvelope))
            {
                _profileService.ApplyEnvelope(profileEnvelope);
            }

            if (LaunchRequestContext.TryConsume(out var launchRequest))
            {
                _run = new RunDirector(seed);
                _autoSave.Bind(_run);

                if (launchRequest.Mode == GameMode.Tutorial)
                {
                    var setup = launchRequest.TutorialSetup ?? new TutorialSetupConfig
                    {
                        BoardSize = 5,
                        Stars = 1,
                        ResourceMode = TutorialResourceMode.Simulation
                    };

                    var validation = TutorialModeService.ValidateSetup(setup);
                    if (!validation.IsValid)
                    {
                        Debug.LogWarning($"Tutorial setup invalid from launch request: {validation.Message}. Falling back to 5x5 1★ Simulation.");
                        setup = new TutorialSetupConfig
                        {
                            BoardSize = 5,
                            Stars = 1,
                            ResourceMode = TutorialResourceMode.Simulation
                        };
                    }

                    _run.StartTutorialRun(setup);
                    BindRuntimeControllers();

                    Debug.Log("Tutorial run started from Main Menu.");
                    Debug.Log($"Tutorial state: HP={_run.RunState.CurrentHP}, Pencil={_run.RunState.CurrentPencil}");
                    return;
                }

                try
                {
                    _run.StartRun(launchRequest.ClassId, launchRequest.Mode, runNumber: runNumber, meta: _profileService.Meta);
                }
                catch
                {
                    _run.StartRun(ClassId.NumberFreak, launchRequest.Mode, runNumber: runNumber, meta: _profileService.Meta);
                    Debug.LogWarning($"Launch request class {launchRequest.ClassId} was unavailable. Fallback to Number Freak.");
                }
                var requestedLevel = _run.BuildLevelConfig(runNumber, depth: 1);
                _run.StartLevel(requestedLevel);
                BindRuntimeControllers();
                Debug.Log($"Run started from launch request. Mode={launchRequest.Mode}, Class={launchRequest.ClassId}");
                return;
            }

            if (resumeRunIfAvailable && _saveFileService.TryLoadRun(out var runEnvelope))
            {
                _profileService.ApplyEnvelope(runEnvelope);
                _run = new RunDirector(seed);
                if (_resumeService.TryResumeFromSave(_run, runEnvelope))
                {
                    _autoSave.Bind(_run);
                    BindRuntimeControllers();
                    Debug.Log($"Run resumed for {_run.RunState.ClassId}. HP={_run.RunState.CurrentHP}, Pencil={_run.RunState.CurrentPencil}");
                    return;
                }
            }

            _run = new RunDirector(seed);
            try
            {
                _run.StartRun(startingClass, runNumber: runNumber, meta: _profileService.Meta);
            }
            catch
            {
                _run.StartRun(ClassId.NumberFreak, runNumber: runNumber, meta: _profileService.Meta);
                Debug.LogWarning($"Starting class {startingClass} unavailable. Fallback to Number Freak.");
            }
            _autoSave.Bind(_run);

            var levelConfig = _run.BuildLevelConfig(runNumber, depth: 1);
            _run.StartLevel(levelConfig);
            BindRuntimeControllers();

            Debug.Log($"Run started with {_run.RunState.ClassId}. HP={_run.RunState.CurrentHP}, Pencil={_run.RunState.CurrentPencil}");
            Debug.Log($"Level size={levelConfig.BoardSize}, stars={levelConfig.Stars}, missing={levelConfig.MissingPercent:P0}");
        }

        public void DebugCompleteLevel()
        {
            _run.CompleteLevelAndGrantRewards();

            if (_run.RunState != null && _run.RunState.TutorialMode && _run.TryConsumeLastCompletedTutorialSetup(out var completedSetup))
            {
                var tutorialProgress = new TutorialProgressService(_profileService.TutorialProgress);
                tutorialProgress.MarkCompleted(completedSetup);

                var envelope = new SaveFileEnvelope
                {
                    PlayerProfile = new ProfileSaveData { Options = _profileService.Options },
                    MetaProgress = _profileService.Meta,
                    TutorialProgress = _profileService.TutorialProgress,
                    Statistics = _profileService.Stats,
                    Mastery = _profileService.Mastery,
                    Completion = _profileService.Completion
                };

                _saveFileService.SaveProfile(envelope);
                Debug.Log($"Tutorial completion saved: {TutorialModeService.BuildCompletionKey(completedSetup)}");
            }

            Debug.Log($"Rewards applied. Gold={_run.RunState.CurrentGold}, XP={_run.RunState.CurrentXP}, Level={_run.RunState.Level}");
        }

        private void BindRuntimeControllers()
        {
            if (_run == null)
            {
                return;
            }

            var runMapController = FindFirstObjectByType<RunMapController>();
            runMapController?.BindRun(_run);

            var inputController = FindFirstObjectByType<PrototypeInputController>();
            inputController?.Bind(_run);

            var pauseController = FindFirstObjectByType<PauseRunController>();
            pauseController?.Bind(_run);

            var shopController = FindFirstObjectByType<ShopController>();
            shopController?.Bind(_run);
        }
    }
}
