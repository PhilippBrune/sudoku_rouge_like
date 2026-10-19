using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // Parts of the full-screen in-run scenes in the ink-and-paper family (layout-proposals.md 1.3/1.5): the reward
    // tally and ema plaques (UI-STYLE-007), the relic shrine (UI-STYLE-008) and the market stall (UI-STYLE-009).
    // Anchors are fractions of the canvas, measured on the 1920×1080 mockups (x = px / 1920, y = 1 − py / 1080).
    internal static partial class ScrollUiKit
    {
        internal static readonly Color OfferingFocus = new Color(0.74f, 0.54f, 0.18f, 1f);
        internal static readonly Color OfferingText = new Color(0.99f, 0.93f, 0.76f, 1f);
        internal static readonly Color PaperText = new Color(0.96f, 0.93f, 0.85f, 1f);

        /// <summary>
        /// A full-screen scene over the path: a click-blocking backdrop with the painting. It sits directly after the
        /// path panel, so it covers the path and its buttons while the in-game options (a later sibling) stay on top.
        /// </summary>
        internal static Image Scene(GameObject pathPanel, string name, string background, Color tint)
        {
            var root = pathPanel.transform.parent;
            var screen = Fill(root, name, Vector2.zero, Vector2.one, Color.black);
            screen.raycastTarget = true; // nothing behind the scene takes clicks
            screen.transform.SetSiblingIndex(pathPanel.transform.GetSiblingIndex() + 1);
            Painting(screen.transform, "Backdrop", background, tint);
            return screen;
        }

        /// <summary>A paper tag with a soft shadow, slightly turned, optionally hanging from a gold cord.</summary>
        internal static Image PaperTag(Transform parent, string name, Vector2 min, Vector2 max, float angle, float cord = 0f)
        {
            var tag = Fill(parent, name, min, max, Paper);
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var shadow = tag.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(4f, -6f);
            if (cord > 0f)
            {
                var line = Fill(tag.transform, "Cord", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Gold);
                line.rectTransform.offsetMin = new Vector2(-1f, 0f);
                line.rectTransform.offsetMax = new Vector2(1f, cord);
            }
            return tag;
        }

        /// <summary>A paper banner with vermilion edges: a small eyebrow line over a large title.</summary>
        internal static Image Banner(Transform parent, string name, Vector2 min, Vector2 max,
            string eyebrowName, string eyebrow, string titleName, string title, int titleSize = 50)
        {
            var banner = Fill(parent, name, min, max, Paper);
            var shadow = banner.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -8f);
            Fill(banner.transform, "SideLeft", Vector2.zero, new Vector2(0.026f, 1f), Vermilion);
            Fill(banner.transform, "SideRight", new Vector2(0.974f, 0f), Vector2.one, Vermilion);
            Label(banner.transform, eyebrowName, eyebrow.ToUpperInvariant(), 18, TextAnchor.MiddleCenter, InkSoft,
                new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.88f), 12);
            Label(banner.transform, titleName, title, titleSize, TextAnchor.MiddleCenter, Ink,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.64f), Mathf.Max(18, titleSize / 2));
            return banner;
        }

        /// <summary>A small rounded tag with one word in capitals (rarity, tier).</summary>
        internal static Image Pill(Transform parent, string name, Vector2 min, Vector2 max, string text, Color fill,
            Color textColor, int size = 15)
        {
            var pill = Fill(parent, name, min, max, fill, Rounded(12f));
            Label(pill.transform, "Text", text.ToUpperInvariant(), size, TextAnchor.MiddleCenter, textColor,
                Vector2.zero, Vector2.one, 10);
            return pill;
        }

        /// <summary>A dark disc with the key that picks this choice (1–3).</summary>
        internal static Image KeyBadge(Transform parent, string name, Vector2 center, int key, float size = 34f)
        {
            var disc = Spot(parent, name, center, new Vector2(size, size), new Color(0.24f, 0.15f, 0.08f, 1f), Disc());
            Label(disc.transform, "Key", key.ToString(), Mathf.RoundToInt(size * 0.55f), TextAnchor.MiddleCenter,
                PaperText, Vector2.zero, Vector2.one);
            return disc;
        }

        /// <summary>One "label … value" row; an optional icon sits to the left of the label.</summary>
        internal static void ValueRow(Transform parent, string name, Vector2 min, Vector2 max, string label, string value,
            int size, Color valueColor, string iconPath = null)
        {
            var labelMinX = min.x;
            if (!string.IsNullOrEmpty(iconPath))
            {
                var h = max.y - min.y;
                Icon(parent, name + "_Icon", iconPath, new Vector2(min.x, min.y - h * 0.15f), new Vector2(min.x + 0.11f, max.y + h * 0.15f),
                    Color.white);
                labelMinX = min.x + 0.135f;
            }
            Label(parent, name + "_Label", label, size, TextAnchor.MiddleLeft, Ink,
                new Vector2(labelMinX, min.y), new Vector2(Mathf.Lerp(min.x, max.x, 0.72f), max.y), Mathf.Max(11, size - 8));
            Label(parent, name + "_Value", value, size, TextAnchor.MiddleRight, valueColor,
                new Vector2(Mathf.Lerp(min.x, max.x, 0.60f), min.y), max, Mathf.Max(11, size - 8));
        }

        /// <summary>
        /// One relic offering: a glow, the relic on a plinth, its tier, name and rule, and a Take button (the boss's
        /// offering table, UI-STYLE-005, and the relic shrine, UI-STYLE-008). <paramref name="center"/> is the x
        /// fraction of the canvas; the slot spans y 0.10–0.53. Returns the Take button.
        /// </summary>
        internal static Button Offering(Transform screen, int index, RelicInstance relic, float center, string takeLabel)
        {
            var slot = Area(screen, $"Offering_{index}", new Vector2(center - 0.11f, 0.10f), new Vector2(center + 0.11f, 0.53f));
            Spot(slot, "Glow", new Vector2(0.5f, 0.77f), new Vector2(200f, 200f), new Color(1f, 0.86f, 0.55f, 0.30f), Glow());
            Fill(slot, "Plinth", new Vector2(0.216f, 0.43f), new Vector2(0.784f, 0.586f), Color.white, Plinth());
            var icon = Icon(slot, "Icon", RelicService.GetIconFolder(relic.Id) + "/icon_" + RelicService.GetIconName(relic.Id),
                new Vector2(0.31f, 0.60f), new Vector2(0.69f, 0.92f), Color.white);
            InRunUiFactory.AddRelicTierPip(icon.transform, relic.Tier);
            Pill(slot, "Tier", new Vector2(0.40f, 0.384f), new Vector2(0.60f, 0.424f),
                LocalizationService.T($"Relic.Tier.{relic.Tier}", relic.Tier.ToString()), TierColor(relic.Tier),
                TierTextColor(relic.Tier), 14);
            Label(slot, "Name", RelicService.GetRelicName(relic.Id), 30, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.95f, 0.88f, 1f), new Vector2(0.02f, 0.295f), new Vector2(0.98f, 0.378f), 18);
            Label(slot, "Rule", RelicService.GetRelicDescription(relic.Id), 20, TextAnchor.UpperCenter,
                new Color(0.88f, 0.84f, 0.76f, 1f), new Vector2(0.02f, 0.160f), new Vector2(0.98f, 0.290f), 13);
            return RimButton(slot, $"OfferingTake_{index}", new Vector2(0.30f, 0.035f), new Vector2(0.70f, 0.138f),
                takeLabel, 22, Gold, OfferingFocus, OfferingText);
        }

        internal static Color RarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Epic => new Color(0.80f, 0.62f, 0.14f, 1f),
            ItemRarity.Rare => new Color(0.22f, 0.32f, 0.66f, 1f),
            _ => new Color(0.40f, 0.37f, 0.33f, 1f)
        };

        internal static Color RarityTextColor(ItemRarity rarity) =>
            rarity == ItemRarity.Epic ? new Color(0.20f, 0.12f, 0.04f, 1f) : PaperText;

        internal static Color TierColor(RelicTier tier) => tier switch
        {
            RelicTier.Legendary => new Color(0.80f, 0.62f, 0.14f, 1f),
            RelicTier.Tier1 => new Color(0.40f, 0.37f, 0.33f, 1f),
            _ => new Color(0.22f, 0.32f, 0.66f, 1f)
        };

        internal static Color TierTextColor(RelicTier tier) =>
            tier == RelicTier.Legendary ? new Color(0.20f, 0.12f, 0.04f, 1f) : PaperText;

        /// <summary>An ema: a wooden votive plaque, a rectangle under a low gable roof, lighter at the top.</summary>
        internal static Sprite Ema() => Cached("ema", 128, (x, y, s) =>
        {
            const float roof = 0.16f;          // share of the height the gable takes
            var u = Mathf.Abs(x + 0.5f - s * 0.5f) / (s * 0.5f);   // 0 at the centre, 1 at the sides
            var top = 1f - roof * u;           // the roof line falls from the ridge to the eaves
            var v = (y + 0.5f) / s;
            return Coverage((top - v) * s) * Coverage((1f - u) * s * 0.5f);
        }, (x, y, s) => Color.Lerp(new Color(0.72f, 0.51f, 0.31f), new Color(0.93f, 0.77f, 0.53f), y / (s - 1f)));

        /// <summary>A price tag: a card with a cut-off point on the left.</summary>
        internal static Sprite PriceTag() => Cached("pricetag", 64, (x, y, s) =>
        {
            const float point = 0.24f;         // width of the pointed end
            const float half = 0.36f;          // half the card's height
            var u = (x + 0.5f) / s;
            var w = Mathf.Abs((y + 0.5f) / s - 0.5f);
            var halfAt = u >= point ? half : half * u / point;
            return Coverage(Mathf.Min(halfAt - w, 1f - u) * s);
        });
    }
}
