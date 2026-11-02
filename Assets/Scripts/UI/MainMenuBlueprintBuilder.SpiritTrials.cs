using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-035] [REQ: TRIAL-TIER-001] Spirit Trials · Four steles (owner's pick, RM-217): the tiers stand as stone
    // steles in the jizo forest, rising with the difficulty (name, stars, rules, HP, Pencil, par, best); the chosen one is
    // framed in gold and wears the red bib; its terms are on a slip below; "Choose a class" goes on to Select Class.
    // SpiritTrialsTierSelectController keeps the choice and fills the bests.
    public sealed partial class MainMenuBlueprintBuilder
    {
        private static readonly float[] SteleLeft = { 0.130f, 0.318f, 0.505f, 0.693f };
        private static readonly float[] SteleHeight = { 0.306f, 0.343f, 0.398f, 0.417f };
        private const float SteleWidth = 0.156f, SteleBottom = 0.315f;

        private GameObject BuildSpiritTrialsTierSelectPanel(RectTransform root, MainMenuController mc,
            SpiritTrialsTierSelectController trialsCtrl)
        {
            var pr = EnsureRect("SpiritTrialsTierSelectPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ScrollUiKit.DestroyChildrenNow(pr);
            var backdrop = EnsureOrGetImage(pr.gameObject, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_menu_trials_select"))
                backdrop.color = new Color(0.55f, 0.55f, 0.55f, 1f);

            ScrollUiKit.Banner(pr, "TrialsBanner", new Vector2(0.318f, 0.844f), new Vector2(0.682f, 0.957f),
                "TrialsEyebrow", T("Spirit Trials").ToUpperInvariant(), "Title", T("SpiritTrials.Choose"), 46);

            // HP, Pencil and par from SpiritTrialsTierSelectController.Terms; stars and rules from SpiritTrialsService.
            var terms = SpiritTrialsTierSelectController.Terms;
            var steles = new Button[terms.Length];
            var bests = new Text[terms.Length];
            for (var i = 0; i < terms.Length; i++)
            {
                var tier = (SpiritTrialsTier)i;
                (steles[i], bests[i]) = BuildStele(pr, i, tier, T("SpiritTrials.Tier." + tier), terms[i].Hp, terms[i].Pencil, terms[i].Par, mc);
            }

            var slip = ScrollUiKit.Fill(pr, "TrialsSlip", new Vector2(0.1875f, 0.169f), new Vector2(0.8125f, 0.278f), ScrollUiKit.Paper);
            slip.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);
            var slipTitle = ScrollUiKit.Label(slip.transform, "SlipTitle", string.Empty, 26, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.025f, 0.50f), new Vector2(0.975f, 0.92f), 14);
            slipTitle.supportRichText = true;
            var slipBody = ScrollUiKit.Label(slip.transform, "SlipBody", string.Empty, 19, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.025f, 0.10f), new Vector2(0.975f, 0.50f), 11);

            var back = ScrollUiKit.RimButton(pr, "BtnTrialsSelectBack", new Vector2(0.404f, 0.085f), new Vector2(0.466f, 0.139f),
                T("Back"), 22, ScrollUiKit.Gold, new Color(0.45f, 0.33f, 0.14f, 0.95f), ScrollUiKit.PaperText);
            back.onClick.AddListener(mc.BackFromPanel);
            var choose = ScrollUiKit.PillButton(pr, "BtnTrialsChoose", new Vector2(0.481f, 0.085f), new Vector2(0.596f, 0.139f),
                T("GameModes.ChooseClass"), 22, ScrollUiKit.Vermilion, Color.white, 26f);
            choose.onClick.AddListener(() => mc.StartSpiritTrials(trialsCtrl.Chosen));
            for (var i = 0; i < steles.Length; i++)
                steles[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? steles[i - 1] : null,
                    selectOnRight = i < steles.Length - 1 ? steles[i + 1] : null,
                    selectOnDown = choose,
                };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = choose, selectOnUp = steles[1] };
            choose.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = back, selectOnUp = steles[2] };

            var hint = ScrollUiKit.Label(pr, "TrialsKeyHint", string.Empty, 18, TextAnchor.MiddleCenter, new Color(0.85f, 0.79f, 0.66f, 1f),
                new Vector2(0.30f, 0.045f), new Vector2(0.70f, 0.078f), 11);
            DeviceHint.Attach(hint, "SpiritTrials.KeyHint");   // INPUT-HINT-001

            trialsCtrl.Configure(steles, bests, slipTitle, slipBody, terms.Length);
            pr.gameObject.SetActive(false);
            return pr.gameObject;
        }

        // A stele: an arched stone with the tier's name, stars, rules, HP, Pencil, par and best; the gold frame and the red
        // bib show when it is the chosen tier. A click begins this tier (on to Select Class).
        private (Button stele, Text best) BuildStele(RectTransform pr, int index, SpiritTrialsTier tier, string name, int hp, int pencil,
            string par, MainMenuController mc)
        {
            var top = SteleBottom + SteleHeight[index];
            var area = ScrollUiKit.Area(pr, $"TierRow_{index}", new Vector2(SteleLeft[index], SteleBottom),
                new Vector2(SteleLeft[index] + SteleWidth, top));
            var heightPx = SteleHeight[index] * 1080f;
            (Vector2, Vector2) Band(float fromTop, float height) =>
                (new Vector2(0.10f, 1f - (fromTop + height) / heightPx), new Vector2(0.90f, 1f - fromTop / heightPx));

            var frame = ScrollUiKit.Fill(area, "Frame", new Vector2(-0.025f, -0.02f), new Vector2(1.025f, 1.015f),
                new Color(0.91f, 0.77f, 0.42f, 1f), ScrollUiKit.Stele());
            frame.raycastTarget = false;
            var stone = ScrollUiKit.Fill(area, $"BtnTierStart_{index}", Vector2.zero, Vector2.one, new Color(0.47f, 0.47f, 0.43f, 1f),
                ScrollUiKit.Stele());
            stone.raycastTarget = true;
            var button = stone.gameObject.AddComponent<Button>();
            button.targetGraphic = stone;
            button.transition = Selectable.Transition.None;   // the frame and the bib show the choice
            button.onClick.AddListener(() => mc.StartSpiritTrials(tier));
            stone.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            ScrollUiKit.Fill(stone.transform, "Moss", Vector2.zero, new Vector2(1f, 0.055f), new Color(0.24f, 0.33f, 0.24f, 1f))
                .raycastTarget = false;
            var light = new Color(0.95f, 0.94f, 0.89f, 1f);
            var (nMin, nMax) = Band(52f, 40f);
            ScrollUiKit.Label(stone.transform, $"TierName_{index}", name, 30, TextAnchor.MiddleCenter, light, nMin, nMax, 16)
                .gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            var (sMin, sMax) = Band(94f, 30f);
            ScrollUiKit.Label(stone.transform, $"TierStars_{index}", new string('★', SpiritTrialsService.StarsFor(tier)), 24,
                TextAnchor.MiddleCenter, new Color(0.91f, 0.77f, 0.42f, 1f), sMin, sMax, 12);
            var rules = SpiritTrialsService.RulesFor(tier);
            var (rMin, rMax) = Band(126f, 24f);
            ScrollUiKit.Label(stone.transform, "TierRules", rules == 0 ? T("SpiritTrials.NoRule")
                    : LocalizationService.FormatPlural(rules, "SpiritTrials.Rule.One", "SpiritTrials.Rule.Many", "{0} rule", "{0} rules"),
                18, TextAnchor.MiddleCenter, new Color(0.85f, 0.82f, 0.75f, 1f), rMin, rMax, 10);
            var (fMin, fMax) = Band(160f, 100f);
            ScrollUiKit.Label(stone.transform, $"TierStats_{index}", T("SpiritTrials.Stele.Facts"), 19, TextAnchor.UpperLeft, light,
                fMin, fMax, 11).lineSpacing = 1.3f;
            ScrollUiKit.Label(stone.transform, "TierFacts", $"{hp}\n{pencil}\n{par}", 19, TextAnchor.UpperRight, light,
                fMin, fMax, 11).lineSpacing = 1.3f;
            var best = ScrollUiKit.Label(stone.transform, $"TierBest_{index}", string.Empty, 18, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.82f, 0.75f, 1f), new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.16f), 10);

            var bib = ScrollUiKit.Fill(area, "Bib", new Vector2(0.25f, 1f - 34f / heightPx), new Vector2(0.75f, 1.005f),
                ScrollUiKit.Vermilion, ScrollUiKit.Bib());
            bib.raycastTarget = false;

            return (button, best);
        }
    }
}
