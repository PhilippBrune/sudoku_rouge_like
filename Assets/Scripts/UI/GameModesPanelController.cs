using UnityEngine;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    public sealed class GameModesPanelController : MonoBehaviour
    {
        public SpiritTrialsTier SelectedTrialsTier { get; set; } = SpiritTrialsTier.Apprentice;
        public int EndlessZenSize { get; set; } = 9;
        public int EndlessZenStars { get; set; } = 2;

        public void SelectTrialsTier(int tierIndex)
        {
            SelectedTrialsTier = (SpiritTrialsTier)tierIndex;
        }

        public void SetEndlessZenSize(int size)
        {
            EndlessZenSize = Mathf.Clamp(size, 5, 9);
        }

        public void SetEndlessZenStars(int stars)
        {
            EndlessZenStars = Mathf.Clamp(stars, 1, 6);
        }
    }
}
