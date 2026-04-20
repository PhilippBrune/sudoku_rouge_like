using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Bootstrap
{
    /// <summary>
    /// Simple logo fade-in/out shown once before the main menu appears.
    /// Attach to the menu group; GameBootstrap calls Show() during Start.
    /// </summary>
    public sealed class SplashScreenController : MonoBehaviour
    {
        private GameObject _splashPanel;
        private CanvasGroup _splashCg;
        private bool _done;

        public bool IsDone => _done;

        // B, D — floorTint optionally blends the background colour with the last-played floor's palette
        public void Initialize(Transform parent, Color floorTint = default)
        {
            _splashPanel = new GameObject("SplashPanel", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            _splashPanel.transform.SetParent(parent, false);

            var canvas = _splashPanel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // above everything

            var rt = _splashPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _splashCg = _splashPanel.AddComponent<CanvasGroup>();
            _splashCg.alpha = 1f;
            _splashCg.blocksRaycasts = true;

            // Background fill — blend base dark with floor tint when available (D)
            var baseBg = new Color(0.06f, 0.05f, 0.10f, 1f);
            var bgColor = floorTint.a > 0.01f
                ? Color.Lerp(baseBg, new Color(floorTint.r * 0.25f, floorTint.g * 0.25f, floorTint.b * 0.25f, 1f), 0.35f)
                : baseBg;
            var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_splashPanel.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = bgColor;

            // Faint garden pixel texture — atmospheric depth (C)
            var gardenGo = new GameObject("GardenBG", typeof(RectTransform), typeof(RawImage));
            gardenGo.transform.SetParent(_splashPanel.transform, false);
            var gardenRt = gardenGo.GetComponent<RectTransform>();
            gardenRt.anchorMin = Vector2.zero; gardenRt.anchorMax = Vector2.one;
            gardenRt.offsetMin = Vector2.zero; gardenRt.offsetMax = Vector2.zero;
            var rawImg = gardenGo.GetComponent<RawImage>();
            rawImg.texture = GardenTextureFactory.GetOrCreate();
            rawImg.uvRect  = new Rect(0f, 0f, 20f, 12f);
            rawImg.color   = new Color(1f, 1f, 1f, 0.10f);
            rawImg.raycastTarget = false;

            // Decorative gold rule above title — garden gate imagery (1)
            AddSplashRule(new Vector2(0.30f, 0.625f), new Vector2(0.70f, 0.629f));

            // Logo text — shifted up slightly to make room for subtitle
            var logoGo = new GameObject("LogoText", typeof(RectTransform), typeof(Text));
            logoGo.transform.SetParent(_splashPanel.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.1f, 0.42f);
            logoRt.anchorMax = new Vector2(0.9f, 0.62f);
            logoRt.offsetMin = Vector2.zero;
            logoRt.offsetMax = Vector2.zero;
            var logoText = logoGo.GetComponent<Text>();
            logoText.text = "Run of the Nine";
            logoText.fontSize = 48;
            logoText.fontStyle = FontStyle.Bold;
            logoText.alignment = TextAnchor.MiddleCenter;
            logoText.color = new Color(0.98f, 0.83f, 0.26f, 1f); // AccentGold
            logoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Subtitle (B)
            var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(_splashPanel.transform, false);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.25f, 0.37f);
            subRt.anchorMax = new Vector2(0.75f, 0.43f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            var subText = subGo.GetComponent<Text>();
            subText.text = "Sudoku Roguelike";
            subText.fontSize = 20;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.98f, 0.83f, 0.26f, 0.60f);
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Decorative gold rule below subtitle — garden gate imagery (1)
            AddSplashRule(new Vector2(0.30f, 0.362f), new Vector2(0.70f, 0.366f));

            _done = false;
        }

        private void AddSplashRule(Vector2 anchorMin, Vector2 anchorMax)
        {
            var ruleGo = new GameObject("SplashRule", typeof(RectTransform), typeof(Image));
            ruleGo.transform.SetParent(_splashPanel.transform, false);
            var ruleRt = ruleGo.GetComponent<RectTransform>();
            ruleRt.anchorMin = anchorMin;
            ruleRt.anchorMax = anchorMax;
            ruleRt.offsetMin = ruleRt.offsetMax = Vector2.zero;
            ruleGo.GetComponent<Image>().color = new Color(0.98f, 0.83f, 0.26f, 0.45f);
        }

        public void Show(System.Action onComplete)
        {
            if (_splashPanel == null) { onComplete?.Invoke(); return; }
            StartCoroutine(RunSplash(onComplete));
        }

        private IEnumerator RunSplash(System.Action onComplete)
        {
            // Fade in
            yield return StartCoroutine(AnimationHelper.FadeIn(_splashCg, 0.6f));
            yield return new WaitForSecondsRealtime(0.9f);
            // Gate opens: scale forward slightly while fading to black (2)
            StartCoroutine(ScaleToward(_splashPanel.transform, 1.025f, 0.6f));
            yield return StartCoroutine(AnimationHelper.FadeOut(_splashCg, 0.6f));

            _splashPanel.SetActive(false);
            _done = true;
            onComplete?.Invoke();
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
                var s = Mathf.Lerp(fromScale, toScale, elapsed / duration);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (target != null) target.localScale = new Vector3(toScale, toScale, 1f);
        }
        // Splash garden texture previously duplicated here; now delegated to GardenTextureFactory.
    }
}
