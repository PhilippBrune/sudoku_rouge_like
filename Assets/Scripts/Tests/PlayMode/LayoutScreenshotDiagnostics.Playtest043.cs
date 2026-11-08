using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // The owner's picks after the 0.4.2 playtest: the Pattern Scroll's slip (RM-231, B), the Recent Runs slip and card (RM-230, C)
    // and the Custom Puzzle with seven rules and the pressure tab (RM-232).
    public sealed partial class LayoutScreenshotDiagnostics
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        private static void Call(object target, string method) =>
            target.GetType().GetMethod(method, Hidden, null, Type.EmptyTypes, null)?.Invoke(target, null);

        private static Button Named(GameObject host, string name) => host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

        [UnityTest]
        public IEnumerator RenderPatternScrollSlip()
        {
            var camera = CreateCamera();
            var (host, _) = StartRun(7450, true, BossModifierId.DutchWhispers, BossModifierId.Thermo, BossModifierId.ConsecutiveLine);
            yield return DismissIntro();
            yield return new WaitForSecondsRealtime(0.5f);
            var screen = host.GetComponentInChildren<InRunController>(true);
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            Call(screen, "ShowSudoku");
            Call(screen, "RenderBoard");
            yield return null;
            for (var i = 0; i < 4 && Named(host, "BtnRuleCardNext") != null; i++)
            {
                Named(host, "BtnRuleCardNext").onClick.Invoke();
                yield return null;
            }
            typeof(InRunController).GetField("_patternScrollPending", Hidden)?.SetValue(screen, true);
            typeof(InRunController).GetField("_patternScrollChargesLeft", Hidden)?.SetValue(screen, 2);
            Call(screen, "ShowPatternScrollSlip");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "pt043_pattern_scroll_first.png");
            Named(host, "MechBtn_2")?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "pt043_pattern_scroll_third.png");
            Named(host, "BtnConfirmScroll")?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "pt043_pattern_scroll_after_one.png");
            host.SetActive(false);
        }

        [UnityTest]
        public IEnumerator RenderRecentRuns()
        {
            var camera = CreateCamera();
            var save = new SaveFileService(TestSlot);
            var envelope = new SaveFileEnvelope();
            var solution = "123456456123231564564231312645645312";
            var digits = solution.ToCharArray();
            foreach (var i in new[] { 1, 5, 8, 10, 13, 16, 19, 21, 25, 30, 33 }) digits[i] = '0';
            var now = DateTime.UtcNow;
            foreach (var (cls, daily, won, floor, seed, hoursAgo, minutes) in new[]
                     {
                         (ClassId.KoiGambler, false, false, 3, 771045, 72.0, 39), (ClassId.ReedDuelist, false, true, 3, 66310, 30.0, 34),
                         ((ClassId)0, true, false, 1, 907211, 9.0, 14), (ClassId.GardenMonk, false, true, 3, 120577, 2.0, 28),
                         (ClassId.NumberFreak, false, false, 2, 482913, 0.1, 31)
                     })
                RecentRuns.Add(envelope, new RecentRun
                {
                    EndedUtc = now.AddHours(-hoursAgo).ToString("o"), ClassId = cls, DailyWalk = daily, Victory = won, FloorReached = floor, Seed = seed,
                    HarmonyLevel = 2, ActiveSeconds = minutes * 60, Garden = 2, Tile = 6, BoardSize = 6, Stars = 2, Digits = new string(digits),
                    Givens = new string('1', 36), Regions = "000111000111222333222333444555444555",
                    WrongRow = won ? -1 : 3, WrongCol = 4, WrongDigit = 5, RightDigit = 3,
                    Rules = { BossModifierId.KillerCages }, Items = { ItemType.PatternScroll }, Relics = { (RelicId)1 }
                });
            save.Save(envelope);

            var host = new GameObject("RecentRunsShotHost");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var root = host.GetComponentsInChildren<Transform>(true).First(t => t.name == "MainMenuRoot");
            var group = root.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            host.GetComponent<MainMenuController>().ShowMainMenu();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shoot(host, camera, "pt043_recent_runs_menu.png");
            Named(host, "RecentRun_0")?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "pt043_recent_runs_card.png");
            host.SetActive(false);
        }

        [UnityTest]
        public IEnumerator RenderBagAndTwelveRelics()
        {
            var camera = CreateCamera();
            _beforeBindAlways = r =>
            {
                r.State.ClassId = ClassId.StoneGardener;
                r.State.ItemSlots = 5;
                r.State.HeldItems.Clear();
                foreach (var type in new[] { ItemType.InkWell, ItemType.PatternScroll, ItemType.GinkgoLeaf, ItemType.Finder })
                    r.State.HeldItems.Add(new ItemInstance { Id = type.ToString(), Type = type, Rarity = ItemRarity.Rare, Charges = 2 });
                r.State.HeldRelics.Clear();
                var ids = (RelicId[])Enum.GetValues(typeof(RelicId));
                for (var i = 0; i < 12; i++) r.State.HeldRelics.Add(new RelicInstance { Id = ids[i], UsesRemaining = 1 });
            };
            var (host, _) = StartRun(7470, true, BossModifierId.Thermo);
            _beforeBindAlways = null;
            yield return DismissIntro();
            yield return new WaitForSecondsRealtime(0.5f);
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            var screen = host.GetComponentInChildren<InRunController>(true);
            Call(screen, "ShowSudoku");
            Call(screen, "RenderBoard");
            yield return null;
            for (var i = 0; i < 4 && Named(host, "BtnRuleCardNext") != null; i++)
            {
                Named(host, "BtnRuleCardNext").onClick.Invoke();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "pt043_bag_and_relics.png");
            host.SetActive(false);
        }

        [UnityTest]
        public IEnumerator RenderCustomPuzzleSevenRules()
        {
            var camera = CreateCamera();
            var host = new GameObject("CustomSevenShotHost");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var root = host.GetComponentsInChildren<Transform>(true).First(t => t.name == "MainMenuRoot");
            var group = root.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            menu.OpenTutorial();
            yield return new WaitForSecondsRealtime(0.4f);
            var setup = host.GetComponentInChildren<TutorialMenuController>(true);
            setup.SetBoardSizeIndex(4);
            foreach (var id in new[]
                     {
                         BossModifierId.GermanWhispers, BossModifierId.Thermo, BossModifierId.KillerCages, BossModifierId.DifferenceKropki,
                         BossModifierId.EvenOdd, BossModifierId.Quadruples, BossModifierId.CountdownFill
                     })
                setup.ToggleModifier(id);
            host.GetComponentInChildren<CustomPuzzleScreen>(true).ShowFamily(Boss.ModifierFamily.Pressure);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(host, camera, "pt043_custom_seven_rules.png");
            host.SetActive(false);
        }
    }
}
