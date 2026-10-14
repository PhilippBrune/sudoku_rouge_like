using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    // Display modes (GameDesignSpec §Display Modes, 2026-10-12): Fullscreen, Borderless Fullscreen, Window and
    // Frameless Window. The frame itself is set by WindowFrameService in the Windows player (E2E report §10).
    [TestFixture]
    public sealed class DisplayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private int _originalSlot;
        private FullScreenMode _originalMode;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalSlot = SaveProfileService.ActiveSlot;
            _originalMode = Screen.fullScreenMode;
            SaveProfileService.ActiveSlot = TestSlot;
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            SaveProfileService.ActiveSlot = _originalSlot;
            Screen.fullScreenMode = _originalMode;
        }

        [Test]
        [Requirement("DISPLAY-MODE-001")]
        public void EachModeMapsToItsUnityMode()
        {
            Assert.AreEqual(FullScreenMode.ExclusiveFullScreen, OptionsController.ToUnityFullScreenMode(DisplayModeOption.Fullscreen));
            Assert.AreEqual(FullScreenMode.FullScreenWindow, OptionsController.ToUnityFullScreenMode(DisplayModeOption.Borderless));
            Assert.AreEqual(FullScreenMode.Windowed, OptionsController.ToUnityFullScreenMode(DisplayModeOption.Windowed));
            Assert.AreEqual(FullScreenMode.Windowed, OptionsController.ToUnityFullScreenMode(DisplayModeOption.FramelessWindow));
            Assert.IsTrue(OptionsController.IsWindowMode(DisplayModeOption.Windowed));
            Assert.IsTrue(OptionsController.IsWindowMode(DisplayModeOption.FramelessWindow));
            Assert.IsFalse(OptionsController.IsWindowMode(DisplayModeOption.Borderless));
        }

        [TestCase(1920, 1080, 1920, 1080, 1600, 900, TestName = "Display-sized choice on 1080p becomes 1600x900")]
        [TestCase(1280, 720, 1920, 1080, 1280, 720, TestName = "A choice that fits is kept")]
        [TestCase(1440, 900, 1920, 1080, 1440, 900, TestName = "A 16:10 choice that fits is kept")]
        [TestCase(2560, 1440, 2560, 1440, 1920, 1080, TestName = "Display-sized choice on 1440p becomes 1920x1080")]
        [TestCase(3840, 2160, 3840, 2160, 2560, 1440, TestName = "Display-sized choice on 4K becomes 2560x1440")]
        [TestCase(1366, 768, 1366, 768, 960, 540, TestName = "Small laptop display gets 960x540")]
        [Requirement("DISPLAY-MODE-002")]
        public void WindowSize_LeavesTenPercentOfTheDisplayFree(int selW, int selH, int dispW, int dispH, int expW, int expH)
        {
            Assert.AreEqual(new Vector2Int(expW, expH), OptionsController.GetWindowSize(selW, selH, dispW, dispH));
        }

        [Test]
        [Requirement("DISPLAY-MODE-005")]
        public void ModesPersist_AndTheFullscreenFlagFollowsTheMode()
        {
            var expected = new Dictionary<DisplayModeOption, bool>
            {
                { DisplayModeOption.Fullscreen, true }, { DisplayModeOption.Borderless, true },
                { DisplayModeOption.Windowed, false }, { DisplayModeOption.FramelessWindow, false }
            };
            foreach (var pair in expected)
            {
                var host = new GameObject("DisplayModeOptions");
                try
                {
                    var controller = host.AddComponent<OptionsController>();
                    controller.LoadOptions();
                    controller.SetDisplayMode(pair.Key);
                    SaveFileService.FlushSharedPendingWrites();
                }
                finally
                {
                    Object.DestroyImmediate(host);
                }

                var graphics = new SaveFileService(TestSlot).Load().PlayerProfile.Options.Graphics;
                Assert.AreEqual(pair.Key, graphics.DisplayMode, pair.Key.ToString());
                Assert.AreEqual(pair.Value, graphics.Fullscreen, pair.Key + " fullscreen flag");
            }
        }

        [Test]
        [Requirement("DISPLAY-MODE-005")]
        public void LoadNormalizes_LegacyFlagUnknownValuesAndFramelessWindow()
        {
            Assert.AreEqual(DisplayModeOption.Windowed, SaveAndReload(DisplayModeOption.Fullscreen, false).DisplayMode,
                "a save that only says 'not fullscreen' becomes Window");
            Assert.AreEqual(DisplayModeOption.Fullscreen, SaveAndReload((DisplayModeOption)99, true).DisplayMode);
            var frameless = SaveAndReload(DisplayModeOption.FramelessWindow, true);
            Assert.AreEqual(DisplayModeOption.FramelessWindow, frameless.DisplayMode);
            Assert.IsFalse(frameless.Fullscreen);
        }

        [Test]
        [Requirement("DISPLAY-MODE-006")]
        public void FullscreenToggle_KeepsTheChosenWindowOrBorderlessMode()
        {
            var host = new GameObject("DisplayModeToggle");
            try
            {
                var controller = host.AddComponent<OptionsController>();
                controller.LoadOptions();

                controller.SetDisplayMode(DisplayModeOption.FramelessWindow);
                controller.SetFullscreen(false);
                Assert.AreEqual(DisplayModeOption.FramelessWindow, controller.GetOptions().Graphics.DisplayMode);
                controller.SetFullscreen(true);
                Assert.AreEqual(DisplayModeOption.Fullscreen, controller.GetOptions().Graphics.DisplayMode);
                controller.SetFullscreen(false);
                Assert.AreEqual(DisplayModeOption.Windowed, controller.GetOptions().Graphics.DisplayMode);

                controller.SetDisplayMode(DisplayModeOption.Borderless);
                controller.SetFullscreen(true);
                Assert.AreEqual(DisplayModeOption.Borderless, controller.GetOptions().Graphics.DisplayMode);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static GraphicsSettingsModel SaveAndReload(DisplayModeOption mode, bool fullscreenFlag)
        {
            var save = new SaveFileService(TestSlot);
            var envelope = new SaveFileEnvelope();
            envelope.PlayerProfile.Options.Graphics.DisplayMode = mode;
            envelope.PlayerProfile.Options.Graphics.Fullscreen = fullscreenFlag;
            save.Save(envelope);
            SaveFileService.FlushSharedPendingWrites();
            return new SaveFileService(TestSlot).Load().PlayerProfile.Options.Graphics;
        }
    }
}
