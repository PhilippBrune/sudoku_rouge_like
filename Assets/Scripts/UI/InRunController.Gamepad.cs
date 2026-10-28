using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: INPUT-PAD-004] The pad in a run (0.3.0 playtest, RM-206): in a puzzle the D-pad and left stick move on the board,
    // the right stick on the number pad, A places the number pad's digit, B (held) clears the cell, LB opens the bag, LT
    // the relics, RB (and X) switches Solve and Pencil, L3 (and R3) highlights the cell's digit, Start opens the Options,
    // Select saves and quits. On the path Start, B and LT/RT act here; A and moving stay with the EventSystem, which
    // already drives the path's nodes and the scenes over it (GAP-080). Buttons are read with PadButtons (GAP-077).
    public sealed partial class InRunController
    {
        private bool _gbControllerInRelicsFocus;

        private void HandleGamepadOptionsFocus()
        {
            if (_optionsFocusCache.Count == 0 || OptionsPageChanged()) RebuildOptionsFocusCache();
            if (_optionsFocusCache.Count == 0) return;

            var es = UnityEngine.EventSystems.EventSystem.current;
            var curGo = es?.currentSelectedGameObject;
            var curSel = curGo != null ? curGo.GetComponent<Selectable>() : null;
            var idx = curSel != null ? _optionsFocusCache.IndexOf(curSel) : 0;
            if (idx < 0) idx = 0;

            // Up/Down cycles elements; Left/Right adjusts sliders
            var moveDown = _inputRemap.WasActionPressed(InputAction.MoveDown);
            var moveUp   = _inputRemap.WasActionPressed(InputAction.MoveUp);
            if (moveDown) idx = (idx + 1) % _optionsFocusCache.Count;
            if (moveUp)   idx = (idx + _optionsFocusCache.Count - 1) % _optionsFocusCache.Count;
            if (moveDown || moveUp)
                es?.SetSelectedGameObject(_optionsFocusCache[idx].gameObject);

            var moveRight = _inputRemap.WasActionPressed(InputAction.MoveRight);
            var moveLeft  = _inputRemap.WasActionPressed(InputAction.MoveLeft);
            if ((moveRight || moveLeft) && _optionsFocusCache[idx] is Slider sl)
                sl.value = Mathf.Clamp(sl.value + (moveRight ? 0.1f : -0.1f) * (sl.maxValue - sl.minValue),
                    sl.minValue, sl.maxValue);

            // A: invoke button or flip toggle (unless the UI's Submit does it this frame)
            if (PadButtons.ConfirmPressed())
            {
                var sel = _optionsFocusCache[idx];
                if (sel is Button btn) btn.onClick.Invoke();
                else if (sel is Toggle tgl) tgl.isOn = !tgl.isOn;
                else if (sel is Dropdown dd) dd.Show();
            }

            // B: close options panel (Start toggles it in HandleGamepadController)
            if (PadButtons.Pressed(KeyCode.JoystickButton1))
                ToggleOptionsPanel();
        }

        private void HandleGamepadTutorialFocus()
        {
            if (_basicsTutorial == null) return;
            var panel = _basicsTutorial.PanelRoot;
            if (panel == null || !panel.activeSelf) return;
            if (PadButtons.ConfirmPressed())
            {
                var tutBtns = panel.GetComponentsInChildren<Button>(false);
                for (var k = 0; k < tutBtns.Length; k++)
                {
                    if (tutBtns[k].IsInteractable() && tutBtns[k].gameObject.activeSelf)
                    { tutBtns[k].onClick.Invoke(); break; }
                }
            }
        }

        private void HandleGamepadController()
        {
            _inputRemap.Tick();

            // Start: open/close the in-game Options
            if (PadButtons.Pressed(KeyCode.JoystickButton7))
            {
                ToggleOptionsPanel();
                return;
            }

            // ── Tutorial panel: A advances step, puzzle input suppressed ──────────
            if (_basicsTutorial != null && _basicsTutorial.IsActive)
            {
                HandleGamepadTutorialFocus();
                return;
            }

            // ── In-game options panel ─────────────────────────────────────────────
            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf)
            {
                HandleGamepadOptionsFocus();
                return;
            }

            // ── Overlay panels (shop / reward / rest / relic / bag-swap / boss-gate / event) ──
            if (TryGetActiveOverlay(out var overlayPanel))
            {
                HandleGamepadOverlayFocus(overlayPanel);
                return;
            }

            // LB: into the bag and back; LT: into the relics and back (RM-206); an empty bag or no relic: nothing to enter
            var state = _map?.Run?.State;
            if (PadButtons.Pressed(KeyCode.JoystickButton4) && (_gbControllerInItemsFocus || (state?.HeldItems?.Count ?? 0) > 0))
                SetPadFocus(items: !_gbControllerInItemsFocus, relics: false);
            else if (TriggerPressed(left: true) && (_gbControllerInRelicsFocus || (state?.HeldRelics?.Count ?? 0) > 0))
                SetPadFocus(items: false, relics: !_gbControllerInRelicsFocus);

            if (_gbControllerInItemsFocus) HandleGamepadItemsFocus();
            else if (_gbControllerInRelicsFocus) HandleGamepadRelicsFocus();
            else HandleGamepadPuzzleFocus();
            _triggerLPrev = InputRemapService.ReadTrigger(left: true);
            _triggerRPrev = InputRemapService.ReadTrigger(left: false);
        }

        private void SetPadFocus(bool items, bool relics)
        {
            _gbControllerInItemsFocus = items;
            _gbControllerInRelicsFocus = relics;
            if (items) _hudView?.ControllerSelectBagSlot(0);
            else if (relics) _hudView?.ControllerSelectRelicSlot(0);
            else SetStatus(T("[Controller] Puzzle mode  (LB to switch)"));
            _hudView?.ClearControllerRelicSelection(keep: relics);
            _numpadView?.HighlightControllerCursor(items || relics ? (int?)null : _gbNumpadRow * 3 + _gbNumpadCol);
        }

        private bool TriggerPressed(bool left)
        {
            var now = InputRemapService.ReadTrigger(left);
            return now > TriggerThresh && (left ? _triggerLPrev : _triggerRPrev) <= TriggerThresh;
        }

        // GAP-080: on the path (no puzzle open) Start opens the Options, B asks to leave (or closes the Options or a scene
        // over the path), LT/RT scroll; Select saves and quits. A and moving are the EventSystem's.
        private void HandleGamepadOnPath()
        {
            if (_pathQuitModalActive) return;   // its own Quit and Stay take A
            if (PadButtons.Pressed(KeyCode.JoystickButton7)) { ToggleOptionsPanel(); return; }
            if (_inGameOptionsPanel != null && _inGameOptionsPanel.activeSelf)
            {
                if (PadButtons.Pressed(KeyCode.JoystickButton1)) ToggleOptionsPanel();
                return;
            }
            if (TryGetActiveOverlay(out var overlay))
            {
                if (PadButtons.Pressed(KeyCode.JoystickButton1)) InvokeOverlayCancel(overlay);
                return;
            }
            if (PadButtons.Pressed(KeyCode.JoystickButton1))
            {
                ExecuteInRunCancelRoute(CancelRouteContract.ResolveInRunGamepadCancel(BuildInRunCancelContext()));
                return;
            }
            if (PadButtons.Pressed(KeyCode.JoystickButton6)) { SaveAndQuit(); return; }

            var lt = InputRemapService.ReadTrigger(left: true);
            var rt = InputRemapService.ReadTrigger(left: false);
            var scrollDelta = (rt - lt) * Time.unscaledDeltaTime * 1.5f;
            if (_pathScrollRect != null && Mathf.Abs(scrollDelta) > 0.001f)
                _pathScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(_pathScrollRect.horizontalNormalizedPosition + scrollDelta);
        }

        private void HandleGamepadPuzzleFocus()
        {
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveUp))    MoveSelection(-1, 0);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveDown))  MoveSelection(1, 0);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveLeft))  MoveSelection(0, -1);
            if (_inputRemap.WasGamepadActionPressed(InputAction.MoveRight)) MoveSelection(0, 1);

            if (PadButtons.Pressed(KeyCode.JoystickButton0))
            {
                if (_gameOverPanel != null && _gameOverPanel.activeSelf) { SaveAndQuit(); return; }
                // PS-3: A also confirms pending cell-select interactions
                bool anyPending = _koiReflectionPending || _templeIncensePending ||
                    _patternScrollPending || _swapMode || _windChimePending;
                if (anyPending && _selectedRow >= 0 && _selectedCol >= 0)
                { OnCellClicked(_selectedRow, _selectedCol); return; }
                _pendingKeyboardDigit = _gbNumpadRow * 3 + _gbNumpadCol + 1;
                _numpadView?.HighlightControllerCursor(_gbNumpadRow * 3 + _gbNumpadCol);
            }

            if (PadButtons.Pressed(KeyCode.JoystickButton1) && _gameOverPanel != null && _gameOverPanel.activeSelf)
            { SaveAndQuit(); return; }

            // PS-2: B held 0.4 s clears the cell
            if (PadButtons.Held(KeyCode.JoystickButton1)) _bButtonHoldTime += Time.unscaledDeltaTime;
            else _bButtonHoldTime = 0f;
            if (_bButtonHoldTime >= 0.4f)
            {
                _bButtonHoldTime = -1f; // prevent re-trigger while still held
                ClearCell();
            }

            if (PadButtons.Pressed(KeyCode.JoystickButton2) || PadButtons.Pressed(KeyCode.JoystickButton5))
                TogglePencilMode();

            // L3 (and R3 as before): highlight the chosen cell's digit
            if (PadButtons.Pressed(KeyCode.JoystickButton8) || PadButtons.Pressed(KeyCode.JoystickButton9))
                ToggleHighlightAtSelection();

            // Select: save and quit (a tap; it was a 1.5 s hold)
            if (PadButtons.Pressed(KeyCode.JoystickButton6)) { SaveAndQuit(); return; }

            HandleRightStickNumpad();
        }

        // GAP-081: the right stick moves the number pad's cursor, read through the Input System (the legacy axes gave
        // nothing on an Xbox pad); the cursor stays on the board's digits.
        private void HandleRightStickNumpad()
        {
            var stick = PadButtons.RightStick();
            var h = stick.x;
            var v = stick.y;
            var hRight = h >  RightStickThresh && _gbRStickPrevH <=  RightStickThresh;
            var hLeft  = h < -RightStickThresh && _gbRStickPrevH >= -RightStickThresh;
            var vUp    = v >  RightStickThresh && _gbRStickPrevV <=  RightStickThresh;
            var vDown  = v < -RightStickThresh && _gbRStickPrevV >= -RightStickThresh;
            _gbRStickPrevH = h;
            _gbRStickPrevV = v;
            if (!(hRight || hLeft || vUp || vDown)) return;

            var size = _map?.Run?.CurrentBoard?.Size ?? 9;
            var col = Mathf.Clamp(_gbNumpadCol + (hRight ? 1 : hLeft ? -1 : 0), 0, 2);
            var row = Mathf.Clamp(_gbNumpadRow + (vDown ? 1 : vUp ? -1 : 0), 0, 2);
            if (row * 3 + col + 1 <= size) (_gbNumpadRow, _gbNumpadCol) = (row, col);
            _numpadView?.HighlightControllerCursor(_gbNumpadRow * 3 + _gbNumpadCol);
        }

        private void HandleGamepadItemsFocus()
        {
            if (InputRemapService.WasDpadPressed(InputAction.MoveUp)) _hudView?.ControllerSelectBagSlot(-1);
            if (InputRemapService.WasDpadPressed(InputAction.MoveDown)) _hudView?.ControllerSelectBagSlot(+1);
            if (PadButtons.Pressed(KeyCode.JoystickButton0)) { _hudView?.ControllerActivateBagSlot(); SetPadFocus(false, false); }
            if (PadButtons.Pressed(KeyCode.JoystickButton8)) _hudView?.ControllerInspectBagSlot();
            if (PadButtons.Pressed(KeyCode.JoystickButton1)) SetPadFocus(false, false);
        }

        // RM-206: the relics: D-pad moves between them, A or L3 shows what one does, B or LT goes back to the board.
        private void HandleGamepadRelicsFocus()
        {
            if (InputRemapService.WasDpadPressed(InputAction.MoveLeft) || InputRemapService.WasDpadPressed(InputAction.MoveUp))
                _hudView?.ControllerSelectRelicSlot(-1);
            if (InputRemapService.WasDpadPressed(InputAction.MoveRight) || InputRemapService.WasDpadPressed(InputAction.MoveDown))
                _hudView?.ControllerSelectRelicSlot(+1);
            if (PadButtons.Pressed(KeyCode.JoystickButton0) || PadButtons.Pressed(KeyCode.JoystickButton8))
                _hudView?.ControllerSelectRelicSlot(0);
            if (PadButtons.Pressed(KeyCode.JoystickButton1)) SetPadFocus(false, false);
        }

        private static bool IsShown(GameObject panel) => panel != null && panel.activeSelf;

        // B on a scene over the path: its own skip, leave or cancel button (GAP-080).
        private static void InvokeOverlayCancel(GameObject overlay)
        {
            var buttons = overlay.GetComponentsInChildren<Button>(false);
            for (var k = buttons.Length - 1; k >= 0; k--)
                if (buttons[k].IsInteractable() && CancelRouteContract.IsOverlayCancelControlName(buttons[k].name))
                { buttons[k].onClick.Invoke(); return; }
        }

        // After the Options or the leave question close on the path, the pad's focus goes back to a node (C# review: it
        // stayed on a hidden control and the D-pad reached nothing).
        private void RestorePadFocusOnPath()
        {
            if (_pathPanel == null || !_pathPanel.activeSelf || (_sudokuPanel != null && _sudokuPanel.activeSelf)) return;
            if (_gbReachableNodeButtons.Count == 0) return;
            var node = _gbReachableNodeButtons[Mathf.Clamp(_gbPathCursor, 0, _gbReachableNodeButtons.Count - 1)];
            if (node != null) UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(node.gameObject);
        }

        /// <summary>H, Enter, L3 and R3: highlight every cell with the chosen cell's digit, or clear it (GAP-082).</summary>
        private void ToggleHighlightAtSelection()
        {
            var board = _map?.Run?.CurrentBoard;
            if (board == null || _selectedRow < 0 || _selectedCol < 0) return;
            var val = board.Cells[_selectedRow, _selectedCol];
            if (val <= 0) return;
            _highlightValue = _highlightValue == val ? 0 : val;
            RenderBoard();
            SetStatus(_highlightValue > 0 ? Tf("Highlighting {0}.", val) : T("Highlight cleared."));
        }
    }
}
