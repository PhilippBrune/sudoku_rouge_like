using System;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    public sealed class FirstRunSetupModalController : MonoBehaviour
    {
        private SaveFileService _saveFile;
        private Action _onCompleted;
        private LanguageOption _selectedLanguage;
        private AccessibilitySettings _accessibility;

        private Text _title;
        private Text _body;
        private Text _languageHeader;
        private Text _accessibilityHeader;
        private Text _legalHeader;
        private Text _legalBody;
        private Text _moreOptionsHint;
        private Text _englishLabel;
        private Text _germanLabel;
        private Text _continueLabel;
        private Text _colorblindLabel;
        private Text _contrastLabel;
        private Text _motionLabel;
        private Text _readerLabel;
        private Button _englishButton;
        private Button _germanButton;

        public GameObject Root => gameObject;

        public static FirstRunSetupModalController Show(
            Canvas canvas,
            SaveFileService saveFile,
            Action onCompleted)
        {
            if (canvas == null)
                return null;

            var shell = InRunUiFactory.CreateThemedModalShell(
                canvas.transform,
                "FirstRunSetupModal",
                new Vector2(0.16f, 0.12f),
                new Vector2(0.84f, 0.88f),
                "bg_tutorial_setup",
                0.12f);

            var controller = shell.Root.AddComponent<FirstRunSetupModalController>();
            controller.Build(shell.Panel, saveFile ?? new SaveFileService(SaveProfileService.ActiveSlot), onCompleted);
            return controller;
        }

        private void Build(RectTransform panel, SaveFileService saveFile, Action onCompleted)
        {
            _saveFile = saveFile;
            _onCompleted = onCompleted;

            var envelope = _saveFile.Load();
            var options = envelope.PlayerProfile?.Options ?? new OptionsState();
            _selectedLanguage = options.Language;
            _accessibility = CopyAccessibility(options.Accessibility ?? new AccessibilitySettings());
            LocalizationService.SetLanguage(_selectedLanguage);

            _title = CreateText(panel, "Title", 24, TextAnchor.UpperCenter, GamePalette.AccentGold,
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f));
            _body = CreateText(panel, "Body", 13, TextAnchor.UpperCenter, GamePalette.TextPrimary,
                new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.87f));

            _languageHeader = CreateText(panel, "LanguageHeader", 16, TextAnchor.MiddleLeft, GamePalette.AccentGold,
                new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.72f));
            _englishButton = InRunUiFactory.CreateActionButton(panel, "BtnLanguageEnglish",
                new Vector2(0.08f, 0.57f), new Vector2(0.48f, 0.65f), "", UiAction.Confirm);
            _germanButton = InRunUiFactory.CreateActionButton(panel, "BtnLanguageGerman",
                new Vector2(0.52f, 0.57f), new Vector2(0.92f, 0.65f), "", UiAction.Confirm);
            _englishLabel = GetButtonLabel(_englishButton);
            _germanLabel = GetButtonLabel(_germanButton);
            _englishButton.onClick.AddListener(() => SelectLanguage(LanguageOption.English));
            _germanButton.onClick.AddListener(() => SelectLanguage(LanguageOption.German));

            _accessibilityHeader = CreateText(panel, "AccessibilityHeader", 16, TextAnchor.MiddleLeft, GamePalette.AccentGold,
                new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.54f));
            _colorblindLabel = CreateToggle(panel, "ToggleColorblind",
                new Vector2(0.08f, 0.41f), new Vector2(0.48f, 0.47f),
                _accessibility.ColorblindMode, value => _accessibility.ColorblindMode = value);
            _contrastLabel = CreateToggle(panel, "ToggleContrast",
                new Vector2(0.52f, 0.41f), new Vector2(0.92f, 0.47f),
                _accessibility.HighContrastMode, value => _accessibility.HighContrastMode = value);
            _motionLabel = CreateToggle(panel, "ToggleMotion",
                new Vector2(0.08f, 0.34f), new Vector2(0.48f, 0.40f),
                _accessibility.ReduceMotion, value => _accessibility.ReduceMotion = value);
            _readerLabel = CreateToggle(panel, "ToggleScreenReader",
                new Vector2(0.52f, 0.34f), new Vector2(0.92f, 0.40f),
                _accessibility.ScreenReaderEnabled, value => _accessibility.ScreenReaderEnabled = value);
            _moreOptionsHint = CreateText(panel, "MoreOptionsHint", 11, TextAnchor.UpperLeft, GamePalette.TextMuted,
                new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.33f));

            _legalHeader = CreateText(panel, "LegalHeader", 16, TextAnchor.MiddleLeft, GamePalette.AccentGold,
                new Vector2(0.08f, 0.21f), new Vector2(0.92f, 0.27f));
            _legalBody = CreateText(panel, "LegalBody", 11, TextAnchor.UpperLeft, GamePalette.TextPrimary,
                new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.21f));

            var continueButton = InRunUiFactory.CreateActionButton(panel, "BtnContinueFirstRun",
                new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.08f), "", UiAction.Accept);
            _continueLabel = GetButtonLabel(continueButton);
            continueButton.onClick.AddListener(CompleteSetup);

            RefreshLocalizedText();
            UpdateLanguageButtonState();
            EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
        }

        private static AccessibilitySettings CopyAccessibility(AccessibilitySettings source)
        {
            return new AccessibilitySettings
            {
                ColorblindMode = source.ColorblindMode,
                HighContrastMode = source.HighContrastMode,
                FontScale = source.FontScale,
                ReduceMotion = source.ReduceMotion,
                AlternativeConstraintSymbols = source.AlternativeConstraintSymbols,
                ScreenReaderEnabled = source.ScreenReaderEnabled
            };
        }

        private void SelectLanguage(LanguageOption language)
        {
            _selectedLanguage = language;
            LocalizationService.SetLanguage(language);
            RefreshLocalizedText();
            UpdateLanguageButtonState();
        }

        private void CompleteSetup()
        {
            var envelope = _saveFile.Load();
            var options = StartupFlowController.EnsureFirstRunState(envelope) != null
                ? envelope.PlayerProfile.Options
                : new OptionsState();

            options.Language = _selectedLanguage;
            options.Accessibility = _accessibility;
            StartupFlowController.MarkFirstRunSetupComplete(envelope);
            envelope.TimestampUtc = DateTime.UtcNow.ToString("o");

            _saveFile.Save(envelope);
            LocalizationService.SetLanguage(_selectedLanguage);

            var optionsController = FindFirstObjectByType<OptionsController>();
            optionsController?.LoadOptions();

            var completed = _onCompleted;
            Destroy(gameObject);
            completed?.Invoke();
        }

        private void RefreshLocalizedText()
        {
            SetText(_title, "FirstRun.Title");
            SetText(_body, "FirstRun.Body");
            SetText(_languageHeader, "FirstRun.LanguageHeader");
            SetText(_accessibilityHeader, "FirstRun.AccessibilityHeader");
            SetText(_legalHeader, "FirstRun.LegalHeader");
            SetText(_legalBody, "FirstRun.LegalBody");
            SetText(_moreOptionsHint, "FirstRun.MoreOptionsHint");
            SetText(_englishLabel, "FirstRun.Language.English");
            SetText(_germanLabel, "FirstRun.Language.German");
            SetText(_continueLabel, "FirstRun.Continue");
            SetText(_colorblindLabel, "FirstRun.Accessibility.Colorblind");
            SetText(_contrastLabel, "FirstRun.Accessibility.HighContrast");
            SetText(_motionLabel, "FirstRun.Accessibility.ReduceMotion");
            SetText(_readerLabel, "FirstRun.Accessibility.ScreenReader");
        }

        private static void SetText(Text text, string key)
        {
            if (text != null)
                text.text = LocalizationService.T(key);
        }

        private void UpdateLanguageButtonState()
        {
            SetSelected(_englishButton, _selectedLanguage == LanguageOption.English);
            SetSelected(_germanButton, _selectedLanguage == LanguageOption.German);
        }

        private static void SetSelected(Button button, bool selected)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = selected ? GamePalette.AccentGold : InRunUiFactory.BtnColor;
            var label = GetButtonLabel(button);
            if (label != null)
                label.color = selected ? Color.black : GamePalette.AccentGold;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            int size,
            TextAnchor anchor,
            Color color,
            Vector2 min,
            Vector2 max)
        {
            var text = InRunUiFactory.CreateText(parent, name, "", size, anchor, color);
            text.rectTransform.anchorMin = min;
            text.rectTransform.anchorMax = max;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Text CreateToggle(
            Transform parent,
            string name,
            Vector2 min,
            Vector2 max,
            bool initialValue,
            Action<bool> changed)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = row.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.04f, 0.70f);

            var toggle = row.GetComponent<Toggle>();
            toggle.targetGraphic = bg;

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(row.transform, false);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.02f, 0.18f);
            checkRt.anchorMax = new Vector2(0.10f, 0.82f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            var checkImg = check.GetComponent<Image>();
            checkImg.color = GamePalette.AccentGold;
            toggle.graphic = checkImg;

            var label = InRunUiFactory.CreateText(row.transform, "Label", "", 12,
                TextAnchor.MiddleLeft, GamePalette.AccentGold);
            label.rectTransform.anchorMin = new Vector2(0.14f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.98f, 1f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            toggle.SetIsOnWithoutNotify(initialValue);
            toggle.onValueChanged.AddListener(value => changed?.Invoke(value));
            return label;
        }

        private static Text GetButtonLabel(Button button)
        {
            return button == null ? null : button.transform.Find("Label")?.GetComponent<Text>();
        }
    }
}
