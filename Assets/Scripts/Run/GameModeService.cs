using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public static class GameModeService
    {
        public static bool HasProgressionRewards(GameMode mode)
        {
            return mode == GameMode.GardenRun || mode == GameMode.SpiritTrials;
        }

        public static bool HasShop(GameMode mode)
        {
            return mode == GameMode.GardenRun;
        }

        public static bool HasPath(GameMode mode)
        {
            return mode == GameMode.GardenRun;
        }

        public static bool HasBoss(GameMode mode)
        {
            return mode == GameMode.GardenRun || mode == GameMode.SpiritTrials;
        }

        public static bool IsInfinite(GameMode mode)
        {
            return mode == GameMode.EndlessZen;
        }

        public static string GetModeName(GameMode mode)
        {
            return mode switch
            {
                GameMode.GardenRun => "Garden Run",
                GameMode.Tutorial => "Tutorial",
                GameMode.EndlessZen => "Endless Zen",
                GameMode.SpiritTrials => "Spirit Trials",
                _ => mode.ToString()
            };
        }
    }
}
