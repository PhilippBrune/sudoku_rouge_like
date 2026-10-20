using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.Tests
{
    // GAP-057 (0.2.0 playtest): changing a volume in the in-run Options started the main menu's music. Runs the real
    // GameBootstrap: a run is launched, the in-run music volume is raised, and the menu track must stay silent.
    [TestFixture]
    [Requirement("UI-OPT-001")]
    public sealed class InRunAudioPlayModeTests
    {
        private const int TestSlot = SaveFileService.MaxSlots - 1;
        private readonly HashSet<GameObject> _rootsBefore = new HashSet<GameObject>();
        private int _originalActiveSlot;
        private bool _originalSplashShown;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveFileService.FlushSharedPendingWrites();
            _originalActiveSlot = SaveProfileService.ActiveSlot;
            SaveProfileService.ActiveSlot = TestSlot;
            DeleteAllSlots();
            _originalSplashShown = SplashSceneBootstrap.HasShownSplash;
            SetSplashShown(true);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                _rootsBefore.Add(t.root.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveFileService.FlushSharedPendingWrites();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.parent == null && !_rootsBefore.Contains(t.gameObject))
                    Object.Destroy(t.gameObject);
            _rootsBefore.Clear();
            SetSplashShown(_originalSplashShown);
            DeleteAllSlots();
            SaveProfileService.ActiveSlot = _originalActiveSlot;
            yield return null;
        }

        [UnityTest]
        public IEnumerator InRunVolumeChange_DoesNotStartTheMenuMusic()
        {
            var bootstrap = new GameObject("AudioBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;
            Object.FindAnyObjectByType<MainMenuController>().SelectProfileSlot(TestSlot);
            for (var i = 0; i < 3; i++) yield return null;
            Find("BtnContinueFirstRun")?.GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;

            bootstrap.LaunchRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun });
            for (var i = 0; i < 10; i++) yield return null;
            var menuMusic = Object.FindObjectsByType<MenuMusicController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var menuSource = menuMusic.GetComponent<AudioSource>();
            Debug.Log("[GAP-057] before: " + Playing());
            Assert.IsFalse(menuSource.isPlaying && menuSource.isActiveAndEnabled, "no menu music in a run");

            // Into a puzzle: the first reachable puzzle tile, then the puzzle's own Options button.
            var tile = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name.StartsWith("Node_Puzzle_") && b.interactable);
            Assert.NotNull(tile, "a puzzle tile to enter");
            tile.onClick.Invoke();
            for (var waited = 0f; waited < 8f && !(Find("SudokuGameplayPanel")?.activeInHierarchy ?? false); waited += Time.unscaledDeltaTime)
                yield return null;
            Assert.IsTrue(Find("SudokuGameplayPanel").activeInHierarchy, "the puzzle opened");
            for (var i = 0; i < 5; i++) yield return null;
            Find("BtnSudokuOptions").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Debug.Log("[GAP-057] in puzzle, options open: " + Playing());

            foreach (var control in new[] { "IGMusicVolumeDec", "IGMusicVolumeInc", "IGMasterVolumeDec", "IGMasterVolumeInc",
                         "IGSfxVolumeInc", "IGUiVolumeInc" })
            {
                var button = Find(control);
                Assert.NotNull(button, control);
                button.GetComponent<Button>().onClick.Invoke();
                for (var i = 0; i < 3; i++) yield return null;
            }
            var muteAll = Find("IGMuteAll")?.GetComponent<Toggle>();
            if (muteAll != null) { muteAll.isOn = !muteAll.isOn; yield return null; muteAll.isOn = !muteAll.isOn; }
            for (var i = 0; i < 5; i++) yield return null;
            Debug.Log("[GAP-057] after: " + Playing());

            var menuClips = new[] { "MenuLoop", "menu_loop" };
            var playingMenu = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(s => s.isPlaying && s.clip != null && menuClips.Any(n => s.clip.name.Contains(n))).Select(s => s.name).ToList();
            CollectionAssert.IsEmpty(playingMenu, "changing a volume in a run must not start the menu track");
            Assert.IsFalse(menuSource.isPlaying && menuSource.isActiveAndEnabled, "the menu music stays off");
        }

        private static string Playing() => string.Join(", ",
            Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(s => $"{s.name}[{(s.clip != null ? s.clip.name : "-")}, playing={s.isPlaying}, enabled={s.isActiveAndEnabled}, vol={s.volume:0.00}]"));

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name == name) return t.gameObject;
            return null;
        }

        private static void SetSplashShown(bool value)
        {
            var property = typeof(SplashSceneBootstrap).GetProperty("HasShownSplash", BindingFlags.Public | BindingFlags.Static);
            property.GetSetMethod(true).Invoke(null, new object[] { value });
        }

        private static void DeleteAllSlots()
        {
            for (var slot = 0; slot < SaveFileService.MaxSlots; slot++)
            {
                var path = SaveFileService.GetSlotPath(slot);
                foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                    if (File.Exists(p)) File.Delete(p);
            }
        }
    }
}
