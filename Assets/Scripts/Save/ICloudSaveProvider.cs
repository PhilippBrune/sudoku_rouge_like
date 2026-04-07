namespace SudokuRoguelike.Save
{
    public interface ICloudSaveProvider
    {
        bool IsAvailable { get; }
        string LoadCloudData();
        void SaveCloudData(string json);
    }
}
