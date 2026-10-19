using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // The puzzle screen as a hanging scroll (UI-STYLE-002); the layout itself is built in
    // InRunUiBlueprintBuilder.BuildSudokuGameplayPanel, the board colours live in GamePalette.
    public sealed partial class InRunController
    {
        // [REQ: UI-STYLE-002] The hanging scroll names the floor and hangs in front of the floor's painting. Modes without
        // floors (Endless Zen, Spirit Trials, Monthly Walk, tutorials) keep their title and the puzzle-board painting.
        private int _puzzleArtFloor = -2;
        private LanguageOption _puzzleArtLanguage;

        private void ApplyPuzzleScrollFloor(RectTransform panel, RunState state)
        {
            if (panel == null || state == null) return;
            var gardenRun = state.Mode == GameMode.GardenRun && !state.TutorialMode;
            var floorKey = gardenRun ? state.CurrentFloor : -1;
            // The language can change in Options during a puzzle; the title follows it.
            if (floorKey == _puzzleArtFloor && LocalizationService.Current == _puzzleArtLanguage) return;
            _puzzleArtFloor = floorKey;
            _puzzleArtLanguage = LocalizationService.Current;

            var title = panel.Find("InfoScrimBg/SudokuGameplayTitle")?.GetComponent<Text>();
            if (title != null)
                title.text = gardenRun
                    ? FloorThemeData.GetFloorName(state.CurrentFloor)
                    : LocalizationService.T("InRun.Blueprint.SudokuGameplayTitle", "Sudoku Puzzle");
            var painting = panel.Find("PanelBackground")?.GetComponent<RawImage>();
            if (painting != null)
            {
                var name = gardenRun ? PathBackgroundName(state.CurrentFloor) : "bg_puzzle_board";
                var texture = Resources.Load<Texture2D>("background/" + name);
                if (texture == null) Debug.LogWarning($"[InRunController] Puzzle painting background/{name} is missing.");
                painting.texture = texture;
                painting.color = texture != null ? Color.white : Color.clear;
            }
        }
    }
}
