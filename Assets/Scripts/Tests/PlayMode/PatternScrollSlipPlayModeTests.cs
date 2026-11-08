using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // RM-231, the owner's pick B: the Pattern Scroll on a paper slip.
    public sealed class PatternScrollSlipPlayModeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> _roots = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in _roots) if (root != null) Object.Destroy(root);
            _roots.Clear();
            yield return null;
        }

        private (GameObject host, InRunController screen, RunDirector run) OpenSlip(int charges)
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _roots.Add(new GameObject("SlipEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("PatternScrollSlipHost");
            _roots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, 7461);
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = 9;
            config.Stars = 3;
            config.ActiveModifiers.Clear();
            config.ActiveModifiers.Add(BossModifierId.Thermo);
            config.ActiveModifiers.Add(BossModifierId.GermanWhispers);
            run.StartLevel(config);
            var screen = host.GetComponentInChildren<InRunController>(true);
            host.GetComponentInChildren<RunMapController>(true).BindRun(run, new SudokuRoguelike.Save.SaveFileService(SudokuRoguelike.Save.SaveFileService.MaxSlots - 1));
            typeof(InRunController).GetField("_patternScrollPending", Hidden).SetValue(screen, true);
            typeof(InRunController).GetField("_patternScrollChargesLeft", Hidden).SetValue(screen, charges);
            typeof(InRunController).GetMethod("ShowPatternScrollSlip", Hidden).Invoke(screen, null);
            return (host, screen, run);
        }

        private static Button Named(GameObject host, string name) => host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

        private static HashSet<(int, int)> Gold(GameObject host)
        {
            var view = typeof(InRunController).GetField("_boardView", Hidden).GetValue(host.GetComponentInChildren<InRunController>(true));
            return (HashSet<(int, int)>)typeof(BoardViewController).GetField("_previewPieceCells", Hidden).GetValue(view);
        }

        [UnityTest]
        [Requirement("ITEM-SCROLL-002")]
        public IEnumerator TheSlip_ShowsTheChosenPieceInGold_AndSolveWritesIt()
        {
            var (host, _, run) = OpenSlip(2);
            yield return null;
            var pieces = InRunController.BuildPatternScrollMechanics(run.CurrentOverlay, run.CurrentLevelConfig.ActiveModifiers, 9);
            Assert.Greater(pieces.Count, 1);
            Assert.IsNotNull(GameObject.Find("PatternScrollOverlay"), "the slip is open");
            Assert.AreEqual(pieces.Count, host.GetComponentsInChildren<Button>(true).Count(b => b.name.StartsWith("MechBtn_")));
            Assert.IsTrue(Gold(host).SetEquals(pieces[0].cells.Select(c => (c.Row, c.Col))), "the first piece is chosen and gold");

            Named(host, "MechBtn_1").onClick.Invoke();
            yield return null;
            Assert.IsTrue(Gold(host).SetEquals(pieces[1].cells.Select(c => (c.Row, c.Col))), "another row moves the gold");
            var open = pieces[1].cells.Count(c => run.CurrentBoard.Cells[c.Row, c.Col] == 0);

            Named(host, "MechBtn_1").onClick.Invoke();
            yield return null;
            Assert.AreEqual(open, pieces[1].cells.Count(c => run.CurrentBoard.Cells[c.Row, c.Col] == 0), "clicking a chosen row writes nothing");

            Named(host, "BtnConfirmScroll").onClick.Invoke();
            yield return null;
            foreach (var c in pieces[1].cells)
                Assert.AreEqual(run.CurrentBoard.Solution[c.Row, c.Col], run.CurrentBoard.Cells[c.Row, c.Col], $"{c.Row},{c.Col} written ({open} were empty)");
            var row = Named(host, "MechBtn_1");
            Assert.IsNotNull(row, "the slip stays for the second charge");
            Assert.IsFalse(row.interactable, "a solved piece cannot be chosen again");
            Assert.IsNotNull(row.transform.Find("Strike"), "and is struck through");
            Assert.IsTrue(Gold(host).SetEquals(pieces[0].cells.Select(c => (c.Row, c.Col))), "the next open piece is chosen");
        }

        [UnityTest]
        [Requirement("ITEM-SCROLL-002")]
        public IEnumerator Cancel_ClosesTheSlip_AndClearsTheGold()
        {
            var (host, _, _) = OpenSlip(1);
            yield return null;
            Assert.IsNotEmpty(Gold(host));
            Named(host, "BtnCancelScroll").onClick.Invoke();
            yield return null;
            Assert.IsNull(GameObject.Find("PatternScrollOverlay"));
            Assert.IsEmpty(Gold(host));
        }
    }
}
