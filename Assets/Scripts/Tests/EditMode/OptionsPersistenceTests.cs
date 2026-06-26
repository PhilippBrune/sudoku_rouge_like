using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class OptionsPersistenceTests
    {
        private const int PrimaryTestSlot = SaveFileService.MaxSlots - 1;
        private const int IsolationTestSlot = SaveFileService.MaxSlots - 2;

        private readonly List<PreservedSaveFile> _preservedFiles = new List<PreservedSaveFile>();
        private int _originalActiveSlot;
        private LanguageOption _originalLanguage;
        private float _originalAudioListenerVolume;
        private int _originalVSyncCount;
        private FullScreenMode _originalFullScreenMode;
        private bool _originalFullScreen;
        private bool _originalRumbleEnabled;

        [SetUp]
        public void SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();

            _originalActiveSlot = SaveProfileService.ActiveSlot;
            _originalLanguage = LocalizationService.Current;
            _originalAudioListenerVolume = AudioListener.volume;
            _originalVSyncCount = QualitySettings.vSyncCount;
            _originalFullScreenMode = Screen.fullScreenMode;
            _originalFullScreen = Screen.fullScreen;
            _originalRumbleEnabled = RumbleService.Enabled;

            Directory.CreateDirectory(Application.persistentDataPath);
            PreserveAndClearSlot(PrimaryTestSlot);
            PreserveAndClearSlot(IsolationTestSlot);

            SaveProfileService.ActiveSlot = PrimaryTestSlot;
            LocalizationService.SetLanguage(LanguageOption.English);
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();

            foreach (var preserved in _preservedFiles)
                preserved.Restore();
            _preservedFiles.Clear();

            SaveProfileService.ActiveSlot = _originalActiveSlot;
            LocalizationService.SetLanguage(_originalLanguage);
            AudioListener.volume = _originalAudioListenerVolume;
            QualitySettings.vSyncCount = _originalVSyncCount;
            Screen.fullScreenMode = _originalFullScreenMode;
            Screen.fullScreen = _originalFullScreen;
            RumbleService.Enabled = _originalRumbleEnabled;
        }

        [Test]
        public void OptionsController_SettersPersistAcrossControllerRecreationAndProfileSwitch()
        {
            var firstHost = new GameObject("OptionsPersistenceController");
            try
            {
                var controller = firstHost.AddComponent<OptionsController>();
                controller.LoadOptions();

                controller.SetMasterVolume(0.37f);
                controller.SetMusicVolume(0.41f);
                controller.SetSfxVolume(0.42f);
                controller.SetUiVolume(0.43f);
                controller.SetMuteAll(true);
                controller.SetMuteWhenUnfocused(true);
                controller.SetMusicStyleIndex(2);
                controller.SetLanguage(LanguageOption.German);
                controller.SetColorblindMode(true);
                controller.SetHighContrast(true);
                controller.SetReduceMotion(true);
                controller.SetAltSymbols(true);
                controller.SetFontScale(1.35f);
                controller.SetHighlightConflicts(false);
                controller.SetConfirmBeforeWrongPlacement(true);
                controller.SetDoubleTapConfirm(true);
                controller.SetAutoPencilCleanup(true);
                controller.SetShowCandidateCount(false);
                controller.SetCursorSnapOnSelection(true);
                controller.SetControllerRumbleEnabled(false);
                controller.SetScreenShake(false);
                controller.SetParticleIntensity(0.42f);
                controller.SetVSync(false);
                controller.SetBrightness(0.72f);
                controller.SetContrast(0.31f);
                controller.SetDisplayMode(DisplayModeOption.Borderless);

                SaveFileService.FlushSharedPendingWrites();
            }
            finally
            {
                Object.DestroyImmediate(firstHost);
            }

            SaveProfileService.ActiveSlot = IsolationTestSlot;
            var isolationOptions = LoadOptionsFromNewController("OptionsIsolationController");
            Assert.AreEqual(LanguageOption.English, isolationOptions.Language);
            Assert.AreNotEqual(0.37f, isolationOptions.Audio.MasterVolume);
            Assert.AreEqual(0, isolationOptions.Audio.MenuMusicStyleIndex);
            Assert.IsTrue(isolationOptions.Gameplay.HighlightConflicts);
            Assert.IsTrue(isolationOptions.Graphics.ScreenShake);

            SaveProfileService.ActiveSlot = PrimaryTestSlot;
            var reloaded = LoadOptionsFromNewController("OptionsReloadController");

            Assert.AreEqual(LanguageOption.German, reloaded.Language);
            Assert.AreEqual(LanguageOption.German, LocalizationService.Current);
            Assert.AreEqual(0.37f, reloaded.Audio.MasterVolume, 0.001f);
            Assert.AreEqual(0.41f, reloaded.Audio.MusicVolume, 0.001f);
            Assert.AreEqual(0.42f, reloaded.Audio.SfxVolume, 0.001f);
            Assert.AreEqual(0.43f, reloaded.Audio.UiVolume, 0.001f);
            Assert.IsTrue(reloaded.Audio.MuteAll);
            Assert.IsTrue(reloaded.Audio.MuteWhenUnfocused);
            Assert.AreEqual(2, reloaded.Audio.MenuMusicStyleIndex);
            Assert.IsTrue(reloaded.Accessibility.ColorblindMode);
            Assert.IsTrue(reloaded.Accessibility.HighContrastMode);
            Assert.IsTrue(reloaded.Accessibility.ReduceMotion);
            Assert.IsTrue(reloaded.Accessibility.AlternativeConstraintSymbols);
            Assert.AreEqual(1.35f, reloaded.Accessibility.FontScale, 0.001f);
            Assert.IsFalse(reloaded.Gameplay.HighlightConflicts);
            Assert.IsTrue(reloaded.Gameplay.ConfirmBeforeWrongPlacement);
            Assert.IsTrue(reloaded.Gameplay.DoubleTapConfirmNumberEntry);
            Assert.IsTrue(reloaded.Gameplay.AutoPencilCleanup);
            Assert.IsFalse(reloaded.Gameplay.ShowCandidateCount);
            Assert.IsTrue(reloaded.Gameplay.CursorSnapOnSelection);
            Assert.IsFalse(reloaded.Gameplay.ControllerRumbleEnabled);
            Assert.IsFalse(reloaded.Graphics.ScreenShake);
            Assert.AreEqual(0.42f, reloaded.Graphics.ParticleIntensity, 0.001f);
            Assert.IsFalse(reloaded.Graphics.VSync);
            Assert.AreEqual(0.72f, reloaded.Graphics.Brightness, 0.001f);
            Assert.AreEqual(0.31f, reloaded.Graphics.Contrast, 0.001f);
            Assert.AreEqual(DisplayModeOption.Borderless, reloaded.Graphics.DisplayMode);
            Assert.IsTrue(reloaded.Graphics.Fullscreen);
        }

        [Test]
        public void ResolutionResolver_DeduplicatesSortsAndRejectsUnsupportedSizes()
        {
            var supported = OptionsController.NormalizeSupportedResolutions(new[]
            {
                new Resolution { width = 3840, height = 2160 },
                new Resolution { width = 1920, height = 1080 },
                new Resolution { width = 1920, height = 1080 },
                new Resolution { width = 0, height = 0 },
                new Resolution { width = 1280, height = 720 }
            });

            Assert.AreEqual(3, supported.Length);
            Assert.AreEqual(1280, supported[0].width);
            Assert.AreEqual(720, supported[0].height);
            Assert.AreEqual(3840, supported[2].width);
            Assert.AreEqual(2160, supported[2].height);

            var exact = OptionsController.ResolveSupportedResolution(3840, 2160, supported);
            Assert.AreEqual(3840, exact.width);
            Assert.AreEqual(2160, exact.height);

            var fallback = OptionsController.ResolveSupportedResolution(13, 37, supported);
            Assert.AreEqual(1920, fallback.width);
            Assert.AreEqual(1080, fallback.height);
        }

        [Test]
        public void EmptyResolutionList_UsesSafeFallbackModes()
        {
            var supported = OptionsController.NormalizeSupportedResolutions(null);

            Assert.GreaterOrEqual(supported.Length, 3);
            CollectionAssert.Contains(
                System.Array.ConvertAll(supported, resolution => $"{resolution.width}x{resolution.height}"),
                "1920x1080");
        }

        [Test]
        public void DisplayModeNormalizer_RejectsUnknownValues()
        {
            Assert.AreEqual(DisplayModeOption.Fullscreen,
                OptionsController.NormalizeDisplayMode((DisplayModeOption)999));
            Assert.AreEqual(DisplayModeOption.Windowed,
                OptionsController.NormalizeDisplayMode(DisplayModeOption.Windowed));
        }

        private static OptionsState LoadOptionsFromNewController(string hostName)
        {
            var host = new GameObject(hostName);
            try
            {
                var controller = host.AddComponent<OptionsController>();
                controller.LoadOptions();
                return controller.GetOptions();
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private void PreserveAndClearSlot(int slotIndex)
        {
            var savePath = Path.Combine(Application.persistentDataPath, $"save_profile_{slotIndex}.json");
            PreserveAndDelete(savePath);
            PreserveAndDelete(savePath + ".bak");
            PreserveAndDelete(savePath + ".tmp");
        }

        private void PreserveAndDelete(string path)
        {
            _preservedFiles.Add(new PreservedSaveFile(
                path,
                File.Exists(path) ? File.ReadAllText(path) : null));

            if (File.Exists(path))
                File.Delete(path);
        }

        private readonly struct PreservedSaveFile
        {
            public PreservedSaveFile(string path, string contents)
            {
                Path = path;
                Contents = contents;
            }

            private string Path { get; }
            private string Contents { get; }

            public void Restore()
            {
                if (Contents == null)
                {
                    if (File.Exists(Path))
                        File.Delete(Path);
                    return;
                }

                File.WriteAllText(Path, Contents);
            }
        }
    }
}
