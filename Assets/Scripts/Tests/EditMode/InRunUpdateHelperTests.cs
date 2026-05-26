using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class InRunUpdateHelperTests
    {
        [Test]
        public void ControllerIndicatorPoller_PollsImmediatelyThenWaitsForInterval()
        {
            var poller = new ControllerIndicatorPoller(2f);
            var joystickCount = 1;

            Assert.IsTrue(poller.Tick(0f, () => joystickCount, () => "Keyboard", out var firstLabel));
            Assert.AreEqual("Keyboard", firstLabel);
            Assert.AreEqual(1, poller.PreviousJoystickCount);

            joystickCount = 2;
            Assert.IsFalse(poller.Tick(1f, () => joystickCount, () => "Controller", out var waitingLabel));
            Assert.IsNull(waitingLabel);
            Assert.AreEqual(1, poller.PreviousJoystickCount);

            Assert.IsTrue(poller.Tick(1f, () => joystickCount, () => "Controller", out var secondLabel));
            Assert.AreEqual("Controller", secondLabel);
            Assert.AreEqual(2, poller.PreviousJoystickCount);
        }

        [Test]
        public void ControllerIndicatorPoller_ForceNextPollClearsTimer()
        {
            var poller = new ControllerIndicatorPoller(5f);

            Assert.IsTrue(poller.Tick(0f, () => 0, () => "Keyboard", out _));
            Assert.IsFalse(poller.Tick(1f, () => 1, () => "Controller", out _));

            poller.ForceNextPoll();

            Assert.IsTrue(poller.Tick(0f, () => 1, () => "Controller", out var label));
            Assert.AreEqual("Controller", label);
            Assert.AreEqual(1, poller.PreviousJoystickCount);
        }

        [Test]
        public void AsyncLevelCompletionPresenter_BuildsFailureViewWhenBoardIsMissing()
        {
            var presenter = new AsyncLevelCompletionPresenter();

            var view = presenter.BuildCompletedView(new LevelConfig { BoardSize = 9, Stars = 3 }, hasCurrentBoard: false);

            Assert.AreEqual(AsyncLevelCompletionViewState.Failed, view.State);
            Assert.IsTrue(view.LocalizeStatus);
            Assert.AreEqual(AsyncLevelCompletionPresenter.GenerationFailedStatus, view.StatusText);
        }

        [Test]
        public void AsyncLevelCompletionPresenter_BuildsBossStatusWhenBoardIsReady()
        {
            var presenter = new AsyncLevelCompletionPresenter();

            var view = presenter.BuildCompletedView(new LevelConfig { BoardSize = 9, Stars = 3, IsBoss = true }, hasCurrentBoard: true);

            Assert.AreEqual(AsyncLevelCompletionViewState.Ready, view.State);
            Assert.IsFalse(view.LocalizeStatus);
            Assert.AreEqual("Boss - 9x9 3*", view.StatusText);
        }

        [Test]
        public void AsyncLevelCompletionPresenter_BuildsPuzzleStatusWhenNormalBoardIsReady()
        {
            var presenter = new AsyncLevelCompletionPresenter();

            var view = presenter.BuildCompletedView(new LevelConfig { BoardSize = 6, Stars = 2 }, hasCurrentBoard: true);

            Assert.AreEqual(AsyncLevelCompletionViewState.Ready, view.State);
            Assert.IsFalse(view.LocalizeStatus);
            Assert.AreEqual("Puzzle - 6x6 2*", view.StatusText);
        }

        [Test]
        public void TimedVisualStateController_OnlyExpiresPositivePastTimestamps()
        {
            var controller = new TimedVisualStateController();

            Assert.IsFalse(controller.HasExpired(0f, 10f));
            Assert.IsFalse(controller.HasExpired(12f, 10f));
            Assert.IsTrue(controller.HasExpired(10f, 10f));
            Assert.IsTrue(controller.HasExpired(8f, 10f));
        }
    }
}
