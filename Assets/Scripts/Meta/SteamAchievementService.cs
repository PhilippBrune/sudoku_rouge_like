using UnityEngine;

namespace SudokuRoguelike.Meta
{
    public sealed class SteamAchievementService
    {
        public void TryUnlock(string achievementId)
        {
            // Steam integration stub — no-op until Steamworks SDK is integrated
            Debug.Log($"[SteamAchievement] Would unlock: {achievementId}");
        }

        public bool IsUnlocked(string achievementId)
        {
            return false;
        }

        public void ResetAll()
        {
            Debug.Log("[SteamAchievement] Would reset all achievements.");
        }
    }
}
