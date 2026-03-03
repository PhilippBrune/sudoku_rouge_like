using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class OptionsController : MonoBehaviour
    {
        [SerializeField] private bool requireRestartForExclusiveFullscreen;

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        public OptionsState Options => _profile.Options;

        private void Awake()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }
        }

        public void SetLanguage(LanguageOption language)
        {
            Options.Language = language;
            Persist();
        }

        public void SetMasterVolume(float value)
        {
            Options.Audio.MasterVolume = Mathf.Clamp01(value);
            Persist();
        }

        public void SetMusicVolume(float value)
        {
            Options.Audio.MusicVolume = Mathf.Clamp01(value);
            Persist();
        }

        public void SetSfxVolume(float value)
        {
            Options.Audio.SfxVolume = Mathf.Clamp01(value);
            Persist();
        }

        public void SetResolution(int width, int height, bool fullscreen)
        {
            Screen.SetResolution(width, height, fullscreen);
            Options.Graphics.Width = width;
            Options.Graphics.Height = height;
            Options.Graphics.Fullscreen = fullscreen;
            Persist();
        }

        public bool RequiresRestartForResolutionModeSwitch(bool exclusiveFullscreen)
        {
            return requireRestartForExclusiveFullscreen && exclusiveFullscreen;
        }

        public void SetHighContrast(bool enabled)
        {
            Options.Accessibility.HighContrastMode = enabled;
            Persist();
        }

        public void SetHighlightConflicts(bool enabled)
        {
            Options.Gameplay.HighlightConflicts = enabled;
            Persist();
        }

        private void Persist()
        {
            var envelope = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = _profile.Options },
                MetaProgress = _profile.Meta,
                TutorialProgress = _profile.TutorialProgress,
                Statistics = _profile.Stats,
                Mastery = _profile.Mastery,
                Completion = _profile.Completion
            };

            _save.SaveProfile(envelope);
        }
    }
}
