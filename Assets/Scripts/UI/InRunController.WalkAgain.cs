using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    // [REQ: RUN-AGAIN-001] "Walk again" from the end of a run (RM-110): the way back takes the same steps as "Back to the
    // garden gate" (the finished run is cleared, the menu opens), then the menu's own launch starts the next run with the
    // same class, mode, Harmony and perk, so nothing differs from starting it by hand.
    public sealed partial class InRunController
    {
        private void SaveAndQuit() => TrySaveAndQuit();

        private void WalkAgain()
        {
            var state = _map?.Run?.State;
            var kind = RunAgain.KindOf(state);
            var request = RunAgain.Request(state);
            if (!TrySaveAndQuit()) return;   // the return was refused (a puzzle still loading): no second run on top
            var bootstrap = FindFirstObjectByType<Bootstrap.GameBootstrap>();
            if (bootstrap == null) return;
            if (kind == RunAgain.Kind.MonthlyWalk) bootstrap.LaunchSeasonalChallenge();
            else if (request != null) bootstrap.LaunchRun(request);
        }
    }
}
