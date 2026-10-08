using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // Toggle and slider widgets of the generated menus. Split out of MainMenuBlueprintBuilder.cs to keep it within its
    // size budget (docs/controller-extraction-plan.md).
    public sealed partial class MainMenuBlueprintBuilder
    {
        private Toggle BuildToggle(string name, RectTransform parent, string label)
        {
            var row = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.sizeDelta = new Vector2(260f, 32f);

            var bg = EnsureOrGetImage(row.gameObject, GamePalette.WithAlpha(BtnColor, 0.85f));
            var toggle = EnsureComponent<Toggle>(row.gameObject);

            // [REQ: REQ-UI-103] GAP-029: an always-visible box, so "off" reads as an empty box and "on" as a filled one.
            var box = EnsureRect("Box", row,
                new Vector2(0.02f, 0.12f), new Vector2(0.10f, 0.88f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(box.gameObject, new Color(0.02f, 0.02f, 0.02f, 0.90f)).raycastTarget = false;
            var boxOutline = EnsureComponent<Outline>(box.gameObject);
            boxOutline.effectColor = AccentColor;
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var check = EnsureRect("Checkmark", box,
                new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            var checkImg = EnsureOrGetImage(check.gameObject, AccentColor);
            checkImg.raycastTarget = false;

            var text = BuildText("Label", row, label, 15, TextAnchor.MiddleLeft);
            text.color = BtnTextColor; // dark text on cream background
            SetRect(text.rectTransform,
                new Vector2(0.14f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            return toggle;
        }

        private Slider BuildSlider(string name, RectTransform parent)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var slider = EnsureComponent<Slider>(rect.gameObject);

            var bg = EnsureRect("Background", rect,
                new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            EnsureOrGetImage(bg.gameObject, GamePalette.ShadowMid);

            var fillArea = EnsureRect("Fill Area", rect,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var fill = EnsureRect("Fill", fillArea,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = EnsureOrGetImage(fill.gameObject, GamePalette.ToggleFill);
            fillImg.raycastTarget = false;

            var handleArea = EnsureRect("Handle Slide Area", rect,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var handle = EnsureRect("Handle", handleArea,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(-10f, 0f), new Vector2(10f, 0f));
            var handleImg = EnsureOrGetImage(handle.gameObject, GamePalette.ToggleHandle);

            slider.targetGraphic = handleImg;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return slider;
        }
    }
}
