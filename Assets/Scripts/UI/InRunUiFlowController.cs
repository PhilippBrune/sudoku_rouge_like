using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class InRunUiFlowController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private EventChoiceScreenController eventChoiceScreen;
        [SerializeField] private CursePanelController cursePanel;

        private void Awake()
        {
            BindPanelsToRunMap();
        }

        public void Configure(RunMapController runMap, EventChoiceScreenController eventController, CursePanelController curseController)
        {
            runMapController = runMap;
            eventChoiceScreen = eventController;
            cursePanel = curseController;

            BindPanelsToRunMap();
        }

        public void BindRunMap(RunMapController runMap)
        {
            runMapController = runMap;
            BindPanelsToRunMap();
        }

        public void OnNodeEntered(NodeType nodeType)
        {
            EnsureRunMap();
            if (nodeType == NodeType.Event)
            {
                eventChoiceScreen?.OpenEvent();
            }

            cursePanel?.RefreshPanel();
        }

        public void OnEventClosed()
        {
            EnsureRunMap();
            eventChoiceScreen?.CloseEvent();
            cursePanel?.RefreshPanel();
        }

        public void RefreshRuntimePanels()
        {
            EnsureRunMap();
            cursePanel?.RefreshPanel();
        }

        private void EnsureRunMap()
        {
            if (runMapController != null)
            {
                return;
            }

            runMapController = FindFirstObjectByType<RunMapController>();
            BindPanelsToRunMap();
        }

        private void BindPanelsToRunMap()
        {
            if (runMapController == null)
            {
                return;
            }

            eventChoiceScreen?.Bind(runMapController);
            cursePanel?.Bind(runMapController);
        }
    }
}
