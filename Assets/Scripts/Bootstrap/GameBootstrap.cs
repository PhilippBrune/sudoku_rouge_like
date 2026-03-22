using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SudokuRoguelike.Bootstrap
{
    /// <summary>
    /// Single entry point for the entire game. The app has one scene ("Game").
    /// GameBootstrap starts in menu mode and transitions between menu, gameplay,
    /// and end screen by showing/hiding panel groups via ScreenManager.
    /// Creates Camera, EventSystem, ScreenManager, and menu components at runtime
    /// if they are not already present in the scene.
    /// </summary>
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
        private ScreenManager _screenManager;

        public RunDirector Run => _run;
        public ProfileService Profile => _profileService;
        public SaveFileService SaveFile => _saveFileService;

        private void Start()
        {
            EnsureSceneInfrastructure();

            _autoSave = new RunAutoSaveCoordinator(_saveFileService, _profileService);
            _screenManager = FindFirstObjectByType<ScreenManager>();

            // Load persistent profile
            if (_saveFileService.TryLoadProfile(out var profileEnvelope))
            {
                _profileService.ApplyEnvelope(profileEnvelope);
            }

            // Start in menu mode
            _screenManager?.ShowMenu();
        }

        /// <summary>
        /// Creates all required scene infrastructure that the single-scene architecture needs.
        /// Safe to call multiple times — skips anything that already exists.
        /// </summary>
        private void EnsureSceneInfrastructure()
        {
            // 1. Camera (required for rendering)
            if (FindFirstObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                camGo.AddComponent<AudioListener>();
                Debug.Log("GameBootstrap: Created Main Camera.");
            }

            // 2. EventSystem (required for UI interaction)
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Debug.Log("GameBootstrap: Created EventSystem.");
            }

            // 3. ScreenManager (required for panel group transitions)
            if (FindFirstObjectByType<ScreenManager>() == null)
            {
                var smGo = new GameObject("ScreenManager");
                smGo.AddComponent<ScreenManager>();
                Debug.Log("GameBootstrap: Created ScreenManager.");
            }

            // 4. Menu components (MainMenuController, MainMenuBlueprintBuilder, etc.)
            if (FindFirstObjectByType<MainMenuController>() == null)
            {
                var menuGo = new GameObject("MenuSetup");
                var controller = menuGo.AddComponent<MainMenuController>();
                var builder = menuGo.AddComponent<MainMenuBlueprintBuilder>();
                menuGo.AddComponent<OptionsController>();
                menuGo.AddComponent<TutorialMenuController>();
                menuGo.AddComponent<MetaProgressionPanelController>();
                menuGo.AddComponent<GameModesPanelController>();
                menuGo.AddComponent<ItemsMenuController>();
                var autoWire = menuGo.AddComponent<MainMenuRuntimeAutoWire>();
                autoWire.Configure(controller);
                // builder auto-discovers MainMenuController via GetComponent in its Start()
                Debug.Log("GameBootstrap: Created MenuSetup with all menu components.");
            }
        }

        // ── Launch methods (called by menu controllers instead of LaunchRequestContext) ──

        /// <summary>
        /// Start a new Garden Run, Spirit Trials, or Endless Zen run.
        /// Called by MainMenuController when the player confirms class selection.
        /// </summary>
        public void LaunchRun(LaunchRequest request)
        {
            var runtimeSeed = BuildRuntimeSeed(seed);
            _run = new RunDirector(runtimeSeed);
            _autoSave.Bind(_run);

            try
            {
                _run.StartRun(request.ClassId, request.Mode, runNumber: runNumber, meta: _profileService.Meta);
            }
            catch
            {
                _run.StartRun(ClassId.NumberFreak, request.Mode, runNumber: runNumber, meta: _profileService.Meta);
                Debug.LogWarning($"Class {request.ClassId} unavailable. Fallback to Number Freak.");
            }

            _run.RunState.AllowIrregularPuzzles = request.AllowIrregularPuzzles;
            var levelConfig = _run.BuildLevelConfig(runNumber, depth: 1);
            _run.StartLevel(levelConfig);

            BindRuntimeControllers();
            _screenManager?.ShowGame();

            Debug.Log($"Run started. Mode={request.Mode}, Class={request.ClassId}");
        }

        /// <summary>
        /// Start a tutorial run.
        /// Called by MainMenuController / TutorialMenuController.
        /// </summary>
        public void LaunchTutorial(TutorialSetupConfig setup)
        {
            var validation = TutorialModeService.ValidateSetup(setup);
            if (!validation.IsValid)
            {
                Debug.LogWarning($"Tutorial setup invalid: {validation.Message}. Falling back to 5x5 1★.");
                setup = new TutorialSetupConfig
                {
                    BoardSize = 5,
                    Stars = 1,
                    ResourceMode = TutorialResourceMode.Simulation
                };
            }

            var runtimeSeed = BuildRuntimeSeed(seed);
            _run = new RunDirector(runtimeSeed);
            _autoSave.Bind(_run);
            _run.StartTutorialRun(setup);

            BindRuntimeControllers();
            _screenManager?.ShowGame();

            Debug.Log($"Tutorial started. Size={setup.BoardSize}, Stars={setup.Stars}");
        }

        /// <summary>
        /// Resume an in-progress run from save file.
        /// Called by MainMenuController when Resume Game is pressed.
        /// </summary>
        public bool LaunchResume()
        {
            if (!_saveFileService.TryLoadRun(out var resumeEnvelope))
            {
                Debug.LogWarning("Resume requested but no valid run save found.");
                return false;
            }

            _profileService.ApplyEnvelope(resumeEnvelope);
            var runtimeSeed = BuildRuntimeSeed(seed);
            _run = new RunDirector(runtimeSeed);

            if (!_resumeService.TryResumeFromSave(_run, resumeEnvelope))
            {
                Debug.LogWarning("Resume failed — save data could not be restored.");
                return false;
            }

            _autoSave.Bind(_run);
            BindRuntimeControllers();
            _screenManager?.ShowGame();

            Debug.Log($"Run resumed. Class={_run.RunState.ClassId}, HP={_run.RunState.CurrentHP}");
            return true;
        }

        /// <summary>
        /// Return to main menu after a run ends (victory, defeat, or quit).
        /// Handles post-run profile updates before showing menu.
        /// </summary>
        public void ReturnToMenu()
        {
            _run = null;
            _screenManager?.ShowMenu();
        }

        /// <summary>
        /// Show end screen (victory or defeat) after a run completes.
        /// </summary>
        public void ShowEndScreen()
        {
            _screenManager?.ShowEndScreen();
        }

        /// <summary>
        /// Check whether a valid run save exists for the Resume button.
        /// </summary>
        public bool HasResumableRun()
        {
            return _saveFileService.TryLoadRun(out _);
        }

        // ── Debug helpers ───────────────────────────────────────────────────

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

            Debug.Log($"Rewards applied. Gold={_run.RunState.CurrentGold}, XP={_run.RunState.CurrentXP}");
        }

        // ── Internal wiring ─────────────────────────────────────────────────

        private void BindRuntimeControllers()
        {
            if (_run == null) return;

            var inRunBuilder = FindFirstObjectByType<InRunUiBlueprintBuilder>();
            inRunBuilder?.BuildBlueprint();

            var runMapController = FindFirstObjectByType<RunMapController>();
            runMapController?.BindRun(_run);

            var inputController = FindFirstObjectByType<PrototypeInputController>();
            inputController?.Bind(_run);

            var pauseController = FindFirstObjectByType<PauseRunController>();
            pauseController?.Bind(_run);

            var shopController = FindFirstObjectByType<ShopController>();
            shopController?.Bind(_run);
        }

        private static int BuildRuntimeSeed(int baseSeed)
        {
            unchecked
            {
                var ticks = (int)System.DateTime.UtcNow.Ticks;
                return ticks ^ baseSeed ^ UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
        }
    }
}
