using UnityEngine;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusicController : MonoBehaviour
    {
        private AudioSource _audioSource;
        private int _styleIndex = -1;

        public void Initialize()
        {
            EnsureAudioSource();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            ApplySettings(LoadActiveAudioOptions());
        }

        public void ApplySettings(AudioSettingsModel settings)
        {
            EnsureAudioSource();

            var audio = settings ?? new AudioSettingsModel();
            SetStyle(audio.MenuMusicStyleIndex);
            SetVolume(audio.MuteAll ? 0f : audio.MasterVolume * audio.MusicVolume);
        }

        public void SetStyle(int styleIndex)
        {
            if (_audioSource == null)
                return;

            var normalized = AudioAssetService.NormalizeMenuMusicStyleIndex(styleIndex);
            if (_styleIndex == normalized && _audioSource.clip != null)
                return;

            var wasPlaying = _audioSource.isPlaying;
            _styleIndex = normalized;
            _audioSource.clip = AudioAssetService.GetClip(
                AudioAssetService.GetMenuMusicStyleLoopId(normalized),
                () => BuildFallbackLoop(normalized));

            if (wasPlaying && _audioSource.clip != null)
                _audioSource.Play();
        }

        private static AudioClip BuildFallbackLoop(int styleIndex)
        {
            switch (AudioAssetService.NormalizeMenuMusicStyleIndex(styleIndex))
            {
                case 1:
                    return ProceduralSfxLibrary.BuildPuzzleLoop();
                case 2:
                    return ProceduralSfxLibrary.BuildRestLoop();
                default:
                    return ProceduralSfxLibrary.BuildMenuLoop();
            }
        }

        public void SetVolume(float volume)
        {
            if (_audioSource != null)
                _audioSource.volume = volume;
        }

        public void Play()
        {
            if (_audioSource != null && !_audioSource.isPlaying)
                _audioSource.Play();
        }

        public void Stop()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Stop();
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null)
                return;

            if (!TryGetComponent(out _audioSource))
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private static AudioSettingsModel LoadActiveAudioOptions()
        {
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var profile = new ProfileService(save);
            return profile.LoadOptions().Audio;
        }
    }
}
