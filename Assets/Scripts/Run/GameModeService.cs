using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class GameModeService
    {
        public bool IsModeUnlocked(GameMode mode, MetaProgressionState meta)
        {
            return mode switch
            {
                GameMode.GardenRun => true,
                GameMode.EndlessZen => meta.EndlessZenUnlocked,
                GameMode.SpiritTrials => meta.SpiritTrialsUnlocked,
                GameMode.Tutorial => true,
                _ => false
            };
        }

        public int GetSpiritTrialsBaseStars() => 3;

        public int GetEndlessZenBoardSize() => 9;
    }
}
