using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // "Before" screenshots for the layout proposals (docs/design/layout-proposals.md): the garden path overview and a
    // puzzle at 1920×1080. Needs graphics, so it is [Explicit] and runs without -nographics (see
    // OptionsScreenshotDiagnostics for the command line). Output: SR_SHOT_DIR or %TEMP%\sr_layout_shots.
    [TestFixture]
    [Explicit("Writes screenshots; run on demand with graphics")]
    public sealed class LayoutScreenshotDiagnostics
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;
        private string _outDir;
        private Action<RunDirector> _beforeBind;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            _outDir = Environment.GetEnvironmentVariable("SR_SHOT_DIR");
            if (string.IsNullOrEmpty(_outDir)) _outDir = Path.Combine(Path.GetTempPath(), "sr_layout_shots");
            Directory.CreateDirectory(_outDir);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            for (var i = _createdRoots.Count - 1; i >= 0; i--)
                if (_createdRoots[i] != null) Object.Destroy(_createdRoots[i]);
            _createdRoots.Clear();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RenderPathOverviewAndPuzzle()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var camera = new GameObject("LayoutShotCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            _createdRoots.Add(camera.gameObject);

            // Garden path overview right after the run starts (Floor 1).
            var (pathHost, _) = StartRun(7301, false);
            yield return DismissIntro();
            yield return Shoot(pathHost, camera, "before_path_floor1.png");
            pathHost.SetActive(false);

            // A 6×6 puzzle with two modifiers and some digits placed.
            var (puzzleHost, run) = StartRun(7302, true, BossModifierId.GermanWhispers, BossModifierId.KillerCages);
            yield return DismissIntro();
            for (var frame = 0; frame < 30 && Find(puzzleHost, "C_0_0") == null; frame++)
                yield return null;
            yield return Shoot(puzzleHost, camera, "before_puzzle_6x6.png");
            Assert.NotNull(run.CurrentBoard);
        }

        // UI-STYLE-004/005: the trial scroll on Floor 3 (Koi Terrace, four trials, highest intensity, two picked) and
        // on Floor 5 (six trials in two rows, none discovered yet), then the offering table after the Floor 3 boss.
        [UnityTest]
        public IEnumerator RenderBossGateAndBossDefeated()
        {
            var camera = CreateCamera();

            var (gateHost, gateRun) = StartRun(7303, true);
            gateRun.State.CurrentFloor = 2;
            gateRun.State.RunNumber = 9;
            gateRun.State.CurrentGold = 212;   // ECON-GATE-001/002: the purse and the gate's gold actions
            foreach (BossModifierId id in Enum.GetValues(typeof(BossModifierId)))
                gateRun.State.SeenBossModifiers.Add(id);
            yield return DismissIntro();
            var gate = gateHost.GetComponentInChildren<BossGateViewController>(true);
            gate.ShowBossGateChoice();
            yield return null;
            Find(gateHost, "Mod_0")?.GetComponent<Button>().onClick.Invoke();
            Find(gateHost, "Mod_1")?.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(gateHost, camera, "after_boss_gate.png");
            Find(gateHost, "BtnStrikeTrial").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Find(gateHost, "Mod_3").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(gateHost, camera, "after_boss_gate_struck.png");
            gateHost.SetActive(false);

            var (wideHost, wideRun) = StartRun(7306, true);
            wideRun.State.CurrentFloor = 4;
            wideRun.State.RunNumber = 3;
            yield return DismissIntro();
            wideHost.GetComponentInChildren<BossGateViewController>(true).ShowBossGateChoice();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(wideHost, camera, "after_boss_gate_floor5.png");
            wideHost.SetActive(false);

            var (clearedHost, clearedRun) = StartRun(7304, true);
            clearedRun.State.CurrentFloor = 2;
            // The boss fight and the whole floor (REQ-BOSS-107, 0.2.0 playtest): six puzzles, the boss one included.
            clearedRun.CurrentLevelState.Mistakes = 1;
            clearedRun.CurrentLevelState.HpLost = 1;
            clearedRun.CurrentLevelState.PencilMarksUsed = 9;
            clearedRun.State.FloorTally = new FloorTally { Puzzles = 6, Mistakes = 4, HpLost = 5, ItemsUsed = 2, PencilMarks = 37 };
            yield return DismissIntro();
            clearedHost.GetComponentInChildren<EndScreenViewController>(true)
                .ShowBossCleared(clearedRun, clearedRun.RollRelicChoices(3), _ => { }, () => { });
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(clearedHost, camera, "after_boss_defeated.png");

            // [REQ: RELIC-KEY-001] After the first boss the table offers keystones.
            clearedRun.State.CurrentFloor = 0;
            clearedHost.GetComponentInChildren<EndScreenViewController>(true)
                .ShowBossCleared(clearedRun, clearedRun.RollBossOfferings(), _ => { }, () => { });
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(clearedHost, camera, "after_boss_defeated_keystones.png");
        }

        // GAP-058: the second board of a run (8×8, Dutch Whispers, as in the 0.2.0 playtest) keeps its digits over the lines.
        [UnityTest]
        public IEnumerator RenderSecondBoardLines()
        {
            var camera = CreateCamera();
            _puzzleSize = 8;
            var (host, run) = StartRun(7351, true, BossModifierId.DutchWhispers, BossModifierId.FullWhiteKropki);
            _puzzleSize = 6;
            yield return DismissIntro();
            var config = run.BuildLevelConfig(false, false, run.State.CurrentNodeIndex);
            config.BoardSize = 8;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            config.ActiveModifiers.Add(BossModifierId.DutchWhispers);
            config.ActiveModifiers.Add(BossModifierId.FullWhiteKropki);
            run.StartLevel(config);
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)          // fill some digits so lines cross them
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0 && (r + c) % 3 == 0) board.PlaceValue(r, c, board.Solution[r, c]);
            host.GetComponentInChildren<BoardViewController>(true).RebuildBoard(-1, -1, 0, false, null, 0, new AccessibilityService());
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_lines_second_board.png");
        }

        // UI-STYLE-001: the garden path as a hand scroll on each of the five floors (one run advanced floor by floor).
        [UnityTest]
        public IEnumerator RenderPathScrollOnEveryFloor()
        {
            var camera = CreateCamera();
            for (var floor = 0; floor < 5; floor++)
            {
                var target = floor;
                _beforeBindAlways = run =>
                {
                    for (var f = 0; f < target; f++) run.AdvanceToNextFloor();
                    run.State.HeldRelics.Clear();
                };
                var (host, _) = StartRun(7310 + floor, false);
                _beforeBindAlways = null;
                yield return DismissIntro();
                yield return new WaitForSecondsRealtime(2.0f); // node pop-in and first-pick pulses settle
                var label = host.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name == "Label" && t.transform.parent.name.StartsWith("Node_"));
                if (label != null) Debug.Log($"[LayoutShot] node label scale {label.transform.lossyScale}, color {label.color}, size {label.fontSize}/{label.cachedTextGenerator.fontSizeUsedForBestFit}");
                yield return Shoot(host, camera, $"after_path_floor{floor + 1}.png");
                host.SetActive(false);
            }
        }

        // UI-STYLE-021: entering a floor (garden flow D) on floors 1, 2 and 4 (with ECON-BAL-003's mistake cost), and the
        // path half unrolled.
        [UnityTest]
        public IEnumerator RenderFloorEntry()
        {
            var camera = CreateCamera();
            foreach (var floor in new[] { 0, 1, 3 })
            {
                var target = floor;
                _beforeBindAlways = run =>
                {
                    for (var f = 0; f < target; f++) run.AdvanceToNextFloor();
                    run.State.HeldRelics.Clear();
                };
                var (host, _) = StartRun(7340 + floor, false);
                _beforeBindAlways = null;
                yield return null;
                yield return null;
                var intro = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.name == "RunIntroCard");
                if (intro != null) intro.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Shoot(host, camera, $"after_floor_entry_floor{floor + 1}.png");
                if (floor == 1)
                {
                    host.GetComponentInChildren<InRunController>(true).DismissFloorEntry(true);
                    yield return new WaitForSecondsRealtime(0.12f);
                    yield return Shoot(host, camera, "after_floor_entry_unroll.png");
                }
                host.SetActive(false);
            }
        }

        private Action<RunDirector> _beforeBindAlways;
        private int _puzzleSize = 6;
        private int _regionVariant;

        // UI-STYLE-002: the puzzle on the hanging scroll: 6×6 on floor 1 with Dutch whispers and parity lines, a 7×7 with
        // German whispers and cages (both need 7×7), a few digits placed (own, pencil marks); 9×9 on floor 5; an irregular
        // 7×7 on floor 3.
        [UnityTest]
        public IEnumerator RenderPuzzleScroll()
        {
            var camera = CreateCamera();
            var shots = new (int size, int region, int floor, string file, BossModifierId[] mods)[]
            {
                (6, 0, 0, "after_puzzle_6x6.png", new[] { BossModifierId.DutchWhispers, BossModifierId.ParityLines }),
                (7, 0, 1, "after_puzzle_7x7_cages.png", new[] { BossModifierId.GermanWhispers, BossModifierId.KillerCages }),
                (9, 0, 4, "after_puzzle_9x9.png", new[] { BossModifierId.KillerCages, BossModifierId.DifferenceKropki }),
                (7, 1, 2, "after_puzzle_7x7_irregular.png", new[] { BossModifierId.Thermo }),
            };
            foreach (var shot in shots)
            {
                _puzzleSize = shot.size;
                _regionVariant = shot.region;
                var floor = shot.floor;
                _beforeBindAlways = run => { for (var f = 0; f < floor; f++) run.AdvanceToNextFloor(); };
                _beforeBind = run =>
                {
                    var board = run.CurrentBoard;
                    var placed = 0;
                    for (var r = 0; r < board.Size && placed < 4; r++)
                    for (var c = 0; c < board.Size && placed < 4; c++)
                        if (!board.GivenMask[r, c] && board.Cells[r, c] == 0 && (r + c) % 3 == 1)
                        {
                            board.PlaceValue(r, c, board.Solution[r, c]);
                            placed++;
                        }
                    for (var r = board.Size - 1; r >= 0; r--)
                    for (var c = 0; c < board.Size; c++)
                        if (board.Cells[r, c] == 0 && (r * 7 + c) % 5 == 0)
                        {
                            board.TogglePencilMark(r, c, board.Solution[r, c]);
                            board.TogglePencilMark(r, c, board.Solution[r, c] % board.Size + 1);
                        }
                };
                var (host, run) = StartRun(7320 + shot.size, true, shot.mods);
                _beforeBind = null;
                _beforeBindAlways = null;
                yield return DismissIntro();
                for (var frame = 0; frame < 30 && Find(host, "C_0_0") == null; frame++)
                    yield return null;
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Shoot(host, camera, shot.file);
                host.SetActive(false);
            }
            _puzzleSize = 6;
            _regionVariant = 0;
        }

        // UI-STYLE-020: Pencil mode on a 6×6 puzzle with a few marks on the board. File names start with SR_SHOT_PREFIX
        // ("after_" by default).
        [UnityTest]
        public IEnumerator RenderPencilMode()
        {
            var prefix = Environment.GetEnvironmentVariable("SR_SHOT_PREFIX");
            if (string.IsNullOrEmpty(prefix)) prefix = "after_";
            var camera = CreateCamera();
            _beforeBind = run =>
            {
                var board = run.CurrentBoard;
                for (var r = 0; r < board.Size; r++)
                for (var c = 0; c < board.Size; c++)
                    if (board.Cells[r, c] == 0 && (r + c) % 2 == 0)
                    {
                        board.TogglePencilMark(r, c, board.Solution[r, c]);
                        board.TogglePencilMark(r, c, board.Solution[r, c] % board.Size + 1);
                    }
            };
            var (host, _) = StartRun(7331, true, BossModifierId.DutchWhispers);
            _beforeBind = null;
            yield return DismissIntro();
            for (var frame = 0; frame < 30 && Find(host, "C_0_0") == null; frame++)
                yield return null;
            Find(host, "PencilBtn").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, prefix + "pencil_mode.png");
        }

        // UI-STYLE-006: killer cages of five shapes on a 6×6 board; each must read as one dashed outline.
        [UnityTest]
        public IEnumerator RenderKillerCageShapes()
        {
            var camera = CreateCamera();
            _beforeBind = run =>
            {
                var cages = run.CurrentOverlay.KillerCages;
                cages.Clear();
                cages.Add(Cage(run, (0, 0), (0, 1)));
                cages.Add(Cage(run, (1, 3), (2, 3), (3, 3), (2, 2)));
                cages.Add(Cage(run, (3, 0), (4, 0), (4, 1)));
                cages.Add(Cage(run, (0, 4), (0, 5), (1, 4), (1, 5)));
                cages.Add(Cage(run, (4, 4), (4, 5), (5, 3), (5, 4)));
            };
            var (host, _) = StartRun(7305, true, BossModifierId.KillerCages);
            _beforeBind = null;
            yield return DismissIntro();
            for (var frame = 0; frame < 30 && Find(host, "C_0_0") == null; frame++)
                yield return null;
            yield return Shoot(host, camera, "cages_6x6.png");
        }

        // UI-STYLE-007/008/009: the reward tally with ema plaques, the relic shrine and the market stall, one after the
        // other in the same run on Floor 2 (two items in a four-slot bag, two relics held, 142 gold, one reroll token).
        [UnityTest]
        public IEnumerator RenderRewardShrineAndShop()
        {
            var camera = CreateCamera();
            _beforeBindAlways = run =>
            {
                run.AdvanceToNextFloor();
                var state = run.State;
                state.ItemSlots = 4;
                state.HeldItems.Clear();
                state.HeldItems.Add(new ItemInstance { Id = "shot-a", Type = ItemType.InkWell, Charges = 1 });
                state.HeldItems.Add(new ItemInstance { Id = "shot-b", Type = ItemType.MeditationStone, Charges = 1 });
                foreach (var relic in run.RollRelicChoices(2)) run.AcceptRelicChoice(relic);
            };
            var (host, run) = StartRun(7311, true);
            _beforeBindAlways = null;
            yield return DismissIntro();

            // Solve the puzzle behind the game's back (no placement events), so only this test claims the reward.
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0) board.PlaceValue(r, c, board.Solution[r, c]);
            run.State.ComboStreak = 5;
            run.State.CurrentGold = 130;
            run.State.RerollTokens = 1;
            var reward = host.GetComponentInChildren<RewardViewController>(true);
            // The game notices the solved board and opens the reward itself; open it here if it has not after 3 s.
            for (var waited = 0f; waited < 3f && !reward.IsAwaitingReward; waited += Time.unscaledDeltaTime)
                yield return null;
            if (!reward.IsAwaitingReward) reward.ShowRewardScreen();
            yield return new WaitForSecondsRealtime(1.2f);
            reward.SelectRewardByIndex(1);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_reward.png");
            // GAP-056: five plaques (as in the 0.2.0 playtest) shrink to fit right of the tally.
            reward.RerollRewardSlots(new List<ItemInstance>
            {
                null, null, new ItemInstance { Id = "five-a", Type = ItemType.MeditationStone, Charges = 1 }, null,
                new ItemInstance { Id = "five-b", Type = ItemType.BambooScroll, Charges = 1 }
            });
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shoot(host, camera, "after_reward_five.png");
            reward.SelectRewardByIndex(1);
            reward.SelectRewardByIndex(1);   // the same key again takes it
            yield return null;

            reward.ShowRelicChoicePanel(run.RollRelicChoices(3));
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, "after_relic_shrine.png");
            Find(Find(host, "RelicChoicePanel"), "BtnSkip").GetComponent<Button>().onClick.Invoke();
            yield return null;

            run.State.CurrentGold = 142;
            var shop = host.GetComponentInChildren<ShopViewController>(true);
            shop.ShowShop();
            yield return new WaitForSecondsRealtime(0.6f);
            shop.SelectSlotByIndex(1);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shoot(host, camera, "after_shop.png");

            // ITEM-COUNTER-007: with lines and cages on the floor the market leans toward their counter items (the first
            // reroll count whose offers hold one).
            run.State.ActiveFloorModifiers = new List<BossModifierId> { BossModifierId.DutchWhispers, BossModifierId.KillerCages };
            for (var reroll = 0; reroll < 12; reroll++)
            {
                run.State.ShopRerollCount = reroll;
                if (run.BuildShopOffers().Any(o => SudokuRoguelike.Items.CounterItems.IsCounterItem(o.Item.Type))) break;
            }
            shop.CloseScenes();
            yield return null;
            shop.ShowShop();
            yield return new WaitForSecondsRealtime(0.6f);
            for (var i = 0; i < run.CurrentShopOffers.Count; i++)
                if (SudokuRoguelike.Items.CounterItems.IsCounterItem(run.CurrentShopOffers[i].Item.Type)) { shop.SelectSlotByIndex(i); break; }
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shoot(host, camera, "after_shop_counters.png");
        }

        // UI-STYLE-013..016: the rest, event, cursed-tile and run-end screens on Floor 2 (a curse active, so the rest shows
        // its fourth choice). File names start with SR_SHOT_PREFIX ("after_" by default; "before_" for the old screens).
        [UnityTest]
        public IEnumerator RenderRestEventCursedAndRunEnd()
        {
            var prefix = Environment.GetEnvironmentVariable("SR_SHOT_PREFIX");
            if (string.IsNullOrEmpty(prefix)) prefix = "after_";
            var camera = CreateCamera();
            _beforeBindAlways = run =>
            {
                run.AdvanceToNextFloor();
                run.State.CurrentHP = Math.Max(1, run.State.MaxHP - 3);
                run.State.CurrentPencil = 3;
                run.State.CurrentGold = 18;
                run.ApplyCurse("hollow_pencil");
            };
            var (host, run) = StartRun(7321, false);
            _beforeBindAlways = null;
            yield return DismissIntro();

            var reward = host.GetComponentInChildren<RewardViewController>(true);
            reward.ShowRestChoicePanel();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, prefix + "rest.png");
            reward.CloseScenes();
            yield return null;

            // The real path: the flow controller binds the run map and opens the event.
            var flow = host.GetComponentInChildren<InRunUiFlowController>(true);
            flow.OnNodeEntered(NodeType.Event);
            flow.EventChoiceScreen.SelectChoiceByIndex(1);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, prefix + "event.png");
            flow.OnEventClosed();
            yield return null;

            var gate = host.GetComponentInChildren<BossGateViewController>(true);
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First();
            gate.ShowCursedNodePanel(Find(host, "PathOverviewPanel"), node, cfg => { }, cfg => { });
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, prefix + "cursed.png");
            gate.CloseCursedNodePanel();
            yield return null;

            var end = host.GetComponentInChildren<EndScreenViewController>(true);
            run.State.CurrentGold = 212;
            // A ledger with every kind of row and a grade stamp per puzzle (SCORE-GRADE-003): five floors of puzzles.
            var mistakesPerFloor = new[] { new[] { 0, 1, 0, 2, 0 }, new[] { 0, 0, 3, 1, 0 }, new[] { 0, 4, 0, 0, 2 },
                new[] { 1, 0, 0, 0, 0 }, new[] { 0, 2, 0, 0 } };
            for (var f = 0; f < mistakesPerFloor.Length; f++)
                foreach (var mistakes in mistakesPerFloor[f])
                {
                    var tile = new TileXpEntry { BoardSize = 9, Stars = 3, TotalXp = 16 };
                    run.TileXpLog.Add(tile);
                    run.GetAnalytics().RecordTileXp(tile);
                    run.GetAnalytics().RecordPuzzleSolved(mistakes, 0, f);
                }
            run.GetAnalytics().RecordCursedPuzzleCompleted();
            run.GetAnalytics().RecordRunComplete();
            end.ShowGameOver(run, true);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, prefix + "run_end_victory.png");
            // [REQ: SCORE-TIME-001] The same win on the seed of the week, in 38 minutes: the ledger's time row.
            run.State.WeeklySeedRun = true;
            run.State.ActiveSeconds = 38 * 60;
            end.ShowGameOver(run, true);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, prefix + "run_end_weekly.png");
            run.State.WeeklySeedRun = false;
            run.State.CurrentHP = 0;
            end.ShowGameOver(run, false);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, prefix + "run_end_defeat.png");
        }

        // The main menu (with its version) and the menu screens the next proposals rework: Options, the Codex and Meta
        // Progression. Shot as they are, for the proposals' "before" images.
        [UnityTest]
        public IEnumerator RenderMenuScreens()
        {
            var prefix = Environment.GetEnvironmentVariable("SR_SHOT_PREFIX");
            if (string.IsNullOrEmpty(prefix)) prefix = "before_";
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotMenus");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "after_main_menu.png");
            foreach (var (button, file) in new[] { ("BtnOptions", "options"), ("BtnItems", "codex"), ("BtnMeta", "meta_progression") })
            {
                menu.ShowMainMenu();
                yield return null;
                Find(host, button).GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(host, camera, prefix + file + ".png");
            }
        }

        // UI-STYLE-018: the Codex book with a profile about half way through its collections (the mockup's state):
        // the Items page with an item chosen that was found and used, then the Curses page.
        [UnityTest]
        public IEnumerator RenderCodexBook()
        {
            var meta = new MetaProgressionState { ItemCodex = new ItemCodexData() };
            var items = (ItemType[])Enum.GetValues(typeof(ItemType));
            for (var i = 0; i < items.Length; i++)
                if (i % 5 != 2)
                    meta.ItemCodex.Entries.Add(new ItemCodexEntry
                    {
                        ItemId = items[i].ToString(), Discovered = true, DiscoveredDate = "2026-10-1" + (i % 10),
                        TimesFound = 1 + i % 7, TimesUsed = i % 5
                    });
            meta.DiscoveredRelics = ((RelicId[])Enum.GetValues(typeof(RelicId))).Where((_, i) => i % 4 == 0).ToList();
            meta.DiscoveredBossModifiers = ModifierFamilies.All().Where((_, i) => i % 3 != 1).ToList();
            meta.DiscoveredCurses = CurseService.All.Where((_, i) => i % 3 == 0).Select(c => c.Id).ToList();
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { MetaProgress = meta });
            SaveFileService.FlushSharedPendingWrites();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotCodexEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            // No pointer in a batch run: its stale position would hover a button in the shot.
            var inputModules = Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None);
            foreach (var module in inputModules) module.enabled = false;
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotCodex");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            yield return null;
            menu.OpenItems();
            Find(host, "BtnCodexItems").GetComponent<Button>().onClick.Invoke();
            yield return null;
            EventSystem.current.SetSelectedGameObject(Find(host, "CodexTile_3"));   // Meditation Stone: found and used
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_codex.png");

            Find(host, "BtnCodexCurses").GetComponent<Button>().onClick.Invoke();
            EventSystem.current.SetSelectedGameObject(Find(host, "CodexTile_0"));   // the entry the right page shows
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_codex_curses.png");

            // META-CODEX-010: the Items page filtered to Rare, the Meditation Stone with "Best paired with".
            Find(host, "BtnCodexItems").GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 2; i++) Find(host, "BtnCodexRarity").GetComponent<Button>().onClick.Invoke();
            yield return null;
            foreach (var tile in host.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("CodexTile_")))
                if (tile.transform.Find("Name").GetComponent<Text>().text == "Meditation Stone")
                {
                    tile.onClick.Invoke();
                    EventSystem.current.SetSelectedGameObject(tile.gameObject);
                }
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_codex_rarity.png");
            foreach (var module in inputModules) if (module != null) module.enabled = true;
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
        }

        // UI-STYLE-019: the garden of classes with the mockup's profile: Number Freak at Level 23 with two prestige stars
        // after 41 runs, Garden Monk and Shrine Archivist started, the other five locked part way to their goals.
        [UnityTest]
        public IEnumerator RenderClassGarden()
        {
            var meta = new MetaProgressionState
            {
                ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 6, ItemsUsed = 22, RelicsCollected = 9, GoldCollected = 12400 },
                MaxUnlockedHarmonyLevel = 4,
                ItemCodex = new ItemCodexData(),
                DiscoveredRelics = ((RelicId[])Enum.GetValues(typeof(RelicId))).Take(9).ToList(),
                DiscoveredBossModifiers = ModifierFamilies.All().Take(30).ToList()
            };
            meta.UnlockedClasses.AddRange(new[] { ClassId.GardenMonk, ClassId.ShrineArchivist });
            foreach (var type in ((ItemType[])Enum.GetValues(typeof(ItemType))).Take(18))
                meta.ItemCodex.Entries.Add(new ItemCodexEntry { ItemId = type.ToString(), Discovered = true });
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
            {
                ClassId = ClassId.NumberFreak, TotalXp = Economy.XpTable.CumulativeXpForLevel(23) + 400, PrestigeTier = 2,
                GardenRuns = 41, BestFloor = 5
            });
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
                { ClassId = ClassId.GardenMonk, TotalXp = Economy.XpTable.CumulativeXpForLevel(9), GardenRuns = 6, BestFloor = 3 });
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
                { ClassId = ClassId.ShrineArchivist, TotalXp = Economy.XpTable.CumulativeXpForLevel(4), GardenRuns = 2, BestFloor = 2 });
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { MetaProgress = meta });
            SaveFileService.FlushSharedPendingWrites();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotGardenEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var inputModules = Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None);
            foreach (var module in inputModules) module.enabled = false;
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotGarden");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            yield return null;
            menu.OpenMetaProgression();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_meta_progression.png");

            Find(host, "ClassPlaque_KoiGambler").GetComponent<Button>().onClick.Invoke();
            EventSystem.current.SetSelectedGameObject(Find(host, "ClassPlaque_KoiGambler"));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_meta_progression_locked.png");
            foreach (var module in inputModules) if (module != null) module.enabled = true;
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
        }

        // The main menu, Game Modes and Select Class as they are: the "before" images for the next proposals.
        [UnityTest]
        public IEnumerator RenderMenuScreensBefore()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotBeforeEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var inputModules = Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None);
            foreach (var module in inputModules) module.enabled = false;
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotMenusBefore");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "before_main_menu.png");
            foreach (var (button, file) in new[] { ("BtnModes", "game_modes"), ("BtnStart", "class_select") })
            {
                menu.ShowMainMenu();
                yield return null;
                Find(host, button).GetComponent<Button>().onClick.Invoke();
                EventSystem.current.SetSelectedGameObject(null);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(host, camera, "before_" + file + ".png");
            }
            foreach (var module in inputModules) if (module != null) module.enabled = true;
        }

        // UI-STYLE-024 and UI-STYLE-025: Select Class and Game Modes with the mockups' profile (Number Freak at Level 23 with
        // two prestige stars, Harmony IV open and chosen, Endless Zen at depth 14, Spirit Trials still locked), plus the main
        // menu with the Codex renamed.
        [UnityTest]
        public IEnumerator RenderModeScreens()
        {
            var meta = new MetaProgressionState
            {
                ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 1, ItemsUsed = 22, RelicsCollected = 9, GoldCollected = 12400 },
                MaxUnlockedHarmonyLevel = 4,
                LastSelectedHarmonyLevel = 4,
                EndlessZenUnlocked = true
            };
            meta.UnlockedClasses.AddRange(new[] { ClassId.GardenMonk, ClassId.ShrineArchivist });
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
            {
                ClassId = ClassId.NumberFreak, TotalXp = Economy.XpTable.CumulativeXpForLevel(23) + 400, PrestigeTier = 2,
                GardenRuns = 41, BestFloor = 5
            });
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
                { ClassId = ClassId.GardenMonk, TotalXp = Economy.XpTable.CumulativeXpForLevel(9), GardenRuns = 6, BestFloor = 3 });
            var envelope = new SaveFileEnvelope { MetaProgress = meta };
            // UI-STYLE-027: the Daily Walk's slip at streak day 4 with 818 XP still waiting for a class.
            envelope.DailyGoals = new DailyGoalState { CurrentStreak = 3, TotalInkStamps = 9 };
            DailyGoalService.RefreshIfNewDay(envelope.DailyGoals, DateTime.Today);
            envelope.DailyGoals.TodayGoalCompleted[0] = true;
            envelope.DailyGoals.PendingWalkXp = 818;
            envelope.Statistics.HighestEndlessDepth = 14;
            envelope.Statistics.TotalZenSessions = 5;
            envelope.Statistics.TotalRunsStarted = 47;
            envelope.Statistics.TrialsBestScore_Adept = 900;
            envelope.Statistics.TrialsBestScore_Master = 1240;
            meta.SpiritTrialsUnlocked = true;
            new SaveFileService(TestSlot).Save(envelope);
            SaveFileService.FlushSharedPendingWrites();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotModesEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var inputModules = Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None);
            foreach (var module in inputModules) module.enabled = false;
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotModes");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "after_main_menu.png");

            menu.ShowGameModes();
            EventSystem.current.SetSelectedGameObject(Find(host, "BtnGarden"));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_game_modes.png");
            EventSystem.current.SetSelectedGameObject(Find(host, "BtnDailyWalk"));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_game_modes_daily.png");
            // UI-STYLE-028: the way in while XP waits gives it first.
            menu.EnterDailyWalk();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_daily_walk_waiting.png");
            Find(host, "BtnWaitingBack").GetComponent<Button>().onClick.Invoke();
            yield return null;
            // UI-STYLE-027: the sixth curtain, the Incense Walk, with its rule, records and practice.
            EventSystem.current.SetSelectedGameObject(Find(host, "BtnIncenseWalk"));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_game_modes_incense.png");
            EventSystem.current.SetSelectedGameObject(Find(host, "BtnTrials"));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_game_modes_trials.png");

            Find(host, "BtnGarden").GetComponent<Button>().onClick.Invoke();
            EventSystem.current.SetSelectedGameObject(Find(host, "BtnClassNumberFreak"));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_class_select.png");

            menu.BackToMainMenu();
            menu.StartEndlessZen();
            menu.SetSelectedClass(ClassId.GardenMonk);
            EventSystem.current.SetSelectedGameObject(Find(host, "BtnClassGardenMonk"));
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "after_class_select_endless.png");

            // LEGAL-LICENSE-001 (GAP-068): the Credits end with the licence texts.
            menu.BackToMainMenu();
            menu.OpenCredits();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var credits = Find(host, "CreditsScroll").GetComponentInChildren<ScrollRect>(true);
            if (credits == null) credits = Find(host, "CreditsScroll").GetComponentInParent<ScrollRect>();
            var heading = host.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.text == LocalizationService.T("Credits.Section.Licenses"));
            if (credits != null && heading != null)
            {
                var content = credits.content;
                var offset = -heading.rectTransform.anchoredPosition.y - 20f;
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Max(0f, offset));
            }
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "after_credits_licenses.png");
            foreach (var module in inputModules) if (module != null) module.enabled = true;
        }

        // UI-STYLE-026: the results of Endless Zen (depth 14 after a best of 11), Spirit Trials (Adept, passed) and the
        // Monthly Walk (completed). SR_SHOT_PREFIX "before_" shoots the old text results on a mirror with the old code.
        [UnityTest]
        public IEnumerator RenderModeResults()
        {
            var prefix = Environment.GetEnvironmentVariable("SR_SHOT_PREFIX");
            if (string.IsNullOrEmpty(prefix)) prefix = "after_";
            var envelope = new SaveFileEnvelope();
            envelope.Statistics.HighestEndlessDepth = 11;
            envelope.Statistics.TotalZenSessions = 4;
            envelope.Statistics.TrialsBestScore_Adept = 900;
            new SaveFileService(TestSlot).Save(envelope);
            SaveFileService.FlushSharedPendingWrites();
            var camera = CreateCamera();
            var (host, run) = StartRun(7321, true);
            yield return DismissIntro();
            var end = host.GetComponentInChildren<EndScreenViewController>(true);
            var level = run.CurrentLevelState;
            level.CorrectPlacements = 52;
            level.Mistakes = 2;
            level.PencilMarksUsed = 9;
            run.State.Depth = 14;
            run.State.TotalRunSeconds = 754f;

            end.ShowEndlessZenResult(run);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, prefix + "result_endless_zen.png");
            run.State.SpiritTrialsTier = SpiritTrialsTier.Adept;
            end.ShowSpiritTrialsResult(run, true);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, prefix + "result_spirit_trials.png");
            run.State.IsSeasonalChallenge = true;
            end.ShowGameOver(run, true);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, prefix + "result_monthly_walk.png");
        }

        // UI-STYLE-028: the Daily Walk's result as the mockup has it: both floors walked, two of three goals, streak day 4,
        // the walk's XP waiting for one of four unlocked classes.
        [UnityTest]
        public IEnumerator RenderDailyWalkResult()
        {
            var meta = new MetaProgressionState();
            meta.UnlockedClasses.AddRange(new[] { ClassId.GardenMonk, ClassId.ShrineArchivist, ClassId.KoiGambler });
            foreach (var (id, level) in new[] { (ClassId.NumberFreak, 9), (ClassId.GardenMonk, 4), (ClassId.ShrineArchivist, 2), (ClassId.KoiGambler, 2) })
                meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
                    { ClassId = id, TotalXp = Economy.XpTable.CumulativeXpForLevel(level) + 30 });
            var goals = new DailyGoalState { CurrentStreak = 3, TotalInkStamps = 9 };
            DailyGoalService.RefreshIfNewDay(goals, DateTime.Today);
            goals.TodayGoalCompleted[0] = goals.TodayGoalCompleted[1] = true;
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { MetaProgress = meta, DailyGoals = goals });
            SaveFileService.FlushSharedPendingWrites();

            var camera = CreateCamera();
            _beforeBindAlways = r =>
            {
                r.State.ClassId = 0;
                r.State.DailyWalk = r.State.DailyWalkScored = true;
                r.State.TotalFloors = DailyGoalService.WalkFloors;
                r.State.CurrentHP = 6;
            };
            var (host, run) = StartRun(7321, false);
            _beforeBindAlways = null;
            yield return DismissIntro();
            run.DailyGoals = goals;
            for (var i = 0; i < 9; i++)
            {
                var tile = new TileXpEntry { BoardSize = 6, Stars = 2, TotalXp = 44, PerfectBonus = i % 3 == 1 ? 0 : 25 };
                run.TileXpLog.Add(tile);
                run.GetAnalytics().RecordTileXp(tile);
                run.GetAnalytics().RecordPuzzleSolved(i % 3 == 1 ? 1 : 0, 0, i / 5);
                if (i % 3 != 1) run.GetAnalytics().RecordPerfectPuzzle();
            }
            run.GetAnalytics().RecordRunComplete();
            host.GetComponentInChildren<EndScreenViewController>(true).ShowGameOver(run, true);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, "after_daily_walk_result.png");
        }

        // UI-STYLE-030: the Incense Walk's puzzle screen as the mockup has it (Garden Monk, puzzle 6, five solved, 5:42 left,
        // one mistake on this puzzle), then the last 30 seconds, then the result when the stick has burned down.
        [UnityTest]
        public IEnumerator RenderIncenseWalk()
        {
            var envelope = new SaveFileEnvelope();
            envelope.Statistics.IncenseWalks = 3;
            envelope.Statistics.IncenseBestScore = 2840;
            envelope.Statistics.IncenseMostPuzzles = 11;
            new SaveFileService(TestSlot).Save(envelope);
            SaveFileService.FlushSharedPendingWrites();

            var camera = CreateCamera();
            var host = new GameObject("LayoutShotIncense");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.GardenMonk, Mode = GameMode.IncenseWalk, ClassLevel = 4 }, 4242);
            for (var i = 0; i < 5; i++)
            {
                run.Incense.Tick(70f);
                run.Incense.OnPuzzleSolved(i < 3 ? 5 : 6, 2, i % 2, true);
                run.AdvanceIncenseLevel();
            }
            run.Incense.Tick(run.Incense.SecondsLeft - 352f);   // 5:52, the mistake below takes it to 5:42
            var map = host.GetComponentInChildren<RunMapController>(true);
            map.BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            yield return new WaitForSecondsRealtime(0.4f);
            var board = run.CurrentBoard;
            for (var r = 0; r < board.Size && run.CurrentLevelState.Mistakes == 0; r++)
                for (var c = 0; c < board.Size && run.CurrentLevelState.Mistakes == 0; c++)
                    if (board.Cells[r, c] == 0) run.PlaceNumber(r, c, board.Solution[r, c] % board.Size + 1);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "after_incense_walk.png");

            run.Incense.Tick(run.Incense.SecondsLeft - 24f);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "after_incense_walk_late.png");

            run.Incense.Tick(60f);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shoot(host, camera, "after_incense_walk_result.png");
        }

        // UI-STYLE-029: the stamp shop with nine ink stamps (Indigo chosen), then the looks in the game: a board in indigo
        // ink, Select Class with the moss ring.
        [UnityTest]
        public IEnumerator RenderStampShop()
        {
            var goals = new DailyGoalState { TotalInkStamps = 9 };
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { DailyGoals = goals });
            SaveFileService.FlushSharedPendingWrites();
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotShopEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var inputModules = Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None);
            foreach (var module in inputModules) module.enabled = false;
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotShop");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            try
            {
                menu.ShowMainMenu();
                yield return new WaitForSecondsRealtime(0.3f);
                menu.ShowGameModes();
                yield return new WaitForSecondsRealtime(0.3f);
                menu.OpenStampShop();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(host, camera, "after_stamp_shop.png");

                var owned = new DailyGoalState { TotalInkStamps = 20 };
                Meta.InkStampShop.TryBuy(owned, Meta.InkStampShop.Catalog.First(l => l.Key == "MossRing"));
                Meta.InkStampShop.TryBuy(owned, Meta.InkStampShop.Catalog.First(l => l.Key == "Indigo"));
                menu.StartGame();
                EventSystem.current.SetSelectedGameObject(Find(host, "BtnClassNumberFreak"));
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(host, camera, "after_stamp_seal_frame.png");

                host.SetActive(false);   // the menu's canvas would lie over the puzzle
                _puzzleSize = 6;
                var (puzzleHost, _) = StartRun(7321, true);
                yield return DismissIntro();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shoot(puzzleHost, camera, "after_stamp_board_ink.png");
            }
            finally
            {
                Meta.InkStampShop.Apply(new DailyGoalState());
                foreach (var module in inputModules) if (module != null) module.enabled = true;
            }
        }

        // UI-STYLE-012: the Custom Puzzle screen at 9×9, 3 stars, with Dutch Whispers, Killer Cages and Skyscrapers chosen
        // (the mockup's example). ToggleModifier is looked up by name, so the same test also shoots the old panel.
        [UnityTest]
        public IEnumerator RenderCustomPuzzle()
        {
            var prefix = Environment.GetEnvironmentVariable("SR_SHOT_PREFIX");
            if (string.IsNullOrEmpty(prefix)) prefix = "after_";
            var camera = CreateCamera();
            var host = new GameObject("LayoutShotMainMenu");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(host, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            host.GetComponent<MainMenuController>().ShowMainMenu();
            yield return null;
            Find(host, "BtnTutorial").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var setup = host.GetComponentInChildren<TutorialMenuController>(true);
            setup.SetBoardSizeIndex(4);
            setup.SetStarsIndex(2);
            var toggle = typeof(TutorialMenuController).GetMethod("ToggleModifier");
            if (toggle != null)
                foreach (var id in new[] { BossModifierId.DutchWhispers, BossModifierId.KillerCages, BossModifierId.Skyscrapers })
                    toggle.Invoke(setup, new object[] { id });
            EventSystem.current?.SetSelectedGameObject(null);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, prefix + "custom_puzzle.png");
        }

        // BOSS-MOD-002 / GAP-048: the line colours on real generated 9×9 puzzles. Every line on a board belongs to a rule
        // on its slip: Dutch Whispers (orange), Thermo (grey with a bulb) and Consecutive Line (gold); German Whispers,
        // Renban and Palindrome; Parity, Between, Slow Thermo and Unique Set.
        [UnityTest]
        public IEnumerator RenderLineTypes()
        {
            var camera = CreateCamera();
            var shots = new (string file, BossModifierId[] mods)[]
            {
                ("after_lines_dutch_thermo_consecutive.png", new[] { BossModifierId.DutchWhispers, BossModifierId.Thermo, BossModifierId.ConsecutiveLine }),
                ("after_lines_german_renban_palindrome.png", new[] { BossModifierId.GermanWhispers, BossModifierId.RenbanLines, BossModifierId.Palindrome }),
                ("after_lines_parity_between_slow_unique.png", new[] { BossModifierId.ParityLines, BossModifierId.BetweenLines, BossModifierId.SlowThermo, BossModifierId.UniqueSetLine }),
            };
            _puzzleSize = 9;
            var seed = 7340;
            foreach (var (file, mods) in shots)
            {
                var (host, _) = StartRun(seed++, true, mods);
                yield return DismissIntro();
                for (var frame = 0; frame < 30 && Find(host, "C_0_0") == null; frame++)
                    yield return null;
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shoot(host, camera, file);
                host.SetActive(false);
            }
            _puzzleSize = 6;
        }

        // GEN-MOD-002: the catalog batch B1/B2 modifiers on generated 9×9 boards: six line variants, then the thermometer
        // without a bulb, Difference ≥ 3, Multiples of 3, Mirror Cells and the complete white-dot set.
        [UnityTest]
        public IEnumerator RenderCatalogModifiers()
        {
            var camera = CreateCamera();
            var shots = new (string file, BossModifierId[] mods)[]
            {
                ("after_catalog_lines.png", new[] { BossModifierId.NonconsecLine, BossModifierId.HighLowAlternating, BossModifierId.NabnerLine,
                    BossModifierId.EntropicLine, BossModifierId.ModularLine, BossModifierId.ZipperLine }),
                ("after_catalog_marks.png", new[] { BossModifierId.AmbiguousThermo, BossModifierId.FastThermo, BossModifierId.DifferenceK,
                    BossModifierId.MultipleCells, BossModifierId.MirrorCells, BossModifierId.FullWhiteKropki }),
                ("after_catalog_cages.png", new[] { BossModifierId.KillerHiddenSum, BossModifierId.CageProduct,
                    BossModifierId.CageDifference, BossModifierId.RenbanCage }),
                ("after_catalog_arrows_region_sum.png", new[] { BossModifierId.ArrowAverage, BossModifierId.DoubleArrow,
                    BossModifierId.RegionSumLine }),
                ("after_catalog_b6_lines.png", new[] { BossModifierId.WhisperGeneralized, BossModifierId.NLine,
                    BossModifierId.SumLine, BossModifierId.ConstraintNegationZone }),
                ("after_catalog_b6_pairs.png", new[] { BossModifierId.RatioN, BossModifierId.DistancePair,
                    BossModifierId.CageRatio, BossModifierId.PillArrow }),
                ("after_catalog_b6_cells.png", new[] { BossModifierId.OppositeCellsSum, BossModifierId.CountingCircles,
                    BossModifierId.DistanceCell }),
                ("after_catalog_skyscrapers_quadruples.png", new[] { BossModifierId.Skyscrapers, BossModifierId.Quadruples }),
                ("after_catalog_sandwich.png", new[] { BossModifierId.Sandwich }),
                ("after_catalog_xsums.png", new[] { BossModifierId.XSums }),
                ("after_catalog_little_killer.png", new[] { BossModifierId.LittleKiller }),
                ("after_catalog_x_hyper_magic.png", new[] { BossModifierId.XSudoku, BossModifierId.HyperSudoku,
                    BossModifierId.MagicSquareRegion }),
                ("after_catalog_disjoint_extra.png", new[] { BossModifierId.DisjointGroups, BossModifierId.ExtraRegion }),
            };
            _puzzleSize = 9;
            var seed = 7330;
            foreach (var (file, mods) in shots)
            {
                var (host, _) = StartRun(seed++, true, mods);
                yield return DismissIntro();
                for (var frame = 0; frame < 30 && Find(host, "C_0_0") == null; frame++)
                    yield return null;
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shoot(host, camera, file);
                host.SetActive(false);
            }
            _puzzleSize = 6;
        }

        private static KillerCage Cage(RunDirector run, params (int r, int c)[] cells)
        {
            var cage = new KillerCage();
            foreach (var (r, c) in cells)
            {
                cage.Cells.Add(new CellCoord(r, c));
                cage.Sum += run.CurrentBoard.Solution[r, c];
            }
            return cage;
        }

        private Camera CreateCamera()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var camera = new GameObject("LayoutShotCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            _createdRoots.Add(camera.gameObject);
            return camera;
        }

        private (GameObject host, RunDirector run) StartRun(int seed, bool startPuzzle, params BossModifierId[] modifiers)
        {
            var host = new GameObject("LayoutShotHost" + seed);
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1
            }, seed);
            _beforeBindAlways?.Invoke(run);
            if (startPuzzle)
            {
                var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
                run.TryAdvanceToNode(node.Index);
                var config = run.BuildLevelConfig(false, false, node.Index);
                config.BoardSize = _puzzleSize;
                config.RegionVariant = _regionVariant;
                config.ActiveModifiers.Clear();
                config.ActiveModifiers.AddRange(modifiers);
                run.StartLevel(config);
                _beforeBind?.Invoke(run);
            }
            var map = host.GetComponentInChildren<RunMapController>(true);
            map.BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
        }

        private static IEnumerator DismissIntro()
        {
            yield return null;
            yield return null;
            var intro = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "RunIntroCard");
            if (intro != null) intro.onClick.Invoke();
            foreach (var controller in Object.FindObjectsByType<InRunController>(FindObjectsSortMode.None))
                controller.DismissFloorEntry(false);   // UI-STYLE-021: the floor's painting comes after the class card
            yield return null;
            yield return null;
        }

        private IEnumerator Shoot(GameObject host, Camera camera, string file)
        {
            var rt = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = rt;
            foreach (var canvas in host.GetComponentsInChildren<Canvas>(true).Where(c => c.isRootCanvas))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }
            yield return null;
            yield return null;
            // Texts built before the font atlas grew keep stale glyph coordinates (small labels smear): rebuild them all
            // against the final atlas before rendering.
            for (var pass = 0; pass < 2; pass++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var text in host.GetComponentsInChildren<Text>()) text.SetAllDirty();
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(_outDir, file), tex.EncodeToPNG());
            camera.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        private static GameObject Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)?.gameObject;
    }
}
