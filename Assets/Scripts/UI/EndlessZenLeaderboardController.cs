using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Holds Text references for the Endless Zen leaderboard panel.
    /// Call Configure() once during panel build, then Refresh(stats) each time the panel is shown.
    /// </summary>
    public sealed class EndlessZenLeaderboardController : MonoBehaviour
    {
        private Text _bestDepthLabel;
        private Text _totalSessionsLabel;

        public void Configure(Text bestDepth, Text totalSessions)
        {
            _bestDepthLabel      = bestDepth;
            _totalSessionsLabel  = totalSessions;
        }

        public void Refresh(ProfileStats stats)
        {
            if (stats == null) return;
            if (_bestDepthLabel != null)
                _bestDepthLabel.text = stats.HighestEndlessDepth > 0
                    ? LocalizationService.Format("EndlessZen.PersonalBestDepth", "Personal Best: Depth {0}", stats.HighestEndlessDepth)
                    : LocalizationService.T("EndlessZen.PersonalBestPlaceholder");
            if (_totalSessionsLabel != null)
                _totalSessionsLabel.text = LocalizationService.Format("EndlessZen.TotalSessions", "Total Sessions: {0}", stats.TotalZenSessions);
        }
    }
}
