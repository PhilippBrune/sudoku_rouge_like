using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
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
    // [REQ: UI-STYLE-018] The Codex as a collector's book; [REQ: META-CODEX-005] unknown entries as ??? and a silhouette;
    // [REQ: META-CODEX-008] the Curses page.
    [TestFixture]
    [Requirement("UI-STYLE-018", "META-CODEX-005", "META-CODEX-008")]
    public sealed class CodexBookPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private const string FoundCurse = "inkblot";

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

        // One curse found; one item found before the Codex counted (TimesFound 0) and used three times since.
        private static MetaProgressionState SeededMeta()
        {
            var meta = new MetaProgressionState
            {
                DiscoveredCurses = new List<string> { FoundCurse },
                ItemCodex = new ItemCodexData()
            };
            meta.ItemCodex.Entries.Add(new ItemCodexEntry
            {
                ItemId = nameof(ItemType.MeditationStone), Discovered = true, DiscoveredDate = "2026-10-01", TimesUsed = 3
            });
            return meta;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in _createdRoots) if (root != null) Object.Destroy(root);
            _createdRoots.Clear();
            LocalizationService.SetLanguage(LanguageOption.English);
            SaveFileService.FlushSharedPendingWrites();
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
            SaveProfileService.ActiveSlot = _originalSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CursesPage_CountsFiltersAndShowsTheChosenCurse()
        {
            var host = OpenCodex();
            yield return null;

            Click(host, "BtnCodexCurses");
            yield return null;

            var total = CurseService.All.Count;
            StringAssert.Contains($"1/{total}", Find<Button>(host, "BtnCodexCurses").GetComponentInChildren<Text>().text);
            Assert.AreEqual(total, TileCount(host), "one tile per curse");

            Click(host, "BtnCodexFilterFound");
            yield return null;
            Assert.AreEqual(1, TileCount(host), "Found shows only the found curse");
            Assert.AreEqual(CurseService.GetLocalizedName(CurseService.GetDefinition(FoundCurse)), Find<Text>(host, "CodexName").text);
            Assert.NotNull(FindOrNull(host, "CodexFact_0_Label"), "a found curse says from which floor it comes");
            StringAssert.Contains(Mathf.RoundToInt(100f / total) + "%", Find<Text>(host, "Percent").text);

            Click(host, "BtnCodexFilterUnknown");
            yield return null;
            Assert.AreEqual(total - 1, TileCount(host), "Not yet found shows the rest");
            Assert.AreEqual("???", Find<Text>(host, "CodexName").text);
            Assert.IsNull(FindOrNull(host, "CodexFact_0_Label"), "an unknown entry keeps its facts hidden");
        }

        [UnityTest]
        public IEnumerator ItemsPage_NamesTheClassTheCollectionUnlocks()
        {
            var host = OpenCodex();
            yield return null;

            Click(host, "BtnCodexItems");
            yield return null;

            StringAssert.StartsWith("Items 1/", Find<Button>(host, "BtnCodexItems").GetComponentInChildren<Text>().text);
            Assert.Greater(TileCount(host), 0);
            StringAssert.Contains("unlock", Find<Text>(host, "CodexGoal").text);
        }

        [UnityTest]
        public IEnumerator Book_EveryBookmarkTileAndBackCanBeReachedWithTheDpad()
        {
            var host = OpenCodex();
            yield return null;
            Click(host, "BtnCodexRelics");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var reached = Reachable(Find<Button>(host, "BtnCodexRelics"));
            foreach (var name in new[] { "BtnCodexItems", "BtnCodexModifiers", "BtnCodexCurses", "BtnCodexFilterFound",
                         "CodexTile_0", "BtnItemsBack" })
                Assert.IsTrue(reached.Any(s => s.name == name), $"{name} cannot be reached from the Relics bookmark");
        }

        [UnityTest]
        [Requirement("META-CODEX-009")]
        public IEnumerator ItemsPage_ShowsHowOftenTheItemWasFoundAndUsed()
        {
            var host = OpenCodex();
            yield return null;
            Click(host, "BtnCodexItems");
            Click(host, "BtnCodexFilterFound");
            yield return null;

            Assert.AreEqual(1, TileCount(host));
            Assert.AreEqual("Found", Find<Text>(host, "CodexFact_0_Label").text);
            Assert.AreEqual("once", Find<Text>(host, "CodexFact_0_Value").text, "found before the counters: at least once");
            Assert.AreEqual("Used", Find<Text>(host, "CodexFact_1_Label").text);
            Assert.AreEqual("3 times", Find<Text>(host, "CodexFact_1_Value").text);
        }

        // [REQ: META-CODEX-010] The Items page filters by rarity (each press the next one, then every rarity again), and an
        // item a class claims names that class under "Best paired with" (the Meditation Stone starts the Garden Monk's runs).
        [UnityTest]
        [Requirement("META-CODEX-010")]
        public IEnumerator ItemsPage_FiltersByRarity_AndNamesTheClassAnItemBelongsTo()
        {
            var host = OpenCodex();
            yield return null;
            Click(host, "BtnCodexItems");
            yield return null;
            var every = TileCount(host);
            var epics = System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>()
                .Count(t => ItemService.IsTiered(t) || ItemService.GetFixedRarity(t) == ItemRarity.Epic);

            for (var i = 0; i < 3; i++) Click(host, "BtnCodexRarity");   // Normal, Rare, Epic
            yield return null;
            Assert.AreEqual(epics, TileCount(host), "every item that can be Epic");
            Assert.Less(TileCount(host), every);
            Assert.AreEqual("Epic", Find<Button>(host, "BtnCodexRarity").GetComponentInChildren<Text>().text);
            Click(host, "BtnCodexRarity");
            yield return null;
            Assert.AreEqual(every, TileCount(host), "the fourth press shows every rarity again");

            Click(host, "BtnCodexFilterFound");
            yield return null;
            Assert.AreEqual("Best paired with", Find<Text>(host, "CodexFact_3_Label").text);
            StringAssert.Contains("Garden Monk", Find<Text>(host, "CodexFact_3_Value").text);

            Click(host, "BtnCodexRelics");
            yield return null;
            Assert.IsFalse(Find<Button>(host, "BtnCodexRarity").gameObject.activeSelf, "rarity belongs to the Items page");
        }

        [UnityTest]
        [Requirement("META-CODEX-005")]
        public IEnumerator UnknownEntries_ShowQuestionMarksAndSilhouettesAndHideTheirKind()
        {
            var host = OpenCodex();
            yield return null;
            Click(host, "BtnCodexCurses");
            Click(host, "BtnCodexFilterUnknown");
            yield return null;

            var tile = Find<Transform>(host, "CodexTile_0");
            Assert.AreEqual("???", tile.Find("Name").GetComponent<Text>().text);
            Assert.Less(tile.Find("Icon").GetComponent<Image>().color.a, 1f, "an unknown icon is a faint silhouette");
            Assert.IsNull(FindOrNull(host, "CodexPill_0"), "an unknown entry does not show its severity");
        }

        [UnityTest]
        public IEnumerator FilterAndEmptyPages_KeepTheControllerFocus()
        {
            var host = OpenCodex();
            yield return null;
            var codex = host.GetComponentInChildren<ItemsMenuController>(true);
            var eventSystem = EventSystem.current;

            Assert.AreEqual("CodexTile_0", SelectedName(eventSystem), "the Codex opens on its first tile");

            Click(host, "BtnCodexCurses");
            yield return null;
            eventSystem.SetSelectedGameObject(Find<Transform>(host, "CodexTile_2").gameObject);
            codex.CycleFilter();   // Y: All → Found destroys the focused tile
            yield return null;
            Assert.AreEqual("CodexTile_0", SelectedName(eventSystem), "the focus moves to the first tile");

            Click(host, "BtnCodexRelics");   // no relic found yet
            yield return null;
            eventSystem.SetSelectedGameObject(Find<Transform>(host, "CodexTile_0").gameObject);
            codex.CycleFilter();
            yield return null;
            Assert.AreEqual(0, TileCount(host));
            Assert.AreEqual("BtnCodexFilterFound", SelectedName(eventSystem),
                "with no tiles the focus stays on the filter");
        }

        [UnityTest]
        public IEnumerator Grid_DpadLinksRunFromTheFilterThroughTheRowsToBack()
        {
            const int columns = 5;
            var host = OpenCodex();
            yield return null;
            Click(host, "BtnCodexCurses");
            yield return null;

            var count = TileCount(host);
            var lastRowStart = (count - 1) / columns * columns;
            Selectable Tile(int i) => Find<Button>(host, $"CodexTile_{i}");
            var back = Find<Button>(host, "BtnItemsBack");
            var filterAll = Find<Button>(host, "BtnCodexFilterAll");

            Assert.AreSame(filterAll, Find<Button>(host, "BtnCodexCurses").navigation.selectOnDown);
            Assert.AreSame(filterAll, Tile(0).navigation.selectOnUp);
            Assert.AreSame(Tile(0), filterAll.navigation.selectOnDown);
            Assert.AreSame(back, Tile(lastRowStart).navigation.selectOnDown);
            Assert.AreSame(back, Tile(count - 1).navigation.selectOnDown);
            Assert.AreSame(Tile(lastRowStart), back.navigation.selectOnUp);
            Assert.AreSame(Tile(Mathf.Min(lastRowStart - 1 + columns, count - 1)), Tile(lastRowStart - 1).navigation.selectOnDown,
                "a tile above a short last row goes down to its last tile");
            Assert.IsNull(Tile(columns - 1).navigation.selectOnRight, "the row ends at the page's edge");
        }

        [UnityTest]
        public IEnumerator Bookmarks_GermanAndFrenchLabelsFitTheirBookmarks()
        {
            foreach (var language in new[] { LanguageOption.German, LanguageOption.French })
            {
                LocalizationService.SetLanguage(language);
                var host = OpenCodex();
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (var name in new[] { "BtnCodexItems", "BtnCodexRelics", "BtnCodexModifiers", "BtnCodexCurses" })
                {
                    var label = Find<Button>(host, name).GetComponentInChildren<Text>();
                    var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                    settings.resizeTextForBestFit = false;
                    settings.fontSize = label.resizeTextMinSize;
                    settings.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var width = new TextGenerator().GetPreferredWidth(label.text, settings) / label.pixelsPerUnit;
                    Assert.Greater(width, 0f, $"{language} {name}: no text measured");
                    Assert.LessOrEqual(width, label.rectTransform.rect.width,
                        $"{language} {name} \"{label.text}\" does not fit its bookmark even at the smallest size");
                }
                Object.Destroy(host);
                yield return null;
            }
        }

        private GameObject OpenCodex()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("CodexTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var host = new GameObject("CodexTestMainMenu");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            menu.OpenItems();
            Assert.IsTrue(Find<Transform>(host, "ItemsPanel").gameObject.activeInHierarchy, "the Codex opens");
            return host;
        }

        // No ?. on a GameObject: a destroyed selection is fake null.
        private static string SelectedName(EventSystem eventSystem)
        {
            var selected = eventSystem.currentSelectedGameObject;
            return selected != null ? selected.name : "nothing";
        }

        private static int TileCount(GameObject host)
        {
            var grid = Find<Transform>(host, "CodexGrid");
            var count = 0;
            for (var i = 0; i < grid.childCount; i++)
                if (grid.GetChild(i).name.StartsWith("CodexTile_")) count++;
            return count;
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

        private static HashSet<Selectable> Reachable(Selectable start)
        {
            var seen = new HashSet<Selectable> { start };
            var queue = new Queue<Selectable>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in new[]
                         {
                             current.FindSelectableOnUp(), current.FindSelectableOnDown(),
                             current.FindSelectableOnLeft(), current.FindSelectableOnRight()
                         })
                    if (next != null && next.IsActive() && seen.Add(next))
                        queue.Enqueue(next);
            }
            return seen;
        }
    }
}
