using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Bootstrap
{
    public sealed class SplashScreenController : MonoBehaviour
    {
        private GameObject _panel;
        private CanvasGroup _cg;
        private bool _done;

        public bool IsDone => _done;

        /// <summary>[REQ: UI-MENU-TITLE-001] True while the title screen covers the game (GAP-097).</summary>
        public static bool Showing { get; private set; }

        public void Initialize(Transform _unused = null, Color _unusedColor = default)
        {
            Showing = true;
            // Root-level object — not parented to anything so no hierarchy can interfere.
            _panel = new GameObject("_SplashOverlay");
            DontDestroyOnLoad(_panel);

            var canvas = _panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // renders above every other canvas

            var scaler = _panel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _panel.AddComponent<GraphicRaycaster>();

            _cg = _panel.AddComponent<CanvasGroup>();
            _cg.alpha = 1f; // visible immediately — no fade-in gap where game UI bleeds through
            _cg.blocksRaycasts = true;

            // Full-screen background image
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(_panel.transform, false);
            var rt = bgGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tex = Resources.Load<Texture2D>("background/bg_splash");
            if (tex != null)
            {
                var raw = bgGo.AddComponent<RawImage>();
                raw.texture = tex;
                raw.uvRect  = new Rect(0f, 0f, 1f, 1f);
                raw.color   = Color.white;
            }
            else
            {
                bgGo.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.10f, 1f);
            }

            _done = false;
        }

        public void Show(Action onComplete)
        {
            if (_panel == null) { onComplete?.Invoke(); return; }
            StartCoroutine(RunSplash(onComplete));
        }

        private IEnumerator RunSplash(Action onComplete)
        {
            PlayTitleSting();
            // Already fully visible — just hold, then fade out.
            yield return new WaitForSecondsRealtime(2.9f);
            StartCoroutine(ScaleToward(_panel.transform, 1.025f, 0.6f));
            yield return StartCoroutine(AnimationHelper.FadeOut(_cg, 0.6f));
            _panel.SetActive(false);
            _done = true;
            Showing = false;
            onComplete?.Invoke();
        }

        // [REQ: AUDIO-TITLE-001] The title screen's sound (RM-215), at Master × SFX of the last profile, silent with Mute all.
        // The splash runs before any options are loaded, so it reads them itself.
        private void PlayTitleSting()
        {
            if (!AudioAssetService.TryLoadAuthoredClip(AudioClipId.SfxTitleSting, out var clip)) return;
            var audio = new Save.ProfileService(new Save.SaveFileService(Save.SaveProfileService.ActiveSlot)).LoadOptions().Audio
                        ?? new Core.AudioSettingsModel();
            if (audio.MuteAll) return;
            var source = _panel.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.PlayOneShot(clip, Mathf.Clamp01(audio.MasterVolume) * Mathf.Clamp01(audio.SfxVolume));
        }

        private static IEnumerator ScaleToward(Transform target, float toScale, float duration)
        {
            if (target == null || duration <= 0f) yield break;
            var fromScale = target.localScale.x;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, elapsed / duration);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one * toScale;
        }

        private void OnDestroy() => Showing = false;
    }
}
