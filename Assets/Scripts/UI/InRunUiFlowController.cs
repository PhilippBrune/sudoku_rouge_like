using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class InRunUiFlowController : MonoBehaviour
    {
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private EventChoiceScreenController eventChoiceScreen;
        [SerializeField] private CursePanelController cursePanel;
        [SerializeField] private HeatCurveGraphController heatCurveGraph;

        private void Awake()
        {
            BindPanelsToRunMap();
        }

        public void Configure(RunMapController runMap, EventChoiceScreenController eventController, CursePanelController curseController, HeatCurveGraphController heatController)
        {
            runMapController = runMap;
            eventChoiceScreen = eventController;
            cursePanel = curseController;
            heatCurveGraph = heatController;

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
            heatCurveGraph?.RenderCurrentRunCurve();
        }

        public void OnEventClosed()
        {
            EnsureRunMap();
            eventChoiceScreen?.CloseEvent();
            cursePanel?.RefreshPanel();
            heatCurveGraph?.RenderCurrentRunCurve();
        }

        public void RefreshRuntimePanels()
        {
            EnsureRunMap();
            cursePanel?.RefreshPanel();
            heatCurveGraph?.RenderCurrentRunCurve();
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
            heatCurveGraph?.Bind(runMapController);
        }
    }
}
