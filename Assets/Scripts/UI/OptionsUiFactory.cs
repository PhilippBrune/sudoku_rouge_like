using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Builds the Options controls. The main-menu Options (MainMenuBlueprintBuilder) and the in-run Options
    /// (InRunUiBlueprintBuilder) both call <see cref="BuildStandardTabs"/>, so they have the same tabs, controls, look and
    /// D-pad navigation (GAP-029, FUTURE-015). The widget builders also serve the main menu's other panels.
    /// </summary>
    // [REQ: UI-OPT-001]
    public static class OptionsUiFactory
    {
        public sealed class Setup
        {
            /// <summary>Prefix of every object name: "" in the main menu, "IG" in a run.</summary>
            public string Prefix = "";

            /// <summary>Runs after a new language is applied. Null: the Display tab shows a hint instead of the
            /// language row (the in-run UI cannot rebuild itself in the middle of a puzzle).</summary>
            public Action OnLanguageApplied;

            /// <summary>Extra reaction to Highlight Errors, e.g. repainting the board during a run.</summary>
            public Action<bool> OnHighlightConflictsChanged;
        }

        public readonly struct ArrowRow
        {
            public ArrowRow(Button dec, Button inc, Text value, Action<float> refresh)
            {
                Dec = dec;
                Inc = inc;
                Value = value;
                Refresh = refresh;
            }

            public Button Dec { get; }
            public Button Inc { get; }
            public Text Value { get; }
            public Action<float> Refresh { get; }
        }

        private const float DropdownItemHeight = 32f;

        private static string T(string key) => LocalizationService.T(key);

        private static Selectable[] Row(params Selectable[] items) => items;

        // ------------------------------------------------------------------------
        // Options tabs: Audio, Display, Gameplay
        // ------------------------------------------------------------------------

        /// <summary>
        /// Adds the Audio, Display and Gameplay tabs to <paramref name="tabs"/> with every control, reset button and
        /// navigation row. Returns the refresh that re-reads all controls from <paramref name="optCtrl"/>; register it
        /// with <c>OptionsController.OnOptionsChanged</c> (resets and profile switches raise it).
        /// </summary>
        public static Action BuildStandardTabs(OptionsTabSet tabs, OptionsController optCtrl, Setup setup)
        {
            var p = setup?.Prefix ?? "";
            var opts = optCtrl.GetOptions();
            var audioIdx = tabs.Count;
            var audioSec   = tabs.Add(p + "TabAudio",   p + "AudioSection",   T("Audio"));
            var displaySec = tabs.Add(p + "TabDisplay", p + "DisplaySection", T("Display"));
            var gameSec    = tabs.Add(p + "TabGame",    p + "GameSection",    T("Gameplay"));

            // --------------------------------------------------------------------
            // AUDIO SECTION
            // --------------------------------------------------------------------
            SectionHeader(p + "AudioHdr", audioSec, T("Audio"));
            var btnResetAudio = ResetButton(p + "BtnResetAudio", audioSec, optCtrl, optCtrl.ResetAudioToDefaults);

            var master = BuildVolumeArrowRow(p + "MasterVolume", T("Master Volume"), audioSec,
                new Vector2(0.05f, 0.70f), new Vector2(0.45f, 0.88f),
                opts.Audio.MasterVolume, optCtrl.SetMasterVolume);
            var music = BuildVolumeArrowRow(p + "MusicVolume", T("Music Volume"), audioSec,
                new Vector2(0.52f, 0.70f), new Vector2(0.92f, 0.88f),
                opts.Audio.MusicVolume, optCtrl.SetMusicVolume);
            var sfx = BuildVolumeArrowRow(p + "SfxVolume", T("SFX Volume"), audioSec,
                new Vector2(0.05f, 0.52f), new Vector2(0.45f, 0.70f),
                opts.Audio.SfxVolume, optCtrl.SetSfxVolume);
            var ui = BuildVolumeArrowRow(p + "UiVolume", T("UI Volume"), audioSec,
                new Vector2(0.52f, 0.52f), new Vector2(0.92f, 0.70f),
                opts.Audio.UiVolume, optCtrl.SetUiVolume);

            var tMute = BuildOptionToggle(audioSec, p + "MuteUnfocused", T("Mute when unfocused"),
                new Vector2(0.05f, 0.37f), new Vector2(0.45f, 0.50f),
                opts.Audio.MuteWhenUnfocused, optCtrl.SetMuteWhenUnfocused);
            var tMuteAll = BuildOptionToggle(audioSec, p + "MuteAll", T("Mute all"),
                new Vector2(0.52f, 0.37f), new Vector2(0.92f, 0.50f),
                opts.Audio.MuteAll, optCtrl.SetMuteAll);

            var musicStyleLabel = BuildText(p + "MenuMusicStyleLabel", audioSec,
                T("Audio.MenuMusicStyle.Label"), 14, TextAnchor.MiddleLeft);
            SetRect(musicStyleLabel.rectTransform, new Vector2(0.05f, 0.25f), new Vector2(0.32f, 0.35f));
            var musicStyleDrop = BuildDropdown(p + "MenuMusicStyleDropdown", audioSec);
            SetRect(musicStyleDrop.GetComponent<RectTransform>(), new Vector2(0.33f, 0.23f), new Vector2(0.92f, 0.35f));
            musicStyleDrop.ClearOptions();
            var styleOptions = new List<string>();
            foreach (var key in AudioAssetService.MenuMusicStyleLocalizationKeys)
                styleOptions.Add(T(key));
            musicStyleDrop.AddOptions(styleOptions);
            FitDropdownToItems(musicStyleDrop);
            musicStyleDrop.SetValueWithoutNotify(
                AudioAssetService.NormalizeMenuMusicStyleIndex(opts.Audio.MenuMusicStyleIndex));
            musicStyleDrop.onValueChanged.RemoveAllListeners();
            musicStyleDrop.onValueChanged.AddListener(optCtrl.SetMusicStyleIndex);

            tabs.SetRows(audioIdx,
                Row(btnResetAudio),
                Row(master.Dec, master.Inc, music.Dec, music.Inc),
                Row(sfx.Dec, sfx.Inc, ui.Dec, ui.Inc),
                Row(tMute, tMuteAll),
                Row(musicStyleDrop));

            // --------------------------------------------------------------------
            // DISPLAY SECTION
            // --------------------------------------------------------------------
            SectionHeader(p + "DisplayHdr", displaySec, T("Display"));
            var btnResetDisplay = ResetButton(p + "BtnResetDisplay", displaySec, optCtrl, optCtrl.ResetGraphicsToDefaults);

            var tFullscreen = BuildOptionToggle(displaySec, p + "Fullscreen", T("Fullscreen"),
                new Vector2(0.05f, 0.74f), new Vector2(0.45f, 0.87f),
                opts.Graphics.Fullscreen, optCtrl.SetFullscreen);
            var tVSync = BuildOptionToggle(displaySec, p + "VSync", T("VSync"),
                new Vector2(0.52f, 0.74f), new Vector2(0.92f, 0.87f),
                opts.Graphics.VSync, optCtrl.SetVSync);
            var tScreenShake = BuildOptionToggle(displaySec, p + "ScreenShake", T("Screen Shake"),
                new Vector2(0.05f, 0.59f), new Vector2(0.45f, 0.72f),
                opts.Graphics.ScreenShake, optCtrl.SetScreenShake);
            var particle = BuildVolumeArrowRow(p + "ParticleIntensity", T("Particles"), displaySec,
                new Vector2(0.52f, 0.59f), new Vector2(0.92f, 0.72f),
                opts.Graphics.ParticleIntensity, optCtrl.SetParticleIntensity);
            var brightness = BuildVolumeArrowRow(p + "Brightness", T("Brightness"), displaySec,
                new Vector2(0.05f, 0.45f), new Vector2(0.45f, 0.58f),
                opts.Graphics.Brightness, optCtrl.SetBrightness);
            var contrast = BuildVolumeArrowRow(p + "Contrast", T("Contrast"), displaySec,
                new Vector2(0.52f, 0.45f), new Vector2(0.92f, 0.58f),
                opts.Graphics.Contrast, optCtrl.SetContrast);

            // Resolution row
            var resolutions = OptionsController.GetSupportedResolutions();
            var resLabel = BuildText(p + "ResLabel", displaySec, T("Resolution"), 14, TextAnchor.MiddleLeft);
            SetRect(resLabel.rectTransform, new Vector2(0.05f, 0.34f), new Vector2(0.22f, 0.44f));
            var resDrop = BuildDropdown(p + "ResolutionDropdown", displaySec);
            SetRect(resDrop.GetComponent<RectTransform>(), new Vector2(0.23f, 0.32f), new Vector2(0.96f, 0.44f));
            resDrop.ClearOptions();
            var resOpts = new List<string>();
            foreach (var r in resolutions)
                resOpts.Add($"{r.width} x {r.height}");
            resDrop.AddOptions(resOpts);
            resDrop.SetValueWithoutNotify(ResolutionIndex(resolutions, opts.Graphics));
            resDrop.onValueChanged.RemoveAllListeners();
            resDrop.onValueChanged.AddListener(idx =>
            {
                var r = resolutions[idx];
                optCtrl.SetResolution(r.width, r.height);
            });

            // Display mode row
            var displayModeLabel = BuildText(p + "DisplayModeLabel", displaySec, T("DisplayMode"), 14, TextAnchor.MiddleLeft);
            SetRect(displayModeLabel.rectTransform, new Vector2(0.05f, 0.22f), new Vector2(0.22f, 0.32f));
            var displayModeDrop = BuildDropdown(p + "DisplayModeDropdown", displaySec);
            SetRect(displayModeDrop.GetComponent<RectTransform>(), new Vector2(0.23f, 0.20f), new Vector2(0.96f, 0.32f));
            displayModeDrop.ClearOptions();
            displayModeDrop.AddOptions(new List<string>
            {
                T("DisplayMode.Fullscreen"),
                T("DisplayMode.Borderless"),
                T("DisplayMode.Windowed"),
                T("DisplayMode.FramelessWindow") // [REQ: DISPLAY-MODE-001] order = DisplayModeOption values
            });
            FitDropdownToItems(displayModeDrop);
            displayModeDrop.SetValueWithoutNotify((int)OptionsController.NormalizeDisplayMode(opts.Graphics.DisplayMode));
            displayModeDrop.onValueChanged.RemoveAllListeners();
            displayModeDrop.onValueChanged.AddListener(idx => optCtrl.SetDisplayMode((DisplayModeOption)idx));

            // Language row: only where the UI can rebuild itself (main menu); a run shows where to change it.
            Dropdown langDrop = null;
            GameObject langConfirmRow = null;
            Button btnLangApply = null;
            Button btnLangCancel = null;
            if (setup?.OnLanguageApplied != null)
            {
                var langLabel = BuildText(p + "LangLabel", displaySec, T("Language"), 14, TextAnchor.MiddleLeft);
                SetRect(langLabel.rectTransform, new Vector2(0.05f, 0.10f), new Vector2(0.22f, 0.20f));
                langDrop = BuildDropdown(p + "LanguageDropdown", displaySec);
                SetRect(langDrop.GetComponent<RectTransform>(), new Vector2(0.23f, 0.08f), new Vector2(0.65f, 0.20f));
                langDrop.ClearOptions();
                langDrop.AddOptions(new List<string> { "English", "Deutsch", "Français", "Español", "Português (BR)" });
                FitDropdownToItems(langDrop);
                langDrop.SetValueWithoutNotify((int)opts.Language);

                // Language confirmation row (initially hidden)
                var langConfirmRt = EnsureRect(p + "LangConfirmRow", displaySec,
                    new Vector2(0.05f, 0.00f), new Vector2(0.96f, 0.08f), Vector2.zero, Vector2.zero);
                langConfirmRow = langConfirmRt.gameObject;
                var langWarnText = BuildText(p + "LangWarnText", langConfirmRt,
                    T("Options.LanguageConfirm.Warning"), 12, TextAnchor.MiddleLeft);
                SetRect(langWarnText.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f));
                langWarnText.color = GamePalette.AccentGold;
                btnLangApply = BuildButton(p + "BtnLangApply", langConfirmRt, T("Apply"), 12);
                SetRect(btnLangApply.GetComponent<RectTransform>(), new Vector2(0.57f, 0.05f), new Vector2(0.75f, 0.95f));
                btnLangCancel = BuildButton(p + "BtnLangCancel", langConfirmRt, T("Cancel"), 12);
                SetRect(btnLangCancel.GetComponent<RectTransform>(), new Vector2(0.77f, 0.05f), new Vector2(0.96f, 0.95f));
                langConfirmRow.SetActive(false);

                var pendingLangIdx = (int)opts.Language;
                var drop = langDrop;
                var confirmRow = langConfirmRow;
                langDrop.onValueChanged.RemoveAllListeners();
                langDrop.onValueChanged.AddListener(idx =>
                {
                    pendingLangIdx = idx;
                    confirmRow.SetActive(idx != (int)optCtrl.GetOptions().Language);
                });
                btnLangApply.onClick.RemoveAllListeners();
                btnLangApply.onClick.AddListener(() =>
                {
                    optCtrl.SetLanguage((LanguageOption)pendingLangIdx);
                    confirmRow.SetActive(false);
                    setup.OnLanguageApplied();
                });
                btnLangCancel.onClick.RemoveAllListeners();
                btnLangCancel.onClick.AddListener(() =>
                {
                    drop.SetValueWithoutNotify((int)optCtrl.GetOptions().Language);
                    confirmRow.SetActive(false);
                });
            }
            else
            {
                var hint = BuildText(p + "LangHint", displaySec, T("Options.LanguageInMainMenu"), 13, TextAnchor.MiddleLeft);
                SetRect(hint.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.96f, 0.20f));
                hint.color = GamePalette.TextMuted;
            }

            tabs.SetRows(audioIdx + 1,
                Row(btnResetDisplay),
                Row(tFullscreen, tVSync),
                Row(tScreenShake, particle.Dec, particle.Inc),
                Row(brightness.Dec, brightness.Inc, contrast.Dec, contrast.Inc),
                Row(resDrop),
                Row(displayModeDrop),
                Row(langDrop),
                Row(btnLangApply, btnLangCancel));

            // --------------------------------------------------------------------
            // GAMEPLAY SECTION
            // --------------------------------------------------------------------
            SectionHeader(p + "GameHdr", gameSec, T("Gameplay"));
            var btnResetGame = ResetButton(p + "BtnResetGameplay", gameSec, optCtrl, optCtrl.ResetGameplayToDefaults);

            var tHighlight = BuildOptionToggle(gameSec, p + "HighlightErrors", T("Highlight Errors"),
                new Vector2(0.05f, 0.74f), new Vector2(0.45f, 0.87f),
                opts.Gameplay.HighlightConflicts, v =>
                {
                    optCtrl.SetHighlightConflicts(v);
                    setup?.OnHighlightConflictsChanged?.Invoke(v);
                });
            var tConfirmWrong = BuildOptionToggle(gameSec, p + "ConfirmWrong", T("Confirm Wrong Placement"),
                new Vector2(0.52f, 0.74f), new Vector2(0.96f, 0.87f),
                opts.Gameplay.ConfirmBeforeWrongPlacement, optCtrl.SetConfirmBeforeWrongPlacement);
            var tAutoPencil = BuildOptionToggle(gameSec, p + "AutoPencil", T("Auto Pencil Cleanup"),
                new Vector2(0.05f, 0.58f), new Vector2(0.45f, 0.71f),
                opts.Gameplay.AutoPencilCleanup, optCtrl.SetAutoPencilCleanup);
            var tCandidates = BuildOptionToggle(gameSec, p + "CandidateCount", T("Show Candidate Count"),
                new Vector2(0.52f, 0.58f), new Vector2(0.96f, 0.71f),
                opts.Gameplay.ShowCandidateCount, optCtrl.SetShowCandidateCount);
            var tCursorSnap = BuildOptionToggle(gameSec, p + "CursorSnap", T("Cursor Snap"),
                new Vector2(0.05f, 0.42f), new Vector2(0.45f, 0.55f),
                opts.Gameplay.CursorSnapOnSelection, optCtrl.SetCursorSnapOnSelection);
            var tDoubleTap = BuildOptionToggle(gameSec, p + "DoubleTap", T("Double-Tap to Confirm"),
                new Vector2(0.52f, 0.42f), new Vector2(0.96f, 0.55f),
                opts.Gameplay.DoubleTapConfirmNumberEntry, optCtrl.SetDoubleTapConfirm);
            var tRumble = BuildOptionToggle(gameSec, p + "ControllerRumble", T("Controller Rumble"),
                new Vector2(0.05f, 0.26f), new Vector2(0.45f, 0.39f),
                opts.Gameplay.ControllerRumbleEnabled, optCtrl.SetControllerRumbleEnabled);

            tabs.SetRows(audioIdx + 2,
                Row(btnResetGame),
                Row(tHighlight, tConfirmWrong),
                Row(tAutoPencil, tCandidates),
                Row(tCursorSnap, tDoubleTap),
                Row(tRumble));

            return () =>
            {
                var o = optCtrl.GetOptions();
                master.Refresh(o.Audio.MasterVolume);
                music.Refresh(o.Audio.MusicVolume);
                sfx.Refresh(o.Audio.SfxVolume);
                ui.Refresh(o.Audio.UiVolume);
                tMute.SetIsOnWithoutNotify(o.Audio.MuteWhenUnfocused);
                tMuteAll.SetIsOnWithoutNotify(o.Audio.MuteAll);
                musicStyleDrop.SetValueWithoutNotify(
                    AudioAssetService.NormalizeMenuMusicStyleIndex(o.Audio.MenuMusicStyleIndex));
                tFullscreen.SetIsOnWithoutNotify(o.Graphics.Fullscreen);
                tVSync.SetIsOnWithoutNotify(o.Graphics.VSync);
                tScreenShake.SetIsOnWithoutNotify(o.Graphics.ScreenShake);
                particle.Refresh(o.Graphics.ParticleIntensity);
                brightness.Refresh(o.Graphics.Brightness);
                contrast.Refresh(o.Graphics.Contrast);
                resDrop.SetValueWithoutNotify(ResolutionIndex(resolutions, o.Graphics));
                displayModeDrop.SetValueWithoutNotify((int)OptionsController.NormalizeDisplayMode(o.Graphics.DisplayMode));
                if (langDrop != null) langDrop.SetValueWithoutNotify((int)o.Language);
                if (langConfirmRow != null) langConfirmRow.SetActive(false);
                tHighlight.SetIsOnWithoutNotify(o.Gameplay.HighlightConflicts);
                tConfirmWrong.SetIsOnWithoutNotify(o.Gameplay.ConfirmBeforeWrongPlacement);
                tAutoPencil.SetIsOnWithoutNotify(o.Gameplay.AutoPencilCleanup);
                tCandidates.SetIsOnWithoutNotify(o.Gameplay.ShowCandidateCount);
                tCursorSnap.SetIsOnWithoutNotify(o.Gameplay.CursorSnapOnSelection);
                tDoubleTap.SetIsOnWithoutNotify(o.Gameplay.DoubleTapConfirmNumberEntry);
                tRumble.SetIsOnWithoutNotify(o.Gameplay.ControllerRumbleEnabled);
            };
        }

        /// <summary>
        /// The main menu's accessibility toggles as a tab section. Used by the in-run Options; the main menu keeps its
        /// own Accessibility panel with the preview board and text size. Returns the refresh like
        /// <see cref="BuildStandardTabs"/>.
        /// </summary>
        public static Action BuildAccessibilitySection(OptionsTabSet tabs, int index, RectTransform section,
            OptionsController optCtrl, string prefix)
        {
            var p = prefix ?? "";
            var opts = optCtrl.GetOptions();
            SectionHeader(p + "AccHdr", section, T("Accessibility"));
            var btnReset = ResetButton(p + "BtnResetAccessibility", section, optCtrl, optCtrl.ResetAccessibilityToDefaults);

            var tColorblind = BuildOptionToggle(section, p + "Colorblind", T("Colorblind Mode"),
                new Vector2(0.05f, 0.74f), new Vector2(0.45f, 0.87f),
                opts.Accessibility.ColorblindMode, optCtrl.SetColorblindMode);
            var tHighContrast = BuildOptionToggle(section, p + "HighContrast", T("High Contrast"),
                new Vector2(0.52f, 0.74f), new Vector2(0.96f, 0.87f),
                opts.Accessibility.HighContrastMode, optCtrl.SetHighContrast);
            var tReduceMotion = BuildOptionToggle(section, p + "ReduceMotion", T("Reduce Motion"),
                new Vector2(0.05f, 0.58f), new Vector2(0.45f, 0.71f),
                opts.Accessibility.ReduceMotion, optCtrl.SetReduceMotion);
            var tAltSymbols = BuildOptionToggle(section, p + "AltSymbols", T("Alt Constraint Symbols"),
                new Vector2(0.52f, 0.58f), new Vector2(0.96f, 0.71f),
                opts.Accessibility.AlternativeConstraintSymbols, optCtrl.SetAltSymbols);
            var tScreenReader = BuildOptionToggle(section, p + "ScreenReader", T("Screen Reader"),
                new Vector2(0.05f, 0.42f), new Vector2(0.45f, 0.55f),
                opts.Accessibility.ScreenReaderEnabled, optCtrl.SetScreenReaderEnabled);

            tabs.SetRows(index,
                Row(btnReset),
                Row(tColorblind, tHighContrast),
                Row(tReduceMotion, tAltSymbols),
                Row(tScreenReader));

            return () =>
            {
                var o = optCtrl.GetOptions();
                tColorblind.SetIsOnWithoutNotify(o.Accessibility.ColorblindMode);
                tHighContrast.SetIsOnWithoutNotify(o.Accessibility.HighContrastMode);
                tReduceMotion.SetIsOnWithoutNotify(o.Accessibility.ReduceMotion);
                tAltSymbols.SetIsOnWithoutNotify(o.Accessibility.AlternativeConstraintSymbols);
                tScreenReader.SetIsOnWithoutNotify(o.Accessibility.ScreenReaderEnabled);
            };
        }

        private static int ResolutionIndex(Resolution[] resolutions, GraphicsSettingsModel graphics)
        {
            for (var i = 0; i < resolutions.Length; i++)
                if (resolutions[i].width == graphics.ResolutionWidth && resolutions[i].height == graphics.ResolutionHeight)
                    return i;
            return 0;
        }

        public static Text SectionHeader(string name, RectTransform section, string label)
        {
            var header = BuildText(name, section, label, OptionsUiStyle.HeaderSize, TextAnchor.MiddleLeft);
            SetRect(header.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.75f, 1.00f));
            return header;
        }

        private static Button ResetButton(string name, RectTransform section, OptionsController optCtrl, Action reset)
        {
            var btn = BuildButton(name, section, T("Reset"), 11);
            SetRect(btn.GetComponent<RectTransform>(), new Vector2(0.78f, 0.90f), new Vector2(0.96f, 0.99f));
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                reset();
                optCtrl.NotifyOptionsChangedForUiRefresh();
            });
            return btn;
        }

        // OPT-1: [<] [value%] [>] button row instead of a Unity Slider (controller-friendly).
        public static ArrowRow BuildVolumeArrowRow(string id, string label,
            RectTransform parent, Vector2 aMin, Vector2 aMax, float initValue, Action<float> setter)
        {
            var current = initValue;

            string FormatPercent(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
            string FormatVolumeLabel(float value) => label + " " + FormatPercent(value);

            var lbl = BuildText(id + "Label", parent, FormatVolumeLabel(initValue), 14, TextAnchor.MiddleLeft);
            SetRect(lbl.rectTransform, new Vector2(aMin.x, aMin.y), new Vector2(aMin.x + 0.28f, aMax.y));

            var btnDec = BuildButton(id + "Dec", parent, "<", 16);
            SetRect(btnDec.GetComponent<RectTransform>(),
                new Vector2(aMin.x + 0.29f, aMin.y), new Vector2(aMin.x + 0.35f, aMax.y));

            var valTxt = BuildText(id + "Val", parent, FormatPercent(initValue), 13, TextAnchor.MiddleCenter);
            SetRect(valTxt.rectTransform, new Vector2(aMin.x + 0.35f, aMin.y), new Vector2(aMax.x - 0.06f, aMax.y));
            valTxt.color = GamePalette.TextMuted;

            var btnInc = BuildButton(id + "Inc", parent, ">", 16);
            SetRect(btnInc.GetComponent<RectTransform>(), new Vector2(aMax.x - 0.06f, aMin.y), aMax);

            void Step(float delta)
            {
                current = Mathf.Clamp(Mathf.Round((current + delta) * 20f) / 20f, 0f, 1f);
                setter(current);
                lbl.text = FormatVolumeLabel(current);
                valTxt.text = FormatPercent(current);
            }

            btnDec.onClick.RemoveAllListeners();
            btnDec.onClick.AddListener(() => Step(-0.05f));
            btnInc.onClick.RemoveAllListeners();
            btnInc.onClick.AddListener(() => Step(0.05f));

            return new ArrowRow(btnDec, btnInc, valTxt, v =>
            {
                current = v;
                lbl.text = FormatVolumeLabel(v);
                valTxt.text = FormatPercent(v);
            });
        }

        // ------------------------------------------------------------------------
        // Widgets (also used by the main menu's other panels)
        // ------------------------------------------------------------------------

        public static Text BuildText(string name, RectTransform parent, string value, int size, TextAnchor anchor)
        {
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = EnsureComponent<Text>(rect.gameObject);
            text.text = value;
            text.font = InRunUiFactory.GetFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = OptionsUiStyle.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button BuildButton(string name, RectTransform parent, string label, int size)
        {
            var btnColor = OptionsUiStyle.Button;
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = EnsureImage(rect.gameObject, btnColor);
            img.raycastTarget = true;

            var btn = EnsureComponent<Button>(rect.gameObject);
            var outline = EnsureComponent<Outline>(rect.gameObject);
            outline.effectColor = GamePalette.AccentGoldGlow;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var colors = btn.colors;
            colors.normalColor      = btnColor;
            colors.highlightedColor = new Color(btnColor.r, btnColor.g, btnColor.b, 0.30f);
            colors.pressedColor     = new Color(btnColor.r - 0.05f, btnColor.g - 0.05f, btnColor.b - 0.05f, 0.48f);
            colors.selectedColor    = new Color(0.98f, 0.83f, 0.26f, 0.70f); // gold tint when controller-focused
            colors.disabledColor    = GamePalette.DisabledGray;
            colors.fadeDuration     = 0.08f;
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

            // Enable automatic navigation so EventSystem can traverse with stick/d-pad
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            var lblRect = EnsureRect("Label", rect, Vector2.zero, Vector2.one,
                new Vector2(10f, 8f), new Vector2(-10f, -8f));
            var text = EnsureComponent<Text>(lblRect.gameObject);
            text.text = label;
            text.font = InRunUiFactory.GetFont();
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = OptionsUiStyle.ButtonText;

            lblRect.SetAsLastSibling();
            return btn;
        }

        public static Toggle BuildToggle(string name, RectTransform parent, string label)
        {
            var row = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.sizeDelta = new Vector2(260f, 32f);

            var bg = EnsureImage(row.gameObject, GamePalette.WithAlpha(OptionsUiStyle.Button, 0.85f));
            var toggle = EnsureComponent<Toggle>(row.gameObject);

            // [REQ: REQ-UI-103] GAP-029: an always-visible box, so "off" reads as an empty box and "on" as a filled one.
            var box = EnsureRect("Box", row,
                new Vector2(0.02f, 0.12f), new Vector2(0.10f, 0.88f), Vector2.zero, Vector2.zero);
            EnsureImage(box.gameObject, new Color(0.02f, 0.02f, 0.02f, 0.90f)).raycastTarget = false;
            var boxOutline = EnsureComponent<Outline>(box.gameObject);
            boxOutline.effectColor = OptionsUiStyle.Accent;
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var check = EnsureRect("Checkmark", box,
                new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            var checkImg = EnsureImage(check.gameObject, OptionsUiStyle.Accent);
            checkImg.raycastTarget = false;

            var text = BuildText("Label", row, label, 15, TextAnchor.MiddleLeft);
            text.color = OptionsUiStyle.ButtonText;
            SetRect(text.rectTransform, new Vector2(0.14f, 0f), new Vector2(0.98f, 1f));

            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            return toggle;
        }

        public static Toggle BuildOptionToggle(RectTransform parent, string id, string label,
            Vector2 anchorMin, Vector2 anchorMax, bool value, UnityAction<bool> onChanged)
        {
            var tgl = BuildToggle(id + "Toggle", parent, label);
            SetRect(tgl.GetComponent<RectTransform>(), anchorMin, anchorMax);
            tgl.SetIsOnWithoutNotify(value);
            tgl.onValueChanged.RemoveAllListeners();
            tgl.onValueChanged.AddListener(onChanged);
            return tgl;
        }

        public static Dropdown BuildDropdown(string name, RectTransform parent)
        {
            var btnColor = OptionsUiStyle.Button;
            var rect = EnsureRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureImage(rect.gameObject, btnColor);
            var dropdown = EnsureComponent<Dropdown>(rect.gameObject);

            var label = BuildText("Label", rect, T("GeneratedUi.Dropdown.Select"), 16, TextAnchor.MiddleLeft);
            label.color = OptionsUiStyle.ButtonText;
            SetRect(label.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.76f, 0.9f));

            var arrowRect = EnsureRect("Arrow", rect,
                new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            var arrowText = EnsureComponent<Text>(arrowRect.gameObject);
            arrowText.font = InRunUiFactory.GetFont();
            arrowText.text = "▼";
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = OptionsUiStyle.ButtonText;
            arrowText.fontSize = 16;

            // Template
            var template = EnsureRect("Template", rect,
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 180f);
            // Opaque: the options underneath showed through the open list (1.1 display-mode check).
            EnsureImage(template.gameObject, new Color(btnColor.r, btnColor.g, btnColor.b, 1f));
            template.gameObject.SetActive(false);

            var viewport = EnsureRect("Viewport", template,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EnsureImage(viewport.gameObject, GamePalette.ScrollBgDark);
            EnsureComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

            var content = EnsureRect("Content", viewport,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            // One item tall: Unity measures the item's offsets against this rect when it opens the list; at 0 px
            // it subtracted one item height and the last option was hidden.
            content.sizeDelta = new Vector2(0f, DropdownItemHeight);
            var cl = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            cl.childAlignment = TextAnchor.UpperCenter;
            cl.childControlHeight = true;
            cl.childControlWidth = true;
            cl.childForceExpandHeight = false;
            cl.childForceExpandWidth = true;
            // Unity sizes the list as item height x item count, so the layout adds no spacing or padding;
            // before, the list was shorter than its items and the last option was cut off.
            cl.spacing = 0f;
            cl.padding = new RectOffset(0, 0, 0, 0);
            var cf = EnsureComponent<ContentSizeFitter>(content.gameObject);
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Item template
            var item = EnsureRect("Item", content,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -DropdownItemHeight), Vector2.zero);
            var itemLayout = EnsureComponent<LayoutElement>(item.gameObject);
            itemLayout.minHeight = DropdownItemHeight;
            itemLayout.preferredHeight = DropdownItemHeight;
            var itemBg = EnsureImage(item.gameObject, GamePalette.ListItemBg);
            var itemTgl = EnsureComponent<Toggle>(item.gameObject);
            itemTgl.targetGraphic = itemBg;

            var itemCheck = EnsureRect("Item Checkmark", item,
                new Vector2(0.02f, 0.18f), new Vector2(0.08f, 0.82f), Vector2.zero, Vector2.zero);
            var itemCheckImg = EnsureImage(itemCheck.gameObject, OptionsUiStyle.Accent);
            itemTgl.graphic = itemCheckImg;

            var itemLabel = BuildText("Item Label", item, T("GeneratedUi.Dropdown.Option"), 16, TextAnchor.MiddleLeft);
            itemLabel.color = OptionsUiStyle.ButtonText;
            SetRect(itemLabel.rectTransform, new Vector2(0.10f, 0f), new Vector2(0.98f, 1f));

            AttachDropdownItemCompat(item.gameObject, item, itemLabel, itemBg, itemTgl);

            var scroll = EnsureComponent<ScrollRect>(template.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            dropdown.captionText = label;
            dropdown.template = template;
            dropdown.itemText = itemLabel;
            dropdown.itemImage = null;
            dropdown.targetGraphic = EnsureImage(rect.gameObject, btnColor);
            dropdown.captionText.raycastTarget = false;
            dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return dropdown;
        }

        /// <summary>
        /// Resizes the dropdown template so all items are visible without scrolling (GAP-042).
        /// Call this after AddOptions(). Capped at 300px to stay on screen.
        /// </summary>
        public static void FitDropdownToItems(Dropdown dd)
        {
            if (dd == null || dd.template == null || dd.options.Count == 0) return;
            var h = Mathf.Min(dd.options.Count * DropdownItemHeight, 300f);
            var sd = dd.template.sizeDelta;
            dd.template.sizeDelta = new Vector2(sd.x, h);
        }

        // ------------------------------------------------------------------------
        // Core helpers
        // ------------------------------------------------------------------------

        /// <summary>Finds or creates the child <paramref name="name"/>, so a rebuild reuses the existing objects.</summary>
        public static RectTransform EnsureRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                child = go.GetComponent<RectTransform>();
            }
            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.offsetMin = offsetMin;
            child.offsetMax = offsetMax;
            return child;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static TComponent EnsureComponent<TComponent>(GameObject go) where TComponent : Component
        {
            var existing = go.GetComponent<TComponent>();
            return existing != null ? existing : go.AddComponent<TComponent>();
        }

        public static Image EnsureImage(GameObject go, Color color)
        {
            var img = EnsureComponent<Image>(go);
            img.color = color;
            return img;
        }

        // ------------------------------------------------------------------------
        // Helpers: Dropdown Compatibility
        // ------------------------------------------------------------------------

        private static void AttachDropdownItemCompat(GameObject itemGo, RectTransform itemRect,
            Text itemLabel, Image itemBg, Toggle itemTgl)
        {
            var compat = DropdownItemCompatResolver.Instance;
            if (!compat.IsAvailable)
            {
                Debug.LogWarning($"[OptionsUiFactory] DropdownItem compatibility unavailable: {compat.FailureReason}. Dropdowns may not work.");
                return;
            }

            var di = itemGo.GetComponent(compat.ItemType) ?? itemGo.AddComponent(compat.ItemType);
            if (di == null || !compat.Assign(di, itemRect, itemLabel, itemBg, itemTgl))
            {
                Debug.LogWarning("[OptionsUiFactory] Failed to attach DropdownItem compatibility component. Dropdowns may not work.");
            }
        }

        private sealed class DropdownItemCompatResolver
        {
            private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private static DropdownItemCompatResolver _instance;

            private readonly MemberSetter _text;
            private readonly MemberSetter _image;
            private readonly MemberSetter _toggle;
            private readonly MemberSetter _rectTransform;

            private DropdownItemCompatResolver(
                Type itemType,
                MemberSetter text,
                MemberSetter image,
                MemberSetter toggle,
                MemberSetter rectTransform,
                string failureReason)
            {
                ItemType = itemType;
                _text = text;
                _image = image;
                _toggle = toggle;
                _rectTransform = rectTransform;
                FailureReason = failureReason;
            }

            public static DropdownItemCompatResolver Instance
            {
                get
                {
                    if (_instance == null)
                        _instance = Create();
                    return _instance;
                }
            }

            public Type ItemType { get; }
            public string FailureReason { get; }
            public bool IsAvailable => ItemType != null
                && _text.IsAvailable
                && _image.IsAvailable
                && _toggle.IsAvailable
                && _rectTransform.IsAvailable;

            public bool Assign(Component target, RectTransform itemRect, Text itemLabel, Image itemBg, Toggle itemTgl)
            {
                if (!IsAvailable || target == null)
                    return false;

                _text.Set(target, itemLabel);
                _image.Set(target, itemBg);
                _toggle.Set(target, itemTgl);
                _rectTransform.Set(target, itemRect);
                return true;
            }

            private static DropdownItemCompatResolver Create()
            {
                var itemType = typeof(Dropdown).GetNestedType("DropdownItem",
                    BindingFlags.Public | BindingFlags.NonPublic);
                if (itemType == null)
                    return Unavailable("UnityEngine.UI.Dropdown.DropdownItem was not found");

                if (!typeof(Component).IsAssignableFrom(itemType))
                    return Unavailable("UnityEngine.UI.Dropdown.DropdownItem is not a Component");

                var text = MemberSetter.Resolve(itemType, "text", "m_Text", typeof(Text));
                var image = MemberSetter.Resolve(itemType, "image", "m_Image", typeof(Image));
                var toggle = MemberSetter.Resolve(itemType, "toggle", "m_Toggle", typeof(Toggle));
                var rectTransform = MemberSetter.Resolve(itemType, "rectTransform", "m_RectTransform", typeof(RectTransform));

                var failure = BuildFailureReason(text, image, toggle, rectTransform);
                return new DropdownItemCompatResolver(itemType, text, image, toggle, rectTransform, failure);
            }

            private static DropdownItemCompatResolver Unavailable(string reason)
            {
                return new DropdownItemCompatResolver(null, default, default, default, default, reason);
            }

            private static string BuildFailureReason(
                MemberSetter text,
                MemberSetter image,
                MemberSetter toggle,
                MemberSetter rectTransform)
            {
                if (text.IsAvailable && image.IsAvailable && toggle.IsAvailable && rectTransform.IsAvailable)
                    return null;

                var missing = new List<string>();
                if (!text.IsAvailable) missing.Add("text/m_Text");
                if (!image.IsAvailable) missing.Add("image/m_Image");
                if (!toggle.IsAvailable) missing.Add("toggle/m_Toggle");
                if (!rectTransform.IsAvailable) missing.Add("rectTransform/m_RectTransform");
                return "missing writable member(s): " + string.Join(", ", missing);
            }

            private readonly struct MemberSetter
            {
                private readonly PropertyInfo _property;
                private readonly FieldInfo _field;

                private MemberSetter(PropertyInfo property, FieldInfo field)
                {
                    _property = property;
                    _field = field;
                }

                public bool IsAvailable => _property != null || _field != null;

                public static MemberSetter Resolve(Type ownerType, string propertyName, string fieldName, Type valueType)
                {
                    var property = ownerType.GetProperty(propertyName, MemberFlags);
                    if (property != null
                        && property.CanWrite
                        && property.GetIndexParameters().Length == 0
                        && property.PropertyType.IsAssignableFrom(valueType))
                    {
                        return new MemberSetter(property, null);
                    }

                    var field = ownerType.GetField(fieldName, MemberFlags);
                    if (field != null && field.FieldType.IsAssignableFrom(valueType))
                        return new MemberSetter(null, field);

                    return default;
                }

                public void Set(Component target, object value)
                {
                    if (_property != null)
                    {
                        _property.SetValue(target, value, null);
                        return;
                    }

                    _field?.SetValue(target, value);
                }
            }
        }
    }
}
