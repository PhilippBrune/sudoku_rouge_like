using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class InRunUiFlowController : MonoBehaviour
    {
        private RunMapController _runMapController;
        private EventChoiceScreenController _eventChoiceScreen;
        private CursePanelController _cursePanel;

        public EventChoiceScreenController EventChoiceScreen => _eventChoiceScreen;

        public void Configure(RunMapController runMap, EventChoiceScreenController eventController, CursePanelController curseController)
        {
            _runMapController = runMap;
            _eventChoiceScreen = eventController;
            _cursePanel = curseController;
            BindPanelsToRunMap();
        }

        public void OnNodeEntered(NodeType nodeType)
        {
            EnsureRunMap();
            if (nodeType == NodeType.Event)
                _eventChoiceScreen?.OpenEvent();
            _cursePanel?.RefreshPanel();
        }

        public void OnEventClosed()
        {
            EnsureRunMap();
            _eventChoiceScreen?.CloseEvent();
            _cursePanel?.RefreshPanel();
        }

        public void RefreshRuntimePanels()
        {
            EnsureRunMap();
            _cursePanel?.RefreshPanel();
        }

        private void EnsureRunMap()
        {
            if (_runMapController != null) return;
            _runMapController = FindFirstObjectByType<RunMapController>();
            BindPanelsToRunMap();
        }

        private void BindPanelsToRunMap()
        {
            if (_runMapController == null) return;
            _eventChoiceScreen?.Bind(_runMapController);
            _cursePanel?.Bind(_runMapController);
        }
    }
}
