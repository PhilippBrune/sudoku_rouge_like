using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public static class MainMenuTextPresenter
    {
        public static string T(string key)
        {
            return LocalizationService.T(key);
        }
    }
}
