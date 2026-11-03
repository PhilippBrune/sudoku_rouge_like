using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Sudoku;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // [REQ: BOSS-MOD-007] Golden geometry snapshots (BossMechanicsSystem "Modifier catalog", Phase 0 step 5): every player
    // modifier draws its geometry on one fixed 9×9 board with one fixed seed, and the result must match the stored
    // snapshot. A generator change that moves lines, cages or clues shows up here; when the change is meant, write the
    // snapshot again with SR_UPDATE_GOLDEN=1 and review its diff. The file is written where the tests run: on the test
    // mirror, copy TestData/geometry_golden.txt back into the repository.
    [TestFixture]
    [Requirement("BOSS-MOD-007")]
    public sealed class GeometrySnapshotTests
    {
        private const string GoldenPath = "TestData/geometry_golden.txt";
        private const int Seed = 20261103;

        [Test]
        public void EveryModifier_DrawsTheSameGeometryAsItsSnapshot()
        {
            var board = FixedBoard();
            var current = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var mod in BossService.GetGeneratablePlayerModifiers(9))
                current[mod.ToString()] = JsonUtility.ToJson(
                    ModifierGeometryGenerator.Generate(board, new List<BossModifierId> { mod }, Seed));
            var text = string.Join("\n", current.Select(p => p.Key + "\t" + p.Value)) + "\n";

            if (Environment.GetEnvironmentVariable("SR_UPDATE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(GoldenPath));
                File.WriteAllText(GoldenPath, text);
                Assert.Pass($"Snapshot written for {current.Count} modifiers: {GoldenPath}");
            }

            Assert.IsTrue(File.Exists(GoldenPath), $"No snapshot at {GoldenPath}: run once with SR_UPDATE_GOLDEN=1");
            var golden = File.ReadAllLines(GoldenPath).Where(l => l.Length > 0)
                .ToDictionary(l => l.Substring(0, l.IndexOf('\t')), l => l.Substring(l.IndexOf('\t') + 1));
            var failures = new List<string>();
            foreach (var pair in current)
                if (!golden.TryGetValue(pair.Key, out var stored)) failures.Add($"{pair.Key}: not in the snapshot");
                else if (stored != pair.Value) failures.Add($"{pair.Key}: the geometry changed");
            foreach (var key in golden.Keys.Where(k => !current.ContainsKey(k)))
                failures.Add($"{key}: in the snapshot but no longer generatable");
            Assert.IsEmpty(failures, "Meant? Write the snapshot again with SR_UPDATE_GOLDEN=1 and review the diff.");
        }

        // A valid 9×9 solution from the shifted-row pattern, with standard boxes and a third of the cells given (the
        // generators place their pieces over open cells): no generator budget or random board, so the snapshot depends on
        // the geometry code alone.
        private static SudokuBoard FixedBoard()
        {
            var solution = new int[9, 9];
            var cells = new int[9, 9];
            var regions = new int[9, 9];
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
            {
                solution[r, c] = (r * 3 + r / 3 + c) % 9 + 1;
                cells[r, c] = (r + 2 * c) % 3 == 0 ? solution[r, c] : 0;
                regions[r, c] = r / 3 * 3 + c / 3;
            }
            return new SudokuBoard(9, solution, cells, regions);
        }
    }
}
