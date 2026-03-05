using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class PrototypeRunMapBootstrap : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private ClassId classId = ClassId.NumberFreak;

        private readonly ProfileService _profile = new();

        private void Start()
        {
            if (runMapController == null)
            {
                return;
            }

            runMapController.Initialize(classId, _profile.Meta);
        }
    }
}
