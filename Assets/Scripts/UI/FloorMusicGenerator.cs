using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class FloorMusicGenerator
    {
        private const int SampleRate = 22050;

        private static readonly float[] PentatonicDMinor = { 293.66f, 349.23f, 392f, 440f, 523.25f };
        private static readonly float[] AMinor = { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 392f };
        private static readonly float[] PentatonicGMajor = { 392f, 440f, 493.88f, 587.33f, 659.25f };
        private static readonly float[] EMinor = { 329.63f, 369.99f, 392f, 440f, 493.88f, 523.25f, 587.33f };
        private static readonly float[] CMinor = { 261.63f, 293.66f, 311.13f, 349.23f, 392f, 415.30f, 466.16f };
        private static readonly float[] CMajor = { 261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f, 493.88f };

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

        private static AudioClip BuildBambooCourtyard()
        {
            const float seconds = 32f;
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

                var breathEnv = 0.6f + 0.4f * Mathf.Sin(t * 0.3f * Mathf.PI);
                var phase = 2f * Mathf.PI * noteFreq * t;
                var shakuhachi = TriangleWave(phase) * 0.16f * breathEnv;

                var kotoPhase = (beat * 0.5f) % 1f;
                var kotoDecay = Mathf.Exp(-kotoPhase * beatLen * 4f);
                var kotoFreq = PentatonicDMinor[(noteIdx + 2) % PentatonicDMinor.Length] * 0.5f;
                var koto = Mathf.Sin(2f * Mathf.PI * kotoFreq * t) * 0.10f * kotoDecay;

                var chimePhase = (beat * 0.125f) % 1f;
                var chimeDecay = Mathf.Exp(-chimePhase * beatLen * 8f * 3f);
                var chime = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.04f * chimeDecay;

                var wind = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.06f;

                data[i] = Mathf.Clamp(shakuhachi + koto + chime + wind, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_BambooCourt", count, data);
        }

        private static AudioClip BuildMossGarden()
        {
            const float seconds = 36f;
            const float bpm = 60f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;
            var noiseSeed = 42.17f;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                var rain = (Mathf.Sin(t * 1247f + noiseSeed) * Mathf.Sin(t * 3719f) +
                            Mathf.Sin(t * 5113f + 1.3f) * Mathf.Sin(t * 7919f)) * 0.04f;

                var bellPhase = t % 30f;
                var bellDecay = Mathf.Exp(-bellPhase * 0.5f);
                var bell = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.7f +
                            Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.2f +
                            Mathf.Sin(2f * Mathf.PI * 1318f * t) * 0.1f) * 0.08f * bellDecay;

                var drone = (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.08f +
                             Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.05f);

                var frogPhase = t % 12f;
                var frogEnv = frogPhase < 0.15f ? Mathf.Exp(-frogPhase * 20f) : 0f;
                var frog = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(380f, 320f, frogPhase / 0.15f) * t) * 0.05f * frogEnv;

                var beat = t / beatLen;
                var melIdx = (int)(beat * 0.25f) % AMinor.Length;
                var melPhase = (beat * 0.25f) % 1f;
                var melDecay = Mathf.Exp(-melPhase * beatLen * 4f * 3f);
                var melody = Mathf.Sin(2f * Mathf.PI * AMinor[melIdx] * 0.5f * t) * 0.06f * melDecay;

                data[i] = Mathf.Clamp(rain + bell + drone + frog + melody, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_MossGarden", count, data);
        }

        private static AudioClip BuildKoiTerrace()
        {
            const float seconds = 40f;
            const float bpm = 75f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                var water = (Mathf.Sin(t * 2341f) * Mathf.Sin(t * 4517f) +
                             Mathf.Sin(t * 6133f + 0.7f) * Mathf.Cos(t * 1597f)) * 0.03f;
                var waterSwell = 0.7f + 0.3f * Mathf.Sin(t * 0.15f * Mathf.PI);
                water *= waterSwell;

                var arpIdx = (int)beat % PentatonicGMajor.Length;
                var arpPhase = beat % 1f;
                var arpDecay = Mathf.Exp(-arpPhase * beatLen * 6f);
                var arp = Mathf.Sin(2f * Mathf.PI * PentatonicGMajor[arpIdx] * t) * 0.14f * arpDecay;

                var shamPhase = (beat * 0.25f) % 1f;
                var shamDecay = Mathf.Exp(-shamPhase * beatLen * 4f * 4f);
                var shamIdx = (int)(beat * 0.25f) % PentatonicGMajor.Length;
                var shamisen = Mathf.Sin(2f * Mathf.PI * PentatonicGMajor[shamIdx] * 0.5f * t) * 0.09f * shamDecay;

                var splashPhase = t % 18f;
                var splashEnv = splashPhase < 0.2f ? Mathf.Exp(-splashPhase * 15f) : 0f;
                var splash = (Mathf.Sin(t * 3100f) * Mathf.Sin(t * 7700f)) * 0.06f * splashEnv;

                data[i] = Mathf.Clamp(water + arp + shamisen + splash, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_KoiTerrace", count, data);
        }

        private static AudioClip BuildStoneLanternWalk()
        {
            // Relaxed evening pace — slow koto arpeggios, breathy flute drone and
            // warm bell overtones replace the Fire/Taiko combination.
            const float seconds = 48f;
            const float bpm     = 52f;
            var count   = Mathf.RoundToInt(SampleRate * seconds);
            var data    = new float[count];
            var beatLen = 60f / bpm;

            // Pentatonic D-minor for a calm, introspective feel
            var scale = PentatonicDMinor;

            for (var i = 0; i < count; i++)
            {
                var t    = (float)i / SampleRate;
                var beat = t / beatLen;

                // Slow breath envelope (inhale/exhale every ~4 beats)
                var breath = 0.55f + 0.45f * Mathf.Sin(t * (Mathf.PI / (2f * beatLen)));

                // Shakuhachi (flute) — long held notes, very soft
                var fluteIdx   = (int)(beat * 0.25f) % scale.Length;
                var fluteFreq  = scale[fluteIdx] * 0.5f; // one octave down
                var fluteVib   = 1f + 0.004f * Mathf.Sin(t * 4.2f * Mathf.PI);
                var flute      = TriangleWave(2f * Mathf.PI * fluteFreq * fluteVib * t) * 0.13f * breath;

                // Koto plucks — gentle arpeggios, two per beat
                var kotoPhase  = (beat * 2f) % 1f;
                var kotoDecay  = Mathf.Exp(-kotoPhase * beatLen * 0.5f * 6f);
                var kotoIdx    = (int)(beat * 2f) % scale.Length;
                var koto       = Mathf.Sin(2f * Mathf.PI * scale[kotoIdx] * t) * 0.11f * kotoDecay;

                // Distant bell overtone — swell in and fade every 12 s
                var bellCycle  = t % 12f;
                var bellEnv    = Mathf.Exp(-bellCycle * 0.5f) * 0.06f;
                var bell       = (Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.6f
                                + Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.3f
                                + Mathf.Sin(2f * Mathf.PI * 1320f  * t) * 0.1f) * bellEnv;

                // Very gentle wind-texture underscore
                var wind = (Mathf.Sin(t * 2313f) * Mathf.Sin(t * 4871f)) * 0.02f
                           * (0.5f + 0.5f * Mathf.Sin(t * 0.11f * Mathf.PI));

                data[i] = Mathf.Clamp(flute + koto + bell + wind, -0.55f, 0.55f);
            }

            return MakeClip("FloorLoop_StoneLantern", count, data);
        }

        private static AudioClip BuildShrineSummit()
        {
            const float seconds = 48f;
            const float bpm = 80f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                var progress = t / seconds;
                var scale = progress < 0.67f ? CMinor : CMajor;
                var intensity = Mathf.Clamp01(progress * 1.2f);

                var kotoIdx = (int)beat % scale.Length;
                var kotoPhase = beat % 1f;
                var kotoDecay = Mathf.Exp(-kotoPhase * beatLen * 5f);
                var koto = Mathf.Sin(2f * Mathf.PI * scale[kotoIdx] * t) * 0.13f * kotoDecay;

                var shakIdx = (int)(beat * 0.5f) % scale.Length;
                var shakPhase = (beat * 0.5f) % 1f;
                var shakBreath = 0.6f + 0.4f * Mathf.Sin(shakPhase * Mathf.PI);
                var shak = TriangleWave(2f * Mathf.PI * scale[(shakIdx + 2) % scale.Length] * t) * 0.09f * shakBreath;

                var shamPhase2 = (beat * 2f) % 1f;
                var shamDecay2 = Mathf.Exp(-shamPhase2 * beatLen * 0.5f * 12f);
                var shamIdx2 = (int)(beat * 2f) % scale.Length;
                var shamisen = Mathf.Sin(2f * Mathf.PI * scale[shamIdx2] * 0.5f * t) * 0.07f * shamDecay2 * intensity;

                var taikoPhase2 = beat % 1f;
                var taikoEnv2 = taikoPhase2 < 0.08f ? Mathf.Exp(-taikoPhase2 * beatLen * 10f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.12f * taikoEnv2 * intensity;

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

        public static AudioClip BuildBossLayer(int floorIndex)
        {
            var baseBpm = floorIndex switch { 0 => 70f, 1 => 60f, 2 => 75f, 3 => 65f, 4 => 80f, _ => 70f };
            var bpm = baseBpm + 10f;
            const float seconds = 20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;

                var taikoPhase = (beat * 0.5f) % 1f;
                var taikoEnv = taikoPhase < 0.1f ? Mathf.Exp(-taikoPhase * beatLen * 2f * 8f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.14f * taikoEnv;

                var tremolo = Mathf.Sin(t * 12f * Mathf.PI);
                var dissonant = (Mathf.Sin(2f * Mathf.PI * 185f * t) +
                                 Mathf.Sin(2f * Mathf.PI * 261.63f * t)) * 0.04f * (0.5f + 0.5f * tremolo);

                var hbPhase = (t * 1.2f) % 1f;
                var hb1 = hbPhase < 0.1f ? Mathf.Exp(-hbPhase * 15f) : 0f;
                var hb2 = (hbPhase > 0.2f && hbPhase < 0.3f) ? Mathf.Exp(-(hbPhase - 0.2f) * 18f) : 0f;
                var heartbeat = Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.10f * (hb1 + hb2 * 0.7f);

                data[i] = Mathf.Clamp(taiko + dissonant + heartbeat, -0.50f, 0.50f);
            }

            return MakeClip($"BossLayer_F{floorIndex}", count, data);
        }

        public static AudioClip BuildMainMenuLoop()
        {
            const float seconds = 30f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                var wind = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.06f +
                           Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.03f +
                           Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.04f;
                var windSwell = 0.7f + 0.3f * Mathf.Sin(t * 0.1f * Mathf.PI);
                wind *= windSwell;

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

        public static AudioClip BuildEndlessZenLoop()
        {
            const float seconds = 45f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                var drone = Mathf.Sin(2f * Mathf.PI * 82.41f * t) * 0.07f +
                            Mathf.Sin(2f * Mathf.PI * 123.47f * t) * 0.04f;
                var noiseMod = 0.5f + 0.5f * Mathf.Sin(t * 0.05f * Mathf.PI);
                var noise = Mathf.Sin(t * 1811f) * Mathf.Sin(t * 4219f) * 0.02f * noiseMod;

                var kotoInterval = 30f;
                var kotoPhase = t % kotoInterval;
                var kotoDecay = kotoPhase < 3f ? Mathf.Exp(-kotoPhase * 1.2f) : 0f;
                var noteSelect = (int)(t / kotoInterval) % PentatonicDMinor.Length;
                var koto = Mathf.Sin(2f * Mathf.PI * PentatonicDMinor[noteSelect] * 0.5f * t) * 0.05f * kotoDecay;

                data[i] = Mathf.Clamp(drone + noise + koto, -0.40f, 0.40f);
            }

            return MakeClip("EndlessZenLoop", count, data);
        }

        public static AudioClip BuildSpiritTrialsLoop()
        {
            const float seconds = 30f;
            const float bpm = 100f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var beatLen = 60f / bpm;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var beat = t / beatLen;
                var layerProgress = Mathf.Clamp01(t / 60f);

                var blockPhase = beat % 1f;
                var blockEnv = blockPhase < 0.02f ? Mathf.Exp(-blockPhase * beatLen * 40f) : 0f;
                var block = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.12f * blockEnv;

                var rimPhase = (beat + 0.5f) % 1f;
                var rimEnv = rimPhase < 0.015f ? Mathf.Exp(-rimPhase * beatLen * 50f) : 0f;
                var rim = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.08f * rimEnv * (0.5f + 0.5f * layerProgress);

                var shamPhase = (beat * 0.25f) % 1f;
                var shamEnv = shamPhase < 0.05f ? Mathf.Exp(-shamPhase * beatLen * 4f * 10f) : 0f;
                var shamIdx = (int)(beat * 0.25f) % CMinor.Length;
                var shamisen = Mathf.Sin(2f * Mathf.PI * CMinor[shamIdx] * t) * 0.08f * shamEnv * layerProgress;

                var taikoLayer = t > 30f ? Mathf.Clamp01((t - 30f) / 30f) : 0f;
                var taikoPhase = (beat * 0.5f) % 1f;
                var taikoEnv = taikoPhase < 0.06f ? Mathf.Exp(-taikoPhase * beatLen * 2f * 10f) : 0f;
                var taiko = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.10f * taikoEnv * taikoLayer;

                data[i] = Mathf.Clamp(block + rim + shamisen + taiko, -0.55f, 0.55f);
            }

            return MakeClip("SpiritTrialsLoop", count, data);
        }

        public static AudioClip BuildFloorTransitionStinger()
        {
            const float seconds = 5f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                var windEnv = t < 3f ? Mathf.Sin(t / 3f * Mathf.PI) : 0f;
                var wind = (Mathf.Sin(t * 1317f) * Mathf.Sin(t * 3719f) +
                            Mathf.Sin(t * 5813f) * Mathf.Sin(t * 2111f)) * 0.06f * windEnv;

                var chimeActive = t > 1f && t < 3f ? 1f : 0f;
                var chimePitch = Mathf.Lerp(523.25f, 1046.5f, (t - 1f) / 2f);
                var chimeEnv = chimeActive * Mathf.Exp(-(t - 1f) * 1.5f);
                var chime = Mathf.Sin(2f * Mathf.PI * chimePitch * t) * 0.10f * chimeEnv;

                var tailWind = t > 4f ? (t - 4f) / 1f * 0.02f : 0f;
                var tail = Mathf.Sin(2f * Mathf.PI * 110f * t) * tailWind;

                data[i] = Mathf.Clamp(wind + chime + tail, -0.55f, 0.55f);
            }

            return MakeClip("Stinger_FloorTransition", count, data);
        }

        public static AudioClip BuildGameOverStinger()
        {
            const float seconds = 15f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 0.25f);

                // Low ominous drone
                var drone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f * env;

                // Descending chime (high to low)
                var chimePitch = Mathf.Lerp(880f, 110f, t / seconds);
                var chimeEnv = t < 5f ? Mathf.Exp(-t * 0.8f) : 0f;
                var chime = Mathf.Sin(2f * Mathf.PI * chimePitch * t) * 0.08f * chimeEnv;

                // Wind/noise tail
                var windEnv = t > 3f ? Mathf.Clamp01((t - 3f) / 5f) * Mathf.Exp(-(t - 3f) * 0.15f) : 0f;
                var wind = (Mathf.Sin(t * 1317f) * Mathf.Sin(t * 3719f)) * 0.04f * windEnv;

                // Temple bell toll at 0s, 4s, 8s
                var bell = 0f;
                for (var b = 0; b < 3; b++)
                {
                    var bellStart = b * 4f;
                    if (t >= bellStart && t < bellStart + 4f)
                    {
                        var bt = t - bellStart;
                        var bellEnv = Mathf.Exp(-bt * 1.2f);
                        bell += Mathf.Sin(2f * Mathf.PI * 220f * bt) * 0.06f * bellEnv;
                    }
                }

                data[i] = Mathf.Clamp(drone + chime + wind + bell, -0.55f, 0.55f);
            }

            return MakeClip("Stinger_GameOver", count, data);
        }

        public static AudioClip BuildVictoryStinger()
        {
            const float seconds = 20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var masterEnv = t < 18f ? 1f : Mathf.Clamp01((20f - t) / 2f);

                // Ascending koto melody (pentatonic)
                var noteIdx = Mathf.FloorToInt(t / 1.5f) % 5;
                var melodyPitch = PentatonicGMajor[noteIdx] * (1f + Mathf.Floor(t / 7.5f) * 0.5f);
                var noteT = t % 1.5f;
                var noteEnv = Mathf.Exp(-noteT * 2f);
                var koto = Mathf.Sin(2f * Mathf.PI * melodyPitch * t) * 0.12f * noteEnv;

                // Bright sustained chord (major triad)
                var chordEnv = t > 2f ? Mathf.Clamp01((t - 2f) / 3f) : 0f;
                var chord = (Mathf.Sin(2f * Mathf.PI * 392f * t) +
                             Mathf.Sin(2f * Mathf.PI * 493.88f * t) +
                             Mathf.Sin(2f * Mathf.PI * 587.33f * t)) * 0.04f * chordEnv;

                // Temple bell celebration at 0s, 5s, 10s, 15s
                var bell = 0f;
                for (var b = 0; b < 4; b++)
                {
                    var bellStart = b * 5f;
                    if (t >= bellStart && t < bellStart + 3f)
                    {
                        var bt = t - bellStart;
                        var bellEnv = Mathf.Exp(-bt * 1.5f);
                        bell += Mathf.Sin(2f * Mathf.PI * 440f * bt) * 0.07f * bellEnv;
                    }
                }

                // Shimmer
                var shimmer = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.02f *
                              Mathf.Sin(t * 3f) * (t > 5f ? 1f : 0f);

                data[i] = Mathf.Clamp((koto + chord + bell + shimmer) * masterEnv, -0.55f, 0.55f);
            }

            return MakeClip("Stinger_Victory", count, data);
        }

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
