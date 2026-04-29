using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Holds per-tier best-score Text references for the Spirit Trials tier select panel.
    /// Call Configure() once during panel build, then Refresh(stats) each time the panel is shown.
    /// </summary>
    public sealed class SpiritTrialsTierSelectController : MonoBehaviour
    {
        private Text _apprenticeBest;
        private Text _adeptBest;
        private Text _masterBest;
        private Text _grandmasterBest;

        public void Configure(Text apprenticeBest, Text adeptBest, Text masterBest, Text grandmasterBest)
        {
            _apprenticeBest  = apprenticeBest;
            _adeptBest       = adeptBest;
            _masterBest      = masterBest;
            _grandmasterBest = grandmasterBest;
        }

        public void Refresh(ProfileStats stats)
        {
            if (stats == null) return;
            SetBest(_apprenticeBest,  stats.TrialsBestScore_Apprentice);
            SetBest(_adeptBest,       stats.TrialsBestScore_Adept);
            SetBest(_masterBest,      stats.TrialsBestScore_Master);
            SetBest(_grandmasterBest, stats.TrialsBestScore_Grandmaster);
        }

        private static void SetBest(Text label, int score)
        {
            if (label == null) return;
            label.text = score > 0 ? $"Best: {score:N0}" : "Best: \u2014";
        }
    }
}
