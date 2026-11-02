using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: STORY-SCENE-001] [REQ: UI-STYLE-040] Story A · The painted dialogue (owner's pick, 0.3.1 playtest): a chapter's
    /// scene over the painting of its warden's floor. The keeper's seal on the left, the warden's on the right (chapters 2
    /// to 4), the talk on a paper band along the bottom one line at a time, the one not speaking dimmed; last the secret
    /// voice tells the chapter in italics. A, Enter or a click: the next line; B or Esc: skip. Its own canvas: on top of the
    /// screen, or inside a parent (the floor's entry), which then ends it with itself.
    /// </summary>
    public sealed class StoryScene : MonoBehaviour
    {
        private static readonly Color DotOff = new Color(0.24f, 0.18f, 0.12f, 0.25f);
        private static StoryScene _current;
        private static int _closedFrame = -1;

        private List<(StoryService.Speaker Speaker, string Key)> _lines;
        private int _index;
        private GameObject _backdrop, _returnFocus;
        private Image _keeperDim, _wardenDim;
        private Text _speaker, _line;
        private string _keeperName, _wardenName;
        private readonly List<Image> _dots = new List<Image>();

        /// <summary>The scene playing now, or null.</summary>
        public static StoryScene Current => _current;

        /// <summary>True while a scene plays and in the frame it closed, so its last press acts on nothing behind it.</summary>
        public static bool Blocking => _current != null || Time.frameCount == _closedFrame;

        public int LineIndex => _index;
        public int LineCount => _lines.Count;

        public static StoryScene Show(ClassId keeper, int chapter, Transform parent = null) =>
            Open(StoryService.SceneLines(keeper, chapter), keeper, chapter, parent);

        /// <summary>The last chapter: the voice alone.</summary>
        public static StoryScene ShowNinth(Transform parent = null) =>
            Open(new List<(StoryService.Speaker, string)> { (StoryService.Speaker.Voice, "Story.Ninth.Text") }, null, 0, parent);

        private static StoryScene Open(List<(StoryService.Speaker, string)> lines, ClassId? keeper, int chapter, Transform parent)
        {
            if (_current != null) return _current;
            var root = new GameObject("StoryScene", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                var rt = (RectTransform)root.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                canvas.overrideSorting = true;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            canvas.sortingOrder = 31000;   // over the run and the menus, under the quit screen
            var scene = root.AddComponent<StoryScene>();
            scene._lines = lines;
            try
            {
                scene.Build((RectTransform)root.transform, keeper, chapter);
                scene.ShowLine(0);
            }
            catch (Exception ex)
            {
                // A scene that cannot be built is left out; it must never hold the input (C# review).
                Debug.LogWarning("[Story] The scene could not be built: " + ex.Message);
                Destroy(root);
                return null;
            }
            _current = scene;
            return scene;
        }

        private void Build(RectTransform root, ClassId? keeper, int chapter)
        {
            var wardenFloor = keeper.HasValue ? StoryService.SceneWardenFloor(chapter) : -1;
            var painting = !keeper.HasValue ? "bg_victory"
                : chapter == StoryService.Chapters ? "bg_shrine_garden" : InRunController.PathBackgroundName(wardenFloor);
            var backdrop = ScrollUiKit.Fill(root, "StoryBackdrop", Vector2.zero, Vector2.one, Color.black);
            backdrop.raycastTarget = true;   // a click anywhere is the next line; nothing behind takes it
            _backdrop = backdrop.gameObject;
            ScrollUiKit.Painting(backdrop.transform, "Painting", painting, new Color(0.62f, 0.62f, 0.62f, 1f));
            ScrollUiKit.Fill(backdrop.transform, "Shade", Vector2.zero, new Vector2(1f, 0.45f), new Color(0f, 0f, 0f, 0.35f));
            var next = _backdrop.AddComponent<Button>();
            next.transition = Selectable.Transition.None;
            next.navigation = new Navigation { mode = Navigation.Mode.None };
            next.onClick.AddListener(Next);

            var tag = ScrollUiKit.PaperTag(root, "StoryChapterTag", new Vector2(0.036f, 0.835f), new Vector2(0.255f, 0.944f), 0f);
            if (keeper.HasValue)
                ScrollUiKit.Label(tag.transform, "Eyebrow", LocalizationService.Format("Story.Chapter", "Chapter {0} of {1} · {2}", chapter,
                        StoryService.Chapters, ClassCatalog.DisplayName(keeper.Value)).ToUpperInvariant(), 15, TextAnchor.MiddleLeft,
                    ScrollUiKit.InkSoft, new Vector2(0.06f, 0.60f), new Vector2(0.96f, 0.92f), 10);
            ScrollUiKit.Label(tag.transform, "Title", keeper.HasValue ? StoryService.Title(keeper.Value, chapter) : StoryService.NinthTitle,
                26, TextAnchor.MiddleLeft, ScrollUiKit.Ink, new Vector2(0.06f, 0.08f), new Vector2(0.96f, keeper.HasValue ? 0.60f : 0.92f), 14);

            if (keeper.HasValue)
            {
                _keeperName = ClassCatalog.DisplayName(keeper.Value);
                _keeperDim = Portrait(root, "KeeperSeal", 0.078f, ScrollUiKit.Gold, ScrollUiKit.Paper,
                    "class/icon_" + ClassCatalog.GetIconName(keeper.Value), -2f, _keeperName);
            }
            if (wardenFloor >= 0)
            {
                _wardenName = StoryService.WardenName(wardenFloor);
                _wardenDim = Portrait(root, "WardenSeal", 0.75f, ScrollUiKit.Paper, ScrollUiKit.Vermilion,
                    "boss/icon_" + FloorThemeData.GetBossKey(wardenFloor), 3f, _wardenName);
            }

            var band = ScrollUiKit.Fill(root, "StoryBand", new Vector2(0.057f, 0.065f), new Vector2(0.943f, 0.25f), ScrollUiKit.Paper);
            var shadow = band.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -14f);
            ScrollUiKit.Fill(band.transform, "RodLeft", new Vector2(-0.016f, -0.05f), new Vector2(-0.003f, 1.05f), ScrollUiKit.Wood, ScrollUiKit.Rounded(11f));
            ScrollUiKit.Fill(band.transform, "RodRight", new Vector2(1.003f, -0.05f), new Vector2(1.016f, 1.05f), ScrollUiKit.Wood, ScrollUiKit.Rounded(11f));
            _speaker = ScrollUiKit.Label(band.transform, "Speaker", string.Empty, 20, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                new Vector2(0.033f, 0.76f), new Vector2(0.967f, 0.92f), 12);
            _line = ScrollUiKit.Label(band.transform, "Line", string.Empty, 32, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.033f, 0.22f), new Vector2(0.967f, 0.74f), 18);
            for (var i = 0; i < _lines.Count; i++)
                _dots.Add(ScrollUiKit.Spot(band.transform, "Dot_" + i, new Vector2(0.036f + i * 0.012f, 0.12f), new Vector2(12f, 12f),
                    DotOff, ScrollUiKit.Disc()));
            var hint = ScrollUiKit.Label(band.transform, "Hint", string.Empty, 17, TextAnchor.MiddleRight, ScrollUiKit.InkSoft,
                new Vector2(0.4f, 0.03f), new Vector2(0.967f, 0.20f), 11);
            DeviceHint.Attach(hint, "Story.Scene.Hint");

            // A (the UI's Submit, PadUiBridge) and Enter land on the backdrop; the focus goes back when the scene ends.
            var es = EventSystem.current;
            _returnFocus = es != null ? es.currentSelectedGameObject : null;
            if (es != null) es.SetSelectedGameObject(_backdrop);
        }

        // A seal on the painting: its frame, the inner colour and the icon, the name under it, and the veil that dims it
        // while the other one speaks (returned).
        private static Image Portrait(Transform root, string name, float left, Color frame, Color fill, string icon, float angle, string label)
        {
            var seal = ScrollUiKit.Fill(root, name, new Vector2(left, 0.315f), new Vector2(left + 0.172f, 0.62f), frame, ScrollUiKit.Rounded(18f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var shadow = seal.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -18f);
            var inner = ScrollUiKit.Fill(seal.transform, "Inner", Vector2.zero, Vector2.one, fill, ScrollUiKit.Rounded(14f));
            inner.rectTransform.offsetMin = new Vector2(6f, 6f);
            inner.rectTransform.offsetMax = new Vector2(-6f, -6f);
            ScrollUiKit.Icon(inner.transform, "Icon", icon, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Color.white);
            var nameLabel = ScrollUiKit.Label(root, name + "Name", label.ToUpperInvariant(), 22, TextAnchor.MiddleCenter, ScrollUiKit.PaperText,
                new Vector2(left - 0.03f, 0.27f), new Vector2(left + 0.202f, 0.305f), 12);
            var nameShadow = nameLabel.gameObject.AddComponent<Shadow>();
            nameShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            nameShadow.effectDistance = new Vector2(2f, -2f);
            return ScrollUiKit.Fill(seal.transform, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.5f), ScrollUiKit.Rounded(18f));
        }

        private void ShowLine(int index)
        {
            _index = index;
            var (speaker, key) = _lines[index];
            var name = speaker == StoryService.Speaker.Keeper ? _keeperName
                : speaker == StoryService.Speaker.Warden ? _wardenName : LocalizationService.T("Story.Scene.Voice", "The voice");
            _speaker.text = name?.ToUpperInvariant();
            _line.text = LocalizationService.T(key);
            var voice = speaker == StoryService.Speaker.Voice;
            _line.fontStyle = voice ? FontStyle.Italic : FontStyle.Normal;
            _line.color = voice ? ScrollUiKit.InkSoft : ScrollUiKit.Ink;
            if (_keeperDim != null) _keeperDim.enabled = speaker != StoryService.Speaker.Keeper;
            if (_wardenDim != null) _wardenDim.enabled = speaker != StoryService.Speaker.Warden;
            for (var i = 0; i < _dots.Count; i++) _dots[i].color = i <= index ? ScrollUiKit.Vermilion : DotOff;
        }

        /// <summary>The next line; after the last one the scene ends.</summary>
        public void Next()
        {
            if (_index + 1 < _lines.Count) ShowLine(_index + 1);
            else Close();
        }

        private void Update()
        {
            if (PadButtons.Pressed(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != _backdrop) es.SetSelectedGameObject(_backdrop);
        }

        /// <summary>Ends the scene and gives the focus back to what held it before.</summary>
        public void Close()
        {
            if (_current != this) return;
            _current = null;
            _closedFrame = Time.frameCount;
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(_returnFocus != null && _returnFocus.activeInHierarchy ? _returnFocus : null);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_current != this) return;
            _current = null;
            _closedFrame = Time.frameCount;
        }
    }
}
