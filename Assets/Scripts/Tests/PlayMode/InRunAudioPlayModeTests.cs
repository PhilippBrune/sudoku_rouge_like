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

        // [REQ: AUDIO-SFX-001] GAP-073 round 2: in a puzzle, choosing a cell, gold coming in and a combo of five sound, with
        // the owner's clips. The run state is changed directly; RunSoundCuesTests covers each cue's rule.
        [UnityTest]
        [Requirement("AUDIO-SFX-001")]
        public IEnumerator InAPuzzle_ACell_Gold_AndACombo_Sound()
        {
            var bootstrap = new GameObject("AudioBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;
            Object.FindAnyObjectByType<MainMenuController>().SelectProfileSlot(TestSlot);
            for (var i = 0; i < 3; i++) yield return null;
            Find("BtnContinueFirstRun")?.GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;
            bootstrap.LaunchRun(new LaunchRequest { ClassId = ClassId.NumberFreak, Mode = GameMode.GardenRun });
            for (var i = 0; i < 10; i++) yield return null;
            var tile = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name.StartsWith("Node_Puzzle_") && b.interactable);
            Assert.NotNull(tile, "a puzzle tile to enter");
            tile.onClick.Invoke();
            for (var waited = 0f; waited < 8f && !(Find("SudokuGameplayPanel")?.activeInHierarchy ?? false); waited += Time.unscaledDeltaTime)
                yield return null;
            Assert.IsTrue(Find("SudokuGameplayPanel").activeInHierarchy, "the puzzle opened");
            for (var i = 0; i < 5; i++) yield return null;

            var heard = new List<string>();
            void Hear(string clip) => heard.Add(clip);
            RunAudioController.Played += Hear;
            try
            {
                Find("C_0_0").GetComponent<Button>().onClick.Invoke();
                Find("C_1_1").GetComponent<Button>().onClick.Invoke();
                yield return null;
                CollectionAssert.Contains(heard, "cell_select", "choosing another cell");

                var state = bootstrap.Run.State;
                heard.Clear();
                state.CurrentGold += 5;
                state.ComboStreak = 5;
                for (var i = 0; i < 2; i++) yield return null;
                CollectionAssert.IsSubsetOf(new[] { "gold_gain", "combo_05" }, heard, string.Join(", ", heard));
            }
            finally { RunAudioController.Played -= Hear; }
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

        // [REQ: OPT-DRAG-001] A press on a value row's bar sets the value at that point, in the row's steps (RM-210).
        [UnityTest]
        [Requirement("OPT-DRAG-001")]
        public IEnumerator APressOnTheBar_SetsTheValueThere()
        {
            new GameObject("DragBootstrapUnderTest").AddComponent<GameBootstrap>();
            for (var i = 0; i < 5; i++) yield return null;
            Object.FindAnyObjectByType<MainMenuController>().SelectProfileSlot(TestSlot);
            for (var i = 0; i < 3; i++) yield return null;
            Find("BtnContinueFirstRun")?.GetComponent<Button>().onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;
            Click("BtnOptions");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var bar = (RectTransform)Find("MusicVolumeBar").transform;
            var corners = new Vector3[4];
            bar.GetWorldCorners(corners);
            var canvas = bar.GetComponentInParent<Canvas>().rootCanvas;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            // a quarter of the way along the bar, vertically in its middle
            var point = RectTransformUtility.WorldToScreenPoint(cam, Vector3.Lerp(corners[0], corners[3], 0.25f) + (corners[1] - corners[0]) * 0.5f);
            var press = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { position = point };   // an overlay canvas: no camera
            UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(bar.gameObject, press, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            yield return null;
            Assert.AreEqual("25%", Shown("MusicVolumeVal"), "the value where the bar was pressed");

            UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(bar.gameObject, press, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.AreEqual(0.25f, OptionsController.LiveAudio.MusicVolume, 0.001f, "set on release");
            Assert.AreEqual(0.25f, new ProfileService(new SaveFileService(TestSlot)).LoadOptions().Audio.MusicVolume, 0.001f,
                "and saved once");
            Assert.AreEqual(0.35f, DragValueBar.ValueAt(0.33f, 0f, 1f, 0.05f), 0.001f, "rounded to the row's 5 % steps");
            Assert.AreEqual(0.10f, DragValueBar.ValueAt(-1f, 0.10f, 1f, 0.05f), 0.001f, "never under the row's limit");
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
