using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SudokuRoguelike.Core;
using SudokuRoguelike.Platform;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    public sealed class OptionsController : MonoBehaviour
    {
        // [REQ: UI-OPT-004] One options state for both controllers (the main menu's and the run's) and the slot it was read
        // from. Each kept its own copy, so the run's Options showed a main-menu change undone and saved over it.
        private static OptionsState _options;
        private static int _optionsSlot = -1;
        private int _lastActiveSlot = -1;

        /// <summary>
        /// Live copy of audio settings updated immediately on every audio setter call.
        /// RunAudioController reads this each frame so volume changes are instant, not disk-polled.
        /// </summary>
        public static AudioSettingsModel LiveAudio { get; private set; } = new AudioSettingsModel();
        public static AccessibilitySettings LiveAccessibility { get; private set; } = new AccessibilitySettings();
        /// <summary>Live copy of the gameplay settings; the board, the number pad and digit entry read it (GAP-061).</summary>
        public static GameplaySettings LiveGameplay
        {
            get => _liveGameplay;
            set => _liveGameplay = value ?? new GameplaySettings();
        }
        private static GameplaySettings _liveGameplay = new GameplaySettings();

        private void Awake()
        {
            _lastActiveSlot = SaveProfileService.ActiveSlot;
            _options = null;   // a new game session reads the save again
        }

        /// <summary>Fires whenever the active profile slot changes, after services have been re-wired.</summary>
        public System.Action OnSlotChanged;

        /// <summary>Fires whenever option-backed UI should refresh from the current profile state.</summary>
        public System.Action OnOptionsChanged;

        /// <summary>Fires after any accessibility setting is saved.</summary>
        public System.Action OnAccessibilityChanged;

        private void Update()
        {
            var slot = SaveProfileService.ActiveSlot;
            if (slot == _lastActiveSlot) return;
            _lastActiveSlot = slot;
            _options = null; // force reload from new slot on next access
            OnSlotChanged?.Invoke();
            RefreshAllScreens();   // the other Options screen may be inactive and miss its own Update
        }

        /// <summary>A profile was chosen or deleted: read the active slot's options again and redraw both Options screens.</summary>
        // UI-OPT-004: no slot check, since the number can stay while its file changes (a profile deleted, its slot chosen again).
        public static void ReloadAfterProfileChange()
        {
            LoadShared();
            RefreshAllScreens();
        }

        public void LoadOptions() => LoadShared();

        private static void LoadShared()
        {
            _optionsSlot = SaveProfileService.ActiveSlot;
            _options = new ProfileService(new SaveFileService(_optionsSlot)).LoadOptions();
            LiveAudio = _options.Audio;
            LiveAccessibility = _options.Accessibility ?? new AccessibilitySettings();
            ApplyTextSettings();
            AccessibilityAnnouncementOverlay.SetEnabled(LiveAccessibility.ScreenReaderEnabled);
            RumbleService.Enabled = _options.Gameplay.ControllerRumbleEnabled;
            PadButtons.Mode = _options.Gameplay.ControllerLayout;
            LiveGameplay = _options.Gameplay ?? new GameplaySettings();
            // [REQ: OPT-AUDIO-001] Every audio source multiplies the master volume in; the listener stays at full volume
            // (it was set to the master volume too, so the master counted twice: 50 % played at 25 %; GAP-061).
            AudioListener.volume = 1f;
            var gfx = _options.Graphics;
            TakeThisPcDisplay(gfx);
            QualitySettings.vSyncCount = gfx.VSync ? 1 : 0;
        }

        private static bool _displayApplied;

        /// <summary>Applied display changes this process; a profile choice must not add one (GAP-075).</summary>
        public static int DisplayApplications { get; private set; }

        // [REQ: DISPLAY-MODE-008] The options show this PC's display, never the profile's (it travels with the cloud save,
        // GAP-074). This PC's choice is applied once per start, since a frameless window is not restored by Unity; without
        // one, what Unity opened (borderless at the display's size, or its own last mode) stays. A later load (a profile
        // choice, the first-run setup) applies nothing, so a window no longer jumps (GAP-075).
        private static void TakeThisPcDisplay(GraphicsSettingsModel gfx)
        {
            if (DisplayPrefs.TryLoad(out var mode, out var width, out var height))
            {
                gfx.DisplayMode = NormalizeDisplayMode(mode);
                if (width > 0 && height > 0) (gfx.ResolutionWidth, gfx.ResolutionHeight) = (width, height);
                if (!_displayApplied) ApplyDisplay(gfx);
            }
            else
            {
                // Size from this PC too: the profile's (another PC's) must not reach a later switch to Fullscreen.
                gfx.DisplayMode = FromUnity(Screen.fullScreenMode);
                var size = gfx.DisplayMode == DisplayModeOption.Borderless ? DisplaySize() : new Vector2Int(Screen.width, Screen.height);
                if (size.x > 0 && size.y > 0) (gfx.ResolutionWidth, gfx.ResolutionHeight) = (size.x, size.y);
            }
            gfx.Fullscreen = !IsWindowMode(gfx.DisplayMode);
            _displayApplied = true;
        }

        public static DisplayModeOption FromUnity(FullScreenMode mode) => mode switch
        {
            FullScreenMode.ExclusiveFullScreen => DisplayModeOption.Fullscreen,
            FullScreenMode.FullScreenWindow => DisplayModeOption.Borderless,
            _ => DisplayModeOption.Windowed
        };

        // The player's own change: shown at once and kept for this PC.
        private static void ApplyAndRemember(GraphicsSettingsModel gfx)
        {
            if (NormalizeDisplayMode(gfx.DisplayMode) == DisplayModeOption.Fullscreen)
            {
                // Exclusive mode only at a size the monitor offers.
                var safe = ResolveSupportedResolution(gfx.ResolutionWidth, gfx.ResolutionHeight, GetSupportedResolutions());
                (gfx.ResolutionWidth, gfx.ResolutionHeight) = (safe.width, safe.height);
            }
            ApplyDisplay(gfx);
            DisplayPrefs.Store(gfx);
        }

        public OptionsState GetOptions()
        {
            if (_options == null || _optionsSlot != SaveProfileService.ActiveSlot) LoadOptions();
            return _options;
        }

        // ── Audio ────────────────────────────────────────────────────────────────

        public void SetMasterVolume(float value)
        {
            GetOptions().Audio.MasterVolume = value;
            AudioListener.volume = 1f;   // OPT-AUDIO-001
            LiveAudio = GetOptions().Audio;
            SaveOptions();
        }

        public void SetMusicVolume(float value)
        {
            GetOptions().Audio.MusicVolume = value;
            LiveAudio = GetOptions().Audio;
            SaveOptions();
        }

        public void SetSfxVolume(float value)
        {
            GetOptions().Audio.SfxVolume = value;
            LiveAudio = GetOptions().Audio;
            SaveOptions();
        }

        public void SetMuteAll(bool on)
        {
            GetOptions().Audio.MuteAll = on;
            LiveAudio = GetOptions().Audio;
            SaveOptions();
        }

        public void SetMusicStyleIndex(int index)
        {
            GetOptions().Audio.MenuMusicStyleIndex = AudioAssetService.NormalizeMenuMusicStyleIndex(index);
            LiveAudio = GetOptions().Audio;
            SaveOptions();
        }

        public void SetMuteWhenUnfocused(bool on)
        {
            GetOptions().Audio.MuteWhenUnfocused = on;
            LiveAudio = GetOptions().Audio;
            SaveOptions();
        }

        // ── Language ──────────────────────────────────────────────────────────────

        public void SetLanguage(LanguageOption lang)
        {
            GetOptions().Language = lang;
            LocalizationService.SetLanguage(lang);
            SaveOptions();
        }

        // ── Accessibility ─────────────────────────────────────────────────────────

        public void SetColorblindMode(bool on)
        {
            GetOptions().Accessibility.ColorblindMode = on;
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetHighContrast(bool on)
        {
            GetOptions().Accessibility.HighContrastMode = on;
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetReduceMotion(bool on)
        {
            GetOptions().Accessibility.ReduceMotion = on;
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetAltSymbols(bool on)
        {
            GetOptions().Accessibility.AlternativeConstraintSymbols = on;
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetFontScale(float scale)
        {
            GetOptions().Accessibility.FontScale = Mathf.Clamp(scale, 0.8f, 1.5f);
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetDyslexiaFont(bool on)
        {
            GetOptions().Accessibility.DyslexiaFont = on;
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        // [REQ: ACCESS-FONT-003] [REQ: ACCESS-FONT-004] Text Size and the font apply to every text on screen at once.
        private static void ApplyTextSettings()
        {
            TextSizeFit.SetScale(LiveAccessibility.FontScale);
            FontAssetService.SetDyslexiaFont(LiveAccessibility.DyslexiaFont);
        }

        public void SetScreenReaderEnabled(bool on)
        {
            GetOptions().Accessibility.ScreenReaderEnabled = on;
            LiveAccessibility = GetOptions().Accessibility;
            SaveOptions();
            NotifyAccessibilityChanged();
            if (on)
                AccessibilityAnnouncementOverlay.Announce(
                    LocalizationService.T("Accessibility.ScreenReader.Enabled", "Screen reader captions enabled."));
        }

        // ── Gameplay ──────────────────────────────────────────────────────────────

        // GAP-061: every Gameplay option now acts (OPT-GAME-001..005); the board and the number pad read LiveGameplay and
        // are redrawn when an option changes during a puzzle.
        public void SetHighlightConflicts(bool on) => SetGameplay(g => g.HighlightConflicts = on);

        public void SetDoubleTapConfirm(bool on) => SetGameplay(g => g.DoubleTapConfirmNumberEntry = on);

        /// <summary>[REQ: OPT-GAME-006] The input lock after choosing a cell: on or off, and its time (0.10–1.00 s).</summary>
        // The board does not draw the lock, so a change (or each step of the time row) does not rebuild the open board.
        public void SetInputLockEnabled(bool on) => SetGameplay(g => g.InputLockEnabled = on, redrawBoard: false);

        public void SetInputLockSeconds(float seconds) =>
            SetGameplay(g => g.InputLockSeconds = GameplaySettings.ClampInputLockSeconds(seconds), redrawBoard: false);

        /// <summary>[REQ: OPT-GAME-002] On: a placed digit leaves the pencil marks of its row, column and box.</summary>
        public void SetAutoPencilCleanup(bool on) => SetGameplay(g =>
        {
            g.KeepPencilMarks = !on;
            g.AutoPencilCleanup = on;
        });

        /// <summary>[REQ: OPT-GAME-003] "Show Digits Left" (the old "Show Candidate Count" field).</summary>
        public void SetShowCandidateCount(bool on) => SetGameplay(g => g.ShowCandidateCount = on);

        /// <summary>[REQ: OPT-GAME-004] "Jump to Next Empty Cell" (the old "Cursor Snap" field).</summary>
        public void SetCursorSnapOnSelection(bool on) => SetGameplay(g => g.CursorSnapOnSelection = on);

        private void SetGameplay(Action<GameplaySettings> change, bool redrawBoard = true)
        {
            change(GetOptions().Gameplay);
            LiveGameplay = GetOptions().Gameplay;
            SaveOptions();
            if (!redrawBoard) return;
            FindFirstObjectByType<InRunController>()?.OnGameplayOptionsChanged();
        }

        /// <summary>[REQ: INPUT-PAD-002] Automatic, X-input or D-input.</summary>
        public void SetControllerLayout(ControllerLayoutMode mode)
        {
            GetOptions().Gameplay.ControllerLayout = mode;
            PadButtons.Mode = mode;
            SaveOptions();
        }

        public void SetControllerRumbleEnabled(bool on)
        {
            GetOptions().Gameplay.ControllerRumbleEnabled = on;
            RumbleService.Enabled = on;
            if (!on) RumbleService.Stop();
            SaveOptions();
        }

        // ── Graphics ──────────────────────────────────────────────────────────────

        // [REQ: DISPLAY-MODE-006] The Fullscreen toggle switches between a fullscreen mode and a window. Turning it off
        // keeps Frameless Window when that was chosen; turning it on keeps Borderless Fullscreen.
        public void SetFullscreen(bool on)
        {
            var gfx = GetOptions().Graphics;
            var current = NormalizeDisplayMode(gfx.DisplayMode);
            if (on) gfx.DisplayMode = IsWindowMode(current) ? DisplayModeOption.Fullscreen : current;
            else gfx.DisplayMode = IsWindowMode(current) ? current : DisplayModeOption.Windowed;
            gfx.Fullscreen = !IsWindowMode(gfx.DisplayMode);
            ApplyAndRemember(gfx);
            SaveOptions();
        }

        public void SetDisplayMode(DisplayModeOption mode)
        {
            var gfx = GetOptions().Graphics;
            gfx.DisplayMode = NormalizeDisplayMode(mode);
            gfx.Fullscreen = !IsWindowMode(gfx.DisplayMode);
            ApplyAndRemember(gfx);
            SaveOptions();
        }

        public void SetVSync(bool on)
        {
            GetOptions().Graphics.VSync = on;
            QualitySettings.vSyncCount = on ? 1 : 0;
            SaveOptions();
        }

        public void SetScreenShake(bool on)
        {
            GetOptions().Graphics.ScreenShake = on;
            SaveOptions();
            NotifyAccessibilityChanged();   // OPT-GFX-001: the run reads it with Reduce Motion
        }

        public void SetParticleIntensity(float value)
        {
            GetOptions().Graphics.ParticleIntensity = Mathf.Clamp01(value);
            SaveOptions();
            FindFirstObjectByType<AmbientParticleController>()?.RefreshSettings();
        }

        // [REQ: OPT-LABEL-001] Board Brightness and Board Contrast calibrate the puzzle board's cells; the open board is
        // redrawn at once (it waited for the next puzzle).
        public void SetBrightness(float value)
        {
            GetOptions().Graphics.Brightness = Mathf.Clamp01(value);
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetContrast(float value)
        {
            GetOptions().Graphics.Contrast = Mathf.Clamp01(value);
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetResolution(int width, int height)
        {
            var opt = GetOptions();
            var safe = ResolveSupportedResolution(width, height, GetSupportedResolutions());
            opt.Graphics.ResolutionWidth = safe.width;
            opt.Graphics.ResolutionHeight = safe.height;
            ApplyAndRemember(opt.Graphics);
            SaveOptions();
        }

        public static Resolution[] GetSupportedResolutions()
        {
            return NormalizeSupportedResolutions(Screen.resolutions);
        }

        public static Resolution[] NormalizeSupportedResolutions(IEnumerable<Resolution> resolutions)
        {
            var source = resolutions ?? Array.Empty<Resolution>();
            var normalized = source
                .Where(r => r.width > 0 && r.height > 0)
                .GroupBy(r => $"{r.width}x{r.height}")
                .Select(group => group.First())
                .OrderBy(r => r.width * r.height)
                .ThenBy(r => r.width)
                .ThenBy(r => r.height)
                .ToArray();

            return normalized.Length > 0 ? normalized : GetFallbackResolutions();
        }

        public static Resolution ResolveSupportedResolution(
            int width,
            int height,
            IEnumerable<Resolution> supportedResolutions)
        {
            var supported = NormalizeSupportedResolutions(supportedResolutions);
            foreach (var resolution in supported)
            {
                if (resolution.width == width && resolution.height == height)
                    return resolution;
            }

            foreach (var resolution in supported)
            {
                if (resolution.width == 1920 && resolution.height == 1080)
                    return resolution;
            }

            return supported[0];
        }

        // ── Reset to defaults ─────────────────────────────────────────────────────

        public void ResetAudioToDefaults()
        {
            var defaults = new AudioSettingsModel();
            var audio = GetOptions().Audio;
            audio.MasterVolume       = defaults.MasterVolume;
            audio.MusicVolume        = defaults.MusicVolume;
            audio.SfxVolume          = defaults.SfxVolume;
            audio.MuteAll            = defaults.MuteAll;
            audio.MuteWhenUnfocused  = defaults.MuteWhenUnfocused;
            AudioListener.volume     = 1f;   // OPT-AUDIO-001
            LiveAudio                = audio;
            SaveOptions();
        }

        public void ResetGraphicsToDefaults()
        {
            var defaults = new GraphicsSettingsModel();
            var gfx = GetOptions().Graphics;
            // DISPLAY-MODE-008: the reset gives Borderless Fullscreen at the display's size, right on every screen.
            gfx.DisplayMode       = DisplayModeOption.Borderless;
            gfx.Fullscreen        = true;
            gfx.Brightness        = defaults.Brightness;
            gfx.Contrast          = defaults.Contrast;
            gfx.VSync             = defaults.VSync;
            gfx.ScreenShake       = defaults.ScreenShake;
            gfx.ParticleIntensity = defaults.ParticleIntensity;
            var safeResolution    = ResolveSupportedResolution(
                defaults.ResolutionWidth,
                defaults.ResolutionHeight,
                GetSupportedResolutions());
            gfx.ResolutionWidth   = safeResolution.width;
            gfx.ResolutionHeight  = safeResolution.height;
            ApplyAndRemember(gfx);
            QualitySettings.vSyncCount = gfx.VSync ? 1 : 0;
            SaveOptions();
        }

        public void ResetGameplayToDefaults()
        {
            var defaults = new GameplaySettings();
            var gp = GetOptions().Gameplay;
            gp.HighlightConflicts           = defaults.HighlightConflicts;
            gp.ConfirmBeforeWrongPlacement  = defaults.ConfirmBeforeWrongPlacement;
            gp.DoubleTapConfirmNumberEntry  = defaults.DoubleTapConfirmNumberEntry;
            gp.AutoPencilCleanup            = defaults.AutoPencilCleanup;
            gp.KeepPencilMarks              = defaults.KeepPencilMarks;
            gp.ShowCandidateCount           = defaults.ShowCandidateCount;
            gp.CursorSnapOnSelection        = defaults.CursorSnapOnSelection;
            gp.ControllerRumbleEnabled      = defaults.ControllerRumbleEnabled;
            gp.InputLockEnabled             = defaults.InputLockEnabled;
            gp.InputLockSeconds             = defaults.InputLockSeconds;
            RumbleService.Enabled           = gp.ControllerRumbleEnabled;
            LiveGameplay                    = gp;
            SaveOptions();
            FindFirstObjectByType<InRunController>()?.OnGameplayOptionsChanged();
        }

        public void ResetAccessibilityToDefaults()
        {
            var defaults = new AccessibilitySettings();
            var acc = GetOptions().Accessibility;
            acc.ColorblindMode                 = defaults.ColorblindMode;
            acc.HighContrastMode               = defaults.HighContrastMode;
            acc.ReduceMotion                   = defaults.ReduceMotion;
            acc.AlternativeConstraintSymbols   = defaults.AlternativeConstraintSymbols;
            acc.FontScale                      = defaults.FontScale;
            acc.DyslexiaFont                   = defaults.DyslexiaFont;
            acc.ScreenReaderEnabled            = defaults.ScreenReaderEnabled;
            LiveAccessibility                  = acc;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void SaveOptions()
        {
            var saveFile = new SaveFileService(_optionsSlot);
            var envelope = saveFile.Load();
            envelope.PlayerProfile.Options = _options;
            saveFile.Save(envelope);
            FindFirstObjectByType<MenuMusicController>()?.ApplySettings(_options.Audio);
            NotifyOptionsChangedForUiRefresh();
        }

        public void NotifyOptionsChangedForUiRefresh() => RefreshAllScreens();

        // UI-OPT-004: both Options screens show the one state, so a change refreshes the rows of each.
        private static void RefreshAllScreens()
        {
            foreach (var controller in FindObjectsByType<OptionsController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                controller.OnOptionsChanged?.Invoke();
        }

        private void NotifyAccessibilityChanged()
        {
            ApplyTextSettings();
            AccessibilityAnnouncementOverlay.SetEnabled(LiveAccessibility.ScreenReaderEnabled);
            OnAccessibilityChanged?.Invoke();
            var runScreen = FindFirstObjectByType<InRunController>();
            runScreen?.NotifyAccessibilityChanged();
        }

        private static Resolution[] GetFallbackResolutions()
        {
            return new[]
            {
                new Resolution { width = 1280, height = 720 },
                new Resolution { width = 1920, height = 1080 },
                new Resolution { width = 2560, height = 1440 },
                new Resolution { width = 3840, height = 2160 }
            };
        }

        public static DisplayModeOption NormalizeDisplayMode(DisplayModeOption mode)
        {
            return Enum.IsDefined(typeof(DisplayModeOption), mode)
                ? mode
                : DisplayModeOption.Fullscreen;
        }

        public static bool IsWindowMode(DisplayModeOption mode) =>
            mode == DisplayModeOption.Windowed || mode == DisplayModeOption.FramelessWindow;

        // [REQ: DISPLAY-MODE-001] Fullscreen is exclusive; Borderless Fullscreen is a borderless window covering the
        // display; Window and Frameless Window are Unity windows whose frame WindowFrameService sets.
        public static FullScreenMode ToUnityFullScreenMode(DisplayModeOption mode)
        {
            return NormalizeDisplayMode(mode) switch
            {
                DisplayModeOption.Borderless => FullScreenMode.FullScreenWindow,
                DisplayModeOption.Windowed => FullScreenMode.Windowed,
                DisplayModeOption.FramelessWindow => FullScreenMode.Windowed,
                _ => FullScreenMode.ExclusiveFullScreen
            };
        }

        /// <summary>Standard 16:9 window sizes, largest first.</summary>
        public static readonly Vector2Int[] StandardWindowSizes =
        {
            new Vector2Int(3840, 2160), new Vector2Int(2560, 1440), new Vector2Int(1920, 1080),
            new Vector2Int(1600, 900), new Vector2Int(1280, 720), new Vector2Int(960, 540)
        };

        // [REQ: DISPLAY-MODE-002] A window uses the chosen resolution when it leaves 10 % of the display free in both
        // directions (room for the frame and the taskbar); otherwise the largest standard 16:9 size that does.
        public static Vector2Int GetWindowSize(int selectedWidth, int selectedHeight, int displayWidth, int displayHeight)
        {
            if (displayWidth <= 0 || displayHeight <= 0)
                return new Vector2Int(Math.Max(selectedWidth, 960), Math.Max(selectedHeight, 540));
            var maxWidth = displayWidth * 9 / 10;
            var maxHeight = displayHeight * 9 / 10;
            if (selectedWidth > 0 && selectedHeight > 0 && selectedWidth <= maxWidth && selectedHeight <= maxHeight)
                return new Vector2Int(selectedWidth, selectedHeight);
            foreach (var size in StandardWindowSizes)
                if (size.x <= maxWidth && size.y <= maxHeight) return size;
            return StandardWindowSizes[StandardWindowSizes.Length - 1];
        }

        /// <summary>The display's own (native) size.</summary>
        public static Vector2Int DisplaySize()
        {
            // The monitor the window is on; in exclusive mode it reports the lowered mode, so the primary's native size.
            var info = Screen.mainWindowDisplayInfo;
            if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen && info.width > 0 && info.height > 0)
                return new Vector2Int(info.width, info.height);
            var display = Display.main;
            if (display != null && display.systemWidth > 0 && display.systemHeight > 0)
                return new Vector2Int(display.systemWidth, display.systemHeight);
            var desktop = Screen.currentResolution;
            return new Vector2Int(desktop.width, desktop.height);
        }

        /// <summary>
        /// [REQ: DISPLAY-MODE-009] The size a mode is shown at: Borderless Fullscreen always at the display's own size (a
        /// smaller one was stretched by Windows, blurry and in other colours, GAP-076); windows by DISPLAY-MODE-002;
        /// exclusive Fullscreen at the chosen resolution.
        /// </summary>
        public static Vector2Int SizeFor(DisplayModeOption mode, int width, int height, Vector2Int display)
        {
            mode = NormalizeDisplayMode(mode);
            if (mode == DisplayModeOption.Borderless && display.x > 0 && display.y > 0) return display;
            return IsWindowMode(mode) ? GetWindowSize(width, height, display.x, display.y) : new Vector2Int(width, height);
        }

        // The one place that applies display mode and size: this PC's choice at start, and every display change.
        private static void ApplyDisplay(GraphicsSettingsModel graphics)
        {
            if (graphics == null) return;
            var mode = NormalizeDisplayMode(graphics.DisplayMode);
            var unityMode = ToUnityFullScreenMode(mode);
            var size = SizeFor(mode, graphics.ResolutionWidth, graphics.ResolutionHeight, DisplaySize());
            DisplayApplications++;
            if (size.x > 0 && size.y > 0) Screen.SetResolution(size.x, size.y, unityMode);
            else Screen.fullScreenMode = unityMode;
            WindowFrameService.Apply(mode, size.x, size.y);
        }
    }
}
