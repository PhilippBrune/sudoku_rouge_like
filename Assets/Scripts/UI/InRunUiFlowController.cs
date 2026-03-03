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
            if (runMapController == null)
            {
                return;
            }

            eventChoiceScreen?.Bind(runMapController);
            cursePanel?.Bind(runMapController);
            heatCurveGraph?.Bind(runMapController);
        }

        public void Configure(RunMapController runMap, EventChoiceScreenController eventController, CursePanelController curseController, HeatCurveGraphController heatController)
        {
            runMapController = runMap;
            eventChoiceScreen = eventController;
            cursePanel = curseController;
            heatCurveGraph = heatController;

            if (runMapController != null)
            {
                eventChoiceScreen?.Bind(runMapController);
                cursePanel?.Bind(runMapController);
                heatCurveGraph?.Bind(runMapController);
            }
        }

        public void OnNodeEntered(NodeType nodeType)
        {
            if (nodeType == NodeType.Event)
            {
                eventChoiceScreen?.OpenEvent();
            }

            cursePanel?.RefreshPanel();
            heatCurveGraph?.RenderCurrentRunCurve();
        }

        public void OnEventClosed()
        {
            eventChoiceScreen?.CloseEvent();
            cursePanel?.RefreshPanel();
            heatCurveGraph?.RenderCurrentRunCurve();
        }

        public void RefreshRuntimePanels()
        {
            cursePanel?.RefreshPanel();
            heatCurveGraph?.RenderCurrentRunCurve();
        }
    }
}
