using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class SteamAchievementServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            SteamAchievementService.ResetPlatformBridgeForTests();
        }

        [Test]
        public void EvaluateHarmonyAchievements_PersistsAndForwardsUnlockedIds()
        {
            var bridge = new RecordingAchievementBridge();
            SteamAchievementService.SetPlatformBridgeForTests(bridge);

            var meta = new MetaProgressionState();
            var mastery = new MasteryAchievementState();
            var result = new RunResult
            {
                Victory = true,
                Mode = GameMode.GardenRun,
                HarmonyLevel = 10,
                MistakesMade = 0,
                PlayedClassId = ClassId.NumberFreak
            };

            SteamAchievementService.EvaluateHarmonyAchievements(meta, mastery, result);

            CollectionAssert.Contains(mastery.UnlockedAchievements, "harmony_10_win");
            CollectionAssert.Contains(mastery.UnlockedAchievements, "harmony_10_perfect");
            CollectionAssert.Contains(bridge.UnlockedIds, "harmony_10_win");
            CollectionAssert.Contains(bridge.UnlockedIds, "harmony_10_perfect");
            Assert.IsNull(SteamAchievementService.LastPlatformError);
        }

        [Test]
        public void SyncUnlockedAchievements_ForwardsExistingLocalUnlocks()
        {
            var bridge = new RecordingAchievementBridge();
            SteamAchievementService.SetPlatformBridgeForTests(bridge);
            var mastery = new MasteryAchievementState
            {
                UnlockedAchievements = new List<string>
                {
                    "harmony_1_win",
                    "harmony_3_win"
                }
            };

            var forwarded = SteamAchievementService.SyncUnlockedAchievements(mastery);

            Assert.AreEqual(2, forwarded);
            CollectionAssert.AreEquivalent(mastery.UnlockedAchievements, bridge.UnlockedIds);
        }

        [Test]
        public void SteamworksReflectionBridge_IsUnavailableWhenSdkAssemblyIsAbsent()
        {
            var bridge = new SteamworksReflectionAchievementBridge();

            Assert.IsFalse(bridge.IsAvailable);
        }

        private sealed class RecordingAchievementBridge : IPlatformAchievementBridge
        {
            public readonly List<string> UnlockedIds = new List<string>();

            public string ProviderName => "Test";
            public bool IsAvailable => true;

            public bool TryUnlockAchievement(string achievementId, out string error)
            {
                error = null;
                UnlockedIds.Add(achievementId);
                return true;
            }
        }
    }
}
