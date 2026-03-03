namespace SudokuRoguelike.Save
{
    public sealed class LocalCloudSaveProvider : ICloudSaveProvider
    {
        public bool TryLoadProfile(out string json, out long timestampUtc)
        {
            json = null;
            timestampUtc = 0;
            return false;
        }

        public bool TryLoadRun(out string json, out long timestampUtc)
        {
            json = null;
            timestampUtc = 0;
            return false;
        }

        public void SaveProfile(string json, long timestampUtc)
        {
        }

        public void SaveRun(string json, long timestampUtc)
        {
        }
    }
}
