# Startup Experience

This document is the maintained replacement for the completed intro and
first-time-experience audit plan.

## Supported Flow

The build starts with `Assets/Scenes/Splash.unity`, which runs
`SplashSceneBootstrap` and then loads `Assets/Scenes/Prototype.unity` by scene
name. `GameBootstrap` keeps the inline `SplashScreenController` fallback so
developers can still open `Prototype.unity` directly in the Unity editor.

After splash, `StartupFlowController` gates the menu in this order:

1. Profile selection, when startup is allowed to select profiles and no profile
   exists.
2. First-run setup, when the active profile has not completed language,
   accessibility, and legal acknowledgement.
3. Sudoku basics prompt, when first-run setup is complete but the player has
   not completed or skipped the basics prompt.
4. Main menu.

## First-Run Setup

`FirstRunSetupModalController` provides the profile-level setup prompt. It
captures:

- English or German language selection.
- Accessibility defaults for colorblind mode, high contrast, reduced motion,
  and screen-reader captions.
- Acknowledgement of the current legal notice version through
  `StartupFlowController.CurrentLegalNoticeVersion`.

The resulting state is saved in `OptionsState.FirstRun`. `SaveFileService`
migrates older saves by creating a missing `FirstRunSetupState` during load.

## Audit Coverage

| Finding | Current coverage |
| --- | --- |
| INTRO-001 | `ProjectSettings/EditorBuildSettings.asset` includes `Splash.unity` before `Prototype.unity`; `StartupExperienceReadinessTests.BuildSettings_UseDedicatedSplashBeforePrototype` prevents regression. |
| INTRO-002 | `StartupFlowController` routes incomplete profiles to `FirstRunSetup`; `FirstRunSetupPlayModeTests` validates German language, accessibility, legal persistence, and tutorial continuation. |
| INTRO-003 | Dedicated splash is the build path; inline splash remains as a documented editor fallback. |
| INTRO-004 | First-run setup stores `AcceptedLegalNoticeVersion` and points players to Credits for full notices. |
| INTRO-005 | Sudoku basics completion behavior is intentional: accepting or skipping the prompt marks the prompt as handled, and the tutorial remains manually launchable from Options. |

## Validation

Static guardrails:

- `Assets/Scripts/Tests/EditMode/StartupExperienceReadinessTests.cs`
- `Assets/Scripts/Tests/EditMode/ModifierDiscoveryPersistenceTests.cs`

Runtime smoke coverage:

- `Assets/Scripts/Tests/PlayMode/FirstRunSetupPlayModeTests.cs`

Release candidates still require Unity batchmode EditMode and PlayMode test
results because direct `dotnet test` is only a compile/test-adapter smoke check.
