using System;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-025] Game Modes · Five paths (owner pick 2026-10-21): the modes painting dimmed, the banner, a wooden
    // beam with five noren curtains (Garden Run, Endless Zen, Spirit Trials, Daily Walk, Monthly Walk), Endless Zen's
    // Records under its curtain, and a paper slip with the focused mode's settings and the way in (GameModesScreen).
    // The Harmony setting stays here and on Select Class (owner 2026-10-21).
    public sealed partial class MainMenuBlueprintBuilder
    {
        // [REQ: UI-STYLE-027] Six curtains (the Incense Walk joins), 272 px wide with 20 px between them.
        private static readonly float[] CurtainLeft = { 90f / 1920f, 382f / 1920f, 674f / 1920f, 966f / 1920f, 1258f / 1920f, 1550f / 1920f };
        private const float CurtainWidth = 272f / 1920f, CurtainTop = 0.804f, CurtainBottom = 0.406f;

        private GameObject BuildGameModesPanel(RectTransform root, MainMenuController mc, GameModesPanelController modesCtrl)
        {
            using var paperScope = OptionsUiStyle.UsePaper();
            var panel = EnsureRect("GameModesPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var backdrop = EnsureOrGetImage(panel, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_game_modes"))
                backdrop.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            var pr = (RectTransform)panel.transform;
            // A rebuild (language change) reuses the panel: everything built below goes first, the curtains by their names.
            foreach (var part in new[] { "Content", "ModesBanner", "ModesBeam", "CurtainGlow", "BtnGarden", "BtnEndless", "BtnTrials",
                         "BtnDailyWalk", "BtnMonthlyWalk", "BtnIncenseWalk", "IncenseNew", "BtnZenRecords", "ModeSlip", "ModesKeyHint" })
                ScrollUiKit.DestroyChildrenNow(pr, part);
            var screen = EnsureComponent<GameModesScreen>(panel);

            ScrollUiKit.Banner(pr, "ModesBanner", new Vector2(0.318f, 0.884f), new Vector2(0.682f, 0.980f),
                "ModesEyebrow", T("GameModes.Eyebrow"), "Title", T("Game Modes"), 44);
            var beam = ScrollUiKit.Fill(pr, "ModesBeam", new Vector2(0.036f, 0.791f), new Vector2(0.964f, 0.819f),
                new Color(0.55f, 0.36f, 0.20f, 1f), ScrollUiKit.Rounded(14f));
            ScrollUiKit.Fill(beam.transform, "Light", new Vector2(0.004f, 0.40f), new Vector2(0.996f, 0.62f),
                new Color(0.75f, 0.54f, 0.32f, 1f), ScrollUiKit.Rounded(6f));
            var beamShadow = beam.gameObject.AddComponent<Shadow>();
            beamShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            beamShadow.effectDistance = new Vector2(0f, -6f);

            var slip = ScrollUiKit.Fill(pr, "ModeSlip", new Vector2(0.135f, 0.120f), new Vector2(0.865f, 0.361f), ScrollUiKit.Paper);
            var slipShadow = slip.gameObject.AddComponent<Shadow>();
            slipShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            slipShadow.effectDistance = new Vector2(0f, -10f);
            var slipRt = slip.rectTransform;

            var modes = new (GameModesScreen.Mode Mode, string Button, string Name, string Icon, string Desc, Action Enter, string EnterLabel)[]
            {
                (GameModesScreen.Mode.Garden, "BtnGarden", "Garden Run", "ui/icon_framed_garden", "GameModes.Desc.Garden", mc.StartGame, "GameModes.ChooseClass"),
                (GameModesScreen.Mode.Endless, "BtnEndless", "Endless Zen", "ui/icon_zen_mode", "GameModes.Desc.Endless", mc.StartEndlessZen, "GameModes.ChooseClass"),
                (GameModesScreen.Mode.Trials, "BtnTrials", "Spirit Trials", "ui/icon_trials_seal", "GameModes.Desc.Trials", mc.ShowSpiritTrialsTierSelect, "GameModes.ChooseTier"),
                (GameModesScreen.Mode.Daily, "BtnDailyWalk", "Daily Walk", "ui/icon_start_run", "GameModes.Desc.Daily", mc.EnterDailyWalk, "GameModes.Daily.Walk"),
                (GameModesScreen.Mode.Monthly, "BtnMonthlyWalk", "Monthly Walk", "ui/icon_monthly_lantern", "GameModes.Desc.Monthly", mc.ShowMonthlyWalkPanel, "GameModes.Open"),
                (GameModesScreen.Mode.Incense, "BtnIncenseWalk", "Incense Walk", "items/icon_temple_incense", "GameModes.Desc.Incense", mc.StartIncenseWalkWeekly, "GameModes.ChooseClass")
            };
            for (var i = 0; i < modes.Length; i++)
            {
                var m = modes[i];
                var curtain = BuildCurtain(pr, m.Mode, m.Button, T(m.Name), m.Icon, T(m.Desc), CurtainLeft[i], m.Enter, screen);
                var page = ScrollUiKit.Area(slipRt, "Slip_" + m.Mode, Vector2.zero, Vector2.one);
                ScrollUiKit.Label(page, "Caption", T("GameModes.Caption." + m.Mode).ToUpperInvariant(), 14, TextAnchor.MiddleLeft,
                    ScrollUiKit.InkSoft, new Vector2(0.029f, 0.83f), new Vector2(0.60f, 0.93f), 10);
                var enterButton = OptionsUiFactory.BuildButton(m.Button + "Enter", page, T(m.EnterLabel), 22);
                SetRect((RectTransform)enterButton.transform, new Vector2(0.814f, 0.09f), new Vector2(0.971f, 0.31f), Vector2.zero, Vector2.zero);
                OptionsUiFactory.PaintPill(enterButton, OptionsUiStyle.PaperVermilion, new Color(0.99f, 0.95f, 0.88f, 1f));
                var enter = m.Enter;
                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(() => enter());
                if (m.Mode == GameModesScreen.Mode.Garden)
                    BuildHarmonyPage(page, mc, screen);
                else
                {
                    // [REQ: UI-STYLE-027] The Daily Walk's slip: its own title, the rules, and a side box with the streak,
                    // the ink stamps and the day's board.
                    var incense = m.Mode == GameModesScreen.Mode.Incense;
                    var side = m.Mode == GameModesScreen.Mode.Daily || incense;
                    var title = m.Mode == GameModesScreen.Mode.Daily ? T("GameModes.Daily.Title") : incense ? T("GameModes.Incense.Title") : T(m.Name);
                    ScrollUiKit.Label(page, "Title", title, 32, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                        new Vector2(0.029f, 0.63f), new Vector2(0.70f, 0.83f), 18);
                    var trials = m.Mode == GameModesScreen.Mode.Trials;
                    ScrollUiKit.Label(page, "Body", string.Empty, side ? 18 : 19, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                        new Vector2(0.029f, trials ? 0.34f : 0.06f), new Vector2(incense ? 0.485f : side ? 0.60f : 0.70f, 0.61f), 12).supportRichText = true;
                    if (trials) BuildTrialTiers(page, mc, screen);
                    if (side) BuildSideBox(page);
                    if (incense) BuildIncensePractice(page, mc, screen);
                }
                screen.Register(m.Mode, curtain, page.gameObject);
            }

            // [REQ: UI-STYLE-027] "New" on the Incense Walk's curtain until it has been walked once.
            ScrollUiKit.Pill(pr, "IncenseNew", new Vector2(CurtainLeft[5] + CurtainWidth - 0.032f, 0.792f),
                new Vector2(CurtainLeft[5] + CurtainWidth + 0.006f, 0.818f), T("GameModes.New"), ScrollUiKit.Gold, ScrollUiKit.Ink, 14);

            // Endless Zen's Records hang under its curtain.
            var records = OptionsUiFactory.BuildButton("BtnZenRecords", pr, T("Records"), 18);
            SetRect((RectTransform)records.transform, new Vector2(CurtainLeft[1] + 0.03f, 0.366f),
                new Vector2(CurtainLeft[1] + CurtainWidth - 0.03f, 0.401f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(records, new Color(0.24f, 0.20f, 0.16f, 1f), new Color(0.97f, 0.93f, 0.84f, 1f));
            records.onClick.RemoveAllListeners();
            records.onClick.AddListener(mc.ShowZenLeaderboard);

            var back = OptionsUiFactory.BuildButton("BtnModesBack", slipRt, T("Back"), 22);
            SetRect((RectTransform)back.transform, new Vector2(0.725f, 0.09f), new Vector2(0.807f, 0.31f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(back, new Color(0.24f, 0.20f, 0.16f, 1f), new Color(0.97f, 0.93f, 0.84f, 1f));
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(mc.BackToMainMenu);

            var hint = OptionsUiFactory.BuildText("ModesKeyHint", pr, T("GameModes.KeyHint"), 17, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.07f), Vector2.zero, Vector2.zero);
            hint.color = new Color(0.86f, 0.82f, 0.72f, 1f);

            screen.SetNavigationButtons(records, back);
            mc.SetGameModesScreen(screen);
            screen.Show(GameModesScreen.Mode.Garden);
            panel.SetActive(false);
            return panel;
        }

        // One noren curtain: the cloth (the button), a split up its lower half, the icon, the name ("Label"), a line on the
        // mode and its state on a paper strip. A click enters the mode; focus or the pointer shows its slip.
        private Button BuildCurtain(RectTransform pr, GameModesScreen.Mode mode, string name, string title, string icon, string desc,
            float left, Action enter, GameModesScreen screen)
        {
            var min = new Vector2(left, CurtainBottom);
            var max = new Vector2(left + CurtainWidth, CurtainTop);
            ScrollUiKit.Fill(pr, "CurtainGlow_" + mode, min - new Vector2(0.02f, 0.03f), max + new Vector2(0.02f, 0.02f),
                new Color(1f, 0.82f, 0.48f, 0f), ScrollUiKit.Glow());
            var cloth = ScrollUiKit.Fill(pr, name, min, max, new Color(0.19f, 0.28f, 0.23f, 1f));
            cloth.raycastTarget = true;
            var shadow = cloth.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -10f);
            ScrollUiKit.Fill(cloth.transform, "Split", new Vector2(0.495f, 0f), new Vector2(0.505f, 0.40f), new Color(0f, 0f, 0f, 0.35f));
            ScrollUiKit.Icon(cloth.transform, "Icon", icon, new Vector2(0.31f, 0.63f), new Vector2(0.69f, 0.89f), Color.white);
            ScrollUiKit.Label(cloth.transform, "Label", title, 30, TextAnchor.MiddleCenter, new Color(0.96f, 0.92f, 0.82f, 1f),
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.60f), 18);
            ScrollUiKit.Label(cloth.transform, "Desc", desc, 17, TextAnchor.UpperCenter, new Color(0.85f, 0.81f, 0.71f, 1f),
                new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.47f), 11);
            var state = ScrollUiKit.Fill(cloth.transform, "State", new Vector2(0.067f, 0.04f), new Vector2(0.933f, 0.13f),
                new Color(0.94f, 0.89f, 0.78f, 0.95f));
            ScrollUiKit.Label(state.transform, "StateText", string.Empty, 18, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 11).supportRichText = true;

            var button = cloth.gameObject.AddComponent<Button>();
            button.targetGraphic = cloth;
            var colors = ScrollUiKit.StateColors(Color.white);
            colors.highlightedColor = new Color(1.1f, 1.08f, 1.02f, 1f);
            colors.selectedColor = new Color(1.12f, 1.1f, 1.05f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() => enter());
            cloth.gameObject.AddComponent<FocusNotifier>().OnFocus = () => screen.Show(mode);
            var hover = cloth.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => screen.Show(mode));
            hover.triggers.Add(enterEntry);
            return button;
        }

        // [REQ: UI-STYLE-027] A darker slip at the top right of a mode's page (like the perk box), filled by the refresh.
        private static void BuildSideBox(RectTransform page)
        {
            var box = ScrollUiKit.Fill(page, "SideBox", new Vector2(0.700f, 0.36f), new Vector2(0.971f, 0.90f),
                new Color(0.90f, 0.84f, 0.69f, 1f));
            ScrollUiKit.Label(box.transform, "SideText", string.Empty, 17, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.95f), 11).supportRichText = true;
        }

        // [REQ: INCENSE-BOARD-001] The Incense Walk's practice on a random seed (no board), left of Back.
        private static void BuildIncensePractice(RectTransform page, MainMenuController mc, GameModesScreen screen)
        {
            var practice = OptionsUiFactory.BuildButton("BtnIncensePractice", page, T("GameModes.Incense.Practice"), 20);
            SetRect((RectTransform)practice.transform, new Vector2(0.495f, 0.09f), new Vector2(0.718f, 0.31f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(practice, new Color(0.24f, 0.20f, 0.16f, 1f), new Color(0.97f, 0.93f, 0.84f, 1f));
            var label = practice.GetComponentInChildren<Text>(true);   // long in German and French: shrink to fit
            if (label != null)
            {
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 20;
            }
            practice.onClick.RemoveAllListeners();
            practice.onClick.AddListener(mc.StartIncensePractice);
            screen.SetExtraButton(GameModesScreen.Mode.Incense, practice);
        }

        // The Garden Run slip: Harmony with its stones (< >), the level's rule and the next level's condition, the perk.
        private void BuildHarmonyPage(RectTransform page, MainMenuController mc, GameModesScreen screen)
        {
            ScrollUiKit.Label(page, "HarmonySelectedLabel", string.Empty, 32, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.029f, 0.63f), new Vector2(0.60f, 0.83f), 18);
            var prev = OptionsUiFactory.BuildButton("BtnHarmonyPrev", page, "‹", 30);
            SetRect((RectTransform)prev.transform, new Vector2(0.008f, 0.45f), new Vector2(0.028f, 0.61f), Vector2.zero, Vector2.zero);
            prev.onClick.RemoveAllListeners();
            prev.onClick.AddListener(() => mc.StepHarmonyClassLevel(-1));
            var stones = new Image[11];
            for (var i = 0; i < stones.Length; i++)
            {
                stones[i] = ScrollUiKit.Spot(page, $"HarmonyStone_{i}", new Vector2(0.040f + i * 0.030f, 0.53f), new Vector2(30f, 30f),
                    new Color(0.81f, 0.78f, 0.69f, 1f), ScrollUiKit.Disc());
                ScrollUiKit.Label(stones[i].transform, "Numeral", GameModesScreen.Numeral(i), 12, TextAnchor.MiddleCenter, ScrollUiKit.InkSoft,
                    Vector2.zero, Vector2.one, 8);
            }
            var next = OptionsUiFactory.BuildButton("BtnHarmonyNext", page, "›", 30);
            SetRect((RectTransform)next.transform, new Vector2(0.367f, 0.45f), new Vector2(0.387f, 0.61f), Vector2.zero, Vector2.zero);
            next.onClick.RemoveAllListeners();
            next.onClick.AddListener(() => mc.StepHarmonyClassLevel(+1));
            foreach (var chevron in new[] { prev, next }) QuietChevron(chevron);
            screen.SetStones(stones);
            ScrollUiKit.Label(page, "HarmonySubLabel", string.Empty, 19, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.029f, 0.05f), new Vector2(0.70f, 0.42f), 12);

            // The perk box (Y changes it): a darker slip at the top right with the perk's name and what it does.
            var box = ScrollUiKit.Fill(page, "PerkBox", new Vector2(0.700f, 0.39f), new Vector2(0.971f, 0.90f),
                new Color(0.90f, 0.84f, 0.69f, 1f));
            ScrollUiKit.Label(box.transform, "PerkCaption", T("GameModes.PerkCaption").ToUpperInvariant(), 13, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.96f), 9);
            var perk = OptionsUiFactory.BuildButton("BtnHarmonyPerk", box.rectTransform, string.Empty, 18);
            SetRect((RectTransform)perk.transform, new Vector2(0.03f, 0.50f), new Vector2(0.97f, 0.80f), Vector2.zero, Vector2.zero);
            OptionsUiFactory.PaintPill(perk, new Color(0.84f, 0.76f, 0.58f, 1f), ScrollUiKit.Ink);
            // A 40 px pill: the label gets the height (the factory's 8 px padding would clip 18 pt to nothing).
            var perkLabel = perk.transform.Find("Label");
            ((RectTransform)perkLabel).offsetMin = new Vector2(10f, 2f);
            ((RectTransform)perkLabel).offsetMax = new Vector2(-10f, -2f);
            var perkText = perkLabel.GetComponent<Text>();
            perkText.resizeTextForBestFit = true;
            perkText.resizeTextMinSize = 11;
            perkText.resizeTextMaxSize = 18;
            perk.onClick.RemoveAllListeners();
            perk.onClick.AddListener(mc.CyclePerkOnClassSelect);
            ScrollUiKit.Label(box.transform, "HarmonyPerkDesc", string.Empty, 16, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.47f), 10);
            screen.SetPerkButton(perk);
            screen.HarmonyStep = mc.StepHarmonyClassLevel;
            screen.PerkCycle = mc.CyclePerkOnClassSelect;
        }

        // [REQ: UI-STYLE-025] Spirit Trials' slip carries its tiers (the mockup): one pill per tier with its best score; a
        // pill goes straight to Select Class for that tier. "Choose a tier" still opens the tier screen with each tier's rules.
        private void BuildTrialTiers(RectTransform page, MainMenuController mc, GameModesScreen screen)
        {
            var tiers = (SpiritTrialsTier[])Enum.GetValues(typeof(SpiritTrialsTier));
            var pills = new Button[tiers.Length];
            const float left = 0.029f, width = 0.160f, gap = 0.008f;
            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                var pill = OptionsUiFactory.BuildButton("BtnTrialTier_" + tier, page, string.Empty, 17);
                var x = left + i * (width + gap);
                SetRect((RectTransform)pill.transform, new Vector2(x, 0.09f), new Vector2(x + width, 0.31f), Vector2.zero, Vector2.zero);
                OptionsUiFactory.PaintPill(pill, new Color(0.90f, 0.84f, 0.69f, 1f), ScrollUiKit.Ink);
                var label = pill.transform.Find("Label");
                ((RectTransform)label).offsetMin = new Vector2(6f, 2f);
                ((RectTransform)label).offsetMax = new Vector2(-6f, -2f);
                var text = label.GetComponent<Text>();
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 11;
                text.resizeTextMaxSize = 17;
                pill.onClick.RemoveAllListeners();
                pill.onClick.AddListener(() => mc.StartSpiritTrials(tier));
                pills[i] = pill;
            }
            screen.SetTierButtons(pills);
        }
    }
}
