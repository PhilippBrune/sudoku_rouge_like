using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private SaveConflictDecision defaultConflictDecision = SaveConflictDecision.KeepLocal;

        private readonly MenuFlowService _menu = new();
        private readonly SaveFileService _save = new();
        private readonly ICloudSaveProvider _cloud = new LocalCloudSaveProvider();
        private SaveConflictService _conflicts;

        public MenuFlowService Menu => _menu;

        private void Awake()
        {
            _conflicts = new SaveConflictService(_save, _cloud);
        }

        public void StartGame()
        {
            _menu.OnStartGame();
        }

        public void OpenTutorial()
        {
            _menu.OnTutorial();
        }

        public void ResumeGame()
        {
            var hasSave = false;

            if (_conflicts.TryResolveRunConflict(defaultConflictDecision, out var envelope))
            {
                hasSave = envelope?.ActiveRunState != null && envelope.ActivePuzzle != null;
                if (hasSave && runMapController != null)
                {
                    hasSave = runMapController.ResumeFromEnvelope(envelope);
                }
            }

            _menu.Session.HasRunInProgress = hasSave;
            _menu.OnResumeGame(saveValid: hasSave);
        }

        public void OpenOptions() => _menu.OpenOptions();

        public void OpenCredits() => _menu.OpenCredits();

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
