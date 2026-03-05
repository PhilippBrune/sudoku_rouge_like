using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace SudokuRoguelike.UI
{
    public sealed class PrototypeUiDebugHotkeys : MonoBehaviour
    {
        [SerializeField] private InRunUiFlowController inRunUiFlow;
        [SerializeField] private RunMapController runMapController;
        [SerializeField] private ClassId fallbackClass = ClassId.NumberFreak;

        private readonly ProfileService _profile = new();

        private void Start()
        {
            EnsureEventSystem();
        }

        public void Configure(InRunUiFlowController flowController, RunMapController mapController)
        {
            inRunUiFlow = flowController;
            runMapController = mapController;
        }

        private void Update()
        {
            if (inRunUiFlow == null)
            {
                return;
            }

            if (WasOpenEventPressed()) OpenEventPanel();
            if (WasCloseEventPressed()) CloseEventPanel();
            if (WasRefreshPressed()) RefreshPanels();
            if (WasNextNodePressed()) AdvanceNodeSafe();
        }

        private static bool WasOpenEventPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private static bool WasCloseEventPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        private static bool WasRefreshPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.H);
#endif
        }

        private static bool WasNextNodePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return Keyboard.current.nKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.RightArrow);
#endif
        }

        public void OpenEventPanel()
        {
            if (inRunUiFlow == null)
            {
                return;
            }

            inRunUiFlow.OnNodeEntered(NodeType.Event);
            Debug.Log("Debug UI: Event panel opened.");
        }

        public void CloseEventPanel()
        {
            if (inRunUiFlow == null)
            {
                return;
            }

            inRunUiFlow.OnEventClosed();
            Debug.Log("Debug UI: Event panel closed and panels refreshed.");
        }

        public void RefreshPanels()
        {
            if (inRunUiFlow == null)
            {
                return;
            }

            inRunUiFlow.RefreshRuntimePanels();
            Debug.Log("Debug UI: Heat/Curse panels refreshed.");
        }

        public void AdvanceNodeSafe()
        {
            if (inRunUiFlow == null)
            {
                Debug.LogWarning("Debug UI: InRunUiFlowController reference is missing.");
                return;
            }

            if (runMapController == null)
            {
                Debug.LogWarning("Debug UI: RunMapController reference is missing.");
                return;
            }

            if (runMapController.Run == null)
            {
                runMapController.Initialize(fallbackClass, _profile.Meta);
                Debug.Log("Debug UI: RunMapController auto-initialized for prototype debug flow.");
            }

            var node = runMapController.SelectPath(risk: false);
            if (node != null)
            {
                inRunUiFlow.OnNodeEntered(node.Type);
                Debug.Log($"Debug UI: Advanced to node {node.Type} at depth {node.Depth}.");
                return;
            }

            Debug.LogWarning("Debug UI: No next node available. Ensure RunMapController is initialized and graph exists.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
            Debug.Log("Debug UI: EventSystem was missing and has been auto-created.");
        }
    }
}
