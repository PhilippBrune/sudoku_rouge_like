using System.Collections.Generic;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class PathNodeIconFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static readonly Color Transparent = new Color(0, 0, 0, 0);
        private static readonly Color StoneGray = new Color(0.55f, 0.53f, 0.48f, 1f);
        private static readonly Color CalmGreen = new Color(0.30f, 0.52f, 0.32f, 1f);
        private static readonly Color DarkGreen = new Color(0.18f, 0.34f, 0.20f, 1f);
        private static readonly Color LanternGold = new Color(0.92f, 0.76f, 0.22f, 1f);
        private static readonly Color WarmBrown = new Color(0.48f, 0.32f, 0.18f, 1f);
        private static readonly Color DarkBrown = new Color(0.30f, 0.20f, 0.10f, 1f);
        private static readonly Color BoneWhite = new Color(0.90f, 0.88f, 0.82f, 1f);
        private static readonly Color DeepRed = new Color(0.72f, 0.14f, 0.10f, 1f);
        private static readonly Color Black = new Color(0.08f, 0.06f, 0.06f, 1f);
        private static readonly Color TorikRed = new Color(0.78f, 0.18f, 0.12f, 1f);

        public static Sprite GetIcon(string nodeType)
        {
            if (Cache.TryGetValue(nodeType, out var cached))
                return cached;

            var tex = nodeType switch
            {
                "Puzzle" => DrawPuzzlePiece(),
                "ElitePuzzle" => DrawPuzzlePiece(),
                "Shop" => DrawTemple(),
                "Rest" => DrawBench(),
                "Relic" => DrawWalkingStick(),
                "PreBoss" => DrawEliteBossSkull(),
                "Boss" => DrawBossSkull(),
                _ => DrawPuzzlePiece()
            };

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
            Cache[nodeType] = sprite;
            return sprite;
        }

        private static Texture2D DrawPuzzlePiece()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Clear(t, Transparent);
            FillRect(t, 4, 3, 12, 13, CalmGreen);
            FillRect(t, 12, 6, 15, 10, CalmGreen);
            FillRect(t, 6, 13, 10, 16, CalmGreen);
            FillRect(t, 4, 6, 6, 10, Transparent);
            FillRect(t, 6, 3, 10, 5, Transparent);
            DrawOutline(t, DarkGreen);
            SetPx(t, 8, 8, LanternGold);
            SetPx(t, 7, 9, LanternGold);
            SetPx(t, 9, 7, LanternGold);
            return t;
        }

        private static Texture2D DrawTemple()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Clear(t, Transparent);
            FillRect(t, 3, 1, 5, 12, TorikRed);
            FillRect(t, 11, 1, 13, 12, TorikRed);
            FillRect(t, 2, 12, 14, 14, TorikRed);
            FillRect(t, 1, 14, 15, 16, TorikRed);
            FillRect(t, 4, 9, 12, 10, TorikRed);
            SetPx(t, 7, 15, LanternGold);
            SetPx(t, 8, 15, LanternGold);
            FillRect(t, 2, 0, 6, 1, StoneGray);
            FillRect(t, 10, 0, 14, 1, StoneGray);
            return t;
        }

        private static Texture2D DrawBench()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Clear(t, Transparent);
            FillRect(t, 2, 7, 14, 9, WarmBrown);
            FillRect(t, 3, 2, 5, 7, WarmBrown);
            FillRect(t, 11, 2, 13, 7, WarmBrown);
            FillRect(t, 2, 9, 14, 10, DarkBrown);
            FillRect(t, 3, 10, 4, 14, DarkBrown);
            FillRect(t, 12, 10, 13, 14, DarkBrown);
            FillRect(t, 4, 13, 12, 14, DarkBrown);
            SetPx(t, 1, 1, CalmGreen);
            SetPx(t, 7, 1, CalmGreen);
            SetPx(t, 14, 1, CalmGreen);
            SetPx(t, 7, 8, LanternGold);
            SetPx(t, 8, 8, LanternGold);
            return t;
        }

        private static Texture2D DrawWalkingStick()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Clear(t, Transparent);
            FillRect(t, 7, 1, 9, 13, WarmBrown);
            FillRect(t, 6, 13, 10, 15, DarkBrown);
            FillRect(t, 5, 14, 7, 16, DarkBrown);
            FillRect(t, 7, 11, 9, 12, LanternGold);
            SetPx(t, 7, 0, StoneGray);
            SetPx(t, 8, 0, StoneGray);
            return t;
        }

        private static Texture2D DrawEliteBossSkull()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Clear(t, DeepRed);
            DrawSkull(t, Black, BoneWhite);
            return t;
        }

        private static Texture2D DrawBossSkull()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Clear(t, Black);
            DrawSkull(t, DeepRed, LanternGold);
            return t;
        }

        private static void DrawSkull(Texture2D t, Color skullColor, Color eyeColor)
        {
            FillRect(t, 4, 8, 12, 14, skullColor);
            FillRect(t, 5, 14, 11, 15, skullColor);
            FillRect(t, 3, 9, 5, 13, skullColor);
            FillRect(t, 11, 9, 13, 13, skullColor);
            FillRect(t, 5, 4, 11, 8, skullColor);
            FillRect(t, 6, 3, 10, 4, skullColor);
            FillRect(t, 5, 10, 7, 12, eyeColor);
            FillRect(t, 9, 10, 11, 12, eyeColor);
            SetPx(t, 7, 9, eyeColor);
            SetPx(t, 8, 9, eyeColor);
            SetPx(t, 6, 5, eyeColor);
            SetPx(t, 8, 5, eyeColor);
            SetPx(t, 10, 5, eyeColor);
        }

        private static void Clear(Texture2D t, Color c)
        {
            var px = new Color[t.width * t.height];
            for (var i = 0; i < px.Length; i++) px[i] = c;
            t.SetPixels(px);
        }

        private static void FillRect(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            for (var y = y0; y < y1 && y < t.height; y++)
                for (var x = x0; x < x1 && x < t.width; x++)
                    t.SetPixel(x, y, c);
        }

        private static void SetPx(Texture2D t, int x, int y, Color c)
        {
            if (x >= 0 && x < t.width && y >= 0 && y < t.height)
                t.SetPixel(x, y, c);
        }

        private static void DrawOutline(Texture2D t, Color c)
        {
            for (var x = 0; x < t.width; x++)
            {
                for (var y = 0; y < t.height; y++)
                {
                    if (t.GetPixel(x, y).a < 0.01f) continue;
                    var hasTransparentNeighbor =
                        (x > 0 && t.GetPixel(x - 1, y).a < 0.01f) ||
                        (x < t.width - 1 && t.GetPixel(x + 1, y).a < 0.01f) ||
                        (y > 0 && t.GetPixel(x, y - 1).a < 0.01f) ||
                        (y < t.height - 1 && t.GetPixel(x, y + 1).a < 0.01f);
                    if (hasTransparentNeighbor)
                        t.SetPixel(x, y, c);
                }
            }
        }
    }
}
