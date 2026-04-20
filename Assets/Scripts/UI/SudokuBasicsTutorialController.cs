using System.Collections.Generic;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Structured 15-step tutorial that teaches Sudoku fundamentals (rules + pencil mode)
    /// through visual board highlights, explanatory text, and enforced interactive steps.
    ///
    /// Step types:
    ///   Explanation        — highlight area + text; player clicks "Next →"
    ///   Interaction        — player must place the correct final number to advance
    ///   PencilInteraction  — player must add the required pencil marks to advance
    ///
    /// Dependencies (set by InRunController before StartTutorial):
    ///   GetBoard           — provides the live SudokuBoard
    ///   SetHighlights      — propagates highlight map to BoardViewController (also triggers render)
    /// </summary>
    // [REQ: TUTO-BASICS-STEP-001] Named steps with highlighted overlays + explanatory text
    // [REQ: TUTO-BASICS-STEP-002] Player advances via "Next" button or by performing the required action
    // [REQ: TUTO-BASICS-STEP-006] Step 6 blocks input until player places correct digit in target cell
    public sealed class SudokuBasicsTutorialController : MonoBehaviour
    {
        // ── Step definition ──────────────────────────────────────────────────

        private struct StepDef
        {
            public string Title;
            public string Body;
            /// <summary>null = clear all highlights.</summary>
            public Dictionary<(int, int), TutorialCellHighlight> Highlights;

            // Normal interaction (place final number)
            /// <summary>True = Interaction step; blocks Next until player places CorrectValue.</summary>
            public bool RequiresInput;

            // Pencil interaction (add pencil marks)
            /// <summary>True = PencilInteraction step; blocks Next until all RequiredMarkValues are marked.</summary>
            public bool RequiresPencilMarks;
            /// <summary>Set of pencil mark values the player must add to the target cell.</summary>
            public HashSet<int> RequiredMarkValues;

            /// <summary>Target cell for both Interaction and PencilInteraction steps.</summary>
            public int TargetRow, TargetCol;
            /// <summary>The final value for a normal Interaction step.</summary>
            public int CorrectValue;

            /// <summary>
            /// Optional action executed when this step is entered.
            /// Used to modify board state (e.g. remove a wrong pencil mark) before the step renders.
            /// </summary>
            public System.Action<SudokuBoard> OnEnter;
        }

        // ── State ─────────────────────────────────────────────────────────────

        private List<StepDef> _steps;
        private int _stepIndex = -1; // -1 = not started

        // ── UI refs ───────────────────────────────────────────────────────────

        private GameObject _panel;
        private Text       _titleLabel;
        private Text       _bodyLabel;
        private Text       _hintLabel;
        private Button     _nextBtn;
        private Button     _finishBtn;
        private Image[]    _dotImages;

        // ── Dot colours ───────────────────────────────────────────────────────

        private static readonly Color DotFuture    = new Color(0.30f, 0.30f, 0.30f, 0.70f);
        private static readonly Color DotCompleted = new Color(0.20f, 0.75f, 0.60f, 1.00f);
        private static readonly Color DotCurrent   = new Color(0.95f, 0.80f, 0.15f, 1.00f);

        // ── Dependencies (injected before StartTutorial) ─────────────────────

        /// <summary>Returns the live SudokuBoard.</summary>
        public System.Func<SudokuBoard> GetBoard;

        /// <summary>Pushes a highlight map to BoardViewController (and triggers re-render); pass null to clear.</summary>
        public System.Action<Dictionary<(int, int), TutorialCellHighlight>> SetHighlights;

        // ── Callbacks ────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the final Finish button is pressed.
        /// InRunController uses this to release tutorial control (not to quit the game).
        /// </summary>
        public System.Action OnFinished;

        /// <summary>
        /// Fired when a PencilInteraction step becomes active.
        /// InRunController uses this to auto-enable pencil mode on the numpad.
        /// </summary>
        public System.Action OnPencilStepEntered;

        // ── Public API ────────────────────────────────────────────────────────

        public bool IsActive => _stepIndex >= 0 && _steps != null && _stepIndex < _steps.Count;

        /// <summary>True while the current step is a pencil-interaction step.</summary>
        public bool IsOnPencilStep => IsActive && _steps[_stepIndex].RequiresPencilMarks;

        /// <summary>The tutorial overlay panel — used by InRunController to route gamepad input.</summary>
        public GameObject PanelRoot => _panel;

        /// <summary>
        /// Builds the UI overlay and starts at step 0.
        /// Call AFTER the board is loaded and the sudoku panel is visible.
        /// </summary>
        public void StartTutorial(Transform parentTransform)
        {
            var board = GetBoard?.Invoke();
            _steps = BuildSteps(board);
            BuildUI(parentTransform);
            _stepIndex = -1;
            AdvanceStep();
        }

        /// <summary>
        /// Called by InRunController when the player places a final number.
        /// Only acts during Interaction steps for the correct cell.
        /// </summary>
        public void OnPlayerPlacedNumber(int row, int col, int value, bool isCorrect)
        {
            if (_stepIndex < 0 || _steps == null || _stepIndex >= _steps.Count) return;
            var step = _steps[_stepIndex];
            if (!step.RequiresInput) return;
            if (row != step.TargetRow || col != step.TargetCol) return;

            if (isCorrect)
            {
                if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
                AdvanceStep();
            }
            else
            {
                if (_hintLabel != null)
                {
                    _hintLabel.text = BuildConstraintHint(row, col, value, step.CorrectValue);
                    _hintLabel.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Called by InRunController after a pencil mark is toggled (add or remove).
        /// Only acts during PencilInteraction steps for the correct cell and required values.
        /// Reads current board state so toggles (add then remove) are handled correctly.
        /// </summary>
        public void OnPlayerAddedPencilMark(int row, int col, int value)
        {
            if (_stepIndex < 0 || _steps == null || _stepIndex >= _steps.Count) return;
            var step = _steps[_stepIndex];
            if (!step.RequiresPencilMarks) return;
            if (row != step.TargetRow || col != step.TargetCol) return;
            if (step.RequiredMarkValues == null || !step.RequiredMarkValues.Contains(value)) return;

            // Read actual board state — handles the case where the player toggles a mark back off
            var board = GetBoard?.Invoke();
            if (board == null) return;
            var marksOnBoard = board.GetPencilMarks(row, col);

            var allPresent = true;
            var presentCount = 0;
            foreach (var v in step.RequiredMarkValues)
            {
                if (marksOnBoard.Contains(v)) presentCount++;
                else allPresent = false;
            }

            if (allPresent)
            {
                if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
                AdvanceStep();
            }
            else
            {
                var remaining = step.RequiredMarkValues.Count - presentCount;
                if (_hintLabel != null)
                {
                    _hintLabel.text = remaining == 1
                        ? "Good! Now add the other candidate mark."
                        : $"Good! {remaining} more marks to add.";
                    _hintLabel.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Returns true when input to (row, col) should be blocked.
        /// — Blocks ALL input during pure Explanation steps.
        /// — During Interaction or PencilInteraction: only the target cell is unblocked.
        /// </summary>
        public bool IsInputLockedToCell(int row, int col)
        {
            if (!IsActive) return false;
            var step = _steps[_stepIndex];
            if (!step.RequiresInput && !step.RequiresPencilMarks) return true; // explanation: no input
            return row != step.TargetRow || col != step.TargetCol;
        }

        // ── Step machine ─────────────────────────────────────────────────────

        private void AdvanceStep()
        {
            _stepIndex++;

            if (_steps == null || _stepIndex >= _steps.Count)
            {
                Finish();
                return;
            }

            var step = _steps[_stepIndex];

            // Apply board modifications BEFORE rendering (OnEnter may remove pencil marks, etc.)
            step.OnEnter?.Invoke(GetBoard?.Invoke());

            // Propagate highlights — this also triggers a board re-render via InRunController's callback
            SetHighlights?.Invoke(step.Highlights);

            // Update text
            if (_titleLabel != null) _titleLabel.text = step.Title;
            if (_bodyLabel  != null) _bodyLabel.text  = step.Body;
            if (_hintLabel  != null) _hintLabel.gameObject.SetActive(false);

            // Button visibility
            var isInteractStep = step.RequiresInput || step.RequiresPencilMarks;
            var isLast         = _stepIndex == _steps.Count - 1;
            if (_nextBtn   != null) _nextBtn.gameObject.SetActive(!isInteractStep && !isLast);
            if (_finishBtn != null) _finishBtn.gameObject.SetActive(isLast && !isInteractStep);

            // Set controller focus to the active navigation button (non-interaction steps only)
            var focusBtn = (!isInteractStep && !isLast) ? _nextBtn
                         : (isLast && !isInteractStep)  ? _finishBtn
                         : null;
            if (focusBtn != null)
                UnityEngine.EventSystems.EventSystem.current
                    ?.SetSelectedGameObject(focusBtn.gameObject);

            // Notify InRunController to enable pencil mode when a pencil step starts
            if (step.RequiresPencilMarks)
                OnPencilStepEntered?.Invoke();

            RefreshDots();
        }

        private void Finish()
        {
            _stepIndex = _steps?.Count ?? 0;
            SetHighlights?.Invoke(null);
            if (_panel != null) _panel.SetActive(false);
            OnFinished?.Invoke();
        }

        // ── Step builder ─────────────────────────────────────────────────────

        private static List<StepDef> BuildSteps(SudokuBoard board)
        {
            var size  = board?.Size ?? 6;
            var steps = new List<StepDef>();

            // ── 0: The Grid ───────────────────────────────────────────────────
            steps.Add(new StepDef
            {
                Title = "Welcome to Sudoku",
                Body  = "Fill every empty cell using the numbers 1 to " + size + ".\n" +
                        "No number can appear twice in any row, column, or 2\u00d73 box."
            });

            // ── 1: Row Rule ───────────────────────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "The Row Rule",
                Body       = "Every ROW must contain each number exactly once.\n" +
                             "The highlighted row has some given numbers and some empty cells to fill.",
                Highlights = MakeRowHL(0, size)
            });

            // ── 2: Column Rule ────────────────────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "The Column Rule",
                Body       = "Every COLUMN must also use each number exactly once.\n" +
                             "The highlighted column has some numbers given and some still missing.",
                Highlights = MakeColHL(1, size)
            });

            // ── 3: Box Rule ───────────────────────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "The Box Rule",
                Body       = "Every 2\u00d73 BOX must contain all six numbers as well.\n" +
                             "The highlighted box has 1, 3, 4, 5 \u2014 it still needs 2 and 6.",
                Highlights = MakeBoxHL(0, 3, 2, 3)
            });

            // ── 4: Row elimination analysis ───────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "Finding the Answer",
                Body       = "We\u2019ll solve the highlighted cell.\n" +
                             "Row 3 already has 5, 2, 3, 4 \u2014 so only 1 or 6 could go here.",
                Highlights = MakeRowHL(3, size, target: (3, 1))
            });

            // ── 5: Column elimination analysis ────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "Narrow It Down",
                Body       = "Column 1 already has 5, 3, 4, 1.\n" +
                             "That eliminates 1 \u2014 and the row already ruled out 2, 3, 4.\n" +
                             "Combining both, only ONE number is left...",
                Highlights = MakeColHL(1, size, target: (3, 1))
            });

            // ── 6: Conclusion ─────────────────────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "Only One Answer!",
                Body       = "Row 3 rules out 2, 3, 4, 5.\n" +
                             "Column 1 rules out 1, 3, 4, 5.\n" +
                             "The only number that satisfies both rules is: 6",
                Highlights = MakeRowColHL(3, 1, size, target: (3, 1))
            });

            // ── 7: Interaction — place 6 at (3,1) ────────────────────────────
            steps.Add(new StepDef
            {
                Title         = "Your Turn!",
                Body          = "Tap the highlighted cell, then press 6 on the numpad.",
                Highlights    = MakeSingleHL(3, 1, TutorialCellHighlight.Target),
                RequiresInput = true,
                TargetRow     = 3,
                TargetCol     = 1,
                CorrectValue  = 6
            });

            // ── 8: Post-placement explanation ─────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "Well Done!",
                Body       = "Row 3 now has 5, 6, 2, 3, 4.\n" +
                             "What\u2019s the only missing number for the last empty cell in row 3?",
                Highlights = MakeRowHL(3, size, target: (3, 2))
            });

            // ── 9: Interaction — place 1 at (3,2) ────────────────────────────
            steps.Add(new StepDef
            {
                Title         = "One More!",
                Body          = "Row 3 has 5, 6, 2, 3, 4 \u2014 only 1 is left.\n" +
                                "Tap this cell and press 1.",
                Highlights    = MakeSingleHL(3, 2, TutorialCellHighlight.Target),
                RequiresInput = true,
                TargetRow     = 3,
                TargetCol     = 2,
                CorrectValue  = 1
            });

            // ── 10: Introduce Pencil Marks ────────────────────────────────────
            steps.Add(new StepDef
            {
                Title      = "Pencil Marks",
                Body       = "Sometimes two numbers could fit a cell at first glance.\n" +
                             "Pencil marks let you note candidates so you can eliminate\n" +
                             "wrong options step by step.",
                Highlights = MakeSingleHL(0, 1, TutorialCellHighlight.Focus)
            });

            // ── 11: PencilInteraction — mark 2 and 6 in (0,1) ────────────────
            steps.Add(new StepDef
            {
                Title              = "Mark Your Candidates",
                Body               = "Row 0 is missing 2 and 6 \u2014 either could fit here.\n" +
                                     "Pencil mode is now active. Press 2 then 6 to mark\n" +
                                     "both numbers as candidates.",
                Highlights         = MakeRowHL(0, size, target: (0, 1)),
                RequiresPencilMarks = true,
                TargetRow          = 0,
                TargetCol          = 1,
                RequiredMarkValues = new HashSet<int> { 2, 6 }
            });

            // ── 12: Elimination explanation — remove mark 6 programmatically ──
            steps.Add(new StepDef
            {
                Title      = "Eliminate With Logic",
                Body       = "Column 1 already contains 6 \u2014 so 6 cannot go here!\n" +
                             "The invalid candidate is crossed off. Only 2 remains.",
                Highlights = MakeColHL(1, size, target: (0, 1)),
                OnEnter    = b => b?.RemovePencilMark(0, 1, 6)
            });

            // ── 13: Interaction — commit 2 at (0,1) ──────────────────────────
            steps.Add(new StepDef
            {
                Title         = "Commit the Answer",
                Body          = "One pencil mark left \u2014 that\u2019s your answer!\n" +
                                "Turn off pencil mode, tap this cell, and press 2.",
                Highlights    = MakeSingleHL(0, 1, TutorialCellHighlight.Target),
                RequiresInput = true,
                TargetRow     = 0,
                TargetCol     = 1,
                CorrectValue  = 2
            });

            // ── 14: Free-play release ─────────────────────────────────────────
            steps.Add(new StepDef
            {
                Title = "You\u2019ve Got It!",
                Body  = "Use rows, columns, boxes, and pencil marks to reason through every cell.\n" +
                        "When only one number fits \u2014 that\u2019s your answer!\n\n" +
                        "Finish solving the rest of the puzzle on your own."
            });

            return steps;
        }

        // ── Highlight helpers ────────────────────────────────────────────────

        private static Dictionary<(int, int), TutorialCellHighlight> MakeRowHL(int row, int size,
            (int, int)? target = null)
        {
            var d = new Dictionary<(int, int), TutorialCellHighlight>();
            for (var c = 0; c < size; c++)
                d[(row, c)] = (target.HasValue && target.Value == (row, c))
                    ? TutorialCellHighlight.Target : TutorialCellHighlight.Focus;
            return d;
        }

        private static Dictionary<(int, int), TutorialCellHighlight> MakeColHL(int col, int size,
            (int, int)? target = null)
        {
            var d = new Dictionary<(int, int), TutorialCellHighlight>();
            for (var r = 0; r < size; r++)
                d[(r, col)] = (target.HasValue && target.Value == (r, col))
                    ? TutorialCellHighlight.Target : TutorialCellHighlight.Focus;
            return d;
        }

        private static Dictionary<(int, int), TutorialCellHighlight> MakeBoxHL(
            int startRow, int startCol, int boxRows, int boxCols)
        {
            var d = new Dictionary<(int, int), TutorialCellHighlight>();
            for (var r = startRow; r < startRow + boxRows; r++)
            for (var c = startCol; c < startCol + boxCols; c++)
                d[(r, c)] = TutorialCellHighlight.Focus;
            return d;
        }

        private static Dictionary<(int, int), TutorialCellHighlight> MakeRowColHL(
            int row, int col, int size, (int, int)? target = null)
        {
            var d = new Dictionary<(int, int), TutorialCellHighlight>();
            for (var c = 0; c < size; c++)
                d[(row, c)] = TutorialCellHighlight.Focus;
            for (var r = 0; r < size; r++)
                d[(r, col)] = TutorialCellHighlight.Focus;
            if (target.HasValue) d[target.Value] = TutorialCellHighlight.Target;
            return d;
        }

        private static Dictionary<(int, int), TutorialCellHighlight> MakeSingleHL(
            int row, int col, TutorialCellHighlight hl)
        {
            return new Dictionary<(int, int), TutorialCellHighlight> { { (row, col), hl } };
        }

        // ── Constraint-aware wrong-input hint ────────────────────────────────

        private string BuildConstraintHint(int row, int col, int value, int correctValue)
        {
            var board = GetBoard?.Invoke();
            if (board == null) return "That doesn\u2019t fit \u2014 check the row and column.";

            for (var c = 0; c < board.Size; c++)
            {
                if (c == col) continue;
                if (board.Cells[row, c] == value)
                    return value + " is already in this row! Try " + correctValue + ".";
            }
            for (var r = 0; r < board.Size; r++)
            {
                if (r == row) continue;
                if (board.Cells[r, col] == value)
                    return value + " is already in this column! Try " + correctValue + ".";
            }
            var regionId = board.RegionMap[row, col];
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
            {
                if (r == row && c == col) continue;
                if (board.RegionMap[r, c] == regionId && board.Cells[r, c] == value)
                    return value + " is already in this box! Try " + correctValue + ".";
            }
            return "That doesn\u2019t fit here. The answer is " + correctValue + ".";
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI(Transform parent)
        {
            // ── Outer panel ───────────────────────────────────────────────────
            _panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(parent, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.01f, 0.00f);
            rt.anchorMax = new Vector2(0.99f, 0.28f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 0.94f);

            var overrideCanvas = _panel.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder    = 50;
            _panel.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // ── Progress dots ─────────────────────────────────────────────────
            var stepCount = _steps?.Count ?? 15;
            _dotImages = new Image[stepCount];
            var dotW = 1f / (stepCount + 1);
            for (var i = 0; i < stepCount; i++)
            {
                var dotGo = new GameObject("Dot_" + i, typeof(RectTransform), typeof(Image));
                dotGo.transform.SetParent(_panel.transform, false);
                var drt = dotGo.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2((i + 0.5f) * dotW - 0.010f, 0.86f);
                drt.anchorMax = new Vector2((i + 0.5f) * dotW + 0.010f, 0.97f);
                drt.offsetMin = drt.offsetMax = Vector2.zero;
                var img = dotGo.GetComponent<Image>();
                img.color    = DotFuture;
                _dotImages[i] = img;
            }

            // ── Title label ───────────────────────────────────────────────────
            _titleLabel = InRunUiFactory.CreateText(_panel.transform, "TutTitle", "",
                17, TextAnchor.MiddleLeft, new Color(0.95f, 0.82f, 0.40f));
            _titleLabel.rectTransform.anchorMin = new Vector2(0.02f, 0.66f);
            _titleLabel.rectTransform.anchorMax = new Vector2(0.80f, 0.85f);
            _titleLabel.rectTransform.offsetMin = _titleLabel.rectTransform.offsetMax = Vector2.zero;
            _titleLabel.fontStyle = FontStyle.Bold;

            // ── Body text ─────────────────────────────────────────────────────
            _bodyLabel = InRunUiFactory.CreateText(_panel.transform, "TutBody", "",
                13, TextAnchor.UpperLeft, new Color(0.93f, 0.91f, 0.84f));
            _bodyLabel.rectTransform.anchorMin = new Vector2(0.02f, 0.22f);
            _bodyLabel.rectTransform.anchorMax = new Vector2(0.80f, 0.66f);
            _bodyLabel.rectTransform.offsetMin = _bodyLabel.rectTransform.offsetMax = Vector2.zero;
            _bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ── Hint text (wrong-input / progress feedback) ───────────────────
            _hintLabel = InRunUiFactory.CreateText(_panel.transform, "TutHint", "",
                12, TextAnchor.MiddleLeft, new Color(1f, 0.50f, 0.15f));
            _hintLabel.rectTransform.anchorMin = new Vector2(0.02f, 0.04f);
            _hintLabel.rectTransform.anchorMax = new Vector2(0.80f, 0.21f);
            _hintLabel.rectTransform.offsetMin = _hintLabel.rectTransform.offsetMax = Vector2.zero;
            _hintLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _hintLabel.gameObject.SetActive(false);

            // ── Next → button ─────────────────────────────────────────────────
            var nextGo = new GameObject("BtnNext", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGo.transform.SetParent(_panel.transform, false);
            var brt = nextGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.82f, 0.18f);
            brt.anchorMax = new Vector2(0.98f, 0.88f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            nextGo.GetComponent<Image>().color = new Color(0.14f, 0.44f, 0.22f, 0.95f);
            var nLabel = InRunUiFactory.CreateText(nextGo.transform, "Label", "Next \u2192",
                14, TextAnchor.MiddleCenter, Color.white);
            nLabel.rectTransform.anchorMin = Vector2.zero;
            nLabel.rectTransform.anchorMax = Vector2.one;
            nLabel.rectTransform.offsetMin = nLabel.rectTransform.offsetMax = Vector2.zero;
            _nextBtn = nextGo.GetComponent<Button>();
            _nextBtn.onClick.AddListener(AdvanceStep);

            // ── Finish ✔ button ────────────────────────────────────────────────
            var finGo = new GameObject("BtnFinish", typeof(RectTransform), typeof(Image), typeof(Button));
            finGo.transform.SetParent(_panel.transform, false);
            var frt = finGo.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.82f, 0.18f);
            frt.anchorMax = new Vector2(0.98f, 0.88f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            finGo.GetComponent<Image>().color = new Color(0.55f, 0.35f, 0.12f, 0.95f);
            var fLabel = InRunUiFactory.CreateText(finGo.transform, "Label", "Play! \u25ba",
                14, TextAnchor.MiddleCenter, Color.white);
            fLabel.rectTransform.anchorMin = Vector2.zero;
            fLabel.rectTransform.anchorMax = Vector2.one;
            fLabel.rectTransform.offsetMin = fLabel.rectTransform.offsetMax = Vector2.zero;
            _finishBtn = finGo.GetComponent<Button>();
            _finishBtn.onClick.AddListener(Finish);
            _finishBtn.gameObject.SetActive(false);
        }

        // ── Dot refresh ───────────────────────────────────────────────────────

        private void RefreshDots()
        {
            if (_dotImages == null) return;
            for (var i = 0; i < _dotImages.Length; i++)
            {
                if (_dotImages[i] == null) continue;
                _dotImages[i].color = i < _stepIndex  ? DotCompleted
                                    : i == _stepIndex ? DotCurrent
                                    : DotFuture;
            }
        }
    }
}
