using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.UI
{
    // [REQ: ECON-GATE-001] [REQ: ECON-GATE-002] Gold at the boss gate (GoldEconomySystem lever L2): a row under the title
    // with the purse, the Warding Flame relic when held, "Strike a trial" (then a click on the trial to strike) and
    // "Lower intensity". A button that cannot be paid fades; a used one names what it did.
    public sealed partial class BossGateViewController
    {
        private Button _strikeBtn;
        private Button _calmBtn;
        private bool _striking;
        private readonly List<Button> _headerButtons = new List<Button>();

        private const float GateRowMin = 0.728f, GateRowMax = 0.790f, GateRowGap = 0.012f;

        private void BuildGateGoldRow(RectTransform scroll, RunDirector run)
        {
            _headerButtons.Clear();
            _wardingBtn = null;
            _strikeBtn = null;
            _calmBtn = null;
            if (run?.State == null) return;
            var x = 0.066f;
            var gold = run.BossGateGoldAllowed;   // outside a Garden Run the gate has no gold row

            if (gold)
            {
                var purse = ScrollUiKit.Label(scroll, "GatePurse", F("InRun.BossGate.Purse", run.State.CurrentGold), 20,
                    TextAnchor.MiddleLeft, ScrollUiKit.Ink, new Vector2(x, GateRowMin), new Vector2(x + 0.09f, GateRowMax), 13);
                purse.fontStyle = FontStyle.Bold;
                x += 0.09f + GateRowGap;
            }

            // WardingFlame relic: remove the first (lowest-tier) offered trial, once per run (kept in the save).
            if (RelicService.HasRelicOfType(run.State, RelicId.WardingFlame) && !run.State.WardingFlameUsed)
            {
                _wardingBtn = RowButton(scroll, "BtnWardingFlame", ref x, 0.20f, T("InRun.BossGate.WardingFlame"));
                ScrollUiKit.Icon(_wardingBtn.transform, "Icon", "relic/icon_warding_flame",
                    new Vector2(0.02f, 0.08f), new Vector2(0.17f, 0.92f), Color.white);
                ((RectTransform)_wardingBtn.transform.Find("Label")).anchorMin = new Vector2(0.18f, 0.05f);
                _wardingBtn.onClick.AddListener(() =>
                {
                    if (_confirming || _bossGateOptions.Count == 0 || !run.TryUseWardingFlame(_bossGateOptions[0])) return;
                    _bossGateOptions.RemoveAt(0);
                    _map?.SaveNow();
                    RebuildGate();
                });
            }
            if (!gold)
            {
                BuildGateStakes(scroll, x);
                return;
            }

            // A floor whose boss takes a single trial (Floor 1) has nothing to strike.
            run.GetBossModifierCounts(out _, out var basePicks);
            if (basePicks > 1)
            {
                var struck = run.BossTrialStruckThisFloor;
                _strikeBtn = RowButton(scroll, "BtnStrikeTrial", ref x, 0.21f,
                    struck ? T("InRun.BossGate.Struck")
                    : _striking ? T("InRun.BossGate.StrikeChoose")
                    : F("InRun.BossGate.Strike", run.GetBossStrikeCost(), XpTable.ModifierBonusPerMod));
                _strikeBtn.interactable = !struck && (_striking || run.CanStrikeBossTrial(_bossGateOptions.Count));
                _strikeBtn.onClick.AddListener(() =>
                {
                    if (_confirming || run.BossTrialStruckThisFloor) return;
                    _striking = !_striking && run.CanStrikeBossTrial(_bossGateOptions.Count);
                    RebuildGate();
                    if (_strikeBtn != null)
                        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                            _striking && _trialCards.Count > 0 ? _trialCards[0].Button.gameObject : _strikeBtn.gameObject);
                });
            }

            var baseIntensity = BossService.IntensityForRunNumber(run.State.RunNumber);
            if (baseIntensity > BossModifierIntensity.Low)
            {
                var lowered = run.BossIntensityLoweredThisFloor;
                _calmBtn = RowButton(scroll, "BtnLowerIntensity", ref x, 0.21f,
                    lowered ? T("InRun.BossGate.Calmed") : F("InRun.BossGate.Calm", run.GetBossCalmCost()));
                _calmBtn.interactable = !lowered && run.CanLowerBossIntensity();
                _calmBtn.onClick.AddListener(() =>
                {
                    if (_confirming || !run.TryLowerBossIntensity()) return;
                    _map?.SaveNow();
                    RebuildGate();
                    if (_calmBtn != null) UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_calmBtn.gameObject);
                });
            }
            BuildGateStakes(scroll, x);
        }

        // [REQ: STAKES-001] The stake of the fight, right of the gold row (RM-115): the boss pays more XP than a puzzle, and
        // each trial faced adds to it, so striking one costs that XP.
        private static void BuildGateStakes(RectTransform scroll, float x)
        {
            const float right = 0.775f;   // the boss's seal starts right of it
            if (right - x < 0.12f) return;
            var stakes = ScrollUiKit.Label(scroll, "GateStakes",
                F("InRun.BossGate.Stakes", XpTable.BossMultiplier, XpTable.ModifierBonusPerMod), 16, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(x, GateRowMin - 0.012f), new Vector2(right, GateRowMax + 0.012f), 11);
            stakes.fontStyle = FontStyle.Italic;
        }

        private Button RowButton(RectTransform scroll, string name, ref float x, float width, string label)
        {
            var button = ScrollUiKit.PillButton(scroll, name, new Vector2(x, GateRowMin), new Vector2(x + width, GateRowMax),
                label, 18, ScrollUiKit.PaperDeep, ScrollUiKit.Ink);
            x += width + GateRowGap;
            _headerButtons.Add(button);
            return button;
        }

        // [REQ: ECON-GATE-001] In strike mode a click on a trial pays and removes it; the boss then takes one trial fewer.
        private bool TryStrikeTrial(BossModifierId mod)
        {
            if (!_striking) return false;
            _striking = false;
            var run = _map?.Run;
            if (run != null && run.TryStrikeBossTrial(mod, _bossGateOptions.Count))
            {
                _bossGateOptions.Remove(mod);
                _bossPicksRequired = run.GetBossPicksRequired();
                _map?.SaveNow();
            }
            RebuildGate();
            return true;
        }

        // Same screen with the new state: no second fade; picks that left the offer are dropped.
        private void RebuildGate()
        {
            _selectedBossMods.RemoveAll(m => !_bossGateOptions.Contains(m));
            while (_selectedBossMods.Count > PicksRequired) _selectedBossMods.RemoveAt(_selectedBossMods.Count - 1);
            if (_bossGatePanel != null) Object.Destroy(_bossGatePanel);
            _bossGatePanel = null;
            BuildBossGatePanel(fadeIn: false);
        }

        // D-pad over the header row: left/right along it, down to the first trial.
        private void WireHeaderNavigation()
        {
            for (var i = 0; i < _headerButtons.Count; i++)
                _headerButtons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _headerButtons[i - 1] : null,
                    selectOnRight = i + 1 < _headerButtons.Count ? _headerButtons[i + 1] : null,
                    selectOnDown = CardAt(0, 0)
                };
        }

        private Button FirstHeaderButton => _headerButtons.Count > 0 ? _headerButtons[0] : null;
    }
}
