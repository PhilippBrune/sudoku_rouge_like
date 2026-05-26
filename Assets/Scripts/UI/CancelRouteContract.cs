using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public enum CancelRouteAction
    {
        None,
        DismissSudokuBasicsOverlay,
        DismissStartRunConfirmation,
        BackFromMenuPanel,
        CloseInRunOptions,
        ExitItemsFocus,
        CancelPendingItemInteraction,
        ShowPathQuitConfirmation,
        InvokeOverlayCancelButton
    }

    public readonly struct MainMenuCancelContext
    {
        public MainMenuCancelContext(
            bool firstRunSetupActive,
            bool sudokuBasicsOverlayActive,
            bool startRunConfirmationActive,
            MenuScreen currentScreen)
        {
            FirstRunSetupActive = firstRunSetupActive;
            SudokuBasicsOverlayActive = sudokuBasicsOverlayActive;
            StartRunConfirmationActive = startRunConfirmationActive;
            CurrentScreen = currentScreen;
        }

        public bool FirstRunSetupActive { get; }
        public bool SudokuBasicsOverlayActive { get; }
        public bool StartRunConfirmationActive { get; }
        public MenuScreen CurrentScreen { get; }
    }

    public readonly struct InRunCancelContext
    {
        public InRunCancelContext(
            bool inGameOptionsActive,
            bool overlayActive,
            bool pathPanelActive,
            bool pathQuitModalActive,
            bool itemsFocusActive,
            bool pendingItemInteraction)
        {
            InGameOptionsActive = inGameOptionsActive;
            OverlayActive = overlayActive;
            PathPanelActive = pathPanelActive;
            PathQuitModalActive = pathQuitModalActive;
            ItemsFocusActive = itemsFocusActive;
            PendingItemInteraction = pendingItemInteraction;
        }

        public bool InGameOptionsActive { get; }
        public bool OverlayActive { get; }
        public bool PathPanelActive { get; }
        public bool PathQuitModalActive { get; }
        public bool ItemsFocusActive { get; }
        public bool PendingItemInteraction { get; }
    }

    public static class CancelRouteContract
    {
        public static CancelRouteAction ResolveMainMenuCancel(MainMenuCancelContext context)
        {
            if (context.FirstRunSetupActive)
                return CancelRouteAction.None;
            if (context.SudokuBasicsOverlayActive)
                return CancelRouteAction.DismissSudokuBasicsOverlay;
            if (context.StartRunConfirmationActive)
                return CancelRouteAction.DismissStartRunConfirmation;
            return context.CurrentScreen == MenuScreen.MainMenu
                ? CancelRouteAction.None
                : CancelRouteAction.BackFromMenuPanel;
        }

        public static CancelRouteAction ResolveInRunKeyboardCancel(InRunCancelContext context)
        {
            if (context.InGameOptionsActive)
                return CancelRouteAction.CloseInRunOptions;
            if (context.ItemsFocusActive)
                return CancelRouteAction.ExitItemsFocus;
            if (context.PendingItemInteraction)
                return CancelRouteAction.CancelPendingItemInteraction;
            if (context.PathPanelActive && !context.PathQuitModalActive)
                return CancelRouteAction.ShowPathQuitConfirmation;
            return CancelRouteAction.None;
        }

        public static CancelRouteAction ResolveInRunGamepadCancel(InRunCancelContext context)
        {
            if (context.InGameOptionsActive)
                return CancelRouteAction.CloseInRunOptions;
            if (context.OverlayActive)
                return CancelRouteAction.InvokeOverlayCancelButton;
            if (context.PathPanelActive && !context.PathQuitModalActive)
                return CancelRouteAction.ShowPathQuitConfirmation;
            if (context.ItemsFocusActive)
                return CancelRouteAction.ExitItemsFocus;
            if (context.PendingItemInteraction)
                return CancelRouteAction.CancelPendingItemInteraction;
            return CancelRouteAction.None;
        }

        public static bool IsOverlayCancelControlName(string controlName)
        {
            if (string.IsNullOrEmpty(controlName))
                return false;

            return ContainsIgnoreCase(controlName, "skip")
                || ContainsIgnoreCase(controlName, "abort")
                || ContainsIgnoreCase(controlName, "cancel")
                || ContainsIgnoreCase(controlName, "leave")
                || ContainsIgnoreCase(controlName, "stay");
        }

        private static bool ContainsIgnoreCase(string value, string token)
        {
            return value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
