using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class PrototypeRunMapBootstrap : MonoBehaviour
    {
        private RunMapController _runMapController;
        private ClassId _classId = ClassId.NumberFreak;

        public void Configure(RunMapController runMap, ClassId classId = ClassId.NumberFreak)
        {
            _runMapController = runMap;
            _classId = classId;
        }

        private void Start()
        {
            if (_runMapController == null)
                _runMapController = FindFirstObjectByType<RunMapController>();
            if (_runMapController == null) return;

            var request = new LaunchRequest
            {
                ClassId = _classId,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = true
            };
            _runMapController.Initialize(request, System.Environment.TickCount);
        }
    }
}
