using NUnit.Framework;
using SudokuRoguelike.Run;
using static SudokuRoguelike.Run.DailyGoalService;

namespace SudokuRoguelike.Tests
{
    // DailyGoalSystem 0.3: the daily walk pays XP to a class the player picks.
    [TestFixture]
    public sealed class DailyWalkXpTests
    {
        [Test]
        [Requirement("DAILY-WALK-003")]
        public void TheWalksXp_GetsTheDawnBonusTheGoalsAndTheStreak()
        {
            Assert.AreEqual(594, WalkXp(396, new GoalTier[0], 0), "×1.5");
            Assert.AreEqual(744, WalkXp(396, new[] { GoalTier.Easy, GoalTier.Medium }, 2), "+50 +100, no streak bonus before day 3");
            Assert.AreEqual(818, WalkXp(396, new[] { GoalTier.Easy, GoalTier.Medium }, 4), "day 3 to 6: +10 % (the mockup's sum)");
            Assert.AreEqual(1180, WalkXp(396, new[] { GoalTier.Easy, GoalTier.Medium, GoalTier.Hard }, 7), "from day 7: +25 %");
        }
    }
}
