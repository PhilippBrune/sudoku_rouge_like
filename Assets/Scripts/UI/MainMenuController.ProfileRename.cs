using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: PROFILE-NAME-001] A profile can be named (owner, 0.4.3 playtest): a field over the card's name; Enter keeps it, Esc leaves
    // it. Only the name in the save changes, so its runs and statistics stay.
    public sealed partial class MainMenuController
    {
        private InputField _renameField;

        public void BeginRenameProfile(int slot)
        {
            var card = _profileSelectPanel != null ? _profileSelectPanel.transform.Find($"Content/SlotCard_{slot}") : null;
            if (card == null) return;
            CloseRenameField();
            var go = new GameObject("ProfileNameField", typeof(RectTransform), typeof(Image), typeof(GamepadKeyboardInputField));
            go.transform.SetParent(card, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.05f, 0.535f);
            rt.anchorMax = new Vector2(0.95f, 0.612f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var box = go.GetComponent<Image>();
            box.sprite = ScrollUiKit.Rounded(10f);
            box.type = Image.Type.Sliced;
            box.color = new Color(1f, 0.98f, 0.94f, 1f);
            var text = ScrollUiKit.Label(go.transform, "Text", string.Empty, 28, TextAnchor.MiddleCenter, ScrollUiKit.Ink, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));
            text.supportRichText = false;
            _renameField = go.GetComponent<InputField>();
            _renameField.textComponent = text;
            _renameField.characterLimit = SaveProfileService.MaxProfileNameLength;
            _renameField.text = _profileSlots.GetAllSlotSummaries()[slot].CustomName ?? string.Empty;
            _renameField.navigation = new Navigation { mode = Navigation.Mode.None };
            _renameField.onEndEdit.AddListener(value =>
            {
                var canceled = _renameField.wasCanceled;
                CloseRenameField();
                if (!canceled) SaveProfileService.RenameSlot(slot, value);
                RefreshProfileSelectCards();
                var rename = card.Find($"BtnRename_{slot}");
                if (rename != null) EventSystem.current?.SetSelectedGameObject(rename.gameObject);
            });
            EventSystem.current?.SetSelectedGameObject(go);
            _renameField.ActivateInputField();
        }

        private void CloseRenameField()
        {
            if (_renameField == null) return;
            var field = _renameField;
            _renameField = null;
            ScrollUiKit.DestroyNow(field.gameObject);
        }
    }
}
