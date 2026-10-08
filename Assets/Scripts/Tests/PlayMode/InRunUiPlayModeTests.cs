using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // Drives the real in-run UI (InRunUiBlueprintBuilder + InRunController) for the 1.1 UI work:
    // [REQ: REQ-BOSS-104] modifier legend + tap-to-isolate (GAP-025)
    // [REQ: REQ-BOSS-103] [REQ: REQ-BOSS-108] boss gate intensity label and boss debuff (GAP-025, GAP-026)
    // [REQ: REQ-RUN-101] in-run menu: seed, View Modifiers, Abandon Run (GAP-020)
    // [REQ: PRESSURE-UI-001..011] pressure visuals verified on the rendered board (SPEC-CONF-020)
    [TestFixture]
    [Requirement("REQ-BOSS-104", "REQ-BOSS-103", "REQ-BOSS-108", "REQ-RUN-101")]
    public sealed class InRunUiPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;

        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            DeleteSlotFiles();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            for (var i = _createdRoots.Count - 1; i >= 0; i--)
                if (_createdRoots[i] != null) Object.Destroy(_createdRoots[i]);
            _createdRoots.Clear();
            Time.timeScale = 1f;
            DeleteSlotFiles();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        // ── Legend (GAP-025) ──

        [UnityTest]
        public IEnumerator Legend_ListsTheModifiers_AndTapIsolatesOneLayer()
        {
            var h = StartPuzzle(8601, BossModifierId.GermanWhispers, BossModifierId.KillerCages);
            yield return null;
            yield return null;

            var legendButton = FindObject(h.Host, "BtnModifierLegend");
            Assert.IsTrue(legendButton.activeInHierarchy, "legend button is shown for a puzzle with modifiers");
            legendButton.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var panel = FindObject(h.Host, "ModifierLegendPanel");
            Assert.IsTrue(panel.activeInHierarchy);
            Assert.NotNull(FindObjectOrNull(panel, "LegendEntry_GermanWhispers"));
            Assert.NotNull(FindObjectOrNull(panel, "LegendEntry_KillerCages"));

            var board = h.Host.GetComponentInChildren<BoardViewController>(true);
            Assert.AreEqual(1f, board.GetOverlayGroupAlpha("Line.GermanWhispers"), "whisper lines were drawn into their layer");
            Assert.AreEqual(1f, board.GetOverlayGroupAlpha("Cages"), "cages were drawn into their layer");

            ClickButton(panel, "LegendEntry_GermanWhispers");
            yield return null;
            Assert.AreEqual(1f, board.GetOverlayGroupAlpha("Line.GermanWhispers"));
            Assert.AreEqual(BoardViewController.IsolatedOthersAlpha, board.GetOverlayGroupAlpha("Cages"), 0.001f);

            ClickButton(panel, "LegendEntry_GermanWhispers");
            yield return null;
            Assert.AreEqual(1f, board.GetOverlayGroupAlpha("Cages"), "tapping the entry again restores all layers");

            ClickButton(panel, "LegendEntry_KillerCages");
            yield return new WaitForSeconds(BoardViewController.IsolationSeconds + 0.3f);
            Assert.AreEqual(1f, board.GetOverlayGroupAlpha("Line.GermanWhispers"), "isolation ends after 3 s");
        }

        // ── Boss gate (GAP-025 / GAP-026) ──

        [UnityTest]
        [Requirement("DEBUFF-DELIV-004")]
        public IEnumerator BossGate_ShowsIntensity_AndTheDebuffFromFloorThree()
        {
            var h = StartPuzzle(8602);
            h.Run.State.CurrentFloor = 2;
            h.Run.State.RunNumber = 9;
            yield return null;

            h.Host.GetComponentInChildren<BossGateViewController>(true).ShowBossGateChoice();
            yield return null;

            var info = FindObject(h.Host, "GateInfo").GetComponent<Text>();
            StringAssert.Contains(BossService.GetIntensityName(BossModifierIntensity.VeryHigh), info.text);
            var debuff = h.Run.GetBossDebuffForCurrentFloor();
            Assert.IsTrue(debuff.HasValue);
            StringAssert.Contains(BossService.GetModifierName(debuff.Value), info.text);
        }

        [UnityTest]
        [Requirement("HARMONY-FLAG-001")]
        public IEnumerator BossGate_BelowHarmony5_RevealsSeenModifiers() => GateMasking(4, hidden: false);

        [UnityTest]
        [Requirement("HARMONY-FLAG-001")]
        public IEnumerator BossGate_FromHarmony5_HidesEvenSeenModifiers() => GateMasking(5, hidden: true);

        private IEnumerator GateMasking(int harmony, bool hidden)
        {
            var h = StartPuzzle(8610 + harmony);
            h.Run.State.HarmonyLevel = harmony;
            foreach (BossModifierId id in Enum.GetValues(typeof(BossModifierId)))
                h.Run.State.SeenBossModifiers.Add(id);
            yield return null;

            h.Host.GetComponentInChildren<BossGateViewController>(true).ShowBossGateChoice();
            yield return null;

            var texts = FindObject(h.Host, "Mod_0").GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
            Assert.AreEqual(hidden, texts.Any(t => t == "???"), $"H{harmony}: {string.Join(" | ", texts)}");
        }

        // ── Path map (GAP-022) ──

        [UnityTest]
        [Requirement("REQ-PATH-100", "MAP-CROSS-001")]
        public IEnumerator PathMap_MarksThePlayersTile_AndBadgesBothCrossLinkTiles()
        {
            var h = StartPathMap(8604);
            yield return null;
            yield return null;

            var markers = FindAll(h.Host, "PlayerMarker");
            Assert.AreEqual(1, markers.Count, "exactly one player marker");
            Assert.IsTrue(markers[0].activeInHierarchy);

            var crossLinks = h.Run.CurrentFloorGraph.Count(n => n.IsCrossLink);
            Assert.AreEqual(crossLinks, FindAll(h.Host, "CrossLinkBadge").Count, "one badge per cross-link tile");
        }

        // ── In-run menu (GAP-020) ──

        [UnityTest]
        public IEnumerator RunMenu_ShowsSeedAndModifiers_AndAbandonEndsTheRun()
        {
            var h = StartPuzzle(8603, BossModifierId.GermanWhispers);
            yield return null;

            ClickButton(h.Host, "BtnSudokuOptions");
            yield return null;
            var options = FindObject(h.Host, "InGameOptionsPanel");
            Assert.IsTrue(options.activeInHierarchy);
            StringAssert.Contains(h.Run.State.Seed.ToString(), FindObject(options, "IGRunSeed").GetComponent<Text>().text);

            ClickButton(options, "BtnInGameOptionsModifiers");
            yield return null;
            var list = FindObject(options, "ModifierList");
            Assert.AreEqual(1f, list.GetComponent<Image>().color.a, 0.001f, "the list must hide the options underneath");
            StringAssert.Contains(BossService.GetModifierName(BossModifierId.GermanWhispers),
                FindObject(list, "Body").GetComponent<Text>().text);
            ClickButton(list, "BtnModifierListBack");
            yield return null;
            Assert.IsNull(FindObjectOrNull(options, "ModifierList"));

            ClickButton(options, "BtnInGameOptionsAbandon");
            yield return null;
            Assert.IsTrue(options.activeInHierarchy, "the first press only asks for confirmation");
            ClickButton(options, "BtnInGameOptionsAbandon");
            yield return null;
            yield return null;

            Assert.IsFalse(options.activeInHierarchy);
            Assert.IsTrue(FindObject(h.Host, "GameOverPanel").activeInHierarchy, "Abandon Run shows the defeat screen");
        }

        // ── Pressure UI (SPEC-CONF-020) ──

        [UnityTest]
        [Requirement("PRESSURE-UI-002", "PRESSURE-UI-003")]
        public IEnumerator PressureBadge_ShowsRemainingPlacements_ColouredByRatio()
        {
            var h = StartPuzzle(8611);
            var (target, other) = TwoEmptyCells(h.Run);
            var threat = AddThreat(h.Run, ThreatScope.SingleCell, new[] { target }, remaining: 4, initial: 4);
            yield return Select(h, other);

            var badge = Badge(h, target);
            Assert.AreEqual("4", badge.text);
            AssertColor(new Color(0.70f, 0.70f, 0.70f, 0.90f), badge.color, "> 50 %: grey");

            threat.RemainingPlacements = 2;
            yield return Select(h, other);
            AssertColor(new Color(1.00f, 0.65f, 0.10f, 0.90f), Badge(h, target).color, "25–50 %: amber");

            threat.RemainingPlacements = 1;
            yield return Select(h, other);
            AssertColor(new Color(0.95f, 0.20f, 0.20f, 0.90f), Badge(h, target).color, "< 25 %: red");
        }

        [UnityTest]
        [Requirement("PRESSURE-UI-001")]
        public IEnumerator PressureVisuals_AreHiddenInTheBasicsTutorial()
        {
            var h = StartPuzzle(8612);
            var (target, other) = TwoEmptyCells(h.Run);
            AddThreat(h.Run, ThreatScope.SingleCell, new[] { target }, remaining: 3, initial: 3);
            h.Run.State.RunNumber = 0;
            yield return Select(h, other);
            Assert.AreEqual("", Badge(h, target).text);
        }

        [UnityTest]
        [Requirement("PRESSURE-UI-004")]
        public IEnumerator ThreatExpiry_FlashesTheCellsRed()
        {
            var h = StartPuzzle(8613);
            var (target, other) = TwoEmptyCells(h.Run);
            AddThreat(h.Run, ThreatScope.SingleCell, new[] { target }, remaining: 1, initial: 3);
            var hp = h.Run.State.CurrentHP;

            yield return PlaceCorrect(h, other);

            Assert.Less(h.Run.State.CurrentHP, hp, "expiry costs HP");
            Assert.IsNotEmpty(Flashes(h), "expired cells flash");
        }

        [UnityTest]
        [Requirement("PRESSURE-UI-005")]
        public IEnumerator ChainSuccess_FlashesGold()
        {
            var h = StartPuzzle(8614);
            var (target, _) = TwoEmptyCells(h.Run);
            var threat = AddThreat(h.Run, ThreatScope.Chain, new[] { target }, remaining: 5, initial: 5);
            threat.IsChain = true;

            yield return PlaceCorrect(h, target);

            Assert.IsNotEmpty(Flashes(h), "a completed chain flashes");
        }

        [UnityTest]
        [Requirement("PRESSURE-UI-006", "PRESSURE-UI-007")]
        public IEnumerator HauntedAndCrumblingCells_AreTinted()
        {
            var h = StartPuzzle(8615);
            var cells = EmptyCells(h.Run).Take(4).ToList();
            var (plain, haunted, crumbling, select) = (cells[0], cells[1], cells[2], cells[3]);
            yield return Select(h, select);
            var plainColor = CellImage(h, plain).color;

            h.Run.CurrentLevelState.HauntedCell = haunted;
            h.Run.CurrentLevelState.CrumblingCells[crumbling.row * 100 + crumbling.col] = 0;
            h.Run.CurrentLevelState.CrumblingStartFragility = 4;
            yield return Select(h, select);

            Assert.AreNotEqual(plainColor, CellImage(h, haunted).color, "haunted cell pulses purple");
            var crack = CellImage(h, crumbling).color;
            Assert.Greater(crack.r, crack.b, "a fully crumbled cell is tinted brown");
        }

        [UnityTest]
        [Requirement("PRESSURE-UI-008", "PRESSURE-UI-009")]
        public IEnumerator AutoFillAndWaveSpawn_PlayTheirAnimations()
        {
            var h = StartPuzzle(8616);
            var (flashCell, place) = TwoEmptyCells(h.Run);
            h.Run.CurrentLevelState.CrumbledFlashCells.Add(flashCell);
            h.Run.CurrentLevelState.WaveRipplePending = true;

            yield return PlaceCorrect(h, place);

            Assert.IsNotEmpty(Flashes(h), "an auto-filled cell flashes");
            Assert.NotNull(FindObjectOrNull(h.Host, "WaveRipple"), "a wave spawn plays the ripple");
        }

        [UnityTest]
        [Requirement("PRESSURE-UI-010", "PRESSURE-UI-011")]
        public IEnumerator RegionAndChainThreats_DrawStripsAndLines()
        {
            var h = StartPuzzle(8617);
            var empty = EmptyCells(h.Run);
            var row = empty[0].row;
            AddThreat(h.Run, ThreatScope.Row, empty.Where(c => c.row == row).ToArray(), remaining: 6, initial: 6);
            var chain = AddThreat(h.Run, ThreatScope.Chain, empty.Where(c => c.row != row).Take(3).ToArray(), remaining: 6, initial: 6);
            chain.IsChain = true;

            yield return Select(h, empty.Last());

            Assert.NotNull(FindObjectOrNull(h.Host, "PressureStrip"), "row threats draw a border strip");
            Assert.NotNull(FindObjectOrNull(h.Host, "PressureLine"), "chain threats draw a line");
        }

        // ── harness ──

        private sealed class Harness
        {
            public GameObject Host;
            public RunDirector Run;
        }

        private Harness StartPathMap(int seed)
        {
            var host = BuildHost();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            var map = host.GetComponentInChildren<RunMapController>(true);
            map.BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return new Harness { Host = host, Run = run };
        }

        private static List<GameObject> FindAll(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).Select(t => t.gameObject).ToList();

        private GameObject BuildHost()
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("InRunUiCamera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                camera.GetComponent<Camera>().orthographic = true;
                _createdRoots.Add(camera);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("InRunUiEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));

            var host = new GameObject("InRunUiHost");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            return host;
        }

        private Harness StartPuzzle(int seed, params BossModifierId[] modifiers)
        {
            var host = BuildHost();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1
            }, seed);
            run.State.HeldRelics.Clear();
            run.State.CurrentHP = run.State.MaxHP = 20;
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = 6;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            config.ActiveModifiers.AddRange(modifiers);
            run.StartLevel(config);
            Assert.NotNull(run.CurrentBoard, "puzzle generation failed for the test seed");

            var map = host.GetComponentInChildren<RunMapController>(true);
            map.BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return new Harness { Host = host, Run = run };
        }

        private static PressureThreat AddThreat(RunDirector run, ThreatScope scope, (int row, int col)[] cells,
            int remaining, int initial)
        {
            var threat = new PressureThreat
            {
                Scope = scope,
                RemainingPlacements = remaining,
                InitialPlacements = initial
            };
            threat.Cells.AddRange(cells);
            run.CurrentLevelState.ActiveThreats.Add(threat);
            return threat;
        }

        private static IEnumerator Select(Harness h, (int row, int col) cell)
        {
            // The board is built a frame or two after the run is bound.
            var name = $"C_{cell.row}_{cell.col}";
            for (var frame = 0; frame < 30 && FindObjectOrNull(h.Host, name) == null; frame++)
                yield return null;
            ClickButton(h.Host, name);
            yield return null;
        }

        private static IEnumerator PlaceCorrect(Harness h, (int row, int col) cell)
        {
            yield return Select(h, cell);
            // InRunController ignores digits for 0.5 s after a cell is selected (touch safety window).
            yield return new WaitForSecondsRealtime(0.6f);
            ClickButton(h.Host, $"Num_{h.Run.CurrentBoard.Solution[cell.row, cell.col]}");
            yield return null;
        }

        private static List<(int row, int col)> EmptyCells(RunDirector run)
        {
            var board = run.CurrentBoard;
            var cells = new List<(int, int)>();
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0 && !board.IsGiven(r, c))
                    cells.Add((r, c));
            return cells;
        }

        private static ((int row, int col) a, (int row, int col) b) TwoEmptyCells(RunDirector run)
        {
            var cells = EmptyCells(run);
            Assert.GreaterOrEqual(cells.Count, 2);
            return (cells[0], cells[cells.Count - 1]);
        }

        // The countdown badge is the "Badge" label in the cell's number layer N_{row}_{col}.
        private static Text Badge(Harness h, (int row, int col) cell)
        {
            var layer = FindObject(h.Host, $"N_{cell.row}_{cell.col}");
            var badge = FindObject(layer, "Badge").GetComponent<Text>();
            Assert.NotNull(badge, $"no badge label on N_{cell.row}_{cell.col}");
            return badge;
        }

        private static Image CellImage(Harness h, (int row, int col) cell) =>
            FindObject(h.Host, $"C_{cell.row}_{cell.col}").GetComponent<Image>();

        private static List<GameObject> Flashes(Harness h) =>
            h.Host.GetComponentsInChildren<Transform>(true).Where(t => t.name == "PressureFlash").Select(t => t.gameObject).ToList();

        private static void AssertColor(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, message);
            Assert.AreEqual(expected.g, actual.g, 0.01f, message);
            Assert.AreEqual(expected.b, actual.b, 0.01f, message);
        }

        private static void ClickButton(GameObject root, string name)
        {
            var go = FindObjectOrNull(root, name);
            Assert.NotNull(go, $"Button not found: {name}");
            Assert.IsTrue(go.activeInHierarchy, $"Button is not active in hierarchy: {name}");
            var button = go.GetComponent<Button>();
            Assert.IsTrue(button.interactable, $"Button is not interactable: {name}");
            button.onClick.Invoke();
        }

        private static GameObject FindObject(GameObject root, string name)
        {
            var go = FindObjectOrNull(root, name);
            Assert.NotNull(go, $"Generated object not found: {name}");
            return go;
        }

        private static GameObject FindObjectOrNull(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        private static void DeleteSlotFiles()
        {
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }
    }
}
