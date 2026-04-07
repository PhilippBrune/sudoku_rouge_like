using UnityEngine;

namespace SudokuRoguelike.Save
{
    public sealed class SteamCloudSyncService : ICloudSaveProvider
    {
        // Stub — Steam integration not yet configured
        public bool IsAvailable => false;

        public string LoadCloudData()
        {
            Debug.Log("SteamCloudSyncService: LoadCloudData stub — returning null.");
            return null;
        }

        public void SaveCloudData(string json)
        {
            Debug.Log("SteamCloudSyncService: SaveCloudData stub — not persisted.");
        }
    }
}
