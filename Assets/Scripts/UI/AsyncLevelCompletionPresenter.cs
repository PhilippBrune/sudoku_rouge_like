using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public enum AsyncLevelCompletionViewState
    {
        Ready,
        Failed
    }

    public readonly struct AsyncLevelCompletionView
    {
        public AsyncLevelCompletionView(
            AsyncLevelCompletionViewState state,
            string statusText,
            bool localizeStatus)
        {
            State = state;
            StatusText = statusText;
            LocalizeStatus = localizeStatus;
        }

        public AsyncLevelCompletionViewState State { get; }
        public string StatusText { get; }
        public bool LocalizeStatus { get; }
    }

    public sealed class AsyncLevelCompletionPresenter
    {
        public const string GenerationFailedStatus =
            "Puzzle generation failed. Please return to the path and choose another node.";

        public AsyncLevelCompletionView BuildCompletedView(LevelConfig level, bool hasCurrentBoard)
        {
            if (!hasCurrentBoard || level == null)
            {
                return new AsyncLevelCompletionView(
                    AsyncLevelCompletionViewState.Failed,
                    GenerationFailedStatus,
                    localizeStatus: true);
            }

            // [REQ: REQ-UI-101] Localized node label; size and stars written "7×7 3★" everywhere (GAP-019, GAP-029).
            var labelKey = "InRun.Path.Node.Puzzle";
            if (level.IsBoss) labelKey = "InRun.Path.Node.Boss";
            else if (level.IsCursed) labelKey = "InRun.Path.Node.Cursed";
            else if (level.IsElite) labelKey = "InRun.Path.Node.Elite";
            else if (level.IsPreBoss) labelKey = "InRun.Path.Node.PreBoss";

            return new AsyncLevelCompletionView(
                AsyncLevelCompletionViewState.Ready,
                LocalizationService.Format("InRun.Status.ArrivedPuzzle", "{0} - {1}×{1} {2}★",
                    LocalizationService.T(labelKey), level.BoardSize, level.Stars),
                localizeStatus: false);
        }
    }
}
