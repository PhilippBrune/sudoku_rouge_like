using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // Toggle and slider widgets of the generated menus. Split out of MainMenuBlueprintBuilder.cs to keep it within its
    // size budget (docs/controller-extraction-plan.md).
    public sealed partial class MainMenuBlueprintBuilder
    {
        // Same widget as the Options panels (OptionsUiFactory).
        private Toggle BuildToggle(string name, RectTransform parent, string label) =>
            OptionsUiFactory.BuildToggle(name, parent, label);

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
