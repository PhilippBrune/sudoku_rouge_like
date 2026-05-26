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

            var label = "Puzzle";
            if (level.IsBoss) label = "Boss";
            else if (level.IsCursed) label = "Cursed";
            else if (level.IsElite) label = "Elite";
            else if (level.IsPreBoss) label = "Pre-Boss";

            return new AsyncLevelCompletionView(
                AsyncLevelCompletionViewState.Ready,
                $"{label} - {level.BoardSize}x{level.BoardSize} {level.Stars}*",
                localizeStatus: false);
        }
    }
}
