using UnityEngine;

namespace SudokuRoguelike.UI
{
    public static class FloorThemeIconGenerator
    {
        private static readonly Sprite[] Cache = new Sprite[5];

        public static Sprite Get(int floorIndex)
        {
            var idx = Mathf.Clamp(floorIndex, 0, 4);
            if (Cache[idx] != null) return Cache[idx];
            Cache[idx] = Generate(idx);
            return Cache[idx];
        }

        private static Sprite Generate(int floorIndex)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var bg = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    tex.SetPixel(x, y, bg);

            switch (floorIndex)
            {
                case 0: DrawSakuraBlossom(tex, size); break;
                case 1: DrawBambooStalks(tex, size); break;
                case 2: DrawZenStones(tex, size); break;
                case 3: DrawKoiFish(tex, size); break;
                case 4: DrawTempleGate(tex, size); break;
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static void DrawSakuraBlossom(Texture2D tex, int size)
        {
            var pink = new Color(1f, 0.65f, 0.75f, 1f);
            var center = new Color(1f, 0.90f, 0.40f, 1f);
            var cx = size / 2f;
            var cy = size / 2f;

            for (var a = 0; a < 5; a++)
            {
                var angle = a * 72f * Mathf.Deg2Rad;
                var px = cx + Mathf.Cos(angle) * 14f;
                var py = cy + Mathf.Sin(angle) * 14f;
                FillCircle(tex, size, (int)px, (int)py, 10, pink);
            }
            FillCircle(tex, size, size / 2, size / 2, 6, center);
        }

        private static void DrawBambooStalks(Texture2D tex, int size)
        {
            var green = new Color(0.30f, 0.65f, 0.25f, 1f);
            var darkGreen = new Color(0.18f, 0.45f, 0.15f, 1f);
            var leaf = new Color(0.45f, 0.80f, 0.35f, 1f);

            void DrawStalk(int x)
            {
                for (var y = 8; y < 56; y++)
                    for (var dx = -2; dx <= 2; dx++)
                        SetPixelSafe(tex, size, x + dx, y, green);
                for (var dy = 0; dy < 3; dy++)
                    for (var dx = -3; dx <= 3; dx++)
                        SetPixelSafe(tex, size, x + dx, 20 + dy, darkGreen);
                for (var dy = 0; dy < 3; dy++)
                    for (var dx = -3; dx <= 3; dx++)
                        SetPixelSafe(tex, size, x + dx, 40 + dy, darkGreen);
            }

            DrawStalk(20);
            DrawStalk(32);
            DrawStalk(44);

            for (var i = 0; i < 6; i++)
                for (var j = 0; j < 3; j++)
                    SetPixelSafe(tex, size, 46 + i, 48 + j, leaf);
            for (var i = 0; i < 6; i++)
                for (var j = 0; j < 3; j++)
                    SetPixelSafe(tex, size, 14 - i, 28 + j, leaf);
        }

        private static void DrawZenStones(Texture2D tex, int size)
        {
            var stone1 = new Color(0.55f, 0.53f, 0.50f, 1f);
            var stone2 = new Color(0.65f, 0.63f, 0.58f, 1f);
            var stone3 = new Color(0.48f, 0.46f, 0.42f, 1f);
            var sand = new Color(0.88f, 0.85f, 0.78f, 0.60f);

            for (var y = 10; y < 18; y++)
                for (var x = 5; x < 59; x++)
                    if (y % 3 == 0) SetPixelSafe(tex, size, x, y, sand);

            FillEllipse(tex, size, 32, 30, 16, 8, stone1);
            FillEllipse(tex, size, 32, 40, 12, 7, stone2);
            FillEllipse(tex, size, 32, 50, 8, 6, stone3);
        }

        private static void DrawKoiFish(Texture2D tex, int size)
        {
            var water = new Color(0.15f, 0.45f, 0.60f, 0.40f);
            var orange = new Color(0.95f, 0.55f, 0.15f, 1f);
            var white = new Color(1f, 1f, 1f, 0.90f);
            var eye = new Color(0.05f, 0.05f, 0.05f, 1f);

            FillCircle(tex, size, 32, 32, 28, water);
            FillEllipse(tex, size, 28, 32, 14, 7, orange);
            FillEllipse(tex, size, 26, 34, 8, 3, white);

            for (var i = 0; i < 8; i++)
            {
                SetPixelSafe(tex, size, 42 + i, 30 + i / 2, orange);
                SetPixelSafe(tex, size, 42 + i, 34 - i / 2, orange);
                SetPixelSafe(tex, size, 42 + i, 31 + i / 2, orange);
                SetPixelSafe(tex, size, 42 + i, 33 - i / 2, orange);
            }
            SetPixelSafe(tex, size, 18, 31, eye);
            SetPixelSafe(tex, size, 19, 31, eye);
        }

        private static void DrawTempleGate(Texture2D tex, int size)
        {
            var red = new Color(0.80f, 0.15f, 0.10f, 1f);
            var darkRed = new Color(0.55f, 0.10f, 0.08f, 1f);
            var lantern = new Color(1f, 0.80f, 0.20f, 0.85f);

            for (var y = 10; y < 55; y++)
                for (var dx = 0; dx < 5; dx++)
                {
                    SetPixelSafe(tex, size, 18 + dx, y, red);
                    SetPixelSafe(tex, size, 41 + dx, y, red);
                }

            for (var x = 12; x < 52; x++)
                for (var dy = 0; dy < 5; dy++)
                    SetPixelSafe(tex, size, x, 52 + dy, darkRed);

            for (var x = 16; x < 48; x++)
                for (var dy = 0; dy < 3; dy++)
                    SetPixelSafe(tex, size, x, 46 + dy, red);

            FillCircle(tex, size, 32, 38, 4, lantern);
        }

        private static void FillCircle(Texture2D tex, int size, int cx, int cy, int r, Color color)
        {
            for (var y = cy - r; y <= cy + r; y++)
                for (var x = cx - r; x <= cx + r; x++)
                {
                    var dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= r + 0.5f)
                        SetPixelSafe(tex, size, x, y, color);
                }
        }

        private static void FillEllipse(Texture2D tex, int size, int cx, int cy, int rx, int ry, Color color)
        {
            for (var y = cy - ry; y <= cy + ry; y++)
                for (var x = cx - rx; x <= cx + rx; x++)
                {
                    var dx = (float)(x - cx) / rx;
                    var dy = (float)(y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                        SetPixelSafe(tex, size, x, y, color);
                }
        }

        private static void SetPixelSafe(Texture2D tex, int size, int x, int y, Color c)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
                tex.SetPixel(x, y, c);
        }
    }
}
