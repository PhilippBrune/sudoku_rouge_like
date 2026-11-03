using SudokuRoguelike.Platform;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: DECK-KEYBOARD-001] A text field that A opens in Steam's gamepad keyboard on the Steam Deck and in Big Picture
    /// mode, so a seed can be typed without a keyboard (RM-102). There the pad can reach the field, and choosing it does
    /// not start typing (a typing field took every key and the pad could not leave it, which is why GAP-078 kept fields
    /// out of the pad's way). Elsewhere it is a plain InputField.
    /// </summary>
    public sealed class GamepadKeyboardInputField : InputField
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            // Asked each time the screen shows: Steam may start after the menu was built.
            if (!SteamDeck.KeyboardAvailable) return;
            navigation = new Navigation { mode = Navigation.Mode.Automatic };
            shouldActivateOnSelect = false;
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            var title = placeholder is Text hint ? hint.text : string.Empty;
            if (SteamDeck.OpenKeyboard(title, text, characterLimit, typed =>
                {
                    if (this == null) return;   // the screen closed while Steam's keyboard was open
                    text = typed;
                    onEndEdit.Invoke(text);
                }))
                return;
            base.OnSubmit(eventData);
        }
    }
}
