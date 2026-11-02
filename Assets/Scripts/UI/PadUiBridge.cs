using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: INPUT-PAD-005] A confirms the chosen control on every screen, read through the Input System (0.3.1 playtest:
    /// "A only works now and then"). The owner's pad reaches the Input System only, never the legacy input the UI module
    /// reads ("legacy no" for every button in the player log), so the module's pad Submit never fired. This runs before
    /// every screen's own handler, submits the chosen control once and marks the press as used (PadButtons.SubmitUsed), so
    /// a screen that also reads A does not act a second time. Keys and the mouse stay with the UI module.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PadUiBridge : MonoBehaviour
    {
        private static PadUiBridge _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("PadUiBridge");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PadUiBridge>();
        }

        private void Update()
        {
            PadButtons.SyncUiModule();
            // A pad only the legacy input sees confirms through the UI module's own Submit axis.
            if (!PadButtons.InputSystemPad || !PadButtons.Pressed(KeyCode.JoystickButton0)) return;
            var es = EventSystem.current;
            var chosen = es != null && es.sendNavigationEvents ? es.currentSelectedGameObject : null;
            if (chosen == null || !chosen.activeInHierarchy) return;
            PadButtons.UseSubmit();
            ExecuteEvents.Execute(chosen, new BaseEventData(es), ExecuteEvents.submitHandler);
        }
    }
}
