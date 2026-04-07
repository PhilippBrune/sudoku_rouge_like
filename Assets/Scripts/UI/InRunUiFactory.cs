using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Shared static factory helpers for all in-run sub-controllers.
    /// </summary>
    internal static class InRunUiFactory
    {
        // ── In-run panel/button color scheme (distinct from GamePalette) ──
        internal static readonly Color PanelBg    = new Color(0.08f, 0.12f, 0.16f, 0.94f);
        internal static readonly Color BtnColor   = new Color(0.15f, 0.22f, 0.29f, 0.94f);
        internal static readonly Color AccentGold = new Color(0.98f, 0.83f, 0.26f, 1f);
        internal static readonly Color TextColor  = new Color(0.96f, 0.93f, 0.82f, 1f);

        // ── Font ──
        private static Font _font;
        internal static Font GetFont()
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        // ── Text ──
        internal static Text CreateText(Transform parent, string name, string content,
            int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = GetFont();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            return t;
        }

        // ── Layout helpers ──
        internal static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (var i = root.childCount - 1; i >= 0; i--)
                Object.Destroy(root.GetChild(i).gameObject);
        }

        internal static void ClearNamedChildren(Transform parent, string prefix)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                    Object.Destroy(child.gameObject);
            }
        }

        internal static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        internal static void HidePanel(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }

        // ── Bar sprite ──
        internal static void EnsureBarSprite(Image fill)
        {
            if (fill.sprite != null) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            fill.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
        }

        // ── Border helper ──
        internal static Image CreateBorder(Transform parent, string name,
            Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = omin; rt.offsetMax = omax;
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 0.78f, 0.26f, 0);
            img.raycastTarget = false;
            return img;
        }

        // ── RectTransform copy ──
        internal static void CopyRectTransform(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, src.rect.width);
            dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, src.rect.height);
        }

        // ── Panel button ──
        internal static Button CreatePanelButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = BtnColor;

            var t = CreateText(go.transform, "Label", label, 12, TextAnchor.MiddleCenter, TextColor);
            StretchFill(t.rectTransform);
            t.verticalOverflow = VerticalWrapMode.Overflow;

            return go.GetComponent<Button>();
        }

        // ── Overlay panel (reward / shop) ──
        internal static GameObject CreateOverlayPanel(Transform parent, string name, string title)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.20f, 0.18f);
            pr.anchorMax = new Vector2(0.80f, 0.78f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = PanelBg;

            var t = CreateText(panel.transform, "Title", title, 22, TextAnchor.MiddleCenter, AccentGold);
            t.rectTransform.anchorMin = new Vector2(0.06f, 0.86f);
            t.rectTransform.anchorMax = new Vector2(0.94f, 0.97f);
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;

            var summary = CreateText(panel.transform, "Summary", "", 14, TextAnchor.UpperLeft, TextColor);
            summary.rectTransform.anchorMin = new Vector2(0.08f, 0.58f);
            summary.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            summary.rectTransform.offsetMin = Vector2.zero;
            summary.rectTransform.offsetMax = Vector2.zero;

            return panel;
        }

        // ── Button icon ──
        internal static void SetButtonIcon(Button btn, string iconName, bool unknown = false)
        {
            var sprite = unknown ? null : (string.IsNullOrEmpty(iconName) ? null : Resources.Load<Sprite>("GeneratedIcons/icon_" + iconName));

            // Reposition label to bottom
            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
                label.rectTransform.anchorMax = new Vector2(0.98f, 0.36f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.fontSize = 10;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (sprite == null && !unknown) return;

            // Icon fills top 60% of button
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(btn.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.05f, 0.38f);
            iconRt.anchorMax = new Vector2(0.95f, 0.97f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var img = iconGo.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            else
            {
                // "???" placeholder
                img.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                var qText = new GameObject("UnknownLabel", typeof(RectTransform), typeof(Text));
                qText.transform.SetParent(iconGo.transform, false);
                var qt = qText.GetComponent<Text>();
                qt.text = "???";
                qt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                qt.fontSize = 22;
                qt.alignment = TextAnchor.MiddleCenter;
                qt.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                var qRt = qText.GetComponent<RectTransform>();
                qRt.anchorMin = Vector2.zero;
                qRt.anchorMax = Vector2.one;
                qRt.offsetMin = Vector2.zero;
                qRt.offsetMax = Vector2.zero;
            }
        }

        // ── Bag slot button ──
        internal static Button CreateBagSlotButton(Transform parent, string name, Vector2 ancMin, Vector2 ancMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.16f, 0.20f, 0.80f);

            var btn = go.GetComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = new Color(0.10f, 0.16f, 0.20f, 0.80f);
            cols.highlightedColor = new Color(0.17f, 0.26f, 0.33f, 0.90f);
            cols.pressedColor = new Color(0.07f, 0.11f, 0.14f, 0.90f);
            cols.disabledColor = new Color(0.06f, 0.09f, 0.11f, 0.45f);
            btn.colors = cols;

            // Icon (left ~30%)
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.04f, 0.12f);
            iconRt.anchorMax = new Vector2(0.36f, 0.88f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            iconGo.GetComponent<Image>().raycastTarget = false;

            // Name text (right of icon)
            var nameText = CreateText(go.transform, "NameText", "", 10, TextAnchor.MiddleLeft, TextColor);
            nameText.rectTransform.anchorMin = new Vector2(0.38f, 0.04f);
            nameText.rectTransform.anchorMax = new Vector2(0.98f, 0.96f);
            nameText.rectTransform.offsetMin = new Vector2(2f, 1f);
            nameText.rectTransform.offsetMax = new Vector2(-2f, -1f);
            nameText.supportRichText = true;

            return btn;
        }

        // ── Modifier name/description ──
        internal static string FormatModName(BossModifierId m) => m switch
        {
            BossModifierId.FogOfWar          => "Fog of War",
            BossModifierId.ArrowSums         => "Arrow Sums",
            BossModifierId.GermanWhispers    => "German Whispers",
            BossModifierId.DutchWhispers     => "Dutch Whispers",
            BossModifierId.ParityLines       => "Parity Lines",
            BossModifierId.RenbanLines       => "Renban Lines",
            BossModifierId.KillerCages       => "Killer Cages",
            BossModifierId.DifferenceKropki  => "Difference Kropki",
            BossModifierId.RatioKropki       => "Ratio Kropki",
            BossModifierId.Palindrome        => "Palindrome",
            BossModifierId.Thermo            => "Thermo",
            BossModifierId.BetweenLines      => "Between Lines",
            BossModifierId.EvenOdd           => "Even/Odd",
            BossModifierId.Nonconsecutive    => "Nonconsecutive",
            BossModifierId.Antiknight        => "Antiknight",
            BossModifierId.Antiking          => "Anti-King",
            BossModifierId.AntiBishop        => "Anti-Bishop",
            BossModifierId.NonconsecDiagonal => "Nonconsec. Diagonal",
            BossModifierId.DistanceGe2       => "Distance \u2265 2",
            BossModifierId.EntropyGlobal     => "Entropy",
            BossModifierId.ModularRegions    => "Modular Regions",
            BossModifierId.ConsecutiveLine   => "Consecutive Line",
            BossModifierId.SlowThermo        => "Slow Thermo",
            BossModifierId.UniqueSetLine     => "Unique Set Line",
            BossModifierId.FullKropki        => "Full Kropki",
            BossModifierId.SumKropki         => "Sum Dot",
            BossModifierId.GreaterLessThan   => "Greater/Less Than",
            BossModifierId.XVPairs           => "XV Pairs",
            BossModifierId.PrimeCells        => "Prime Cells",
            BossModifierId.FortressCells     => "Fortress Cells",
            _                                => m.ToString()
        };

        internal static string GetModDesc(BossModifierId m) => m switch
        {
            BossModifierId.FogOfWar          => "Correct placements reveal hidden cells.",
            BossModifierId.ArrowSums         => "Arrow circles: digits on the arrow sum to the number in the circle.",
            BossModifierId.GermanWhispers    => "Green line: neighbours on the line must differ by 5 or more.",
            BossModifierId.DutchWhispers     => "Teal line: neighbours on the line must differ by 4 or more.",
            BossModifierId.ParityLines       => "Cyan line: adjacent cells must strictly alternate between odd and even.",
            BossModifierId.RenbanLines       => "Pink line: all digits on the line form a consecutive set (any order).",
            BossModifierId.KillerCages       => "Dashed cage: digits sum to the cage label; no repeats inside the cage.",
            BossModifierId.DifferenceKropki  => "White dot between cells: those two digits differ by exactly 1.",
            BossModifierId.RatioKropki       => "Black dot between cells: one digit is exactly double the other.",
            BossModifierId.Palindrome        => "Grey line: digits read the same from either end of the line.",
            BossModifierId.Thermo            => "Orange line with bulb: digits must strictly increase from bulb to tip.",
            BossModifierId.BetweenLines      => "White line: every digit on the line must fall strictly between the two endpoint values.",
            BossModifierId.EvenOdd           => "Blue square = even digit. Orange circle = odd digit.",
            BossModifierId.Nonconsecutive    => "Global: no two orthogonally adjacent cells may contain consecutive digits.",
            BossModifierId.Antiknight        => "Global: no two cells a chess knight's move apart may share the same digit.",
            BossModifierId.Antiking          => "Global: no two cells a king's move apart (including diagonal) may share a digit.",
            BossModifierId.AntiBishop        => "Global: no two cells on the same diagonal may share a digit.",
            BossModifierId.NonconsecDiagonal => "Global: diagonally adjacent cells cannot be consecutive (e.g. 4 cannot touch 3 or 5 diagonally).",
            BossModifierId.DistanceGe2       => "Global: equal digits must be at least 2 cells apart (no king-adjacent equal digits).",
            BossModifierId.EntropyGlobal     => "Every 3 consecutive row/col cells must contain one low (1\u20133), one mid (4\u20136), and one high (7\u20139) digit.",
            BossModifierId.ModularRegions    => "Every box region must contain at least one digit from {1\u20133}, {4\u20136}, and {7\u20139}.",
            BossModifierId.ConsecutiveLine   => "Orange line: adjacent cells on the line must differ by exactly 1 (e.g. 3-4-5).",
            BossModifierId.SlowThermo        => "Purple line with open bulb: digits must increase or stay equal from bulb to tip (e.g. 2-2-3-5).",
            BossModifierId.UniqueSetLine     => "Sky-blue line: no digit may repeat anywhere on the line.",
            BossModifierId.FullKropki        => "All diff-by-1 pairs show a white dot; all 1:2 ratio pairs show a black dot. No dot = neither.",
            BossModifierId.SumKropki         => "Labelled dot between two cells: those cells must sum to that value.",
            BossModifierId.GreaterLessThan   => "Orange chevrons (> / <) between cells indicate which digit must be larger.",
            BossModifierId.XVPairs           => "X between two cells: they sum to 10. V between two cells: they sum to 5.",
            BossModifierId.PrimeCells        => "Gold 'P' cells must contain a prime digit: 2, 3, 5, or 7.",
            BossModifierId.FortressCells     => "Grey shaded cells must be strictly greater than every orthogonally adjacent unshaded cell.",
            _                                => m.ToString()
        };
    }
}
