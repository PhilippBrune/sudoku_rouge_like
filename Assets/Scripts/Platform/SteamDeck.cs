using System;
using UnityEngine;
#if STEAMWORKS_NET
using Steamworks;
#endif

namespace SudokuRoguelike.Platform
{
    /// <summary>
    /// [REQ: DECK-KEYBOARD-001] [REQ: DECK-BUTTONS-001] The Steam Deck (RM-102): whether Steam runs the game on one, and
    /// Steam's gamepad keyboard for a text field (on the Deck and in Big Picture mode). Without Steam both are off.
    /// </summary>
    public static class SteamDeck
    {
        /// <summary>Tests set this to act as on a Deck (true) or off one (false); null asks Steam.</summary>
        public static bool? Override;

        private static bool? _running;

        /// <summary>True when Steam runs the game on a Steam Deck.</summary>
        public static bool Running
        {
            get
            {
                if (Override.HasValue) return Override.Value;
                if (_running.HasValue) return _running.Value;
#if STEAMWORKS_NET
                if (!SteamReady) return false;   // asked again once Steam is up
                try { _running = SteamUtils.IsSteamRunningOnSteamDeck(); }
                catch (Exception ex) { Debug.LogWarning("[SteamDeck] " + ex.Message); _running = false; }
                return _running.Value;
#else
                return false;
#endif
            }
        }

        /// <summary>True when Steam can show its gamepad keyboard: on a Deck or in Big Picture mode.</summary>
        public static bool KeyboardAvailable
        {
            get
            {
                if (Override.HasValue) return false;   // tests never open Steam's keyboard
#if STEAMWORKS_NET
                try { return SteamReady && (Running || SteamUtils.IsSteamInBigPictureMode()); }
                catch (Exception) { return false; }
#else
                return false;
#endif
            }
        }

#if STEAMWORKS_NET
        private static bool SteamReady => PlatformServiceRegistry.SteamworksCallsEnabled && SteamManager.Initialized;

        private static Callback<GamepadTextInputDismissed_t> _dismissed;
        private static Action<string> _onSubmitted;
#endif

        /// <summary>Opens Steam's gamepad keyboard for one line; <paramref name="onSubmitted"/> gets the text the player
        /// confirmed (nothing when they closed it). False when the keyboard is not available.</summary>
        public static bool OpenKeyboard(string description, string current, int maxChars, Action<string> onSubmitted)
        {
#if STEAMWORKS_NET
            if (!KeyboardAvailable) return false;
            try
            {
                _dismissed ??= Callback<GamepadTextInputDismissed_t>.Create(OnDismissed);
                _onSubmitted = onSubmitted;
                // The full keyboard has no number layout (only the floating one, which returns no text); a seed field
                // checks its digits itself.
                var shown = SteamUtils.ShowGamepadTextInput(EGamepadTextInputMode.k_EGamepadTextInputModeNormal,
                    EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, description ?? string.Empty,
                    (uint)Mathf.Max(1, maxChars), current ?? string.Empty);
                if (!shown) _onSubmitted = null;
                return shown;
            }
            catch (Exception ex)
            {
                _onSubmitted = null;
                Debug.LogWarning("[SteamDeck] keyboard: " + ex.Message);
                return false;
            }
#else
            return false;
#endif
        }

#if STEAMWORKS_NET
        private static void OnDismissed(GamepadTextInputDismissed_t result)
        {
            var submitted = _onSubmitted;
            _onSubmitted = null;
            if (!result.m_bSubmitted || submitted == null) return;
            var length = SteamUtils.GetEnteredGamepadTextLength();
            if (SteamUtils.GetEnteredGamepadTextInput(out var text, length + 1)) submitted(text);
        }
#endif
    }
}
