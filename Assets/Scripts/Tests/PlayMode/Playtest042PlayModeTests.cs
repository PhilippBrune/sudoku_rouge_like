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

namespace SudokuRoguelike.Tests
{
    // The playtests of 0.4.2 (the owner's Daily Walk run and Paul's notes): the fixes and the picks FB1 and FB5.
    [TestFixture]
    public sealed class Playtest042PlayModeTests
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
            TextSizeFit.SetScale(1f);
            DeleteSlotFiles();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        [Requirement("SHOP-MULTI-001")]
        public IEnumerator Shop_StaysOpenAfterAPurchase_AndStampsTheGoodSold()
        {
            var (host, run) = StartRun(5161);
            yield return null;
            run.State.CurrentGold = 999;
            var shop = host.GetComponentInChildren<ShopViewController>(true);
            shop.ShowShop();
            yield return null;
            var offers = run.CurrentShopOffers;
            var first = offers.FindIndex(o => o?.Item != null);
            if (first < 0) Assert.Inconclusive("this seed's shop has no item");
            shop.SelectSlotByIndex(first);
            ClickButton(FindObject(host, "ShopPanel"), "ShopBuyBtn");
            yield return null;
            Assert.NotNull(FindObjectOrNull(host, "ShopPanel"), "the stall stays open");
            Assert.IsTrue(offers[first].IsSold);
            Assert.NotNull(FindObjectOrNull(FindObject(host, $"Offer_{first}"), "Sold"), "the good is stamped sold");
            shop.SelectSlotByIndex(first);
            Assert.IsFalse(FindObject(host, "ShopBuyBtn").GetComponent<Button>().interactable, "a sold good cannot be bought again");
        }

        [UnityTest]
        [Requirement("REST-REFRESH-001")]
        public IEnumerator Rest_ThePathShowsTheHealedHp()
        {
            var (host, run) = StartRun(5162);
            yield return null;
            run.State.CurrentHP = 1;
            var reward = host.GetComponentInChildren<RewardViewController>(true);
            reward.ShowRestChoicePanel();
            yield return null;
            reward.SelectRestByIndex(0);
            reward.SelectRestByIndex(0);
            yield return null;
            StringAssert.Contains(run.State.CurrentHP.ToString(), FindObject(host, "PathStatsText").GetComponent<Text>().text);
        }

        [UnityTest]
        [Requirement("EVENT-OUTCOME-001")]
        public IEnumerator Event_SaysWhatHappened_WithTheNumbers()
        {
            var seed = SeedWithEvent(e => e.Options.Any(o => o.GoldCost > 0));
            var (host, run) = StartRun(seed);
            yield return null;
            run.State.CurrentGold = 500;
            var flow = host.GetComponentInChildren<InRunUiFlowController>(true);
            flow.OnNodeEntered(NodeType.Event);
            yield return null;
            var paid = run.CurrentEvent.Options.FindIndex(o => o.GoldCost > 0);
            flow.EventChoiceScreen.SelectChoiceByIndex(paid);
            flow.EventChoiceScreen.SelectChoiceByIndex(paid);
            var scene = FindObject(host, "EventScene");
            Assert.IsNotEmpty(FindObject(scene, "EventOutcomeText").GetComponent<Text>().text);
            StringAssert.Contains("500 →", FindObject(scene, "EventOutcomeDelta_0").GetComponent<Text>().text, "the gold before and after");
            Assert.IsFalse(FindObject(scene, $"EventOption_{paid}").activeInHierarchy, "the strips give way to the result");
        }

        [UnityTest]
        [Requirement("RULE-CARD-001")]
        public IEnumerator ANewRule_OpensACardFirst_OnlyOnce()
        {
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            var (host, _) = StartPuzzle(5163, BossModifierId.Thermo);
            for (var frame = 0; frame < 30 && FindObjectOrNull(host, "RuleCardLayer") == null; frame++) yield return null;
            var card = FindObject(host, "RuleCardLayer");
            StringAssert.Contains(Boss.BossService.GetModifierName(BossModifierId.Thermo), FindObject(card, "Name").GetComponent<Text>().text);
            ClickButton(card, "BtnRuleCardNext");
            yield return null;
            Assert.IsNull(FindObjectOrNull(host, "RuleCardLayer"));
            CollectionAssert.Contains(new SaveFileService(TestSlot).Load().MetaProgress.RulesExplained, BossModifierId.Thermo);

            Object.Destroy(host);   // one in-run UI at a time
            yield return null;
            var (again, _) = StartPuzzle(5164, BossModifierId.Thermo);
            for (var frame = 0; frame < 10; frame++) yield return null;
            Assert.IsNull(FindObjectOrNull(again, "RuleCardLayer"), "a rule is explained once");
        }

        [UnityTest]
        [Requirement("UI-CELL-FOCUS-001")]
        public IEnumerator TheChosenCell_WearsTheFrame()
        {
            var (host, run) = StartPuzzle(5165);
            var cell = FirstFilled(run);
            var name = $"C_{cell.r}_{cell.c}";
            for (var frame = 0; frame < 30 && FindObjectOrNull(host, name) == null; frame++) yield return null;
            ClickButton(host, name);
            yield return null;
            var frameGo = FindObject(host, "SelectionFrame");
            Assert.IsTrue(frameGo.activeInHierarchy);
            Assert.AreEqual($"N_{cell.r}_{cell.c}", frameGo.transform.parent.name, "on the chosen cell, also one with a digit");
        }

        [UnityTest]
        [Requirement("FOG-SOLVED-001")]
        public IEnumerator ASolvedBoard_ShowsNoFog()
        {
            var (_, run) = StartPuzzle(5166, BossModifierId.FogOfWar);
            yield return null;
            Assert.Greater(run.CurrentOverlay.FogCells.Count, 0, "the puzzle starts fogged");
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) b.PlaceValue(r, c, b.Solution[r, c]);
            for (var frame = 0; frame < 5; frame++) yield return null;
            Assert.AreEqual(0, run.CurrentOverlay.FogCells.Count);
        }

        [UnityTest]
        [Requirement("ACCESS-FONT-005")]
        public IEnumerator TextSize_NeverBreaksAWord()
        {
            var canvas = new GameObject("FitCanvas", typeof(Canvas));
            _createdRoots.Add(canvas);
            var go = new GameObject("FitText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(130f, 80f);
            var text = go.GetComponent<Text>();
            text.font = FontAssetService.GetFont();
            text.fontSize = 22;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = "Combination x11!";
            go.AddComponent<TextSizeFit>();
            TextSizeFit.SetScale(1.5f);
            yield return null;
            yield return null;
            var settings = text.GetGenerationSettings(new Vector2(130f, 0f));
            Assert.LessOrEqual(text.cachedTextGeneratorForLayout.GetPreferredWidth("Combination", settings) / text.pixelsPerUnit, 130.5f);
        }

        // ── harness ──

        private GameObject BuildHost()
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Pt042Camera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                _createdRoots.Add(camera);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("Pt042EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("Pt042Host");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            return host;
        }

        private (GameObject host, RunDirector run) StartRun(int seed)
        {
            var host = BuildHost();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            host.GetComponentInChildren<RunMapController>(true).BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
        }

        private (GameObject host, RunDirector run) StartPuzzle(int seed, params BossModifierId[] modifiers)
        {
            var host = BuildHost();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false, ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = 6;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            config.ActiveModifiers.AddRange(modifiers);
            run.StartLevel(config);
            Assert.NotNull(run.CurrentBoard);
            host.GetComponentInChildren<RunMapController>(true).BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
        }

        private static int SeedWithEvent(System.Func<RunEvent, bool> wanted)
        {
            for (var seed = 6000; seed < 6400; seed++)
            {
                var probe = new RunDirector();
                probe.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
                var rolled = probe.BuildCurrentEvent();
                if (rolled != null && wanted(rolled)) return seed;
            }
            Assert.Fail("no seed in 6000-6399 rolls such an event");
            return -1;
        }

        private static (int r, int c) FirstFilled(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] != 0) return (r, c);
            return (0, 0);
        }

        private static void ClickButton(GameObject root, string name)
        {
            var go = FindObject(root, name);
            var button = go.GetComponent<Button>();
            Assert.IsTrue(go.activeInHierarchy, $"Button is not active in hierarchy: {name}");
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
