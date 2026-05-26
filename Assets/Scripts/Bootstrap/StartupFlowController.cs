using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Bootstrap
{
    public enum StartupFlowStep
    {
        MainMenu,
        ProfileSelect,
        FirstRunSetup,
        SudokuBasicsPrompt
    }

    public sealed class StartupFlowContext
    {
        public bool AllowProfileSelection;
        public bool HasAnyProfile;
        public SaveFileEnvelope Envelope;
        public bool SudokuBasicsComplete;
    }

    public sealed class StartupFlowController
    {
        public const string CurrentLegalNoticeVersion = "2026-05-10";

        public StartupFlowStep GetNextStep(StartupFlowContext context)
        {
            if (context == null)
                return StartupFlowStep.MainMenu;

            if (context.AllowProfileSelection && !context.HasAnyProfile)
                return StartupFlowStep.ProfileSelect;

            if (!IsFirstRunSetupComplete(context.Envelope))
                return StartupFlowStep.FirstRunSetup;

            return context.SudokuBasicsComplete
                ? StartupFlowStep.MainMenu
                : StartupFlowStep.SudokuBasicsPrompt;
        }

        public static bool IsFirstRunSetupComplete(SaveFileEnvelope envelope)
        {
            var firstRun = envelope?.PlayerProfile?.Options?.FirstRun;
            return firstRun != null
                && firstRun.LanguageSelected
                && firstRun.AccessibilityReviewed
                && string.Equals(firstRun.AcceptedLegalNoticeVersion,
                    CurrentLegalNoticeVersion, StringComparison.Ordinal);
        }

        public static FirstRunSetupState EnsureFirstRunState(SaveFileEnvelope envelope)
        {
            if (envelope.PlayerProfile == null)
                envelope.PlayerProfile = new ProfileSaveData();
            if (envelope.PlayerProfile.Options == null)
                envelope.PlayerProfile.Options = new OptionsState();
            if (envelope.PlayerProfile.Options.FirstRun == null)
                envelope.PlayerProfile.Options.FirstRun = new FirstRunSetupState();
            return envelope.PlayerProfile.Options.FirstRun;
        }

        public static void MarkFirstRunSetupComplete(SaveFileEnvelope envelope, string completedUtc = null)
        {
            var firstRun = EnsureFirstRunState(envelope);
            firstRun.LanguageSelected = true;
            firstRun.AccessibilityReviewed = true;
            firstRun.AcceptedLegalNoticeVersion = CurrentLegalNoticeVersion;
            firstRun.CompletedUtc = string.IsNullOrEmpty(completedUtc)
                ? DateTime.UtcNow.ToString("o")
                : completedUtc;
        }
    }
}
