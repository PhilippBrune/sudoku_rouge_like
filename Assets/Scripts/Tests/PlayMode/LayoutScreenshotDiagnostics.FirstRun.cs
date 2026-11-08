using System.Collections;
using System.Linq;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    // A brand-new profile (owner, 2026-11-08: "not yet in the new design"): the first-run setup (language, accessibility,
    // legal) and the Sudoku Basics prompt after it.
    public sealed partial class LayoutScreenshotDiagnostics
    {
        [UnityTest]
        public IEnumerator RenderFirstRunScreens()
        {
            var camera = CreateCamera();
            var host = new GameObject("FirstRunShotHost");
            _createdRoots.Add(host);
            host.AddComponent<MainMenuBlueprintBuilder>().Build();
            var root = host.GetComponentsInChildren<Transform>(true).First(t => t.name == "MainMenuRoot");
            var group = root.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            var menu = host.GetComponent<MainMenuController>();
            menu.ShowMainMenu();
            menu.ShowFirstRunSetupPrompt(menu.ShowSudokuBasicsPrompt);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, "first_run_setup.png");
            host.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "BtnContinueFirstRun")?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shoot(host, camera, "first_run_basics_prompt.png");
            host.SetActive(false);
        }
    }
}
