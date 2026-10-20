using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-012] Custom Puzzle as a rule book (layout-proposals.md, "Custom Puzzle · A"): the settings on a
    // paper slip, the 71 rules in a book with one tab per family (LB/RB switch tabs), the chosen rules as chips in the
    // tray under the book with the Start button. A rule's text shows on hover or focus; a rule that does not fit says
    // why. Up to five rules (TutorialModeService.CustomModifierLimit).
    public sealed partial class CustomPuzzleScreen : MonoBehaviour
    {
        private const int MaxStars = 7, MinSize = 5, MaxSize = 9;
        private static readonly Color Rim = new Color(0.86f, 0.71f, 0.38f, 1f);
        private static readonly Color SegmentOn = new Color(0.80f, 0.60f, 0.25f, 1f);
        private static readonly Color SegmentOff = new Color(0.97f, 0.94f, 0.86f, 1f);

        private TutorialMenuController _setup;
        private Text _sizeValue, _starsValue, _rulesCount, _rulesMessage;
        private readonly List<Image> _regionSegments = new List<Image>();
        private readonly List<Image> _resourceSegments = new List<Image>();
        private readonly List<GameObject> _chips = new List<GameObject>();
        private RectTransform _tray;
        private Button _randomButton;

        /// <summary>Builds the slip, the book and the tray on <paramref name="screen"/> (a full-screen panel).</summary>
        public void Build(RectTransform screen, TutorialMenuController setup, Button startButton)
        {
            if (_setup != null) _setup.SetupChanged -= Refresh;   // built once; a second build must not refresh twice
            _regionSegments.Clear();
            _resourceSegments.Clear();
            _tabs.Clear();
            _setup = setup;
            BuildSettingsSlip(screen);
            BuildBook(screen);
            BuildTray(screen, startButton);
            _setup.SetupChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_setup != null) _setup.SetupChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_setup == null) return;
            _sizeValue.text = $"{_setup.GetBoardSize()}×{_setup.GetBoardSize()}";
            _starsValue.text = new string('★', Mathf.Clamp(_setup.GetStars(), 1, MaxStars));
            MarkSegments(_regionSegments, _setup.GetRegionIndex());
            MarkSegments(_resourceSegments, _setup.IsClassBased ? 1 : 0);
            RefreshTiles();
            RefreshTray();
        }

        // ── settings slip ──

        private void BuildSettingsSlip(RectTransform screen)
        {
            var slip = ScrollUiKit.PaperTag(screen, "CustomSettings", new Vector2(0.0458f, 0.4333f), new Vector2(0.2354f, 0.8176f), 1f);
            var t = slip.transform;
            Header(t, "BoardHeader", "CustomPuzzle.Book.Board", "Board", 0.904f);
            _sizeValue = Stepper(t, "Size", T("CustomPuzzle.Book.Size", "Size"), 0.834f,
                d => _setup.SetBoardSizeIndex(Mathf.Clamp(_setup.GetBoardSize() + d, MinSize, MaxSize) - MinSize));
            _starsValue = Stepper(t, "Stars", T("Difficulty", "Difficulty"), 0.696f,
                d => _setup.SetStarsIndex(Mathf.Clamp(_setup.GetStars() + d, 1, MaxStars) - 1));
            Header(t, "RegionHeader", "CustomPuzzle.Book.Regions", "Regions", 0.578f);
            Segments(t, "RegionSeg", 0.501f, _regionSegments, _setup.SetRegionIndex,
                T("CustomPuzzle.Book.Region.Boxes", "Boxes"), T("CustomPuzzle.Book.Region.Turned", "Turned"),
                T("CustomPuzzle.Book.Region.Irregular", "Irregular"));
            Header(t, "ResourceHeader", "CustomPuzzle.Book.Resources", "Resources", 0.376f);
            Segments(t, "ResourceSeg", 0.299f, _resourceSegments, _setup.SetResourceModeIndex,
                T("CustomPuzzle.Resources.Free", "Free"), T("CustomPuzzle.Resources.ClassBased", "Class-based"));

            var classRow = ScrollUiKit.Area(t, "ClassRow", new Vector2(0.06f, 0.11f), new Vector2(0.95f, 0.20f));
            ScrollUiKit.Label(classRow, "ClassLabel", T("CustomPuzzle.Book.Class", "Class"), 21, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0f, 0f), new Vector2(0.30f, 1f), 13);
            RoundButton(classRow, "ClassPrev", new Vector2(0.39f, 0.5f), "‹", () => _setup.StepClass(-1));
            var classValue = ScrollUiKit.Label(classRow, "ClassValue", string.Empty, 19, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.45f, 0f), new Vector2(0.86f, 1f), 12);
            RoundButton(classRow, "ClassNext", new Vector2(0.92f, 0.5f), "›", () => _setup.StepClass(1));
            _setup.ConfigureArrowClassRow(classRow);
            _setup.SetClassArrowLabel(classValue);
        }

        private static void Header(Transform parent, string name, string key, string fallback, float centre)
        {
            ScrollUiKit.Label(parent, name, T(key, fallback).ToUpperInvariant(), 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.08f, centre - 0.025f), new Vector2(0.92f, centre + 0.025f), 10);
        }

        // "Label  [−] value [+]"
        private static Text Stepper(Transform parent, string id, string label, float centre, Action<int> step)
        {
            ScrollUiKit.Label(parent, id + "Label", label, 21, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.08f, centre - 0.04f), new Vector2(0.45f, centre + 0.04f), 13);
            RoundButton(parent, id + "Dec", new Vector2(0.51f, centre), "−", () => step(-1));
            var value = ScrollUiKit.Label(parent, id + "Value", string.Empty, 22, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.56f, centre - 0.04f), new Vector2(0.82f, centre + 0.04f), 12);
            RoundButton(parent, id + "Inc", new Vector2(0.87f, centre), "+", () => step(1));
            return value;
        }

        private static Button RoundButton(Transform parent, string name, Vector2 centre, string glyph, Action onClick)
        {
            var disc = ScrollUiKit.Spot(parent, name, centre, new Vector2(34f, 34f), Color.white, ScrollUiKit.Disc());
            disc.raycastTarget = true;
            var button = disc.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            button.colors = ScrollUiKit.StateColors(new Color(0.80f, 0.60f, 0.30f, 1f));
            ScrollUiKit.Label(disc.transform, "Glyph", glyph, 20, TextAnchor.MiddleCenter, ScrollUiKit.WoodDark, Vector2.zero, Vector2.one);
            button.onClick.AddListener(() => onClick());
            return button;
        }

        // A rounded rim with one segment per option; the chosen one is filled gold.
        private static void Segments(Transform parent, string id, float centre, List<Image> fills, Action<int> choose,
            params string[] labels)
        {
            var rim = ScrollUiKit.Fill(parent, id, new Vector2(0.08f, centre - 0.046f), new Vector2(0.92f, centre + 0.046f),
                new Color(0.75f, 0.55f, 0.25f, 1f), ScrollUiKit.Rounded(18f));
            var width = 1f / labels.Length;
            for (var i = 0; i < labels.Length; i++)
            {
                var fill = ScrollUiKit.Fill(rim.transform, $"{id}_{i}", new Vector2(i * width, 0f), new Vector2((i + 1) * width, 1f),
                    SegmentOff, ScrollUiKit.Rounded(16f));
                fill.rectTransform.offsetMin = new Vector2(i == 0 ? 2f : 1f, 2f);
                fill.rectTransform.offsetMax = new Vector2(i == labels.Length - 1 ? -2f : -1f, -2f);
                fill.raycastTarget = true;
                var button = fill.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.transition = Selectable.Transition.None;
                ScrollUiKit.Label(fill.transform, "Label", labels[i], 18, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                    new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 11);
                var index = i;
                button.onClick.AddListener(() => choose(index));
                fills.Add(fill);
            }
        }

        private static void MarkSegments(List<Image> fills, int chosen)
        {
            for (var i = 0; i < fills.Count; i++)
                fills[i].color = i == chosen ? SegmentOn : SegmentOff;
        }

        // ── tray ──

        private void BuildTray(RectTransform screen, Button startButton)
        {
            var tray = ScrollUiKit.Fill(screen, "RulesTray", new Vector2(0.2708f, 0.0944f), new Vector2(0.9469f, 0.2315f),
                new Color(0.10f, 0.08f, 0.06f, 0.82f), ScrollUiKit.Rounded(14f));
            var rim = tray.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(Rim.r, Rim.g, Rim.b, 0.35f);
            rim.effectDistance = new Vector2(1.5f, -1.5f);
            _tray = tray.rectTransform;
            ScrollUiKit.Label(_tray, "RulesTitle", T("CustomPuzzle.Book.YourRules", "Your rules"), 24, TextAnchor.MiddleLeft,
                ScrollUiKit.PaperText, new Vector2(0.022f, 0.62f), new Vector2(0.14f, 0.86f), 14);
            _rulesCount = ScrollUiKit.Label(_tray, "RulesCount", string.Empty, 17, TextAnchor.MiddleLeft, Rim,
                new Vector2(0.14f, 0.62f), new Vector2(0.24f, 0.86f), 11);
            _rulesMessage = ScrollUiKit.Label(_tray, "RulesMessage", string.Empty, 16, TextAnchor.MiddleLeft,
                new Color(0.96f, 0.62f, 0.52f, 1f), new Vector2(0.24f, 0.62f), new Vector2(0.585f, 0.86f), 10);

            // Random: fills the tray with as many random rules as the count says (0.2.0 playtest).
            _randomButton = ScrollUiKit.PillButton(_tray, "BtnRandomRules", new Vector2(0.597f, 0.63f), new Vector2(0.700f, 0.88f),
                string.Empty, 18, new Color(0.45f, 0.33f, 0.14f, 1f), ScrollUiKit.PaperText, 18f);
            _randomButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            _randomButton.onClick.AddListener(() => _setup.RollRandomRules());
            RoundButton(_tray, "RandomDec", new Vector2(0.722f, 0.755f), "−", () => _setup.StepRandomRuleCount(-1));
            RoundButton(_tray, "RandomInc", new Vector2(0.760f, 0.755f), "+", () => _setup.StepRandomRuleCount(1));

            var start = (RectTransform)startButton.transform;
            start.SetParent(_tray, false);
            start.anchorMin = new Vector2(0.788f, 0.30f);
            start.anchorMax = new Vector2(0.975f, 0.72f);
            start.offsetMin = start.offsetMax = Vector2.zero;
        }

        private void RefreshTray()
        {
            foreach (var chip in _chips) ScrollUiKit.DestroyNow(chip);
            _chips.Clear();
            var chosen = _setup.SelectedModifiers;
            for (var i = 0; i < Tutorial.TutorialModeService.CustomModifierLimit; i++)
                _chips.Add(i < chosen.Count ? BuildChip(i, chosen[i]) : BuildEmptyChip(i));
            _rulesCount.text = F("CustomPuzzle.Book.Count", "{0} of {1}", chosen.Count, Tutorial.TutorialModeService.CustomModifierLimit);
            _rulesMessage.text = _setup.ValidationMessage ?? string.Empty;
            var randomLabel = _randomButton != null ? _randomButton.transform.Find("Label")?.GetComponent<Text>() : null;
            if (randomLabel != null) randomLabel.text = F("CustomPuzzle.Book.Random", "Random: {0}", _setup.RandomRuleCount);
        }

        private static Vector2 ChipMin(int i) => new Vector2(0.022f + i * 0.153f, 0.20f);
        private static Vector2 ChipMax(int i) => new Vector2(0.022f + i * 0.153f + 0.146f, 0.58f);

        // A chosen rule: icon and name on a paper pill; clicking it takes the rule out again.
        private GameObject BuildChip(int index, BossModifierId id)
        {
            var chip = ScrollUiKit.Fill(_tray, $"RuleChip_{index}", ChipMin(index), ChipMax(index), new Color(0.95f, 0.91f, 0.82f, 1f),
                ScrollUiKit.Rounded(24f));
            chip.raycastTarget = true;
            var button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chip;
            button.colors = ScrollUiKit.StateColors(Color.white);
            ScrollUiKit.Icon(chip.transform, "Icon", IconPath(id), new Vector2(0.04f, 0.12f), new Vector2(0.24f, 0.88f), Color.white);
            // One line: the rect is too low for two, so a long name shrinks instead of wrapping.
            ScrollUiKit.Label(chip.transform, "Name", BossService.GetModifierName(id), 18, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.26f, 0.18f), new Vector2(0.96f, 0.82f), 11);
            button.onClick.AddListener(() => _setup.ToggleModifier(id));
            AddHover(chip.gameObject, id);
            return chip.gameObject;
        }

        private GameObject BuildEmptyChip(int index)
        {
            var rim = ScrollUiKit.Fill(_tray, $"RuleChip_{index}", ChipMin(index), ChipMax(index), new Color(Rim.r, Rim.g, Rim.b, 0.45f),
                ScrollUiKit.Rounded(24f));
            var inner = ScrollUiKit.Fill(rim.transform, "Inner", Vector2.zero, Vector2.one, new Color(0.10f, 0.08f, 0.06f, 1f),
                ScrollUiKit.Rounded(22f));
            inner.rectTransform.offsetMin = new Vector2(2f, 2f);
            inner.rectTransform.offsetMax = new Vector2(-2f, -2f);
            ScrollUiKit.Label(rim.transform, "Empty", T("CustomPuzzle.Book.Empty", "empty"), 18, TextAnchor.MiddleCenter,
                new Color(Rim.r, Rim.g, Rim.b, 0.7f), Vector2.zero, Vector2.one, 11);
            return rim.gameObject;
        }

        internal static string IconPath(BossModifierId id) => BossService.GetIconFolder(id) + "/icon_" + BossService.GetIconName(id);

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, string fallback, params object[] args) => LocalizationService.Format(key, fallback, args);
    }
}
