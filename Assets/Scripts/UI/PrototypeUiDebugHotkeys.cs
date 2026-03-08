using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private Text pathPreviewText;

        private readonly ProfileService _profile = new();

        private void Start()
        {
            EnsureEventSystem();
            RefreshPathPreview();
        }

        public void Configure(InRunUiFlowController flowController, RunMapController mapController, Text previewText = null)
        {
            inRunUiFlow = flowController;
            runMapController = mapController;
            if (previewText != null)
            {
                pathPreviewText = previewText;
            }
            RefreshPathPreview();
        }

        private void Update()
        {
            if (inRunUiFlow == null)
            {
                return;
            }

            if (WasOpenEventPressed()) OpenEventPanel();
            if (WasCloseEventPressed()) CloseEventPanel();
            if (WasNextNodePressed()) AdvanceNodeSafe();
            if (WasPathAPressed()) ChoosePathCalm();
            if (WasPathBPressed()) ChoosePathRisky();
            if (WasQuitAndSavePressed()) QuitAndSave();
            if (WasAutoSolvePressed()) DebugAutoSolve();
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

        private static bool WasNextNodePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return Keyboard.current.nKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.N);
#endif
        }

    private static bool WasPathAPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F1);
#endif
    }

    private static bool WasPathBPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F2);
#endif
    }

    private static bool WasQuitAndSavePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

        private static bool WasAutoSolvePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.P);
#endif
        }

        public void OpenEventPanel()
        {
            EnsureBindings();
            if (inRunUiFlow == null)
            {
                Debug.LogWarning("Debug UI: InRunUiFlowController reference is missing.");
                return;
            }

            inRunUiFlow.OnNodeEntered(NodeType.Event);
            Debug.Log("Debug UI: Event panel opened.");
        }

        public void CloseEventPanel()
        {
            EnsureBindings();
            if (inRunUiFlow == null)
            {
                Debug.LogWarning("Debug UI: InRunUiFlowController reference is missing.");
                return;
            }

            inRunUiFlow.OnEventClosed();
            Debug.Log("Debug UI: Event panel closed and panels refreshed.");
        }

        public void ChoosePathCalm()
        {
            AdvancePath(risk: false);
        }

        public void ChoosePathRisky()
        {
            AdvancePath(risk: true);
        }

        public void AdvanceNodeSafe()
        {
            ChoosePathCalm();
        }

        private void AdvancePath(bool risk)
        {
            EnsureBindings();
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

            if (runMapController.TryAdvancePathAndStartNextPuzzle(risk, out var node, out var level, out var failureReason))
            {
                inRunUiFlow.OnNodeEntered(node.Type);
                RefreshPathPreview();
                Debug.Log($"Garden path chosen: {(risk ? "Risk" : "Calm")} -> {node.Type} (Depth {node.Depth}) {level.BoardSize}x{level.BoardSize}, {level.Stars}★");
                return;
            }

            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                if (pathPreviewText != null)
                {
                    pathPreviewText.text = $"{pathPreviewText.text}\n\nBlocked: {failureReason}";
                }
                Debug.LogWarning($"Garden path blocked: {failureReason}");
            }
            else
            {
                Debug.LogWarning("Debug UI: No next node available. Ensure RunMapController is initialized and graph exists.");
            }
        }

        public void QuitAndSave()
        {
            EnsureBindings();
            var run = runMapController?.Run;
            if (run != null)
            {
                run.OnQuitRequested();
            }

            if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
                return;
            }

            Debug.LogWarning("Debug UI: Quit & Save could not load menu scene. Add MainMenu to build settings or set mainMenuSceneName.");
        }

        private void DebugAutoSolve()
        {
            EnsureBindings();
            var board = runMapController?.Run?.CurrentBoard;
            if (board == null)
            {
                Debug.LogWarning("Debug UI: No active puzzle to auto-solve.");
                return;
            }

            var size = board.Size;
            for (var r = 0; r < size; r++)
            {
                for (var c = 0; c < size; c++)
                {
                    if (!board.IsGiven(r, c) && board.IsEmpty(r, c))
                    {
                        runMapController.Run.PlaceNumber(r, c, board.Solution[r, c]);
                    }
                }
            }

            Debug.Log("Debug UI: Auto-solved current puzzle.");
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

        private void EnsureBindings()
        {
            if (inRunUiFlow == null)
            {
                inRunUiFlow = FindFirstObjectByType<InRunUiFlowController>();
            }

            if (runMapController == null)
            {
                runMapController = FindFirstObjectByType<RunMapController>();
            }

            if (inRunUiFlow != null && runMapController != null)
            {
                inRunUiFlow.BindRunMap(runMapController);
            }

            if (pathPreviewText == null)
            {
                pathPreviewText = FindByName<Text>("PathPreviewText");
            }
        }

        private void RefreshPathPreview()
        {
            EnsureBindings();

            if (pathPreviewText == null || runMapController == null)
            {
                return;
            }

            var calm = runMapController.BuildPathChoicePreview(risk: false);
            var risk = runMapController.BuildPathChoicePreview(risk: true);

            pathPreviewText.text =
                $"Path A: {FormatPreview(calm)}\n" +
                $"Path B: {FormatPreview(risk)}\n" +
                BuildLockLine(calm, risk) + "\n" +
                "Progress requires solving the current Sudoku.";
        }

        private static string BuildLockLine(RunMapController.PathChoicePreview calm, RunMapController.PathChoicePreview risk)
        {
            var lockValue = calm?.LockedPath ?? risk?.LockedPath;
            if (!lockValue.HasValue)
            {
                return "Path lock: not chosen yet.";
            }

            return lockValue.Value ? "Path lock: B (Risk)" : "Path lock: A (Calm)";
        }

        private static string FormatPreview(RunMapController.PathChoicePreview preview)
        {
            if (preview == null || !preview.Available)
            {
                return "Not available";
            }

            var bossTag = preview.IsBoss ? " [Boss]" : string.Empty;
            return $"D{preview.Depth} {preview.NodeType}{bossTag} | {preview.BoardSize}x{preview.BoardSize} | {preview.Stars}★";
        }

        private static T FindByName<T>(string name) where T : Component
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                var go = candidate.gameObject;
                if (go == null || go.name != name)
                {
                    continue;
                }

                var scene = go.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
