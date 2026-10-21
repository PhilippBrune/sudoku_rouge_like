using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-STYLE-019] Meta Progression as the garden of classes: plaques with level or unlock goal (META-UNLOCK-006),
    // the chosen class on the scroll with runs and best floor (META-STATS-001) and its level path (META-XP-006), the
    // totals, prestige with two presses (META-PRESTIGE-004) and the D-pad links.
    [TestFixture]
    [Requirement("UI-STYLE-019")]
    public sealed class ClassGardenPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private const int MonkLevel = 9;

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
            yield return null;
        }

        // Number Freak at Level 40 with one prestige star and the bosses for the next; Garden Monk at Level 9 after
        // 4 runs (best floor 3); the Koi Gambler one relic short of its unlock.
        private static MetaProgressionState SeededMeta()
        {
            var meta = new MetaProgressionState
            {
                ClassUnlocks = new ClassUnlockProgress
                {
                    BossesDefeated = ClassGardenProgressionService.RequiredBossesForPrestige(1),
                    RelicsCollected = 9
                },
                MaxUnlockedHarmonyLevel = 4
            };
            meta.UnlockedClasses.Add(ClassId.GardenMonk);
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
            {
                ClassId = ClassId.NumberFreak, TotalXp = XpTable.CumulativeXpForLevel(XpTable.MaxLevel), PrestigeTier = 1
            });
            meta.GardenProgression.ClassEntries.Add(new ClassGardenProgressEntry
            {
                ClassId = ClassId.GardenMonk, TotalXp = XpTable.CumulativeXpForLevel(MonkLevel) + 10, GardenRuns = 4, BestFloor = 3
            });
            return meta;
        }

        [UnityTest]
        [Requirement("META-UNLOCK-006")]
        public IEnumerator Plaques_ShowLevelsForUnlockedClassesAndGoalsForLockedOnes()
        {
            var host = OpenGarden();
            yield return null;

            Assert.AreEqual(8, host.GetComponentsInChildren<Button>(true).Count(b => b.name.StartsWith("ClassPlaque_")));
            Assert.AreEqual($"Lv {MonkLevel}", PlaqueText(host, ClassId.GardenMonk, "Level"));
            Assert.AreEqual("Collect 10 relics · 9/10", PlaqueText(host, ClassId.KoiGambler, "Level"));
            var koiIcon = Find<Transform>(host, "ClassPlaque_KoiGambler").Find("Icon").GetComponent<Image>();
            Assert.Less(koiIcon.color.a, 1f, "a locked class is a silhouette");
            Assert.AreEqual("★", PlaqueText(host, ClassId.NumberFreak, "Stars"));

            var totals = Find<Transform>(host, "MetaTotals");
            Assert.AreEqual("2 / 8", totals.Find("Total_0/Value").GetComponent<Text>().text);
            Assert.AreEqual("H4", totals.Find("Total_3/Value").GetComponent<Text>().text);
        }

        [UnityTest]
        [Requirement("META-STATS-001", "META-XP-006")]
        public IEnumerator Scroll_ShowsTheChosenClassWithRunsAndItsLevelPath()
        {
            var host = OpenGarden();
            yield return null;

            Click(host, "ClassPlaque_GardenMonk");
            yield return null;

            Assert.AreEqual("Garden Monk", Find<Text>(host, "ClassName").text);
            Assert.AreEqual($"Level {MonkLevel} of 40 · 4 runs · best floor 3", Find<Text>(host, "ClassSub").text);
            StringAssert.EndsWith("XP", Find<Text>(host, "ClassStrokeLeft").text);
            Assert.AreEqual($"to Level {MonkLevel + 1}", Find<Text>(host, "ClassStrokeRight").text);

            var steps = host.GetComponentsInChildren<Text>(true).Where(t => t.name.StartsWith("StepText_")).ToList();
            Assert.AreEqual(12, steps.Count, "one row per milestone level of the schedule");
            Assert.IsNull(Find<Transform>(host, "Step_0").Find("Hollow"), "Lv 3 is reached: a filled dot");
            Assert.NotNull(Find<Transform>(host, "Step_11").Find("Hollow"), "Lv 40 is not: a hollow dot");
            StringAssert.Contains("exclusive item ???", string.Join("|", steps.Select(s => s.text)), "the L15 item stays hidden");
            Assert.IsNull(FindOrNull(host, "BtnPrestige_GardenMonk"), "no prestige before Level 40");
            StringAssert.Contains("Level 40", Find<Text>(host, "ClassPrestigeStatus").text);
        }

        [UnityTest]
        [Requirement("META-PRESTIGE-004")]
        public IEnumerator Prestige_TakesTwoPresses_ThenKeepsTheClassChosen()
        {
            var host = OpenGarden();
            yield return null;
            Click(host, "ClassPlaque_NumberFreak");
            yield return null;

            Click(host, "BtnPrestige_NumberFreak");
            yield return null;
            StringAssert.StartsWith("Confirm", Find<Button>(host, "BtnPrestige_NumberFreak").GetComponentInChildren<Text>().text);
            Assert.AreEqual(1, Tier(ClassId.NumberFreak), "the first press only asks");

            Click(host, "BtnPrestige_NumberFreak");
            yield return null;

            Assert.AreEqual(2, Tier(ClassId.NumberFreak));
            Assert.AreEqual("Number Freak", Find<Text>(host, "ClassName").text);
            Assert.AreEqual("ClassPlaque_NumberFreak", SelectedName());
            StringAssert.StartsWith("Level 1 of 40", Find<Text>(host, "ClassSub").text);
        }

        [UnityTest]
        public IEnumerator Dpad_LinksRunThroughTheGridToBackAndThePrestigeButton()
        {
            var host = OpenGarden();
            yield return null;
            Click(host, "ClassPlaque_NumberFreak");
            yield return null;

            Selectable Plaque(ClassId id) => Find<Button>(host, $"ClassPlaque_{id}");
            var back = Find<Button>(host, "BtnMetaBack");
            var prestige = Find<Button>(host, "BtnPrestige_NumberFreak");

            Assert.AreSame(Plaque(ClassId.StoneGardener), Plaque(ClassId.NumberFreak).navigation.selectOnDown);
            Assert.AreSame(back, Plaque(ClassId.StoneGardener).navigation.selectOnDown);
            Assert.AreSame(prestige, Plaque(ClassId.KoiGambler).navigation.selectOnRight);
            Assert.AreSame(Plaque(ClassId.NumberFreak), prestige.navigation.selectOnLeft);
            Assert.AreSame(Plaque(ClassId.NumberFreak), back.navigation.selectOnUp, "Back leads up to the chosen class");
            Assert.AreEqual("ClassPlaque_NumberFreak", SelectedName(), "the garden opens on a class");
        }

        // No ?. on a GameObject: a destroyed selection is fake null.
        private static string SelectedName()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null ? selected.name : "nothing";
        }

        // German and French: every plaque line, every row of every class's level path (with its level) and the scroll's
        // lines stay readable: at 12 px they fit their box (best fit may shrink them that far, never further).
        [UnityTest]
        [Requirement("META-XP-006", "META-UNLOCK-006")]
        public IEnumerator GermanAndFrench_EveryLabelFitsAtAReadableSize()
        {
            const int readable = 12;
            var tooLong = new List<string>();
            foreach (var language in new[] { LanguageOption.German, LanguageOption.French })
            {
                LocalizationService.SetLanguage(language);
                var host = OpenGarden();
                yield return null;
                foreach (ClassId id in System.Enum.GetValues(typeof(ClassId)))
                {
                    Click(host, $"ClassPlaque_{id}");
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var labels = host.GetComponentsInChildren<Text>(true).Where(t =>
                        t.name.StartsWith("StepText_") || t.name.StartsWith("StepLevel_") || t.name == "ClassSub"
                        || t.name.StartsWith("ClassStroke") || (t.name == "Level" && t.transform.parent.name.StartsWith("ClassPlaque_")));
                    foreach (var label in labels)
                        if (!FitsAt(label, readable))
                            tooLong.Add($"{language} {id} {label.name}: \"{label.text}\"");
                }
                Object.Destroy(host);
                yield return null;
            }
            CollectionAssert.IsEmpty(tooLong.Distinct().ToList());
        }

        private static bool FitsAt(Text label, int size)
        {
            var rect = label.rectTransform.rect.size;
            var settings = label.GetGenerationSettings(rect);
            settings.resizeTextForBestFit = false;
            settings.fontSize = size;
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var height = new TextGenerator().GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
            return height <= rect.y + 0.5f;
        }

        private int Tier(ClassId id) => new ClassGardenProgressionService().GetPrestigeTier(new SaveFileService(TestSlot).Load().MetaProgress, id);

        private GameObject OpenGarden()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("GardenTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("GardenTestMainMenu");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            menu.OpenMetaProgression();
            Assert.IsTrue(Find<Transform>(host, "MetaProgressionPanel").gameObject.activeInHierarchy, "the garden opens");
            return host;
        }

        private static string PlaqueText(GameObject host, ClassId id, string part)
        {
            var label = Find<Transform>(host, $"ClassPlaque_{id}").Find(part);
            Assert.NotNull(label, $"{id} has no {part}");
            return label.GetComponent<Text>().text;
        }

        private static void Click(GameObject host, string name)
        {
            var button = Find<Button>(host, name);
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.interactable, $"{name} can be pressed");
            button.onClick.Invoke();
        }

        private static GameObject FindOrNull(GameObject host, string name) =>
            host.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)?.gameObject;

        private static T Find<T>(GameObject host, string name) where T : Component
        {
            var match = host.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
            Assert.NotNull(match, $"missing {name}");
            return match;
        }
    }
}
