using System;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-045] A new profile's first screen (RM-233, the owner's pick C): a banner over the garden, language and
    /// comfort on one slip, and the two ways on as ema tablets. "Learn the basics" starts Sudoku Basics, "Enter the garden" goes
    /// to the main menu. Both accept the setup, so no second prompt follows.
    /// </summary>
    public sealed class FirstRunSetupModalController : MonoBehaviour
    {
        private static readonly Color SealPaper = new Color(0.96f, 0.93f, 0.85f, 1f);
        private static readonly Color EmaWood = new Color(0.86f, 0.66f, 0.42f, 1f);
        private static readonly Color EmaFocus = new Color(0.91f, 0.62f, 0.56f, 1f);

        // [REQ: REQ-UI-101] SPEC-CONF-032: every shipped catalog is offered (GAP-019).
        private static readonly LanguageOption[] Languages =
        {
            LanguageOption.English, LanguageOption.German, LanguageOption.French,
            LanguageOption.Spanish, LanguageOption.BrazilianPortuguese
        };

        private SaveFileService _saveFile;
        private Action _onCompleted, _onLearnBasics;
        private LanguageOption _selectedLanguage;
        private AccessibilitySettings _accessibility;
        private readonly Button[] _languageButtons = new Button[Languages.Length];
        private readonly Text[] _languageLabels = new Text[Languages.Length];
        private Text _eyebrow, _welcome, _languageHeader, _comfortHeader, _note, _enterTitle, _enterText, _learnTitle, _learnText;
        private Text _colorblindLabel, _contrastLabel, _motionLabel, _readerLabel, _gamepadHint;

        public GameObject Root => gameObject;

        public static FirstRunSetupModalController Show(Canvas canvas, SaveFileService saveFile, Action onCompleted, Action onLearnBasics = null)
        {
            if (canvas == null) return null;
            var root = ScrollUiKit.Fill(canvas.transform, "FirstRunSetupModal", Vector2.zero, Vector2.one, Color.black);
            root.raycastTarget = true;   // nothing under it reacts
            var controller = root.gameObject.AddComponent<FirstRunSetupModalController>();
            controller.Build(root.rectTransform, saveFile ?? new SaveFileService(SaveProfileService.ActiveSlot), onCompleted, onLearnBasics);
            return controller;
        }

        private void Build(RectTransform screen, SaveFileService saveFile, Action onCompleted, Action onLearnBasics)
        {
            _saveFile = saveFile;
            _onCompleted = onCompleted;
            _onLearnBasics = onLearnBasics;
            var envelope = _saveFile.Load();
            var options = envelope.PlayerProfile?.Options ?? new OptionsState();
            _selectedLanguage = options.Language;
            _accessibility = CopyAccessibility(options.Accessibility ?? new AccessibilitySettings());
            LocalizationService.SetLanguage(_selectedLanguage);

            ScrollUiKit.Painting(screen, "FirstRunPainting", "bg_main_menu", new Color(0.66f, 0.62f, 0.62f, 1f));
            var banner = ScrollUiKit.Banner(screen, "FirstRunBanner", new Vector2(0.2396f, 0.7685f), new Vector2(0.7604f, 0.9167f),
                "FirstRunEyebrow", string.Empty, "FirstRunTitle", string.Empty);
            _eyebrow = banner.GetComponentsInChildren<Text>(true).FirstNamed("FirstRunEyebrow");
            _welcome = banner.GetComponentsInChildren<Text>(true).FirstNamed("FirstRunTitle");

            var slip = ScrollUiKit.Fill(screen, "FirstRunSlip", new Vector2(0.1042f, 0.0926f), new Vector2(0.4375f, 0.7037f), ScrollUiKit.Paper);
            slip.raycastTarget = true;
            var shadow = slip.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -10f);
            var t = slip.transform;
            _languageHeader = ScrollUiKit.Label(t, "LanguageHeader", string.Empty, 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.98f), 11);
            for (var i = 0; i < Languages.Length; i++) BuildSeal(t, i);
            _comfortHeader = ScrollUiKit.Label(t, "AccessibilityHeader", string.Empty, 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.04f, 0.47f), new Vector2(0.96f, 0.52f), 11);
            _colorblindLabel = BuildTick(t, "ToggleColorblind", 0, _accessibility.ColorblindMode, v => _accessibility.ColorblindMode = v);
            _contrastLabel = BuildTick(t, "ToggleContrast", 1, _accessibility.HighContrastMode, v => _accessibility.HighContrastMode = v);
            _motionLabel = BuildTick(t, "ToggleMotion", 2, _accessibility.ReduceMotion, v => _accessibility.ReduceMotion = v);
            _readerLabel = BuildTick(t, "ToggleScreenReader", 3, _accessibility.ScreenReaderEnabled, v => _accessibility.ScreenReaderEnabled = v);
            _note = ScrollUiKit.Label(t, "LegalBody", string.Empty, 15, TextAnchor.UpperLeft, ScrollUiKit.InkSoft, new Vector2(0.04f, 0.015f), new Vector2(0.96f, 0.145f), 10);

            var learn = BuildEma(screen, "BtnLearnBasicsFirstRun", new Vector2(0.50f, 0.3889f), new Vector2(0.6875f, 0.6944f), "ui/icon_scroll_stamp", out _learnTitle, out _learnText);
            learn.onClick.AddListener(() => CompleteSetup(_onLearnBasics ?? _onCompleted));
            var enter = BuildEma(screen, "BtnContinueFirstRun", new Vector2(0.7083f, 0.3889f), new Vector2(0.8958f, 0.6944f), "ui/icon_confirm_seal", out _enterTitle, out _enterText);
            enter.onClick.AddListener(() => CompleteSetup(_onCompleted));

            _gamepadHint = ScrollUiKit.Label(screen, "GamepadHint", string.Empty, 18, TextAnchor.MiddleCenter, new Color(0.93f, 0.89f, 0.80f, 1f), new Vector2(0.5f, 0.32f), new Vector2(0.9f, 0.37f), 12);

            RefreshLocalizedText();
            PaintSeals();
            EventSystem.current?.SetSelectedGameObject(learn.gameObject);
        }

        private void BuildSeal(Transform slip, int index)
        {
            const float left = 0.04f, width = 0.30f, gap = 0.01f, top = 0.915f, height = 0.19f, rowGap = 0.02f;
            var col = index % 3;
            var row = index / 3;
            var x = left + col * (width + gap);
            var y = top - row * (height + rowGap);
            var language = Languages[index];
            var seal = ScrollUiKit.Fill(slip, "BtnLanguage" + language, new Vector2(x, y - height), new Vector2(x + width, y), SealPaper, ScrollUiKit.Rounded(10f));
            seal.raycastTarget = true;
            seal.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            var button = seal.gameObject.AddComponent<Button>();
            button.targetGraphic = seal;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SelectLanguage(language));
            _languageLabels[index] = ScrollUiKit.Label(seal.transform, "Label", string.Empty, 24, TextAnchor.MiddleCenter, ScrollUiKit.Ink, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), 13);
            _languageButtons[index] = button;
        }

        private static Text BuildTick(Transform slip, string name, int index, bool value, Action<bool> changed)
        {
            var top = 0.46f - index * 0.078f;
            var row = ScrollUiKit.Fill(slip, name, new Vector2(0.04f, top - 0.072f), new Vector2(0.96f, top), new Color(1f, 1f, 1f, 0f));
            row.raycastTarget = true;
            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = row;
            var colors = ScrollUiKit.StateColors(new Color(1f, 1f, 1f, 0f));
            colors.highlightedColor = colors.selectedColor = new Color(0.91f, 0.77f, 0.42f, 0.38f);
            toggle.colors = colors;
            var box = ScrollUiKit.Fill(row.transform, "Box", new Vector2(0.01f, 0.12f), new Vector2(0.075f, 0.88f), new Color(0.98f, 0.96f, 0.90f, 1f));
            ScrollUiKit.InkFrame(box.transform, ScrollUiKit.Ink, 1.5f);
            toggle.graphic = ScrollUiKit.Fill(box.transform, "Checkmark", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), ScrollUiKit.Vermilion);
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(v => changed(v));
            return ScrollUiKit.Label(row.transform, "Label", string.Empty, 22, TextAnchor.MiddleLeft, ScrollUiKit.Ink, new Vector2(0.11f, 0f), new Vector2(0.99f, 1f), 13);
        }

        private static Button BuildEma(RectTransform screen, string name, Vector2 min, Vector2 max, string icon, out Text title, out Text text)
        {
            var ema = ScrollUiKit.Fill(screen, name, min, max, Color.white, ScrollUiKit.Ema());
            ema.raycastTarget = true;
            var button = ema.gameObject.AddComponent<Button>();
            button.targetGraphic = ema;
            var colors = ScrollUiKit.StateColors(EmaWood);
            colors.highlightedColor = colors.selectedColor = EmaFocus;
            button.colors = colors;
            ScrollUiKit.Icon(ema.transform, "Icon", icon, new Vector2(0.34f, 0.55f), new Vector2(0.66f, 0.84f), Color.white);
            title = ScrollUiKit.Label(ema.transform, "Label", string.Empty, 30, TextAnchor.MiddleCenter, ScrollUiKit.Ink, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.54f), 18);
            text = ScrollUiKit.Label(ema.transform, "Text", string.Empty, 19, TextAnchor.UpperCenter, ScrollUiKit.Ink, new Vector2(0.10f, 0.07f), new Vector2(0.90f, 0.35f), 12);
            return button;
        }

        private static AccessibilitySettings CopyAccessibility(AccessibilitySettings source) => new AccessibilitySettings
        {
            ColorblindMode = source.ColorblindMode,
            HighContrastMode = source.HighContrastMode,
            FontScale = source.FontScale,
            ReduceMotion = source.ReduceMotion,
            AlternativeConstraintSymbols = source.AlternativeConstraintSymbols,
            ScreenReaderEnabled = source.ScreenReaderEnabled
        };

        private void SelectLanguage(LanguageOption language)
        {
            _selectedLanguage = language;
            LocalizationService.SetLanguage(language);
            RefreshLocalizedText();
            PaintSeals();
        }

        private void CompleteSetup(Action next)
        {
            var envelope = _saveFile.Load();
            var options = StartupFlowController.EnsureFirstRunState(envelope) != null ? envelope.PlayerProfile.Options : new OptionsState();
            options.Language = _selectedLanguage;
            options.Accessibility = _accessibility;
            StartupFlowController.MarkFirstRunSetupComplete(envelope);
            envelope.TimestampUtc = DateTime.UtcNow.ToString("o");
            _saveFile.Save(envelope);
            LocalizationService.SetLanguage(_selectedLanguage);
            FindFirstObjectByType<OptionsController>()?.LoadOptions();
            ScrollUiKit.DestroyNow(gameObject);
            next?.Invoke();
        }

        private void Update()
        {
            if (_gamepadHint == null) return;
            if (ControllerGlyphService.Detect() == ControllerPlatform.None) { _gamepadHint.text = string.Empty; return; }
            var (confirm, _, _, _) = ControllerGlyphService.GetFaceLabels();
            _gamepadHint.text = LocalizationService.Format("FirstRun.GamepadHint", "[{0}] Confirm  ·  D-Pad", confirm);
        }

        private void RefreshLocalizedText()
        {
            Set(_eyebrow, LocalizationService.Format("FirstRun.Eyebrow", "A new profile · Profile {0}", SaveProfileService.ActiveSlot + 1).ToUpperInvariant());
            Set(_welcome, LocalizationService.T("FirstRun.Welcome"));
            Set(_languageHeader, LocalizationService.T("FirstRun.LanguageHeader").ToUpperInvariant());
            Set(_comfortHeader, LocalizationService.T("FirstRun.AccessibilityHeader").ToUpperInvariant());
            Set(_note, LocalizationService.T("FirstRun.MoreOptionsHint") + " " + LocalizationService.T("FirstRun.LegalBody"));
            for (var i = 0; i < Languages.Length; i++) Set(_languageLabels[i], LocalizationService.T("FirstRun.Language." + Languages[i]));
            Set(_colorblindLabel, LocalizationService.T("FirstRun.Accessibility.Colorblind"));
            Set(_contrastLabel, LocalizationService.T("FirstRun.Accessibility.HighContrast"));
            Set(_motionLabel, LocalizationService.T("FirstRun.Accessibility.ReduceMotion"));
            Set(_readerLabel, LocalizationService.T("FirstRun.Accessibility.ScreenReader"));
            Set(_learnTitle, LocalizationService.T("FirstRun.LearnBasics"));
            Set(_learnText, LocalizationService.T("FirstRun.LearnBasicsText"));
            Set(_enterTitle, LocalizationService.T("FirstRun.EnterGarden"));
            Set(_enterText, LocalizationService.T("FirstRun.EnterGardenText"));
        }

        private static void Set(Text text, string value)
        {
            if (text != null) text.text = value;
        }

        // The chosen language is a red seal, slightly turned, the others paper.
        private void PaintSeals()
        {
            for (var i = 0; i < Languages.Length; i++)
            {
                var chosen = Languages[i] == _selectedLanguage;
                var seal = (Image)_languageButtons[i].targetGraphic;
                seal.color = chosen ? ScrollUiKit.Vermilion : SealPaper;
                seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, chosen ? -2f : 0f);
                seal.GetComponent<Outline>().effectColor = chosen ? ScrollUiKit.Gold : new Color(0.33f, 0.29f, 0.24f, 0.35f);
                _languageLabels[i].color = chosen ? Color.white : ScrollUiKit.Ink;
            }
        }
    }

    internal static class FirstRunTextLookup
    {
        internal static Text FirstNamed(this Text[] texts, string name) => Array.Find(texts, t => t.name == name);
    }
}
