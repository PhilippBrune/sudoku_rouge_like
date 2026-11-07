using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: ACCESS-FONT-003] Text Size on every screen (RM-106, owner 2026-11-04: the same setting). A text grows to its
    /// built size × Text Size and back down where its box is too small, never below the size it was built with; below
    /// 100 % it shrinks. A size another script sets later becomes the new built size. The board sizes its own digits and
    /// clues (ACCESS-FONT-001/002), so <see cref="InRunUiFactory.CreateText"/> leaves those out. In OpenDyslexic a text may
    /// shrink to 75 % of its built size where its box is too small (ACCESS-FONT-004: the font is about 45 % wider).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TextSizeFit : MonoBehaviour
    {
        private static readonly HashSet<TextSizeFit> Dirty = new HashSet<TextSizeFit>();
        private static readonly List<TextSizeFit> Fitting = new List<TextSizeFit>();
        private static bool _hooked;
        private Text _text;
        private int _base = -1, _set = -1;
        private bool _busy;
        private const float DyslexiaFloor = 0.75f;

        /// <summary>The Text Size every text follows (1 = as built).</summary>
        public static float Scale { get; private set; } = 1f;

        public static void SetScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.8f, 1.5f);
            if (Mathf.Approximately(scale, Scale)) return;
            Scale = scale;
            foreach (var fit in FindObjectsByType<TextSizeFit>(FindObjectsInactive.Include, FindObjectsSortMode.None)) fit.MarkDirty();
        }

        internal static void Follow(Text text)
        {
            if (text != null && text.GetComponent<TextSizeFit>() == null) text.gameObject.AddComponent<TextSizeFit>();
        }

        private void Awake() => _text = GetComponent<Text>();

        private void OnEnable()
        {
            if (_text == null) return;
            _text.RegisterDirtyLayoutCallback(MarkDirty);   // new text, a new size or a resized box
            MarkDirty();
        }

        private void OnDisable()
        {
            _text?.UnregisterDirtyLayoutCallback(MarkDirty);
            Dirty.Remove(this);
        }

        private void MarkDirty()
        {
            if (_busy) return;
            if (!_hooked) { Canvas.willRenderCanvases += FitDirty; _hooked = true; }
            Dirty.Add(this);
        }

        // Once before the canvases draw: every text that changed since the last frame.
        private static void FitDirty()
        {
            if (Dirty.Count == 0) return;
            Fitting.Clear();
            Fitting.AddRange(Dirty);
            Dirty.Clear();
            foreach (var fit in Fitting) if (fit != null) fit.Fit();
        }

        private int Size => _text.resizeTextForBestFit ? _text.resizeTextMaxSize : _text.fontSize;

        private void Fit()
        {
            if (_text == null) return;
            if (Size != _set) _base = Size;   // built, or another script chose a new size
            var target = Mathf.Max(1, Mathf.RoundToInt(_base * Scale));
            var least = Mathf.Min(target, FontAssetService.DyslexiaFont ? Mathf.Max(1, Mathf.RoundToInt(_base * DyslexiaFloor)) : _base);
            _busy = true;
            var size = target > least ? Largest(least, target) : target;
            if (_text.resizeTextForBestFit) _text.resizeTextMaxSize = size;   // Unity may shrink it further into its box
            else _text.fontSize = size;
            _set = Size;
            _busy = false;
        }

        // The largest size from lo to hi whose text fits the box; lo always counts as fitting.
        private int Largest(int lo, int hi)
        {
            var box = ((RectTransform)transform).rect.size;
            if (box.x <= 0f || box.y <= 0f) return lo;   // not laid out yet: a later resize fits it again
            var lines = Lines(Settings(lo, box));
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (Fits(mid, box, lines)) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        private TextGenerationSettings Settings(int size, Vector2 box)
        {
            _text.fontSize = size;
            var settings = _text.GetGenerationSettings(new Vector2(box.x, 0f));
            settings.verticalOverflow = VerticalWrapMode.Overflow;   // count every line
            settings.resizeTextForBestFit = false;
            return settings;
        }

        private int Lines(TextGenerationSettings settings)
        {
            var generator = _text.cachedTextGeneratorForLayout;
            generator.Populate(_text.text, settings);
            return generator.lineCount;
        }

        // Fits its box, and a text that grows never takes another line: a box taller than its line would let a label
        // wrap into its neighbour's row.
        private bool Fits(int size, Vector2 box, int lines)
        {
            var settings = Settings(size, box);
            var generator = _text.cachedTextGeneratorForLayout;
            var ppu = _text.pixelsPerUnit;
            if (generator.GetPreferredHeight(_text.text, settings) / ppu > box.y + 0.5f) return false;
            if (_text.horizontalOverflow != HorizontalWrapMode.Wrap
                && generator.GetPreferredWidth(_text.text, settings) / ppu > box.x + 0.5f) return false;
            if (_text.horizontalOverflow == HorizontalWrapMode.Wrap && WidestWord(generator, settings) / ppu > box.x + 0.5f) return false;
            return Lines(settings) <= lines;
        }

        // [REQ: ACCESS-FONT-005] A word wider than its box is broken inside itself ("comb / o x11!" at 150 %, playtest 0.4.2);
        // such a size does not fit, the text stays a size smaller.
        private float WidestWord(TextGenerator generator, TextGenerationSettings settings)
        {
            var widest = 0f;
            foreach (var word in _text.text.Split(WordBreaks, System.StringSplitOptions.RemoveEmptyEntries))
                widest = Mathf.Max(widest, generator.GetPreferredWidth(word, settings));
            return widest;
        }

        private static readonly char[] WordBreaks = { ' ', '\n', '\t' };
    }
}
