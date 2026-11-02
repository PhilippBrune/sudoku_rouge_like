using System.Collections;
using System.Linq;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // The 0.3.3 shots that need the new screens (the story scenes, the XP brush); the 0.3.2 mirror builds without them.
    public sealed partial class LayoutScreenshotDiagnostics
    {
        // STORY-SCENE-001 / UI-STYLE-040 (owner's pick A): a warden's line in chapter 3, a keeper alone in chapter 1, the
        // voice's line, the ninth chapter, and chapter 1's scene on the first floor's entry.
        [UnityTest]
        public IEnumerator RenderStoryScenes()
        {
            var camera = CreateCamera();
            var scenes = new[]
            {
                (ClassId.NumberFreak, 3, 1, "story_scene_warden"),
                (ClassId.GardenMonk, 1, 0, "story_scene_keeper"),
                (ClassId.ReedDuelist, 4, 3, "story_scene_voice"),
            };
            foreach (var (keeper, chapter, line, file) in scenes)
            {
                var scene = StoryScene.Show(keeper, chapter);
                _createdRoots.Add(scene.gameObject);
                for (var i = 0; i < line; i++) scene.Next();
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Shoot(scene.gameObject, camera, $"after_{file}.png");
                scene.Close();
                yield return null;
            }
            var ninth = StoryScene.ShowNinth();
            _createdRoots.Add(ninth.gameObject);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(ninth.gameObject, camera, "after_story_scene_ninth.png");
            ninth.Close();
            yield return null;

            new SaveFileService(TestSlot).Save(new SaveFileEnvelope { MetaProgress = new MetaProgressionState() });
            SaveFileService.FlushSharedPendingWrites();
            var (host, _) = StartRun(7340, false);
            yield return null;
            yield return null;
            var intro = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "RunIntroCard");   // not DismissIntro: it ends the floor's entry too
            if (intro != null) intro.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shoot(host, camera, "after_floor_entry_story_scene.png");
            StoryScene.Current?.Close();
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
        }

        // UI-STYLE-041 (owner's pick B): the brush paints a won run's XP into the bar: early, halfway and at the end.
        [UnityTest]
        public IEnumerator RenderXpBrush()
        {
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
            var camera = CreateCamera();
            var (host, run) = StartRun(7363, false);
            yield return DismissIntro();
            for (var i = 0; i < 14; i++)
            {
                var tile = new TileXpEntry { BoardSize = 9, Stars = 3, TotalXp = 16 };
                run.TileXpLog.Add(tile);
                run.GetAnalytics().RecordTileXp(tile);
                run.GetAnalytics().RecordPuzzleSolved(0, 0, i / 3);
            }
            run.GetAnalytics().RecordRunComplete();
            host.GetComponentInChildren<EndScreenViewController>(true).ShowGameOver(run, true);
            var started = Time.unscaledTime;
            foreach (var (at, file) in new[] { (0.35f, "xp_brush_early"), (1.1f, "xp_brush_half"), (XpBrushAnimator.Seconds + 0.6f, "xp_brush_end") })
            {
                while (Time.unscaledTime - started < at) yield return null;
                yield return Shoot(host, camera, $"after_{file}.png");
            }
            new SaveFileService(TestSlot).Save(new SaveFileEnvelope());
            SaveFileService.FlushSharedPendingWrites();
        }
    }
}
