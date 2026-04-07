namespace SudokuRoguelike.Save
{
    public sealed class SaveConflictService
    {
        private readonly SaveFileService _saveFile;
        private readonly ICloudSaveProvider _cloud;

        public SaveConflictService(SaveFileService saveFile, ICloudSaveProvider cloud)
        {
            _saveFile = saveFile;
            _cloud = cloud;
        }

        public bool HasRunConflict()
        {
            return _saveFile.HasActiveRun();
        }

        public void ResolveKeepLocal()
        {
            // Local save is already present — nothing to do
        }

        public void ResolveDiscardLocal()
        {
            var coordinator = new RunAutoSaveCoordinator(_saveFile);
            coordinator.ClearActiveRun();
        }
    }
}
