using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Builds the <see cref="RunResult"/> recorded at run end. Shared by the end screen and by abandoning a run
    /// (in-run Abandon Run, or starting a new run over a saved one), so every path records the same XP and progress.
    /// </summary>
    public static class RunResultFactory
    {
        public static RunResult Build(RunDirector run, bool victory, int bossPhaseReached, int secondsPlayed)
        {
            if (run?.State == null) return null;

            var state = run.State;
            var result = new RunResult
            {
                PlayedClassId = state.ClassId,
                Mode = state.Mode,
                Victory = victory && !state.DailyWalk,   // [REQ: DAILY-WALK-003] two floors are no Garden Run win
                DailyWalk = state.DailyWalk,
                GardenDepthReached = state.Depth,
                FloorReached = Mathf.Clamp(state.CurrentFloor + 1, 1, Mathf.Max(1, state.TotalFloors)),
                // [REQ: REQ-ECON-101] Gold collected during the run, not gold still held at the end (GAP-027).
                GoldEarned = state.GoldCollectedThisRun,
                XpEarned = run.GetTotalRunXp(),
                BossPhaseReached = bossPhaseReached,
                BossesDefeatedThisRun = state.BossesDefeatedThisRun,
                MistakesMade = run.GetAnalytics()?.TotalMistakes ?? run.CurrentLevelState?.Mistakes ?? 0,
                SecondsPlayed = secondsPlayed > 0 ? secondsPlayed : Mathf.RoundToInt(state.TotalRunSeconds),
                TutorialMode = state.TutorialMode,
                DisableProgressionRewards = state.DisableProgressionRewards,
                PlayedTier = state.SpiritTrialsTier,
                HarmonyLevel = state.HarmonyLevel
            };

            for (var i = 0; i < run.TileXpLog.Count; i++)
                result.TileXpEntries.Add(run.TileXpLog[i]);
            if (state.ClearedBossModifiersThisRun != null)
                result.ClearedBossModifiers.AddRange(state.ClearedBossModifiersThisRun);

            var analytics = run.GetAnalytics();
            if (analytics != null)
            {
                // [REQ: SCORE-GRADE-002] A Garden Run lost (or left) inside an unsolved puzzle grades that puzzle Messy; the
                // other modes end their sessions inside a puzzle by design. [REQ: SCORE-GOLD-001] The gold still held scores.
                var gardenRun = state.Mode == GameMode.GardenRun && !state.TutorialMode && !state.IsSeasonalChallenge;
                var lostInPuzzle = gardenRun && !victory && run.CurrentBoard != null && run.CurrentLevelState != null
                    && !run.IsLevelComplete;
                result.Analytics = analytics.Build(state.CurrentGold, lostInPuzzle ? state.CurrentFloor : -1,
                    lostInPuzzle ? run.CurrentLevelState.Mistakes : 0, state.WeeklySeedRun ? (int)state.ActiveSeconds : -1);
                result.ItemsUsedThisRun = analytics.TotalItemsUsed;
                // [REQ: MAP-NODE-CURSED-004] Cursed puzzles accepted and completed (GameDesignSpec §9c); perfect puzzles (GAP-054).
                result.CursedPuzzlesAccepted = analytics.CursedPuzzlesCompleted;
                result.PerfectPuzzleCount = analytics.PerfectPuzzleCount;
                result.RunScore = result.Analytics.RunScore;
            }

            result.AcquiredRelic = state.HasRelic;
            result.RelicsCollectedThisRun = state.HeldRelics?.Count ?? 0;

            return result;
        }
    }
}
