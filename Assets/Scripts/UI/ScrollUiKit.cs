using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Paper, ink, brocade and wood pieces of the scroll screens (docs/design/layout-proposals.md, UI-STYLE-004/005):
    /// a hand scroll between two wooden rollers, ink frames, ensō pick rings, pill buttons, glows and plinths. All
    /// sprites are drawn in code once and cached; positions are anchors relative to the parent, like the rest of the
    /// procedural UI.
    /// </summary>
    internal static class ScrollUiKit
    {
        internal static readonly Color Paper      = new Color(0.92f, 0.88f, 0.79f, 1f);
        internal static readonly Color PaperDeep  = new Color(0.83f, 0.77f, 0.65f, 1f);
        internal static readonly Color Ink        = new Color(0.13f, 0.11f, 0.09f, 1f);
        internal static readonly Color InkSoft    = new Color(0.33f, 0.29f, 0.24f, 1f);
        internal static readonly Color Vermilion  = new Color(0.70f, 0.20f, 0.15f, 1f);
        internal static readonly Color Brocade    = new Color(0.38f, 0.11f, 0.11f, 1f);
        internal static readonly Color BrocadeAlt = new Color(0.29f, 0.08f, 0.09f, 1f);
        internal static readonly Color Gold       = new Color(0.86f, 0.71f, 0.38f, 1f);
        internal static readonly Color Wood       = new Color(0.70f, 0.46f, 0.24f, 1f);
        internal static readonly Color WoodDark   = new Color(0.20f, 0.12f, 0.06f, 1f);

        // ── layout ──

        internal static RectTransform Area(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        internal static Image Fill(Transform parent, string name, Vector2 min, Vector2 max, Color color,
            Sprite sprite = null)
        {
            var img = Area(parent, name, min, max).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        /// <summary>A square-ish element of a fixed size in canvas units, centred on an anchor point.</summary>
        internal static Image Spot(Transform parent, string name, Vector2 center, Vector2 size, Color color,
            Sprite sprite = null)
        {
            var img = Fill(parent, name, center, center, color, sprite);
            img.rectTransform.sizeDelta = size;
            return img;
        }

        internal static Image Icon(Transform parent, string name, string spritePath, Vector2 min, Vector2 max,
            Color tint)
        {
            var img = Fill(parent, name, min, max, tint);
            img.sprite = string.IsNullOrEmpty(spritePath) ? null : InRunUiFactory.LoadResourceSprite(spritePath);
            img.preserveAspect = true;
            if (img.sprite == null) img.color = Color.clear;
            return img;
        }

        /// <summary>Text in the game font. A <paramref name="minSize"/> above 0 lets long translations shrink to fit.</summary>
        internal static Text Label(Transform parent, string name, string text, int size, TextAnchor anchor,
            Color color, Vector2 min, Vector2 max, int minSize = 0, FontStyle style = FontStyle.Normal)
        {
            var t = InRunUiFactory.CreateText(parent, name, text, size, anchor, color);
            t.rectTransform.anchorMin = min;
            t.rectTransform.anchorMax = max;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = minSize > 0 ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            if (minSize > 0)
            {
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = minSize;
                t.resizeTextMaxSize = size;
            }
            return t;
        }

        /// <summary>A full-size painting from Resources/background, tinted (a dark tint makes it a backdrop).</summary>
        internal static RawImage Painting(Transform parent, string name, string background, Color tint)
        {
            var raw = Area(parent, name, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            raw.texture = Resources.Load<Texture2D>("background/" + background);
            raw.color = raw.texture != null ? tint : Color.clear;
            raw.raycastTarget = false;
            return raw;
        }

        /// <summary>
        /// A horizontal hand scroll: paper with the painting as a faint wash, brocade bands at top and bottom and a
        /// wooden roller on each side (outside the rect). Returns the paper rect; children added later sit on the paper.
        /// </summary>
        internal static RectTransform HandScroll(Transform parent, string name, Vector2 min, Vector2 max,
            string watermark)
        {
            var paper = Fill(parent, name, min, max, Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -14f);
            var rt = paper.rectTransform;

            if (!string.IsNullOrEmpty(watermark))
            {
                var wash = Painting(rt, "PaperWash", watermark, new Color(0.42f, 0.35f, 0.26f, 0.16f));
                wash.uvRect = new Rect(0.05f, 0.05f, 0.9f, 0.9f);
            }
            Fill(rt, "PaperLight", new Vector2(0f, 0.35f), new Vector2(0.6f, 1f), new Color(1f, 1f, 1f, 0.18f), Glow());

            BrocadeBand(rt, "BrocadeTop", new Vector2(0f, 0.965f), Vector2.one);
            BrocadeBand(rt, "BrocadeBottom", Vector2.zero, new Vector2(1f, 0.035f));

            Roller(rt, "RollerLeft", -0.029f, 0f);
            Roller(rt, "RollerRight", 1f, 1.029f);
            return rt;
        }

        private static void BrocadeBand(RectTransform paper, string name, Vector2 min, Vector2 max)
        {
            var band = Area(paper, name, min, max).gameObject.AddComponent<RawImage>();
            band.texture = StripeTexture();
            band.uvRect = new Rect(0f, 0f, 90f, 1f);
            band.raycastTarget = false;
            Line(band.transform, "GoldTop", new Vector2(0f, 1f), new Vector2(1f, 1f), Gold, 2f);
            Line(band.transform, "GoldBottom", Vector2.zero, new Vector2(1f, 0f), Gold, 2f);
        }

        private static void Roller(RectTransform paper, string name, float xMin, float xMax)
        {
            var roller = Fill(paper, name, new Vector2(xMin, -0.045f), new Vector2(xMax, 1.045f), Wood, RollerShade());
            Fill(roller.transform, "CapTop", new Vector2(-0.12f, 0.985f), new Vector2(1.12f, 1.015f), WoodDark, Disc());
            Fill(roller.transform, "CapBottom", new Vector2(-0.12f, -0.015f), new Vector2(1.12f, 0.015f), WoodDark, Disc());
        }

        /// <summary>A straight line of <paramref name="width"/> canvas units along a horizontal or vertical anchor span.</summary>
        internal static Image Line(Transform parent, string name, Vector2 from, Vector2 to, Color color, float width)
        {
            var img = Fill(parent, name, from, to, color);
            var horizontal = Mathf.Approximately(from.y, to.y);
            img.rectTransform.offsetMin = horizontal ? new Vector2(0f, -width * 0.5f) : new Vector2(-width * 0.5f, 0f);
            img.rectTransform.offsetMax = horizontal ? new Vector2(0f, width * 0.5f) : new Vector2(width * 0.5f, 0f);
            return img;
        }

        /// <summary>An ink frame on the inside edge of <paramref name="parent"/>, with a faint second stroke like a brush line.</summary>
        internal static List<Image> InkFrame(Transform parent, Color color, float width)
        {
            var lines = new List<Image>();
            var faint = new Color(color.r, color.g, color.b, color.a * 0.35f);
            foreach (var (inset, tint, suffix) in new[] { (0f, color, ""), (width + 1.5f, faint, "Faint") })
            {
                lines.Add(Edge(parent, "Ink_T" + suffix, new Vector2(0f, 1f), Vector2.one, new Vector2(inset, -inset - width), new Vector2(-inset, -inset), tint));
                lines.Add(Edge(parent, "Ink_B" + suffix, Vector2.zero, new Vector2(1f, 0f), new Vector2(inset, inset), new Vector2(-inset, inset + width), tint));
                lines.Add(Edge(parent, "Ink_L" + suffix, Vector2.zero, new Vector2(0f, 1f), new Vector2(inset, inset), new Vector2(inset + width, -inset), tint));
                lines.Add(Edge(parent, "Ink_R" + suffix, new Vector2(1f, 0f), Vector2.one, new Vector2(-inset - width, inset), new Vector2(-inset, -inset), tint));
            }
            return lines;
        }

        private static Image Edge(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
            Color color)
        {
            var img = Fill(parent, name, aMin, aMax, color);
            img.rectTransform.offsetMin = oMin;
            img.rectTransform.offsetMax = oMax;
            return img;
        }

        /// <summary>
        /// A rounded button: <paramref name="fill"/> as its normal colour, lighter when highlighted or selected
        /// (mouse or controller focus), darker when pressed, faded when not yet usable.
        /// </summary>
        internal static Button PillButton(Transform parent, string name, Vector2 min, Vector2 max, string label,
            int fontSize, Color fill, Color textColor, float radius = 12f)
        {
            var img = Fill(parent, name, min, max, Color.white, Rounded(radius));
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.colors = StateColors(fill);
            btn.navigation = new Navigation { mode = Navigation.Mode.Explicit };
            var text = Label(img.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, textColor,
                new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), Mathf.Max(10, fontSize - 8));
            text.raycastTarget = false;
            return btn;
        }

        /// <summary>A pill with a <paramref name="rim"/> border: transparent-dark at rest, filled when focused.</summary>
        internal static Button RimButton(Transform parent, string name, Vector2 min, Vector2 max, string label,
            int fontSize, Color rim, Color focusFill, Color textColor)
        {
            var rimImg = Fill(parent, name, min, max, rim, Rounded(30f));
            rimImg.raycastTarget = true;
            var fillImg = Fill(rimImg.transform, "Fill", Vector2.zero, Vector2.one, Color.white, Rounded(30f));
            fillImg.rectTransform.offsetMin = new Vector2(2f, 2f);
            fillImg.rectTransform.offsetMax = new Vector2(-2f, -2f);
            var btn = rimImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = fillImg;
            var colors = StateColors(new Color(0.10f, 0.08f, 0.05f, 0.80f));
            colors.highlightedColor = focusFill;
            colors.selectedColor = focusFill;
            colors.pressedColor = Color.Lerp(focusFill, Color.black, 0.25f);
            btn.colors = colors;
            btn.navigation = new Navigation { mode = Navigation.Mode.Explicit };
            Label(rimImg.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, textColor,
                new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), Mathf.Max(10, fontSize - 8));
            return btn;
        }

        internal static ColorBlock StateColors(Color fill)
        {
            return new ColorBlock
            {
                normalColor = fill,
                highlightedColor = Color.Lerp(fill, Color.white, 0.22f),
                selectedColor = Color.Lerp(fill, Color.white, 0.32f),
                pressedColor = Color.Lerp(fill, Color.black, 0.25f),
                disabledColor = new Color(fill.r, fill.g, fill.b, fill.a * 0.45f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        // ── sprites, drawn once ──

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static Texture2D _stripes;

        internal static Sprite Disc() => Cached("disc", 64, (x, y, s) => Coverage(s * 0.5f - Dist(x, y, s)));

        internal static Sprite Glow() => Cached("glow", 64, (x, y, s) =>
        {
            var t = Mathf.Clamp01(1f - Dist(x, y, s) / (s * 0.5f));
            return t * t;
        });

        /// <summary>An ensō: a brush circle that thickens in the middle of the stroke and leaves a gap at the top right.</summary>
        internal static Sprite Enso() => Cached("enso", 128, (x, y, s) =>
        {
            var c = s * 0.5f - 0.5f;
            var dx = x - c;
            var dy = y - c;
            var along = Mathf.Repeat((Mathf.Atan2(dy, dx) - 1.25f) / (2f * Mathf.PI), 1f);
            const float strokeEnd = 0.91f;
            if (along > strokeEnd) return 0f;
            var phase = along / strokeEnd;
            var width = s * (0.035f + 0.05f * Mathf.Pow(Mathf.Sin(Mathf.PI * phase), 0.6f));
            var radius = s * 0.40f + s * 0.012f * Mathf.Sin(3f * Mathf.Atan2(dy, dx));
            return Coverage(width * 0.5f - Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius));
        });

        internal static Sprite Tick() => Cached("tick", 128, (x, y, s) =>
        {
            var a = new Vector2(0.27f, 0.50f) * s;
            var b = new Vector2(0.44f, 0.33f) * s;
            var c = new Vector2(0.76f, 0.70f) * s;
            var p = new Vector2(x, y);
            var d = Mathf.Min(SegmentDistance(p, a, b), SegmentDistance(p, b, c));
            return Coverage(s * 0.045f - d);
        });

        /// <summary>A trapezoid plinth (narrow top face, wide base), gold in light and bronze in shade.</summary>
        internal static Sprite Plinth() => Cached("plinth", 128, (x, y, s) =>
        {
            var half = Mathf.Lerp(0.5f, 0.40f, y / (s - 1f)) * s;   // y = 0 is the wide base
            return Coverage(half - Mathf.Abs(x + 0.5f - s * 0.5f));
        }, (x, y, s) => Color.Lerp(new Color(0.58f, 0.41f, 0.24f), new Color(0.88f, 0.73f, 0.50f), y / (s - 1f)));

        /// <summary>A vertical roller's shading: dark edges, a lit band left of the centre.</summary>
        internal static Sprite RollerShade() => Cached("roller", 32, (x, y, s) => 1f, (x, y, s) =>
        {
            var u = x / (s - 1f);
            var light = 0.55f + 0.5f * Mathf.Exp(-Mathf.Pow((u - 0.38f) / 0.22f, 2f));
            return new Color(light, light, light);
        });

        /// <summary>A rounded rectangle for 9-slicing; <paramref name="radius"/> is the corner radius in canvas units.</summary>
        internal static Sprite Rounded(float radius)
        {
            var r = Mathf.Clamp(Mathf.RoundToInt(radius), 2, 30);
            var key = "rounded" + r;
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var size = r * 2 + 4;
            var tex = NewTexture(size);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var qx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - r), 0f);
                var qy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - r), 0f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Coverage(r - Mathf.Sqrt(qx * qx + qy * qy))));
            }
            tex.Apply();
            var border = r + 1;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset; // cached for the session: survive scene loads
            Sprites[key] = sprite;
            return sprite;
        }

        private static Texture2D StripeTexture()
        {
            if (_stripes != null) return _stripes;
            _stripes = NewTexture(8, 1);
            _stripes.filterMode = FilterMode.Point;
            _stripes.wrapMode = TextureWrapMode.Repeat;
            for (var x = 0; x < 8; x++) _stripes.SetPixel(x, 0, x < 4 ? Brocade : BrocadeAlt);
            _stripes.Apply();
            return _stripes;
        }

        private static Sprite Cached(string key, int size, System.Func<float, float, float, float> alpha,
            System.Func<float, float, float, Color> color = null)
        {
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var tex = NewTexture(size);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var rgb = color?.Invoke(x, y, size) ?? Color.white;
                tex.SetPixel(x, y, new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(alpha(x, y, size))));
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset; // cached for the session: survive scene loads
            Sprites[key] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int width, int height = -1)
        {
            var tex = new Texture2D(width, height < 0 ? width : height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            return tex;
        }

        private static float Dist(float x, float y, float size)
        {
            var c = size * 0.5f - 0.5f;
            return Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
        }

        private static float Coverage(float signedDistance) => Mathf.Clamp01(signedDistance + 0.5f);

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
