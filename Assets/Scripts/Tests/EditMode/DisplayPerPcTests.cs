using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // GAP-074, GAP-075, GAP-076 (0.3.0 playtest): the display belongs to the PC, is applied once per start and by the
    // player's own changes, and Borderless Fullscreen always uses the display's size.
    [TestFixture]
    public sealed class DisplayPerPcTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private int _originalSlot;
        private FullScreenMode _originalMode;
        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalSlot = SaveProfileService.ActiveSlot;
            _originalMode = Screen.fullScreenMode;
            DisplayPrefs.Clear();
            SaveProfileService.ActiveSlot = TestSlot;
            // A profile from another PC: the Legion Go's exclusive 1920×1200.
            var envelope = new SaveFileEnvelope();
            envelope.PlayerProfile.Options.Graphics.DisplayMode = DisplayModeOption.Fullscreen;
            envelope.PlayerProfile.Options.Graphics.ResolutionWidth = 1920;
            envelope.PlayerProfile.Options.Graphics.ResolutionHeight = 1200;
            new SaveFileService(TestSlot).Save(envelope);
            SaveFileService.FlushSharedPendingWrites();
            _host = new GameObject("DisplayPerPc");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            SaveFileService.FlushSharedPendingWrites();
            DisplayPrefs.Clear();
            var path = SaveFileService.GetSlotPath(TestSlot);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
            SaveProfileService.ActiveSlot = _originalSlot;
            Screen.fullScreenMode = _originalMode;
        }

        [Test]
        [Requirement("DISPLAY-MODE-008")]
        public void AProfileFromAnotherPc_DoesNotSetThisPcsDisplay()
        {
            var options = _host.AddComponent<OptionsController>();
            options.LoadOptions();
            Assert.AreEqual(OptionsController.FromUnity(Screen.fullScreenMode), options.GetOptions().Graphics.DisplayMode,
                "the Options show what is on screen, not the other PC's exclusive mode");
        }

        [Test]
        [Requirement("DISPLAY-MODE-008")]
        public void SwitchingToFullscreen_NeverUsesTheOtherPcsResolution()
        {
            Assume.That(new Vector2Int(Screen.width, Screen.height), Is.Not.EqualTo(new Vector2Int(1920, 1200)));
            var options = _host.AddComponent<OptionsController>();
            options.LoadOptions();
            options.SetDisplayMode(DisplayModeOption.Fullscreen);
            Assert.IsTrue(DisplayPrefs.TryLoad(out _, out var width, out var height));
            Assert.AreNotEqual(new Vector2Int(1920, 1200), new Vector2Int(width, height),
                "the Legion Go's 1920×1200 from the cloud profile must not become this PC's Fullscreen size");
        }

        [Test]
        [Requirement("DISPLAY-MODE-008")]
        public void ThisPcsChoice_IsKept_AndAProfileChoiceAppliesNothing()
        {
            var options = _host.AddComponent<OptionsController>();
            options.LoadOptions();
            var before = OptionsController.DisplayApplications;
            options.SetDisplayMode(DisplayModeOption.Windowed);
            Assert.AreEqual(before + 1, OptionsController.DisplayApplications, "the player's change is applied");
            Assert.IsTrue(DisplayPrefs.TryLoad(out var mode, out _, out _));
            Assert.AreEqual(DisplayModeOption.Windowed, mode, "and kept for this PC");

            options.LoadOptions();
            OptionsController.ReloadAfterProfileChange();
            Assert.AreEqual(before + 1, OptionsController.DisplayApplications, "a profile choice applies nothing (GAP-075)");
            Assert.AreEqual(DisplayModeOption.Windowed, options.GetOptions().Graphics.DisplayMode,
                "this PC's choice wins over the profile's");
        }

        [Test]
        [Requirement("DISPLAY-MODE-008")]
        public void ResetThisPage_GivesBorderlessFullscreen()
        {
            var options = _host.AddComponent<OptionsController>();
            options.LoadOptions();
            options.SetDisplayMode(DisplayModeOption.Fullscreen);
            options.ResetGraphicsToDefaults();
            Assert.AreEqual(DisplayModeOption.Borderless, options.GetOptions().Graphics.DisplayMode);
        }

        [Test]
        [Requirement("DISPLAY-MODE-009")]
        public void Borderless_UsesTheDisplaysOwnSize()
        {
            var display = new Vector2Int(3440, 1440);
            Assert.AreEqual(display, OptionsController.SizeFor(DisplayModeOption.Borderless, 1920, 1200, display));
            Assert.AreEqual(new Vector2Int(1920, 1200), OptionsController.SizeFor(DisplayModeOption.Fullscreen, 1920, 1200, display),
                "exclusive Fullscreen keeps the chosen resolution");
            Assert.AreEqual(new Vector2Int(1920, 1080), OptionsController.SizeFor(DisplayModeOption.Windowed, 1920, 1080, display),
                "a window keeps DISPLAY-MODE-002");
        }
    }
}
