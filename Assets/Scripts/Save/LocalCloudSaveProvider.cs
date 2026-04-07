namespace SudokuRoguelike.Save
{
    public sealed class LocalCloudSaveProvider : ICloudSaveProvider
    {
        public bool IsAvailable => false;

        public string LoadCloudData() => null;

        public void SaveCloudData(string json)
        {
            // No-op for local builds
        }
    }
}
