using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Platform;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class PlatformServiceRegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            PlatformServiceRegistry.ResetForTests();
            SteamAchievementService.ResetPlatformBridgeForTests();
        }

        [Test]
        public void DefaultStatus_ReportsUnconfiguredSteamAndCloudCapabilities()
        {
            SteamAchievementService.ResetPlatformBridgeForTests();

            var status = PlatformServiceRegistry.CurrentStatus;

            Assert.AreEqual(PlatformCapabilityState.NotConfigured, status.Achievements.State);
            Assert.AreEqual(PlatformCapabilityState.NotConfigured, status.CloudSave.State);
            Assert.AreEqual(PlatformCapabilityState.Available, status.PrivacyNotice.State);
            Assert.AreEqual("None", status.PrivacyNotice.ProviderName);
            Assert.IsTrue(status.HasReleaseBlockers);
            CollectionAssert.Contains(status.ReleaseBlockers, "Platform achievements are not release-verified.");
            CollectionAssert.Contains(status.ReleaseBlockers, "Cloud save is not configured; local saves only.");
            CollectionAssert.DoesNotContain(status.ReleaseBlockers, "Privacy/analytics notice is not finalized.");
            StringAssert.Contains("local profile slots", status.CloudSave.Detail);
            StringAssert.Contains("No platform telemetry package", status.PrivacyNotice.Detail);
        }

        [Test]
        public void Status_ReportsAchievementsAvailableWhenBridgeIsAvailable()
        {
            SteamAchievementService.SetPlatformBridgeForTests(new AvailableAchievementBridge());

            var status = PlatformServiceRegistry.CurrentStatus;

            Assert.AreEqual(PlatformCapabilityState.Available, status.Achievements.State);
            Assert.AreEqual("TestAchievements", status.Achievements.ProviderName);
            Assert.IsTrue(status.Achievements.IsAvailable);
            CollectionAssert.DoesNotContain(status.ReleaseBlockers, "Platform achievements are not release-verified.");
            CollectionAssert.Contains(status.ReleaseBlockers, "Cloud save is not configured; local saves only.");
        }

        [Test]
        public void Registry_AllowsInjectedPlatformServiceForTests()
        {
            PlatformServiceRegistry.SetForTests(new ReadyPlatformService());

            var status = PlatformServiceRegistry.CurrentStatus;

            Assert.IsFalse(status.HasReleaseBlockers);
            Assert.AreEqual(PlatformCapabilityState.Available, status.Achievements.State);
            Assert.AreEqual(PlatformCapabilityState.Available, status.CloudSave.State);
            Assert.AreEqual(PlatformCapabilityState.Available, status.PrivacyNotice.State);
        }

        private sealed class AvailableAchievementBridge : IPlatformAchievementBridge
        {
            public string ProviderName => "TestAchievements";
            public bool IsAvailable => true;

            public bool TryUnlockAchievement(string achievementId, out string error)
            {
                error = null;
                return true;
            }
        }

        private sealed class ReadyPlatformService : IPlatformService
        {
            public string ProviderName => "ReadyTest";

            public PlatformReadinessStatus GetReadinessStatus()
            {
                return new PlatformReadinessStatus(
                    new PlatformCapabilityReport("Achievements", PlatformCapabilityState.Available, ProviderName, "Ready"),
                    new PlatformCapabilityReport("Cloud Save", PlatformCapabilityState.Available, ProviderName, "Ready"),
                    new PlatformCapabilityReport("Privacy/Analytics Notice", PlatformCapabilityState.Available, ProviderName, "Ready"),
                    new List<string>());
            }
        }
    }
}
