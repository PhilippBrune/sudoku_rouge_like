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

        private void Awake()
        {
            _saveFile = new SaveFileService();
            _profile = new ProfileService(_saveFile);
        }

        public void LoadOptions()
        {
            if (_saveFile == null) _saveFile = new SaveFileService();
            if (_profile == null) _profile = new ProfileService(_saveFile);
            _options = _profile.LoadOptions();
        }

        public OptionsState GetOptions()
        {
            if (_options == null) LoadOptions();
            return _options;
        }

        public void SetMasterVolume(float value)
        {
            GetOptions().Audio.MasterVolume = value;
            AudioListener.volume = value;
            SaveOptions();
        }

        public void SetMusicVolume(float value)
        {
            GetOptions().Audio.MusicVolume = value;
            SaveOptions();
        }

        public void SetSfxVolume(float value)
        {
            GetOptions().Audio.SfxVolume = value;
            SaveOptions();
        }

        public void SetUiVolume(float value)
        {
            GetOptions().Audio.UiVolume = value;
            SaveOptions();
        }

        public void SetMusicStyleIndex(int index)
        {
            GetOptions().Audio.MenuMusicStyleIndex = index;
            SaveOptions();
        }

        public void SetMuteWhenUnfocused(bool on)
        {
            GetOptions().Audio.MuteWhenUnfocused = on;
            SaveOptions();
        }

        public void SetLanguage(LanguageOption lang)
        {
            GetOptions().Language = lang;
            LocalizationService.SetLanguage(lang);
            SaveOptions();
        }

        public void SetColorblindMode(bool on)
        {
            GetOptions().Accessibility.ColorblindMode = on;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetHighContrast(bool on)
        {
            GetOptions().Accessibility.HighContrastMode = on;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetReduceMotion(bool on)
        {
            GetOptions().Accessibility.ReduceMotion = on;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetAltSymbols(bool on)
        {
            GetOptions().Accessibility.AlternativeConstraintSymbols = on;
            SaveOptions();
            NotifyAccessibilityChanged();
        }

        public void SetFontScale(float scale)
        {
            GetOptions().Accessibility.FontScale = scale;
            SaveOptions();
        }

        public void SetHighlightConflicts(bool on)
        {
            GetOptions().Gameplay.HighlightConflicts = on;
            SaveOptions();
        }

        public void SetFullscreen(bool on)
        {
            GetOptions().Graphics.Fullscreen = on;
            Screen.fullScreen = on;
            SaveOptions();
        }

        public void SetResolution(int width, int height)
        {
            var opt = GetOptions();
            opt.Graphics.ResolutionWidth = width;
            opt.Graphics.ResolutionHeight = height;
            Screen.SetResolution(width, height, opt.Graphics.Fullscreen);
            SaveOptions();
        }

        private void SaveOptions()
        {
            var envelope = _saveFile.Load();
            envelope.PlayerProfile.Options = _options;
            _saveFile.Save(envelope);
        }

        private void NotifyAccessibilityChanged()
        {
            var runScreen = FindFirstObjectByType<InRunController>();
            runScreen?.NotifyAccessibilityChanged();
        }
    }
}
