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
        private SaveFileService _saveFile;
        private ProfileService _profile;
        private OptionsState _options;
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
            var slot = SaveProfileService.ActiveSlot;
            _lastActiveSlot = slot;
            _saveFile = new SaveFileService(slot);
            _profile = new ProfileService(_saveFile);
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
            _saveFile = new SaveFileService(slot);
            _profile = new ProfileService(_saveFile);
            _options = null; // force reload from new slot on next access
            OnSlotChanged?.Invoke();
            OnOptionsChanged?.Invoke();
        }

        public void LoadOptions()
        {
            if (_saveFile == null) _saveFile = new SaveFileService(SaveProfileService.ActiveSlot);
            if (_profile == null) _profile = new ProfileService(_saveFile);
            _options = _profile.LoadOptions();
            LiveAudio = _options.Audio;
            LiveAccessibility = _options.Accessibility ?? new AccessibilitySettings();
            AccessibilityAnnouncementOverlay.SetEnabled(LiveAccessibility.ScreenReaderEnabled);
            RumbleService.Enabled = _options.Gameplay.ControllerRumbleEnabled;
            LiveGameplay = _options.Gameplay ?? new GameplaySettings();
            // [REQ: OPT-AUDIO-001] Every audio source multiplies the master volume in; the listener stays at full volume
            // (it was set to the master volume too, so the master counted twice: 50 % played at 25 %; GAP-061).
            AudioListener.volume = 1f;
            var gfx = _options.Graphics;
            ApplyDisplay(gfx);
            QualitySettings.vSyncCount = gfx.VSync ? 1 : 0;
        }

        public OptionsState GetOptions()
        {
            if (_options == null) LoadOptions();
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

        public void SetUiVolume(float value)
        {
            GetOptions().Audio.UiVolume = value;
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
        public void SetInputLockEnabled(bool on) => SetGameplay(g => g.InputLockEnabled = on);

        public void SetInputLockSeconds(float seconds) =>
            SetGameplay(g => g.InputLockSeconds = GameplaySettings.ClampInputLockSeconds(seconds));

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

        private void SetGameplay(Action<GameplaySettings> change)
        {
            change(GetOptions().Gameplay);
            LiveGameplay = GetOptions().Gameplay;
            SaveOptions();
            FindFirstObjectByType<InRunController>()?.OnGameplayOptionsChanged();
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
            ApplyDisplay(gfx);
            SaveOptions();
        }

        public void SetDisplayMode(DisplayModeOption mode)
        {
            var gfx = GetOptions().Graphics;
            gfx.DisplayMode = NormalizeDisplayMode(mode);
            gfx.Fullscreen = !IsWindowMode(gfx.DisplayMode);
            ApplyDisplay(gfx);
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
            ApplyDisplay(opt.Graphics);
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
            audio.UiVolume           = defaults.UiVolume;
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
            gfx.Fullscreen        = defaults.Fullscreen;
            gfx.DisplayMode       = defaults.DisplayMode;
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
            ApplyDisplay(gfx);
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
            acc.ScreenReaderEnabled            = defaults.ScreenReaderEnabled;
            LiveAccessibility                  = acc;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void SaveOptions()
        {
            var envelope = _saveFile.Load();
            envelope.PlayerProfile.Options = _options;
            _saveFile.Save(envelope);
            FindFirstObjectByType<MenuMusicController>()?.ApplySettings(_options.Audio);
            OnOptionsChanged?.Invoke();
        }

        public void NotifyOptionsChangedForUiRefresh()
        {
            OnOptionsChanged?.Invoke();
        }

        private void NotifyAccessibilityChanged()
        {
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

        private static Vector2Int DisplaySize()
        {
            var display = Display.main;
            if (display != null && display.systemWidth > 0 && display.systemHeight > 0)
                return new Vector2Int(display.systemWidth, display.systemHeight);
            var desktop = Screen.currentResolution;
            return new Vector2Int(desktop.width, desktop.height);
        }

        // The one place that applies display mode and size: startup, mode, resolution and reset.
        private static void ApplyDisplay(GraphicsSettingsModel graphics)
        {
            if (graphics == null) return;
            var mode = NormalizeDisplayMode(graphics.DisplayMode);
            var unityMode = ToUnityFullScreenMode(mode);
            int width = graphics.ResolutionWidth, height = graphics.ResolutionHeight;
            if (IsWindowMode(mode))
            {
                var display = DisplaySize();
                var size = GetWindowSize(width, height, display.x, display.y);
                width = size.x;
                height = size.y;
            }

            if (width > 0 && height > 0) Screen.SetResolution(width, height, unityMode);
            else Screen.fullScreenMode = unityMode;
            WindowFrameService.Apply(mode, width, height);
        }
    }
}
