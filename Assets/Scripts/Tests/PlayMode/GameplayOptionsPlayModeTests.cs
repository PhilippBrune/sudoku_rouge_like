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
    // GAP-061 (Options audit after the 0.2.0 playtest): the Gameplay, Display and Accessibility options do what their names
    // say, checked on the real puzzle screen.
    [TestFixture]
    [Requirement("OPT-GAME-001", "OPT-GAME-003", "OPT-GAME-004", "OPT-GAME-005", "OPT-GFX-001", "ACCESS-FONT-001",
        "OPT-LABEL-001")]
    public sealed class GameplayOptionsPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;
        private GameplaySettings _originalGameplay;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            _originalGameplay = OptionsController.LiveGameplay;
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
            OptionsController.LiveGameplay = _originalGameplay;
            DeleteSlotFiles();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        // [REQ: OPT-GAME-001] Highlight Errors off: a wrong digit flashes no cell; on: it does.
        [UnityTest]
        [Requirement("OPT-GAME-001")]
        public IEnumerator HighlightErrors_Off_AWrongDigitFlashesNoCell()
        {
            foreach (var on in new[] { false, true })
            {
                var (host, run) = StartPuzzle(9501);
                yield return WaitForBoard(host);
                UseGameplay(host, new GameplaySettings { HighlightConflicts = on });
                var cell = EmptyCell(run);
                yield return Select(host, cell);
                ClickButton(host, $"Num_{run.CurrentBoard.Solution[cell.r, cell.c] % run.CurrentBoard.Size + 1}");
                yield return null;
                Assert.AreEqual(on, host.GetComponentsInChildren<Transform>(true).Any(t => t.name == "PressureFlash"),
                    $"Highlight Errors {(on ? "on" : "off")}");
                host.SetActive(false);
            }
        }

        // [REQ: OPT-GAME-003] Show Digits Left: each tile of the number pad shows how many of its digit are still missing.
        [UnityTest]
        [Requirement("OPT-GAME-003")]
        public IEnumerator ShowDigitsLeft_NumberPadCountsTheMissingDigits()
        {
            foreach (var on in new[] { true, false })
            {
                var (host, run) = StartPuzzle(9502);
                yield return WaitForBoard(host);
                UseGameplay(host, new GameplaySettings { ShowCandidateCount = on });
                yield return null;
                var board = run.CurrentBoard;
                var placed = Enumerable.Range(0, board.Size * board.Size).Count(i => board.Cells[i / board.Size, i % board.Size] == 1);
                var left = FindObjectOrNull(FindObject(host, "Num_1"), "Left");
                if (on)
                {
                    Assert.NotNull(left, "the tile has a count");
                    Assert.IsTrue(left.activeInHierarchy);
                    Assert.AreEqual((board.Size - placed).ToString(), left.GetComponent<Text>().text);
                }
                else
                    Assert.IsTrue(left == null || !left.activeInHierarchy, "no count when off");
                host.SetActive(false);
            }
        }

        // [REQ: OPT-GAME-004] Jump to Next Empty Cell: after a correct digit the selection moves on to the next empty cell.
        [UnityTest]
        [Requirement("OPT-GAME-004")]
        public IEnumerator JumpToNextEmptyCell_AfterACorrectDigit()
        {
            var (host, run) = StartPuzzle(9503);
            yield return WaitForBoard(host);
            UseGameplay(host, new GameplaySettings { CursorSnapOnSelection = true });
            var cell = EmptyCell(run);
            yield return Select(host, cell);
            ClickButton(host, $"Num_{run.CurrentBoard.Solution[cell.r, cell.c]}");
            yield return null;

            var selected = host.GetComponentInChildren<InRunController>(true).SelectedCell;
            Assert.AreNotEqual((cell.r, cell.c), selected, "the selection moved");
            Assert.AreEqual(0, run.CurrentBoard.Cells[selected.row, selected.col], "to an empty cell");
        }

        // [REQ: OPT-GAME-005] Double-Tap to Confirm: the first press of a digit only asks; the second places it.
        [UnityTest]
        [Requirement("OPT-GAME-005")]
        public IEnumerator DoubleTapToConfirm_TheSecondPressPlacesTheDigit()
        {
            var (host, run) = StartPuzzle(9504);
            yield return WaitForBoard(host);
            UseGameplay(host, new GameplaySettings { DoubleTapConfirmNumberEntry = true });
            var cell = EmptyCell(run);
            var digit = run.CurrentBoard.Solution[cell.r, cell.c];
            yield return Select(host, cell);

            ClickButton(host, $"Num_{digit}");
            yield return null;
            Assert.AreEqual(0, run.CurrentBoard.Cells[cell.r, cell.c], "the first press only asks");
            ClickButton(host, $"Num_{digit}");
            yield return null;
            Assert.AreEqual(digit, run.CurrentBoard.Cells[cell.r, cell.c], "the second press places it");
        }

        // [REQ: OPT-GFX-001] Screen Shake off stops the shake (it used to stop only with Reduce Motion).
        [UnityTest]
        [Requirement("OPT-GFX-001")]
        public IEnumerator ScreenShakeOff_StopsTheShake()
        {
            var (host, _) = StartPuzzle(9505);
            yield return WaitForBoard(host);
            var controller = host.GetComponentInChildren<InRunController>(true);
            Assert.IsTrue(controller.ScreenShakeAllowed, "on by default");

            SaveOptions(o => o.Graphics.ScreenShake = false);
            controller.NotifyAccessibilityChanged();
            yield return null;
            Assert.IsFalse(controller.ScreenShakeAllowed);
        }

        // [REQ: ACCESS-FONT-001] Text size scales the board's digits (it only changed the preview).
        [UnityTest]
        [Requirement("ACCESS-FONT-001")]
        public IEnumerator TextSize_ScalesTheBoardDigits()
        {
            var (host, _) = StartPuzzle(9506);
            yield return WaitForBoard(host);
            var before = FindObject(FindObject(host, "N_0_0"), "Val").GetComponent<Text>().fontSize;

            SaveOptions(o => o.Accessibility.FontScale = 1.4f);
            host.GetComponentInChildren<InRunController>(true).NotifyAccessibilityChanged();
            yield return null;
            yield return null;

            var after = FindObject(FindObject(host, "N_0_0"), "Val").GetComponent<Text>().fontSize;
            Assert.Greater(after, before, "larger digits at 140 %");
        }

        // [REQ: OPT-LABEL-001] The Options name what the controls do: no "Confirm Wrong Placement" (a confirmation only for
        // wrong digits would give the answer away), board brightness and contrast, status captions.
        [UnityTest]
        [Requirement("OPT-LABEL-001")]
        public IEnumerator Options_NameWhatTheyDo()
        {
            var host = new GameObject("OptionsLabelHost");
            _createdRoots.Add(host);
            EnsureCameraAndEventSystem();
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            yield return null;
            FindObject(host, "BtnOptions").GetComponent<Button>().onClick.Invoke();   // the tabs are built when opened
            yield return new WaitForSecondsRealtime(0.3f);
            var names = host.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
            Assert.IsFalse(names.Any(n => n.EndsWith("ConfirmWrong")), "no Confirm Wrong Placement toggle");
            var texts = host.GetComponentsInChildren<Text>(true).Select(t => t.text).ToList();
            // A value row writes its value after the name ("Board Brightness 50%").
            foreach (var (key, english) in new[]
            {
                ("Options.BoardBrightness", "Board Brightness"), ("Options.BoardContrast", "Board Contrast"),
                ("Options.StatusCaptions", "Status Captions"), ("Options.DigitsLeft", "Show Digits Left"),
                ("Options.JumpToNextEmpty", "Jump to Next Empty Cell")
            })
            {
                var label = LocalizationService.T(key, english);
                Assert.IsTrue(texts.Any(t => t.StartsWith(label)), $"the Options show \"{label}\"");
            }
        }

        // ── harness ──

        // The options as the player sets them: in the save, live, and the open board redrawn (the run's Options panel
        // loads the save when it is built, so setting only the live copy before the run would be overwritten).
        private void UseGameplay(GameObject host, GameplaySettings settings)
        {
            SaveOptions(o => o.Gameplay = settings);
            OptionsController.LiveGameplay = settings;
            host.GetComponentInChildren<InRunController>(true).OnGameplayOptionsChanged();
        }

        private void SaveOptions(System.Action<OptionsState> change)
        {
            var save = new SaveFileService(TestSlot);
            var envelope = save.Load();
            change(envelope.PlayerProfile.Options);
            save.Save(envelope);
            SaveFileService.FlushSharedPendingWrites();
        }

        private void EnsureCameraAndEventSystem()
        {
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("OptionsTestCamera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                _createdRoots.Add(camera);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("OptionsTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
        }

        private (GameObject host, RunDirector run) StartPuzzle(int seed)
        {
            EnsureCameraAndEventSystem();
            var host = new GameObject("OptionsTestHost");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, AllowIrregularPuzzles = false,
                ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            run.State.CurrentHP = run.State.MaxHP = 20;
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = 6;
            config.RegionVariant = 0;
            config.ActiveModifiers.Clear();
            run.StartLevel(config);
            Assert.NotNull(run.CurrentBoard);
            host.GetComponentInChildren<RunMapController>(true).BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
        }

        private static IEnumerator WaitForBoard(GameObject host)
        {
            for (var frame = 0; frame < 30 && FindObjectOrNull(host, "C_0_0") == null; frame++)
                yield return null;
            yield return null;
        }

        private static (int r, int c) EmptyCell(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
                if (b.Cells[r, c] == 0) return (r, c);
            Assert.Fail("no empty cell");
            return default;
        }

        private static IEnumerator Select(GameObject host, (int r, int c) cell)
        {
            FindObject(host, $"C_{cell.r}_{cell.c}").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(InRunController.CellSelectInputDelay + 0.05f);
        }

        private static void ClickButton(GameObject root, string name)
        {
            var button = FindObject(root, name).GetComponent<Button>();
            Assert.IsTrue(button.interactable, $"{name} is interactable");
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
