using System;
using System.Collections.Generic;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the numpad and pencil-mode toggle button.
    /// [REQ: UI-STYLE-002] The digits are wooden tiles; the controller cursor is a gold ring around a tile (it no longer
    /// recolours the tile, which used to hide the disabled and completed states); Solve | Pencil is a two-part switch.
    /// </summary>
    public sealed class NumpadViewController : MonoBehaviour
    {
        private RectTransform _numpadRoot;
        private List<Button> _numpadButtons;
        private bool _pencilMode;
        private Button _pencilModeButton;
        private Image _solveHalf;
        private Image _pencilHalf;
        private Text _solveLabel;
        private Text _pencilLabel;

        private static readonly Color SegmentOn = new Color(0.80f, 0.64f, 0.30f, 1f);
        private static readonly Color SegmentOff = new Color(0.10f, 0.08f, 0.05f, 0.55f);
        private static readonly Color SegmentTextOn = new Color(0.16f, 0.09f, 0.03f, 1f);
        private static readonly Color SegmentTextOff = new Color(0.95f, 0.91f, 0.82f, 1f);

        public Action<int> OnNumberPressed;
        public Action OnPencilModeToggled;

        public bool PencilMode => _pencilMode;

        public void Configure(RectTransform numpadRoot)
        {
            _numpadRoot = numpadRoot;
            _numpadButtons = new List<Button>();
            BuildNumpad();
        }

        public void UpdateButtonStates(int boardSize, SudokuBoard board)
        {
            // Count how many of each digit are placed on the board
            var counts = new int[boardSize + 1];
            if (board != null)
                for (var r = 0; r < boardSize; r++)
                for (var c = 0; c < boardSize; c++)
                {
                    var v = board.Cells[r, c];
                    if (v >= 1 && v <= boardSize) counts[v]++;
                }

            for (var i = 0; i < _numpadButtons.Count; i++)
            {
                var btn = _numpadButtons[i];
                if (btn == null) continue;
                var digit = i + 1;
                var inRange = digit <= boardSize;
                var completed = inRange && board != null && counts[digit] >= boardSize;
                btn.interactable = inRange && !completed;
                // Out of range: the tile fades (Button disabled tint); completed: it darkens like worn wood.
                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = completed ? new Color(0.70f, 0.66f, 0.60f, 1f) : Color.white;
                var label = btn.transform.Find("L")?.GetComponent<Text>();
                if (label != null)
                    label.color = inRange && !completed ? ScrollUiKit.WoodTileText : new Color(0.20f, 0.11f, 0.05f, 0.35f);
            }
        }

        /// <summary>
        /// Shows the controller cursor as a gold ring around the tile at <paramref name="digitIndex"/>.
        /// Pass null to clear the cursor.
        /// </summary>
        public void HighlightControllerCursor(int? digitIndex)
        {
            for (var i = 0; i < _numpadButtons.Count; i++)
            {
                var ring = _numpadButtons[i] != null ? _numpadButtons[i].transform.Find("CursorRing") : null;
                if (ring != null) ring.gameObject.SetActive(digitIndex.HasValue && i == digitIndex.Value);
            }
        }

        public void SetPencilMode(bool on)
        {
            _pencilMode = on;
            if (_solveHalf != null) _solveHalf.color = on ? SegmentOff : SegmentOn;
            if (_pencilHalf != null) _pencilHalf.color = on ? SegmentOn : SegmentOff;
            if (_solveLabel != null) _solveLabel.color = on ? SegmentTextOff : SegmentTextOn;
            if (_pencilLabel != null) _pencilLabel.color = on ? SegmentTextOn : SegmentTextOff;
        }

        private void BuildNumpad()
        {
            if (_numpadRoot == null) return;
            _numpadButtons.Clear();
            InRunUiFactory.ClearChildren(_numpadRoot);

            var grid = _numpadRoot.GetComponent<GridLayoutGroup>() ?? _numpadRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(110, 96);
            grid.spacing = new Vector2(14, 14);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var v = 1; v <= 9; v++)
            {
                var go = new GameObject($"Num_{v}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_numpadRoot, false);
                var button = go.GetComponent<Button>();
                ScrollUiKit.MakeWoodTile(button, go.GetComponent<Image>());
                var val = v;
                button.onClick.AddListener(() =>
                {
                    OnNumberPressed?.Invoke(val);
                });
                var t = InRunUiFactory.CreateText(go.transform, "L", v.ToString(), 44, TextAnchor.MiddleCenter, ScrollUiKit.WoodTileText);
                InRunUiFactory.StretchFill(t.rectTransform);
                t.raycastTarget = false;
                var ring = ScrollUiKit.Area(go.transform, "CursorRing", Vector2.zero, Vector2.one);
                ring.offsetMin = new Vector2(-5f, -5f);
                ring.offsetMax = new Vector2(5f, 5f);
                ScrollUiKit.InkFrame(ring, new Color(1f, 0.84f, 0.35f, 1f), 3f);
                ring.gameObject.SetActive(false);
                _numpadButtons.Add(button);
            }

            // Solve | Pencil switch under the tiles (outside the grid layout)
            var pmGo = new GameObject("PencilBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            pmGo.transform.SetParent(_numpadRoot, false);
            pmGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var pmRect = pmGo.GetComponent<RectTransform>();
            pmRect.anchorMin = new Vector2(0f, -0.21f);
            pmRect.anchorMax = new Vector2(1f, -0.06f);
            pmRect.offsetMin = Vector2.zero;
            pmRect.offsetMax = Vector2.zero;
            var rim = pmGo.GetComponent<Image>();
            rim.sprite = ScrollUiKit.Rounded(24f);
            rim.type = Image.Type.Sliced;
            rim.color = ScrollUiKit.Gold;
            _pencilModeButton = pmGo.GetComponent<Button>();
            _pencilModeButton.targetGraphic = rim;
            _pencilModeButton.onClick.AddListener(() =>
            {
                _pencilMode = !_pencilMode;
                SetPencilMode(_pencilMode);
                OnPencilModeToggled?.Invoke();
            });
            _solveHalf = ScrollUiKit.Fill(pmGo.transform, "SolveHalf", Vector2.zero, new Vector2(0.5f, 1f), SegmentOn, ScrollUiKit.Rounded(22f));
            _solveHalf.rectTransform.offsetMin = new Vector2(2f, 2f);
            _solveHalf.rectTransform.offsetMax = new Vector2(-1f, -2f);
            _pencilHalf = ScrollUiKit.Fill(pmGo.transform, "PencilHalf", new Vector2(0.5f, 0f), Vector2.one, SegmentOff, ScrollUiKit.Rounded(22f));
            _pencilHalf.rectTransform.offsetMin = new Vector2(1f, 2f);
            _pencilHalf.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _solveLabel = ScrollUiKit.Label(pmGo.transform, "L", ModeLabel(false), 20, TextAnchor.MiddleCenter, SegmentTextOn,
                Vector2.zero, new Vector2(0.5f, 1f), 12);
            _pencilLabel = ScrollUiKit.Label(pmGo.transform, "PencilLabel", ModeLabel(true), 20, TextAnchor.MiddleCenter,
                SegmentTextOff, new Vector2(0.5f, 0f), Vector2.one, 12);
            SetPencilMode(false);
        }

        // GAP-019: localized numpad mode labels ("Solve" | "Pencil").
        private static string ModeLabel(bool pencil) => pencil
            ? LocalizationService.T("InRun.Numpad.Pencil", "Pencil")
            : LocalizationService.T("InRun.Numpad.Solve", "Solve");
    }
}
