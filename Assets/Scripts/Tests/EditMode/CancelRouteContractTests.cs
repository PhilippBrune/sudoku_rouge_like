using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class CancelRouteContractTests
    {
        [Test]
        public void MainMenuCancel_RespectsOverlayPriority()
        {
            Assert.AreEqual(
                CancelRouteAction.None,
                CancelRouteContract.ResolveMainMenuCancel(
                    new MainMenuCancelContext(true, true, true, MenuScreen.Options)));

            Assert.AreEqual(
                CancelRouteAction.DismissSudokuBasicsOverlay,
                CancelRouteContract.ResolveMainMenuCancel(
                    new MainMenuCancelContext(false, true, true, MenuScreen.Options)));

            Assert.AreEqual(
                CancelRouteAction.DismissStartRunConfirmation,
                CancelRouteContract.ResolveMainMenuCancel(
                    new MainMenuCancelContext(false, false, true, MenuScreen.Options)));

            Assert.AreEqual(
                CancelRouteAction.BackFromMenuPanel,
                CancelRouteContract.ResolveMainMenuCancel(
                    new MainMenuCancelContext(false, false, false, MenuScreen.Options)));

            Assert.AreEqual(
                CancelRouteAction.None,
                CancelRouteContract.ResolveMainMenuCancel(
                    new MainMenuCancelContext(false, false, false, MenuScreen.MainMenu)));
        }

        [Test]
        public void InRunKeyboardCancel_RespectsRecoverableStatePriority()
        {
            Assert.AreEqual(
                CancelRouteAction.CloseInRunOptions,
                CancelRouteContract.ResolveInRunKeyboardCancel(
                    new InRunCancelContext(true, true, true, false, true, true)));

            Assert.AreEqual(
                CancelRouteAction.ExitItemsFocus,
                CancelRouteContract.ResolveInRunKeyboardCancel(
                    new InRunCancelContext(false, false, false, false, true, true)));

            Assert.AreEqual(
                CancelRouteAction.CancelPendingItemInteraction,
                CancelRouteContract.ResolveInRunKeyboardCancel(
                    new InRunCancelContext(false, false, false, false, false, true)));

            Assert.AreEqual(
                CancelRouteAction.ShowPathQuitConfirmation,
                CancelRouteContract.ResolveInRunKeyboardCancel(
                    new InRunCancelContext(false, false, true, false, false, false)));

            Assert.AreEqual(
                CancelRouteAction.None,
                CancelRouteContract.ResolveInRunKeyboardCancel(
                    new InRunCancelContext(false, false, true, true, false, false)));
        }

        [Test]
        public void InRunGamepadCancel_RespectsOptionsOverlayAndPathPriority()
        {
            Assert.AreEqual(
                CancelRouteAction.CloseInRunOptions,
                CancelRouteContract.ResolveInRunGamepadCancel(
                    new InRunCancelContext(true, true, true, false, true, true)));

            Assert.AreEqual(
                CancelRouteAction.InvokeOverlayCancelButton,
                CancelRouteContract.ResolveInRunGamepadCancel(
                    new InRunCancelContext(false, true, true, false, true, true)));

            Assert.AreEqual(
                CancelRouteAction.ShowPathQuitConfirmation,
                CancelRouteContract.ResolveInRunGamepadCancel(
                    new InRunCancelContext(false, false, true, false, true, true)));

            Assert.AreEqual(
                CancelRouteAction.ExitItemsFocus,
                CancelRouteContract.ResolveInRunGamepadCancel(
                    new InRunCancelContext(false, false, false, false, true, true)));

            Assert.AreEqual(
                CancelRouteAction.CancelPendingItemInteraction,
                CancelRouteContract.ResolveInRunGamepadCancel(
                    new InRunCancelContext(false, false, false, false, false, true)));
        }

        [Test]
        public void OverlayCancelControlName_MatchesExpectedRuntimeButtons()
        {
            Assert.IsTrue(CancelRouteContract.IsOverlayCancelControlName("BtnSkipReward"));
            Assert.IsTrue(CancelRouteContract.IsOverlayCancelControlName("BtnAbort"));
            Assert.IsTrue(CancelRouteContract.IsOverlayCancelControlName("BtnCancel"));
            Assert.IsTrue(CancelRouteContract.IsOverlayCancelControlName("BtnLeaveShop"));
            Assert.IsTrue(CancelRouteContract.IsOverlayCancelControlName("BtnStay"));
            Assert.IsFalse(CancelRouteContract.IsOverlayCancelControlName("BtnConfirm"));
            Assert.IsFalse(CancelRouteContract.IsOverlayCancelControlName("BtnPurchase"));
        }
    }
}
