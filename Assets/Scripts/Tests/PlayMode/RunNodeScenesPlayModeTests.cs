using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
    // The run's node scenes in the ink-and-paper style, driven through the real in-run UI:
    // [REQ: UI-STYLE-013] rest as tea bowls, [REQ: UI-STYLE-014] event as a hand scroll (GAP-052: it used to open
    // behind the path), [REQ: UI-STYLE-015] cursed tile as a talisman, [REQ: UI-STYLE-016] the run-end scroll.
    [TestFixture]
    [Requirement("UI-STYLE-013", "UI-STYLE-014", "UI-STYLE-015", "UI-STYLE-016")]
    public sealed class RunNodeScenesPlayModeTests
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
            DeleteSlotFiles();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        // GAP-052: the event opens as a scene drawn over the path. A choice that costs more gold than the player has is
        // greyed with the reason; a free choice is taken by choosing it twice; Continue closes the scene.
        [UnityTest]
        [Requirement("UI-STYLE-014")]
        public IEnumerator Event_OpensAboveThePath_UnaffordableChoicesAreGreyed_ContinueCloses()
        {
            var seed = SeedWithEvent(e => e.Options.Any(o => o.GoldCost > 0), out var expected);
            var (host, run) = StartRun(seed);
            yield return null;
            var cost = expected.Options.First(o => o.GoldCost > 0).GoldCost;
            run.State.CurrentGold = cost - 1;
            var flow = host.GetComponentInChildren<InRunUiFlowController>(true);
            flow.OnNodeEntered(NodeType.Event);
            yield return null;
            Assert.AreEqual(expected.Title, run.CurrentEvent.Title, "the UI run rolls the probed event");

            var scene = FindObjectOrNull(host, "EventScene");
            Assert.NotNull(scene, "the event opens its scene");
            var path = FindObject(host, "PathOverviewPanel");
            Assert.AreEqual(path.transform.parent, scene.transform.parent, "the scene is a sibling of the path");
            Assert.Greater(scene.transform.GetSiblingIndex(), path.transform.GetSiblingIndex(), "and is drawn over it");
            Assert.AreEqual(scene, flow.EventChoiceScreen.ActivePanel);

            var options = run.CurrentEvent.Options;
            var free = -1;
            for (var i = 0; i < options.Count; i++)
            {
                var strip = FindObject(scene, $"EventOption_{i}").GetComponent<Button>();
                var payable = options[i].GoldCost <= run.State.CurrentGold && options[i].PencilCost <= run.State.CurrentPencil;
                Assert.AreEqual(payable, strip.interactable, $"option {i} ({options[i].Label}) can be taken only when it can be paid");
                var why = FindObjectOrNull(strip.gameObject, "Why");
                Assert.AreEqual(!payable, why != null, $"option {i} says why it cannot be taken");
                if (!payable && options[i].GoldCost > 0)
                    StringAssert.Contains(options[i].GoldCost.ToString(), why.GetComponent<Text>().text, "the reason names the price");
                if (payable && options[i].GoldCost == 0 && free < 0) free = i;
            }
            Assert.GreaterOrEqual(free, 0, "every event has a free way out");

            var gold = run.State.CurrentGold;
            flow.EventChoiceScreen.SelectChoiceByIndex(options.FindIndex(o => o.GoldCost > 0));   // greyed: ignored
            Assert.IsFalse(FindObject(scene, "BtnEventMain").GetComponent<Button>().interactable, "a greyed choice cannot be chosen");
            flow.EventChoiceScreen.SelectChoiceByIndex(free);   // choose
            Assert.IsTrue(FindObject(scene, "BtnEventMain").GetComponent<Button>().interactable);
            Assert.IsEmpty(FindObject(scene, "EventResult").GetComponent<Text>().text, "choosing alone changes nothing");
            flow.EventChoiceScreen.SelectChoiceByIndex(free);   // take
            Assert.IsNotEmpty(FindObject(scene, "EventResult").GetComponent<Text>().text, "the result is shown");
            Assert.AreEqual(gold, run.State.CurrentGold, "a free choice costs no gold");
            Assert.IsTrue(Enumerable.Range(0, options.Count)
                .All(i => !FindObject(scene, $"EventOption_{i}").GetComponent<Button>().interactable), "one choice only");
            flow.EventChoiceScreen.SelectChoiceByIndex(free);   // a quick third tap does not skip the result
            Assert.NotNull(FindObjectOrNull(host, "EventScene"));

            ClickButton(scene, "BtnEventMain");   // Continue
            yield return null;
            Assert.IsNull(FindObjectOrNull(host, "EventScene"), "Continue closes the event");
            Assert.IsNull(flow.EventChoiceScreen.ActivePanel);
        }

        // An HP cost never blocks (it stops at 1 HP), and a paid choice opens once the player has exactly enough gold.
        [UnityTest]
        [Requirement("UI-STYLE-014")]
        public IEnumerator Event_HpCostsNeverBlock_AndExactGoldPays()
        {
            var hpSeed = SeedWithEvent(e => e.Options.Any(o => o.HpCost > 0 && o.GoldCost == 0 && o.PencilCost == 0), out var hpEvent);
            var (host, run) = StartRun(hpSeed);
            yield return null;
            run.State.CurrentHP = 1;
            var flow = host.GetComponentInChildren<InRunUiFlowController>(true);
            flow.OnNodeEntered(NodeType.Event);
            yield return null;
            Assert.AreEqual(hpEvent.Title, run.CurrentEvent.Title);
            var hpOption = run.CurrentEvent.Options.FindIndex(o => o.HpCost > 0);
            Assert.IsTrue(FindObject(FindObject(host, "EventScene"), $"EventOption_{hpOption}").GetComponent<Button>().interactable,
                "at 1 HP an HP-cost choice can still be taken");
            flow.OnEventClosed();
            Object.Destroy(host);   // the blueprint looks up a run map scene-wide; the second run needs its own
            yield return null;

            var goldSeed = SeedWithEvent(e => e.Options.Any(o => o.GoldCost > 0), out var goldEvent);
            var (host2, run2) = StartRun(goldSeed);
            yield return null;
            var paid = goldEvent.Options.FindIndex(o => o.GoldCost > 0);
            run2.State.CurrentGold = goldEvent.Options[paid].GoldCost;
            run2.State.CurrentPencil = run2.State.MaxPencil;
            host2.GetComponentInChildren<InRunUiFlowController>(true).OnNodeEntered(NodeType.Event);
            yield return null;
            Assert.IsTrue(FindObject(FindObject(host2, "EventScene"), $"EventOption_{paid}").GetComponent<Button>().interactable,
                "exactly enough gold pays");
        }

        // [REQ: CURSE-ACQUIRE-004] [REQ: CURSE-ACQUIRE-005] Three bowls without a curse, Cleanse as the fourth with one;
        // choosing marks a bowl, the same choice again drinks it, once.
        [UnityTest]
        [Requirement("UI-STYLE-013", "CURSE-ACQUIRE-004", "CURSE-ACQUIRE-005")]
        public IEnumerator Rest_ChooseThenDrink_AppliesOnce_CleanseOnlyWithACurse()
        {
            var (host, run) = StartRun(5151);
            yield return null;
            var reward = host.GetComponentInChildren<RewardViewController>(true);
            run.State.CurrentHP = 1;
            reward.ShowRestChoicePanel();
            yield return null;
            var scene = FindObject(host, "RestChoicePanel");
            Assert.Greater(scene.transform.GetSiblingIndex(), FindObject(host, "PathOverviewPanel").transform.GetSiblingIndex());
            Assert.NotNull(FindObjectOrNull(scene, "RestChoice_2"));
            Assert.IsNull(FindObjectOrNull(scene, "RestChoice_3"), "no Cleanse without a curse");

            reward.SelectRestByIndex(0);
            Assert.AreEqual(1, run.State.CurrentHP, "choosing alone does not heal");
            Assert.IsTrue(FindObject(scene, "BtnRestDrink").GetComponent<Button>().interactable);
            reward.SelectRestByIndex(0);
            Assert.Greater(run.State.CurrentHP, 1, "the same key again drinks");
            Assert.IsFalse(reward.IsRestOpen, "the rest closes after one bowl");
            var healed = run.State.CurrentHP;
            reward.SelectRestByIndex(0);
            Assert.AreEqual(healed, run.State.CurrentHP, "a closed rest does nothing");

            run.ApplyCurse("hollow_pencil");
            reward.ShowRestChoicePanel();
            yield return null;
            Assert.NotNull(FindObjectOrNull(FindObject(host, "RestChoicePanel"), "RestChoice_3"), "Cleanse with a curse");
            reward.SelectRestByIndex(3);
            reward.SelectRestByIndex(3);
            Assert.AreEqual(0, run.GetActiveCurses().Count, "Cleanse lifts the curse");
        }

        // [REQ: MAP-NODE-CURSED-003] Accept passes the cursed puzzle, Decline the same tile as a normal puzzle; B declines.
        [UnityTest]
        [Requirement("UI-STYLE-015", "MAP-NODE-CURSED-001", "MAP-NODE-CURSED-003")]
        public IEnumerator CursedTile_AcceptPassesTheCursedPuzzle_DeclineTheNormalOne()
        {
            var (host, run) = StartRun(5152);
            yield return null;
            var gate = host.GetComponentInChildren<BossGateViewController>(true);
            var path = FindObject(host, "PathOverviewPanel");
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First();
            LevelConfig accepted = null, declined = null;

            gate.ShowCursedNodePanel(path, node, cfg => accepted = cfg, cfg => declined = cfg);
            yield return null;
            var scene = FindObject(host, "CursedChoicePanel");
            Assert.Greater(scene.transform.GetSiblingIndex(), path.transform.GetSiblingIndex(), "the talisman covers the path");
            Assert.AreEqual(scene, gate.ActivePanel);
            Assert.IsTrue(CancelRouteContract.IsOverlayCancelControlName("BtnDeclineSkip"), "B presses Decline");
            ClickButton(scene, "BtnAccept");
            yield return null;
            Assert.NotNull(accepted);
            Assert.IsTrue(accepted.IsCursed);
            Assert.IsNull(FindObjectOrNull(host, "CursedChoicePanel"), "the choice closes the talisman");

            gate.ShowCursedNodePanel(path, node, cfg => accepted = cfg, cfg => declined = cfg);
            yield return null;
            ClickButton(FindObject(host, "CursedChoicePanel"), "BtnDeclineSkip");
            Assert.NotNull(declined);
            Assert.IsFalse(declined.IsCursed);
            Assert.AreEqual(1f, declined.CursedGoldMult);
        }

        // A Garden Run defeat on Floor 2: the scroll covers the screen, Floor 1 is sealed, Floor 2 crossed out, the back
        // button returns to the menu; another mode's end restores the text layout.
        [UnityTest]
        [Requirement("UI-STYLE-016", "SCORE-GRADE-003")]
        public IEnumerator RunEnd_GardenRunDefeat_SealsAndCrossesTheFloors_OtherModesKeepTheTextLayout()
        {
            var (host, run) = StartRun(5153);
            yield return null;
            run.AdvanceToNextFloor();
            run.State.CurrentHP = 0;
            var end = host.GetComponentInChildren<EndScreenViewController>(true);
            var returned = false;
            end.OnReturnToMenuPressed = () => returned = true;

            end.ShowGameOver(run, false);
            yield return null;
            var panel = FindObject(host, "GameOverPanel");
            Assert.IsTrue(panel.activeInHierarchy);
            var rt = (RectTransform)panel.transform;
            Assert.AreEqual(Vector2.zero, rt.anchorMin, "the scroll covers the whole screen");
            Assert.AreEqual(Vector2.one, rt.anchorMax);
            var scroll = FindObject(panel, "RunEndScroll");
            Assert.NotNull(FindObjectOrNull(FindObject(scroll, "FloorSeal_0"), "Mask"), "Floor 1 is sealed with its boss");
            Assert.NotNull(FindObjectOrNull(FindObject(scroll, "FloorSeal_1"), "CrossA"), "the lost floor is crossed out");
            Assert.IsNull(FindObjectOrNull(FindObject(scroll, "FloorSeal_2"), "Mask"));
            Assert.IsFalse(FindObject(panel, "GameOverDetails").activeSelf, "the text layout is hidden");
            Assert.NotNull(FindObjectOrNull(scroll, "NothingNew"));
            ClickButton(scroll, "BtnRunEndBack");
            Assert.IsTrue(returned, "the back button returns to the menu");

            // [REQ: SCORE-GRADE-003] One stamp per puzzle in the order played: a perfect and a rough puzzle on Floor 1, a
            // messy one on Floor 2, each with its grade's letter; the ledger names the grades.
            run.GetAnalytics().RecordPuzzleSolved(0, 0, 0);
            run.GetAnalytics().RecordPuzzleSolved(2, 0, 0);
            run.GetAnalytics().RecordPuzzleSolved(5, 0, 1);
            run.GetAnalytics().RecordRunComplete();   // as the game does when the Floor 5 boss falls
            end.ShowGameOver(run, true);   // a victory seals every floor and adds the victory row to the ledger
            yield return null;
            scroll = FindObject(panel, "RunEndScroll");
            for (var f = 0; f < run.State.TotalFloors; f++)
                Assert.NotNull(FindObjectOrNull(FindObject(scroll, $"FloorSeal_{f}"), "Mask"), $"floor {f + 1} is sealed");
            Assert.IsNull(FindObjectOrNull(scroll, "CrossA"));
            Assert.IsTrue(scroll.GetComponentsInChildren<Text>(true).Any(t => t.name.EndsWith("_Label")
                && t.text == LocalizationService.Format("InRun.End.Scroll.VictoryBonus", "Victory bonus (+{0}%)",
                    RunScoreBreakdown.VictoryPercent)), "the ledger has the victory row");
            var marks = Enumerable.Range(0, 3).Select(i => FindObject(FindObject(scroll, $"GradeStamp_{i}"), "Mark")
                .GetComponent<Text>().text).ToArray();
            CollectionAssert.AreEqual(new[] { "PerfectMark", "RoughMark", "MessyMark" }.Select(k => LocalizationService.T("InRun.End.Grade." + k)),
                marks, "Perfect, Rough, Messy in the order played");
            Assert.IsNull(FindObjectOrNull(scroll, "GradeStamp_3"));
            Assert.Greater(FindObject(scroll, "GradeStamp_2").GetComponent<RectTransform>().anchorMin.x
                    - FindObject(scroll, "GradeStamp_1").GetComponent<RectTransform>().anchorMin.x,
                FindObject(scroll, "GradeStamp_1").GetComponent<RectTransform>().anchorMin.x
                    - FindObject(scroll, "GradeStamp_0").GetComponent<RectTransform>().anchorMin.x, "a new floor starts after a gap");
            Assert.NotNull(FindObjectOrNull(scroll, "GradeLegend"));
            Assert.IsTrue(scroll.GetComponentsInChildren<Text>(true).Any(t => t.name.EndsWith("_Label")
                && t.text == LocalizationService.Format("InRun.End.Scroll.Messy", "Messy puzzles ×{0} (−{1}%)", 1,
                    RunScoreBreakdown.MessyPercent)), "the ledger has the messy row");

            run.State.Mode = GameMode.EndlessZen;
            end.ShowGameOver(run, false);
            yield return null;
            Assert.IsNull(FindObjectOrNull(panel, "RunEndScroll"), "another mode does not show the scroll");
            Assert.IsTrue(FindObject(panel, "GameOverDetails").activeSelf, "and gets the text layout back");
            Assert.AreNotEqual(Vector2.zero, rt.anchorMin);
        }

        private static LaunchRequest Request() =>
            new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 };

        // The event a fresh run rolls is fixed by its seed; find a seed whose first event has the wanted kind of choice.
        private static int SeedWithEvent(System.Func<RunEvent, bool> wanted, out RunEvent rolled)
        {
            for (var seed = 6000; seed < 6400; seed++)
            {
                var probe = new RunDirector();
                probe.StartRun(Request(), seed);
                rolled = probe.BuildCurrentEvent();
                if (rolled != null && wanted(rolled)) return seed;
            }
            Assert.Fail("no seed in 6000-6399 rolls such an event");
            rolled = null;
            return -1;
        }

        private (GameObject host, RunDirector run) StartRun(int seed)
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("NodeScenesCamera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                _createdRoots.Add(camera);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("NodeScenesEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("NodeScenesHost");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();

            var run = new RunDirector();
            run.StartRun(Request(), seed);
            host.GetComponentInChildren<RunMapController>(true).BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
        }

        private static void ClickButton(GameObject root, string name)
        {
            var go = FindObject(root, name);
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
