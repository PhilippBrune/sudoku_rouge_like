using System;
using UnityEngine;
using UnityEngine.EventSystems;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Tutorial;
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
        private RunAudioController  _runAudio;

        public RunDirector Run => _run;
        public ProfileService Profile => _profileService;
        public SaveFileService SaveFile => _saveFileService;
        public ScreenManager Screen => _screenManager;
        public RunAutoSaveCoordinator AutoSave => _autoSave;

        private SaveProfileService _profileSlots;
        private DailyGoalState _dailyGoals;

        private SplashScreenController _splashInline;

        private void Awake()
        {
            _profileSlots = new SaveProfileService();
            _saveFileService = new SaveFileService(SaveProfileService.ActiveSlot);
            _profileService = new ProfileService(_saveFileService);
            _resumeService = new RunResumeService(_saveFileService);
            _autoSave = new RunAutoSaveCoordinator(_saveFileService);
            _run = new RunDirector();

            // Create the splash in Awake so it covers the screen before any Start() builds game UI.
            if (!SplashSceneBootstrap.HasShownSplash)
            {
                var splashGo = new GameObject("SplashScreenController");
                _splashInline = splashGo.AddComponent<SplashScreenController>();
                _splashInline.Initialize();
            }
        }

        public SaveProfileService ProfileSlots => _profileSlots;

        /// <summary>
        /// Re-wires all save/profile services to the newly selected slot.
        /// Call after SaveProfileService.ActiveSlot has been updated.
        /// </summary>
        public void ApplySlotChange()
        {
            _saveFileService = new SaveFileService(SaveProfileService.ActiveSlot);
            _profileService = new ProfileService(_saveFileService);
            _resumeService = new RunResumeService(_saveFileService);
            _autoSave = new RunAutoSaveCoordinator(_saveFileService);

            var envelope = _saveFileService.Load();
            _dailyGoals = envelope.DailyGoals ?? new DailyGoalState();
            DailyGoalService.RefreshIfNewDay(_dailyGoals, DateTime.Today);
            _run.DailyGoals = _dailyGoals;
        }

        private void Start()
        {
            Application.runInBackground = true;
            LocalizationService.SetLanguage(_profileService.LoadOptions().Language);

            EnsureSceneInfrastructure();

            // Load daily goals and refresh if it's a new day
            var envelope = _saveFileService.Load();
            _dailyGoals = envelope.DailyGoals ?? new DailyGoalState();
            DailyGoalService.RefreshIfNewDay(_dailyGoals, DateTime.Today);
            _run.DailyGoals = _dailyGoals;

            // Persist refreshed state immediately so streak updates aren't lost
            envelope.DailyGoals = _dailyGoals;
            _saveFileService.Save(envelope);

            if (resumeRunIfAvailable && _resumeService.HasActiveRun())
            {
                Debug.Log("[GameBootstrap] Resumable run found.");
            }

            // If a dedicated Splash scene already ran (build index 0), skip the
            // inline splash and go straight to menu. Otherwise show it inline so
            // the game still works when the main scene is launched directly.
            void ShowMenuAfterSplash()
            {
                _screenManager.ShowMenu();
                _menuMusic.Play();

                // A — Fade main menu in smoothly after splash
                var menuRoot = GameObject.Find("MainMenuRoot");
                if (menuRoot != null)
                {
                    var menuCg = menuRoot.GetComponent<CanvasGroup>();
                    if (menuCg != null) StartCoroutine(AnimationHelper.FadeIn(menuCg, 0.28f));
                }

                // If no save files exist at all → jump straight to profile select so the
                // player picks / creates their first profile before hitting the main menu.
                var mc = FindAnyObjectByType<MainMenuController>();
                if (mc == null) return;

                if (!_profileSlots.AnySlotExists())
                {
                    mc.ShowProfileSelect();
                }
                else
                {
                    mc.ShowMainMenu();
                    // [REQ: TUTO-BASICS-TRIGGER-001] trigger if "sudoku_basics" not in CompletedKeys
                    // [REQ: TUTO-BASICS-TRIGGER-002] only triggers once (skipped on subsequent launches)
                    // [REQ: TUTO-BASICS-TRIGGER-003] prompt appears after main menu loads
                    if (!mc.HasCompletedSudokuBasics())
                        mc.ShowSudokuBasicsPrompt();
                }
            }

            if (SplashSceneBootstrap.HasShownSplash)
            {
                ShowMenuAfterSplash();
            }
            else
            {
                _menuMusic.Play();
                _splashInline.Show(ShowMenuAfterSplash);
            }
        }

        // ── Launch Methods ──

        public void LaunchRun(LaunchRequest request)
        {
            var runtimeSeed = BuildRuntimeSeed(seed);
            _menuMusic.Stop();

            // Populate class level from meta progression so stat bonuses are applied correctly
            var meta = _profileService.LoadMetaProgress();
            request.ClassLevel = new ClassGardenProgressionService().GetLevel(meta, request.ClassId);

            _autoSave.ClearActiveRun();
            _run.StartRun(request, runtimeSeed);
            _run.DailyGoals = _dailyGoals;

            // Apply any pending run-start rewards from completed daily goals
            if (_dailyGoals != null)
                DailyGoalService.ApplyPendingRewards(_dailyGoals, _run.State);

            _screenManager.ShowGame();
            BindRunToMap();

            Debug.Log($"[GameBootstrap] Launched {request.Mode} as {request.ClassId}, seed={runtimeSeed}");
        }

        public void LaunchSeasonalChallenge()
        {
            var now = DateTime.Today;
            var runState = SeasonalChallengeService.CreateChallengeRunState(now.Year, now.Month);
            var levelConfig = SeasonalChallengeService.BuildChallengeConfig(now.Year, now.Month);

            _menuMusic.Stop();
            _run.RestoreState(runState);
            _run.DailyGoals = null; // no daily goal tracking in seasonal
            _run.StartSeasonalChallenge(levelConfig);

            _screenManager.ShowGame();
            BindRunToMap();

            Debug.Log($"[GameBootstrap] Launched Seasonal Challenge {now.Year}-{now.Month:D2}");
        }

        public void LaunchTutorial(TutorialSetupConfig setup)
        {
            var runtimeSeed = BuildRuntimeSeed(seed);
            _menuMusic.Stop();
            _run.StartTutorialRun(setup, runtimeSeed);
            _screenManager.ShowGame();
            BindRunToMap();

            Debug.Log($"[GameBootstrap] Launched tutorial, size={setup.BoardSize}, stars={setup.Stars}");
        }

        /// <summary>
        /// Launches the Sudoku Basics first-time tutorial (RunNumber=0) using the fixed
        /// hand-crafted 4×4 board. Always produces the same layout regardless of seed.
        /// </summary>
        // [REQ: TUTO-BASICS-DONE-002] Replayable via Options → "Replay Sudoku Basics" button
        public void LaunchSudokuBasicsTutorial()
        {
            _menuMusic.Stop();

            var tutService = new TutorialModeService();
            var level = tutService.BuildSudokuBasicsLevel();
            var state = TutorialModeService.CreateSudokuBasicsRunState();

            _run.StartSudokuBasicsTutorial(state, level);

            _screenManager.ShowGame();
            BindRunToMap();

            Debug.Log("[GameBootstrap] Launched Sudoku Basics tutorial.");
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

            // Re-initialize services with the run's seed, then overlay saved progress
            // (HP, floor, items, relics, curses, NodePath, etc.)

            _run.StartRun(request, runState.Seed);
            _run.RestoreState(runState);
            var savedNodeIndex = runState.CurrentNodeIndex;
            _run.RebuildFloorGraph(); // rebuild for the restored floor/path state
            _run.State.CurrentNodeIndex = savedNodeIndex;

            if (puzzleState != null)
                _run.TryRestorePuzzleSaveState(puzzleState);

            _screenManager.ShowGame();
            BindRunToMap();
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
                // Inject the correct SaveFileService so saves go to the active profile slot.
                map.BindRun(_run, _saveFileService);

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
            _runAudio?.StopAll();
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

            // Run audio: must be a root GameObject so DontDestroyOnLoad works.
            // Do NOT parent it — FindAnyObjectByType locates it from anywhere.
            _runAudio = FindAnyObjectByType<RunAudioController>();
            if (_runAudio == null)
            {
                var audioGo = new GameObject("RunAudioController");
                audioGo.AddComponent<AudioSource>();
                _runAudio = audioGo.AddComponent<RunAudioController>();
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
