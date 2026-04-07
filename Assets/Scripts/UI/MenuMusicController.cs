using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class MenuMusicController : MonoBehaviour
    {
        private AudioSource _audioSource;

        public void Initialize()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0.5f;
            _audioSource.clip = ProceduralSfxLibrary.BuildMenuLoop();
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
    }
}
