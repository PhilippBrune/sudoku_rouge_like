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

        // ── Region borders (GAP-044) ──

        [UnityTest]
        [Requirement("GEN-REGION-004")]
        public IEnumerator RegionBorders_FollowTheNextPuzzleOfTheSameSize()
        {
            var h = StartPuzzle(8611);
            yield return null;
            yield return null;

            var view = h.Host.GetComponentInChildren<BoardViewController>(true);
            var first = h.Run.CurrentBoard;
            Assert.AreSame(first.RegionMap, view.RegionMapShownByBorders, "first puzzle: borders drawn for its own regions");

            // Next puzzle with the same size but irregular regions, the way a run moves from node to node.
            var config = h.Run.BuildLevelConfig(false, false, h.Run.State.CurrentNodeIndex);
            config.BoardSize = first.Size;
            config.RegionVariant = 2;
            config.ActiveModifiers.Clear();
            h.Run.StartLevel(config);
            var second = h.Run.CurrentBoard;
            Assert.NotNull(second, "second puzzle generated");
            Assert.AreNotSame(first, second);
            Assert.IsFalse(SameRegions(first.RegionMap, second.RegionMap), "test needs a different region layout");

            view.RenderBoard(second, -1, -1, 0, false, null, 0);
            yield return null;

            Assert.AreSame(second.RegionMap, view.RegionMapShownByBorders,
                "a same-size puzzle with other regions must redraw the borders (they showed the previous puzzle's regions)");
        }

        // GAP-058 (0.2.0 playtest): from the second board of a run on, the rule overlay (Dutch Whispers in the report) was
        // drawn over the digits. The digits stay on top on every rebuild.
        [UnityTest]
        [Requirement("UI-STYLE-002")]
        public IEnumerator Lines_StayUnderTheDigits_OnEveryBoardOfARun()
        {
            var h = StartPuzzle(8613, BossModifierId.DutchWhispers);
            yield return null;
            yield return null;
            var view = h.Host.GetComponentInChildren<BoardViewController>(true);
            Assert.IsTrue(view.DigitsAboveOverlay, "first board: digits over the lines");

            for (var board = 0; board < 2; board++)
            {
                var config = h.Run.BuildLevelConfig(false, false, h.Run.State.CurrentNodeIndex);
                config.BoardSize = board == 0 ? 7 : 6;
                config.RegionVariant = 0;
                config.ActiveModifiers.Clear();
                config.ActiveModifiers.Add(BossModifierId.DutchWhispers);
                h.Run.StartLevel(config);
                view.RebuildBoard(-1, -1, 0, false, null, 0, new AccessibilityService());
                yield return null;
                Assert.IsTrue(view.DigitsAboveOverlay, $"board {board + 2}: digits over the lines");
            }
        }

        private static bool SameRegions(int[,] a, int[,] b)
        {
            if (a == null || b == null) return a == b;
            for (var r = 0; r < a.GetLength(0); r++)
            for (var c = 0; c < a.GetLength(1); c++)
                if (a[r, c] != b[r, c]) return false;
            return true;
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

            var intensity = FindObject(h.Host, "GateIntensity").GetComponent<Text>();
            StringAssert.Contains(BossService.GetIntensityName(BossModifierIntensity.VeryHigh), intensity.text);
            var debuff = h.Run.GetBossDebuffForCurrentFloor();
            Assert.IsTrue(debuff.HasValue);
            var curse = FindObject(h.Host, "GateDebuff").GetComponent<Text>();
            StringAssert.Contains(BossService.GetModifierName(debuff.Value), curse.text);
        }

        // [REQ: UI-STYLE-004] Trial scroll: picked trials are ticked, the count follows, Confirm unlocks at the
        // required number and records exactly the picked trials.
        [UnityTest]
        [Requirement("UI-STYLE-004", "REQ-BOSS-103")]
        public IEnumerator BossGate_PickingTrials_TicksThem_AndConfirmRecordsThem()
        {
            var h = StartPuzzle(8603);
            h.Run.State.CurrentFloor = 2; // Floor 3: choose 3 of 4
            yield return null;

            var gate = h.Host.GetComponentInChildren<BossGateViewController>(true);
            gate.ShowBossGateChoice();
            yield return null;

            Assert.IsNotNull(FindObjectOrNull(h.Host, "Mod_3"), "four trials on Floor 3");
            Assert.IsNull(FindObjectOrNull(h.Host, "Mod_4"));
            var confirm = FindObject(h.Host, "ConfirmBossBtn").GetComponent<Button>();
            Assert.IsFalse(confirm.interactable, "Confirm waits for the picks");
            StringAssert.Contains(SudokuRoguelike.Data.FloorThemeData.GetBossName(2),
                FindObject(h.Host, "Title").GetComponent<Text>().text, "the title names the floor boss");
            List<BossModifierId> confirmed = null;
            gate.OnModsConfirmed += mods => confirmed = mods;

            for (var i = 0; i < 4; i++) ClickButton(h.Host, $"Mod_{i}"); // the fourth exceeds the picks and is ignored
            for (var i = 0; i < 4; i++)
                Assert.AreEqual(i < 3, FindObject(FindObject(h.Host, $"Mod_{i}"), "PickTick").activeSelf, $"Mod_{i} tick");
            StringAssert.Contains("3", FindObject(h.Host, "GateChosenCount").GetComponent<Text>().text);
            Assert.IsTrue(confirm.interactable);

            ClickButton(h.Host, "Mod_0");                                   // untick one again
            Assert.IsFalse(confirm.interactable);
            ClickButton(h.Host, "Mod_3");
            Assert.IsTrue(confirm.interactable);

            ClickButton(h.Host, "ConfirmBossBtn");
            Assert.IsFalse(FindObject(h.Host, "Mod_0").GetComponent<Button>().interactable,
                "the picks lock while Confirm flashes");
            Assert.IsFalse(confirm.interactable, "Confirm fires once");
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.NotNull(confirmed, "Confirm reports the picks");
            Assert.AreEqual(3, confirmed.Count);
            Assert.IsFalse(gate.IsAwaiting);
            Assert.IsNull(FindObjectOrNull(h.Host, "BossGatePanel"), "the scroll closes after Confirm");
        }

        // [REQ: UI-STYLE-004] Floor 1 offers 2 trials and asks for 1: choosing the other trial moves the pick.
        [UnityTest]
        [Requirement("UI-STYLE-004")]
        public IEnumerator BossGate_WithOnePick_ChoosingAnotherTrialMovesThePick()
        {
            var h = StartPuzzle(8605);
            h.Run.State.CurrentFloor = 0;
            yield return null;

            h.Host.GetComponentInChildren<BossGateViewController>(true).ShowBossGateChoice();
            yield return null;

            ClickButton(h.Host, "Mod_0");
            ClickButton(h.Host, "Mod_1");
            Assert.IsFalse(FindObject(FindObject(h.Host, "Mod_0"), "PickTick").activeSelf);
            Assert.IsTrue(FindObject(FindObject(h.Host, "Mod_1"), "PickTick").activeSelf);
            Assert.IsTrue(FindObject(h.Host, "ConfirmBossBtn").GetComponent<Button>().interactable);
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

        // ── Puzzle screen as a hanging scroll (UI-STYLE-002) ──

        // [REQ: UI-STYLE-002] The HP lantern shows its name above the value; an empty bag slot is a dark socket and an
        // item hangs on a wooden plaque; pencil marks in a cage's label cell sit under the cage sum; the board's text
        // layers snap to whole pixels and take no clicks.
        [UnityTest]
        [Requirement("UI-STYLE-002")]
        public IEnumerator PuzzleScroll_LanternsBagSlotsAndCageCells_ReadAsDesigned()
        {
            var h = StartPuzzle(8605, BossModifierId.KillerCages);
            for (var frame = 0; frame < 30 && FindObjectOrNull(h.Host, "C_0_0") == null; frame++)
                yield return null;
            yield return null;
            yield return null;

            var hp = FindObject(h.Host, "SudokuGameplayHp").GetComponent<Text>();
            StringAssert.StartsWith("<size=", hp.text, "the lantern's name is written smaller");
            StringAssert.EndsWith("\n20/20", hp.text, "the value sits on its own line under the name");

            var slot = FindObject(h.Host, "BagSlot_0").GetComponent<Image>();
            Assert.Less(Brightness(slot.color), 0.2f, "an empty bag slot is a dark socket");
            h.Run.State.HeldItems.Add(new ItemInstance { Id = "ui-style-002", Type = ItemType.InkWell, Charges = 1 });
            h.Host.GetComponentInChildren<HudViewController>(true).Refresh(h.Run.State, h.Run.CurrentLevelConfig);
            Assert.Greater(Brightness(slot.color), 0.4f, "an item hangs on a wooden plaque");

            var cages = h.Run.CurrentOverlay.KillerCages;
            Assert.IsNotEmpty(cages, "the test puzzle has killer cages");
            var labelCells = cages.Select(cage => cage.Cells.OrderBy(c => c.Row).ThenBy(c => c.Col).First())
                .Select(c => (c.Row, c.Col)).ToList();
            var (row, col) = labelCells[0];
            Assert.AreEqual(TextAnchor.LowerLeft, PencilLabel(h, row, col).alignment, "pencil marks move under the cage sum");
            var plain = Enumerable.Range(0, h.Run.CurrentBoard.Size * h.Run.CurrentBoard.Size)
                .Select(i => (i / h.Run.CurrentBoard.Size, i % h.Run.CurrentBoard.Size))
                .First(cell => !labelCells.Contains(cell));
            Assert.AreEqual(TextAnchor.UpperLeft, PencilLabel(h, plain.Item1, plain.Item2).alignment);

            foreach (var layer in new[] { "GridNumbers", "GridOverlay" })
            {
                var canvas = FindObject(h.Host, layer).GetComponent<Canvas>();
                Assert.NotNull(canvas, $"{layer} has its own canvas");
                Assert.IsTrue(canvas.overridePixelPerfect && canvas.pixelPerfect, $"{layer} snaps to whole pixels");
                Assert.IsNull(canvas.GetComponent<GraphicRaycaster>(), $"{layer} takes no clicks; the cells below do");
            }
        }

        // [REQ: UI-STYLE-007] With a full bag, taking a plaque opens the swap layer, which is modal: keys and the scene
        // behind it do nothing. Keeping the bag returns to the plaques (the reward is not lost). A run that ends while
        // the reward is open closes the scene.
        [UnityTest]
        [Requirement("UI-STYLE-007")]
        public IEnumerator Reward_FullBag_SwapIsModal_KeepingTheBagReturnsToThePlaques()
        {
            var h = StartPuzzle(8606);
            yield return null;
            yield return null;
            var state = h.Run.State;
            state.HeldItems.Clear();
            for (var i = 0; i < state.ItemSlots; i++)
                state.HeldItems.Add(new ItemInstance { Id = "full-" + i, Type = ItemType.InkWell, Charges = 1 });
            var board = h.Run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0) board.PlaceValue(r, c, board.Solution[r, c]);

            var reward = h.Host.GetComponentInChildren<RewardViewController>(true);
            for (var waited = 0f; waited < 3f && !reward.IsAwaitingReward; waited += Time.unscaledDeltaTime)
                yield return null;
            if (!reward.IsAwaitingReward) reward.ShowRewardScreen();
            Assert.IsTrue(reward.IsAwaitingReward, "the reward screen is open");
            var itemSlot = h.Run.RolledItemSlots?.FindIndex(slot => slot != null) ?? -1;
            if (itemSlot < 0 || itemSlot > 2) Assert.Inconclusive("this seed rolled no item in the first three plaques");

            reward.SelectRewardByIndex(itemSlot);   // choose
            reward.SelectRewardByIndex(itemSlot);   // take: the bag is full, so the swap layer opens
            yield return null;
            Assert.IsTrue(reward.IsSwapOpen, "a full bag asks which item to give up");
            var swap = FindObject(h.Host, "BagSwapPanel");
            var layer = swap.GetComponent<RectTransform>();
            Assert.AreEqual(Vector2.zero, layer.anchorMin, "the swap layer covers the whole screen");
            Assert.AreEqual(Vector2.one, layer.anchorMax);
            Assert.IsTrue(swap.GetComponent<Image>().raycastTarget, "and takes the clicks meant for the scene behind it");

            reward.SelectRewardByIndex(itemSlot);   // keys are ignored while the swap is open
            Assert.IsTrue(reward.IsSwapOpen);
            ClickButton(swap, "SwapAbort");
            yield return null;
            Assert.IsFalse(reward.IsSwapOpen);
            Assert.IsTrue(reward.IsAwaitingReward, "keeping the bag returns to the plaques");
            Assert.AreEqual(FindObject(h.Host, "RewardPanel"), reward.ActivePanel, "the reward has the focus again");
            Assert.AreEqual(state.ItemSlots, state.HeldItems.Count, "nothing was swapped");

            reward.CloseScenes();
            yield return null;
            Assert.IsFalse(reward.IsAwaitingReward);
            Assert.IsNull(reward.ActivePanel);
            Assert.IsNull(FindObjectOrNull(h.Host, "RewardPanel"), "a run that ends closes the reward scene");
        }

        // GameDesignSpec 4.1/4.2 (0.2.0 playtest): a digit is taken 0.25 s after a cell is chosen (it was 0.5 s), and a placed
        // digit highlights every cell with that digit.
        [UnityTest]
        public IEnumerator PlacingADigit_AQuarterSecondAfterChoosingTheCell_HighlightsThatDigit()
        {
            var h = StartPuzzle(8612);
            var cell = EmptyCells(h.Run)[0];
            yield return Select(h, cell);
            yield return new WaitForSecondsRealtime(InRunController.CellSelectInputDelay + 0.05f);
            var digit = h.Run.CurrentBoard.Solution[cell.row, cell.col];
            ClickButton(h.Host, $"Num_{digit}");
            yield return null;

            Assert.AreEqual(digit, h.Run.CurrentBoard.Cells[cell.row, cell.col], "the digit is taken 0.3 s after the cell was chosen");
            Assert.AreEqual(digit, h.Host.GetComponentInChildren<InRunController>(true).HighlightValue,
                "the placed digit is highlighted");
        }

        private static Text PencilLabel(Harness h, int row, int col) =>
            FindObject(FindObject(h.Host, $"N_{row}_{col}"), "Pencil").GetComponent<Text>();

        private static float Brightness(Color c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) * c.a;

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

        // [REQ: UI-STYLE-001] The path is refreshed from many places, sometimes twice in one frame: the scroll keeps
        // exactly one legend, one Save & Quit and one Options button, and the floor title follows the run.
        [UnityTest]
        [Requirement("UI-STYLE-001")]
        public IEnumerator PathScroll_TwoRefreshesInOneFrame_KeepOneLegendAndOneSetOfButtons()
        {
            var h = StartPathMap(8606);
            yield return null;
            var controller = h.Host.GetComponentInChildren<InRunController>(true);
            var refresh = typeof(InRunController).GetMethod("RefreshPathOverview",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(refresh, "RefreshPathOverview");
            refresh.Invoke(controller, null);
            refresh.Invoke(controller, null);
            yield return null;
            yield return null;

            Assert.AreEqual(1, FindAll(h.Host, "PathLegend").Count, "one legend");
            Assert.AreEqual(1, FindAll(h.Host, "BtnSaveQuit").Count, "one Save & Quit");
            Assert.AreEqual(1, FindAll(h.Host, "BtnPathOptions").Count, "one Options button");
            Assert.AreEqual(1, FindAll(h.Host, "FirstPickBand").Count, "one first-pick note");
            StringAssert.AreEqualIgnoringCase(SudokuRoguelike.Data.FloorThemeData.GetFloorName(0),
                FindObject(h.Host, "PathTitle").GetComponent<Text>().text);
        }

        // [REQ: UI-STYLE-001] A drag that starts on empty route space reaches the route's ScrollRect.
        [UnityTest]
        [Requirement("UI-STYLE-001")]
        public IEnumerator PathScroll_EmptyRouteSpace_TakesTheDrag()
        {
            var h = StartPathMap(8607);
            yield return null;
            yield return null;
            // The run's intro card covers the screen until it is dismissed, as it does for the player.
            var intro = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "RunIntroCard");
            if (intro != null) intro.onClick.Invoke();
            yield return null;
            yield return null;
            var viewport = FindObject(h.Host, "PathViewport").GetComponent<RectTransform>();
            var corners = new Vector3[4];
            viewport.GetWorldCorners(corners); // bottom-left, top-left, top-right, bottom-right
            var point = Vector3.Lerp(corners[0], corners[2], 0.02f);
            var screen = RectTransformUtility.WorldToScreenPoint(null, point);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
            Assert.IsNotEmpty(hits, "nothing takes a pointer at the bottom-left of the route");
            Assert.AreEqual("PathOverlay", hits[0].gameObject.name, "the route content takes the drag");
            Assert.NotNull(hits[0].gameObject.GetComponentInParent<ScrollRect>());
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
            ClickButton(options, "IGTabRun"); // UI-OPT-002: the run-only actions sit on the Run tab
            yield return null;
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
            // InRunController ignores digits for 0.25 s after a cell is selected (touch safety window).
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
