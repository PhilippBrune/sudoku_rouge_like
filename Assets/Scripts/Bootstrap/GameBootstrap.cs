using System;
using UnityEngine;
using UnityEngine.EventSystems;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool resumeRunIfAvailable = true;

        private SaveFileService _saveFileService;
        private ProfileService _profileService;
        private RunResumeService _resumeService;
        private RunAutoSaveCoordinator _autoSave;
        private RunDirector _run;
        private ScreenManager _screenManager;
        private MenuMusicController _menuMusic;

        public RunDirector Run => _run;
        public ProfileService Profile => _profileService;
        public SaveFileService SaveFile => _saveFileService;
        public ScreenManager Screen => _screenManager;
        public RunAutoSaveCoordinator AutoSave => _autoSave;

        private void Awake()
        {
            _saveFileService = new SaveFileService();
            _profileService = new ProfileService(_saveFileService);
            _resumeService = new RunResumeService(_saveFileService);
            _autoSave = new RunAutoSaveCoordinator(_saveFileService);
            _run = new RunDirector();
        }

        private void Start()
        {
            Application.runInBackground = true;
            LocalizationService.SetLanguage(_profileService.LoadOptions().Language);
            EnsureSceneInfrastructure();

            if (resumeRunIfAvailable && _resumeService.HasActiveRun())
            {
                Debug.Log("[GameBootstrap] Resumable run found.");
            }

            _screenManager.ShowMenu();
            _menuMusic.Play();

            // Ensure main menu panel is visible (MainMenuController.Start() runs next frame)
            var mc = FindAnyObjectByType<MainMenuController>();
            if (mc != null) mc.ShowMainMenu();
        }

        // ── Launch Methods ──

        public void LaunchRun(LaunchRequest request)
        {
            var runtimeSeed = BuildRuntimeSeed(seed);
            _menuMusic.Stop();

            _run.StartRun(request, runtimeSeed);
            BindRunToMap();
            _screenManager.ShowGame();

            Debug.Log($"[GameBootstrap] Launched {request.Mode} as {request.ClassId}, seed={runtimeSeed}");
        }

        public void LaunchTutorial(TutorialSetupConfig setup)
        {
            var runtimeSeed = BuildRuntimeSeed(seed);
            _menuMusic.Stop();
            _run.StartTutorialRun(setup, runtimeSeed);
            BindRunToMap();
            _screenManager.ShowGame();

            Debug.Log($"[GameBootstrap] Launched tutorial, size={setup.BoardSize}, stars={setup.Stars}");
        }

        public bool LaunchResume()
        {
            if (!_resumeService.TryResumeFromSave(out var runState, out var puzzleState))
                return false;

            _menuMusic.Stop();

            var request = new LaunchRequest
            {
                ClassId = runState.ClassId,
                Mode = runState.Mode,
                AllowIrregularPuzzles = runState.AllowIrregularPuzzles
            };

            _run.StartRun(request, runState.Seed);

            if (puzzleState != null)
                _run.TryRestorePuzzleSaveState(puzzleState);

            BindRunToMap();
            _screenManager.ShowGame();
            Debug.Log("[GameBootstrap] Resumed run from save.");
            return true;
        }

        private void BindRunToMap()
        {
            var map = FindAnyObjectByType<RunMapController>();
            if (map != null)
            {
                // Wire the run — do NOT start a level here; the path overview shows first
                // and the player clicks a node to start the first puzzle.
                map.BindRun(_run);

                var runScreen = FindAnyObjectByType<InRunController>();
                runScreen?.NotifyRunStarted();
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] RunMapController not found — run won't display.");
            }
        }

        public void ReturnToMenu()
        {
            _screenManager.ShowMenu();
            _menuMusic.Play();
        }

        public void ShowEndScreen()
        {
            _screenManager.ShowEndScreen();
        }

        public bool HasResumableRun()
        {
            return _resumeService.HasActiveRun();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            var opts = _profileService.LoadOptions();
            if (opts.Audio.MuteWhenUnfocused)
                AudioListener.pause = !hasFocus;
            else
                AudioListener.pause = false;
        }

        // ── Infrastructure ──

        private void EnsureSceneInfrastructure()
        {
            // Camera
            if (Camera.main == null)
            {
                var camGo = new GameObject("MainCamera");
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.backgroundColor = new Color(0.08f, 0.06f, 0.12f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camGo.AddComponent<AudioListener>();
            }

            // EventSystem
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // ScreenManager
            _screenManager = FindAnyObjectByType<ScreenManager>();
            if (_screenManager == null)
            {
                var smGo = new GameObject("ScreenManager");
                _screenManager = smGo.AddComponent<ScreenManager>();
            }

            // Panel groups
            var menuGroup = EnsureGroup("MenuGroup");
            var gameGroup = EnsureGroup("GameGroup");
            var endGroup = EnsureGroup("EndScreenGroup");
            _screenManager.SetGroups(menuGroup, gameGroup, endGroup);

            // Attach blueprint builders and build UI immediately
            var menuBuilder = menuGroup.GetComponent<MainMenuBlueprintBuilder>();
            if (menuBuilder == null)
                menuBuilder = menuGroup.AddComponent<MainMenuBlueprintBuilder>();
            menuBuilder.Build();

            var gameBuilder = gameGroup.GetComponent<InRunUiBlueprintBuilder>();
            if (gameBuilder == null)
                gameBuilder = gameGroup.AddComponent<InRunUiBlueprintBuilder>();
            gameBuilder.BuildBlueprint();
#if UNITY_EDITOR
            if (gameGroup.GetComponent<DebugHotkeys>() == null)
                gameGroup.AddComponent<DebugHotkeys>();
#endif

            // Menu music
            _menuMusic = menuGroup.GetComponent<MenuMusicController>();
            if (_menuMusic == null)
                _menuMusic = menuGroup.AddComponent<MenuMusicController>();
            _menuMusic.Initialize();

            // Run audio (created on game group, plays during runs)
            var runAudio = gameGroup.GetComponent<RunAudioController>();
            if (runAudio == null)
            {
                var audioGo = new GameObject("RunAudioController");
                audioGo.transform.SetParent(gameGroup.transform, false);
                audioGo.AddComponent<AudioSource>();
                runAudio = audioGo.AddComponent<RunAudioController>();
            }
        }

        private static GameObject EnsureGroup(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;
            return new GameObject(name);
        }

        private static int BuildRuntimeSeed(int baseSeed)
        {
            return baseSeed + (int)(DateTime.UtcNow.Ticks % 100000);
        }
    }
}
