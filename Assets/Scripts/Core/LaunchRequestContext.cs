namespace SudokuRoguelike.Core
{
    public static class LaunchRequestContext
    {
        public static LaunchRequest BuildDefault()
        {
            return new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = true
            };
        }
    }
}
