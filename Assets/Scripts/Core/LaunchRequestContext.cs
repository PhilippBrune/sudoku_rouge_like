namespace SudokuRoguelike.Core
{
    public sealed class LaunchRequest
    {
        public GameMode Mode = GameMode.GardenRun;
        public ClassId ClassId = ClassId.NumberFreak;
        public TutorialSetupConfig TutorialSetup;
        public bool ResumeFromSave;
        public bool StartFresh = true;
    }

    public static class LaunchRequestContext
    {
        private static LaunchRequest _pending;

        public static void Request(LaunchRequest request)
        {
            _pending = request;
        }

        public static bool TryConsume(out LaunchRequest request)
        {
            request = _pending;
            _pending = null;
            return request != null;
        }
    }
}
