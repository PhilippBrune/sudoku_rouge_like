using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Procedurally generates per-floor music loops, context tracks, boss layers, and stingers.
    /// All clips: 22050 Hz, mono, normalized ~-6dB peak.
    /// Floor themes from AudioVisualDirection spec.
    /// </summary>
    public static class FloorMusicGenerator
    {
        private const int SampleRate = 22050;

        // D minor pentatonic: D4, F4, G4, A4, C5
        private static readonly float[] PentatonicDMinor = { 293.66f, 349.23f, 392f, 440f, 523.25f };
        // A minor: A3, B3, C4, D4, E4, F4, G4
        private static readonly float[] AMinor = { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 392f };
        // G major pentatonic: G4, A4, B4, D5, E5
        private static readonly float[] PentatonicGMajor = { 392f, 440f, 493.88f, 587.33f, 659.25f };
        // E minor: E4, F#4, G4, A4, B4, C5, D5
        private static readonly float[] EMinor = { 329.63f, 369.99f, 392f, 440f, 493.88f, 523.25f, 587.33f };
        // C minor: C4, D4, Eb4, F4, G4, Ab4, Bb4
        private static readonly float[] CMinor = { 261.63f, 293.66f, 311.13f, 349.23f, 392f, 415.30f, 466.16f };
        // C major (for resolve): C4, D4, E4, F4, G4, A4, B4
        private static readonly float[] CMajor = { 261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f, 493.88f };

        /// <summary>
        /// Build the loop for a given floor index (0-4).
        /// </summary>
        public static AudioClip BuildFloorLoop(int floorIndex)
        {
            return floorIndex switch
            {
                0 => BuildBambooCourtyard(),
                1 => BuildMossGarden(),
                2 => BuildKoiTerrace(),
                3 => BuildStoneLanternWalk(),
                4 => BuildShrineSummit(),
                _ => BuildBambooCourtyard()
            };
        }

        // Floor 0 — Bamboo Courtyard: 70 BPM, D minor pentatonic, shakuhachi lead + koto + wind chime
        private static AudioClip BuildBambooCourtyard()
        {
            const float seconds = 120f;
            const float bpm = 70f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;
                var noteIdx = (int)(beat * 0.5f) % PentatonicDMinor.Length;
                var noteFreq = PentatonicDMinor[noteIdx];

                // Shakuhachi lead (triangle wave with breath envelope)
                var breathEnv = 0.6f + 0.4f * Mathf.Sin(t * 0.3f * Mathf.PI);
                var phase = 2f * Mathf.PI * noteFreq * t;
                var shakuhachi = TriangleWave(phase) * 0.16f * breathEnv;

                // Koto (finger-picked, decaying plucks every 2 beats)
                var kotoPhase = (beat * 0.5f) % 1f;
                var kotoDecay = Mathf.Exp(-kotoPhase * beatLen * 4f);
                var kotoFreq = PentatonicDMinor[(noteIdx + 2) % PentatonicDMinor.Length] * 0.5f;
                var koto = Mathf.Sin(2f * Mathf.PI * kotoFreq * t) * 0.10f * kotoDecay;

                // Wind chime accent (sparse, every ~8 beats)
                var chimePhase = (beat * 0.125f) % 1f;
                var chimeDecay = Mathf.Exp(-chimePhase * beatLen * 8f * 3f);
                var chime = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.04f * chimeDecay;

                // Subtle wind pad (low drone)
                var wind = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.06f; // D2

                data[i] = Mathf.Clamp(shakuhachi + koto + chime + wind, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_BambooCourt", count, data);
        }

        // Floor 1 — Moss Garden: 60 BPM, A minor, rain texture + temple bell + koto drone
        private static AudioClip BuildMossGarden()
        {
            const float seconds = 150f;
            const float bpm = 60f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            // Pre-compute a pseudo-random seed for rain noise
            var noiseSeed = 42.17f;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                // Rain texture (filtered noise using multiple sine interference)
                var rain = (Mathf.Sin(t * 1247f + noiseSeed) * Mathf.Sin(t * 3719f) +
                            Mathf.Sin(t * 5113f + 1.3f) * Mathf.Sin(t * 7919f)) * 0.04f;

                // Temple bell (every ~30 seconds)
                var bellPhase = t % 30f;
                var bellDecay = Mathf.Exp(-bellPhase * 0.5f);
                var bell = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.7f +
                            Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.2f +
                            Mathf.Sin(2f * Mathf.PI * 1318f * t) * 0.1f) * 0.08f * bellDecay;

                // Low koto drone (A2 + E3)
                var drone = (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.08f +
                             Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.05f);

                // Occasional frog chirp (every ~12 seconds)
                var frogPhase = t % 12f;
                var frogEnv = frogPhase < 0.15f ? Mathf.Exp(-frogPhase * 20f) : 0f;
                var frog = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(380f, 320f, frogPhase / 0.15f) * t) * 0.05f * frogEnv;

                // Slow melodic movement (very sparse notes)
                var beat = t / beatLen;
                var melIdx = (int)(beat * 0.25f) % AMinor.Length;
                var melPhase = (beat * 0.25f) % 1f;
                var melDecay = Mathf.Exp(-melPhase * beatLen * 4f * 3f);
                var melody = Mathf.Sin(2f * Mathf.PI * AMinor[melIdx] * 0.5f * t) * 0.06f * melDecay;

                data[i] = Mathf.Clamp(rain + bell + drone + frog + melody, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_MossGarden", count, data);
        }

        // Floor 2 — Koi Terrace: 75 BPM, G major pentatonic, water texture + koto arpeggios
        private static AudioClip BuildKoiTerrace()
        {
            const float seconds = 140f;
            const float bpm = 75f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                // Flowing water texture
                var water = (Mathf.Sin(t * 2341f) * Mathf.Sin(t * 4517f) +
                             Mathf.Sin(t * 6133f + 0.7f) * Mathf.Cos(t * 1597f)) * 0.03f;
                var waterSwell = 0.7f + 0.3f * Mathf.Sin(t * 0.15f * Mathf.PI);
                water *= waterSwell;

                // Koto arpeggios (one note per beat, cycling pentatonic)
                var arpIdx = (int)beat % PentatonicGMajor.Length;
                var arpPhase = beat % 1f;
                var arpDecay = Mathf.Exp(-arpPhase * beatLen * 6f);
                var arp = Mathf.Sin(2f * Mathf.PI * PentatonicGMajor[arpIdx] * t) * 0.14f * arpDecay;

                // Shamisen plucks (every 4 beats, octave lower)
                var shamPhase = (beat * 0.25f) % 1f;
                var shamDecay = Mathf.Exp(-shamPhase * beatLen * 4f * 4f);
                var shamIdx = (int)(beat * 0.25f) % PentatonicGMajor.Length;
                var shamisen = Mathf.Sin(2f * Mathf.PI * PentatonicGMajor[shamIdx] * 0.5f * t) * 0.09f * shamDecay;

                // Fish splash (every ~18 seconds)
                var splashPhase = t % 18f;
                var splashEnv = splashPhase < 0.2f ? Mathf.Exp(-splashPhase * 15f) : 0f;
                var splash = (Mathf.Sin(t * 3100f) * Mathf.Sin(t * 7700f)) * 0.06f * splashEnv;

                data[i] = Mathf.Clamp(water + arp + shamisen + splash, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_KoiTerrace", count, data);
        }

        // Floor 3 — Stone Lantern Walk: 65 BPM, E minor, fire texture + erhu drone + sparse taiko
        private static AudioClip BuildStoneLanternWalk()
        {
            const float seconds = 160f;
            const float bpm = 65f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                // Crackling fire texture (random-ish noise bursts)
                var crackle = Mathf.Sin(t * 4217f) * Mathf.Sin(t * 6311f) * Mathf.Sin(t * 1973f);
                var crackleEnv = Mathf.Abs(Mathf.Sin(t * 7.3f)) > 0.85f ? 1f : 0.2f;
                var fire = crackle * 0.04f * crackleEnv;

                // Erhu/cello drone (E2 + B2, with slow vibrato)
                var vibrato = 1f + 0.003f * Mathf.Sin(t * 5f * Mathf.PI);
                var erhu = (TriangleWave(2f * Mathf.PI * 82.41f * vibrato * t) * 0.09f +
                            Mathf.Sin(2f * Mathf.PI * 123.47f * t) * 0.05f);

                // Sparse taiko at pianissimo (every 8 beats)
                var taikoPhase = (beat * 0.125f) % 1f;
                var taikoEnv = taikoPhase < 0.05f ? Mathf.Exp(-taikoPhase * beatLen * 8f * 8f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.10f * taikoEnv;

                // Melodic movement (slow, sparse E minor)
                var melIdx = (int)(beat * 0.2f) % EMinor.Length;
                var melPhase = (beat * 0.2f) % 1f;
                var melDecay = Mathf.Exp(-melPhase * beatLen * 5f * 3f);
                var melody = TriangleWave(2f * Mathf.PI * EMinor[melIdx] * 0.5f * t) * 0.06f * melDecay;

                // Leaf rustle (very subtle)
                var rustle = Mathf.Sin(t * 8317f) * Mathf.Sin(t * 2113f) * 0.015f;
                var rustleSwell = 0.5f + 0.5f * Mathf.Sin(t * 0.08f * Mathf.PI);
                rustle *= rustleSwell;

                data[i] = Mathf.Clamp(fire + erhu + taiko + melody + rustle, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_StoneLantern", count, data);
        }

        // Floor 4 — Shrine Summit: 80 BPM, C minor → C major resolve, full ensemble
        private static AudioClip BuildShrineSummit()
        {
            const float seconds = 180f;
            const float bpm = 80f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                // Transition from C minor to C major at 2/3 of the loop
                var progress = t / seconds;
                var scale = progress < 0.67f ? CMinor : CMajor;
                var intensity = Mathf.Clamp01(progress * 1.2f); // builds over the loop

                // Koto melody (quarter notes)
                var kotoIdx = (int)beat % scale.Length;
                var kotoPhase = beat % 1f;
                var kotoDecay = Mathf.Exp(-kotoPhase * beatLen * 5f);
                var koto = Mathf.Sin(2f * Mathf.PI * scale[kotoIdx] * t) * 0.13f * kotoDecay;

                // Shakuhachi countermelody (half notes, offset by 2 scale degrees)
                var shakIdx = (int)(beat * 0.5f) % scale.Length;
                var shakPhase = (beat * 0.5f) % 1f;
                var shakBreath = 0.6f + 0.4f * Mathf.Sin(shakPhase * Mathf.PI);
                var shak = TriangleWave(2f * Mathf.PI * scale[(shakIdx + 2) % scale.Length] * t) * 0.09f * shakBreath;

                // Shamisen rhythm (eighth notes, staccato)
                var shamPhase = (beat * 2f) % 1f;
                var shamDecay = Mathf.Exp(-shamPhase * beatLen * 0.5f * 12f);
                var shamIdx2 = (int)(beat * 2f) % scale.Length;
                var shamisen = Mathf.Sin(2f * Mathf.PI * scale[shamIdx2] * 0.5f * t) * 0.07f * shamDecay * intensity;

                // Taiko pulse (quarter notes, building)
                var taikoPhase = beat % 1f;
                var taikoEnv = taikoPhase < 0.08f ? Mathf.Exp(-taikoPhase * beatLen * 10f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.12f * taikoEnv * intensity;

                // Temple bell at loop point (first 3 seconds and last 3 seconds)
                var bellAmp = 0f;
                if (t < 3f) bellAmp = Mathf.Exp(-t * 0.8f) * 0.08f;
                else if (t > seconds - 3f) bellAmp = Mathf.Exp(-(t - (seconds - 3f)) * 0.8f) * 0.08f;
                var bell = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.7f +
                            Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.2f +
                            Mathf.Sin(2f * Mathf.PI * 1568f * t) * 0.1f) * bellAmp;

                data[i] = Mathf.Clamp(koto + shak + shamisen + taiko + bell, -0.60f, 0.60f);
            }

            return MakeClip("FloorLoop_ShrineSummit", count, data);
        }

        // ── Boss Layer ──────────────────────────────────────────────────────

        /// <summary>
        /// Additive boss tension layer. Mixed on top of the active floor loop.
        /// Floor BPM + 10 creates rhythmic tension against the base track.
        /// </summary>
        public static AudioClip BuildBossLayer(int floorIndex)
        {
            var baseBpm = floorIndex switch { 0 => 70f, 1 => 60f, 2 => 75f, 3 => 65f, 4 => 80f, _ => 70f };
            var bpm = baseBpm + 10f;
            const float seconds = 120f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                // Deep taiko pattern (every 2 beats)
                var taikoPhase = (beat * 0.5f) % 1f;
                var taikoEnv = taikoPhase < 0.1f ? Mathf.Exp(-taikoPhase * beatLen * 2f * 8f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.14f * taikoEnv;

                // Dissonant string tremolo (tritone interval)
                var tremolo = Mathf.Sin(t * 12f * Mathf.PI); // tremolo rate
                var dissonant = (Mathf.Sin(2f * Mathf.PI * 185f * t) +
                                 Mathf.Sin(2f * Mathf.PI * 261.63f * t)) * 0.04f * (0.5f + 0.5f * tremolo);

                // Heartbeat low bass pulse (~1.2 Hz)
                var hbPhase = (t * 1.2f) % 1f;
                var hb1 = hbPhase < 0.1f ? Mathf.Exp(-hbPhase * 15f) : 0f;
                var hb2 = (hbPhase > 0.2f && hbPhase < 0.3f) ? Mathf.Exp(-(hbPhase - 0.2f) * 18f) : 0f;
                var heartbeat = Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.10f * (hb1 + hb2 * 0.7f);

                data[i] = Mathf.Clamp(taiko + dissonant + heartbeat, -0.50f, 0.50f);
            }

            return MakeClip($"BossLayer_F{floorIndex}", count, data);
        }

        // ── Context Tracks ──────────────────────────────────────────────────

        /// <summary>
        /// Main menu ambient loop: 55 BPM, ambient koto harmonics over wind pad.
        /// </summary>
        public static AudioClip BuildMainMenuLoop()
        {
            const float seconds = 100f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                // Wind pad (layered sine drones)
                var wind = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.06f +
                           Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.03f +
                           Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.04f;
                var windSwell = 0.7f + 0.3f * Mathf.Sin(t * 0.1f * Mathf.PI);
                wind *= windSwell;

                // Koto harmonics (very sparse, every ~6 seconds)
                var kotoPhase = t % 6f;
                var kotoDecay = Mathf.Exp(-kotoPhase * 1.5f);
                var kotoNote = PentatonicDMinor[(int)(t / 6f) % PentatonicDMinor.Length];
                var kotoHarm = (Mathf.Sin(2f * Mathf.PI * kotoNote * t) * 0.5f +
                                Mathf.Sin(2f * Mathf.PI * kotoNote * 2f * t) * 0.3f +
                                Mathf.Sin(2f * Mathf.PI * kotoNote * 3f * t) * 0.2f) * 0.06f * kotoDecay;

                data[i] = Mathf.Clamp(wind + kotoHarm, -0.45f, 0.45f);
            }

            return MakeClip("MenuLoop", count, data);
        }

        /// <summary>
        /// Endless Zen: ambient drone, no melody, no rhythm. Rare koto note every 20-40s.
        /// </summary>
        public static AudioClip BuildEndlessZenLoop()
        {
            const float seconds = 300f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                // Sine + filtered noise drone
                var drone = Mathf.Sin(2f * Mathf.PI * 82.41f * t) * 0.07f +
                            Mathf.Sin(2f * Mathf.PI * 123.47f * t) * 0.04f;
                var noiseMod = 0.5f + 0.5f * Mathf.Sin(t * 0.05f * Mathf.PI);
                var noise = Mathf.Sin(t * 1811f) * Mathf.Sin(t * 4219f) * 0.02f * noiseMod;

                // Rare koto note (~every 30 seconds, phase-shifted)
                var kotoInterval = 30f;
                var kotoPhase = t % kotoInterval;
                var kotoDecay = kotoPhase < 3f ? Mathf.Exp(-kotoPhase * 1.2f) : 0f;
                var noteSelect = (int)(t / kotoInterval) % PentatonicDMinor.Length;
                var koto = Mathf.Sin(2f * Mathf.PI * PentatonicDMinor[noteSelect] * 0.5f * t) * 0.05f * kotoDecay;

                data[i] = Mathf.Clamp(drone + noise + koto, -0.40f, 0.40f);
            }

            return MakeClip("EndlessZenLoop", count, data);
        }

        /// <summary>
        /// Spirit Trials: rhythmic percussion, building layers every 60s.
        /// </summary>
        public static AudioClip BuildSpiritTrialsLoop()
        {
            const float seconds = 90f;
            const float bpm = 100f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;
                var layerProgress = Mathf.Clamp01(t / 60f); // layers build over 60s

                // Wooden block clicks (every beat)
                var blockPhase = beat % 1f;
                var blockEnv = blockPhase < 0.02f ? Mathf.Exp(-blockPhase * beatLen * 40f) : 0f;
                var block = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.12f * blockEnv;

                // Rim clicks (off-beat, eighth notes)
                var rimPhase = (beat + 0.5f) % 1f;
                var rimEnv = rimPhase < 0.015f ? Mathf.Exp(-rimPhase * beatLen * 50f) : 0f;
                var rim = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.08f * rimEnv * (0.5f + 0.5f * layerProgress);

                // Shamisen stabs (every 4 beats, sparse)
                var shamPhase = (beat * 0.25f) % 1f;
                var shamEnv = shamPhase < 0.05f ? Mathf.Exp(-shamPhase * beatLen * 4f * 10f) : 0f;
                var shamIdx = (int)(beat * 0.25f) % CMinor.Length;
                var shamisen = Mathf.Sin(2f * Mathf.PI * CMinor[shamIdx] * t) * 0.08f * shamEnv * layerProgress;

                // Additional layer: taiko (appears after 30s)
                var taikoLayer = t > 30f ? Mathf.Clamp01((t - 30f) / 30f) : 0f;
                var taikoPhase = (beat * 0.5f) % 1f;
                var taikoEnv = taikoPhase < 0.06f ? Mathf.Exp(-taikoPhase * beatLen * 2f * 10f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.10f * taikoEnv * taikoLayer;

                data[i] = Mathf.Clamp(block + rim + shamisen + taiko, -0.55f, 0.55f);
            }

            return MakeClip("SpiritTrialsLoop", count, data);
        }

        // ── Stingers ────────────────────────────────────────────────────────

        /// <summary>
        /// Floor transition: wind sweep + ascending chime + brief silence. 5s.
        /// </summary>
        public static AudioClip BuildFloorTransitionStinger()
        {
            const float seconds = 5f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                // Wind sweep (0-3s)
                var windEnv = t < 3f ? Mathf.Sin(t / 3f * Mathf.PI) : 0f;
                var wind = (Mathf.Sin(t * 1317f) * Mathf.Sin(t * 3719f) +
                            Mathf.Sin(t * 5813f) * Mathf.Sin(t * 2111f)) * 0.06f * windEnv;

                // Ascending chime (1-3s)
                var chimeActive = t > 1f && t < 3f ? 1f : 0f;
                var chimePitch = Mathf.Lerp(523.25f, 1046.5f, (t - 1f) / 2f);
                var chimeEnv = chimeActive * Mathf.Exp(-(t - 1f) * 1.5f);
                var chime = Mathf.Sin(2f * Mathf.PI * chimePitch * t) * 0.10f * chimeEnv;

                // Silence 3-4s, then faint wind 4-5s
                var tailWind = t > 4f ? (t - 4f) / 1f * 0.02f : 0f;
                var tail = Mathf.Sin(2f * Mathf.PI * 110f * t) * tailWind;

                data[i] = Mathf.Clamp(wind + chime + tail, -0.55f, 0.55f);
            }

            return MakeClip("Stinger_FloorTransition", count, data);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static float TriangleWave(float phase)
        {
            var p = (phase / (2f * Mathf.PI)) % 1f;
            if (p < 0f) p += 1f;
            return 4f * Mathf.Abs(p - 0.5f) - 1f;
        }

        private static AudioClip MakeClip(string name, int sampleCount, float[] data)
        {
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
