using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: ITEM-SCROLL-002] The Pattern Scroll as a list on a paper slip (the owner's pick B after the 0.4.2 playtest, RM-231):
    // one row per rule piece with its colour, "3 empty of 4" or "solved"; the chosen row shows its cells on the board in gold.
    // A row is chosen with a click, the mouse wheel, W/S, the arrows or the D-pad; Solve, Enter or A writes it (a click never
    // does: the first row is chosen when the slip opens). The digits are not previewed: a player could read every piece's
    // solution and cancel, which returns the scroll (GAP-117).
    public sealed partial class InRunController
    {
        private const float PatternRowHeight = 48f;
        private RectTransform _patternScrollContent;
        private readonly List<Button> _patternScrollRows = new List<Button>();
        private Button _patternScrollSolve;

        private void ShowPatternScrollSlip()
        {
            ClosePatternScrollSlip();
            if (_sudokuPanel == null) return;
            var run = _map?.Run;
            var ov = run?.CurrentOverlay;
            var board = run?.CurrentBoard;
            if (ov == null || board == null) return;

            // Built once per use, so the indices stay stable across the picks of a Rare or Epic scroll.
            if (_patternScrollMechanics == null)
            {
                _patternScrollMechanics = BuildPatternScrollMechanics(ov, run.CurrentLevelConfig?.ActiveModifiers, board.Size);
                _patternScrollSolvedIds = new HashSet<int>();
                _patternScrollStagedIndex = -1;
            }

            var slip = ScrollUiKit.Fill(_sudokuPanel.transform, "PatternScrollOverlay", new Vector2(0.030f, 0.007f), new Vector2(0.266f, 0.560f),
                ScrollUiKit.Paper);
            slip.raycastTarget = true;
            var shadow = slip.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -10f);
            _patternScrollOverlay = slip.gameObject;
            var t = slip.transform;
            ScrollUiKit.Fill(t, "Band", new Vector2(0f, 0.985f), Vector2.one, ScrollUiKit.Vermilion);
            var total = _patternScrollSolvedIds.Count + _patternScrollChargesLeft;
            ScrollUiKit.Label(t, "Header", LocalizationService.Format("InRun.PatternScroll.Header", "Pattern Scroll · {0} of {1} left",
                    _patternScrollChargesLeft, total).ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.96f), 10);

            var cancel = ScrollUiKit.PillButton(t, "BtnCancelScroll", new Vector2(0.51f, 0.04f), new Vector2(0.95f, 0.15f),
                T("InRun.PatternScroll.Cancel"), 20, ScrollUiKit.Wood, ScrollUiKit.PaperText, 20f);
            cancel.onClick.AddListener(() => CancelPendingItemInteraction());

            if (_patternScrollMechanics.Count == 0)
            {
                ScrollUiKit.Label(t, "None", T("InRun.PatternScroll.Empty"), 20, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                    new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.85f), 12);
                return;
            }

            var viewport = ScrollUiKit.Area(t, "Rows", new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.89f));
            viewport.gameObject.AddComponent<RectMask2D>();
            _patternScrollContent = ScrollUiKit.Area(viewport, "Content", new Vector2(0f, 1f), Vector2.one);
            _patternScrollContent.pivot = new Vector2(0.5f, 1f);
            _patternScrollContent.sizeDelta = new Vector2(0f, PatternRowHeight * _patternScrollMechanics.Count);
            for (var i = 0; i < _patternScrollMechanics.Count; i++)
                _patternScrollRows.Add(BuildPatternRow(i, board));

            ScrollUiKit.Label(t, "Hint", T("InRun.PatternScroll.Hint"), 16,
                TextAnchor.UpperLeft, ScrollUiKit.InkSoft, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.29f), 10);
            _patternScrollSolve = ScrollUiKit.PillButton(t, "BtnConfirmScroll", new Vector2(0.05f, 0.04f), new Vector2(0.47f, 0.15f),
                T("InRun.PatternScroll.Solve"), 20, ScrollUiKit.Vermilion, Color.white, 20f);
            _patternScrollSolve.onClick.AddListener(PatternScrollConfirmSelection);

            var first = _patternScrollStagedIndex;
            for (var i = 0; first < 0 && i < _patternScrollMechanics.Count; i++)
                if (!IsPieceSolved(i)) first = i;
            if (first >= 0) StagePatternPiece(first);
            RefreshPatternRows();
        }

        private Button BuildPatternRow(int index, SudokuBoard board)
        {
            var piece = _patternScrollMechanics[index];
            var solved = IsPieceSolved(index);
            var row = ScrollUiKit.Fill(_patternScrollContent, $"MechBtn_{index}", new Vector2(0f, 1f), Vector2.one, new Color(1f, 1f, 1f, 0f));
            row.raycastTarget = true;
            row.rectTransform.pivot = new Vector2(0.5f, 1f);
            row.rectTransform.anchoredPosition = new Vector2(0f, -index * PatternRowHeight);
            row.rectTransform.sizeDelta = new Vector2(0f, PatternRowHeight - 2f);
            ScrollUiKit.Fill(row.transform, "Bar", Vector2.zero, new Vector2(0.015f, 1f), ScrollUiKit.Vermilion).gameObject.SetActive(false);
            ScrollUiKit.Fill(row.transform, "Swatch", new Vector2(0.04f, 0.40f), new Vector2(0.11f, 0.60f),
                BoardViewController.TryGetLegendColor(piece.modifier, out var colour) ? colour : new Color(0.55f, 0.55f, 0.55f, 0.9f),
                ScrollUiKit.Rounded(5f));
            var ink = solved ? new Color(ScrollUiKit.InkSoft.r, ScrollUiKit.InkSoft.g, ScrollUiKit.InkSoft.b, 0.45f) : ScrollUiKit.Ink;
            ScrollUiKit.Label(row.transform, "Name", LocalizationService.Format("InRun.PatternScroll.Row", "{0}. {1}", index + 1,
                BossService.GetModifierName(piece.modifier)), 19, TextAnchor.MiddleLeft, ink, new Vector2(0.14f, 0f), new Vector2(0.70f, 1f), 11);
            var empty = 0;
            foreach (var c in piece.cells) if (board.Cells[c.Row, c.Col] == 0) empty++;
            ScrollUiKit.Label(row.transform, "Count", solved ? T("InRun.PatternScroll.Solved")
                    : LocalizationService.Format("InRun.PatternScroll.EmptyOf", "{0} empty of {1}", empty, piece.cells.Count), 15,
                TextAnchor.MiddleRight, solved ? ink : ScrollUiKit.InkSoft, new Vector2(0.68f, 0f), new Vector2(0.97f, 1f), 9);
            if (solved) ScrollUiKit.Line(row.transform, "Strike", new Vector2(0.14f, 0.5f), new Vector2(0.97f, 0.5f), ink, 1.5f);

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.transition = Selectable.Transition.None;
            button.interactable = !solved;
            var idx = index;
            button.onClick.AddListener(() => StagePatternPiece(idx));
            return button;
        }

        private bool IsPieceSolved(int index) => _patternScrollSolvedIds != null && _patternScrollSolvedIds.Contains(index);

        private void StagePatternPiece(int index)
        {
            if (index < 0 || index >= _patternScrollMechanics.Count || IsPieceSolved(index)) return;
            _patternScrollStagedIndex = index;
            var cells = new List<(int, int)>();
            foreach (var c in _patternScrollMechanics[index].cells) cells.Add((c.Row, c.Col));
            _boardView?.PreviewPiece(cells);
            RenderBoard();
            RefreshPatternRows();
            KeepPatternRowVisible(index);
        }

        private void RefreshPatternRows()
        {
            for (var i = 0; i < _patternScrollRows.Count; i++)
            {
                var row = _patternScrollRows[i];
                var staged = i == _patternScrollStagedIndex;
                ((Image)row.targetGraphic).color = staged ? new Color(0.91f, 0.77f, 0.42f, 0.38f) : new Color(1f, 1f, 1f, 0f);
                row.transform.Find("Bar").gameObject.SetActive(staged);
            }
            if (_patternScrollSolve != null) _patternScrollSolve.interactable = _patternScrollStagedIndex >= 0;
        }

        // The rows scroll inside the slip so the chosen one stays in view.
        private void KeepPatternRowVisible(int index)
        {
            if (_patternScrollContent == null) return;
            var view = ((RectTransform)_patternScrollContent.parent).rect.height;
            var top = index * PatternRowHeight;
            var y = _patternScrollContent.anchoredPosition.y;
            if (top < y) y = top;
            else if (top + PatternRowHeight > y + view) y = top + PatternRowHeight - view;
            _patternScrollContent.anchoredPosition = new Vector2(0f, Mathf.Max(0f, y));
        }

        // While the slip is open its keys are the only ones the puzzle hears (nothing is typed into the board behind it).
        private void HandlePatternScrollKeys()
        {
            _inputRemap.Tick();
            EventSystem.current?.SetSelectedGameObject(null);   // a bag slot that was just pressed must not take Enter too
            if (PadButtons.Pressed(KeyCode.JoystickButton7)) { ToggleOptionsPanel(); return; }
            if (PadButtons.Pressed(KeyCode.JoystickButton1)) { CancelPendingItemInteraction(); return; }
            if (PadButtons.Pressed(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return)) { PatternScrollConfirmSelection(); return; }
            var down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || _inputRemap.WasGamepadActionPressed(InputAction.MoveDown)
                       || Input.mouseScrollDelta.y < 0f;
            var up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || _inputRemap.WasGamepadActionPressed(InputAction.MoveUp)
                     || Input.mouseScrollDelta.y > 0f;
            if (down != up) StepPatternRow(down ? 1 : -1);
        }

        private void StepPatternRow(int delta)
        {
            if (_patternScrollMechanics == null) return;
            var i = _patternScrollStagedIndex + delta;
            while (i >= 0 && i < _patternScrollMechanics.Count && IsPieceSolved(i)) i += delta;
            StagePatternPiece(i);
        }

        private void ClosePatternScrollSlip()
        {
            if (_patternScrollOverlay == null) return;
            ScrollUiKit.DestroyNow(_patternScrollOverlay);
            _patternScrollOverlay = null;
            _patternScrollRows.Clear();
            _patternScrollContent = null;
            _patternScrollSolve = null;
            _boardView?.PreviewPiece(null);
            RenderBoard();
        }

        // [REQ: ITEM-SCROLL-001] One entry per piece a rule of this puzzle drew (lines, cages, arrows, dots, marked cells, cell
        // pairs, outside clues, quadruples), named by the rule in the player's language: "German Whispers #1 · 4 cells".
        public static List<(BossModifierId modifier, string label, List<CellCoord> cells)> BuildPatternScrollMechanics(
            ModifierOverlayData ov, IReadOnlyList<BossModifierId> modifiers, int size)
        {
            var list = new List<(BossModifierId, string, List<CellCoord>)>();
            if (modifiers == null) return list;
            var listed = new HashSet<string>();   // a dot two rules share (Difference and Full White Kropki) is listed once
            foreach (var modifier in modifiers)
            {
                var number = 0;
                foreach (var piece in ModifierPresence.Pieces(modifier, ov, size))
                {
                    if (!listed.Add(string.Join(";", piece))) continue;
                    list.Add((modifier, LocalizationService.Format("InRun.PatternScroll.Entry", "{0} #{1} · {2} cells",
                        BossService.GetModifierName(modifier), ++number, piece.Count), piece));
                }
            }
            return list;
        }
    }
}
