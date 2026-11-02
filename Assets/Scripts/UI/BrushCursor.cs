using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-CURSOR-001] The mouse pointer as a Japanese brush (fude), 0.3.1 playtest: the ink tip is the hot spot, a
    /// vermilion ferrule and a bamboo handle with two nodes run to the lower right, and a cream rim keeps it readable on the
    /// dark scenes and on the paper. Drawn once when the first scene has loaded.
    /// </summary>
    public static class BrushCursor
    {
        public const int Size = 32;
        private const float HairEnd = 13f, FerruleEnd = 16f, Rim = 1.4f;
        private static readonly Vector2 Tip = new Vector2(1.5f, Size - 1.5f), End = new Vector2(Size - 2f, 2f);
        private static readonly Color Ink = new Color(0.07f, 0.06f, 0.06f, 1f), InkGloss = new Color(0.25f, 0.23f, 0.22f, 1f);
        private static readonly Color Lacquer = new Color(0.62f, 0.16f, 0.12f, 1f), Bamboo = new Color(0.83f, 0.70f, 0.45f, 1f);
        private static readonly Color Node = new Color(0.55f, 0.42f, 0.22f, 1f), Cream = new Color(0.96f, 0.93f, 0.85f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            if (Application.isBatchMode) return;
            Cursor.SetCursor(Draw(), new Vector2(1f, 1f), CursorMode.Auto);
        }

        public static Texture2D Draw()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var axis = (End - Tip).normalized;
            var length = Vector2.Distance(Tip, End);
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - Tip;
                var t = Vector2.Dot(p, axis);
                var off = Mathf.Abs(p.x * axis.y - p.y * axis.x);
                var along = Mathf.Clamp(t, 0f, length);
                var edge = t < 0f ? p.magnitude : Mathf.Max(off - HalfWidth(along), t - length);
                var fill = FillColor(along, off);
                var rimAlpha = Mathf.Clamp01(Rim + 0.5f - edge);
                var color = Color.Lerp(Cream, fill, Mathf.Clamp01(0.5f - edge));
                color.a = edge <= 0f ? 1f : rimAlpha;
                tex.SetPixel(x, y, color);
            }
            tex.Apply();
            return tex;
        }

        // The hair swells from the point to its belly and narrows into the ferrule; the handle is a thin even reed.
        private static float HalfWidth(float t) =>
            t < 10f ? 3.6f * Mathf.Sin(t / 10f * Mathf.PI / 2f)
            : t < HairEnd ? Mathf.Lerp(3.6f, 3.0f, (t - 10f) / (HairEnd - 10f))
            : t < FerruleEnd ? 2.9f
            : 2.3f;

        private static Color FillColor(float t, float off)
        {
            if (t < HairEnd) return t > 4f && off < 1.1f ? InkGloss : Ink;   // a wet sheen along the hair
            if (t < FerruleEnd) return Lacquer;
            return Mathf.Abs(t - 25f) < 0.8f || Mathf.Abs(t - 34f) < 0.8f ? Node : Bamboo;
        }
    }
}
