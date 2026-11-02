using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-041] XP B · Brush stroke (owner's pick, 0.3.1 playtest: "XP should feel like something is added"):
    /// on the run-end scroll a brush paints the gold into the XP bar from where the class stood before the run, the count
    /// rises with it, and at each level a red seal with the new level is pressed on the bar's end before the brush starts
    /// again from the left. 2.4 s; any key, click or pad button shows the end at once; Reduce Motion shows the end only.
    /// </summary>
    public sealed class XpBrushAnimator : MonoBehaviour
    {
        public const float Seconds = 2.4f;
        private const float SkipAfter = 0.2f, SealPop = 0.2f;

        private RectTransform _track, _brush;
        private Image _fill, _seal;
        private Text _gain, _level, _toNext, _sealText;
        private GameObject _stamp;
        private string _className, _toNextText;
        private int _xp, _preLevel, _postLevel;
        private float _from, _to, _startedAt, _sealAt = -1f;
        private int _sealLevel;
        private readonly Image[] _flecks = new Image[3];
        private Texture2D _brushTexture;
        private Sprite _brushSprite;

        /// <summary>Starts the stroke: from <paramref name="preLevel"/> plus <paramref name="preFrac"/> of it to
        /// <paramref name="postLevel"/> plus <paramref name="postFrac"/> (1 for a class at its highest level).</summary>
        public void Play(RectTransform track, Image fill, Text gain, Text level, Text toNext, GameObject stamp, string className,
            int xp, int preLevel, float preFrac, int postLevel, float postFrac)
        {
            (_track, _fill, _gain, _level, _toNext, _stamp, _className) = (track, fill, gain, level, toNext, stamp, className);
            (_xp, _preLevel, _postLevel) = (xp, preLevel, postLevel);
            _from = preLevel + Mathf.Clamp01(preFrac);
            _to = Mathf.Max(_from, postLevel + Mathf.Clamp01(postFrac));
            _toNextText = toNext != null ? toNext.text : null;
            _startedAt = Time.unscaledTime;
            BuildBrushAndSeal();
            if (_stamp != null) _stamp.SetActive(false);
            if (_toNext != null) _toNext.text = string.Empty;
            Apply(0f);
            if (AnimationHelper.MotionReduced) Finish();
        }

        private void BuildBrushAndSeal()
        {
            var tex = _brushTexture = BrushCursor.Draw();
            var sprite = _brushSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 100f);
            _brush = (RectTransform)new GameObject("XpBrush", typeof(RectTransform), typeof(Image)).transform;
            _brush.SetParent(_track, false);
            _brush.pivot = new Vector2(0.05f, 0.95f);   // the ink tip touches the paint
            _brush.sizeDelta = new Vector2(70f, 70f);
            _brush.localRotation = Quaternion.Euler(0f, 0f, 90f);   // the handle up and to the right
            var brushImage = _brush.GetComponent<Image>();
            brushImage.sprite = sprite;
            brushImage.raycastTarget = false;
            for (var i = 0; i < _flecks.Length; i++)
            {
                _flecks[i] = ScrollUiKit.Spot(_track, "GoldLeaf_" + i, Vector2.zero, new Vector2(7f, 7f), new Color(0.95f, 0.82f, 0.5f, 0f));
                _flecks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 30f + i * 20f);
            }
            _seal = ScrollUiKit.Spot(_track, "LevelSeal", new Vector2(1f, 0.5f), new Vector2(78f, 78f), ScrollUiKit.Vermilion, ScrollUiKit.Rounded(8f));
            _seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            _sealText = ScrollUiKit.Label(_seal.transform, "Text", string.Empty, 22, TextAnchor.MiddleCenter, new Color(0.98f, 0.93f, 0.88f, 1f),
                Vector2.zero, Vector2.one, 12);
            _seal.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_track == null) return;
            var skip = Time.unscaledTime - _startedAt > SkipAfter && (Input.anyKeyDown || PadButtons.AnyPressed());
            var t = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Seconds);
            if (skip || t >= 1f) { Finish(); return; }
            Apply(1f - (1f - t) * (1f - t));   // eases out: fast first, the last of the gold slowly
            if (_sealAt >= 0f)
            {
                var pop = Mathf.Clamp01((Time.unscaledTime - _sealAt) / SealPop);
                _seal.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, pop);
            }
        }

        // Shows the stroke at progress p (0 to 1): the level reached so far, its share of the bar, the count, the seal.
        private void Apply(float p)
        {
            var at = Mathf.Lerp(_from, _to, p);
            var level = Mathf.Min(Mathf.FloorToInt(at), _postLevel);
            var frac = Mathf.Clamp01(at - level);
            _fill.enabled = frac > 0.005f;
            _fill.rectTransform.anchorMax = new Vector2(frac, 1f);
            _brush.anchorMin = _brush.anchorMax = new Vector2(frac, 0.5f);
            _brush.gameObject.SetActive(p < 1f);
            if (_gain != null) _gain.text = LocalizationService.Format("InRun.End.Scroll.XpGain", "+{0} XP", Mathf.RoundToInt(_xp * p));
            if (_level != null) _level.text = LocalizationService.Format("InRun.End.Scroll.ClassLevel", "{0} · Lv {1}", _className, level);
            for (var i = 0; i < _flecks.Length; i++)
            {
                var c = _flecks[i].color;
                c.a = p < 1f && frac > 0.05f ? 0.9f * (1f - ((Time.unscaledTime * 3f + i * 0.33f) % 1f)) : 0f;
                _flecks[i].color = c;
                var rt = _flecks[i].rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Max(0f, frac - 0.03f * (i + 1)), i % 2 == 0 ? 1.25f : -0.2f);
            }
            if (level > _preLevel && level != _sealLevel)
            {
                _sealLevel = level;
                _seal.gameObject.SetActive(true);
                _sealText.text = LocalizationService.Format("InRun.End.Scroll.SealLevel", "Lv {0}", level);
                _sealAt = Time.unscaledTime;
            }
        }

        private void OnDestroy()
        {
            if (_brushSprite != null) Destroy(_brushSprite);
            if (_brushTexture != null) Destroy(_brushTexture);
        }

        private void Finish()
        {
            Apply(1f);
            if (_seal != null) _seal.rectTransform.localScale = Vector3.one;
            if (_stamp != null) _stamp.SetActive(true);
            if (_stamp != null && _seal != null) _seal.gameObject.SetActive(false);   // the gold stamp names the levels now
            if (_toNext != null) _toNext.text = _toNextText;
            _track = null;
            enabled = false;
        }
    }
}
