using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // "Before" screenshots for the layout proposals (docs/design/layout-proposals.md): the garden path overview and a
    // puzzle at 1920×1080. Needs graphics, so it is [Explicit] and runs without -nographics (see
    // OptionsScreenshotDiagnostics for the command line). Output: SR_SHOT_DIR or %TEMP%\sr_layout_shots.
    [TestFixture]
    [Explicit("Writes screenshots; run on demand with graphics")]
    public sealed class LayoutScreenshotDiagnostics
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private readonly List<GameObject> _createdRoots = new List<GameObject>();
        private int _originalActiveSlot;
        private string _outDir;
        private Action<RunDirector> _beforeBind;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            _outDir = Environment.GetEnvironmentVariable("SR_SHOT_DIR");
            if (string.IsNullOrEmpty(_outDir)) _outDir = Path.Combine(Path.GetTempPath(), "sr_layout_shots");
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
        public IEnumerator RenderPathOverviewAndPuzzle()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var camera = new GameObject("LayoutShotCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            _createdRoots.Add(camera.gameObject);

            // Garden path overview right after the run starts (Floor 1).
            var (pathHost, _) = StartRun(7301, false);
            yield return DismissIntro();
            yield return Shoot(pathHost, camera, "before_path_floor1.png");
            pathHost.SetActive(false);

            // A 6×6 puzzle with two modifiers and some digits placed.
            var (puzzleHost, run) = StartRun(7302, true, BossModifierId.GermanWhispers, BossModifierId.KillerCages);
            yield return DismissIntro();
            for (var frame = 0; frame < 30 && Find(puzzleHost, "C_0_0") == null; frame++)
                yield return null;
            yield return Shoot(puzzleHost, camera, "before_puzzle_6x6.png");
            Assert.NotNull(run.CurrentBoard);
        }

        // UI-STYLE-004/005: the trial scroll on Floor 3 (Koi Terrace, four trials, highest intensity, two picked) and
        // on Floor 5 (six trials in two rows, none discovered yet), then the offering table after the Floor 3 boss.
        [UnityTest]
        public IEnumerator RenderBossGateAndBossDefeated()
        {
            var camera = CreateCamera();

            var (gateHost, gateRun) = StartRun(7303, true);
            gateRun.State.CurrentFloor = 2;
            gateRun.State.RunNumber = 9;
            foreach (BossModifierId id in Enum.GetValues(typeof(BossModifierId)))
                gateRun.State.SeenBossModifiers.Add(id);
            yield return DismissIntro();
            var gate = gateHost.GetComponentInChildren<BossGateViewController>(true);
            gate.ShowBossGateChoice();
            yield return null;
            Find(gateHost, "Mod_0")?.GetComponent<Button>().onClick.Invoke();
            Find(gateHost, "Mod_1")?.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(gateHost, camera, "after_boss_gate.png");
            gateHost.SetActive(false);

            var (wideHost, wideRun) = StartRun(7306, true);
            wideRun.State.CurrentFloor = 4;
            wideRun.State.RunNumber = 3;
            yield return DismissIntro();
            wideHost.GetComponentInChildren<BossGateViewController>(true).ShowBossGateChoice();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(wideHost, camera, "after_boss_gate_floor5.png");
            wideHost.SetActive(false);

            var (clearedHost, clearedRun) = StartRun(7304, true);
            clearedRun.State.CurrentFloor = 2;
            yield return DismissIntro();
            clearedHost.GetComponentInChildren<EndScreenViewController>(true)
                .ShowBossCleared(clearedRun, clearedRun.RollRelicChoices(3), _ => { }, () => { });
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shoot(clearedHost, camera, "after_boss_defeated.png");
        }

        // UI-STYLE-001: the garden path as a hand scroll on each of the five floors (one run advanced floor by floor).
        [UnityTest]
        public IEnumerator RenderPathScrollOnEveryFloor()
        {
            var camera = CreateCamera();
            for (var floor = 0; floor < 5; floor++)
            {
                var target = floor;
                _beforeBindAlways = run =>
                {
                    for (var f = 0; f < target; f++) run.AdvanceToNextFloor();
                    run.State.HeldRelics.Clear();
                };
                var (host, _) = StartRun(7310 + floor, false);
                _beforeBindAlways = null;
                yield return DismissIntro();
                yield return new WaitForSecondsRealtime(2.0f); // node pop-in and first-pick pulses settle
                var label = host.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name == "Label" && t.transform.parent.name.StartsWith("Node_"));
                if (label != null) Debug.Log($"[LayoutShot] node label scale {label.transform.lossyScale}, color {label.color}, size {label.fontSize}/{label.cachedTextGenerator.fontSizeUsedForBestFit}");
                yield return Shoot(host, camera, $"after_path_floor{floor + 1}.png");
                host.SetActive(false);
            }
        }

        private Action<RunDirector> _beforeBindAlways;

        // UI-STYLE-006: killer cages of five shapes on a 6×6 board; each must read as one dashed outline.
        [UnityTest]
        public IEnumerator RenderKillerCageShapes()
        {
            var camera = CreateCamera();
            _beforeBind = run =>
            {
                var cages = run.CurrentOverlay.KillerCages;
                cages.Clear();
                cages.Add(Cage(run, (0, 0), (0, 1)));
                cages.Add(Cage(run, (1, 3), (2, 3), (3, 3), (2, 2)));
                cages.Add(Cage(run, (3, 0), (4, 0), (4, 1)));
                cages.Add(Cage(run, (0, 4), (0, 5), (1, 4), (1, 5)));
                cages.Add(Cage(run, (4, 4), (4, 5), (5, 3), (5, 4)));
            };
            var (host, _) = StartRun(7305, true, BossModifierId.KillerCages);
            _beforeBind = null;
            yield return DismissIntro();
            for (var frame = 0; frame < 30 && Find(host, "C_0_0") == null; frame++)
                yield return null;
            yield return Shoot(host, camera, "cages_6x6.png");
        }

        private static KillerCage Cage(RunDirector run, params (int r, int c)[] cells)
        {
            var cage = new KillerCage();
            foreach (var (r, c) in cells)
            {
                cage.Cells.Add(new CellCoord(r, c));
                cage.Sum += run.CurrentBoard.Solution[r, c];
            }
            return cage;
        }

        private Camera CreateCamera()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                _createdRoots.Add(new GameObject("LayoutShotEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            var camera = new GameObject("LayoutShotCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            _createdRoots.Add(camera.gameObject);
            return camera;
        }

        private (GameObject host, RunDirector run) StartRun(int seed, bool startPuzzle, params BossModifierId[] modifiers)
        {
            var host = new GameObject("LayoutShotHost" + seed);
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
            _beforeBindAlways?.Invoke(run);
            if (startPuzzle)
            {
                var node = run.GetReachableNodes().Select(i => run.CurrentFloorGraph[i]).First(n => n.Type == NodeType.Puzzle);
                run.TryAdvanceToNode(node.Index);
                var config = run.BuildLevelConfig(false, false, node.Index);
                config.BoardSize = 6;
                config.RegionVariant = 0;
                config.ActiveModifiers.Clear();
                config.ActiveModifiers.AddRange(modifiers);
                run.StartLevel(config);
                _beforeBind?.Invoke(run);
            }
            var map = host.GetComponentInChildren<RunMapController>(true);
            map.BindRun(run, new SaveFileService(TestSlot));
            host.GetComponentInChildren<InRunController>(true).NotifyRunStarted();
            return (host, run);
        }

        private static IEnumerator DismissIntro()
        {
            yield return null;
            yield return null;
            var intro = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "RunIntroCard");
            if (intro != null) intro.onClick.Invoke();
            yield return null;
            yield return null;
        }

        private IEnumerator Shoot(GameObject host, Camera camera, string file)
        {
            var rt = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = rt;
            foreach (var canvas in host.GetComponentsInChildren<Canvas>(true).Where(c => c.isRootCanvas))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(_outDir, file), tex.EncodeToPNG());
            camera.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        private static GameObject Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)?.gameObject;
    }
}
