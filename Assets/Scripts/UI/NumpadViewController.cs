using System;
using System.Collections.Generic;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the numpad and pencil-mode toggle button.
    /// </summary>
    public sealed class NumpadViewController : MonoBehaviour
    {
        private RectTransform _numpadRoot;
        private List<Button> _numpadButtons;
        private bool _pencilMode;
        private Button _pencilModeButton;

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
                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = !inRange  ? InRunUiFactory.NumpadDisabled
                        : completed       ? InRunUiFactory.NumpadCompleted
                        : InRunUiFactory.NumpadActive;
            }
        }

        /// <summary>
        /// Highlights the specified numpad button with a controller-cursor colour.
        /// Pass null to clear the cursor highlight.
        /// </summary>
        public void HighlightControllerCursor(int? digitIndex)
        {
            for (var i = 0; i < _numpadButtons.Count; i++)
            {
                var img = _numpadButtons[i]?.GetComponent<Image>();
                if (img == null) continue;
                var isSelected = digitIndex.HasValue && i == digitIndex.Value;
                var digit = i + 1;
                var btn = _numpadButtons[i];
                var inRange = btn != null && btn.interactable;
                img.color = isSelected
                    ? new Color(0.85f, 0.72f, 0.20f, 1f) // gold controller cursor
                    : InRunUiFactory.NumpadActive;
            }
        }

        public void SetPencilMode(bool on)
        {
            _pencilMode = on;
            if (_pencilModeButton != null)
            {
                var lbl = _pencilModeButton.GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = _pencilMode ? "Mode: PENCIL" : "Mode: SOLVE";
                var img = _pencilModeButton.GetComponent<Image>();
                if (img != null)
                    img.color = _pencilMode ? InRunUiFactory.NumpadPencilActive : InRunUiFactory.BtnColor;
            }
        }

        private void BuildNumpad()
        {
            if (_numpadRoot == null) return;
            _numpadButtons.Clear();
            InRunUiFactory.ClearChildren(_numpadRoot);

            var grid = _numpadRoot.GetComponent<GridLayoutGroup>() ?? _numpadRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(90, 56);
            grid.spacing = new Vector2(8, 8);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var v = 1; v <= 9; v++)
            {
                var go = new GameObject($"Num_{v}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_numpadRoot, false);
                go.GetComponent<Image>().color = InRunUiFactory.NumpadActive;
                var val = v;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    OnNumberPressed?.Invoke(val);
                });
                var t = InRunUiFactory.CreateText(go.transform, "L", v.ToString(), 24, TextAnchor.MiddleCenter, InRunUiFactory.TextColor);
                InRunUiFactory.StretchFill(t.rectTransform);
                _numpadButtons.Add(go.GetComponent<Button>());
            }

            // Pencil mode button
            var pmGo = new GameObject("PencilBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            pmGo.transform.SetParent(_numpadRoot, false);
            pmGo.AddComponent<LayoutElement>().ignoreLayout = true;
            var pmRect = pmGo.GetComponent<RectTransform>();
            pmRect.anchorMin = new Vector2(0.1f, 0.04f);
            pmRect.anchorMax = new Vector2(0.9f, 0.14f);
            pmRect.offsetMin = Vector2.zero;
            pmRect.offsetMax = Vector2.zero;
            pmGo.GetComponent<Image>().color = InRunUiFactory.BtnColor;
            _pencilModeButton = pmGo.GetComponent<Button>();
            _pencilModeButton.onClick.AddListener(() =>
            {
                _pencilMode = !_pencilMode;
                SetPencilMode(_pencilMode);
                OnPencilModeToggled?.Invoke();
            });
            var pmLbl = InRunUiFactory.CreateText(pmGo.transform, "L", "Mode: SOLVE", 13, TextAnchor.MiddleCenter, InRunUiFactory.TextColor);
            InRunUiFactory.StretchFill(pmLbl.rectTransform);
        }
    }
}
