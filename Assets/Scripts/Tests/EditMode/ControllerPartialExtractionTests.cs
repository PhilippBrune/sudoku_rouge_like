using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    // CBH-002 (docs/controller-extraction-plan.md): focused slices of the large controllers live in
    // their own partial files so the ratcheted size budgets can keep going down.
    [TestFixture]
    public sealed class ControllerPartialExtractionTests
    {
        [TestCase("Assets/Scripts/Run/RunDirector.cs", "Assets/Scripts/Run/RunDirector.LevelGeneration.cs",
            "public sealed partial class RunDirector",
            new[] { "public LevelConfig BuildLevelConfig(", "public void StartLevelAsync(", "public void StartLevel(", "private AsyncLevelResult GenerateLevelContent(", "private static SudokuBoard CreateBoardWithFallbacks(" })]
        [TestCase("Assets/Scripts/UI/InRunController.cs", "Assets/Scripts/UI/InRunController.Completion.cs",
            "public sealed partial class InRunController",
            new[] { "private void HandleCompletion()", "private System.Collections.IEnumerator DelayedShowReward()", "private void OnPostRewardReady()", "private System.Collections.IEnumerator AdvanceFloorAfterBossCleared()" })]
        [TestCase("Assets/Scripts/UI/MainMenuBlueprintBuilder.cs", "Assets/Scripts/UI/MainMenuBlueprintBuilder.CustomPuzzlePanel.cs",
            "public sealed partial class MainMenuBlueprintBuilder",
            new[] { "private GameObject BuildTutorialSetupPanel(" })]
        // 1.1 extractions (2026-10-07): economy actions, choices, path tiles, run menu, home widgets, widgets.
        [TestCase("Assets/Scripts/Run/RunDirector.cs", "Assets/Scripts/Run/RunDirector.Economy.cs",
            "public sealed partial class RunDirector",
            new[] { "public bool TryBuyPencil(", "public List<ItemInstance> RerollRewardSlots(", "public void AcceptRestHeal(" })]
        [TestCase("Assets/Scripts/Run/RunDirector.cs", "Assets/Scripts/Run/RunDirector.Choices.cs",
            "public sealed partial class RunDirector",
            new[] { "public List<BossModifierId> RollBossModifierChoices(", "public BossModifierId? GetBossDebuffForCurrentFloor(" })]
        [TestCase("Assets/Scripts/UI/InRunController.cs", "Assets/Scripts/UI/InRunController.PathTiles.cs",
            "public sealed partial class InRunController",
            new[] { "private static void AddPlayerMarker(", "private static void AddCrossLinkBadge(" })]
        // 2026-10-19: the garden path as a hand scroll (UI-STYLE-001).
        [TestCase("Assets/Scripts/UI/InRunController.cs", "Assets/Scripts/UI/InRunController.PathScroll.cs",
            "public sealed partial class InRunController",
            new[] { "private void EnsurePathOverlayRoot()", "private void RefreshPathOverview()", "private Button CreateNodeButton(",
                    "private void RebuildPathLegend(", "private void RebuildFloorProgressStrip(" })]
        [TestCase("Assets/Scripts/UI/InRunController.cs", "Assets/Scripts/UI/InRunController.RunMenu.cs",
            "public sealed partial class InRunController",
            new[] { "private void OnAbandonPressed(", "private void ShowModifierList(" })]
        [TestCase("Assets/Scripts/UI/InRunController.cs", "Assets/Scripts/UI/InRunController.Economy.cs",
            "public sealed partial class InRunController",
            new[] { "private void WireBuyPencilButton(", "private void RefreshBuyPencilButton(" })]
        [TestCase("Assets/Scripts/UI/MainMenuBlueprintBuilder.cs", "Assets/Scripts/UI/MainMenuBlueprintBuilder.HomeWidgets.cs",
            "public sealed partial class MainMenuBlueprintBuilder",
            new[] { "private void BuildHomeWidgets(" })]
        [TestCase("Assets/Scripts/UI/MainMenuBlueprintBuilder.cs", "Assets/Scripts/UI/MainMenuBlueprintBuilder.Widgets.cs",
            "public sealed partial class MainMenuBlueprintBuilder",
            new[] { "private Toggle BuildToggle(", "private Slider BuildSlider(" })]
        public void ExtractedSlices_AreOwnedByTheirFocusedPartialFile(
            string mainPath, string partialPath, string partialDeclaration, string[] ownedMembers)
        {
            var main = File.ReadAllText(mainPath);
            var partial = File.ReadAllText(partialPath);

            StringAssert.Contains(partialDeclaration, main, $"{mainPath} must stay partial.");
            foreach (var member in ownedMembers)
            {
                StringAssert.Contains(member, partial, $"{member} belongs in {partialPath}.");
                StringAssert.DoesNotContain(member, main, $"{member} moved back into {mainPath}.");
            }
        }
    }
}
