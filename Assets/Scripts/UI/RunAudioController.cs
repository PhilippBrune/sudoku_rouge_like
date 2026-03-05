using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RunAudioController : MonoBehaviour
    {
        public enum Context
        {
            Path,
            Puzzle,
            Shop,
            Rest
        }

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private AudioClip _puzzleLoop;
        private AudioClip _shopLoop;
        private AudioClip _restLoop;
        private AudioClip _wrongSfx;
        private AudioClip _solvedSfx;

        private Context _context = Context.Path;

        private void Awake()
        {
            _musicSource = GetComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;

            var sfxGo = new GameObject("RunSfxSource", typeof(AudioSource));
            sfxGo.transform.SetParent(transform, false);
            _sfxSource = sfxGo.GetComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            // Ensure a listener exists so runtime-generated clips are audible in minimal prototype scenes.
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.gameObject.AddComponent<AudioListener>();
                }
            }

            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            _puzzleLoop = BuildPuzzleLoop();
            _shopLoop = BuildShopLoop();
            _restLoop = BuildRestLoop();
            _wrongSfx = BuildWrongSfx();
            _solvedSfx = BuildSolvedSfx();
        }

        private void Start()
        {
            SetContext(Context.Path);
        }

        private void Update()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            var muted = _profile.Options.Audio.MuteAll;
            var baseVolume = Mathf.Clamp01(_profile.Options.Audio.MasterVolume);
            _musicSource.mute = muted;
            _sfxSource.mute = muted;
            _musicSource.volume = baseVolume * Mathf.Clamp01(_profile.Options.Audio.MusicVolume) * 0.75f;
            _sfxSource.volume = baseVolume * Mathf.Clamp01(_profile.Options.Audio.SfxVolume) * 0.55f;

            if (_context == Context.Puzzle && !_musicSource.mute)
            {
                if (_musicSource.clip != _puzzleLoop)
                {
                    _musicSource.clip = _puzzleLoop;
                }

                if (_musicSource.clip != null && !_musicSource.isPlaying)
                {
                    _musicSource.Play();
                }
            }
        }

        public void SetContext(Context context)
        {
            if (_context == context && _musicSource.isPlaying)
            {
                return;
            }

            _context = context;
            _musicSource.clip = context switch
            {
                Context.Puzzle => _puzzleLoop,
                Context.Shop => _shopLoop,
                Context.Rest => _restLoop,
                _ => null
            };

            if (_musicSource.clip == null)
            {
                _musicSource.Stop();
                return;
            }

            _musicSource.Play();
        }

        public void PlayWrongPlacement()
        {
            if (_wrongSfx != null)
            {
                _sfxSource.PlayOneShot(_wrongSfx, 1f);
            }
        }

        public void PlayPuzzleSolved()
        {
            if (_solvedSfx != null)
            {
                _sfxSource.PlayOneShot(_solvedSfx, 1f);
            }
        }

        private static AudioClip BuildPuzzleLoop()
        {
            const int sampleRate = 22050;
            // 10x longer loop for puzzle solving sessions.
            const float seconds = 280f;
            var sampleCount = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[sampleCount];

            var notes = new[] { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 329.63f, 293.66f, 261.63f, 246.94f };
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var step = (int)(t * 1.8f) % notes.Length;
                var pulse = 0.55f + 0.45f * Mathf.Sin(t * 0.35f * Mathf.PI);
                var lead = Mathf.Sin(2f * Mathf.PI * notes[step] * t) * 0.21f;
                var harmony = Mathf.Sin(2f * Mathf.PI * (notes[(step + 3) % notes.Length] * 0.5f) * t) * 0.14f;
                var bell = Mathf.Sin(2f * Mathf.PI * notes[(step + 5) % notes.Length] * t) * 0.06f;
                data[i] = Mathf.Clamp((lead + harmony + bell) * pulse, -0.65f, 0.65f);
            }

            var clip = AudioClip.Create("RunPuzzleLoop", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildShopLoop()
        {
            const int sampleRate = 22050;
            const float seconds = 20f;
            var sampleCount = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[sampleCount];
            var notes = new[] { 392f, 440f, 523.25f, 659.25f };

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var slot = Mathf.FloorToInt(t * 1.2f) % notes.Length;
                var local = t % 0.83f;
                var decay = Mathf.Exp(-local * 6f);
                var pluck = Mathf.Sin(2f * Mathf.PI * notes[slot] * t) * decay * 0.25f;
                var low = Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.07f;
                data[i] = Mathf.Clamp(pluck + low, -0.55f, 0.55f);
            }

            var clip = AudioClip.Create("RunShopLoop", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildRestLoop()
        {
            const int sampleRate = 22050;
            const float seconds = 22f;
            var sampleCount = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var wind = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.08f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.03f;

                var chirpA = Chirp(t, 1.4f, 1320f, 1560f, 0.11f);
                var chirpB = Chirp(t + 0.23f, 1.9f, 990f, 1260f, 0.09f);
                var chirpC = Chirp(t + 0.57f, 2.7f, 1180f, 1420f, 0.07f);

                data[i] = Mathf.Clamp(wind + chirpA + chirpB + chirpC, -0.50f, 0.50f);
            }

            var clip = AudioClip.Create("RunRestLoop", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Chirp(float t, float rate, float f0, float f1, float amp)
        {
            var phase = t * rate;
            var frac = phase - Mathf.Floor(phase);
            if (frac > 0.18f)
            {
                return 0f;
            }

            var k = frac / 0.18f;
            var freq = Mathf.Lerp(f0, f1, k);
            var env = Mathf.Sin(k * Mathf.PI);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env * amp;
        }

        private static AudioClip BuildWrongSfx()
        {
            const int sampleRate = 22050;
            const float seconds = 0.18f;
            var sampleCount = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var env = Mathf.Exp(-t * 12f);
                var buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 170f * t)) * 0.22f;
                var drop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(280f, 130f, t / seconds) * t) * 0.20f;
                data[i] = Mathf.Clamp((buzz + drop) * env, -0.8f, 0.8f);
            }

            var clip = AudioClip.Create("WrongPlacementSfx", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildSolvedSfx()
        {
            const int sampleRate = 22050;
            const float seconds = 0.85f;
            var sampleCount = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[sampleCount];
            var tones = new[] { 523.25f, 659.25f, 783.99f };

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var env = Mathf.Exp(-t * 2.2f);
                var signal = 0f;
                for (var n = 0; n < tones.Length; n++)
                {
                    signal += Mathf.Sin(2f * Mathf.PI * tones[n] * t) * (0.19f - (n * 0.03f));
                }

                data[i] = Mathf.Clamp(signal * env, -0.8f, 0.8f);
            }

            var clip = AudioClip.Create("PuzzleSolvedSfx", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
