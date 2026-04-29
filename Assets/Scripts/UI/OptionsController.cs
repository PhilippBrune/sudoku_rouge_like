using UnityEngine;
using SudokuRoguelike.Core;
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

        private void Awake()
        {
            var slot = SaveProfileService.ActiveSlot;
            _lastActiveSlot = slot;
            _saveFile = new SaveFileService(slot);
            _profile = new ProfileService(_saveFile);
        }

        /// <summary>Fires whenever the active profile slot changes, after services have been re-wired.</summary>
        public System.Action OnSlotChanged;

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
            // Apply graphics settings immediately so they take effect on startup/slot switch
            AudioListener.volume = _options.Audio.MasterVolume;
            Screen.fullScreen = _options.Graphics.Fullscreen;
            QualitySettings.vSyncCount = _options.Graphics.VSync ? 1 : 0;
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
            AudioListener.volume = value;
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
            GetOptions().Audio.MenuMusicStyleIndex = index;
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
        }

        // ── Gameplay ──────────────────────────────────────────────────────────────

        public void SetHighlightConflicts(bool on)
        {
            GetOptions().Gameplay.HighlightConflicts = on;
            SaveOptions();
        }

        public void SetConfirmBeforeWrongPlacement(bool on)
        {
            GetOptions().Gameplay.ConfirmBeforeWrongPlacement = on;
            SaveOptions();
        }

        public void SetDoubleTapConfirm(bool on)
        {
            GetOptions().Gameplay.DoubleTapConfirmNumberEntry = on;
            SaveOptions();
        }

        public void SetAutoPencilCleanup(bool on)
        {
            GetOptions().Gameplay.AutoPencilCleanup = on;
            SaveOptions();
        }

        public void SetShowCandidateCount(bool on)
        {
            GetOptions().Gameplay.ShowCandidateCount = on;
            SaveOptions();
        }

        public void SetCursorSnapOnSelection(bool on)
        {
            GetOptions().Gameplay.CursorSnapOnSelection = on;
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

        public void SetFullscreen(bool on)
        {
            GetOptions().Graphics.Fullscreen = on;
            Screen.fullScreen = on;
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
        }

        public void SetParticleIntensity(float value)
        {
            GetOptions().Graphics.ParticleIntensity = Mathf.Clamp01(value);
            SaveOptions();
            FindFirstObjectByType<AmbientParticleController>()?.RefreshSettings();
        }

        public void SetResolution(int width, int height)
        {
            var opt = GetOptions();
            opt.Graphics.ResolutionWidth = width;
            opt.Graphics.ResolutionHeight = height;
            Screen.SetResolution(width, height, opt.Graphics.Fullscreen);
            SaveOptions();
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
            AudioListener.volume     = audio.MasterVolume;
            LiveAudio                = audio;
            SaveOptions();
        }

        public void ResetGraphicsToDefaults()
        {
            var defaults = new GraphicsSettingsModel();
            var gfx = GetOptions().Graphics;
            gfx.Fullscreen        = defaults.Fullscreen;
            gfx.VSync             = defaults.VSync;
            gfx.ScreenShake       = defaults.ScreenShake;
            gfx.ParticleIntensity = defaults.ParticleIntensity;
            gfx.ResolutionWidth   = defaults.ResolutionWidth;
            gfx.ResolutionHeight  = defaults.ResolutionHeight;
            Screen.fullScreen     = gfx.Fullscreen;
            QualitySettings.vSyncCount = gfx.VSync ? 1 : 0;
            Screen.SetResolution(gfx.ResolutionWidth, gfx.ResolutionHeight, gfx.Fullscreen);
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
            gp.ShowCandidateCount           = defaults.ShowCandidateCount;
            gp.CursorSnapOnSelection        = defaults.CursorSnapOnSelection;
            gp.ControllerRumbleEnabled      = defaults.ControllerRumbleEnabled;
            RumbleService.Enabled           = gp.ControllerRumbleEnabled;
            SaveOptions();
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
        }

        private void NotifyAccessibilityChanged()
        {
            AccessibilityAnnouncementOverlay.SetEnabled(LiveAccessibility.ScreenReaderEnabled);
            OnAccessibilityChanged?.Invoke();
            var runScreen = FindFirstObjectByType<InRunController>();
            runScreen?.NotifyAccessibilityChanged();
        }
    }
}
