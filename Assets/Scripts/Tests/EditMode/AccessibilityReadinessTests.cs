using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class AccessibilityReadinessTests
    {
        [Test]
        public void AccessibilityService_RefreshAppliesRuntimeFlagsAndScaling()
        {
            var service = new AccessibilityService();
            service.Refresh(
                new AccessibilitySettings
                {
                    ColorblindMode = true,
                    ReduceMotion = true,
                    AlternativeConstraintSymbols = true,
                    FontScale = 1.5f
                },
                new GraphicsSettingsModel
                {
                    ScreenShake = false,
                    ParticleIntensity = 0.42f
                });

            Assert.IsTrue(service.IsColorblind);
            Assert.IsTrue(service.IsReduceMotion);
            Assert.IsTrue(service.UseAltSymbols);
            Assert.AreEqual(1.5f, service.FontScale, 0.001f);
            Assert.IsFalse(service.ScreenShakeEnabled);
            Assert.AreEqual(0.42f, service.ParticleIntensity, 0.001f);
            Assert.AreEqual(18, service.ScaleFont(12));
            Assert.AreEqual(30f, service.GetAnimationDuration(100f), 0.001f);
            Assert.AreEqual(AccessibilityService.ColorMode.Protanopia, service.ActiveColorMode);
        }

        [Test]
        public void AccessibilityService_HighContrastOverridesColorblindAndBoostsOverlayReadability()
        {
            var service = new AccessibilityService();
            service.Refresh(
                new AccessibilitySettings
                {
                    ColorblindMode = true,
                    HighContrastMode = true
                },
                new GraphicsSettingsModel());

            Assert.IsTrue(service.IsColorblind);
            Assert.IsTrue(service.IsHighContrast);
            Assert.AreEqual(AccessibilityService.ColorMode.HighContrast, service.ActiveColorMode);
            Assert.Greater(service.GetOverlayAlpha(0.5f), 0.5f);
            Assert.LessOrEqual(service.GetOverlayAlpha(0.9f), 1f);
            Assert.AreEqual(1.5f, service.GetLineWidthMultiplier(), 0.001f);
        }

        [Test]
        public void AccessibilityService_AppliesDisplayCalibrationToRenderedColors()
        {
            var neutral = new AccessibilityService();
            neutral.Refresh(new AccessibilitySettings(), new GraphicsSettingsModel());
            var neutralColor = neutral.ApplyDisplayCalibration(new Color(0.4f, 0.5f, 0.6f, 1f));

            var calibrated = new AccessibilityService();
            calibrated.Refresh(
                new AccessibilitySettings(),
                new GraphicsSettingsModel
                {
                    Brightness = 0.85f,
                    Contrast = 1.0f
                });
            var brighter = calibrated.ApplyDisplayCalibration(new Color(0.4f, 0.5f, 0.6f, 1f));

            Assert.Greater(brighter.r, neutralColor.r);
            Assert.Greater(brighter.g, neutralColor.g);
            Assert.Greater(brighter.b, neutralColor.b);
            Assert.AreEqual(1f, brighter.a, 0.001f);
        }

        [Test]
        public void AccessibilityValidationManifest_TracksProductionReviewMatrix()
        {
            var manifest = File.ReadAllText("docs/accessibility-validation.md");
            var spec = File.ReadAllText("docs/AccessibilitySpec.md");

            StringAssert.Contains("ACCESS-COLOR", manifest);
            StringAssert.Contains("ACCESS-CONTRAST", manifest);
            StringAssert.Contains("ACCESS-MOTION", manifest);
            StringAssert.Contains("ACCESS-FONT", manifest);
            StringAssert.Contains("ACCESS-SYMBOLS", manifest);
            StringAssert.Contains("ACCESS-CAPTIONS", manifest);
            StringAssert.Contains("ACCESS-RUMBLE", manifest);
            StringAssert.Contains("150% font scale", manifest);
            StringAssert.Contains("OS-level screen reader integration", manifest);

            StringAssert.Contains("docs/accessibility-validation.md", spec);
            StringAssert.Contains("OS-level screen reader bridges", spec);
        }
    }
}
