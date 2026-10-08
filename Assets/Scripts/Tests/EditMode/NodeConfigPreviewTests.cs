using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-PATH-101] [REQ: REQ-PATH-102] [REQ: REQ-BOSS-105] GAP-013 / SPEC-CONF-028:
    // the config shown on a map tile is the puzzle that generates, board sizes stay in the floor's range,
    // and bosses follow that range too (no 8×8 minimum).
    [TestFixture]
    [Requirement("REQ-PATH-101", "REQ-PATH-102", "REQ-BOSS-105")]
    public sealed class NodeConfigPreviewTests
    {
        [TestCase(12001)]
        [TestCase(12002)]
        public void PreviewConfig_MatchesGeneratedBoard_AndStaysInFloorRange(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = true,
                ClassLevel = 1
            }, seed);

            for (var floor = 0; floor < 5; floor++)
            {
                if (floor > 0) run.AdvanceToNextFloor();
                var minSize = FloorThemeData.GetMinBoardSize(floor);
                var maxSize = FloorThemeData.GetMaxBoardSize(floor);
                var checkedNodes = 0;
                for (var i = 0; i < run.CurrentFloorGraph.Count && checkedNodes < 4; i++)
                {
                    var node = run.CurrentFloorGraph[i];
                    if (!IsPuzzle(node.Type)) continue;
                    checkedNodes++;

                    var preview = run.BuildNodeLevelConfig(node, i);
                    AssertInFloorRange(preview, minSize, maxSize, $"floor {floor + 1} node {i} ({node.Type})");

                    run.StartLevel(preview.Clone());
                    Assert.NotNull(run.CurrentBoard, $"floor {floor + 1} node {i} did not generate");
                    Assert.AreEqual(preview.BoardSize, run.CurrentBoard.Size,
                        $"floor {floor + 1} node {i}: map shows {preview.BoardSize}x{preview.BoardSize}");
                    Assert.AreEqual(preview.Stars, run.CurrentLevelConfig.Stars, $"floor {floor + 1} node {i}: stars");
                }

                var boss = run.CurrentFloorGraph.First(n => n.Type == NodeType.Boss);
                var bossConfig = run.BuildNodeLevelConfig(boss, boss.Index);
                AssertInFloorRange(bossConfig, minSize, maxSize, $"floor {floor + 1} boss");
            }
        }

        private static void AssertInFloorRange(LevelConfig config, int minSize, int maxSize, string where)
        {
            Assert.GreaterOrEqual(config.BoardSize, minSize, where);
            // Documented exception: two or more global structural modifiers need at least 8×8 (BossMechanicsSystem).
            var structural = config.ActiveModifiers.Count(BossService.IsBoardShapingModifier);
            var allowedMax = structural >= 2 ? System.Math.Max(maxSize, 8) : maxSize;
            Assert.LessOrEqual(config.BoardSize, allowedMax,
                $"{where}: {config.BoardSize}x{config.BoardSize} with [{string.Join(", ", config.ActiveModifiers)}]");
        }

        private static bool IsPuzzle(NodeType type) =>
            type == NodeType.Puzzle || type == NodeType.ElitePuzzle || type == NodeType.PreBoss;
    }
}
