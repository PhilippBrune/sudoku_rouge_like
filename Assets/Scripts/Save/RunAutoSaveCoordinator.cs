using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunAutoSaveCoordinator
    {
        private readonly SaveFileService _saveFileService;
        private readonly ProfileService _profileService;

        public RunAutoSaveCoordinator(SaveFileService saveFileService, ProfileService profileService)
        {
            _saveFileService = saveFileService;
            _profileService = profileService;
        }

        public void Bind(RunDirector runDirector)
        {
            runDirector.AutoSaveRequested += trigger => Save(runDirector, trigger);
        }

        private void Save(RunDirector runDirector, RunSaveTrigger trigger)
        {
            var envelope = new SaveFileEnvelope
            {
                PlayerProfile = new ProfileSaveData { Options = _profileService.Options },
                MetaProgress = _profileService.Meta,
                ActiveRunState = runDirector.RunState,
                ActivePuzzle = runDirector.ExportPuzzleSaveState(),
                TutorialProgress = _profileService.TutorialProgress,
                Statistics = _profileService.Stats,
                Mastery = _profileService.Mastery,
                Completion = _profileService.Completion
            };

            _saveFileService.SaveRun(envelope);
        }
    }
}
