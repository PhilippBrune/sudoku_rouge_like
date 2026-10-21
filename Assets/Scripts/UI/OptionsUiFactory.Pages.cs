using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // The Language and Accessibility pages of the Options scroll (UI-STYLE-017), split from OptionsUiFactory.cs.
    public static partial class OptionsUiFactory
    {
        // ------------------------------------------------------------------------
        // Audio, Display and Gameplay pages (UI-STYLE-017): one setting per line, a slip, Reset this page
        // ------------------------------------------------------------------------

        private static Action BuildAudioPage(OptionsTabSet tabs, OptionsController optCtrl, string p)
        {
            var opts = optCtrl.GetOptions();
            var index = tabs.Count;
            var audioSec = tabs.Add(p + "TabAudio", p + "AudioSection", T("Audio"));
            SectionHeader(p + "AudioHdr", audioSec, T("Audio"));
            PageSubtitle(p + "AudioSub", audioSec, T("Options.Sub.Audio"));
            var btnResetAudio = PaperResetButton(p + "BtnResetAudio", audioSec, optCtrl, optCtrl.ResetAudioToDefaults);

            var master = BuildVolumeArrowRow(p + "MasterVolume", T("Master Volume"), audioSec, LineMin(0), LineMax(0),
                opts.Audio.MasterVolume, optCtrl.SetMasterVolume);
            var music = BuildVolumeArrowRow(p + "MusicVolume", T("Music Volume"), audioSec, LineMin(1), LineMax(1),
                opts.Audio.MusicVolume, optCtrl.SetMusicVolume);
            var sfx = BuildVolumeArrowRow(p + "SfxVolume", T("SFX Volume"), audioSec, LineMin(2), LineMax(2),
                opts.Audio.SfxVolume, optCtrl.SetSfxVolume);
            var ui = BuildVolumeArrowRow(p + "UiVolume", T("UI Volume"), audioSec, LineMin(3), LineMax(3),
                opts.Audio.UiVolume, optCtrl.SetUiVolume);
            var tMute = BuildOptionToggle(audioSec, p + "MuteUnfocused", T("Mute when unfocused"), LineMin(4), LineMax(4),
                opts.Audio.MuteWhenUnfocused, optCtrl.SetMuteWhenUnfocused, T("Options.Note.MuteUnfocused"));
            var tMuteAll = BuildOptionToggle(audioSec, p + "MuteAll", T("Mute all"), LineMin(5), LineMax(5),
                opts.Audio.MuteAll, optCtrl.SetMuteAll);

            LineLabel(p + "MenuMusicStyleLabel", audioSec, 6, T("Audio.MenuMusicStyle.Label"));
            var musicStyleDrop = BuildDropdown(p + "MenuMusicStyleDropdown", audioSec);
            SetRect(musicStyleDrop.GetComponent<RectTransform>(), LineMin(6, ControlStart), LineMax(6, 0.82f));
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
            for (var line = 0; line < 7; line++) LineRule(audioSec, line);
            PageSlip(p + "AudioSlip", audioSec, T("Options.Slip.Tip"), T("Options.Tip.Audio"));

            tabs.SetRows(index,
                Row(master.Dec, master.Inc),
                Row(music.Dec, music.Inc),
                Row(sfx.Dec, sfx.Inc),
                Row(ui.Dec, ui.Inc),
                Row(tMute),
                Row(tMuteAll),
                Row(musicStyleDrop),
                Row(btnResetAudio));

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
            };
        }

        private static Action BuildDisplayPage(OptionsTabSet tabs, OptionsController optCtrl, string p)
        {
            var opts = optCtrl.GetOptions();
            var index = tabs.Count;
            var displaySec = tabs.Add(p + "TabDisplay", p + "DisplaySection", T("Display"));
            SectionHeader(p + "DisplayHdr", displaySec, T("Display"));
            PageSubtitle(p + "DisplaySub", displaySec, T("Options.Sub.Display"));
            var btnResetDisplay = PaperResetButton(p + "BtnResetDisplay", displaySec, optCtrl, optCtrl.ResetGraphicsToDefaults);

            var tFullscreen = BuildOptionToggle(displaySec, p + "Fullscreen", T("Fullscreen"), LineMin(0), LineMax(0),
                opts.Graphics.Fullscreen, optCtrl.SetFullscreen);
            var tVSync = BuildOptionToggle(displaySec, p + "VSync", T("VSync"), LineMin(1), LineMax(1),
                opts.Graphics.VSync, optCtrl.SetVSync, T("Options.Note.VSync"));
            var tScreenShake = BuildOptionToggle(displaySec, p + "ScreenShake", T("Screen Shake"), LineMin(2), LineMax(2),
                opts.Graphics.ScreenShake, optCtrl.SetScreenShake, T("Options.Note.ScreenShake"));
            var particle = BuildVolumeArrowRow(p + "ParticleIntensity", T("Particles"), displaySec, LineMin(3), LineMax(3),
                opts.Graphics.ParticleIntensity, optCtrl.SetParticleIntensity);
            var brightness = BuildVolumeArrowRow(p + "Brightness", T("Options.BoardBrightness"), displaySec,
                LineMin(4), LineMax(4), opts.Graphics.Brightness, optCtrl.SetBrightness);
            var contrast = BuildVolumeArrowRow(p + "Contrast", T("Options.BoardContrast"), displaySec,
                LineMin(5), LineMax(5), opts.Graphics.Contrast, optCtrl.SetContrast);

            var resolutions = OptionsController.GetSupportedResolutions();
            LineLabel(p + "ResLabel", displaySec, 6, T("Resolution"));
            var resDrop = BuildDropdown(p + "ResolutionDropdown", displaySec);
            SetRect(resDrop.GetComponent<RectTransform>(), LineMin(6, ControlStart), LineMax(6, 0.82f));
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

            LineLabel(p + "DisplayModeLabel", displaySec, 7, T("DisplayMode"));
            var displayModeDrop = BuildDropdown(p + "DisplayModeDropdown", displaySec);
            SetRect(displayModeDrop.GetComponent<RectTransform>(), LineMin(7, ControlStart), LineMax(7, 0.82f));
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
            for (var line = 0; line < 8; line++) LineRule(displaySec, line);
            PageSlip(p + "DisplaySlip", displaySec, T("Options.Slip.Tip"), T("Options.Tip.Display"));

            tabs.SetRows(index,
                Row(tFullscreen),
                Row(tVSync),
                Row(tScreenShake),
                Row(particle.Dec, particle.Inc),
                Row(brightness.Dec, brightness.Inc),
                Row(contrast.Dec, contrast.Inc),
                Row(resDrop),
                Row(displayModeDrop),
                Row(btnResetDisplay));

            return () =>
            {
                var o = optCtrl.GetOptions();
                tFullscreen.SetIsOnWithoutNotify(o.Graphics.Fullscreen);
                tVSync.SetIsOnWithoutNotify(o.Graphics.VSync);
                tScreenShake.SetIsOnWithoutNotify(o.Graphics.ScreenShake);
                particle.Refresh(o.Graphics.ParticleIntensity);
                brightness.Refresh(o.Graphics.Brightness);
                contrast.Refresh(o.Graphics.Contrast);
                resDrop.SetValueWithoutNotify(ResolutionIndex(resolutions, o.Graphics));
                displayModeDrop.SetValueWithoutNotify((int)OptionsController.NormalizeDisplayMode(o.Graphics.DisplayMode));
            };
        }

        private static Action BuildGameplayPage(OptionsTabSet tabs, OptionsController optCtrl, Setup setup, string p)
        {
            var opts = optCtrl.GetOptions();
            var index = tabs.Count;
            var gameSec = tabs.Add(p + "TabGame", p + "GameSection", T("Gameplay"));
            SectionHeader(p + "GameHdr", gameSec, T("Gameplay"));
            PageSubtitle(p + "GameSub", gameSec, T("Options.Sub.Gameplay"));
            var btnResetGame = PaperResetButton(p + "BtnResetGameplay", gameSec, optCtrl, optCtrl.ResetGameplayToDefaults);

            // GAP-061: every toggle acts and is named for what it does (OPT-GAME-001..005, OPT-LABEL-001); "Confirm Wrong
            // Placement" is gone (a question asked only before a wrong digit would give the answer away).
            var tHighlight = BuildOptionToggle(gameSec, p + "HighlightErrors", T("Highlight Errors"), LineMin(0), LineMax(0),
                opts.Gameplay.HighlightConflicts, v =>
                {
                    optCtrl.SetHighlightConflicts(v);
                    setup?.OnHighlightConflictsChanged?.Invoke(v);
                }, T("Options.Note.HighlightErrors"));
            var tAutoPencil = BuildOptionToggle(gameSec, p + "AutoPencil", T("Auto Pencil Cleanup"), LineMin(1), LineMax(1),
                !opts.Gameplay.KeepPencilMarks, optCtrl.SetAutoPencilCleanup, T("Options.Note.AutoPencil"));
            var tCandidates = BuildOptionToggle(gameSec, p + "CandidateCount", T("Options.DigitsLeft"), LineMin(2), LineMax(2),
                opts.Gameplay.ShowCandidateCount, optCtrl.SetShowCandidateCount, T("Options.Note.DigitsLeft"));
            var tCursorSnap = BuildOptionToggle(gameSec, p + "CursorSnap", T("Options.JumpToNextEmpty"), LineMin(3), LineMax(3),
                opts.Gameplay.CursorSnapOnSelection, optCtrl.SetCursorSnapOnSelection, T("Options.Note.JumpToNextEmpty"));
            var tDoubleTap = BuildOptionToggle(gameSec, p + "DoubleTap", T("Double-Tap to Confirm"), LineMin(4), LineMax(4),
                opts.Gameplay.DoubleTapConfirmNumberEntry, optCtrl.SetDoubleTapConfirm, T("Options.Note.DoubleTap"));
            var tRumble = BuildOptionToggle(gameSec, p + "ControllerRumble", T("Controller Rumble"), LineMin(5), LineMax(5),
                opts.Gameplay.ControllerRumbleEnabled, optCtrl.SetControllerRumbleEnabled, T("Options.Note.Rumble"));
            // [REQ: OPT-GAME-006] The input lock after choosing a cell: on/off and its time, 0.10–1.00 s in 0.05 s steps.
            var tInputLock = BuildOptionToggle(gameSec, p + "InputLock", T("Options.InputLock"), LineMin(6), LineMax(6),
                opts.Gameplay.InputLockEnabled, optCtrl.SetInputLockEnabled, T("Options.Note.InputLock"));
            var lockTime = BuildPaperValueRow(p + "InputLockTime", T("Options.InputLockTime"), gameSec, LineMin(7), LineMax(7),
                GameplaySettings.ClampInputLockSeconds(opts.Gameplay.InputLockSeconds), GameplaySettings.MinInputLockSeconds,
                GameplaySettings.MaxInputLockSeconds, GameplaySettings.InputLockStepSeconds, optCtrl.SetInputLockSeconds, Seconds);
            for (var line = 0; line < 8; line++) LineRule(gameSec, line);
            PageSlip(p + "GameSlip", gameSec, T("Options.Slip.Tip"), T("Options.Tip.Gameplay"));

            tabs.SetRows(index,
                Row(tHighlight),
                Row(tAutoPencil),
                Row(tCandidates),
                Row(tCursorSnap),
                Row(tDoubleTap),
                Row(tRumble),
                Row(tInputLock),
                Row(lockTime.Dec, lockTime.Inc),
                Row(btnResetGame));

            return () =>
            {
                var o = optCtrl.GetOptions();
                tHighlight.SetIsOnWithoutNotify(o.Gameplay.HighlightConflicts);
                tAutoPencil.SetIsOnWithoutNotify(!o.Gameplay.KeepPencilMarks);
                tCandidates.SetIsOnWithoutNotify(o.Gameplay.ShowCandidateCount);
                tCursorSnap.SetIsOnWithoutNotify(o.Gameplay.CursorSnapOnSelection);
                tDoubleTap.SetIsOnWithoutNotify(o.Gameplay.DoubleTapConfirmNumberEntry);
                tRumble.SetIsOnWithoutNotify(o.Gameplay.ControllerRumbleEnabled);
                tInputLock.SetIsOnWithoutNotify(o.Gameplay.InputLockEnabled);
                lockTime.Refresh(GameplaySettings.ClampInputLockSeconds(o.Gameplay.InputLockSeconds));
            };
        }

        // The Language page: the language row where the UI can rebuild itself (main menu); a run says where to change it.
        private static Action BuildLanguagePage(OptionsTabSet tabs, OptionsController optCtrl, Setup setup, string p)
        {
            var opts = optCtrl.GetOptions();
            var langIdx = tabs.Count;
            var langSec = tabs.Add(p + "TabLanguage", p + "LanguageSection", T("Language"));
            SectionHeader(p + "LanguageHdr", langSec, T("Language"));
            PageSubtitle(p + "LanguageSub", langSec, T("Options.Sub.Language"));
            PageSlip(p + "LanguageSlip", langSec, T("Options.Slip.Tip"), T("Options.Tip.Language"));

            if (setup.OnLanguageApplied == null)
            {
                var hint = BuildText(p + "LangHint", langSec, T("Options.LanguageInMainMenu"), 19, TextAnchor.MiddleLeft);
                SetRect(hint.rectTransform, LineMin(0), LineMax(1));
                hint.color = OptionsUiStyle.PaperInkSoft;
                tabs.SetRows(langIdx);
                return () => { };
            }

            LineLabel(p + "LangLabel", langSec, 0, T("Language"));
            var langDrop = BuildDropdown(p + "LanguageDropdown", langSec);
            SetRect(langDrop.GetComponent<RectTransform>(), LineMin(0, ControlStart), LineMax(0, 0.82f));
            langDrop.ClearOptions();
            langDrop.AddOptions(new List<string> { "English", "Deutsch", "Français", "Español", "Português (BR)" });
            FitDropdownToItems(langDrop);
            langDrop.SetValueWithoutNotify((int)opts.Language);
            LineRule(langSec, 0);

            // Language confirmation row (initially hidden)
            var langConfirmRt = EnsureRect(p + "LangConfirmRow", langSec, LineMin(1), LineMax(1), Vector2.zero, Vector2.zero);
            var langConfirmRow = langConfirmRt.gameObject;
            var langWarnText = BuildText(p + "LangWarnText", langConfirmRt, T("Options.LanguageConfirm.Warning"), 16, TextAnchor.MiddleLeft);
            SetRect(langWarnText.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f));
            langWarnText.color = OptionsUiStyle.PaperVermilion;
            var btnLangApply = BuildButton(p + "BtnLangApply", langConfirmRt, T("Apply"), 18);
            SetRect(btnLangApply.GetComponent<RectTransform>(), new Vector2(0.57f, 0.08f), new Vector2(0.77f, 0.92f));
            var btnLangCancel = BuildButton(p + "BtnLangCancel", langConfirmRt, T("Cancel"), 18);
            SetRect(btnLangCancel.GetComponent<RectTransform>(), new Vector2(0.79f, 0.08f), new Vector2(0.99f, 0.92f));
            langConfirmRow.SetActive(false);

            var pendingLangIdx = (int)opts.Language;
            langDrop.onValueChanged.RemoveAllListeners();
            langDrop.onValueChanged.AddListener(idx =>
            {
                pendingLangIdx = idx;
                langConfirmRow.SetActive(idx != (int)optCtrl.GetOptions().Language);
            });
            btnLangApply.onClick.RemoveAllListeners();
            btnLangApply.onClick.AddListener(() =>
            {
                optCtrl.SetLanguage((LanguageOption)pendingLangIdx);
                langConfirmRow.SetActive(false);
                setup.OnLanguageApplied();
            });
            btnLangCancel.onClick.RemoveAllListeners();
            btnLangCancel.onClick.AddListener(() =>
            {
                langDrop.SetValueWithoutNotify((int)optCtrl.GetOptions().Language);
                langConfirmRow.SetActive(false);
            });

            tabs.SetRows(langIdx, Row(langDrop), Row(btnLangApply, btnLangCancel));
            return () =>
            {
                langDrop.SetValueWithoutNotify((int)optCtrl.GetOptions().Language);
                langConfirmRow.SetActive(false);
            };
        }

        /// <summary>
        /// The Accessibility page: colour-blind, high contrast, reduce motion, other rule symbols, status captions and,
        /// with <paramref name="withPreview"/> (main menu), text size and a preview board on the slip. Returns its refresh.
        /// </summary>
        public static Action BuildAccessibilitySection(OptionsTabSet tabs, int index, RectTransform section,
            OptionsController optCtrl, string prefix, bool withPreview = false)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var p = prefix ?? "";
            var opts = optCtrl.GetOptions();
            SectionHeader(p + "AccHdr", section, T("Accessibility"));
            PageSubtitle(p + "AccSub", section, T("Options.Sub.Accessibility"));
            var btnReset = PaperResetButton(p + "BtnResetAccessibility", section, optCtrl, optCtrl.ResetAccessibilityToDefaults);

            var tColorblind = BuildOptionToggle(section, p + "Colorblind", T("Colorblind Mode"), LineMin(0), LineMax(0),
                opts.Accessibility.ColorblindMode, optCtrl.SetColorblindMode, T("Options.Note.Colorblind"));
            var tHighContrast = BuildOptionToggle(section, p + "HighContrast", T("High Contrast"), LineMin(1), LineMax(1),
                opts.Accessibility.HighContrastMode, optCtrl.SetHighContrast, T("Options.Note.HighContrast"));
            var tReduceMotion = BuildOptionToggle(section, p + "ReduceMotion", T("Reduce Motion"), LineMin(2), LineMax(2),
                opts.Accessibility.ReduceMotion, optCtrl.SetReduceMotion, T("Options.Note.ReduceMotion"));
            var tAltSymbols = BuildOptionToggle(section, p + "AltSymbols", T("Alt Constraint Symbols"), LineMin(3), LineMax(3),
                opts.Accessibility.AlternativeConstraintSymbols, optCtrl.SetAltSymbols, T("Options.Note.AltSymbols"));
            var tScreenReader = BuildOptionToggle(section, p + "ScreenReader", T("Options.StatusCaptions"), LineMin(4), LineMax(4),
                opts.Accessibility.ScreenReaderEnabled, optCtrl.SetScreenReaderEnabled, T("Options.Note.StatusCaptions"));
            var lines = 5;

            ArrowRow? textSize = null;
            AccessibilityPreviewController preview = null;
            if (withPreview)
            {
                // [REQ: ACCESS-FONT-001] Text Size (80–150 %) and the preview board that shows the chosen settings.
                textSize = BuildPaperValueRow(p + "FontScale", T("Options.TextSize"), section, LineMin(5), LineMax(5),
                    opts.Accessibility.FontScale, 0.8f, 1.5f, 0.05f, optCtrl.SetFontScale);
                lines = 6;
                var slip = PageSlip(p + "AccSlip", section, T("Accessibility.Preview"), null, 0.30f);
                var previewRt = EnsureRect("AccessPreview", slip, new Vector2(0.07f, 0.06f), new Vector2(0.93f, 0.82f),
                    Vector2.zero, Vector2.zero);
                preview = EnsureComponent<AccessibilityPreviewController>(previewRt.gameObject);
                preview.Configure(optCtrl);
            }
            else
                PageSlip(p + "AccSlip", section, T("Options.Slip.Tip"), T("Options.Tip.Accessibility"));
            for (var line = 0; line < lines; line++) LineRule(section, line);

            var rows = new List<Selectable[]>
            {
                Row(tColorblind), Row(tHighContrast), Row(tReduceMotion), Row(tAltSymbols), Row(tScreenReader)
            };
            if (textSize.HasValue) rows.Add(Row(textSize.Value.Dec, textSize.Value.Inc));
            rows.Add(Row(btnReset));
            tabs.SetRows(index, rows.ToArray());

            return () =>
            {
                var o = optCtrl.GetOptions();
                tColorblind.SetIsOnWithoutNotify(o.Accessibility.ColorblindMode);
                tHighContrast.SetIsOnWithoutNotify(o.Accessibility.HighContrastMode);
                tReduceMotion.SetIsOnWithoutNotify(o.Accessibility.ReduceMotion);
                tAltSymbols.SetIsOnWithoutNotify(o.Accessibility.AlternativeConstraintSymbols);
                tScreenReader.SetIsOnWithoutNotify(o.Accessibility.ScreenReaderEnabled);
                textSize?.Refresh(o.Accessibility.FontScale);
                if (preview != null) preview.Refresh(o.Accessibility);
            };
        }
    }
}
