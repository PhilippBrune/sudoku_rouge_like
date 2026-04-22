using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Builds a read-only 4×4 board snapshot inside the Accessibility panel.
    /// Refreshes cell colours whenever an accessibility toggle changes so the
    /// player can immediately see the visual effect before entering a run.
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

        private const int Size = 4;

        // ── Cell components ──────────────────────────────────────────────────

        private readonly Image[,]  _cellBg      = new Image[Size, Size];
        private readonly Text[,]   _cellText    = new Text[Size, Size];
        private readonly Text[,]   _pencilText  = new Text[Size, Size];

        private OptionsController _optCtrl;

        // ── Public API ───────────────────────────────────────────────────────

        public void Configure(OptionsController optCtrl)
        {
            _optCtrl = optCtrl;
            BuildBoard(GetComponent<RectTransform>());
            Refresh(optCtrl.GetOptions().Accessibility);
            optCtrl.OnAccessibilityChanged += () => Refresh(_optCtrl.GetOptions().Accessibility);
            optCtrl.OnSlotChanged          += () => Refresh(_optCtrl.GetOptions().Accessibility);
        }

        // ── Board construction ───────────────────────────────────────────────

        private void BuildBoard(RectTransform container)
        {
            // Thin border background
            var borderBg = container.gameObject.GetComponent<Image>();
            if (borderBg == null) borderBg = container.gameObject.AddComponent<Image>();
            borderBg.color = new Color(0.04f, 0.06f, 0.05f, 1f);

            const float cellW = 1f / Size;
            const float cellH = 1f / Size;
            const float pad   = 0.01f;

            for (var r = 0; r < Size; r++)
            {
                for (var c = 0; c < Size; c++)
                {
                    float xMin = c * cellW;
                    float yMin = 1f - (r + 1) * cellH;

                    // ── Cell background ──────────────────────────────────────
                    var bgGo = new GameObject($"Cell_{r}_{c}",
                        typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(container, false);
                    var bgRt = bgGo.GetComponent<RectTransform>();
                    bgRt.anchorMin = new Vector2(xMin + pad,      yMin + pad);
                    bgRt.anchorMax = new Vector2(xMin + cellW - pad, yMin + cellH - pad);
                    bgRt.offsetMin = Vector2.zero;
                    bgRt.offsetMax = Vector2.zero;
                    _cellBg[r, c] = bgGo.GetComponent<Image>();

                    // ── Cell content ─────────────────────────────────────────
                    if (Kinds[r, c] == CellKind.Pencil)
                    {
                        var ptGo = new GameObject("PencilText",
                            typeof(RectTransform), typeof(Text));
                        ptGo.transform.SetParent(bgGo.transform, false);
                        var ptRt = ptGo.GetComponent<RectTransform>();
                        ptRt.anchorMin = Vector2.zero;
                        ptRt.anchorMax = Vector2.one;
                        ptRt.offsetMin = new Vector2(3f, 3f);
                        ptRt.offsetMax = new Vector2(-3f, -3f);
                        var pt = ptGo.GetComponent<Text>();
                        pt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        pt.fontSize  = 9;
                        pt.alignment = TextAnchor.UpperLeft;
                        pt.text      = "2  4";
                        _pencilText[r, c] = pt;
                    }
                    else
                    {
                        int digit = Digits[r, c];
                        var txtGo = new GameObject("Digit",
                            typeof(RectTransform), typeof(Text));
                        txtGo.transform.SetParent(bgGo.transform, false);
                        var txtRt = txtGo.GetComponent<RectTransform>();
                        txtRt.anchorMin = Vector2.zero;
                        txtRt.anchorMax = Vector2.one;
                        txtRt.offsetMin = Vector2.zero;
                        txtRt.offsetMax = Vector2.zero;
                        var txt = txtGo.GetComponent<Text>();
                        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        txt.fontSize  = 18;
                        txt.alignment = TextAnchor.MiddleCenter;
                        txt.fontStyle = Kinds[r, c] == CellKind.Given
                            ? FontStyle.Bold : FontStyle.Normal;
                        txt.text = digit > 0 ? digit.ToString() : "";
                        _cellText[r, c] = txt;
                    }
                }
            }
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        public void Refresh(AccessibilitySettings acc)
        {
            var accessSvc = new AccessibilityService();
            accessSvc.Refresh(acc, new GraphicsSettingsModel());

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

            // Normal / colorblind modes: route through AccessibilityService as before.
            var givenBg    = accessSvc.GetCellColor(GamePalette.CellGiven);
            var emptyBg    = accessSvc.GetCellColor(GamePalette.CellEmpty);
            var conflictBg = accessSvc.GetCellColor(GamePalette.CellConflict);
            var givenText  = accessSvc.GetCellColor(GamePalette.CellNumberGiven);
            var enteredText = new Color(0.96f, 0.93f, 0.82f, 1f);

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
