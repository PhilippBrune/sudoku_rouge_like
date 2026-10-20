using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Custom Puzzle (tutorial setup) panel. Split out of MainMenuBlueprintBuilder.cs per
    /// docs/controller-extraction-plan.md, following MainMenuBlueprintBuilder.ChallengePanels.cs.
    /// </summary>
    public sealed partial class MainMenuBlueprintBuilder
    {
        // ------------------------------------------------------------------------
        // Panel: Tutorial Setup
        // ------------------------------------------------------------------------

        // [REQ: UI-STYLE-012] The rule book (layout-proposals.md, "Custom Puzzle · A"): a full-screen scene in the setup
        // painting with the title, Back, and the CustomPuzzleScreen (settings slip, book, tray with Start).
        private GameObject BuildTutorialSetupPanel(RectTransform root, MainMenuController mc,
            TutorialMenuController tutCtrl)
        {
            var screen = EnsureRect("TutorialSetupPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var panel = screen.gameObject;
            var bg = EnsureOrGetImage(panel, BgColor);
            if (ApplyPanelBackground(bg, "background/bg_tutorial_setup"))
                bg.color = new Color(0.62f, 0.60f, 0.56f, 1f);

            ScrollUiKit.Label(screen, "Eyebrow", LocalizationService.T("CustomPuzzle.Book.Eyebrow", "Practice").ToUpperInvariant(), 20, TextAnchor.MiddleLeft,
                ScrollUiKit.Gold, new Vector2(0.0469f, 0.930f), new Vector2(0.30f, 0.952f), 12);
            var title = ScrollUiKit.Label(screen, "Title", T("Custom Puzzle"), 48, TextAnchor.MiddleLeft, ScrollUiKit.PaperText,
                new Vector2(0.0469f, 0.875f), new Vector2(0.27f, 0.930f), 24);
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -2f);

            var startBtn = ScrollUiKit.PillButton(screen, "BtnTutStart", Vector2.zero, Vector2.one,
                LocalizationService.T("CustomPuzzle.Book.Start", "Start puzzle"), 28, ScrollUiKit.Vermilion, Color.white, 30f);
            startBtn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            startBtn.onClick.AddListener(mc.StartTutorialGame);

            var backBtn = ScrollUiKit.RimButton(screen, "BtnTutBack", new Vector2(0.0469f, 0.0667f), new Vector2(0.1094f, 0.1111f),
                T("Back"), 22, ScrollUiKit.Gold, new Color(0.45f, 0.33f, 0.14f, 0.95f), ScrollUiKit.PaperText);
            backBtn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            backBtn.onClick.AddListener(mc.BackToMainMenu);

            tutCtrl.Configure(startBtn);
            EnsureComponent<CustomPuzzleScreen>(panel).Build(screen, tutCtrl, startBtn);

            panel.SetActive(false);
            return panel;
        }
    }
}
