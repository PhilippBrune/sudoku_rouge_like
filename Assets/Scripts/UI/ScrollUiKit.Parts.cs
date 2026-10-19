using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // Scroll pieces shared by the garden path (UI-STYLE-001) and the puzzle screen (UI-STYLE-002): the blurred painting
    // behind the scroll, the seal stamp, round paper buttons and a brush-stroke sprite for ink lines.
    internal static partial class ScrollUiKit
    {
        /// <summary>
        /// A soft, blurred copy of a painting for the backdrop behind a scroll. The background PNGs are not readable on
        /// the CPU, so the copy is made on the GPU by scaling down twice with bilinear filtering; it is cached per
        /// texture. Without a graphics device (batchmode tests) the painting itself is returned.
        /// </summary>
        internal static Texture BlurredCopy(Texture2D painting)
        {
            if (painting == null) return null;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return painting;
            if (BlurCache.TryGetValue(painting, out var cached) && cached != null)
            {
                if (cached.IsCreated()) return cached;
                cached.Release(); // lost with the graphics device: make it again
                Object.Destroy(cached);
            }

            var half = RenderTexture.GetTemporary(Mathf.Max(8, painting.width / 4), Mathf.Max(8, painting.height / 4), 0);
            half.filterMode = FilterMode.Bilinear;
            Graphics.Blit(painting, half);
            var soft = new RenderTexture(Mathf.Max(8, painting.width / 10), Mathf.Max(8, painting.height / 10), 0)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            Graphics.Blit(half, soft);
            RenderTexture.ReleaseTemporary(half);
            BlurCache[painting] = soft;
            return soft;
        }

        private static readonly Dictionary<Texture2D, RenderTexture> BlurCache = new Dictionary<Texture2D, RenderTexture>();

        /// <summary>
        /// A rect that reaches past its parent by <paramref name="bleed"/> of the parent's size on every side, for
        /// backdrops of panels that are inset from the screen edge (the in-run panels sit at 3–97 %).
        /// </summary>
        internal static RectTransform Bleed(Transform parent, string name, float bleed = 0.035f) =>
            Area(parent, name, new Vector2(-bleed, -bleed), new Vector2(1f + bleed, 1f + bleed));

        /// <summary>The vermilion seal stamp with a paper frame and the boss's avatar, slightly turned like a hand-pressed seal.</summary>
        internal static Image Seal(Transform parent, string name, Vector2 center, float size, string iconPath,
            float angle = -4f)
        {
            var seal = Spot(parent, name, center, new Vector2(size, size), Vermilion, Rounded(6f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var inner = Area(seal.transform, "SealInner", Vector2.zero, Vector2.one);
            var inset = Mathf.Max(5f, size * 0.06f);
            inner.offsetMin = new Vector2(inset, inset);
            inner.offsetMax = new Vector2(-inset, -inset);
            InkFrame(inner, new Color(0.98f, 0.93f, 0.85f, 0.9f), Mathf.Max(2f, size * 0.02f));
            Icon(seal.transform, "BossAvatar", iconPath, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Color.white);
            return seal;
        }

        /// <summary>A round paper button with a wooden rim and an icon (Save & Quit, Options).</summary>
        internal static Button RoundButton(Transform parent, string name, Vector2 center, float size, string iconPath)
        {
            var rim = Spot(parent, name, center, new Vector2(size, size), new Color(0.42f, 0.29f, 0.17f, 1f), Disc());
            rim.raycastTarget = true;
            var face = Fill(rim.transform, "Face", Vector2.zero, Vector2.one, Color.white, Disc());
            face.rectTransform.offsetMin = new Vector2(2.5f, 2.5f);
            face.rectTransform.offsetMax = new Vector2(-2.5f, -2.5f);
            var button = rim.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            var colors = StateColors(new Color(0.94f, 0.89f, 0.78f, 0.95f));
            colors.selectedColor = new Color(1f, 0.86f, 0.55f, 1f);
            colors.highlightedColor = new Color(1f, 0.93f, 0.75f, 1f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.Explicit };
            Icon(rim.transform, "Icon", iconPath, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Color.white);
            return button;
        }

        // ── puzzle screen (UI-STYLE-002) ──

        internal static readonly Color WoodLight = new Color(0.84f, 0.62f, 0.38f, 1f);
        internal static readonly Color WoodTileText = new Color(0.20f, 0.11f, 0.05f, 1f);

        /// <summary>
        /// A hanging scroll: paper with moss brocade bands, a wooden rod across the top and the bottom (wider than the
        /// paper) and the cord it hangs from. Returns the paper rect.
        /// </summary>
        internal static RectTransform HangingScroll(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var paper = Fill(parent, name, min, max, Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -18f);
            var rt = paper.rectTransform;
            Fill(rt, "PaperLight", new Vector2(0f, 0.45f), new Vector2(0.7f, 1f), new Color(1f, 1f, 1f, 0.22f), Glow());
            BrocadeBand(rt, "BrocadeTop", new Vector2(0f, 0.977f), Vector2.one, true);
            BrocadeBand(rt, "BrocadeBottom", Vector2.zero, new Vector2(1f, 0.023f), true);
            Rod(rt, "RodTop", 0.984f, 1.016f);
            Rod(rt, "RodBottom", -0.023f, 0.009f);
            var cord = Fill(rt, "Cord", new Vector2(0.5f, 1.016f), new Vector2(0.5f, 1.08f), Gold);
            cord.rectTransform.offsetMin = new Vector2(-1f, 0f);
            cord.rectTransform.offsetMax = new Vector2(1f, 0f);
            return rt;
        }

        private static void Rod(RectTransform paper, string name, float yMin, float yMax)
        {
            var rod = Fill(paper, name, new Vector2(-0.035f, yMin), new Vector2(1.035f, yMax), Wood, Rounded(14f));
            var shadow = rod.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -5f);
            Fill(rod.transform, "Light", new Vector2(0.01f, 0.52f), new Vector2(0.99f, 0.86f), new Color(1f, 0.9f, 0.75f, 0.28f),
                Rounded(8f));
        }

        /// <summary>
        /// Turns an existing bar (background + Filled child) into a paper lantern: an oval body that glows from the
        /// bottom up with the fill amount, dark caps, paper ribs and a hanging string. Keeps the objects and names, so
        /// the code that sets the fill keeps working.
        /// </summary>
        internal static void MakeLantern(Image body, Image fill, Color shade)
        {
            body.sprite = Disc();
            body.type = Image.Type.Simple;
            body.color = shade;
            fill.sprite = Disc();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var glow = Fill(body.transform, "LanternGlow", new Vector2(-0.25f, -0.2f), new Vector2(1.25f, 1.2f),
                new Color(fill.color.r, fill.color.g, fill.color.b, 0.28f), Glow());
            glow.transform.SetAsFirstSibling();
            for (var i = 1; i < 9; i++)
                Line(body.transform, $"Rib_{i}", new Vector2(0.06f, i / 9f), new Vector2(0.94f, i / 9f),
                    new Color(0.24f, 0.12f, 0.05f, 0.22f), 2f);
            Fill(body.transform, "CapTop", new Vector2(0.20f, 0.965f), new Vector2(0.80f, 1.045f), WoodDark, Rounded(4f));
            Fill(body.transform, "CapBottom", new Vector2(0.20f, -0.045f), new Vector2(0.80f, 0.035f), WoodDark, Rounded(4f));
            var cord = Fill(body.transform, "String", new Vector2(0.5f, 1.045f), new Vector2(0.5f, 1.40f), Gold);
            cord.rectTransform.offsetMin = new Vector2(-1f, 0f);
            cord.rectTransform.offsetMax = new Vector2(1f, 0f);
        }

        /// <summary>Wooden tile colours for a Button (number pad): warm wood, lighter when focused, faded when disabled.</summary>
        internal static void MakeWoodTile(Button button, Image image)
        {
            image.sprite = Rounded(10f);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var colors = StateColors(WoodLight);
            colors.disabledColor = new Color(0.48f, 0.36f, 0.24f, 0.45f);
            colors.selectedColor = new Color(0.98f, 0.82f, 0.50f, 1f);
            button.colors = colors;
            var shadow = image.gameObject.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.25f, 0.14f, 0.06f, 0.75f);
            shadow.effectDistance = new Vector2(0f, -5f);
        }

        /// <summary>A paper slip pinned with a vermilion pin: the look of the rule notes beside the puzzle.</summary>
        internal static void MakeSlip(Image image, float angle)
        {
            image.sprite = null;
            image.color = new Color(0.95f, 0.91f, 0.81f, 0.98f);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var shadow = image.gameObject.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(3f, -7f);
            if (image.transform.Find("Pin") == null)
                Spot(image.transform, "Pin", new Vector2(0.5f, 0.985f), new Vector2(12f, 12f), Vermilion, Disc());
        }

        /// <summary>Two opposite arrows (⇄) for bridge tiles; drawn because the game font has no arrow glyphs.</summary>
        internal static Sprite SwapArrows() => Cached("swaparrows", 96, (x, y, s) =>
        {
            var p = new Vector2(x, y) / s;
            float D(Vector2 a, Vector2 b) => SegmentDistance(p, a, b);
            var top = Mathf.Min(D(new Vector2(0.18f, 0.64f), new Vector2(0.82f, 0.64f)),
                Mathf.Min(D(new Vector2(0.82f, 0.64f), new Vector2(0.66f, 0.78f)), D(new Vector2(0.82f, 0.64f), new Vector2(0.66f, 0.50f))));
            var bottom = Mathf.Min(D(new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.36f)),
                Mathf.Min(D(new Vector2(0.18f, 0.36f), new Vector2(0.34f, 0.50f)), D(new Vector2(0.18f, 0.36f), new Vector2(0.34f, 0.22f))));
            return Coverage((0.055f - Mathf.Min(top, bottom)) * s);
        });

        /// <summary>
        /// An ink stroke for route lines: full along its length, soft and slightly ragged across it, so a stretched
        /// and rotated Image reads as a brush line rather than a ruler line.
        /// </summary>
        internal static Sprite Stroke() => Cached("stroke", 64, (x, y, s) =>
        {
            var across = Mathf.Abs(y + 0.5f - s * 0.5f) / (s * 0.5f);            // 0 centre … 1 edge
            var ragged = 0.80f + 0.12f * Mathf.Sin(x * 0.9f) + 0.06f * Mathf.Sin(x * 2.3f + 1.3f);
            return Mathf.Clamp01((ragged - across) * 6f);
        });
    }
}
