using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class RunFlowAcceptanceTests
    {
        [Test]
        public void GardenRun_CanAdvanceToPuzzleSolveGrantRewardsAndExportSaveState()
        {
            var run = StartGardenRun(seed: 424242);

            Assert.AreEqual(GameMode.GardenRun, run.State.Mode);
            Assert.NotNull(run.CurrentFloorGraph);
            Assert.Greater(run.CurrentFloorGraph.Count, 2);
            Assert.AreEqual(NodeType.Start, run.GetCurrentNode().Type);

            var puzzleNode = GetReachablePuzzleNode(run);
            Assert.IsTrue(run.TryAdvanceToNode(puzzleNode.Index));
            Assert.AreEqual(puzzleNode.Index, run.State.CurrentNodeIndex);
            Assert.AreEqual(1, run.State.Depth);
            CollectionAssert.Contains(run.State.NodePath, puzzleNode.Index);

            var config = run.BuildLevelConfig(
                puzzleNode.Type == NodeType.Boss,
                puzzleNode.Type == NodeType.ElitePuzzle,
                puzzleNode.Index,
                puzzleNode.Type == NodeType.PreBoss);
            run.StartLevel(config);

            Assert.NotNull(run.CurrentBoard);
            Assert.NotNull(run.CurrentLevelState);
            Assert.NotNull(run.ConstraintEngine);
            Assert.IsFalse(run.IsLevelComplete);

            SolveCurrentBoard(run);

            Assert.IsTrue(run.IsLevelComplete);

            var goldBeforeReward = run.State.CurrentGold;
            var xpEntry = run.CompleteLevelAndGrantRewards();
            var rewardSlots = run.BuildItemRewardSlots();
            var save = run.ExportPuzzleSaveState();

            Assert.NotNull(xpEntry);
            Assert.AreEqual(config.BoardSize, xpEntry.BoardSize);
            Assert.AreEqual(config.Stars, xpEntry.Stars);
            Assert.Greater(run.State.CurrentGold, goldBeforeReward);
            Assert.AreEqual(1, run.TileXpLog.Count);
            Assert.NotNull(rewardSlots);
            Assert.GreaterOrEqual(rewardSlots.Count, 1);

            Assert.NotNull(save);
            Assert.AreEqual(config.BoardSize, save.BoardSize);
            Assert.AreEqual(config.Stars, save.Stars);
            Assert.AreEqual(config.BoardSize * config.BoardSize, save.Board.Length);
            Assert.AreEqual(0, save.Board.Count(value => value == 0));
        }

        [Test]
        public void BossFlow_CanApplyChosenModifierSolveAndRecordBossDefeat()
        {
            var run = StartGardenRun(seed: 818181);
            var bossNode = run.CurrentFloorGraph.First(node => node.Type == NodeType.Boss);

            Assert.IsTrue(run.TryAdvanceToNode(bossNode.Index, forced: true));
            run.ChooseBossModifiers(new List<BossModifierId> { BossModifierId.EvenOdd });

            var config = run.BuildLevelConfig(isBoss: true, isElite: false, nodeIndex: bossNode.Index);
            run.StartLevel(config);

            Assert.NotNull(run.CurrentBoard);
            Assert.Contains(BossModifierId.EvenOdd, config.ActiveModifiers);

            SolveCurrentBoard(run);
            Assert.IsTrue(run.IsLevelComplete);

            var entry = run.CompleteLevelAndGrantRewards();

            Assert.IsTrue(entry.IsBoss);
            Assert.AreEqual(1, run.State.BossesDefeatedThisRun);
            Assert.IsTrue(run.State.SeenBossModifiers.Contains(BossModifierId.EvenOdd));
        }

        [Test]
        public void NavigationSnapshot_RestoresNodeWhenPuzzleGenerationFails()
        {
            var run = StartGardenRun(seed: 717171);
            var startIndex = run.State.CurrentNodeIndex;
            var startDepth = run.State.Depth;
            var startPathCount = run.State.NodePath.Count;
            var puzzleNode = GetReachablePuzzleNode(run);
            var snapshot = run.CaptureNavigationSnapshot();

            Assert.IsTrue(run.TryAdvanceToNode(puzzleNode.Index));
            Assert.AreEqual(puzzleNode.Index, run.State.CurrentNodeIndex);

            run.RestoreNavigationSnapshot(snapshot);

            Assert.AreEqual(startIndex, run.State.CurrentNodeIndex);
            Assert.AreEqual(startDepth, run.State.Depth);
            Assert.AreEqual(startPathCount, run.State.NodePath.Count);
            Assert.IsFalse(run.CurrentFloorGraph[puzzleNode.Index].Visited);
            Assert.IsNull(run.CurrentBoard);
            Assert.IsNull(run.CurrentLevelState);
        }


        [Test]
        public void TutorialRun_CanCompletePuzzleWithoutProgressionRewards()
        {
            var run = new RunDirector();
            run.StartTutorialRun(
                new TutorialSetupConfig
                {
                    BoardSize = 4,
                    Stars = 1,
                    ResourceMode = TutorialResourceMode.Free,
                    RegionVariant = 0
                },
                seed: 515151);

            Assert.AreEqual(GameMode.Tutorial, run.State.Mode);
            Assert.IsTrue(run.State.TutorialMode);
            Assert.IsTrue(run.State.DisableProgressionRewards);
            Assert.AreEqual(0, run.State.CurrentGold);

            SolveCurrentBoard(run);
            var entry = run.CompleteLevelAndGrantRewards();

            Assert.IsTrue(run.IsLevelComplete);
            Assert.NotNull(entry);
            Assert.AreEqual(0, entry.TotalXp);
            Assert.AreEqual(0, run.State.CurrentGold);
            Assert.AreEqual(1, run.TileXpLog.Count);
        }

        private static RunDirector StartGardenRun(int seed)
        {
            var run = new RunDirector();
            run.StartRun(
                new LaunchRequest
                {
                    ClassId = ClassId.NumberFreak,
                    Mode = GameMode.GardenRun,
                    AllowIrregularPuzzles = false,
                    ClassLevel = 1
                },
                seed);
            return run;
        }

        private static RunNode GetReachablePuzzleNode(RunDirector run)
        {
            foreach (var index in run.GetReachableNodes())
            {
                var node = run.CurrentFloorGraph[index];
                if (RequiresPuzzle(node.Type))
                    return node;
            }

            Assert.Fail("No reachable puzzle node was available from the start node.");
            return null;
        }

        private static bool RequiresPuzzle(NodeType type)
        {
            return type == NodeType.Puzzle
                || type == NodeType.ElitePuzzle
                || type == NodeType.PreBoss
                || type == NodeType.Boss;
        }

        private static void SolveCurrentBoard(RunDirector run)
        {
            var board = run.CurrentBoard;
            for (var row = 0; row < board.Size; row++)
            for (var col = 0; col < board.Size; col++)
            {
                if (board.Cells[row, col] != 0 || board.IsGiven(row, col))
                    continue;

                var result = run.PlaceNumber(row, col, board.Solution[row, col]);
                Assert.AreEqual(PlaceResult.Correct, result, $"Expected correct placement at {row},{col}.");
            }
        }
    }
}
