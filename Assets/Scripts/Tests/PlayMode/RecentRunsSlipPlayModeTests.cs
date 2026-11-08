using System;
using System.Collections;
using System.Collections.Generic;
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
    // RM-230, the owner's pick C: the Recent Runs slip on the real main menu.
    public sealed class RecentRunsSlipPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private readonly List<GameObject> _roots = new List<GameObject>();
        private int _originalSlot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            foreach (var root in _roots) if (root != null) Object.Destroy(root);
            _roots.Clear();
            SaveProfileService.ActiveSlot = _originalSlot;
            yield return null;
        }

        private GameObject BuildMenu(params RecentRun[] runs)
        {
            var save = new SaveFileService(TestSlot);
            var envelope = new SaveFileEnvelope();
            foreach (var run in runs) RecentRuns.Add(envelope, run);
            save.Save(envelope);
            SaveFileService.FlushSharedPendingWrites();
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _roots.Add(new GameObject("RecentRunsEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("RecentRunsMenuHost");
            _roots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            host.GetComponent<MainMenuController>().ShowMainMenu();
            return host;
        }

        private static RecentRun Lost(int seed) => new RecentRun
        {
            EndedUtc = DateTime.UtcNow.ToString("o"), ClassId = ClassId.NumberFreak, FloorReached = 2, Seed = seed, ActiveSeconds = 600, Garden = 1, Tile = 4,
            BoardSize = 6, Stars = 2, Digits = new string('1', 36), Givens = new string('1', 36), Regions = new string('0', 36),
            WrongRow = 3, WrongCol = 4, WrongDigit = 5, RightDigit = 3
        };

        private static Button Named(GameObject host, string name) => host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

        [UnityTest]
        [Requirement("RUN-HISTORY-002")]
        public IEnumerator WithoutAFinishedRun_TheSlipIsHidden()
        {
            var host = BuildMenu();
            yield return null;
            Assert.IsFalse(host.GetComponentInChildren<RecentRunsSlip>(true).gameObject.activeSelf);
        }

        [UnityTest]
        [Requirement("RUN-HISTORY-002")]
        public IEnumerator ARow_OpensItsCard_AndWalkSeedFillsSelectClass()
        {
            var host = BuildMenu(Lost(1111), Lost(482913));
            yield return null;
            Assert.IsTrue(host.GetComponentInChildren<RecentRunsSlip>(true).gameObject.activeInHierarchy);
            Assert.IsNotNull(Named(host, "RecentRun_1"), "a row per run");
            Assert.IsNull(Named(host, "RecentRun_2"));

            Named(host, "RecentRun_0").onClick.Invoke();
            yield return null;
            Assert.IsTrue(RecentRunsSlip.CardOpen);
            Assert.IsNotNull(GameObject.Find("CardMistake"), "a lost run names its last mistake");
            Assert.IsNotNull(GameObject.Find("Cell_3_4/Ring"), "and rings its cell");
            Named(host, "BtnRecentClose").onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsFalse(RecentRunsSlip.CardOpen);

            Named(host, "RecentRun_0").onClick.Invoke();   // newest first
            yield return null;
            Named(host, "BtnRecentWalk").onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsFalse(RecentRunsSlip.CardOpen);
            var seed = host.GetComponentsInChildren<InputField>(true).First(f => f.name == "SeedInput");
            Assert.IsTrue(seed.gameObject.activeInHierarchy, "Select Class is open");
            Assert.AreEqual("482913", seed.text);
        }

        [UnityTest]
        [Requirement("RUN-HISTORY-002")]
        public IEnumerator AMonthlyWalk_HasNoWalkSeed()
        {
            var run = Lost(77);
            run.MonthlyWalk = true;
            var host = BuildMenu(run);
            yield return null;
            Named(host, "RecentRun_0").onClick.Invoke();
            yield return null;
            Assert.IsTrue(RecentRunsSlip.CardOpen);
            Assert.IsNull(Named(host, "BtnRecentWalk"));
            Named(host, "BtnRecentClose").onClick.Invoke();
            yield return null;
        }
    }
}
