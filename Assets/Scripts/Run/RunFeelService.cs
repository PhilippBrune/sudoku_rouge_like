namespace SudokuRoguelike.Run
{
    public sealed class RunFeelService
    {
        public float GetMistakeScreenShakeIntensity(int consecutiveMistakes)
        {
            return consecutiveMistakes switch
            {
                <= 1 => 0.02f,
                2 => 0.04f,
                _ => 0.06f
            };
        }

        public float GetCorrectPlacementPulseScale(bool isPerfectStreak)
        {
            return isPerfectStreak ? 1.15f : 1.05f;
        }

        public float GetLevelCompleteDelay(bool isBoss)
        {
            return isBoss ? 1.5f : 0.8f;
        }
    }
}
