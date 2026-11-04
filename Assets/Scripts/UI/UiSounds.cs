using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: AUDIO-UI-001] The interface sounds the owner kept (GAP-073, 2026-11-03): a soft tick when the player moves the
    /// focus to a button (the pad, the arrow keys or the pointer moving onto it; focus the game sets by itself stays quiet),
    /// a click when a button is pressed, and a sound when a menu screen or the Options open and close. The board's cells
    /// make no sound. They play on the run audio's UI channel, which follows the SFX volume.
    /// </summary>
    public sealed class UiSounds : MonoBehaviour
    {
        public enum Sound { Hover, Click, MenuOpen, MenuClose, ShopReroll }

        /// <summary>Every sound played, for tests.</summary>
        public static event Action<Sound> Played;

        private const int NavigationFrames = 3;   // a focus change this soon after a move press is the player's
        private static bool _forgetPointer;
        private GameObject _lastSelected;
        private GameObject _lastHovered;
        private Vector3 _lastMouse;
        private int _navigatedFrame = -100;
        private bool _stickHeld;
        private PointerEventData _pointer;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();

        public static void Play(Sound sound)
        {
            if (sound == Sound.MenuOpen || sound == Sound.MenuClose) _forgetPointer = true;
            Played?.Invoke(sound);
            var audio = RunAudioController.Current;
            if (audio == null) return;
            switch (sound)
            {
                case Sound.Hover: audio.PlayButtonHover(); break;
                case Sound.Click: audio.PlayButtonClick(); break;
                case Sound.MenuOpen: audio.PlayMenuOpen(); break;
                case Sound.MenuClose: audio.PlayMenuClose(); break;
                default: audio.PlayShopReroll(); break;
            }
        }

        /// <summary>The pad's A pressed the chosen control (PadUiBridge uses the press up, so it reports it here).</summary>
        public static void OnSubmit(GameObject chosen)
        {
            if (Pressable(chosen)) Play(Sound.Click);
        }

        private void Update()
        {
            var es = EventSystem.current;
            if (es == null) return;
            if (_forgetPointer) { _lastHovered = null; _forgetPointer = false; }
            if (NavigationPressed()) _navigatedFrame = Time.frameCount;

            // The player moved the focus with the pad or the keys.
            var selected = es.currentSelectedGameObject;
            if (selected != _lastSelected)
            {
                if (Sounding(selected) && Time.frameCount - _navigatedFrame <= NavigationFrames) Play(Sound.Hover);
                _lastSelected = selected;
            }

            // The pointer moved onto a button.
            var mouse = Input.mousePosition;
            if ((mouse - _lastMouse).sqrMagnitude >= 1f)
            {
                _lastMouse = mouse;
                var hovered = UnderPointer(es, mouse);
                if (hovered != _lastHovered && Sounding(hovered)) Play(Sound.Hover);
                _lastHovered = hovered;
            }

            // A press: a click on a button, Enter or Space on the chosen one, or A on a pad only the old input sees (an
            // Input System pad's A sounds through PadUiBridge).
            if (Input.GetMouseButtonDown(0) && Pressable(UnderPointer(es, mouse))) Play(Sound.Click);
            else if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)
                      || (!PadButtons.InputSystemPad && Input.GetKeyDown(KeyCode.JoystickButton0))) && Pressable(selected))
                Play(Sound.Click);
        }

        // Arrow keys, Tab, the D-pad or the left stick pushed this frame.
        private bool NavigationPressed()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Tab))
                return true;
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad == null) return false;
            var stick = pad.leftStick.ReadValue().magnitude;
            var pushed = !_stickHeld && stick > 0.6f;
            _stickHeld = stick > 0.3f;
            return pushed || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame
                   || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
#else
            return false;
#endif
        }

        // The control under the pointer: the topmost hit only, so a panel over a button keeps it quiet.
        private GameObject UnderPointer(EventSystem es, Vector3 mouse)
        {
            _pointer ??= new PointerEventData(es);
            _pointer.position = mouse;
            _hits.Clear();
            es.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0) return null;
            var selectable = _hits[0].gameObject.GetComponentInParent<Selectable>();
            return selectable != null ? selectable.gameObject : null;
        }

        // An interactable control outside the board (its cells are "C_<row>_<col>").
        private static bool Sounding(GameObject go)
        {
            if (go == null || !go.activeInHierarchy || go.name.StartsWith("C_", StringComparison.Ordinal)) return false;
            var selectable = go.GetComponent<Selectable>();
            return selectable != null && selectable.IsInteractable();
        }

        private static bool Pressable(GameObject go) => Sounding(go) && (go.GetComponent<Button>() != null || go.GetComponent<Toggle>() != null);
    }
}
