namespace SudokuRoguelike.Save
{
    public interface ICloudSaveProvider
    {
        bool TryLoadProfile(out string json, out long timestampUtc);
        bool TryLoadRun(out string json, out long timestampUtc);
        void SaveProfile(string json, long timestampUtc);
        void SaveRun(string json, long timestampUtc);
    }
}
