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
        private readonly StartupFlowController _startupFlow = new StartupFlowController();

        public RunDirector Run => _run;
        public ProfileService Profile => _profileService;
        public SaveFileService SaveFile => _saveFileService;
        public ScreenManager Screen => _screenManager;
        public RunAutoSaveCoordinator AutoSave => _autoSave;

        private SaveProfileService _profileSlots;
        private DailyGoalState _dailyGoals;
        private bool _hadAnyProfileAtStartup;

        private SplashScreenController _splashInline;

        private void Awake()
        {
            _profileSlots = new SaveProfileService();
            _hadAnyProfileAtStartup = _profileSlots.AnySlotExists();
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
        // [REQ: SAVE-SLOT-003] ApplySlotChange: re-wires all save/profile/resume services to the newly active slot after a profile switch
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
            // Persist the refreshed daily goals to the newly active slot immediately.
            envelope.DailyGoals = _dailyGoals;
            _saveFileService.Save(envelope);
        }

        private void Start()
        {
            Application.runInBackground = true;
            SteamLeaderboardService.Initialize();
            LocalizationService.SetLanguage(_profileService.LoadOptions().Language);

            EnsureSceneInfrastructure();

            // Load daily goals and refresh if it's a new day
            var envelope = _saveFileService.Load();
            _dailyGoals = envelope.DailyGoals ?? new DailyGoalState();
            DailyGoalService.RefreshIfNewDay(_dailyGoals, DateTime.Today);
            _run.DailyGoals = _dailyGoals;

            // Only persist if we already have an active profile — avoids creating
            // save_profile_0.json before the user completes the profile-selection flow on first launch.
            if (_hadAnyProfileAtStartup)
            {
                envelope.DailyGoals = _dailyGoals;
                _saveFileService.Save(envelope);
            }

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
                // [REQ: INPUT-PROFILE-001] No save files → ShowProfileSelect() on first launch
                var mc = FindAnyObjectByType<MainMenuController>();
                if (mc == null) return;

                RunStartupFlow(mc, true);
            }

            if (SplashSceneBootstrap.HasShownSplash)
            {
                ShowMenuAfterSplash();
            }
            else
            {
                _menuMusic.Play();
                // Hide all canvases beneath the splash so scene-level UI doesn't bleed through.
                var hiddenCanvases = new System.Collections.Generic.List<Canvas>();
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (c.sortingOrder < 32767 && c.enabled)
                    {
                        c.enabled = false;
                        hiddenCanvases.Add(c);
                    }
                }
                _splashInline.Show(() =>
                {
                    foreach (var c in hiddenCanvases)
                        if (c != null) c.enabled = true;
                    ShowMenuAfterSplash();
                });
            }
        }

        private void OnApplicationQuit()
        {
            SaveFileService.FlushSharedPendingWrites();
        }

        public void ContinueStartupAfterProfileSelection(MainMenuController menu)
        {
            RunStartupFlow(menu, false);
        }

        private void RunStartupFlow(MainMenuController menu, bool allowProfileSelection)
        {
            if (menu == null) return;

            var hasAnyProfile = allowProfileSelection
                ? _hadAnyProfileAtStartup
                : _profileSlots.AnySlotExists();
            var envelope = hasAnyProfile || !allowProfileSelection
                ? _saveFileService.Load()
                : new SaveFileEnvelope();
            var next = _startupFlow.GetNextStep(new StartupFlowContext
            {
                AllowProfileSelection = allowProfileSelection,
                HasAnyProfile = hasAnyProfile,
                Envelope = envelope,
                SudokuBasicsComplete = menu.HasCompletedSudokuBasics()
            });

            switch (next)
            {
                case StartupFlowStep.ProfileSelect:
                    menu.ShowProfileSelect();
                    break;
                case StartupFlowStep.FirstRunSetup:
                    menu.ShowMainMenu();
                    menu.ShowFirstRunSetupPrompt(() => ContinueAfterFirstRunSetup(menu));
                    break;
                case StartupFlowStep.SudokuBasicsPrompt:
                    menu.ShowMainMenu();
                    menu.ShowSudokuBasicsPrompt();
                    break;
                default:
                    menu.ShowMainMenu();
                    break;
            }
        }

        private void ContinueAfterFirstRunSetup(MainMenuController menu)
        {
            LocalizationService.SetLanguage(_profileService.LoadOptions().Language);
            menu.RefreshLanguage();

            var refreshedMenu = FindAnyObjectByType<MainMenuController>() ?? menu;
            refreshedMenu.ShowMainMenu();
            if (!refreshedMenu.HasCompletedSudokuBasics())
                refreshedMenu.ShowSudokuBasicsPrompt();
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

            // Pre-seed modifier discovery so previously seen modifiers don't show as "???"
            if (meta?.DiscoveredBossModifiers != null && meta.DiscoveredBossModifiers.Count > 0)
            {
                var seeded = ModifierDiscoveryService.SeedRunSeenModifiers(_run.State, meta.DiscoveredBossModifiers);
                Debug.Log(
                    $"[ModifierDiscovery] LaunchRun pre-seed: added={seeded}, " +
                    $"meta={ModifierDiscoveryService.Describe(meta.DiscoveredBossModifiers)}, " +
                    $"runSeen={ModifierDiscoveryService.Describe(_run.State.SeenBossModifiers)}");
            }

            var supportsRunProgression = request.Mode == GameMode.GardenRun;
            _run.DailyGoals = supportsRunProgression ? _dailyGoals : null;

            // Apply any pending run-start rewards from completed daily goals only to Garden Run.
            if (supportsRunProgression && _dailyGoals != null)
                DailyGoalService.ApplyPendingRewards(_dailyGoals, _run.State);

            // Daily goals: t3_new_class — fire when this class hasn't been played in 7+ days.
            // Record class played regardless so the next run of the same class won't re-trigger.
            if (supportsRunProgression && _dailyGoals != null)
            {
                if (DailyGoalService.HasBeenUnusedForDays(_dailyGoals, request.ClassId, DateTime.Today))
                    DailyGoalService.EvaluateInRun(_dailyGoals, _run.State, null, DailyGoalService.TriggerNewClassUsed);
                DailyGoalService.RecordClassPlayed(_dailyGoals, request.ClassId, DateTime.Today);
            }

            _screenManager.ShowGame();
            BindRunToMap();

            Debug.Log($"[GameBootstrap] Launched {request.Mode} as {request.ClassId}, seed={runtimeSeed}");
        }

        public void LaunchSeasonalChallenge()
        {
            var now = DateTime.Today;
            _menuMusic.Stop();
            _run.DailyGoals = null; // no daily goal tracking in seasonal

            // Resume in-progress attempt if one was saved for the current month.
            if (_resumeService.TryResumeSeasonalFromSave(now.Year, now.Month,
                out var savedState, out var savedPuzzle))
            {
                _run.RestoreState(savedState);
                _run.StartSeasonalChallenge(SeasonalChallengeService.BuildChallengeConfig(now.Year, now.Month));
                if (savedPuzzle != null)
                    _run.TryRestorePuzzleSaveState(savedPuzzle);
                Debug.Log($"[GameBootstrap] Resumed Seasonal Challenge {now.Year}-{now.Month:D2}");
            }
            else
            {
                var runState = SeasonalChallengeService.CreateChallengeRunState(now.Year, now.Month);
                var levelConfig = SeasonalChallengeService.BuildChallengeConfig(now.Year, now.Month);
                _run.RestoreState(runState);
                _run.StartSeasonalChallenge(levelConfig);
                Debug.Log($"[GameBootstrap] Launched Seasonal Challenge {now.Year}-{now.Month:D2}");
            }

            _screenManager.ShowGame();
            BindRunToMap();
        }

        public void LaunchTutorial(TutorialSetupConfig setup)
        {
            if (!TutorialModeService.TryValidateCustomSetup(setup, out var failureMessage))
            {
                Debug.LogWarning($"[GameBootstrap] Custom puzzle rejected before generation: {failureMessage}");
                return;
            }

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
                AllowIrregularPuzzles = runState.AllowIrregularPuzzles,
                HarmonyLevel = runState.HarmonyLevel,
                HarmonyPerk = runState.HarmonyPerk,
                SelectedTier = runState.Mode == GameMode.SpiritTrials ? runState.SpiritTrialsTier : null
            };

            // Re-initialize services with the run's seed, then overlay saved progress
            // (HP, floor, items, relics, curses, NodePath, etc.)

            _run.StartRun(request, runState.Seed);
            _run.RestoreState(runState);
            var savedNodeIndex = runState.CurrentNodeIndex;
            _run.RebuildFloorGraph(); // rebuild for the restored floor/path state
            _run.State.CurrentNodeIndex = savedNodeIndex;
            Debug.Log(
                $"[ResumeRestore] HP={_run.State.CurrentHP} Pencil={_run.State.CurrentPencil} " +
                $"Items={_run.State.HeldItems?.Count ?? 0} NodeIdx={_run.State.CurrentNodeIndex} " +
                $"Floor={_run.State.CurrentFloor} RunNum={_run.State.RunNumber}");

            // Pre-seed the full cross-run discovery set so the boss gate correctly
            // shows names for modifiers discovered in prior completed runs — not just
            // those saved within this specific run's SeenBossModifierList.
            var resumeMeta = _profileService.LoadMetaProgress();
            if (resumeMeta?.DiscoveredBossModifiers != null)
            {
                foreach (var mod in resumeMeta.DiscoveredBossModifiers)
                    _run.State.SeenBossModifiers.Add(mod);
                _run.State.SyncSeenModifiersToList();
            }

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

        public bool HasResumableSeasonalRun()
        {
            var now = DateTime.Today;
            return _resumeService.HasActiveSeasonalRun(now.Year, now.Month);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            var opts = _profileService?.LoadOptions();
            var muteWhenUnfocused = opts?.Audio?.MuteWhenUnfocused ?? false;
            AudioListener.pause = muteWhenUnfocused && !hasFocus;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
