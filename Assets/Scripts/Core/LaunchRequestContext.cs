namespace SudokuRoguelike.Core
{
    /// <summary>
    /// Data object describing how to launch a run.
    /// Passed directly to GameBootstrap.LaunchRun() — no static shuttle needed
    /// in the single-scene architecture.
    /// </summary>
    public sealed class LaunchRequest
    {
        public GameMode Mode = GameMode.GardenRun;
        public ClassId ClassId = ClassId.NumberFreak;
        public TutorialSetupConfig TutorialSetup;
        public bool ResumeFromSave;
        public bool StartFresh = true;
        public bool AllowIrregularPuzzles = true;
    }
}
