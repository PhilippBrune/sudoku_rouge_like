using System;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-024] Select Class · Who walks the garden (owner pick 2026-10-21): the class painting dimmed, the banner
    // with the mode, eight class seals in two rows, the chosen class on a hanging scroll (ClassSelectScreen fills it) with
    // the optional seed and the warning before a saved run ends, and the run's settings on a paper strip: Harmony stones
    // (< >), the level's rule, the perk, irregular puzzles, Back and Begin. No confirmation window.
    public sealed partial class MainMenuBlueprintBuilder
    {
        // Seal centres in canvas fractions (the mockup's positions, 1920×1080).
        private static readonly float[] SealX = { 0.0755f, 0.193f, 0.310f, 0.427f };
        private static readonly float[] SealY = { 0.755f, 0.449f };
        private const float SealSize = 150f;

        private GameObject BuildClassSelectPanel(RectTransform root, MainMenuController mc)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var panel = EnsureRect("ClassSelectPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var backdrop = EnsureOrGetImage(panel, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_class_select"))
                backdrop.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            var pr = (RectTransform)panel.transform;
            foreach (var part in new[] { "Content", "ClassBanner", "ClassSeals", "ClassScroll", "RunStrip", "ClassKeyHint" })
                ScrollUiKit.DestroyChildrenNow(pr, part);
            var screen = EnsureComponent<ClassSelectScreen>(panel);

            var banner = ScrollUiKit.Banner(pr, "ClassBanner", new Vector2(0.083f, 0.882f), new Vector2(0.406f, 0.978f),
                "ClassSelectEyebrow", T("ClassSelect.Eyebrow.GardenRun"), "Title", T("ClassSelect.Title"), 44);
            var eyebrow = banner.transform.Find("ClassSelectEyebrow").GetComponent<Text>();

            var seals = ScrollUiKit.Area(pr, "ClassSeals", Vector2.zero, Vector2.one);
            var classes = (ClassId[])Enum.GetValues(typeof(ClassId));
            for (var i = 0; i < classes.Length && i < SealX.Length * SealY.Length; i++)
            {
                var id = classes[i];
                var seal = BuildClassSeal(seals, id, new Vector2(SealX[i % SealX.Length], SealY[i / SealX.Length]), mc);
                screen.RegisterSeal(id, seal);
            }

            var scroll = ScrollUiKit.HangingScroll(pr, "ClassScroll", new Vector2(0.536f, 0.233f), new Vector2(0.943f, 0.862f));
            BuildSeedRow(scroll, mc);
            var warning = ScrollUiKit.Label(scroll, "AbandonWarning", T("RunConfirm.AbandonWarning"), 17, TextAnchor.MiddleLeft,
                ScrollUiKit.Vermilion, new Vector2(0.07f, 0.115f), new Vector2(0.93f, 0.185f), 11);

            var strip = BuildRunStrip(pr, mc, out var harmonyArea, out var stones, out var perk, out var perkName, out var modeInfo,
                out var beginLabel);
            var hint = OptionsUiFactory.BuildText("ClassKeyHint", pr, T("ClassSelect.KeyHint"), 17, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.07f), Vector2.zero, Vector2.zero);
            hint.color = new Color(0.86f, 0.82f, 0.72f, 1f);

            screen.Configure(scroll, eyebrow, harmonyArea, stones, perk, perkName, modeInfo, beginLabel, warning.gameObject);
            mc.SetClassSelectScreen(screen);
            mc.SetSelectedClass(mc.SelectedClass);
            mc.RefreshClassLockStates();
            panel.SetActive(false);
            return panel;
        }

        // One class seal: a paper disc with the class icon in an ensō ring (vermilion when chosen), the Level 40 frame,
        // the name, the level or unlock goal and the prestige stars under it; dark with a LockOverlay when locked.
        private Button BuildClassSeal(RectTransform parent, ClassId id, Vector2 centre, MainMenuController mc)
        {
            // The glow is a sibling drawn before the seal (a child would draw over it).
            ScrollUiKit.Spot(parent, $"SealGlow_{id}", centre, new Vector2(SealSize * 1.6f, SealSize * 1.6f),
                new Color(1f, 0.82f, 0.48f, 0f), ScrollUiKit.Glow());
            var disc = ScrollUiKit.Spot(parent, $"BtnClass{id}", centre, new Vector2(SealSize, SealSize), new Color(0.94f, 0.89f, 0.78f, 1f),
                ScrollUiKit.Disc());
            disc.raycastTarget = true;
            var shadow = disc.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -8f);
            var frame = ScrollUiKit.Fill(disc.transform, "Frame", new Vector2(-0.10f, -0.10f), new Vector2(1.10f, 1.10f), Color.white,
                ScrollUiKit.Ring());
            frame.gameObject.SetActive(false);
            ScrollUiKit.Icon(disc.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(id), new Vector2(0.15f, 0.15f),
                new Vector2(0.85f, 0.85f), Color.white);
            var ring = ScrollUiKit.Fill(disc.transform, "Ring", new Vector2(-0.06f, -0.06f), new Vector2(1.06f, 1.06f),
                new Color(0.16f, 0.14f, 0.11f, 0.85f), ScrollUiKit.Enso());
            ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (int)id * 41 % 360);
            var lockOverlay = ScrollUiKit.Fill(disc.transform, "LockOverlay", Vector2.zero, Vector2.one, new Color(0.08f, 0.06f, 0.04f, 0.35f),
                ScrollUiKit.Disc());
            lockOverlay.gameObject.SetActive(false);

            var def = ClassCatalog.GetDefinition(id);
            ScrollUiKit.Label(disc.transform, "Label", T(def.Name), 22, TextAnchor.MiddleCenter, new Color(0.96f, 0.92f, 0.82f, 1f),
                new Vector2(-0.45f, -0.30f), new Vector2(1.45f, -0.10f), 14);
            ScrollUiKit.Label(disc.transform, "Level", string.Empty, 17, TextAnchor.UpperCenter, new Color(0.91f, 0.77f, 0.42f, 1f),
                new Vector2(-0.45f, -0.50f), new Vector2(1.45f, -0.30f), 11);
            ScrollUiKit.Label(disc.transform, "Stars", string.Empty, 17, TextAnchor.UpperCenter, ScrollUiKit.Vermilion,
                new Vector2(-0.45f, -0.64f), new Vector2(1.45f, -0.50f), 11);
            foreach (var text in disc.GetComponentsInChildren<Text>()) text.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            var button = disc.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            var colors = ScrollUiKit.StateColors(Color.white);
            colors.highlightedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colors.selectedColor = new Color(1f, 0.93f, 0.80f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() => mc.SetSelectedClass(id));
            disc.gameObject.AddComponent<FocusNotifier>().OnFocus = () => { if (button.interactable) mc.SetSelectedClass(id); };
            mc.RegisterClassButton(id, button);
            mc.RegisterLockOverlay(id, lockOverlay.gameObject);
            return button;
        }

        // [REQ: REQ-RUN-100] [REQ: RUN-WEEK-001] The optional seed and the seed of the week, at the foot of the class scroll.
        private void BuildSeedRow(RectTransform scroll, MainMenuController mc)
        {
            var go = new GameObject("SeedInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(scroll, false);
            SetRect((RectTransform)go.transform, new Vector2(0.07f, 0.035f), new Vector2(0.56f, 0.105f), Vector2.zero, Vector2.zero);
            var box = go.GetComponent<Image>();
            box.sprite = ScrollUiKit.Rounded(10f);
            box.type = Image.Type.Sliced;
            box.color = new Color(0.97f, 0.94f, 0.86f, 1f);
            ScrollUiKit.InkFrame(go.transform, new Color(0.33f, 0.29f, 0.24f, 0.45f), 1.5f);
            var text = ScrollUiKit.Label(go.transform, "Text", string.Empty, 20, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
            text.supportRichText = false;
            var placeholder = ScrollUiKit.Label(go.transform, "Placeholder", T("RunConfirm.Seed.Placeholder"), 18, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
            placeholder.fontStyle = FontStyle.Italic;
            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;
            mc.SetSeedInput(input);

            var (_, week) = WeeklySeedService.IsoWeek(DateTime.Today);
            var weekly = OptionsUiFactory.BuildButton("BtnWeeklySeed", scroll,
                LocalizationService.Format("RunConfirm.WeeklySeed", "Week {0} seed", week), 18);
            SetRect((RectTransform)weekly.transform, new Vector2(0.59f, 0.035f), new Vector2(0.93f, 0.105f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(weekly, new Color(0.24f, 0.20f, 0.16f, 1f), new Color(0.97f, 0.93f, 0.84f, 1f));
            weekly.onClick.RemoveAllListeners();
            weekly.onClick.AddListener(() => input.text = WeeklySeedService.SeedFor(DateTime.Today).ToString());
        }

        // The run's settings on a paper strip: Harmony (< stones >, its rule), the perk, irregular puzzles, Back and Begin.
        private RectTransform BuildRunStrip(RectTransform pr, MainMenuController mc, out GameObject harmonyArea, out Image[] stones,
            out Button perk, out Text perkName, out Text modeInfo, out Text beginLabel)
        {
            var strip = ScrollUiKit.Fill(pr, "RunStrip", new Vector2(0.042f, 0.094f), new Vector2(0.958f, 0.196f), ScrollUiKit.Paper,
                ScrollUiKit.Rounded(14f));
            var stripShadow = strip.gameObject.AddComponent<Shadow>();
            stripShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            stripShadow.effectDistance = new Vector2(0f, -8f);
            var rt = strip.rectTransform;

            var area = ScrollUiKit.Area(rt, "HarmonyArea", new Vector2(0f, 0f), new Vector2(0.705f, 1f));
            harmonyArea = area.gameObject;
            ScrollUiKit.Label(area, "HarmonyCaption", T("ClassSelect.Harmony"), 14, TextAnchor.LowerLeft, ScrollUiKit.InkSoft,
                new Vector2(0.017f, 0.55f), new Vector2(0.17f, 0.88f));
            var harmonyName = ScrollUiKit.Label(area, "HarmonyReadout", string.Empty, 22, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.017f, 0.12f), new Vector2(0.17f, 0.55f), 13);
            mc.SetHarmonyReadout(harmonyName);

            var prev = OptionsUiFactory.BuildButton("BtnHarmonyPrevClass", area, "‹", 30);
            SetRect((RectTransform)prev.transform, new Vector2(0.17f, 0.15f), new Vector2(0.205f, 0.85f), Vector2.zero, Vector2.zero);
            prev.onClick.RemoveAllListeners();
            prev.onClick.AddListener(() => mc.StepHarmonyClassLevel(-1));
            mc.SetHarmonyPrevClassBtn(prev);
            stones = new Image[11];
            for (var i = 0; i < stones.Length; i++)
            {
                var x = 0.215f + i * 0.0278f;
                stones[i] = ScrollUiKit.Spot(area, $"HarmonyStone_{i}", new Vector2(x + 0.012f, 0.5f), new Vector2(24f, 24f),
                    new Color(0.85f, 0.82f, 0.74f, 1f), ScrollUiKit.Disc());
            }
            var next = OptionsUiFactory.BuildButton("BtnHarmonyNextClass", area, "›", 30);
            SetRect((RectTransform)next.transform, new Vector2(0.525f, 0.15f), new Vector2(0.56f, 0.85f), Vector2.zero, Vector2.zero);
            next.onClick.RemoveAllListeners();
            next.onClick.AddListener(() => mc.StepHarmonyClassLevel(+1));
            mc.SetHarmonyNextClassBtn(next);
            foreach (var chevron in new[] { prev, next }) QuietChevron(chevron);
            var desc = ScrollUiKit.Label(area, "HarmonyDescriptionLabel", string.Empty, 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.57f, 0.08f), new Vector2(0.80f, 0.92f), 10);
            mc.SetHarmonyDescriptionLabel(desc);

            perk = OptionsUiFactory.BuildButton("BtnClassPerk", area, string.Empty, 18);
            SetRect((RectTransform)perk.transform, new Vector2(0.81f, 0.12f), new Vector2(0.995f, 0.88f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(perk, new Color(0.90f, 0.84f, 0.70f, 1f), ScrollUiKit.Ink);
            perk.onClick.RemoveAllListeners();
            perk.onClick.AddListener(() => mc.CyclePerkOnClassSelect());
            var perkLabel = perk.GetComponentInChildren<Text>();
            perkLabel.text = string.Empty;
            ScrollUiKit.Label(perk.transform, "PerkCaption", T("ClassSelect.Perk"), 12, TextAnchor.LowerCenter, ScrollUiKit.InkSoft,
                new Vector2(0f, 0.55f), new Vector2(1f, 0.95f));
            perkName = ScrollUiKit.Label(perk.transform, "PerkName", string.Empty, 17, TextAnchor.UpperCenter, ScrollUiKit.Ink,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.55f), 11);

            modeInfo = ScrollUiKit.Label(rt, "ModeInfo", string.Empty, 20, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.02f, 0.1f), new Vector2(0.69f, 0.9f), 12);

            var irregular = BuildToggle("TglAllowIrregular", rt, T("ClassSelect.Irregular"));
            SetRect(irregular.GetComponent<RectTransform>(), new Vector2(0.712f, 0.2f), new Vector2(0.808f, 0.8f), Vector2.zero, Vector2.zero);
            irregular.isOn = true;   // [REQ: GEN-REGION-003] default ON (SPEC-CONF-031)
            irregular.onValueChanged.RemoveAllListeners();
            irregular.onValueChanged.AddListener(mc.SetAllowIrregularPuzzles);

            var back = OptionsUiFactory.BuildButton("BtnClassBack", rt, T("Back"), 22);
            SetRect((RectTransform)back.transform, new Vector2(0.815f, 0.18f), new Vector2(0.875f, 0.82f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(back, new Color(0.24f, 0.20f, 0.16f, 1f), new Color(0.97f, 0.93f, 0.84f, 1f));
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);
            var begin = OptionsUiFactory.BuildButton("BtnConfirmClass", rt, T("ClassSelect.Begin"), 22);
            SetRect((RectTransform)begin.transform, new Vector2(0.882f, 0.18f), new Vector2(0.99f, 0.82f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(begin, OptionsUiStyle.PaperVermilion, new Color(0.99f, 0.95f, 0.88f, 1f));
            begin.onClick.RemoveAllListeners();
            begin.onClick.AddListener(mc.BeginRunFromSelect);
            beginLabel = begin.GetComponentInChildren<Text>();
            beginLabel.resizeTextForBestFit = true;
            beginLabel.resizeTextMinSize = 14;
            beginLabel.resizeTextMaxSize = 22;
            return rt;
        }

        // A chevron without a fill on the paper strip.
        private static void QuietChevron(Button button)
        {
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(0.97f, 0.83f, 0.52f, 0.35f);
            colors.selectedColor = OptionsUiStyle.PaperFocus;
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;
            button.GetComponent<Image>().color = Color.white;
            var outline = button.GetComponent<Outline>();
            if (outline != null) outline.effectColor = Color.clear;
            var label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.color = ScrollUiKit.Ink;
        }
    }
}
