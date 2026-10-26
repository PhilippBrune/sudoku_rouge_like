using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-025] Game Modes · Five paths (owner pick 2026-10-21): the five modes as noren curtains from a wooden
    /// beam, each with its icon, name, one line on what it is and its own state on a paper strip (a locked mode dark, with
    /// what opens it); the focused mode's slip below with its settings and the way in. A click (or A) on a curtain enters
    /// the mode as the old buttons did; focus or the pointer shows its slip. Built by MainMenuBlueprintBuilder.GameModes;
    /// MainMenuController fills the texts (RefreshGameModesPanel).
    /// </summary>
    public sealed class GameModesScreen : MonoBehaviour
    {
        public enum Mode { Garden, Endless, Trials, Daily, Monthly }

        /// <summary>One curtain's state and its slip's body.</summary>
        public readonly struct ModeView
        {
            public readonly bool Unlocked;
            public readonly string State;
            public readonly string Body;
            public readonly string EnterLabel;   // null: the label the slip was built with
            public readonly string Side;         // the slip's side box (UI-STYLE-027), null: none

            public ModeView(bool unlocked, string state, string body, string enterLabel = null, string side = null)
            {
                Unlocked = unlocked;
                State = state;
                Body = body;
                EnterLabel = enterLabel;
                Side = side;
            }
        }

        /// <summary>The Garden Run slip: the chosen Harmony level, its rule and the next level's condition, the perk.</summary>
        public readonly struct HarmonyView
        {
            public readonly int Level;
            public readonly int MaxUnlocked;
            public readonly string Title;
            public readonly string Body;
            public readonly string State;
            public readonly string PerkName;
            public readonly string PerkDescription;
            public readonly bool PerkUnlocked;

            public HarmonyView(int level, int maxUnlocked, string title, string body, string state, string perkName,
                string perkDescription, bool perkUnlocked)
            {
                Level = level;
                MaxUnlocked = maxUnlocked;
                Title = title;
                Body = body;
                State = state;
                PerkName = perkName;
                PerkDescription = perkDescription;
                PerkUnlocked = perkUnlocked;
            }
        }

        private static readonly Color Cloth = new Color(0.19f, 0.28f, 0.23f, 1f);
        private static readonly Color ClothChosen = new Color(0.56f, 0.17f, 0.14f, 1f);
        private static readonly Color ClothLocked = new Color(0.23f, 0.21f, 0.19f, 1f);
        private static readonly Color StoneReached = new Color(0.81f, 0.78f, 0.69f, 1f);
        private static readonly Color StoneLocked = new Color(0.60f, 0.58f, 0.52f, 0.45f);
        private static readonly Color StripPaper = new Color(0.94f, 0.89f, 0.78f, 0.95f);
        private static readonly Color StripLocked = new Color(0.24f, 0.20f, 0.16f, 0.92f);
        private static readonly Color Amber = new Color(0.88f, 0.64f, 0.29f, 1f);

        private readonly Dictionary<Mode, Button> _curtains = new Dictionary<Mode, Button>();
        private readonly Dictionary<Mode, GameObject> _slips = new Dictionary<Mode, GameObject>();
        private readonly Dictionary<Mode, bool> _unlocked = new Dictionary<Mode, bool>();
        private Image[] _stones = Array.Empty<Image>();
        private Button _perk;
        private Button _records;
        private Button _back;
        private Button[] _tiers = Array.Empty<Button>();
        private Mode _shown = Mode.Garden;

        /// <summary>LB / RB (PageUp / PageDown) on the Garden Run slip: the Harmony level down or up.</summary>
        public Action<int> HarmonyStep;
        /// <summary>Y (F) on the Garden Run slip: the next perk.</summary>
        public Action PerkCycle;

        public void Register(Mode mode, Button curtain, GameObject slip)
        {
            _curtains[mode] = curtain;
            _slips[mode] = slip;
            _unlocked[mode] = true;
        }

        public void SetStones(Image[] stones) => _stones = stones ?? Array.Empty<Image>();

        public void SetPerkButton(Button perk) => _perk = perk;

        private void Update()
        {
            if (_shown != Mode.Garden || !_slips.TryGetValue(Mode.Garden, out var slip) || slip == null || !slip.activeInHierarchy)
                return;
            var (step, perk) = HarmonyHotkeys.Read();
            if (step != 0) HarmonyStep?.Invoke(step);
            if (perk && _perk != null && _perk.interactable) PerkCycle?.Invoke();
        }

        /// <summary>The Records pill under Endless Zen and Back, for the D-pad links (<see cref="WireNavigation"/>).</summary>
        public void SetNavigationButtons(Button records, Button back)
        {
            _records = records;
            _back = back;
        }

        /// <summary>The Spirit Trials tier pills on its slip.</summary>
        public void SetTierButtons(Button[] tiers) => _tiers = tiers ?? Array.Empty<Button>();

        /// <summary>Each tier pill's text (the tier and its best score); the pills work only while Spirit Trials is open.</summary>
        public void ShowTiers(IReadOnlyList<string> labels, bool unlocked)
        {
            for (var i = 0; i < _tiers.Length; i++)
            {
                if (_tiers[i] == null) continue;
                _tiers[i].interactable = unlocked;
                SetText(_tiers[i].transform, "Label", i < labels.Count ? labels[i] : string.Empty);
            }
            WireNavigation();
        }

        /// <summary>A Harmony level as the stones show it: 0, I … IX, VG.</summary>
        public static string Numeral(int level) =>
            level <= 0 ? "0" : level >= 10 ? "VG" : new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" }[level - 1];

        // D-pad: left and right along the curtains (a locked one too, to read what opens it); down from a curtain to the
        // way in, or Records under Endless Zen, or Back when the mode is locked; on the Garden Run slip the perk and the
        // Harmony chevrons. Rewired on every refresh, so focus never lands on a button that is switched off.
        private void WireNavigation()
        {
            var modes = (Mode[])Enum.GetValues(typeof(Mode));
            for (var i = 0; i < modes.Length; i++)
            {
                if (!_curtains.TryGetValue(modes[i], out var curtain) || curtain == null) continue;
                var enter = EnterOf(modes[i]);
                var records = modes[i] == Mode.Endless && Usable(_records) ? _records : null;
                var firstTier = modes[i] == Mode.Trials && _tiers.Length > 0 && Usable(_tiers[0]) ? _tiers[0] : null;
                curtain.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _curtains[modes[i - 1]] : null,
                    selectOnRight = i + 1 < modes.Length ? _curtains[modes[i + 1]] : null,
                    selectOnDown = records != null ? records : firstTier != null ? firstTier : Usable(enter) ? enter : _back
                };
                if (records != null)
                    records.navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit, selectOnUp = curtain, selectOnDown = Usable(enter) ? enter : _back
                    };
                if (enter == null) continue;
                var above = modes[i] == Mode.Garden && Usable(_perk) ? _perk : records != null ? (Selectable)records : curtain;
                enter.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = above, selectOnLeft = _back };
            }
            WireGardenSlip();
            WireTrialTiers();
            WireBack();
        }

        // The tier pills in a row, then Back and "Choose a tier"; up goes to the Spirit Trials curtain.
        private void WireTrialTiers()
        {
            if (_tiers.Length == 0 || !_curtains.TryGetValue(Mode.Trials, out var curtain)) return;
            var enter = EnterOf(Mode.Trials);
            for (var i = 0; i < _tiers.Length; i++)
                if (_tiers[i] != null)
                    _tiers[i].navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit, selectOnUp = curtain, selectOnDown = Usable(enter) ? enter : _back,
                        selectOnLeft = i > 0 ? _tiers[i - 1] : null,
                        selectOnRight = i + 1 < _tiers.Length ? _tiers[i + 1] : (Selectable)_back
                    };
        }

        private void WireGardenSlip()
        {
            if (!_slips.TryGetValue(Mode.Garden, out var slip) || slip == null || !_curtains.TryGetValue(Mode.Garden, out var curtain))
                return;
            var enter = EnterOf(Mode.Garden);
            var prev = FindButton(slip.transform, "BtnHarmonyPrev");
            var next = FindButton(slip.transform, "BtnHarmonyNext");
            if (_perk != null)
                _perk.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit, selectOnUp = curtain, selectOnDown = enter, selectOnLeft = next
                };
            if (next != null)
                next.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit, selectOnUp = curtain, selectOnDown = _back, selectOnLeft = prev,
                    selectOnRight = Usable(_perk) ? _perk : (Selectable)enter
                };
            if (prev != null)
                prev.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit, selectOnUp = curtain, selectOnDown = _back, selectOnRight = next
                };
        }

        // Back leads up to the shown mode's curtain and right to its way in.
        private void WireBack()
        {
            if (_back == null || !_curtains.TryGetValue(_shown, out var curtain)) return;
            var enter = EnterOf(_shown);
            var lastTier = _shown == Mode.Trials && _tiers.Length > 0 && Usable(_tiers[_tiers.Length - 1]) ? _tiers[_tiers.Length - 1] : null;
            _back.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnUp = curtain, selectOnRight = Usable(enter) ? enter : null,
                selectOnLeft = lastTier
            };
        }

        private Button EnterOf(Mode mode) =>
            _slips.TryGetValue(mode, out var slip) && slip != null ? FindEnter(slip.transform) : null;

        private static bool Usable(Selectable selectable) => selectable != null && selectable.interactable;

        private static Button FindButton(Transform parent, string name)
        {
            var found = parent.Find(name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        public Mode Shown => _shown;

        /// <summary>The curtains' states and the slips' bodies.</summary>
        public void Refresh(IReadOnlyDictionary<Mode, ModeView> views)
        {
            foreach (var pair in views)
            {
                _unlocked[pair.Key] = pair.Value.Unlocked;
                if (_curtains.TryGetValue(pair.Key, out var curtain) && curtain != null)
                {
                    var unlocked = pair.Value.Unlocked;
                    curtain.interactable = unlocked;
                    SetText(curtain.transform, "State/StateText", pair.Value.State);
                    var strip = curtain.transform.Find("State");
                    if (strip != null) strip.GetComponent<Image>().color = unlocked ? StripPaper : StripLocked;
                    var stateText = curtain.transform.Find("State/StateText");
                    if (stateText != null) stateText.GetComponent<Text>().color = unlocked ? ScrollUiKit.Ink : Amber;
                    var icon = curtain.transform.Find("Icon");
                    if (icon != null) icon.GetComponent<Image>().color = unlocked ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.8f);
                }
                if (_slips.TryGetValue(pair.Key, out var slip) && slip != null)
                {
                    SetText(slip.transform, "Body", pair.Value.Body);
                    if (pair.Value.Side != null) SetText(slip.transform, "SideBox/SideText", pair.Value.Side);
                    var enter = FindEnter(slip.transform);
                    if (enter != null)
                    {
                        enter.interactable = pair.Value.Unlocked;
                        if (pair.Value.EnterLabel != null) SetText(enter.transform, "Label", pair.Value.EnterLabel);
                    }
                }
            }
            Show(_shown);
            WireNavigation();
        }

        /// <summary>The slip's side box, filled later (the day's board when Steam answers).</summary>
        public void SetSide(Mode mode, string text)
        {
            if (_slips.TryGetValue(mode, out var slip) && slip != null) SetText(slip.transform, "SideBox/SideText", text);
        }

        /// <summary>Shows the slip of a mode and marks its curtain (vermilion cloth).</summary>
        public void Show(Mode mode)
        {
            _shown = mode;
            foreach (var pair in _curtains)
            {
                if (pair.Value == null) continue;
                var cloth = pair.Value.GetComponent<Image>();
                var unlocked = !_unlocked.TryGetValue(pair.Key, out var u) || u;
                if (cloth != null) cloth.color = !unlocked ? ClothLocked : pair.Key == mode ? ClothChosen : Cloth;
                var glow = pair.Value.transform.parent != null ? pair.Value.transform.parent.Find("CurtainGlow_" + pair.Key) : null;
                if (glow != null) glow.GetComponent<Image>().color = new Color(1f, 0.82f, 0.48f, pair.Key == mode ? 0.45f : 0f);
            }
            foreach (var pair in _slips)
                if (pair.Value != null) pair.Value.SetActive(pair.Key == mode);
            WireBack();
        }

        /// <summary>The Garden Run slip and curtain strip; the stones: reached ones light, the chosen one vermilion.</summary>
        public void ShowHarmony(in HarmonyView view)
        {
            for (var i = 0; i < _stones.Length; i++)
            {
                if (_stones[i] == null) continue;
                _stones[i].color = i == view.Level ? ScrollUiKit.Vermilion : i <= view.MaxUnlocked ? StoneReached : StoneLocked;
                var numeral = _stones[i].transform.Find("Numeral");
                if (numeral != null) numeral.GetComponent<Text>().color = i == view.Level ? Color.white : ScrollUiKit.InkSoft;
            }
            if (_curtains.TryGetValue(Mode.Garden, out var curtain) && curtain != null)
                SetText(curtain.transform, "State/StateText", view.State);
            if (!_slips.TryGetValue(Mode.Garden, out var slip) || slip == null) return;
            SetText(slip.transform, "HarmonySelectedLabel", view.Title);
            SetText(slip.transform, "HarmonySubLabel", view.Body);
            SetText(slip.transform, "PerkBox/HarmonyPerkDesc", view.PerkDescription);
            if (_perk != null)
            {
                _perk.interactable = view.PerkUnlocked;
                SetText(_perk.transform, "Label", view.PerkName);
            }
            SetInteractable(slip.transform, "BtnHarmonyPrev", view.Level > 0);
            SetInteractable(slip.transform, "BtnHarmonyNext", view.Level < view.MaxUnlocked);
            WireNavigation();
        }

        private static void SetInteractable(Transform parent, string path, bool interactable)
        {
            var button = parent.Find(path);
            if (button != null) button.GetComponent<Button>().interactable = interactable;
        }

        private static Button FindEnter(Transform slip)
        {
            foreach (var button in slip.GetComponentsInChildren<Button>(true))
                if (button.name.EndsWith("Enter", StringComparison.Ordinal)) return button;
            return null;
        }

        private static void SetText(Transform parent, string path, string text)
        {
            var child = parent.Find(path);
            var label = child != null ? child.GetComponent<Text>() : null;
            if (label != null) label.text = text ?? string.Empty;
        }
    }
}
