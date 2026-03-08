using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusicController : MonoBehaviour
    {
        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private AudioSource _source;
        private AudioClip _clip8;
        private AudioClip _clip16;
        private int _activeStyle = -1;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.28f;

            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            _clip8 = BuildGardenClip("MenuChill8", is8Bit: true);
            _clip16 = BuildGardenClip("MenuChill16", is8Bit: false);
        }

        private void Start()
        {
            ApplyStyle(_profile.Options.Audio.MenuMusicStyleIndex);
        }

        private void Update()
        {
            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            ApplyStyle(_profile.Options.Audio.MenuMusicStyleIndex);
            _source.volume = Mathf.Clamp01(_profile.Options.Audio.MasterVolume * _profile.Options.Audio.MusicVolume) * 0.35f;
            _source.mute = _profile.Options.Audio.MuteAll;
        }

        private void ApplyStyle(int style)
        {
            var clamped = Mathf.Clamp(style, 0, 1);
            if (_activeStyle == clamped && _source.isPlaying)
            {
                return;
            }

            _activeStyle = clamped;
            _source.clip = clamped == 0 ? _clip8 : _clip16;
            if (_source.clip != null)
            {
                _source.Play();
            }
        }

        private static AudioClip BuildGardenClip(string clipName, bool is8Bit)
        {
            const int sampleRate = 22050;
            const float seconds = 114f;
            var sampleCount = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[sampleCount];

            var notes = new[] { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 392f, 349.23f, 329.63f, 293.66f, 261.63f, 246.94f };
            var bass = new[] { 110f, 123.47f, 130.81f, 146.83f, 98f, 130.81f };

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var step = (int)(t * 1.6f) % notes.Length;
                var bassStep = (int)(t * 0.8f) % bass.Length;
                var env = 0.5f + (0.5f * Mathf.Sin(2f * Mathf.PI * t / seconds));

                var lead = is8Bit
                    ? Square(notes[step], t, 0.35f)
                    : Triangle(notes[step], t, 0.30f);

                var chord = is8Bit
                    ? Square(notes[(step + 2) % notes.Length], t, 0.20f)
                    : Mathf.Sin(2f * Mathf.PI * notes[(step + 2) % notes.Length] * t) * 0.18f;

                var low = Mathf.Sin(2f * Mathf.PI * bass[bassStep] * t) * 0.16f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * (is8Bit ? 880f : 1320f) * t) * 0.02f;
                var pulse = 0.9f + 0.1f * Mathf.Sin(t * Mathf.PI * 0.125f);

                var sample = (lead + chord + low + shimmer) * env * pulse;
                if (is8Bit)
                {
                    sample = Mathf.Round(sample * 24f) / 24f;
                }

                data[i] = Mathf.Clamp(sample, -0.8f, 0.8f);
            }

            // Blend loop seam to avoid clicks and make loop transitions smoother.
            var seamSamples = Mathf.Min(sampleCount / 4, Mathf.RoundToInt(sampleRate * 4f));
            for (var i = 0; i < seamSamples; i++)
            {
                var t = i / (float)Mathf.Max(1, seamSamples - 1);
                var w = 0.5f - (0.5f * Mathf.Cos(t * Mathf.PI));
                var a = data[i];
                var b = data[sampleCount - seamSamples + i];
                var blended = Mathf.Lerp(a, b, w);
                data[i] = blended;
                data[sampleCount - seamSamples + i] = blended;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Square(float frequency, float t, float amplitude)
        {
            var phase = Mathf.Sin(2f * Mathf.PI * frequency * t);
            return (phase >= 0f ? 1f : -1f) * amplitude;
        }

        private static float Triangle(float frequency, float t, float amplitude)
        {
            var cycle = t * frequency;
            var frac = cycle - Mathf.Floor(cycle);
            var tri = (4f * Mathf.Abs(frac - 0.5f)) - 1f;
            return tri * amplitude;
        }
    }
}
