using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // GAP-061 (Options audit after the 0.2.0 playtest): options that did nothing or less than their name says.
    [TestFixture]
    [Requirement("OPT-GAME-002", "ACCESS-COLOR-003", "OPT-AUDIO-001")]
    public sealed class GameplayOptionsTests
    {
        // [REQ: OPT-GAME-006] The input lock: on with 0.25 s for new and old saves, 0.10–1.00 s in 0.05 s steps.
        [Test]
        [Requirement("OPT-GAME-006")]
        public void InputLock_DefaultsToOnWithAQuarterSecond_EvenForOldSaves()
        {
            var fresh = new GameplaySettings();
            Assert.IsTrue(fresh.InputLockEnabled);
            Assert.AreEqual(0.25f, fresh.InputLockSeconds, 1e-4f);

            var old = JsonUtility.FromJson<GameplaySettings>("{\"HighlightConflicts\":true}");
            Assert.IsTrue(old.InputLockEnabled, "a save from before the option keeps the lock");
            Assert.AreEqual(0.25f, old.InputLockSeconds, 1e-4f);
        }

        [TestCase(0.02f, 0.10f)]
        [TestCase(0.33f, 0.35f)]
        [TestCase(0.71f, 0.70f)]
        [TestCase(5f, 1.00f)]
        [TestCase(float.NaN, 0.25f)]
        [Requirement("OPT-GAME-006")]
        public void InputLockSeconds_SnapsToTheStepsBetweenTheLimits(float stored, float used)
        {
            Assert.AreEqual(used, GameplaySettings.ClampInputLockSeconds(stored), 1e-4f);
        }

        [Test]
        [Requirement("OPT-GAME-006")]
        public void InputLock_OffMeansNoWait_AndResetBringsItBack()
        {
            var settings = new GameplaySettings { InputLockEnabled = false, InputLockSeconds = 0.8f };
            Assert.AreEqual(0f, settings.InputLockDelay);
            settings.InputLockEnabled = true;
            Assert.AreEqual(0.8f, settings.InputLockDelay, 1e-4f);
        }

        private static RunDirector StartSixBySix(int seed)
        {
            var run = new RunDirector();
            run.StartRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun, ClassLevel = 1 }, seed);
            run.State.HeldRelics.Clear();
            run.State.CurrentPencil = run.State.MaxPencil = 99;
            run.StartLevel(new LevelConfig { BoardSize = 6, Stars = 3, Seed = seed, RegionVariant = 0,
                MissingPercent = StarDensityService.MissingPercentForStars(3) });
            return run;
        }

        // An empty cell and, for its digit, empty cells of its row, column and box (other than itself) to pencil it into.
        private static (int r, int c, int v, (int, int)[] peers) Setup(RunDirector run)
        {
            var b = run.CurrentBoard;
            for (var r = 0; r < b.Size; r++)
            for (var c = 0; c < b.Size; c++)
            {
                if (b.Cells[r, c] != 0) continue;
                var v = b.Solution[r, c];
                var region = b.RegionMap[r, c];
                var peers = Enumerable.Range(0, b.Size * b.Size).Select(i => (i / b.Size, i % b.Size))
                    .Where(p => p != (r, c) && b.Cells[p.Item1, p.Item2] == 0
                        && (p.Item1 == r || p.Item2 == c || b.RegionMap[p.Item1, p.Item2] == region)).ToArray();
                var boxOnly = peers.Any(p => p.Item1 != r && p.Item2 != c);
                if (peers.Length >= 2 && boxOnly) return (r, c, v, peers);
            }
            Assert.Fail("no suitable cell");
            return default;
        }

        // [REQ: OPT-GAME-002] On: a placed digit leaves the pencil marks of its row, column AND box (the box was missed);
        // off: every mark stays.
        [Test]
        [Requirement("OPT-GAME-002")]
        public void AutoPencilCleanup_OnClearsRowColumnAndBox_OffKeepsTheMarks()
        {
            foreach (var on in new[] { true, false })
            {
                var run = StartSixBySix(9401);
                run.AutoPencilCleanup = on;
                var (r, c, v, peers) = Setup(run);
                foreach (var (pr, pc) in peers) run.CurrentBoard.TogglePencilMark(pr, pc, v);

                Assert.AreEqual(PlaceResult.Correct, run.PlaceNumber(r, c, v));

                foreach (var (pr, pc) in peers)
                    Assert.AreEqual(!on, run.CurrentBoard.GetPencilMarks(pr, pc).Contains(v),
                        $"cleanup {(on ? "on" : "off")}: mark {v} at ({pr},{pc})");
            }
        }

        // [REQ: ACCESS-COLOR-003] Colour-blind mode: the line colours that tell the rules apart come from the Okabe–Ito
        // palette, and the board cells are no longer passed through a colour-blindness simulation.
        [Test]
        [Requirement("ACCESS-COLOR-003")]
        public void ColorblindMode_UsesTheOkabeItoLineColours_AndDoesNotSimulateOnCells()
        {
            var lines = new[] { LineType.GermanWhispers, LineType.DutchWhispers, LineType.RenbanLine, LineType.ParityLine,
                LineType.ConsecutiveLine, LineType.BetweenLine };
            var safe = lines.Select(t => BoardViewController.LineColorFor(t, true)).ToArray();
            Assert.AreEqual(lines.Length, safe.Distinct().Count(), "each rule keeps its own colour");
            Assert.AreNotEqual(BoardViewController.LineColorFor(LineType.GermanWhispers, false), safe[0]);
            AssertColor(new Color(0f, 0.62f, 0.45f), safe[0], "German Whispers: bluish green");
            AssertColor(new Color(0.90f, 0.62f, 0f), safe[1], "Dutch Whispers: orange");

            var service = new AccessibilityService();
            service.Refresh(new AccessibilitySettings { ColorblindMode = true }, new GraphicsSettingsModel());
            var cell = new Color(0.62f, 0.20f, 0.18f, 1f);
            AssertColor(service.ApplyDisplayCalibration(cell), service.GetCellColor(cell), "only the board calibration applies");
        }

        // [REQ: OPT-AUDIO-001] Master Volume counts once: every source multiplies it in, so the listener stays at full
        // volume (it was set to the master volume too, so 50 % played at 25 %).
        [Test]
        [Requirement("OPT-AUDIO-001")]
        public void MasterVolume_CountsOnce()
        {
            var slot = SudokuRoguelike.Save.SaveProfileService.ActiveSlot;
            SudokuRoguelike.Save.SaveProfileService.ActiveSlot = SudokuRoguelike.Save.SaveFileService.MaxSlots - 1;
            var host = new GameObject("MasterVolumeHost");
            var listenerVolume = AudioListener.volume;
            try
            {
                var options = host.AddComponent<OptionsController>();
                options.SetMasterVolume(0.5f);
                Assert.AreEqual(0.5f, OptionsController.LiveAudio.MasterVolume, 0.001f);
                Assert.AreEqual(1f, AudioListener.volume, 0.001f, "the sources apply the master volume");
                options.SetMasterVolume(1f);
            }
            finally
            {
                Object.DestroyImmediate(host);
                AudioListener.volume = listenerVolume;
                SudokuRoguelike.Save.SaveFileService.FlushSharedPendingWrites();
                var path = SudokuRoguelike.Save.SaveFileService.GetSlotPath(SudokuRoguelike.Save.SaveFileService.MaxSlots - 1);
                foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                SudokuRoguelike.Save.SaveProfileService.ActiveSlot = slot;
            }
        }

        private static void AssertColor(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.02f, message);
            Assert.AreEqual(expected.g, actual.g, 0.02f, message);
            Assert.AreEqual(expected.b, actual.b, 0.02f, message);
        }
    }
}
