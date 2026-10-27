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
            AudioListener.pause = false;   // a failed focus check must not leave the next tests silent
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
                         "IGSfxVolumeInc" })
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

        // [REQ: OPT-AUDIO-002] Every volume row changes what is heard: the menu music plays Master × Music, the run's music
        // and effects follow Master × Music / SFX, and Mute all silences them. No UI Volume row (no interface sound plays).
        // [REQ: UI-OPT-004] The run's Options show and keep what was set in the main menu's Options.
        [UnityTest]
        [Requirement("OPT-AUDIO-002", "UI-OPT-004")]
        public IEnumerator EveryVolumeRow_ChangesWhatIsHeard_InTheMenuAndInARun()
        {
            var bootstrap = new GameObject("AudioBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;
            Object.FindAnyObjectByType<MainMenuController>().SelectProfileSlot(TestSlot);
            for (var i = 0; i < 3; i++) yield return null;
            Find("BtnContinueFirstRun")?.GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;

            Click("BtnOptions");
            Assert.IsNull(Find("UiVolumeDec"), "no UI Volume row: no interface sound plays");
            for (var i = 0; i < 10; i++) Click("MasterVolumeDec");   // 100 % to 50 %
            Click("MusicVolumeDec");
            Click("MusicVolumeDec");                                  // 80 % to 70 %
            yield return null;
            var menuSource = Object.FindObjectsByType<MenuMusicController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single().GetComponent<AudioSource>();
            Assert.AreEqual("50%", Shown("MasterVolumeVal"));
            Assert.AreEqual(0.5f * 0.7f, menuSource.volume, 0.01f, "the menu music plays Master × Music");
            Find("MuteAllToggle").GetComponent<Toggle>().isOn = true;
            Assert.AreEqual(0f, menuSource.volume, 0.001f, "Mute all silences the menu music");
            Find("MuteAllToggle").GetComponent<Toggle>().isOn = false;
            Assert.AreEqual(0.35f, menuSource.volume, 0.01f);
            var style = Find("MenuMusicStyleDropdown").GetComponent<Dropdown>();
            var clipBefore = menuSource.clip;
            style.value = 2;
            Assert.AreNotSame(clipBefore, menuSource.clip, "Menu music style changes the menu track");
            style.value = 0;
            Find("MuteUnfocusedToggle").GetComponent<Toggle>().isOn = true;
            bootstrap.SendMessage("OnApplicationFocus", false);
            Assert.IsTrue(AudioListener.pause, "Mute when unfocused pauses the sound when the game loses focus");
            bootstrap.SendMessage("OnApplicationFocus", true);
            Assert.IsFalse(AudioListener.pause);
            Find("MuteUnfocusedToggle").GetComponent<Toggle>().isOn = false;
            Click("BtnOptBack");

            bootstrap.LaunchRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun });
            for (var i = 0; i < 10; i++) yield return null;
            var tile = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name.StartsWith("Node_Puzzle_") && b.interactable);
            Assert.NotNull(tile, "a puzzle tile to enter");
            tile.onClick.Invoke();
            for (var waited = 0f; waited < 8f && !(Find("SudokuGameplayPanel")?.activeInHierarchy ?? false); waited += Time.unscaledDeltaTime)
                yield return null;
            for (var i = 0; i < 5; i++) yield return null;
            Click("BtnSudokuOptions");
            yield return null;
            Assert.AreEqual("50%", Shown("IGMasterVolumeVal"), "the run's Options show the main menu's Master Volume");
            Assert.AreEqual("70%", Shown("IGMusicVolumeVal"), "the run's Options show the main menu's Music Volume");

            var run = Object.FindAnyObjectByType<RunAudioController>();
            var music = run.GetComponent<AudioSource>();
            var sfx = Find("RunSfxSource").GetComponent<AudioSource>();
            foreach (var row in new[] { "IGSfxVolumeDec", "IGMusicVolumeDec", "IGMasterVolumeInc" })
            {
                Click(row);
                yield return null;
                float master = Percent("IGMasterVolumeVal"), musicVol = Percent("IGMusicVolumeVal");
                Assert.AreEqual(master * musicVol * 0.75f, music.volume, 0.01f, row + ": run music = Master × Music");
                Assert.AreEqual(master * Percent("IGSfxVolumeVal") * 0.55f, sfx.volume, 0.01f, row + ": effects = Master × SFX");
            }

            var saved = new ProfileService(new SaveFileService(TestSlot)).LoadOptions().Audio;
            Assert.AreEqual(Percent("IGMasterVolumeVal"), saved.MasterVolume, 0.001f, "the save keeps the shown Master Volume");
            Assert.AreEqual(Percent("IGMusicVolumeVal"), saved.MusicVolume, 0.001f, "the save keeps the shown Music Volume");

            Find("IGMuteAllToggle").GetComponent<Toggle>().isOn = true;
            yield return null;
            Assert.IsTrue(music.mute && sfx.mute, "Mute all silences the run");
            Find("IGMuteAllToggle").GetComponent<Toggle>().isOn = false;
            yield return null;
            Assert.IsFalse(music.mute || sfx.mute);
        }

        private static void Click(string name)
        {
            var button = Find(name);
            Assert.NotNull(button, name);
            button.GetComponent<Button>().onClick.Invoke();
        }

        private static string Shown(string name) => Find(name).GetComponent<Text>().text;

        private static float Percent(string name) => float.Parse(Shown(name).TrimEnd('%')) / 100f;

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
