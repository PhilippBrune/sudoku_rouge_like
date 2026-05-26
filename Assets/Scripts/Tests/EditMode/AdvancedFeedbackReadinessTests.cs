using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class AdvancedFeedbackReadinessTests
    {
        private const string InRunControllerPath = "Assets/Scripts/UI/InRunController.cs";
        private const string BossGatePath = "Assets/Scripts/UI/BossGateViewController.cs";
        private const string CursePanelPath = "Assets/Scripts/UI/CursePanelController.cs";

        [Test]
        public void PressureMechanics_HaveVisibleAndAccessibleFeedbackHooks()
        {
            var source = File.ReadAllText(InRunControllerPath);

            StringAssert.Contains("BuildPressureRenderData", source);
            StringAssert.Contains("ExpiryFlashCells", source);
            StringAssert.Contains("FlashCells", source);
            StringAssert.Contains("ShakeBadgeCells", source);
            StringAssert.Contains("TriggerWaveRipple", source);
            StringAssert.Contains("ReduceMotion", source);
        }

        [Test]
        public void BossModifierDiscovery_HasHiddenAndRevealedFeedbackPaths()
        {
            var source = File.ReadAllText(BossGatePath);

            StringAssert.Contains("SeenBossModifiers", source);
            StringAssert.Contains("???", source);
            StringAssert.Contains("BossService.GetModifierName", source);
            StringAssert.Contains("BossService.GetModifierDescription", source);
            StringAssert.Contains("ModifierDiscoveryService.Describe", source);
            StringAssert.Contains("InRun.BossGate.WardingFlame", source);
        }

        [Test]
        public void CursePanels_ShowLocalizedTextIconsAndCursedNodeRiskState()
        {
            var cursePanel = File.ReadAllText(CursePanelPath);
            var bossGate = File.ReadAllText(BossGatePath);

            StringAssert.Contains("RefreshPanel", cursePanel);
            StringAssert.Contains("CurseService.GetLocalizedName", cursePanel);
            StringAssert.Contains("CurseService.GetLocalizedDescription", cursePanel);
            StringAssert.Contains("CurseService.GetIconName", cursePanel);
            StringAssert.Contains("Missing curse icon", cursePanel);

            StringAssert.Contains("ShowCursedNodePanel", bossGate);
            StringAssert.Contains("InRun.Cursed.Title", bossGate);
            StringAssert.Contains("InRun.Cursed.Description", bossGate);
            StringAssert.Contains("bg_curse", bossGate);
        }

        [Test]
        public void PendingItemInteractions_ExposeStatusAndCancellationFeedback()
        {
            var source = File.ReadAllText(InRunControllerPath);

            StringAssert.Contains("_patternScrollPending", source);
            StringAssert.Contains("_koiReflectionPending", source);
            StringAssert.Contains("_templeIncensePending", source);
            StringAssert.Contains("_windChimePending", source);
            StringAssert.Contains("Press ESC to cancel", source);
            StringAssert.Contains("remaining", source);
            StringAssert.Contains("Cancelled.", source);
        }
    }
}
