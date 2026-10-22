using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// [REQ: RUN-AGAIN-001] "Walk again" on the end of a run (one-more-run concept RM-110): the same class, mode, Harmony and
    /// perk, the same Spirit Trials tier, a new seed; the Monthly Walk starts the month's garden again. Tutorials and custom
    /// puzzles have no Walk again.
    /// </summary>
    public static class RunAgain
    {
        public enum Kind { None, Run, MonthlyWalk }

        public static Kind KindOf(RunState state)
        {
            // Endless Zen, Spirit Trials and the Monthly Walk turn progression off too; only TutorialMode marks a tutorial
            // or a custom puzzle.
            if (state == null || state.TutorialMode || state.Mode == GameMode.Tutorial) return Kind.None;
            return state.IsSeasonalChallenge ? Kind.MonthlyWalk : Kind.Run;
        }

        /// <summary>The launch request for the next run, or null when the run has no Walk again (or is a Monthly Walk).</summary>
        public static LaunchRequest Request(RunState state)
        {
            if (KindOf(state) != Kind.Run) return null;
            var garden = state.Mode == GameMode.GardenRun;
            return new LaunchRequest
            {
                ClassId = state.ClassId,
                Mode = state.Mode,
                AllowIrregularPuzzles = state.AllowIrregularPuzzles,
                HarmonyLevel = garden ? state.HarmonyLevel : 0,
                HarmonyPerk = garden ? state.HarmonyPerk : HarmonyPerkId.None,
                SelectedTier = state.Mode == GameMode.SpiritTrials ? state.SpiritTrialsTier : (SpiritTrialsTier?)null
            };
        }
    }
}
