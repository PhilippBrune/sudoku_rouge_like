using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-019] Meta Progression as the garden of classes (layout-proposals.md, owner pick 2026-10-20): the eight
    /// classes as ema plaques on two red cords (locked ones dark, with their unlock goal and progress, META-UNLOCK-006),
    /// the chosen class on a hanging scroll (MetaProgressionPanelController.Scroll.cs) and the collection's totals along
    /// the bottom. The D-pad (or the mouse) chooses a class, X prestiges it when it is ready (two presses, META-PRESTIGE-004).
    /// </summary>
    public sealed partial class MetaProgressionPanelController : MonoBehaviour
    {
        // Plaque layout in canvas fractions (1920×1080 reference, the mockup's positions).
        private const int Columns = 4;
        private const float CordLeft = 0.047f, CordRight = 0.536f;
        private const float FirstPlaqueLeft = 0.052f, PlaqueStep = 0.1224f, PlaqueWidth = 0.104f, PlaqueHeight = 0.232f;
        private static readonly float[] CordY = { 0.835f, 0.537f };
        private static readonly float[] PlaqueTop = { 0.802f, 0.504f };

        private static readonly Color LockedWood = new Color(0.42f, 0.34f, 0.27f, 1f);
        private static readonly Color LockedName = new Color(0.82f, 0.77f, 0.64f, 1f);
        private static readonly Color GoalAmber = new Color(0.90f, 0.66f, 0.30f, 1f);
        private static readonly Color LevelInk = new Color(0.42f, 0.23f, 0.08f, 1f);
        private static readonly Color Silhouette = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color SelectedRim = new Color(0.96f, 0.89f, 0.71f, 1f);

        private readonly ClassGardenProgressionService _progression = new ClassGardenProgressionService();
        private readonly Dictionary<ClassId, Button> _plaques = new Dictionary<ClassId, Button>();
        private readonly Dictionary<ClassId, Image> _glows = new Dictionary<ClassId, Image>();
        private readonly Dictionary<ClassId, Image> _rims = new Dictionary<ClassId, Image>();
        private RectTransform _garden;
        private RectTransform _scroll;
        private RectTransform _totals;
        private Button _back;
        private MetaProgressionState _meta = new MetaProgressionState();
        private int _completionPercent;
        private ClassId _selected = ClassId.NumberFreak;

        /// <summary>Prestiges the class and refreshes the panel. Set by MainMenuController (GAP-017).</summary>
        public Action<ClassId> PrestigeRequested;

        public void Configure(RectTransform garden, RectTransform scroll, RectTransform totals, Button back)
        {
            _garden = garden;
            _scroll = scroll;
            _totals = totals;
            _back = back;
        }

        public void Refresh(MetaProgressionState meta, int completionPercent = 0)
        {
            _meta = meta ?? new MetaProgressionState();
            _completionPercent = completionPercent;
            _pendingPrestige = null;
            if (_garden == null) return;
            BuildPlaques();
            BuildTotals();
            Select(_selected, force: true);
        }

        /// <summary>Puts the controller's focus on the chosen class (the panel opens on it, and a prestige keeps it).</summary>
        public void FocusSelected()
        {
            var eventSystem = EventSystem.current;   // no ?. on a UnityEngine.Object (fake null)
            if (eventSystem != null && _plaques.TryGetValue(_selected, out var plaque) && plaque != null)
                eventSystem.SetSelectedGameObject(plaque.gameObject);
        }

        private void Update()
        {
            if (_garden == null || !_garden.gameObject.activeInHierarchy) return;
            // X: prestige the chosen class when it is ready (the same two presses as its button), only while the focus is on
            // that class or its button, so a press on Back or another class never prestiges (review).
            if (!Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton2)) || _prestigeButton == null || !_prestigeButton.gameObject.activeInHierarchy)
                return;
            var eventSystem = EventSystem.current;
            var focus = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var onClass = _plaques.TryGetValue(_selected, out var plaque) && plaque != null && focus == plaque.gameObject;
            if (onClass || focus == _prestigeButton.gameObject) _prestigeButton.onClick.Invoke();
        }

        // ── Plaques ──────────────────────────────────────────────────────────

        private void BuildPlaques()
        {
            ScrollUiKit.DestroyChildrenNow(_garden);
            _plaques.Clear();
            _glows.Clear();
            _rims.Clear();
            for (var row = 0; row < CordY.Length; row++)
                ScrollUiKit.Fill(_garden, $"Cord_{row}", new Vector2(CordLeft, CordY[row] - 0.0014f),
                    new Vector2(CordRight, CordY[row] + 0.0014f), ScrollUiKit.Vermilion);
            var classes = (ClassId[])Enum.GetValues(typeof(ClassId));
            for (var i = 0; i < classes.Length && i < Columns * CordY.Length; i++)
                BuildPlaque(i, classes[i]);
        }

        private void BuildPlaque(int index, ClassId id)
        {
            var row = index / Columns;
            var left = FirstPlaqueLeft + index % Columns * PlaqueStep;
            var top = PlaqueTop[row];
            var holder = ScrollUiKit.Area(_garden, $"Plaque_{id}", new Vector2(left, top - PlaqueHeight), new Vector2(left + PlaqueWidth, top));
            var str = ScrollUiKit.Fill(holder, "String", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), ScrollUiKit.Vermilion);
            str.rectTransform.offsetMin = new Vector2(-1f, -2f);
            str.rectTransform.offsetMax = new Vector2(1f, (CordY[row] - top) * 1080f + 2f);
            var glow = ScrollUiKit.Fill(holder, "Glow", new Vector2(-0.16f, -0.10f), new Vector2(1.16f, 1.08f),
                new Color(1f, 0.85f, 0.50f, 0f), ScrollUiKit.Glow());

            // The chosen plaque's light rim: the same ema shape, a little larger, behind the board.
            var rim = ScrollUiKit.Fill(holder, "Rim", new Vector2(-0.035f, -0.028f), new Vector2(1.035f, 1.035f), SelectedRim,
                ScrollUiKit.Ema());
            rim.enabled = false;
            var progress = ClassUnlockService.GetProgress(_meta, id);
            var unlocked = progress.Unlocked;
            var board = ScrollUiKit.Fill(holder, $"ClassPlaque_{id}", Vector2.zero, Vector2.one, unlocked ? Color.white : LockedWood,
                ScrollUiKit.Ema());
            board.raycastTarget = true;
            var shadow = board.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(4f, -8f);

            if (ClassFrames.IsUnlocked(_meta, id))   // META-XP-008: the Level 40 frame
                ScrollUiKit.Fill(board.transform, "Frame", Vector2.zero, Vector2.one, ClassFrames.Colour(id), ScrollUiKit.EmaFrame());
            var def = ClassCatalog.GetDefinition(id);
            ScrollUiKit.Icon(board.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(id), new Vector2(0.24f, 0.46f),
                new Vector2(0.76f, 0.82f), unlocked ? Color.white : Silhouette);
            ScrollUiKit.Label(board.transform, "Name", T(def.Name, def.Name), 21, TextAnchor.MiddleCenter,
                unlocked ? ScrollUiKit.WoodTileText : LockedName, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.45f), 13);
            var tier = _progression.GetPrestigeTier(_meta, id);
            var line = unlocked
                ? F("MetaProgression.Plaque.Level", "Lv {0}", _progression.GetLevel(_meta, id))
                : GoalText(id, progress);
            ScrollUiKit.Label(board.transform, "Level", line, unlocked ? 17 : 15, TextAnchor.UpperCenter,
                unlocked ? LevelInk : GoalAmber, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.31f), 10);
            if (unlocked && tier > 0)
                ScrollUiKit.Label(board.transform, "Stars", new string('★', tier), 18, TextAnchor.MiddleCenter, ScrollUiKit.Vermilion,
                    new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.14f), 10);

            var button = board.gameObject.AddComponent<Button>();
            button.targetGraphic = board;
            var colors = ScrollUiKit.StateColors(Color.white);
            colors.highlightedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.selectedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.pressedColor = new Color(0.88f, 0.82f, 0.74f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => Select(id));
            board.gameObject.AddComponent<FocusNotifier>().OnFocus = () => Select(id);
            glow.transform.SetAsFirstSibling();
            _plaques[id] = button;
            _glows[id] = glow;
            _rims[id] = rim;
        }

        // A locked class's goal with its progress, as on the plaque and the scroll (META-UNLOCK-006).
        private static string GoalText(ClassId id, ClassUnlockService.UnlockProgress p) => ClassUnlockText.Goal(id, p);

        private void Select(ClassId id, bool force = false)
        {
            CancelPendingPrestige();
            if (!force && id == _selected && _scroll != null && _scroll.Find("ClassPage") != null) return;
            _selected = id;
            foreach (var pair in _plaques)
            {
                var chosen = pair.Key == id;
                if (_glows.TryGetValue(pair.Key, out var glow) && glow != null)
                    glow.color = new Color(1f, 0.85f, 0.50f, chosen ? 0.55f : 0f);
                if (_rims.TryGetValue(pair.Key, out var rim) && rim != null) rim.enabled = chosen;
            }
            ShowClass(id);
        }

        // ── Totals ───────────────────────────────────────────────────────────

        private void BuildTotals()
        {
            if (_totals == null) return;
            ScrollUiKit.DestroyChildrenNow(_totals);
            var classesUnlocked = 0;
            foreach (ClassId id in Enum.GetValues(typeof(ClassId)))
                if (ClassUnlockService.GetProgress(_meta, id).Unlocked) classesUnlocked++;
            var relics = CodexProgress.Relics(_meta);
            var totals = new[]
            {
                (F("MetaProgression.Total.Count", "{0} / {1}", classesUnlocked, Enum.GetValues(typeof(ClassId)).Length),
                    T("MetaProgression.Total.Classes", "classes")),
                (F("MetaProgression.Total.Count", "{0} / {1}", relics.Found, relics.Total), T("MetaProgression.Total.Relics", "relics")),
                (F("MetaProgression.Total.Percent", "{0} %", CodexProgress.Percent(_meta)), T("MetaProgression.Total.Codex", "codex")),
                (F("MetaProgression.Total.Harmony", "H{0}", Mathf.Max(0, _meta.MaxUnlockedHarmonyLevel)),
                    T("MetaProgression.Total.HighestHarmony", "highest Harmony")),
                (F("MetaProgression.Total.Percent", "{0} %", Mathf.Clamp(_completionPercent, 0, 100)),
                    T("MetaProgression.Total.Completion", "completion"))
            };
            const float gap = 0.03f;
            var width = (1f - gap * (totals.Length - 1)) / totals.Length;
            for (var i = 0; i < totals.Length; i++)
            {
                var x = i * (width + gap);
                var box = ScrollUiKit.Fill(_totals, $"Total_{i}", new Vector2(x, 0f), new Vector2(x + width, 1f),
                    new Color(0.08f, 0.06f, 0.04f, 0.72f), ScrollUiKit.Rounded(12f));
                ScrollUiKit.InkFrame(box.transform, new Color(0.79f, 0.64f, 0.31f, 0.9f), 1.5f);
                ScrollUiKit.Label(box.transform, "Value", totals[i].Item1, 30, TextAnchor.MiddleCenter, ScrollUiKit.Gold,
                    new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.95f), 16);
                ScrollUiKit.Label(box.transform, "Caption", totals[i].Item2, 17, TextAnchor.MiddleCenter, ScrollUiKit.PaperText,
                    new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.42f), 11);
            }
        }

        // ── Navigation ───────────────────────────────────────────────────────

        // Explicit D-pad links: the plaques as a 4×2 grid, Back under them, the prestige button right of the plaques.
        private void WireNavigation()
        {
            var classes = (ClassId[])Enum.GetValues(typeof(ClassId));
            Button At(int i) => i >= 0 && i < classes.Length && _plaques.TryGetValue(classes[i], out var b) ? b : null;
            // activeSelf: the panel is built while still hidden (OpenMetaProgression refreshes before showing it).
            var prestige = _prestigeButton != null && _prestigeButton.gameObject.activeSelf ? _prestigeButton : null;
            _plaques.TryGetValue(_selected, out var selectedPlaque);
            for (var i = 0; i < classes.Length; i++)
            {
                var plaque = At(i);
                if (plaque == null) continue;
                var column = i % Columns;
                var lastRow = i + Columns >= classes.Length;
                plaque.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i >= Columns ? At(i - Columns) : null,
                    selectOnDown = lastRow ? _back : At(i + Columns),
                    selectOnLeft = column > 0 ? At(i - 1) : null,
                    selectOnRight = column < Columns - 1 ? At(i + 1) : prestige
                };
            }
            if (prestige != null)
                prestige.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit, selectOnLeft = selectedPlaque, selectOnDown = _back, selectOnUp = null, selectOnRight = null
                };
            if (_back != null)
                _back.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = selectedPlaque != null ? selectedPlaque : At(Columns),
                    selectOnRight = prestige
                };
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, string fallback, params object[] args) =>
            LocalizationService.Format(key, fallback, args);
    }
}
