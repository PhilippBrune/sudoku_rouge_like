using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class ProceduralSfxLibrary
    {
        private const int SampleRate = 22050;

        public static AudioClip BuildCellSelectSfx()
        {
            const float seconds = 0.08f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 30f);
                data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.10f * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_cell_select", count, data);
        }

        public static AudioClip BuildCorrectPlaceSfx()
        {
            const float seconds = 0.15f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 14f);
                var tone = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.18f;
                var harmonic = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.08f;
                data[i] = Mathf.Clamp((tone + harmonic) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_number_correct", count, data);
        }

        public static AudioClip BuildWrongPlaceSfx()
        {
            const float seconds = 0.20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 12f);
                var buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 170f * t)) * 0.22f;
                var drop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(280f, 130f, t / seconds) * t) * 0.20f;
                data[i] = Mathf.Clamp((buzz + drop) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_number_wrong", count, data);
        }

        public static AudioClip BuildPencilMarkSfx()
        {
            const float seconds = 0.12f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 20f);
                var brush = (Mathf.PerlinNoise1D(t * 800f) - 0.5f) * 0.14f;
                var tone = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.10f;
                data[i] = Mathf.Clamp((brush + tone) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_pencil_mark", count, data);
        }

        public static AudioClip BuildPencilEraseSfx()
        {
            const float seconds = 0.10f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 22f);
                var scrape = (Mathf.PerlinNoise1D(t * 1200f) - 0.5f) * 0.16f;
                data[i] = Mathf.Clamp(scrape * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_pencil_erase", count, data);
        }

        public static AudioClip BuildFogRevealSfx()
        {
            const float seconds = 0.30f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 5f);
                var whisper = (Mathf.PerlinNoise1D(t * 400f) - 0.5f) * 0.18f;
                var sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 600f, t / seconds) * t) * 0.08f;
                data[i] = Mathf.Clamp((whisper + sweep) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_fog_reveal", count, data);
        }

        public static AudioClip BuildModifierActivateSfx()
        {
            const float seconds = 0.50f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 3f);
                var hum = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.16f;
                var overtone = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.08f;
                data[i] = Mathf.Clamp((hum + overtone) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_modifier_activate", count, data);
        }

        public static AudioClip BuildCombo5Sfx()
        {
            const float seconds = 0.40f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 659.25f, 783.99f, 1046.50f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var noteIdx = Mathf.Min((int)(t / (seconds / 3f)), 2);
                var noteT = t - noteIdx * (seconds / 3f);
                var env = Mathf.Exp(-noteT * 8f);
                data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) * 0.18f * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_combo_5", count, data);
        }

        public static AudioClip BuildCombo10Sfx()
        {
            const float seconds = 0.60f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var noteIdx = Mathf.Min((int)(t / (seconds / 5f)), 4);
                var noteT = t - noteIdx * (seconds / 5f);
                var env = Mathf.Exp(-noteT * 7f);
                data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) * 0.16f * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_combo_10", count, data);
        }

        public static AudioClip BuildComboBreakSfx()
        {
            const float seconds = 0.20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 12f);
                var drip = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1200f, 400f, t / seconds) * t) * 0.14f;
                data[i] = Mathf.Clamp(drip * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_combo_break", count, data);
        }

        public static AudioClip BuildHpLossSfx()
        {
            const float seconds = 1.0f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 2f);
                var bell = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.18f;
                var overtone = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.06f;
                var deep = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.10f;
                data[i] = Mathf.Clamp((bell + overtone + deep) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_hp_loss", count, data);
        }

        public static AudioClip BuildHpCriticalSfx()
        {
            const float seconds = 0.50f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var strike1 = t < 0.2f ? Mathf.Exp(-t * 10f) : 0f;
                var strike2 = t >= 0.25f ? Mathf.Exp(-(t - 0.25f) * 10f) : 0f;
                var env = strike1 + strike2;
                var tone = Mathf.Sin(2f * Mathf.PI * 370f * t) * 0.20f;
                var dissonant = Mathf.Sin(2f * Mathf.PI * 415f * t) * 0.10f;
                data[i] = Mathf.Clamp((tone + dissonant) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_hp_critical", count, data);
        }

        public static AudioClip BuildPencilDepletedSfx()
        {
            const float seconds = 0.15f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 18f);
                var scratch = (Mathf.PerlinNoise1D(t * 1500f) - 0.5f) * 0.20f;
                data[i] = Mathf.Clamp(scratch * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_pencil_depleted", count, data);
        }

        public static AudioClip BuildGoldGainSfx()
        {
            const float seconds = 0.12f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 16f);
                var clink = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.12f;
                var body = Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.10f;
                data[i] = Mathf.Clamp((clink + body) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_gold_gain", count, data);
        }

        public static AudioClip BuildButtonHoverSfx()
        {
            const float seconds = 0.06f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 35f);
                var creak = Mathf.Sin(2f * Mathf.PI * 350f * t) * 0.06f;
                data[i] = Mathf.Clamp(creak * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_button_hover", count, data);
        }

        public static AudioClip BuildButtonClickSfx()
        {
            const float seconds = 0.08f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 28f);
                var tap = Mathf.Sin(2f * Mathf.PI * 500f * t) * 0.14f;
                data[i] = Mathf.Clamp(tap * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_button_click", count, data);
        }

        public static AudioClip BuildErrorBlockedSfx()
        {
            const float seconds = 0.10f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 20f);
                var buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 120f * t)) * 0.10f;
                data[i] = Mathf.Clamp(buzz * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_error_blocked", count, data);
        }

        public static AudioClip BuildItemUseSfx()
        {
            const float seconds = 0.25f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 9f);
                var openClose = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.20f;
                var click = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.06f * Mathf.Exp(-t * 18f);
                data[i] = Mathf.Clamp((openClose + click) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_item_use", count, data);
        }

        public static AudioClip BuildItemRewardSfx()
        {
            const float seconds = 0.30f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 5f);
                var chime = Mathf.Sin(2f * Mathf.PI * 1568f * t) * 0.14f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * 2349f * t) * 0.06f * Mathf.Exp(-t * 10f);
                data[i] = Mathf.Clamp((chime + shimmer) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_item_reward", count, data);
        }

        public static AudioClip BuildRelicRevealSfx()
        {
            const float seconds = 0.40f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 4f);
                var crystal = Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.12f;
                var harmonic = Mathf.Sin(2f * Mathf.PI * 3136f * t) * 0.05f;
                var body = Mathf.Sin(2f * Mathf.PI * 1047f * t) * 0.08f;
                data[i] = Mathf.Clamp((crystal + harmonic + body) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_relic_reveal", count, data);
        }

        public static AudioClip BuildXpBarFillSfx()
        {
            const float seconds = 1.50f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var progress = t / seconds;
                var freq = Mathf.Lerp(220f, 880f, progress);
                var env = 0.14f * (0.6f + 0.4f * progress);
                var tone = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
                var shimmer = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.04f * progress;
                data[i] = Mathf.Clamp(tone + shimmer, -0.8f, 0.8f);
            }
            return MakeClip("sfx_xp_bar_fill", count, data);
        }

        public static AudioClip BuildLevelUpSfx()
        {
            const float seconds = 0.80f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 1046.50f, 783.99f, 659.25f, 523.25f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var noteIdx = Mathf.Min((int)(t / (seconds / 4f)), 3);
                var noteT = t - noteIdx * (seconds / 4f);
                var env = Mathf.Exp(-noteT * 6f);
                var tone = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) * 0.16f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * 3f * t) * 0.04f * Mathf.Exp(-noteT * 12f);
                data[i] = Mathf.Clamp((tone + shimmer) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_level_up", count, data);
        }

        public static AudioClip BuildAchievementSfx()
        {
            const float seconds = 0.50f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 3.5f);
                var ring = Mathf.Sin(2f * Mathf.PI * 1568f * t) * 0.14f;
                var body = Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.10f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * 2350f * t) * 0.04f * Mathf.Exp(-t * 8f);
                data[i] = Mathf.Clamp((ring + body + shimmer) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_achievement", count, data);
        }

        public static AudioClip BuildBossGateOpenSfx()
        {
            const float seconds = 0.60f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 2.8f);
                var taiko = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.22f * Mathf.Exp(-t * 8f);
                var creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 320f, t / seconds) * t) * 0.12f;
                data[i] = Mathf.Clamp((taiko + creak) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_boss_gate_open", count, data);
        }

        public static AudioClip BuildFloorTransitionSfx()
        {
            const float seconds = 1.20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var progress = t / seconds;
                var env = Mathf.Sin(progress * Mathf.PI);
                var wind = (Mathf.PerlinNoise1D(t * 60f) - 0.5f) * 0.16f;
                var chime = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(440f, 1320f, progress) * t) * 0.08f * Mathf.Exp(-Mathf.Abs(progress - 0.5f) * 4f);
                data[i] = Mathf.Clamp((wind + chime) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_floor_transition", count, data);
        }

        public static AudioClip BuildMenuOpenSfx()
        {
            const float seconds = 0.20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 10f);
                var unfold = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 600f, t / seconds) * t) * 0.10f;
                data[i] = Mathf.Clamp(unfold * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_menu_open", count, data);
        }

        public static AudioClip BuildMenuCloseSfx()
        {
            const float seconds = 0.15f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 14f);
                var fold = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(600f, 300f, t / seconds) * t) * 0.10f;
                data[i] = Mathf.Clamp(fold * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_menu_close", count, data);
        }

        public static AudioClip BuildShopPurchaseSfx()
        {
            const float seconds = 0.25f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 7.5f);
                var pingA = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.22f;
                var pingB = Mathf.Sin(2f * Mathf.PI * 990f * t) * 0.16f;
                data[i] = Mathf.Clamp((pingA + pingB) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_shop_purchase", count, data);
        }

        public static AudioClip BuildShopRerollSfx()
        {
            const float seconds = 0.30f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 6f);
                var sweepFreq = Mathf.Lerp(340f, 860f, Mathf.Clamp01(t / seconds));
                data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * sweepFreq * t) * 0.24f * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_reroll", count, data);
        }

        public static AudioClip BuildVictoryStingerSfx()
        {
            const float seconds = 2.0f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var noteIdx = Mathf.Min((int)(t / 0.4f), 3);
                var noteT = t - noteIdx * 0.4f;
                var env = Mathf.Exp(-noteT * 2f);
                var tone = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) * 0.18f;
                var fifth = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * 1.5f * t) * 0.08f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * 4f * t) * 0.03f * Mathf.Exp(-noteT * 6f);
                data[i] = Mathf.Clamp((tone + fifth + shimmer) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_victory", count, data);
        }

        public static AudioClip BuildGameOverStingerSfx()
        {
            const float seconds = 0.80f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 2.5f);
                var descend = 440f - 300f * t;
                var tone = Mathf.Sin(2f * Mathf.PI * descend * t) * 0.20f;
                var rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.10f;
                data[i] = Mathf.Clamp((tone + rumble) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_game_over", count, data);
        }

        public static AudioClip BuildLevelCompleteSfx()
        {
            const float seconds = 0.85f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var tones = new[] { 523.25f, 659.25f, 783.99f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 2.2f);
                var signal = 0f;
                for (var n = 0; n < tones.Length; n++)
                    signal += Mathf.Sin(2f * Mathf.PI * tones[n] * t) * (0.19f - n * 0.03f);
                data[i] = Mathf.Clamp(signal * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_level_complete", count, data);
        }

        public static AudioClip BuildPuzzleLoop()
        {
            const float seconds = 32f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 329.63f, 293.66f, 261.63f, 246.94f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var step = (int)(t * 1.8f) % notes.Length;
                var pulse = 0.55f + 0.45f * Mathf.Sin(t * 0.35f * Mathf.PI);
                var lead = Mathf.Sin(2f * Mathf.PI * notes[step] * t) * 0.21f;
                var harmony = Mathf.Sin(2f * Mathf.PI * (notes[(step + 3) % notes.Length] * 0.5f) * t) * 0.14f;
                var bell = Mathf.Sin(2f * Mathf.PI * notes[(step + 5) % notes.Length] * t) * 0.06f;
                data[i] = Mathf.Clamp((lead + harmony + bell) * pulse, -0.65f, 0.65f);
            }
            return MakeClip("RunPuzzleLoop", count, data);
        }

        public static AudioClip BuildMenuLoop()
        {
            const float seconds = 24f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            // Pentatonic ambient — calm garden feel
            var notes = new[] { 196f, 220f, 261.63f, 293.66f, 349.23f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var slot = (int)(t * 0.7f) % notes.Length;
                var local = t % 1.43f;
                var decay = Mathf.Exp(-local * 3f);
                var tone = Mathf.Sin(2f * Mathf.PI * notes[slot] * t) * decay * 0.16f;
                var pad = Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.06f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * notes[(slot + 2) % notes.Length] * 2f * t) * 0.03f * decay;
                data[i] = Mathf.Clamp(tone + pad + shimmer, -0.50f, 0.50f);
            }
            return MakeClip("MenuLoop", count, data);
        }

        public static AudioClip BuildShopLoop()
        {
            const float seconds = 20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 392f, 440f, 523.25f, 659.25f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var slot = Mathf.FloorToInt(t * 1.2f) % notes.Length;
                var local = t % 0.83f;
                var decay = Mathf.Exp(-local * 6f);
                var pluck = Mathf.Sin(2f * Mathf.PI * notes[slot] * t) * decay * 0.25f;
                var low = Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.07f;
                data[i] = Mathf.Clamp(pluck + low, -0.55f, 0.55f);
            }
            return MakeClip("RunShopLoop", count, data);
        }

        public static AudioClip BuildRestLoop()
        {
            const float seconds = 22f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var wind = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.08f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.03f;
                var chirpA = Chirp(t, 1.4f, 1320f, 1560f, 0.11f);
                var chirpB = Chirp(t + 0.23f, 1.9f, 990f, 1260f, 0.09f);
                var chirpC = Chirp(t + 0.57f, 2.7f, 1180f, 1420f, 0.07f);
                data[i] = Mathf.Clamp(wind + chirpA + chirpB + chirpC, -0.50f, 0.50f);
            }
            return MakeClip("RunRestLoop", count, data);
        }

        public static AudioClip BuildPencilToggleSfx()
        {
            const float seconds = 0.10f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 20f);
                var click = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 90f * t)) * 0.08f;
                var tone = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.12f;
                data[i] = Mathf.Clamp((click + tone) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_pencil_toggle", count, data);
        }

        public static AudioClip BuildRestHealSfx()
        {
            const float seconds = 0.50f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 4f);
                var sweep = 330f + 220f * t;
                var tone = Mathf.Sin(2f * Mathf.PI * sweep * t) * 0.15f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.04f * Mathf.Exp(-t * 8f);
                data[i] = Mathf.Clamp((tone + shimmer) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_rest_heal", count, data);
        }

        public static AudioClip BuildRewardClaimSfx()
        {
            const float seconds = 0.32f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            var notes = new[] { 523.25f, 659.25f, 783.99f };
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 4.4f);
                var signal = 0f;
                for (var n = 0; n < notes.Length; n++)
                    signal += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * (0.16f - n * 0.025f);
                data[i] = Mathf.Clamp(signal * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_reward_claim", count, data);
        }

        public static AudioClip BuildPathAdvanceSfx()
        {
            const float seconds = 0.20f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 10f);
                var pop = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.20f;
                var click = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 48f * t)) * 0.06f;
                data[i] = Mathf.Clamp((pop + click) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_path_advance", count, data);
        }

        public static AudioClip BuildRelicPickupSfx()
        {
            const float seconds = 0.35f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var env = Mathf.Exp(-t * 6f);
                var chime = Mathf.Sin(2f * Mathf.PI * 1047f * t) * 0.14f;
                var harmonic = Mathf.Sin(2f * Mathf.PI * 1568f * t) * 0.07f;
                var sparkle = Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.04f * Mathf.Exp(-t * 12f);
                data[i] = Mathf.Clamp((chime + harmonic + sparkle) * env, -0.8f, 0.8f);
            }
            return MakeClip("sfx_relic_pickup", count, data);
        }

        public static float Chirp(float t, float rate, float f0, float f1, float amp)
        {
            var phase = t * rate;
            var frac = phase - Mathf.Floor(phase);
            if (frac > 0.18f) return 0f;
            var k = frac / 0.18f;
            var freq = Mathf.Lerp(f0, f1, k);
            var env = Mathf.Sin(k * Mathf.PI);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env * amp;
        }

        private static AudioClip MakeClip(string name, int sampleCount, float[] data)
        {
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
