using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace SudokuRoguelike.UI
{
    public sealed class PrototypeUiDebugHotkeys : MonoBehaviour
    {
        private InRunUiFlowController _inRunUiFlow;
        private RunMapController _runMapController;
        private Text _debugText;

        public void Configure(InRunUiFlowController flowController, RunMapController mapController, Text debugText = null)
        {
            _inRunUiFlow = flowController;
            _runMapController = mapController;
            _debugText = debugText;
        }

        private void Start()
        {
            EnsureEventSystem();
        }

        private void Update()
        {
            EnsureBindings();
            if (WasKeyPressed(KeyCode.E)) OpenEventPanel();
            if (WasKeyPressed(KeyCode.C)) CloseEventPanel();
            if (WasKeyPressed(KeyCode.N)) AdvanceToNextNode();
            if (WasKeyPressed(KeyCode.Q)) QuitAndSave();
            if (WasKeyPressed(KeyCode.P)) DebugAutoSolve();
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case KeyCode.E: return kb.eKey.wasPressedThisFrame;
                case KeyCode.C: return kb.cKey.wasPressedThisFrame;
                case KeyCode.N: return kb.nKey.wasPressedThisFrame;
                case KeyCode.Q: return kb.qKey.wasPressedThisFrame;
                case KeyCode.P: return kb.pKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(key);
#endif
        }

        private void OpenEventPanel()
        {
            EnsureBindings();
            if (_inRunUiFlow == null) return;
            _inRunUiFlow.OnNodeEntered(NodeType.Event);
            Debug.Log("Debug: Event panel opened.");
        }

        private void CloseEventPanel()
        {
            EnsureBindings();
            if (_inRunUiFlow == null) return;
            _inRunUiFlow.OnEventClosed();
            Debug.Log("Debug: Event panel closed.");
        }

        private void AdvanceToNextNode()
        {
            EnsureBindings();
            if (_runMapController == null || _runMapController.Run == null) return;

            var graph = _runMapController.GetFloorGraph();
            var currentIndex = _runMapController.Run.State.CurrentNodeIndex;
            var nextIndex = currentIndex + 1;

            if (nextIndex >= graph.Count)
            {
                Debug.Log("Debug: No more nodes on this floor.");
                return;
            }

            if (_runMapController.TryAdvanceToNodeAndStartPuzzle(nextIndex, out var node, out var level))
            {
                _inRunUiFlow?.OnNodeEntered(node.Type);
                var info = level != null ? $"{level.BoardSize}x{level.BoardSize} {level.Stars}★" : node.Type.ToString();
                Debug.Log($"Debug: Advanced to node {nextIndex} ({info})");
            }
        }

        private void QuitAndSave()
        {
            Time.timeScale = 1f;
            var bootstrap = FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
                bootstrap.ReturnToMenu();
            else
                Debug.LogWarning("Debug: GameBootstrap not found.");
        }

        private void DebugAutoSolve()
        {
            EnsureBindings();
            var board = _runMapController?.Run?.CurrentBoard;
            if (board == null)
            {
                Debug.LogWarning("Debug: No active puzzle to auto-solve.");
                return;
            }

            var size = board.Size;
            for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    if (!board.IsGiven(r, c) && board.Cells[r, c] == 0)
                        _runMapController.Run.PlaceNumber(r, c, board.Solution[r, c]);

            Debug.Log("Debug: Auto-solved current puzzle.");
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private void EnsureBindings()
        {
            if (_inRunUiFlow == null)
                _inRunUiFlow = FindFirstObjectByType<InRunUiFlowController>();
            if (_runMapController == null)
                _runMapController = FindFirstObjectByType<RunMapController>();
        }
    }
}
