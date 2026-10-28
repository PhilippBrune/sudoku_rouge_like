using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class InRunSaveQuitPolicyTests
    {
        [Test]
        public void Resolve_BlocksReturnWhenPuzzleIsStillLoading()
        {
            var decision = InRunSaveQuitPolicy.Resolve(new InRunSaveQuitContext(
                inPuzzle: true,
                hasCurrentBoard: false,
                hasCurrentLevelState: true,
                onEndScreen: false,
                runState: BuildRunState(disableProgressionRewards: false, seasonal: false)));

            Assert.IsTrue(decision.ShouldBlockReturn);
            Assert.AreEqual(InRunSaveQuitPolicy.PuzzleStillLoadingStatusKey, decision.BlockedStatusKey);
            Assert.AreEqual(InRunSaveQuitPersistenceAction.None, decision.PersistenceAction);
            Assert.IsFalse(decision.ShouldShowMonthlyWalkPanel);
        }

        [Test]
        public void Resolve_SavesResumeStateForProgressionRunBeforeEndScreen()
        {
            var decision = InRunSaveQuitPolicy.Resolve(new InRunSaveQuitContext(
                inPuzzle: true,
                hasCurrentBoard: true,
                hasCurrentLevelState: true,
                onEndScreen: false,
                runState: BuildRunState(disableProgressionRewards: false, seasonal: false)));

            Assert.IsFalse(decision.ShouldBlockReturn);
            Assert.AreEqual(InRunSaveQuitPersistenceAction.SaveResumeState, decision.PersistenceAction);
            Assert.IsFalse(decision.ShouldShowMonthlyWalkPanel);
        }

        [Test]
        public void Resolve_ClearsNonProgressionAutosaveBeforeEndScreen()
        {
            var decision = InRunSaveQuitPolicy.Resolve(new InRunSaveQuitContext(
                inPuzzle: false,
                hasCurrentBoard: false,
                hasCurrentLevelState: false,
                onEndScreen: false,
                runState: BuildRunState(disableProgressionRewards: true, seasonal: false)));

            Assert.IsFalse(decision.ShouldBlockReturn);
            Assert.AreEqual(InRunSaveQuitPersistenceAction.ClearNonProgressionAutosave, decision.PersistenceAction);
            Assert.IsFalse(decision.ShouldShowMonthlyWalkPanel);
        }

        // [REQ: CUSTOM-SAVE-001] A Custom Puzzle is saved on Save & Quit and dropped once solved or lost.
        [Test]
        [Requirement("CUSTOM-SAVE-001")]
        public void Resolve_SavesACustomPuzzle_UntilItIsSolvedOrLost()
        {
            var custom = new RunState { Mode = GameMode.Tutorial, TutorialMode = true, RunNumber = 1, CurrentHP = 5, MaxHP = 5,
                DisableProgressionRewards = true };
            InRunSaveQuitPersistenceAction Action(bool onEndScreen, bool complete) => InRunSaveQuitPolicy.Resolve(
                new InRunSaveQuitContext(true, true, true, onEndScreen, custom, complete)).PersistenceAction;

            Assert.AreEqual(InRunSaveQuitPersistenceAction.SaveResumeState, Action(false, false), "mid-puzzle: kept");
            Assert.AreEqual(InRunSaveQuitPersistenceAction.ClearCustomPuzzle, Action(false, true), "solved");
            Assert.AreEqual(InRunSaveQuitPersistenceAction.ClearCustomPuzzle, Action(true, false), "lost, from the end screen");
        }

        [Test]
        public void Resolve_ClearsFinishedGardenRunFromEndScreen()
        {
            var decision = InRunSaveQuitPolicy.Resolve(new InRunSaveQuitContext(
                inPuzzle: false,
                hasCurrentBoard: false,
                hasCurrentLevelState: false,
                onEndScreen: true,
                runState: BuildRunState(disableProgressionRewards: false, seasonal: false)));

            Assert.IsFalse(decision.ShouldBlockReturn);
            Assert.AreEqual(InRunSaveQuitPersistenceAction.ClearFinishedGardenRun, decision.PersistenceAction);
            Assert.IsFalse(decision.ShouldShowMonthlyWalkPanel);
        }

        [Test]
        public void Resolve_SeasonalEndScreenReturnsToMonthlyWalkWithoutExtraPersistence()
        {
            var decision = InRunSaveQuitPolicy.Resolve(new InRunSaveQuitContext(
                inPuzzle: false,
                hasCurrentBoard: false,
                hasCurrentLevelState: false,
                onEndScreen: true,
                runState: BuildRunState(disableProgressionRewards: true, seasonal: true)));

            Assert.IsFalse(decision.ShouldBlockReturn);
            Assert.AreEqual(InRunSaveQuitPersistenceAction.None, decision.PersistenceAction);
            Assert.IsTrue(decision.ShouldShowMonthlyWalkPanel);
        }

        private static RunState BuildRunState(bool disableProgressionRewards, bool seasonal)
        {
            return new RunState
            {
                Mode = seasonal ? GameMode.SeasonalChallenge : GameMode.GardenRun,
                DisableProgressionRewards = disableProgressionRewards,
                IsSeasonalChallenge = seasonal
            };
        }
    }
}
