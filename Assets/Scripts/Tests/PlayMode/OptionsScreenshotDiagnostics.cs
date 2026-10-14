using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // [REQ: UI-OPT-001] Visual check for GAP-029 / FUTURE-015: renders every tab of the main-menu and the in-run Options
    // at 1920×1080 and 1280×720 into PNGs (default %TEMP%\sr_options_shots, or SR_SHOT_DIR). Needs graphics, so it is
    // [Explicit] and runs without -nographics:
    //   Unity.exe -batchmode -projectPath <copy> -runTests -testPlatform PlayMode
    //     -testFilter SudokuRoguelike.Tests.OptionsScreenshotDiagnostics -testResults <xml>
    [TestFixture]
    [Explicit("Writes screenshots; run on demand with graphics")]
    public sealed class OptionsScreenshotDiagnostics
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private static readonly Vector2Int[] Sizes = { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) };

        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;
        private string _outDir;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            _outDir = Environment.GetEnvironmentVariable("SR_SHOT_DIR");
            if (string.IsNullOrEmpty(_outDir)) _outDir = Path.Combine(Path.GetTempPath(), "sr_options_shots");
            Directory.CreateDirectory(_outDir);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            for (var i = _createdRoots.Count - 1; i >= 0; i--)
                if (_createdRoots[i] != null) Object.Destroy(_createdRoots[i]);
            _createdRoots.Clear();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RenderMainMenuAndInRunOptions()
        {
            EnsureEventSystem();
            var camera = new GameObject("OptionsShotCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            _createdRoots.Add(camera.gameObject);

            // Main menu Options
            var menuHost = new GameObject("OptionsShotMainMenuHost");
            _createdRoots.Add(menuHost);
            menuHost.AddComponent<MainMenuBlueprintBuilder>().Build();
            var rootGroup = Find(menuHost, "MainMenuRoot").GetComponent<CanvasGroup>();
            if (rootGroup != null) rootGroup.alpha = 1f;
            menuHost.GetComponent<MainMenuController>().ShowMainMenu();
            yield return null;
            Find(menuHost, "BtnOptions").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return ShootTabs(menuHost, camera, "main", Find(menuHost, "OptionsPanel"));
            menuHost.SetActive(false);

            // In-run Options over a started puzzle (the Run tab shows its seed)
            var runHost = StartPuzzle(8603);
            yield return null;
            yield return null;
            var intro = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "RunIntroCard");
            if (intro != null) intro.onClick.Invoke(); // the class intro card covers the screen at run start
            yield return null;
            Find(runHost, "BtnSudokuOptions").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var panel = Find(runHost, "InGameOptionsPanel");
            Assert.IsTrue(panel.activeInHierarchy, "in-run Options did not open");
            yield return ShootTabs(runHost, camera, "inrun", panel);

            // Controller focus on a control: the selected state must be visible.
            panel.GetComponentInChildren<OptionsTabSet>(true).Activate(0);
            EventSystem.current.SetSelectedGameObject(Find(runHost, "IGMasterVolumeInc"));
            yield return null;
            yield return Shoot(runHost, camera, Path.Combine(_outDir, "inrun_focus_1920x1080.png"), Sizes[0]);
        }

        private IEnumerator ShootTabs(GameObject host, Camera camera, string prefix, GameObject panel)
        {
            var tabs = panel.GetComponentInChildren<OptionsTabSet>(true);
            Assert.NotNull(tabs, panel.name + " has no OptionsTabSet");
            for (var i = 0; i < tabs.Count; i++)
            {
                tabs.Tabs[i].onClick.Invoke();
                EventSystem.current.SetSelectedGameObject(null);
                yield return null;
                foreach (var size in Sizes)
                    yield return Shoot(host, camera,
                        Path.Combine(_outDir, $"{prefix}_{i}_{tabs.Tabs[i].name}_{size.x}x{size.y}.png"), size);
            }
        }

        private static IEnumerator Shoot(GameObject host, Camera camera, string path, Vector2Int size)
        {
            var rt = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = rt;
            foreach (var canvas in host.GetComponentsInChildren<Canvas>(true).Where(c => c.isRootCanvas))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }
            yield return null; // CanvasScaler and layout pick up the new size
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[OptionsScreenshotDiagnostics] wrote " + path);

            camera.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        private GameObject StartPuzzle(int seed)
        {
            var host = new GameObject("OptionsShotInRunHost");
            _createdRoots.Add(host);
            host.AddComponent<InRunUiBlueprintBuilder>().BuildBlueprint();

            var run = new RunDirector();
            run.StartRun(new LaunchRequest
            {
                ClassId = ClassId.NumberFreak,
                Mode = GameMode.GardenRun,
                AllowIrregularPuzzles = false,
                ClassLevel = 1
            }, seed);
            var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
            run.TryAdvanceToNode(node.Index);
            var config = run.BuildLevelConfig(false, false, node.Index);
            config.BoardSize = 6;
            config.RegionVariant = 0;
            run.StartLevel(config);

            var map = host.GetComponentInChildren<RunMapController>(true);
            map.BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return host;
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            _createdRoots.Add(new GameObject("OptionsShotEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
        }

        private static GameObject Find(GameObject root, string name)
        {
            var match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            Assert.NotNull(match, "missing " + name);
            return match.gameObject;
        }
    }
}
