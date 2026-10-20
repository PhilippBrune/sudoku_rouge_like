using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the boss gate modifier-choice panel and the cursed-node choice panel.
    /// </summary>
    public sealed partial class BossGateViewController : MonoBehaviour
    {
        private RunMapController _map;
        private RectTransform _uiRoot;

        private GameObject _bossGatePanel;
        private bool _awaitingBossGate;
        private List<BossModifierId> _bossGateOptions;
        private List<BossModifierId> _selectedBossMods;
        private int _bossPicksRequired;
        private readonly List<TrialCard> _trialCards = new List<TrialCard>();
        private Text _chosenLabel;
        private Button _confirmBtn;
        private Button _wardingBtn;
        private bool _confirming;

        private const int MaxTrialsPerRow = 4;

        // One offered trial on the scroll: its button, ink frame and ensō pick mark.
        private sealed class TrialCard
        {
            public BossModifierId Mod;
            public Button Button;
            public List<Image> Frame;
            public Image Ring;
            public Image Tick;
            public int Row;
            public int Column;
        }

        public Action<List<BossModifierId>> OnModsConfirmed;

        public bool IsAwaiting => _awaitingBossGate;
        public GameObject ActivePanel { get; private set; }

        public void Configure(RunMapController map, RectTransform uiRoot)
        {
            _map = map;
            _uiRoot = uiRoot;
            _bossGateOptions = new List<BossModifierId>();
            _selectedBossMods = new List<BossModifierId>();
        }

        public void ShowBossGateChoice()
        {
            if (_awaitingBossGate) return;
            var run = _map?.Run;
            if (run == null) return;

            _bossGateOptions.Clear();
            _selectedBossMods.Clear();
            var choices = run.RollBossModifierChoices();
            if (choices != null)
            {
                for (var i = 0; i < choices.Count; i++)
                    if (!_bossGateOptions.Contains(choices[i]))
                        _bossGateOptions.Add(choices[i]);
            }
            // [REQ: ECON-GATE-001] A trial struck (or removed by the Warding Flame) before Save & Quit stays removed.
            run.ApplyGateRemovals(_bossGateOptions);
            if (_bossGateOptions.Count == 0) return;

            _bossPicksRequired = run.GetBossPicksRequired();
            _awaitingBossGate = true;
            _confirming = false;
            _striking = false;
            BuildBossGatePanel(fadeIn: true);
        }

        // The cursed-tile scene (UI-STYLE-015) is in BossGateViewController.Cursed.cs.

        // [REQ: UI-STYLE-004] The boss gate as a trial scroll (layout-proposals.md, B): the gate painting darkened behind
        // a hand scroll with the boss's seal, one ink-framed panel per offered trial with an ensō that is ticked when
        // chosen, and the boss's curse, the count and "Face the <boss>" along the bottom edge.
        private void BuildBossGatePanel(bool fadeIn)
        {
            if (_bossGatePanel != null) Object.Destroy(_bossGatePanel);
            if (_uiRoot == null) return;
            var run = _map?.Run;
            var floor = run?.State?.CurrentFloor ?? 0;
            var bossName = FloorThemeData.GetBossName(floor);

            _bossGatePanel = new GameObject("BossGatePanel", typeof(RectTransform), typeof(Image));
            _bossGatePanel.transform.SetParent(_uiRoot, false);
            InRunUiFactory.StretchFill((RectTransform)_bossGatePanel.transform);
            _bossGatePanel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.03f, 1f);
            ScrollUiKit.Painting(_bossGatePanel.transform, "GateBackdrop", "bg_boss_gate", new Color(0.30f, 0.28f, 0.27f, 1f));

            var scroll = ScrollUiKit.HandScroll(_bossGatePanel.transform, "TrialScroll",
                new Vector2(0.086f, 0.150f), new Vector2(0.914f, 0.889f), "bg_boss_gate");
            BuildGateHeader(scroll, run, floor, bossName);
            BuildTrialCards(scroll, run);
            BuildGateFooter(scroll, run, bossName);
            WireGateNavigation();
            RefreshTrialMarks();

            ActivePanel = _bossGatePanel;
            if (fadeIn) FadeInPanel(_bossGatePanel);
            InRunUiFactory.SelectFirstInteractable(_bossGatePanel);
        }

        private void BuildGateHeader(RectTransform scroll, RunDirector run, int floor, string bossName)
        {
            ScrollUiKit.Label(scroll, "GateEyebrow",
                F("InRun.BossGate.Eyebrow", floor + 1, FloorThemeData.GetFloorName(floor)).ToUpperInvariant(),
                18, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.066f, 0.897f), new Vector2(0.62f, 0.932f), 12);
            ScrollUiKit.Label(scroll, "Title", F("InRun.BossGate.Heading", bossName), 46, TextAnchor.MiddleLeft,
                ScrollUiKit.Ink, new Vector2(0.066f, 0.815f), new Vector2(0.66f, 0.900f), 26);

            ScrollUiKit.Label(scroll, "GateChoose", LocalizationService.FormatPlural(PicksRequired,
                    "InRun.BossGate.Choose.One", "InRun.BossGate.Choose",
                    "Choose {0} of {1} trials", "Choose {0} of {1} trials", _bossGateOptions.Count),
                20, TextAnchor.MiddleRight, ScrollUiKit.Ink, new Vector2(0.50f, 0.872f), new Vector2(0.821f, 0.912f), 14);

            // [REQ: REQ-BOSS-103] Intensity under the count, with one dot per level (GAP-025); [REQ: ECON-GATE-002] after
            // paying, the lowered one.
            var state = run?.State;
            if (state != null)
            {
                var intensity = run.GetBossIntensity();
                ScrollUiKit.Label(scroll, "GateIntensity",
                    F("InRun.BossGate.Intensity", BossService.GetIntensityName(intensity)),
                    20, TextAnchor.MiddleRight, ScrollUiKit.Ink, new Vector2(0.50f, 0.832f), new Vector2(0.771f, 0.870f), 14);
                for (var i = 0; i < 4; i++)
                    ScrollUiKit.Spot(scroll, $"IntensityDot_{i}", new Vector2(0.781f + i * 0.012f, 0.851f),
                        new Vector2(14f, 14f), i <= (int)intensity ? ScrollUiKit.Ink : new Color(0.13f, 0.11f, 0.09f, 0.18f),
                        ScrollUiKit.Disc());
            }

            // The boss's seal: its avatar on a vermilion stamp, slightly turned like a hand-pressed seal.
            var seal = ScrollUiKit.Spot(scroll, "BossSeal", new Vector2(0.886f, 0.846f), new Vector2(150f, 150f),
                ScrollUiKit.Vermilion, ScrollUiKit.Rounded(6f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            var sealInner = ScrollUiKit.Area(seal.transform, "SealInner", Vector2.zero, Vector2.one);
            sealInner.offsetMin = new Vector2(9f, 9f);
            sealInner.offsetMax = new Vector2(-9f, -9f);
            ScrollUiKit.InkFrame(sealInner, new Color(0.98f, 0.93f, 0.85f, 0.9f), 3f);
            ScrollUiKit.Icon(seal.transform, "BossAvatar", "boss/icon_" + FloorThemeData.GetBossKey(floor),
                new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Color.white);

            // The purse, the Warding Flame and the gate's gold actions (BossGateViewController.Gold.cs).
            BuildGateGoldRow(scroll, run);
        }

        private void BuildTrialCards(RectTransform scroll, RunDirector run)
        {
            _trialCards.Clear();
            var seenMods = run?.State?.SeenBossModifiers;
            // [REQ: HARMONY-FLAG-001] H5+ never reveals seen modifiers; the Lantern of Void relic hides them too.
            var masked = run?.State != null
                && (HarmonyDifficultyService.BuildConfig(run.State.HarmonyLevel).HintMaskingEnabled
                    || RelicService.ShouldHideBossModifiers(run.State));

            // Up to four trials in one row; more (Floor 4+, Harmony bonus) in two rows of compact panels.
            var count = _bossGateOptions.Count;
            var rows = count <= MaxTrialsPerRow ? 1 : 2;
            var columns = rows == 1 ? count : (count + 1) / 2;
            const float areaMin = 0.067f, areaMax = 0.933f, gap = 0.025f;
            var cardWidth = Mathf.Min(0.215f, (areaMax - areaMin - (columns - 1) * gap) / columns);
            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var column = i % columns;
                var inRow = row == rows - 1 ? count - row * columns : columns;
                var xMin = 0.5f - (inRow * cardWidth + (inRow - 1) * gap) * 0.5f + column * (cardWidth + gap);
                var (yMin, yMax) = rows == 1 ? (0.253f, 0.698f) : row == 0 ? (0.487f, 0.698f) : (0.258f, 0.469f);

                var mod = _bossGateOptions[i];
                var seen = !masked && seenMods != null && seenMods.Contains(mod);
                Debug.Log(
                    $"[ModifierDiscovery] Boss gate label: mod={mod}, seen={seen}, " +
                    $"runSeen={ModifierDiscoveryService.Describe(seenMods)}, label={(seen ? "revealed" : "???")}");
                var card = BuildTrialCard(scroll, i, mod, seen, new Vector2(xMin, yMin),
                    new Vector2(xMin + cardWidth, yMax), compact: rows > 1);
                card.Row = row;
                card.Column = column;
                _trialCards.Add(card);
            }
        }

        private TrialCard BuildTrialCard(RectTransform scroll, int index, BossModifierId mod, bool seen,
            Vector2 min, Vector2 max, bool compact)
        {
            var bg = ScrollUiKit.Fill(scroll, $"Mod_{index}", min, max, Color.white);
            bg.raycastTarget = true;
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.colors = new ColorBlock
            {
                normalColor = new Color(0.84f, 0.78f, 0.66f, 0.30f),
                highlightedColor = new Color(0.96f, 0.86f, 0.62f, 0.55f),
                selectedColor = new Color(0.97f, 0.83f, 0.52f, 0.65f),
                pressedColor = new Color(0.80f, 0.64f, 0.42f, 0.70f),
                disabledColor = new Color(0.84f, 0.78f, 0.66f, 0.15f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            var card = new TrialCard { Mod = mod, Button = btn, Frame = ScrollUiKit.InkFrame(bg.transform, ScrollUiKit.Ink, 3f) };

            var iconName = seen ? BossService.GetIconName(mod) : string.Empty;
            if (seen && string.IsNullOrEmpty(iconName))
                Debug.LogWarning($"[BossGateViewController] No icon mapped for seen modifier: {mod}");
            var iconPath = seen ? BossService.GetIconFolder(mod) + "/icon_" + iconName : "ui/icon_petal_orb";
            var (iconMin, iconMax) = compact
                ? (new Vector2(0.05f, 0.50f), new Vector2(0.27f, 0.92f))
                : (new Vector2(0.30f, 0.61f), new Vector2(0.70f, 0.88f));
            var icon = ScrollUiKit.Icon(bg.transform, "Icon", iconPath, iconMin, iconMax,
                seen ? Color.white : new Color(1f, 1f, 1f, 0.35f));

            var name = seen ? BossService.GetModifierName(mod) : "???";
            var rule = seen ? BossService.GetModifierDescription(mod) : T("InRun.BossGate.UnknownRule");
            if (compact)
            {
                ScrollUiKit.Label(bg.transform, "Name", name, 22, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                    new Vector2(0.30f, 0.56f), new Vector2(0.78f, 0.92f), 14);
                ScrollUiKit.Label(bg.transform, "Rule", rule, 16, TextAnchor.UpperLeft, ScrollUiKit.InkSoft,
                    new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.48f), 11);
            }
            else
            {
                ScrollUiKit.Label(bg.transform, "Name", name, 26, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                    new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.57f), 16);
                ScrollUiKit.Label(bg.transform, "Rule", rule, 18, TextAnchor.UpperCenter, ScrollUiKit.InkSoft,
                    new Vector2(0.07f, 0.27f), new Vector2(0.93f, 0.45f), 12);
            }

            var ringCenter = compact ? new Vector2(0.88f, 0.74f) : new Vector2(0.5f, 0.155f);
            var ringSize = compact ? 54f : 76f;
            card.Ring = ScrollUiKit.Spot(bg.transform, "PickRing", ringCenter, new Vector2(ringSize, ringSize),
                ScrollUiKit.InkSoft, ScrollUiKit.Enso());
            card.Tick = ScrollUiKit.Spot(card.Ring.transform, "PickTick", new Vector2(0.5f, 0.5f),
                new Vector2(ringSize, ringSize), ScrollUiKit.Vermilion, ScrollUiKit.Tick());

            // Alt Constraint Symbols: the rule's abbreviation over the icon, for players who read symbols poorly.
            if (seen && OptionsController.LiveAccessibility != null
                && OptionsController.LiveAccessibility.AlternativeConstraintSymbols)
            {
                var abbr = ScrollUiKit.Label(icon.transform, "AbbrLabel", BossService.GetConstraintAbbreviation(mod), 22,
                    TextAnchor.MiddleCenter, GamePalette.WinGold, Vector2.zero, Vector2.one, 0, FontStyle.Bold);
                abbr.gameObject.AddComponent<Outline>().effectColor = ScrollUiKit.Ink;
            }

            btn.onClick.AddListener(() => ToggleBossModSelection(mod));
            return card;
        }

        private void BuildGateFooter(RectTransform scroll, RunDirector run, string bossName)
        {
            ScrollUiKit.Line(scroll, "GateRule", new Vector2(0.066f, 0.188f), new Vector2(0.934f, 0.188f),
                new Color(0.70f, 0.20f, 0.15f, 0.85f), 2f);

            // [REQ: REQ-BOSS-108] From Floor 3 the boss's curse (its debuff) is written in the margin (GAP-026).
            if (run?.GetBossDebuffForCurrentFloor() is { } debuff)
            {
                ScrollUiKit.Icon(scroll, "GateDebuffIcon",
                    BossService.GetIconFolder(debuff) + "/icon_" + BossService.GetIconName(debuff),
                    new Vector2(0.080f, 0.095f), new Vector2(0.120f, 0.150f), Color.white);
                ScrollUiKit.Label(scroll, "GateDebuff",
                    F("InRun.BossGate.Debuff", BossService.GetModifierName(debuff), BossService.GetModifierDescription(debuff)),
                    20, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion, new Vector2(0.130f, 0.080f), new Vector2(0.60f, 0.165f), 13);
            }

            _chosenLabel = ScrollUiKit.Label(scroll, "GateChosenCount", string.Empty, 20, TextAnchor.MiddleRight,
                ScrollUiKit.Ink, new Vector2(0.60f, 0.085f), new Vector2(0.738f, 0.160f), 14);

            // Confirm stays visible but faded until enough trials are chosen.
            _confirmBtn = ScrollUiKit.PillButton(scroll, "ConfirmBossBtn", new Vector2(0.752f, 0.085f),
                new Vector2(0.918f, 0.160f), F("InRun.BossGate.Face", bossName), 24, ScrollUiKit.Vermilion,
                new Color(0.99f, 0.95f, 0.88f, 1f));
            _confirmBtn.interactable = false;
            _confirmBtn.onClick.AddListener(ConfirmBossGateAll);
        }

        // Explicit D-pad / arrow navigation over the panel grid: Down from the last row goes to Confirm, Up from the
        // first row to the Warding Flame button when there is one.
        private void WireGateNavigation()
        {
            for (var i = 0; i < _trialCards.Count; i++)
            {
                var card = _trialCards[i];
                card.Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = CardAt(card.Row, card.Column - 1),
                    selectOnRight = CardAt(card.Row, card.Column + 1),
                    selectOnUp = card.Row > 0 ? CardAt(card.Row - 1, card.Column) : FirstHeaderButton,
                    selectOnDown = (Selectable)CardAt(card.Row + 1, card.Column) ?? _confirmBtn
                };
            }
            var lastRow = _trialCards.Count > 0 ? _trialCards[_trialCards.Count - 1].Row : 0;
            var lastInRow = _trialCards.FindLast(c => c.Row == lastRow);
            _confirmBtn.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = lastInRow?.Button };
            WireHeaderNavigation();
        }

        private Button CardAt(int row, int column)
        {
            for (var i = 0; i < _trialCards.Count; i++)
                if (_trialCards[i].Row == row && _trialCards[i].Column == column) return _trialCards[i].Button;
            return null;
        }

        private void ToggleBossModSelection(BossModifierId mod)
        {
            if (_confirming) return; // the picks are fixed once Confirm is pressed
            if (TryStrikeTrial(mod)) return; // strike mode: this click strikes the trial (ECON-GATE-001)
            if (_selectedBossMods.Contains(mod))
                _selectedBossMods.Remove(mod);
            else if (_selectedBossMods.Count < PicksRequired)
                _selectedBossMods.Add(mod);
            else if (PicksRequired == 1)
            {
                // With a single pick, choosing another trial moves the pick instead of doing nothing.
                _selectedBossMods.Clear();
                _selectedBossMods.Add(mod);
            }
            RefreshTrialMarks();
        }

        // Chosen trials get a vermilion ensō with a tick and a vermilion frame; Confirm unlocks at the required count.
        private void RefreshTrialMarks()
        {
            for (var i = 0; i < _trialCards.Count; i++)
            {
                var card = _trialCards[i];
                var chosen = _selectedBossMods.Contains(card.Mod);
                card.Ring.color = chosen ? ScrollUiKit.Vermilion : new Color(0.33f, 0.29f, 0.24f, 0.55f);
                card.Tick.gameObject.SetActive(chosen);
                var tint = chosen ? ScrollUiKit.Vermilion : ScrollUiKit.Ink;
                for (var f = 0; f < card.Frame.Count; f++)
                    card.Frame[f].color = new Color(tint.r, tint.g, tint.b, card.Frame[f].color.a);
            }
            if (_chosenLabel != null)
                _chosenLabel.text = F("InRun.BossGate.Chosen", _selectedBossMods.Count, PicksRequired);
            if (_confirmBtn != null)
                _confirmBtn.interactable = !_striking && _selectedBossMods.Count >= PicksRequired;
        }

        // One confirm only: the cards, the Warding Flame and Confirm lock for the short flash, so a double click or a
        // late card click cannot confirm twice or change the picks.
        private void ConfirmBossGateAll()
        {
            if (_confirming || !_awaitingBossGate) return;
            _confirming = true;
            for (var i = 0; i < _trialCards.Count; i++) _trialCards[i].Button.interactable = false;
            foreach (var button in _headerButtons) button.interactable = false;
            if (_confirmBtn != null && isActiveAndEnabled)
                StartCoroutine(FlashAndAdvance(_confirmBtn));
            else
                FinishConfirm();
        }

        private System.Collections.IEnumerator FlashAndAdvance(Button btn)
        {
            btn.interactable = false;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 0.85f, 0.55f, 1f);
            yield return new WaitForSecondsRealtime(0.3f);
            FinishConfirm();
        }

        private void FinishConfirm()
        {
            if (!_awaitingBossGate) return;
            _confirming = false;
            _awaitingBossGate = false;
            ActivePanel = null;
            var run = _map?.Run;
            if (run != null) run.ChooseBossModifiers(new List<BossModifierId>(_selectedBossMods));
            _map?.SaveNow();
            if (_bossGatePanel != null) { Object.Destroy(_bossGatePanel); _bossGatePanel = null; }

            OnModsConfirmed?.Invoke(new List<BossModifierId>(_selectedBossMods));
        }

        private void FadeInPanel(GameObject panel)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            if (!isActiveAndEnabled)
            {
                cg.alpha = 1f; // no coroutine can run: show the panel at once rather than leave it invisible
                return;
            }
            cg.alpha = 0f;
            StartCoroutine(AnimationHelper.FadeIn(cg, AnimationHelper.MenuPanelDuration));
        }

        // Never more picks than offered trials (the Warding Flame removes one; a struck trial lowers the picks too), so
        // Confirm can always unlock.
        private int PicksRequired => Mathf.Min(_bossPicksRequired, _bossGateOptions.Count);

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);
    }
}
