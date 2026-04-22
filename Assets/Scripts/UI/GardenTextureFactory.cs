using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Shared pixel-art texture factory for garden-themed background tiles.
    /// All textures are created once (static) and reused across controllers.
    /// </summary>
    public static class GardenTextureFactory
    {
        private static Texture2D _mainTex;
        private static readonly Texture2D[] _floorThemeTex = new Texture2D[5];

        // ── Main garden tile ─────────────────────────────────────────────────────

        /// <summary>64×64 garden tile: grass, meandering dirt path, stepping stones, flowers.</summary>
        public static Texture2D GetOrCreate()
        {
            if (_mainTex != null) return _mainTex;

            const int size = 64;
            var tex = NewTex(size);

            var grassA = new Color(0.10f, 0.18f, 0.10f, 1f);
            var grassB = new Color(0.11f, 0.20f, 0.11f, 1f);
            var grassC = new Color(0.09f, 0.16f, 0.09f, 1f);
            var dirtA  = new Color(0.20f, 0.14f, 0.08f, 1f);
            var dirtB  = new Color(0.23f, 0.16f, 0.10f, 1f);
            var stone  = new Color(0.22f, 0.23f, 0.26f, 1f);
            var flower = new Color(0.82f, 0.58f, 0.70f, 1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var n   = Hash(x, y);
                var col = n < 0.33f ? grassA : n < 0.66f ? grassB : grassC;
                if (Hash(x + 11, y + 7) < 0.035f)
                    col = new Color(0.07f, 0.13f, 0.07f, 1f);
                tex.SetPixel(x, y, col);
            }

            // Meandering dirt path band
            for (var x = 0; x < size; x++)
            {
                var wave   = Mathf.Sin(x * 0.23f) * 4f + Mathf.Sin(x * 0.53f) * 2f;
                var center = size / 2 + Mathf.RoundToInt(wave);
                for (var dy = -2; dy <= 2; dy++)
                {
                    var yy = center + dy;
                    if (yy < 0 || yy >= size) continue;
                    if (Mathf.Abs(dy) == 2 && Hash(x + 3, yy + 5) < 0.45f) continue;
                    tex.SetPixel(x, yy, Hash(x + 17, yy + 31) < 0.5f ? dirtA : dirtB);
                }
            }

            // Stepping stones on the path
            for (var i = 0; i < 7; i++)
            {
                var sx = (i * 9 + 7) % size;
                var sy = (size / 2 + (i % 2 == 0 ? -1 : 1) * 2) % size;
                for (var dy = 0; dy < 2; dy++)
                for (var dx = 0; dx < 2; dx++)
                    tex.SetPixel((sx + dx) % size, (sy + dy) % size, stone);
            }

            // Flowers off the path
            for (var i = 0; i < 18; i++)
            {
                var px = (i * 13 + 9) % size;
                var py = (i * 17 + 21) % size;
                if (Mathf.Abs(py - size / 2) <= 4) continue;
                if (Hash(px + 2, py + 9) < 0.65f) continue;
                tex.SetPixel(px, py, flower);
            }

            tex.Apply();
            _mainTex = tex;
            return _mainTex;
        }

        // ── Floor-themed atmospheric overlay tiles ────────────────────────────────

        /// <summary>
        /// 64×64 floor-themed overlay tile drawn at low alpha (8–12%) above the main garden
        /// background. Each floor has a distinct motif that reinforces its visual identity.
        /// </summary>
        public static Texture2D GetOrCreateFloorTheme(int floor)
        {
            floor = Mathf.Clamp(floor, 0, 4);
            if (_floorThemeTex[floor] != null) return _floorThemeTex[floor];
            _floorThemeTex[floor] = floor switch
            {
                0 => BuildBambooTexture(),
                1 => BuildMossTexture(),
                2 => BuildKoiRippleTexture(),
                3 => BuildLanternTexture(),
                4 => BuildEmberTexture(),
                _ => BuildBambooTexture()
            };
            return _floorThemeTex[floor];
        }

        // ── Floor theme builders ──────────────────────────────────────────────────

        // Floor 0 — Garden Grove: bamboo stalks with joint marks
        private static Texture2D BuildBambooTexture()
        {
            const int size = 64;
            var tex   = NewTex(size);
            var clear = Color.clear;
            var stalk = new Color(0.20f, 0.45f, 0.12f, 1f);
            var joint = new Color(0.15f, 0.35f, 0.10f, 1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

            // Stalks every 16 px, 2 px wide
            for (var sx = 4; sx < size; sx += 16)
            {
                for (var y = 0; y < size; y++)
                {
                    tex.SetPixel(sx, y, stalk);
                    if (sx + 1 < size)
                        tex.SetPixel(sx + 1, y, new Color(stalk.r, stalk.g, stalk.b, 0.60f));
                    // Horizontal joint marks every 9 rows
                    if (y % 9 == 0)
                    {
                        tex.SetPixel(sx, y, joint);
                        if (sx + 2 < size) tex.SetPixel(sx + 2, y, joint);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        // Floor 1 — Koi Garden (dark undergrowth): sparse dark-green moss dots
        private static Texture2D BuildMossTexture()
        {
            const int size = 64;
            var tex   = NewTex(size);
            var clear = Color.clear;
            var moss1 = new Color(0.08f, 0.22f, 0.06f, 1f);
            var moss2 = new Color(0.06f, 0.17f, 0.05f, 1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var h = Hash(x, y);
                tex.SetPixel(x, y, h < 0.06f
                    ? (Hash(x + 5, y + 3) < 0.5f ? moss1 : moss2)
                    : clear);
            }
            tex.Apply();
            return tex;
        }

        // Floor 2 — Koi Terrace: concentric ripple rings in muted blue
        private static Texture2D BuildKoiRippleTexture()
        {
            const int size = 64;
            var tex    = NewTex(size);
            var clear  = Color.clear;
            var ripple = new Color(0.22f, 0.45f, 0.75f, 1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

            var centers = new (int cx, int cy)[] { (16, 16), (48, 40), (10, 48), (52, 10) };
            foreach (var (cx, cy) in centers)
            {
                for (var r = 6; r <= 20; r += 7)
                {
                    for (var theta = 0; theta < 360; theta += 2)
                    {
                        var rad = theta * Mathf.Deg2Rad;
                        var px  = cx + Mathf.RoundToInt(Mathf.Cos(rad) * r);
                        var py  = cy + Mathf.RoundToInt(Mathf.Sin(rad) * r);
                        if (px < 0 || px >= size || py < 0 || py >= size) continue;
                        var alpha = 1f - (r - 6) / 14f;
                        tex.SetPixel(px, py, new Color(ripple.r, ripple.g, ripple.b, alpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        // Floor 3 — Shrine Garden: amber lantern-glow diamond clusters
        private static Texture2D BuildLanternTexture()
        {
            const int size = 64;
            var tex     = NewTex(size);
            var clear   = Color.clear;
            var glow    = new Color(0.95f, 0.72f, 0.18f, 1f);
            var dimGlow = new Color(0.85f, 0.60f, 0.10f, 0.50f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

            var positions = new (int x, int y)[] { (12, 20), (40, 10), (28, 48), (54, 36), (6, 54) };
            foreach (var (lx, ly) in positions)
            {
                if (lx < 0 || lx >= size || ly < 0 || ly >= size) continue;
                tex.SetPixel(lx, ly, glow);
                if (lx - 1 >= 0)         tex.SetPixel(lx - 1, ly, dimGlow);
                if (lx + 1 < size)       tex.SetPixel(lx + 1, ly, dimGlow);
                if (ly - 1 >= 0)         tex.SetPixel(lx, ly - 1, dimGlow);
                if (ly + 1 < size)       tex.SetPixel(lx, ly + 1, dimGlow);
            }
            tex.Apply();
            return tex;
        }

        // Floor 4 — Demon Summit: red ember sparks + faint torii arch silhouette
        private static Texture2D BuildEmberTexture()
        {
            const int size = 64;
            var tex   = NewTex(size);
            var clear = Color.clear;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var h = Hash(x + 99, y + 77);
                if (h < 0.04f)
                {
                    var col = Hash(x, y) < 0.5f
                        ? new Color(0.90f, 0.30f, 0.08f, 1f)
                        : new Color(0.95f, 0.55f, 0.12f, 1f);
                    tex.SetPixel(x, y, col);
                }
                else
                    tex.SetPixel(x, y, clear);
            }

            // Faint torii arch — single horizontal sweep with sine lift
            for (var x = 4; x < size - 4; x++)
            {
                var arch = Mathf.RoundToInt(Mathf.Sin((x - 4) * Mathf.PI / (size - 8)) * 8f);
                var ay   = 10 + arch;
                if (ay >= 0 && ay < size)
                    tex.SetPixel(x, ay, new Color(0.70f, 0.12f, 0.06f, 0.55f));
            }
            tex.Apply();
            return tex;
        }

        // ── Shared helpers ────────────────────────────────────────────────────────

        private static Texture2D NewTex(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Repeat
            };

        /// <summary>Fast deterministic hash → [0, 1]. Same algorithm used in InRunController.</summary>
        public static float Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0x00FFFFFF) / 16777215f;
            }
        }
    }
}
