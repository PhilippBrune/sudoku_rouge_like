using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public enum InRunSaveQuitPersistenceAction
    {
        None,
        SaveResumeState,
        ClearNonProgressionAutosave,
        ClearFinishedGardenRun
    }

    public readonly struct InRunSaveQuitContext
    {
        public InRunSaveQuitContext(
            bool inPuzzle,
            bool hasCurrentBoard,
            bool hasCurrentLevelState,
            bool onEndScreen,
            RunState runState)
        {
            InPuzzle = inPuzzle;
            HasCurrentBoard = hasCurrentBoard;
            HasCurrentLevelState = hasCurrentLevelState;
            OnEndScreen = onEndScreen;
            RunState = runState;
        }

        public bool InPuzzle { get; }
        public bool HasCurrentBoard { get; }
        public bool HasCurrentLevelState { get; }
        public bool OnEndScreen { get; }
        public RunState RunState { get; }
    }

    public readonly struct InRunSaveQuitDecision
    {
        public InRunSaveQuitDecision(
            bool shouldBlockReturn,
            string blockedStatusKey,
            InRunSaveQuitPersistenceAction persistenceAction,
            bool shouldShowMonthlyWalkPanel)
        {
            ShouldBlockReturn = shouldBlockReturn;
            BlockedStatusKey = blockedStatusKey;
            PersistenceAction = persistenceAction;
            ShouldShowMonthlyWalkPanel = shouldShowMonthlyWalkPanel;
        }

        public bool ShouldBlockReturn { get; }
        public string BlockedStatusKey { get; }
        public InRunSaveQuitPersistenceAction PersistenceAction { get; }
        public bool ShouldShowMonthlyWalkPanel { get; }
    }

    public static class InRunSaveQuitPolicy
    {
        public const string PuzzleStillLoadingStatusKey = "Puzzle is still loading \u2014 please wait a moment before saving.";

        public static InRunSaveQuitDecision Resolve(InRunSaveQuitContext context)
        {
            if (context.InPuzzle && (!context.HasCurrentBoard || !context.HasCurrentLevelState))
            {
                return new InRunSaveQuitDecision(
                    shouldBlockReturn: true,
                    blockedStatusKey: PuzzleStillLoadingStatusKey,
                    persistenceAction: InRunSaveQuitPersistenceAction.None,
                    shouldShowMonthlyWalkPanel: false);
            }

            var runWasSeasonal = context.RunState?.IsSeasonalChallenge == true;
            var supportsResume = context.RunState != null && !context.RunState.DisableProgressionRewards;

            var persistenceAction = InRunSaveQuitPersistenceAction.None;
            if (!context.OnEndScreen && supportsResume)
                persistenceAction = InRunSaveQuitPersistenceAction.SaveResumeState;
            else if (!context.OnEndScreen)
                persistenceAction = InRunSaveQuitPersistenceAction.ClearNonProgressionAutosave;
            else if (!runWasSeasonal)
                persistenceAction = InRunSaveQuitPersistenceAction.ClearFinishedGardenRun;

            return new InRunSaveQuitDecision(
                shouldBlockReturn: false,
                blockedStatusKey: null,
                persistenceAction: persistenceAction,
                shouldShowMonthlyWalkPanel: runWasSeasonal);
        }
    }
}
