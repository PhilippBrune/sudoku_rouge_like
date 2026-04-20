using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public enum EaseType
    {
        Linear,
        EaseOut,
        EaseInOut
    }

    public static class AnimationHelper
    {
        public const float CellHighlightDuration = 0.1f;
        public const float NumberPlaceDuration = 0.15f;
        public const float WrongFlashDuration = 0.2f;
        public const float HpBarDuration = 0.3f;
        public const float XpBarPerLevelDuration = 1.5f;
        public const float ComboPulseDuration = 0.1f;
        public const float MenuPanelDuration = 0.2f;
        public const float RewardSlotDuration = 0.16f;
        public const float FloorTransitionFadeOut = 0.8f;
        public const float FloorTransitionSilence = 0.2f;
        public const float FloorTransitionFadeIn = 0.8f;

        public const float ShakeWrongPx = 2f;
        public const float ShakeWrongDuration = 0.1f;
        public const float ShakeBossPx = 3f;
        public const float ShakeBossDuration = 0.2f;
        public const float ShakeHpCriticalPx = 2f;
        public const float ShakeHpCriticalDuration = 0.15f;

        public static float GetDuration(float baseDuration, bool reduceMotion)
        {
            return reduceMotion ? 0f : baseDuration;
        }

        public static float Ease(float t, EaseType type)
        {
            t = Mathf.Clamp01(t);
            return type switch
            {
                EaseType.EaseOut => 1f - Mathf.Pow(1f - t, 3f),
                EaseType.EaseInOut => t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f,
                _ => t
            };
        }

        public static IEnumerator ScreenShake(Transform target, float intensityPx, float duration)
        {
            if (target == null || duration <= 0f) yield break;

            var original = target.localPosition;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var decay = 1f - Mathf.Clamp01(elapsed / duration);
                var offsetX = Random.Range(-intensityPx, intensityPx) * decay;
                var offsetY = Random.Range(-intensityPx, intensityPx) * decay;
                target.localPosition = original + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }
            target.localPosition = original;
        }

        public static IEnumerator AnimateFloat(float from, float to, float duration, EaseType ease,
            System.Action<float> onUpdate)
        {
            if (duration <= 0f)
            {
                onUpdate?.Invoke(to);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Ease(Mathf.Clamp01(elapsed / duration), ease);
                onUpdate?.Invoke(Mathf.Lerp(from, to, t));
                yield return null;
            }
            onUpdate?.Invoke(to);
        }

        /// <summary>Fades a CanvasGroup from alpha 0 to 1 over <paramref name="duration"/> seconds.</summary>
        public static IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            cg.alpha = 0f;
            if (duration <= 0f) { cg.alpha = 1f; yield break; }
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Ease(Mathf.Clamp01(elapsed / duration), EaseType.EaseOut);
                yield return null;
            }
            cg.alpha = 1f;
        }

        /// <summary>Fades a CanvasGroup from alpha 1 to 0 over <paramref name="duration"/> seconds.</summary>
        public static IEnumerator FadeOut(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            cg.alpha = 1f;
            if (duration <= 0f) { cg.alpha = 0f; yield break; }
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = 1f - Ease(Mathf.Clamp01(elapsed / duration), EaseType.EaseOut);
                yield return null;
            }
            cg.alpha = 0f;
        }

        /// <summary>Briefly flashes a Graphic to <paramref name="flashColor"/> and back.</summary>
        public static IEnumerator FlashColor(Graphic graphic, Color flashColor, float duration)
        {
            if (graphic == null || duration <= 0f) yield break;
            var original = graphic.color;
            graphic.color = flashColor;
            yield return new WaitForSecondsRealtime(duration * 0.5f);
            graphic.color = original;
        }

        public static IEnumerator PulseScale(Transform target, float baseScale, float peakScale,
            float duration)
        {
            if (target == null || duration <= 0f) yield break;

            var half = duration * 0.5f;
            var elapsed = 0f;
            while (elapsed < half)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / half);
                var s = Mathf.Lerp(baseScale, peakScale, t);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / half);
                var s = Mathf.Lerp(peakScale, baseScale, t);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (target != null) target.localScale = new Vector3(baseScale, baseScale, 1f);
        }
    }
}
