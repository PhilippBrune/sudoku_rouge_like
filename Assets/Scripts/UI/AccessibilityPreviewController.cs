using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: ACCESS-PREVIEW-001] A read-only 4×4 board inside the Accessibility page that shows every setting at once
    /// (0.3.1 playtest: "most items seem to do nothing"): the cells (High Contrast), a green and an orange line (Colorblind
    /// Mode swaps their colours, High Contrast thickens them), two Kropki dots and the lines' letters (Alt Constraint
    /// Symbols), a conflict that shakes (not with Reduce Motion), a caption bar (Status Captions) and the text samples
    /// (Text Size).
    /// </summary>
    public sealed class AccessibilityPreviewController : MonoBehaviour
    {
        // ── Fixed board definition ────────────────────────────────────────────

        private enum CellKind { Given, Entered, Conflict, Pencil }

        // Valid 4×4 solution (2×2 regions):  rows sum 1-4, cols sum 1-4, regions sum 1-4
        private static readonly int[,] Digits =
        {
            { 1, 2, 3, 4 },
            { 3, 4, 1, 3 }, // (1,3): user entered "3" but row already has 3 at (1,0) → conflict
            { 4, 1, 0, 3 }, // (2,2): pencil marks shown (correct digit is 2)
            { 2, 3, 4, 1 },
        };

        private static readonly CellKind[,] Kinds =
        {
            { CellKind.Given,    CellKind.Given,    CellKind.Entered,  CellKind.Given    },
            { CellKind.Entered,  CellKind.Given,    CellKind.Given,    CellKind.Conflict },
            { CellKind.Given,    CellKind.Entered,  CellKind.Pencil,   CellKind.Given    },
            { CellKind.Entered,  CellKind.Entered,  CellKind.Given,    CellKind.Given    },
        };

        // The sample lines: a German Whispers row across the third row, a Dutch Whispers column on the right (the caption
        // bar covers the bottom row).
        private static readonly (LineType Type, int R0, int C0, int R1, int C1)[] Lines =
        {
            (LineType.GermanWhispers, 2, 0, 2, 2),
            (LineType.DutchWhispers,  0, 3, 2, 3),
        };

        private const int Size = 4;
        private const float CellW = 1f / Size, BoardH = 0.68f, CellH = BoardH / Size, BoardTop = 1f, Pad = 0.01f;
        private const float LineWidth = 6f, ShakeEvery = 2.5f, ShakeFor = 0.4f;

        // ── Cell components ──────────────────────────────────────────────────

        private readonly Image[,]  _cellBg      = new Image[Size, Size];
        private readonly Text[,]   _cellText    = new Text[Size, Size];
        private readonly Text[,]   _pencilText  = new Text[Size, Size];
        private readonly RectTransform[] _lines = new RectTransform[Lines.Length];
        private readonly Text[] _lineLetters = new Text[Lines.Length];
        private Text _blackDotLetters, _whiteDotLetters;
        private GameObject _caption;

        private Text _sampleTitle;
        private Text _sampleBody;
        private Text _sampleHint;
        private const int BaseTitleSize = 20;
        private const int BaseBodySize  = 13;
        private const int BaseHintSize  = 13;   // DECK-TEXT-001

        private OptionsController _optCtrl;

        // ── Public API ───────────────────────────────────────────────────────

        private System.Action _accessibilityChangedHandler;

        public void Configure(OptionsController optCtrl)
        {
            Unsubscribe();
            _optCtrl = optCtrl;
            BuildBoard(GetComponent<RectTransform>());
            Refresh(optCtrl.GetOptions().Accessibility);
            UpdateScale(optCtrl.GetOptions().Accessibility.FontScale);
            // Slot switches are handled by the menu builder's managed OnOptionsChanged handler.
            // This handler is removed again on reconfigure/destroy so menu rebuilds do not leak it.
            _accessibilityChangedHandler = () =>
            {
                if (this != null && _optCtrl != null) Refresh(_optCtrl.GetOptions().Accessibility);
            };
            optCtrl.OnAccessibilityChanged += _accessibilityChangedHandler;
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_optCtrl != null && _accessibilityChangedHandler != null)
                _optCtrl.OnAccessibilityChanged -= _accessibilityChangedHandler;
            _accessibilityChangedHandler = null;
        }

        public void UpdateScale(float scale)
        {
            if (_sampleTitle != null)
                _sampleTitle.fontSize = Mathf.RoundToInt(BaseTitleSize * scale);
            if (_sampleBody != null)
                _sampleBody.fontSize = Mathf.RoundToInt(BaseBodySize * scale);
            if (_sampleHint != null)
                _sampleHint.fontSize = Mathf.RoundToInt(BaseHintSize * scale);
        }

        // Reduce Motion: the conflict's digit shakes for 0.4 s every 2.5 s, as a wrong digit does on the board.
        private void Update()
        {
            var digit = _cellText[1, 3];
            if (digit == null) return;
            var t = Time.unscaledTime % ShakeEvery;
            var x = !AnimationHelper.MotionReduced && t < ShakeFor ? Mathf.Sin(t * 60f) * 4f * (1f - t / ShakeFor) : 0f;
            digit.rectTransform.anchoredPosition = new Vector2(x, 0f);
        }

        // ── Board construction ───────────────────────────────────────────────

        private void BuildBoard(RectTransform container)
        {
            // Thin border background
            var borderBg = container.gameObject.GetComponent<Image>();
            if (borderBg == null) borderBg = container.gameObject.AddComponent<Image>();
            borderBg.color = new Color(0.04f, 0.06f, 0.05f, 1f);

            // ── Text scale samples (lower 30% of preview) ────────────────────
            var font = FontAssetService.GetFont();
            _sampleTitle = MakeSampleText(container, "SampleTitle",
                LocalizationService.T("Accessibility.Preview.SampleTitle", "Heading"), BaseTitleSize,
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.16f), TextAnchor.MiddleLeft,
                new Color(0.96f, 0.93f, 0.82f, 1f), font);
            _sampleBody  = MakeSampleText(container, "SampleBody",
                LocalizationService.T("Accessibility.Preview.SampleBody", "Body text example"), BaseBodySize,
                new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.24f), TextAnchor.MiddleLeft,
                new Color(0.80f, 0.78f, 0.68f, 1f), font);
            _sampleHint  = MakeSampleText(container, "SampleHint",
                LocalizationService.T("Accessibility.Preview.SampleHint", "Hint / small caption"), BaseHintSize,
                new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.30f), TextAnchor.MiddleLeft,
                new Color(0.60f, 0.57f, 0.50f, 1f), font);

            // Cells first, then the lines and dots, then the digits on top (as on the board).
            for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
                _cellBg[r, c] = Area(container, $"Cell_{r}_{c}", CellMin(r, c), CellMax(r, c)).gameObject.AddComponent<Image>();

            for (var i = 0; i < Lines.Length; i++)
            {
                var (_, r0, c0, r1, c1) = Lines[i];
                _lines[i] = Area(container, "Line_" + Lines[i].Type, Vector2.Min(Centre(r0, c0), Centre(r1, c1)),
                    Vector2.Max(Centre(r0, c0), Centre(r1, c1)));
                _lines[i].gameObject.AddComponent<Image>().raycastTarget = false;
                _lineLetters[i] = MakeSampleText(container, "LineLetters_" + i, BossService.GetLineTypeAbbreviation(Lines[i].Type), 9,
                    CellMin(r0, c0), CellMax(r0, c0), TextAnchor.LowerLeft, GamePalette.CellNumberGiven, font);
            }
            _blackDotLetters = Dot(container, "BlackDot", 0, 0, GamePalette.OverlayKropkiBlack, "KR", Color.white, font);
            _whiteDotLetters = Dot(container, "WhiteDot", 0, 1, GamePalette.OverlayKropkiWhite, "KC", GamePalette.OverlayKropkiWhiteRim, font);

            for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
            {
                var cell = Area(container, $"Digit_{r}_{c}", CellMin(r, c), CellMax(r, c));
                if (Kinds[r, c] == CellKind.Pencil)
                {
                    var pt = MakeSampleText(cell, "PencilText", "2  4", 9, Vector2.zero, Vector2.one, TextAnchor.UpperLeft,
                        GamePalette.PencilMarkColor, font);
                    pt.rectTransform.offsetMin = new Vector2(3f, 3f);
                    pt.rectTransform.offsetMax = new Vector2(-3f, -3f);
                    _pencilText[r, c] = pt;
                    continue;
                }
                var txt = MakeSampleText(cell, "Digit", Digits[r, c].ToString(), 18, Vector2.zero, Vector2.one,
                    TextAnchor.MiddleCenter, Color.black, font);
                txt.fontStyle = Kinds[r, c] == CellKind.Given ? FontStyle.Bold : FontStyle.Normal;
                _cellText[r, c] = txt;
            }

            // Status Captions: the bar a run shows at the bottom of the screen.
            var bar = Area(container, "CaptionBar", new Vector2(0f, BoardTop - BoardH), new Vector2(1f, BoardTop - BoardH + CellH * 0.62f));
            bar.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            MakeSampleText(bar, "Caption", LocalizationService.T("Accessibility.Preview.Caption", "3 is already in this row."), 13,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), TextAnchor.MiddleLeft, new Color(0.96f, 0.93f, 0.82f, 1f), font);
            _caption = bar.gameObject;
        }

        // A Kropki dot on the edge between (r, c) and (r, c + 1), and its Alt Constraint Symbols letters.
        private static Text Dot(RectTransform parent, string name, int r, int c, Color fill, string letters, Color letterColor, Font font)
        {
            var centre = (Centre(r, c) + Centre(r, c + 1)) * 0.5f;
            var dot = Area(parent, name, centre, centre);
            dot.sizeDelta = new Vector2(22f, 22f);
            var image = dot.gameObject.AddComponent<Image>();
            image.sprite = ScrollUiKit.Disc();
            image.color = fill;
            image.raycastTarget = false;
            dot.gameObject.AddComponent<Outline>().effectColor = GamePalette.OverlayKropkiWhiteRim;
            var text = MakeSampleText(dot, "Letters", letters, 9, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, letterColor, font);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Vector2 CellMin(int r, int c) => new Vector2(c * CellW + Pad, BoardTop - (r + 1) * CellH + Pad);
        private static Vector2 CellMax(int r, int c) => new Vector2((c + 1) * CellW - Pad, BoardTop - r * CellH - Pad);
        private static Vector2 Centre(int r, int c) => new Vector2((c + 0.5f) * CellW, BoardTop - (r + 0.5f) * CellH);

        private static RectTransform Area(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Text MakeSampleText(RectTransform parent, string name, string content,
            int fontSize, Vector2 amin, Vector2 amax, TextAnchor anchor, Color color, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = font; t.text = content; t.fontSize = fontSize;
            t.alignment = anchor; t.color = color;
            t.raycastTarget = false;
            return t;
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        public void Refresh(AccessibilitySettings acc)
        {
            UpdateScale(acc?.FontScale ?? 1f);
            acc ??= new AccessibilitySettings();

            var accessSvc = new AccessibilityService();
            accessSvc.Refresh(acc, new GraphicsSettingsModel());

            for (var i = 0; i < Lines.Length; i++)
            {
                if (_lines[i] == null) continue;
                var width = LineWidth * accessSvc.GetLineWidthMultiplier();
                var horizontal = Lines[i].R0 == Lines[i].R1;
                _lines[i].offsetMin = new Vector2(horizontal ? 0f : -width / 2f, horizontal ? -width / 2f : 0f);
                _lines[i].offsetMax = new Vector2(horizontal ? 0f : width / 2f, horizontal ? width / 2f : 0f);
                var color = BoardViewController.LineColorFor(Lines[i].Type, acc.ColorblindMode);
                _lines[i].GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.85f);
                _lineLetters[i].gameObject.SetActive(acc.AlternativeConstraintSymbols);
            }
            if (_blackDotLetters != null) _blackDotLetters.gameObject.SetActive(acc.AlternativeConstraintSymbols);
            if (_whiteDotLetters != null) _whiteDotLetters.gameObject.SetActive(acc.AlternativeConstraintSymbols);
            if (_caption != null) _caption.SetActive(acc.ScreenReaderEnabled);

            // High-contrast mode: use explicit semantic HC colors so that
            // backgrounds and text are guaranteed to be readable against each other.
            if (acc.HighContrastMode)
            {
                for (var r = 0; r < Size; r++)
                {
                    for (var c = 0; c < Size; c++)
                    {
                        switch (Kinds[r, c])
                        {
                            case CellKind.Given:
                                _cellBg[r, c].color = GamePalette.HcCellGiven;
                                if (_cellText[r, c] != null)
                                    _cellText[r, c].color = GamePalette.HcTextOnLight;
                                break;
                            case CellKind.Entered:
                                _cellBg[r, c].color = GamePalette.HcCellEmpty;
                                if (_cellText[r, c] != null)
                                    _cellText[r, c].color = GamePalette.HcTextOnDark;
                                break;
                            case CellKind.Conflict:
                                _cellBg[r, c].color = GamePalette.HcCellConflict;
                                if (_cellText[r, c] != null)
                                    _cellText[r, c].color = GamePalette.HcTextOnLight;
                                break;
                            case CellKind.Pencil:
                                _cellBg[r, c].color = GamePalette.HcCellEmpty;
                                if (_pencilText[r, c] != null)
                                    _pencilText[r, c].color = GamePalette.HcPencilMark;
                                break;
                        }
                    }
                }
                return;
            }

            // Normal mode: the board's paper colours; an entered digit in blue ink, as on the board.
            var givenBg    = accessSvc.GetCellColor(GamePalette.CellGiven);
            var emptyBg    = accessSvc.GetCellColor(GamePalette.CellEmpty);
            var conflictBg = accessSvc.GetCellColor(GamePalette.CellConflict);
            var givenText  = accessSvc.GetCellColor(GamePalette.CellNumberGiven);
            var enteredText = GamePalette.CellNumberPlaced;

            for (var r = 0; r < Size; r++)
            {
                for (var c = 0; c < Size; c++)
                {
                    switch (Kinds[r, c])
                    {
                        case CellKind.Given:
                            _cellBg[r, c].color   = givenBg;
                            if (_cellText[r, c] != null)
                                _cellText[r, c].color = givenText;
                            break;
                        case CellKind.Entered:
                            _cellBg[r, c].color   = emptyBg;
                            if (_cellText[r, c] != null)
                                _cellText[r, c].color = enteredText;
                            break;
                        case CellKind.Conflict:
                            _cellBg[r, c].color   = conflictBg;
                            if (_cellText[r, c] != null)
                                _cellText[r, c].color = Color.white;
                            break;
                        case CellKind.Pencil:
                            _cellBg[r, c].color   = emptyBg;
                            if (_pencilText[r, c] != null)
                                _pencilText[r, c].color = GamePalette.PencilMarkColor;
                            break;
                    }
                }
            }
        }
    }
}
