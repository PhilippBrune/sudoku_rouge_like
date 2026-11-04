using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public static class FontAssetService
    {
        public const string ProductionFontResourcePath = "Fonts/MainFont";
        public const string DyslexiaFontResourcePath = "Fonts/OpenDyslexic";   // [REQ: ACCESS-FONT-004]
        public const string PrimaryFallbackFontName = "LegacyRuntime.ttf";
        public const string SecondaryFallbackFontName = "Arial.ttf";

        private static Font _font;
        private static Font _productionFont;
        private static bool _productionFontChecked;

        /// <summary>[REQ: ACCESS-FONT-004] True while every text is in OpenDyslexic (Accessibility, RM-106).</summary>
        public static bool DyslexiaFont { get; private set; }

        /// <summary>Switches the game font; every text on screen changes at once, later ones are built with it.</summary>
        public static void SetDyslexiaFont(bool on)
        {
            if (on == DyslexiaFont) return;
            var before = GetFont();
            DyslexiaFont = on;
            _font = null;
            var after = GetFont();
            if (after == before) return;
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.font == before) text.font = after;
        }

        public static Font GetFont()
        {
            if (_font != null)
                return _font;

            _font = (DyslexiaFont ? Resources.Load<Font>(DyslexiaFontResourcePath) : null)
                ?? LoadProductionFont()
                ?? Resources.GetBuiltinResource<Font>(PrimaryFallbackFontName)
                ?? Resources.GetBuiltinResource<Font>(SecondaryFallbackFontName);

            return _font;
        }

        public static bool TryLoadProductionFont(out Font font)
        {
            font = LoadProductionFont();
            return font != null;
        }

        public static void ResetCacheForTests()
        {
            _font = null;
            _productionFont = null;
            _productionFontChecked = false;
        }

        private static Font LoadProductionFont()
        {
            if (_productionFontChecked)
                return _productionFont;

            _productionFont = Resources.Load<Font>(ProductionFontResourcePath);
            _productionFontChecked = true;
            return _productionFont;
        }
    }
}
