using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
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
    // [REQ: UI-STYLE-024] Select Class · Who walks the garden: class seals with level or unlock goal, the chosen class on
    // the scroll, the run's settings on the strip and Begin without a confirmation window.
    // [REQ: UI-STYLE-025] Game Modes · Five paths: five curtains with their state, the focused mode's slip, Harmony shared
    // with Select Class.
    [TestFixture]
    [Requirement("UI-STYLE-024", "UI-STYLE-025")]
    public sealed class ModeScreensPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private const int FreakLevel = 5;

        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalSlot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            LocalizationService.SetLanguage(LanguageOption.English);
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { MetaProgress = SeededMeta() });
            SaveFileService.FlushSharedPendingWrites();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in _createdRoots) if (root != null) Object.Destroy(root);
            _createdRoots.Clear();
            SaveFileService.FlushSharedPendingWrites();
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
            SaveProfileService.ActiveSlot = _originalSlot;
            LocalizationService.SetLanguage(LanguageOption.English);
            yield return null;
        }

        // Number Freak at Level 5, the Garden Monk one boss short of its unlock, Harmony open up to IV, Endless Zen open.
        private static MetaProgressionState SeededMeta()
        {
            var meta = new MetaProgressionState
            {
                ClassUnlocks = new ClassUnlockProgress { BossesDefeated = 1 },
                MaxUnlockedHarmonyLevel = 4,
                EndlessZenUnlocked = true
            };
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
            {
                ClassId = ClassId.NumberFreak, TotalXp = XpTable.CumulativeXpForLevel(FreakLevel)
            });
            return meta;
        }

        [UnityTest]
        [Requirement("UI-STYLE-024", "META-UNLOCK-003")]
        public IEnumerator SelectClass_SealsShowLevelOrGoal_AndTheScrollShowsTheChosenClass()
        {
            var (host, menu) = OpenMenu();
            menu.StartGame();
            yield return null;

            var panel = Panel(host, "ClassSelectPanel");
            foreach (ClassId id in Enum.GetValues(typeof(ClassId)))
                Assert.NotNull(Find<Button>(panel, $"BtnClass{id}"), $"a seal for {id}");
            Assert.AreEqual($"Lv {FreakLevel}", Find<Button>(panel, "BtnClassNumberFreak").transform.Find("Level").GetComponent<Text>().text);
            var monk = Find<Button>(panel, "BtnClassGardenMonk");
            Assert.IsFalse(monk.interactable, "a locked class cannot be chosen");
            Assert.IsTrue(monk.transform.Find("LockOverlay").gameObject.activeSelf);
            StringAssert.EndsWith("1/2", monk.transform.Find("Level").GetComponent<Text>().text, "the seal shows the unlock goal");

            Assert.AreEqual("Number Freak", Find<Text>(panel, "ChosenClassName").text);
            StringAssert.StartsWith($"Level {FreakLevel} of {XpTable.MaxLevel}", Find<Text>(panel, "ChosenClassSub").text);
            for (var i = 0; i < 5; i++) Assert.NotNull(Find<Transform>(panel, $"Stat_{i}"), $"stat box {i}");
            Assert.NotNull(Find<Transform>(panel, "ClassPassive"));

            menu.SetSelectedClass(ClassId.GardenMonk);
            yield return null;
            Assert.AreEqual("Garden Monk", Find<Text>(panel, "ChosenClassName").text);
            StringAssert.StartsWith("Locked", Find<Text>(panel, "ChosenClassSub").text);
        }

        [UnityTest]
        [Requirement("UI-STYLE-024", "REQ-RUN-100")]
        public IEnumerator SelectClass_TheStripHoldsTheRunSettings_AndThereIsNoConfirmationWindow()
        {
            var (host, menu) = OpenMenu();
            menu.StartGame();
            yield return null;

            var panel = Panel(host, "ClassSelectPanel");
            Assert.AreEqual("Begin the run", Find<Button>(panel, "BtnConfirmClass").GetComponentInChildren<Text>().text);
            Assert.IsTrue(Find<Transform>(panel, "HarmonyArea").gameObject.activeSelf, "a Garden Run sets Harmony here");
            Assert.IsFalse(Find<Transform>(panel, "ModeInfo").gameObject.activeSelf);
            Assert.IsFalse(Find<Transform>(panel, "AbandonWarning").gameObject.activeSelf, "no saved run, no warning");
            Assert.NotNull(Find<InputField>(panel, "SeedInput"), "the seed sits on the class scroll");
            Assert.IsNull(FindOrNull(host, "StartRunConfirmOverlay"), "the confirmation window is gone");
            Assert.AreEqual("GARDEN RUN", Find<Text>(panel, "ClassSelectEyebrow").text);

            Click(panel, "BtnWeeklySeed");
            Assert.AreEqual(WeeklySeedService.SeedFor(DateTime.Today).ToString(), Find<InputField>(panel, "SeedInput").text);

            menu.BackToMainMenu();
            menu.StartEndlessZen();
            yield return null;
            Assert.IsFalse(Find<Transform>(panel, "HarmonyArea").gameObject.activeSelf, "Harmony belongs to the Garden Run");
            Assert.IsTrue(Find<Transform>(panel, "ModeInfo").gameObject.activeSelf);
            Assert.AreEqual("ENDLESS ZEN", Find<Text>(panel, "ClassSelectEyebrow").text);
        }

        [UnityTest]
        [Requirement("UI-STYLE-025")]
        public IEnumerator GameModes_FiveCurtains_FocusShowsTheirSlip()
        {
            var (host, menu) = OpenMenu();
            menu.ShowGameModes();
            yield return null;

            var panel = Panel(host, "GameModesPanel");
            var names = new[] { "BtnGarden", "BtnEndless", "BtnTrials", "BtnDailyWalk", "BtnMonthlyWalk" };
            var titles = new[] { "Garden Run", "Endless Zen", "Spirit Trials", "Daily Walk", "Monthly Walk" };
            for (var i = 0; i < names.Length; i++)
                Assert.AreEqual(titles[i], Find<Button>(panel, names[i]).transform.Find("Label").GetComponent<Text>().text);
            Assert.IsTrue(Find<Transform>(panel, "Slip_Garden").gameObject.activeSelf, "the Garden Run slip shows first");
            Assert.AreEqual(11, panel.GetComponentsInChildren<Image>(true).Count(img => img.name.StartsWith("HarmonyStone_")));
            Assert.AreEqual("Harmony 0 of IV", Strip(panel, "BtnGarden"));
            Assert.IsFalse(Find<Button>(panel, "BtnTrials").interactable, "Spirit Trials stay locked before the first boss");
            Assert.AreEqual("Defeat your first boss", Strip(panel, "BtnTrials"));

            EventSystem.current.SetSelectedGameObject(Find<Button>(panel, "BtnEndless").gameObject);
            yield return null;
            Assert.IsTrue(Find<Transform>(panel, "Slip_Endless").gameObject.activeSelf, "focus shows the mode's slip");
            Assert.IsFalse(Find<Transform>(panel, "Slip_Garden").gameObject.activeSelf);
            StringAssert.Contains("Best depth 0", Find<Transform>(panel, "Slip_Endless").Find("Body").GetComponent<Text>().text);
            Assert.AreSame(Find<Button>(panel, "BtnModesBack"), Find<Button>(panel, "BtnTrials").navigation.selectOnDown,
                "down from a locked mode goes to Back, not to its switched-off way in");
            Assert.AreSame(Find<Button>(panel, "BtnZenRecords"), Find<Button>(panel, "BtnEndless").navigation.selectOnDown);

            EventSystem.current.SetSelectedGameObject(Find<Button>(panel, "BtnDailyWalk").gameObject);
            yield return null;
            var goals = Find<Transform>(panel, "Slip_Daily").Find("Body").GetComponent<Text>().text;
            Assert.AreEqual(3, goals.Split('\n').Count(line => line.Contains("•")), "today's three goals");

            menu.BackToMainMenu();
            menu.ShowGameModes();
            yield return null;
            Assert.IsTrue(Find<Transform>(panel, "Slip_Garden").gameObject.activeSelf, "Game Modes opens on the Garden Run again");
        }

        [UnityTest]
        [Requirement("UI-STYLE-025", "UI-STYLE-024")]
        public IEnumerator Harmony_SetOnGameModes_IsTheHarmonyOfSelectClass()
        {
            var (host, menu) = OpenMenu();
            menu.ShowGameModes();
            yield return null;

            var modes = Panel(host, "GameModesPanel");
            for (var i = 0; i < 4; i++) Click(modes, "BtnHarmonyNext");
            yield return null;
            Assert.AreEqual("Harmony IV of IV", Strip(modes, "BtnGarden"));
            Assert.IsFalse(Find<Button>(modes, "BtnHarmonyNext").interactable, "Harmony V is not open yet");
            Assert.IsTrue(Find<Button>(modes, "BtnHarmonyPerk").interactable, "perks from Harmony IV");
            Assert.AreSame(Find<Button>(modes, "BtnHarmonyPerk"), Find<Button>(modes, "BtnGardenEnter").navigation.selectOnUp,
                "the D-pad reaches the perk from the way in once it can be used");

            Click(modes, "BtnGarden");
            yield return null;
            var select = Panel(host, "ClassSelectPanel");
            Assert.IsTrue(select.activeInHierarchy, "the Garden Run curtain leads to Select Class");
            StringAssert.Contains("Harmony IV", Find<Text>(select, "HarmonyReadout").text);

            Click(select, "BtnHarmonyPrevClass");
            yield return null;
            Assert.AreEqual("Harmony III of IV", Strip(modes, "BtnGarden"), "a change on Select Class reaches Game Modes");
        }

        // The scroll follows the profile: a class that levelled up since the last visit shows its new level, and a seed left
        // in the field with Back does not reach the next run.
        [UnityTest]
        [Requirement("UI-STYLE-024", "REQ-RUN-100")]
        public IEnumerator SelectClass_ShowsTheClassAsItIsNow_AndOpensWithAnEmptySeed()
        {
            var (host, menu) = OpenMenu();
            menu.StartGame();
            yield return null;
            var panel = Panel(host, "ClassSelectPanel");
            Find<InputField>(panel, "SeedInput").text = "424242";
            menu.BackToMainMenu();

            var meta = SeededMeta();
            meta.GardenProgression.ClassEntries[0].TotalXp = XpTable.CumulativeXpForLevel(FreakLevel + 3);
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { MetaProgress = meta });
            SaveFileService.FlushSharedPendingWrites();
            menu.StartGame();
            yield return null;

            StringAssert.StartsWith($"Level {FreakLevel + 3} of", Find<Text>(panel, "ChosenClassSub").text);
            Assert.AreEqual(string.Empty, Find<InputField>(panel, "SeedInput").text, "a new visit starts without the old seed");
        }

        // A language change rebuilds the menu in place: each seal and curtain exists once, with the new language's text.
        [UnityTest]
        [Requirement("UI-STYLE-024", "UI-STYLE-025")]
        public IEnumerator Rebuild_AfterALanguageChange_LeavesOneOfEachSealAndCurtain()
        {
            var (host, menu) = OpenMenu();
            yield return null;
            LocalizationService.SetLanguage(LanguageOption.German);
            host.GetComponent<MainMenuBlueprintBuilder>().ForceRebuild();
            yield return null;

            foreach (var name in new[] { "BtnGarden", "BtnEndless", "BtnTrials", "BtnDailyWalk", "BtnMonthlyWalk", "BtnZenRecords",
                         "BtnClassNumberFreak", "BtnConfirmClass", "BtnHarmonyPerk" })
                Assert.AreEqual(1, host.GetComponentsInChildren<Button>(true).Count(b => b.name == name), $"one {name}");
            menu.ShowGameModes();
            yield return null;
            Assert.AreEqual(LocalizationService.T("Garden Run"),
                Find<Button>(Panel(host, "GameModesPanel"), "BtnGarden").transform.Find("Label").GetComponent<Text>().text);
            Assert.AreEqual("Gartenrunde", LocalizationService.T("Garden Run"));
        }

        // German and French: the curtains, strips and slips keep a readable size (11 px, or the label's own size if smaller).
        [UnityTest]
        [Requirement("UI-STYLE-025")]
        public IEnumerator GermanAndFrench_CurtainAndSlipTextsFit()
        {
            const int readable = 11;
            var tooLong = new List<string>();
            foreach (var language in new[] { LanguageOption.German, LanguageOption.French })
            {
                LocalizationService.SetLanguage(language);
                var (host, menu) = OpenMenu();
                menu.ShowGameModes();
                yield return null;
                var panel = Panel(host, "GameModesPanel");
                foreach (var name in new[] { "BtnGarden", "BtnEndless", "BtnTrials", "BtnDailyWalk", "BtnMonthlyWalk" })
                {
                    EventSystem.current.SetSelectedGameObject(Find<Button>(panel, name).gameObject);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in panel.GetComponentsInChildren<Text>(false))
                        if (!FitsAt(label, Mathf.Min(readable, label.fontSize)))
                            tooLong.Add($"{language} {name} {label.name}: \"{label.text}\"");
                }
                Object.Destroy(host);
                yield return null;
            }
            CollectionAssert.IsEmpty(tooLong.Distinct().ToList());
        }

        private static string Strip(GameObject panel, string curtain) =>
            Find<Button>(panel, curtain).transform.Find("State/StateText").GetComponent<Text>().text
                .Replace("<color=#9E2A2B>", string.Empty).Replace("</color>", string.Empty);

        private static bool FitsAt(Text label, int size)
        {
            if (string.IsNullOrEmpty(label.text)) return true;
            var rect = label.rectTransform.rect.size;
            var settings = label.GetGenerationSettings(rect);
            settings.resizeTextForBestFit = false;
            settings.fontSize = size;
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var height = new TextGenerator().GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
            return height <= rect.y + 0.5f;
        }

        private (GameObject Host, MainMenuController Menu) OpenMenu()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("ModeScreensTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("ModeScreensTestMainMenu");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            return (host, menu);
        }

        private static GameObject Panel(GameObject host, string name)
        {
            var panel = Find<Transform>(host, name).gameObject;
            return panel;
        }

        private static void Click(GameObject root, string name)
        {
            var button = Find<Button>(root, name);
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.interactable, $"{name} can be pressed");
            button.onClick.Invoke();
        }

        private static GameObject FindOrNull(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        private static T Find<T>(GameObject root, string name) where T : Component
        {
            var match = root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
            Assert.NotNull(match, $"missing {name}");
            return match;
        }
    }
}
