using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class FontAssetService
    {
        public const string ProductionFontResourcePath = "Fonts/MainFont";
        public const string PrimaryFallbackFontName = "LegacyRuntime.ttf";
        public const string SecondaryFallbackFontName = "Arial.ttf";

        private static Font _font;
        private static Font _productionFont;
        private static bool _productionFontChecked;

        public static Font GetFont()
        {
            if (_font != null)
                return _font;

            _font = LoadProductionFont()
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
