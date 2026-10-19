using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using UnityEngine;
using UnityEngine.TestTools;

namespace SudokuRoguelike.Tests
{
    // [REQ: GEN-MOD-002] [REQ: BOSS-MOD-005] [REQ: BOSS-MOD-006] Catalog batches B1 and B2 (modifier-catalog-plan.md):
    // every new rule on hand-made boards, every new modifier on generated boards (its geometry is there, the solution
    // never trips its rule while it is being filled in, a wrong digit is caught), and every playable modifier complete
    // in the catalog (name, rule text in five languages, abbreviation, icon, legend layer).
    [TestFixture]
    [Requirement("GEN-MOD-002", "BOSS-MOD-005", "BOSS-MOD-006")]
    public sealed class CatalogModifierTests
    {
        private static readonly BossModifierId[] Batch =
        {
            BossModifierId.NonconsecLine, BossModifierId.HighLowAlternating, BossModifierId.NabnerLine,
            BossModifierId.EntropicLine, BossModifierId.ModularLine, BossModifierId.ZipperLine, BossModifierId.FastThermo,
            BossModifierId.AmbiguousThermo, BossModifierId.FullWhiteKropki, BossModifierId.FullBlackKropki,
            BossModifierId.DifferenceK, BossModifierId.MultipleCells, BossModifierId.MirrorCells
        };

        // ── rules on hand-made boards (one row of a 9×9) ──

        private static SudokuBoard Empty(int size = 9)
        {
            var regions = new int[size, size];
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                regions[r, c] = size == 9 ? (r / 3) * 3 + c / 3 : r;
            return new SudokuBoard(size, new int[size, size], new int[size, size], regions);
        }

        private static ModifierOverlayData Line(LineType type, int length, int lineValue = 0)
        {
            var overlay = new ModifierOverlayData();
            var line = new ModifierLine { Type = type, LineValue = lineValue };
            for (var c = 0; c < length; c++) line.Cells.Add(new CellCoord(0, c));
            overlay.Lines.Add(line);
            return overlay;
        }

        private static SudokuBoard With(params int[] firstRow)
        {
            var board = Empty();
            for (var c = 0; c < firstRow.Length; c++) board.Cells[0, c] = firstRow[c];
            return board;
        }

        [Test]
        [Requirement("BOSS-LIB-038")]
        public void NonconsecutiveLine_RejectsConsecutiveNeighboursOnly()
        {
            var o = Line(LineType.NonconsecLine, 3);
            var rule = new NonconsecLineRule();
            Assert.IsFalse(rule.IsValid(With(5), 0, 1, 6, o));
            Assert.IsFalse(rule.IsValid(With(5), 0, 1, 4, o));
            Assert.IsTrue(rule.IsValid(With(5), 0, 1, 7, o));
            Assert.IsTrue(rule.IsValid(With(5, 0, 6), 0, 1, 1, o), "the far end is not a neighbour of the start");
        }

        [Test]
        [Requirement("BOSS-LIB-040")]
        public void HighLowLine_AlternatesHalves_FiveCountsAsLowOn9x9()
        {
            var o = Line(LineType.HighLowAlternating, 4);
            var rule = new HighLowLineRule();
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 7, o));
            Assert.IsFalse(rule.IsValid(With(2), 0, 1, 5, o), "5 is low on 9×9, so it cannot follow a low digit");
            Assert.IsTrue(rule.IsValid(With(2), 0, 2, 4, o), "two steps away: same half");
            Assert.IsFalse(rule.IsValid(With(2), 0, 3, 3, o), "three steps away: other half");
        }

        [Test]
        [Requirement("BOSS-LIB-041")]
        public void NabnerLine_RejectsRepeatsAndConsecutiveAnywhereOnTheLine()
        {
            var o = Line(LineType.NabnerLine, 4);
            var rule = new NabnerLineRule();
            Assert.IsFalse(rule.IsValid(With(4), 0, 3, 5, o), "consecutive with a far cell");
            Assert.IsFalse(rule.IsValid(With(4), 0, 3, 4, o), "repeat");
            Assert.IsTrue(rule.IsValid(With(4), 0, 3, 6, o));
        }

        [Test]
        [Requirement("BOSS-LIB-042", "BOSS-LIB-043")]
        public void TriadLines_GroupRepeatsEveryThirdCell()
        {
            var entropic = Line(LineType.EntropicLine, 4);
            var e = new TriadLineRule(LineType.EntropicLine);
            Assert.IsTrue(e.IsValid(With(2, 5), 0, 2, 8, entropic));
            Assert.IsFalse(e.IsValid(With(2, 5), 0, 2, 6, entropic), "6 and 5 are both middle digits");
            Assert.IsTrue(e.IsValid(With(2, 5, 8), 0, 3, 1, entropic), "three apart: same low group");
            Assert.IsFalse(e.IsValid(With(2, 5, 8), 0, 3, 7, entropic));

            var modular = Line(LineType.ModularLine, 4);
            var m = new TriadLineRule(LineType.ModularLine);
            Assert.IsTrue(m.IsValid(With(1, 5), 0, 2, 9, modular));
            Assert.IsFalse(m.IsValid(With(1, 5), 0, 2, 4, modular), "4 has the remainder of 1");
            Assert.IsTrue(m.IsValid(With(1, 5, 9), 0, 3, 7, modular));
        }

        [Test]
        [Requirement("BOSS-LIB-044")]
        public void ZipperLine_PairsAddUpToTheMiddle()
        {
            var odd = Line(LineType.ZipperLine, 5);
            var rule = new ZipperLineRule();
            Assert.IsTrue(rule.IsValid(With(3, 1, 7, 6), 0, 4, 4, odd), "3 + 4 = 7");
            Assert.IsFalse(rule.IsValid(With(3, 1, 7, 6), 0, 4, 5, odd));
            Assert.IsFalse(rule.IsValid(With(0, 0, 7), 0, 1, 7, odd), "a digit equal to the middle leaves nothing for its partner");
            Assert.IsFalse(rule.IsValid(With(3, 0, 0, 0, 4), 0, 2, 8, odd), "the middle must equal the outer pair's total");

            var even = Line(LineType.ZipperLine, 4);
            Assert.IsTrue(rule.IsValid(With(2, 4, 3), 0, 3, 5, even), "2 + 5 = 4 + 3");
            Assert.IsFalse(rule.IsValid(With(2, 4, 3), 0, 3, 6, even));
        }

        [Test]
        [Requirement("BOSS-LIB-051")]
        public void FastThermo_ClimbsByAtLeastTwo_AndLeavesRoomAtTheEnds()
        {
            var o = Line(LineType.FastThermo, 4, 2);
            var rule = new FastThermoRule();
            Assert.IsTrue(rule.IsValid(With(1), 0, 1, 3, o));
            Assert.IsFalse(rule.IsValid(With(1), 0, 1, 2, o), "only one higher");
            Assert.IsFalse(rule.IsValid(Empty(), 0, 0, 4, o), "4 at the bulb leaves no room: 4-6-8-10");
            Assert.IsFalse(rule.IsValid(Empty(), 0, 3, 6, o), "6 at the tip needs 0 at the bulb");
        }

        [Test]
        [Requirement("BOSS-LIB-052")]
        public void AmbiguousThermo_AcceptsEitherDirectionUntilTheDigitsDecide()
        {
            var o = Line(LineType.AmbiguousThermo, 4);
            var rule = new AmbiguousThermoRule();
            Assert.IsTrue(rule.IsValid(With(7), 0, 1, 5, o), "falling from the start");
            Assert.IsTrue(rule.IsValid(With(2), 0, 1, 5, o), "rising from the start");
            Assert.IsFalse(rule.IsValid(With(7, 5), 0, 2, 6, o), "the first two decided: falling");
            Assert.IsFalse(rule.IsValid(With(7, 0), 0, 2, 6, o), "two steps need a difference of at least 2");
            Assert.IsFalse(rule.IsValid(Empty(), 0, 1, 9, o), "9 in the second cell leaves no room on either side");
        }

        [Test]
        [Requirement("BOSS-LIB-058", "BOSS-LIB-059")]
        public void FullKropkiSets_ForbidTheRelationWhereTheDotIsMissing()
        {
            var white = new ModifierOverlayData { FullWhiteKropkiNeg = true };
            white.KropkiDots.Add(new KropkiDot { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1) });
            var whiteRule = new FullKropkiSetRule(black: false);
            Assert.IsTrue(whiteRule.IsValid(With(4), 0, 1, 5, white), "the dot is there");
            Assert.IsFalse(whiteRule.IsValid(With(0, 4), 0, 2, 5, white), "no dot between (0,1) and (0,2)");
            Assert.IsTrue(whiteRule.IsValid(With(0, 4), 0, 2, 8, white), "8 and 4 are fine for a white set");

            var black = new ModifierOverlayData { FullBlackKropkiNeg = true };
            var blackRule = new FullKropkiSetRule(black: true);
            Assert.IsFalse(blackRule.IsValid(With(4), 0, 1, 8, black));
            Assert.IsTrue(blackRule.IsValid(With(4), 0, 1, 5, black), "5 and 4 are fine for a black set");
        }

        [Test]
        [Requirement("BOSS-LIB-060", "BOSS-LIB-074", "BOSS-LIB-076")]
        public void DifferenceMultiplesAndMirror_CheckTheirCells()
        {
            var pairs = new ModifierOverlayData();
            pairs.PairConstraints.Add(new AdjacentPairConstraint
                { CellA = new CellCoord(0, 0), CellB = new CellCoord(0, 1), Type = PairConstraintType.DifferenceK, Value = 3 });
            var diff = new DifferenceKPairRule();
            Assert.IsTrue(diff.IsValid(With(2), 0, 1, 6, pairs));
            Assert.IsFalse(diff.IsValid(With(2), 0, 1, 4, pairs));
            Assert.IsFalse(diff.IsValid(Empty(5), 0, 0, 3, pairs), "3 on 5×5: no digit is 3 away");

            var markers = new ModifierOverlayData();
            markers.CellMarkers.Add(new CellMarker { Cell = new CellCoord(1, 1), Type = MarkerType.Multiple });
            var mult = new MultipleCellsRule();
            Assert.IsTrue(mult.IsValid(Empty(), 1, 1, 6, markers));
            Assert.IsFalse(mult.IsValid(Empty(), 1, 1, 4, markers));

            var mirror = new ModifierOverlayData();
            mirror.CellPairs.Add(new CellPairConstraint { CellA = new CellCoord(0, 0), CellB = new CellCoord(8, 8) });
            var board = Empty();
            board.Cells[8, 8] = 3;
            Assert.IsTrue(new MirrorCellsRule().IsValid(board, 0, 0, 3, mirror));
            Assert.IsFalse(new MirrorCellsRule().IsValid(board, 0, 0, 4, mirror));
        }

        // ── generated boards ──

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;   // generation fallbacks log warnings by design

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [Test]
        public void EveryNewModifier_GeneratesItsGeometry_AndTheSolutionNeverTripsItsRule()
        {
            var failures = new List<string>();
            foreach (var mod in Batch)
            {
                var minimum = BossService.GetMinimumBoardSize(mod);
                foreach (var size in new[] { minimum, 9 }.Distinct())
                foreach (var seed in new[] { 11, 22, 33 })
                {
                    var run = StartLevel(mod, size, seed * 100 + size);
                    var board = run.CurrentBoard;
                    if (board == null) { failures.Add($"{mod} {size}×{size} seed {seed}: no board"); continue; }
                    if (!ModifierPresence.IsPresent(mod, run.CurrentOverlay))
                    {
                        failures.Add($"{mod} {size}×{size} seed {seed}: no geometry");
                        continue;
                    }
                    var rules = ModifierFactory.BuildRules(new List<BossModifierId> { mod });
                    var problem = FillCheckingRules(board, run.CurrentOverlay, rules, new System.Random(seed))
                                  ?? CheckEveryMarkedCellCatchesAWrongDigit(mod, board, run.CurrentOverlay, rules)
                                  ?? CheckCompleteDotSet(mod, board, run.CurrentOverlay);
                    if (problem != null) failures.Add($"{mod} {size}×{size} seed {seed}: {problem}");
                }
            }
            Assert.IsEmpty(failures);
        }

        private static RunDirector StartLevel(BossModifierId mod, int size, int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
                { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, seed);
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = size;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            config.ActiveModifiers.Add(mod);
            run.StartLevel(config);
            return run;
        }

        // Fills the empty cells with the solution in a random order, checking each digit against the rules first; then
        // tries one wrong digit per cell on the full board and expects the rules to catch at least one.
        private static string FillCheckingRules(SudokuBoard board, ModifierOverlayData overlay,
            List<IOrderedConstraintRule> rules, System.Random rng)
        {
            var size = board.Size;
            var work = new SudokuBoard(size, board.Solution, (int[,])board.Cells.Clone(), board.RegionMap);
            var empty = new List<(int r, int c)>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (work.Cells[r, c] == 0) empty.Add((r, c));
            for (var i = empty.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (empty[i], empty[j]) = (empty[j], empty[i]);
            }
            foreach (var (r, c) in empty)
            {
                var digit = board.Solution[r, c];
                foreach (var rule in rules)
                    if (!rule.IsValid(work, r, c, digit, overlay))
                        return $"{rule.GetType().Name} rejected the solution's {digit} at ({r},{c})";
                work.Cells[r, c] = digit;
            }

            return null;
        }

        // On the solved board, every cell the modifier marks (line cells, pair cells, marked cells, mirror cells, dot
        // cells) must turn away at least one wrong digit; a rule that checks nothing there would pass the fill test.
        private static string CheckEveryMarkedCellCatchesAWrongDigit(BossModifierId mod, SudokuBoard board,
            ModifierOverlayData overlay, List<IOrderedConstraintRule> rules)
        {
            var size = board.Size;
            var solved = new SudokuBoard(size, board.Solution, (int[,])board.Solution.Clone(), board.RegionMap);
            foreach (var cell in MarkedCells(mod, overlay))
            {
                var caught = false;
                for (var wrong = 1; wrong <= size && !caught; wrong++)
                {
                    if (wrong == board.Solution[cell.Row, cell.Col]) continue;
                    foreach (var rule in rules)
                        if (!rule.IsValid(solved, cell.Row, cell.Col, wrong, overlay)) { caught = true; break; }
                }
                if (!caught) return $"no wrong digit is caught at marked cell ({cell.Row},{cell.Col})";
            }
            return null;
        }

        private static IEnumerable<CellCoord> MarkedCells(BossModifierId mod, ModifierOverlayData overlay)
        {
            switch (mod)
            {
                case BossModifierId.DifferenceK:
                    foreach (var p in overlay.PairConstraints.Where(p => p.Type == PairConstraintType.DifferenceK))
                    { yield return p.CellA; yield return p.CellB; }
                    break;
                case BossModifierId.MultipleCells:
                    foreach (var m in overlay.CellMarkers.Where(m => m.Type == MarkerType.Multiple)) yield return m.Cell;
                    break;
                case BossModifierId.MirrorCells:
                    foreach (var p in overlay.CellPairs) { yield return p.CellA; yield return p.CellB; }
                    break;
                case BossModifierId.FullWhiteKropki:
                case BossModifierId.FullBlackKropki:
                    foreach (var d in overlay.KropkiDots) { yield return d.CellA; yield return d.CellB; }
                    break;
                default:
                    foreach (var line in overlay.Lines)
                        foreach (var c in line.Cells) yield return c;
                    break;
            }
        }

        // A complete set has its dot on every neighbour pair that qualifies; a missing dot would let a wrong digit in.
        private static string CheckCompleteDotSet(BossModifierId mod, SudokuBoard board, ModifierOverlayData overlay)
        {
            if (mod != BossModifierId.FullWhiteKropki && mod != BossModifierId.FullBlackKropki) return null;
            var black = mod == BossModifierId.FullBlackKropki;
            var size = board.Size;
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            foreach (var (r2, c2) in new[] { (r, c + 1), (r + 1, c) })
            {
                if (r2 >= size || c2 >= size) continue;
                int a = board.Solution[r, c], b = board.Solution[r2, c2];
                var qualifies = black ? Math.Max(a, b) == 2 * Math.Min(a, b) : Math.Abs(a - b) == 1;
                if (!qualifies) continue;
                var has = overlay.KropkiDots.Exists(d => d.IsBlack == black && d.SumValue == 0 &&
                    ((d.CellA.Row == r && d.CellA.Col == c && d.CellB.Row == r2 && d.CellB.Col == c2) ||
                     (d.CellB.Row == r && d.CellB.Col == c && d.CellA.Row == r2 && d.CellA.Col == c2)));
                if (!has) return $"the {(black ? "black" : "white")} set misses the dot between ({r},{c}) and ({r2},{c2})";
            }
            return null;
        }

        // ── saves and combinations ──

        // Saves store modifier IDs as numbers; the catalog batch must keep its enum values for good.
        [Test]
        [Requirement("BOSS-MOD-005")]
        public void CatalogIds_KeepTheirEnumValues()
        {
            var expected = new (BossModifierId id, int value)[]
            {
                (BossModifierId.NonconsecLine, 38), (BossModifierId.HighLowAlternating, 40), (BossModifierId.NabnerLine, 41),
                (BossModifierId.EntropicLine, 42), (BossModifierId.ModularLine, 43), (BossModifierId.ZipperLine, 44),
                (BossModifierId.FastThermo, 51), (BossModifierId.AmbiguousThermo, 52), (BossModifierId.FullWhiteKropki, 58),
                (BossModifierId.FullBlackKropki, 59), (BossModifierId.DifferenceK, 60), (BossModifierId.MultipleCells, 74),
                (BossModifierId.MirrorCells, 76)
            };
            foreach (var (id, value) in expected) Assert.AreEqual(value, (int)id, id.ToString());
            Assert.AreEqual(11, (int)LineType.NonconsecLine);
            Assert.AreEqual(17, (int)LineType.ZipperLine);
            Assert.AreEqual(24, (int)LineType.FastThermo);
            Assert.AreEqual(25, (int)LineType.AmbiguousThermo);
            Assert.AreEqual(4, (int)MarkerType.Multiple);
            Assert.AreEqual(5, (int)PairConstraintType.DifferenceK);
        }

        // One thermometer kind per puzzle, never two dots on one edge, no plain grey line next to an Ambiguous Thermo.
        [Test]
        public void NewBlockedPairs_AreNeverCombined()
        {
            var blocked = new[]
            {
                (BossModifierId.FastThermo, BossModifierId.Thermo), (BossModifierId.FastThermo, BossModifierId.SlowThermo),
                (BossModifierId.FastThermo, BossModifierId.AmbiguousThermo), (BossModifierId.AmbiguousThermo, BossModifierId.Thermo),
                (BossModifierId.AmbiguousThermo, BossModifierId.SlowThermo), (BossModifierId.AmbiguousThermo, BossModifierId.Palindrome),
                (BossModifierId.FullWhiteKropki, BossModifierId.FullKropki), (BossModifierId.FullWhiteKropki, BossModifierId.FullBlackKropki),
                (BossModifierId.FullWhiteKropki, BossModifierId.DifferenceKropki), (BossModifierId.FullWhiteKropki, BossModifierId.RatioKropki),
                (BossModifierId.FullWhiteKropki, BossModifierId.SumKropki), (BossModifierId.FullWhiteKropki, BossModifierId.Nonconsecutive),
                (BossModifierId.FullBlackKropki, BossModifierId.FullKropki), (BossModifierId.FullBlackKropki, BossModifierId.RatioKropki),
                (BossModifierId.FullBlackKropki, BossModifierId.DifferenceKropki), (BossModifierId.FullBlackKropki, BossModifierId.SumKropki),
                (BossModifierId.Nonconsecutive, BossModifierId.DifferenceKropki)
            };
            foreach (var (a, b) in blocked)
            {
                Assert.IsFalse(BossService.CanCombineModifiers(new List<BossModifierId> { a }, b), $"{a} + {b}");
                Assert.IsFalse(BossService.CanCombineModifiers(new List<BossModifierId> { b }, a), $"{b} + {a}");
            }
            Assert.IsTrue(BossService.CanCombineModifiers(new List<BossModifierId> { BossModifierId.ZipperLine }, BossModifierId.Thermo));
        }

        // ── catalog completeness ──

        [Test]
        public void EveryPlayableModifier_HasNameRuleAbbreviationIconAndLegend()
        {
            var catalogs = new[] { "en", "de", "fr", "es", "pt" }
                .ToDictionary(l => l, l => File.ReadAllText(Path.Combine("Assets", "Resources", "Localization", l + ".json")));
            var abbreviations = new Dictionary<string, BossModifierId>();
            var failures = new List<string>();
            foreach (var mod in BossService.GetGeneratablePlayerModifiers(9))
            {
                if (string.IsNullOrWhiteSpace(BossService.GetModifierName(mod))) failures.Add($"{mod}: no name");
                if (string.IsNullOrEmpty(BossService.GetModifierDescription(mod))) failures.Add($"{mod}: no rule text");
                var abbreviation = BossService.GetConstraintAbbreviation(mod);
                if (abbreviation == "??") failures.Add($"{mod}: no abbreviation");
                else if (abbreviations.TryGetValue(abbreviation, out var other)) failures.Add($"{mod}: abbreviation {abbreviation} also used by {other}");
                else abbreviations[abbreviation] = mod;
                if (Resources.Load<Texture2D>("modifier/icon_" + BossService.GetIconName(mod)) == null) failures.Add($"{mod}: no icon");

                // Rule texts for the new batch must exist in every catalog (older ones may rely on the English fallback).
                if (Array.IndexOf(Batch, mod) >= 0)
                    foreach (var pair in catalogs)
                    {
                        if (!pair.Value.Contains($"\"Boss.Name.{mod}\"")) failures.Add($"{mod}: no name in {pair.Key}.json");
                        if (!pair.Value.Contains($"\"Boss.Description.{mod}\"")) failures.Add($"{mod}: no rule text in {pair.Key}.json");
                    }

                // Fog of War is drawn on the cells themselves and has no legend layer by design.
                var global = mod == BossModifierId.FogOfWar || ModifierPresence.IsPresent(mod, new ModifierOverlayData());
                if (!global && SudokuRoguelike.UI.BoardViewController.GetOverlayGroupKeys(mod).Count == 0)
                    failures.Add($"{mod}: no legend layer");
                if (!global && !SudokuRoguelike.UI.BoardViewController.TryGetLegendColor(mod, out _))
                    failures.Add($"{mod}: no legend colour");
            }
            Assert.IsEmpty(failures);
        }
    }
}
