using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class MenuFlowService
    {
        public SessionState Session { get; } = new();

        public IReadOnlyList<MenuScreen> MainMenuOrder => new[]
        {
            MenuScreen.Main,
            MenuScreen.ModeSelect,
            MenuScreen.TutorialSetup,
            MenuScreen.MetaProgression,
            MenuScreen.GameModes,
            MenuScreen.Options,
            MenuScreen.Credits
        };

        public bool IsResumeVisible() => Session.HasRunInProgress;

        public void OnStartGame()
        {
            Session.CurrentScreen = MenuScreen.ModeSelect;
        }

        public void OnResumeGame(bool saveValid)
        {
            if (!saveValid || !Session.HasRunInProgress)
            {
                return;
            }

            Session.CurrentScreen = MenuScreen.Pause;
        }

        public void SetMode(GameMode mode)
        {
            Session.SelectedMode = mode;
            Session.TutorialMode = mode == GameMode.Tutorial;

            if (mode == GameMode.Tutorial)
            {
                Session.CurrentScreen = MenuScreen.TutorialSetup;
                return;
            }

            Session.CurrentScreen = MenuScreen.ClassSelect;
        }

        public void SetClassAndContinue(ClassId classId)
        {
            Session.CurrentScreen = MenuScreen.SeedSelect;
        }

        public void ConfirmSeed(int seed, bool tutorialMode)
        {
            Session.SelectedSeed = seed;
            Session.TutorialMode = tutorialMode;
            Session.HasRunInProgress = true;
        }

        public void OnTutorial()
        {
            Session.SelectedMode = GameMode.Tutorial;
            Session.TutorialMode = true;
            Session.CurrentScreen = MenuScreen.TutorialSetup;
        }

        public void OpenTutorialProgress()
        {
            Session.CurrentScreen = MenuScreen.TutorialProgress;
        }

        public void ConfirmTutorialSetup(TutorialSetupConfig setup)
        {
            Session.SelectedMode = GameMode.Tutorial;
            Session.TutorialMode = true;
            Session.TutorialSetup = setup;
            Session.HasRunInProgress = true;
        }

        public void OpenMeta() => Session.CurrentScreen = MenuScreen.MetaProgression;

        public void OpenModes() => Session.CurrentScreen = MenuScreen.GameModes;

        public void OpenOptions() => Session.CurrentScreen = MenuScreen.Options;

        public void OpenCredits() => Session.CurrentScreen = MenuScreen.Credits;

        public void OpenPause() => Session.CurrentScreen = MenuScreen.Pause;

        public void OpenEndRun() => Session.CurrentScreen = MenuScreen.EndRun;

        public void OpenVictory() => Session.CurrentScreen = MenuScreen.Victory;

        public void QuitToMain()
        {
            Session.CurrentScreen = MenuScreen.Main;
        }
    }
}
