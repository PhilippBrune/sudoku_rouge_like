using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-035] The Spirit Trials steles: keeps the chosen tier (pointer or pad focus), frames it with the gold
    /// ring and the red bib, writes its terms on the slip, and shows each tier's best. Configure() once during the panel
    /// build, Refresh(stats) each time the panel is shown.
    /// </summary>
    public sealed class SpiritTrialsTierSelectController : MonoBehaviour
    {
        /// <summary>[REQ: TRIAL-TIER-001] HP, Pencil and par per tier (SpiritTrialsMode tier table), for the steles and the slip.</summary>
        internal static readonly (int Hp, int Pencil, string Par)[] Terms = { (5, 15, "5:00"), (4, 12, "7:00"), (3, 10, "9:00"), (2, 8, "12:00") };

        private Button[] _steles = new Button[0];
        private Text[] _bests = new Text[0];
        private Text _slipTitle, _slipBody;

        public SpiritTrialsTier Chosen { get; private set; } = SpiritTrialsTier.Apprentice;

        public void Configure(Button[] steles, Text[] bests, Text slipTitle, Text slipBody, int count)
        {
            _steles = steles;
            _bests = bests;
            _slipTitle = slipTitle;
            _slipBody = slipBody;
            for (var i = 0; i < count; i++)
            {
                var tier = (SpiritTrialsTier)i;
                var trigger = steles[i].gameObject.AddComponent<EventTrigger>();
                foreach (var type in new[] { EventTriggerType.Select, EventTriggerType.PointerEnter })
                {
                    var entry = new EventTrigger.Entry { eventID = type };
                    entry.callback.AddListener(_ => Choose(tier));
                    trigger.triggers.Add(entry);
                }
            }
            Choose(Chosen);
        }

        public void Refresh(ProfileStats stats)
        {
            if (stats == null) return;
            var scores = new[] { stats.TrialsBestScore_Apprentice, stats.TrialsBestScore_Adept, stats.TrialsBestScore_Master,
                stats.TrialsBestScore_Grandmaster };
            for (var i = 0; i < _bests.Length && i < scores.Length; i++)
                if (_bests[i] != null)
                    _bests[i].text = scores[i] > 0
                        ? LocalizationService.Format("SpiritTrials.BestScore", "Best: {0:N0}", scores[i])
                        : LocalizationService.T("SpiritTrials.NotPassed", "Not passed yet");
            Choose(Chosen);
            if (_steles.Length > (int)Chosen && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_steles[(int)Chosen].gameObject);
        }

        private void Choose(SpiritTrialsTier tier)
        {
            Chosen = tier;
            for (var i = 0; i < _steles.Length; i++)
            {
                if (_steles[i] == null) continue;
                var area = _steles[i].transform.parent;
                area.Find("Frame")?.gameObject.SetActive(i == (int)tier);
                area.Find("Bib")?.gameObject.SetActive(i == (int)tier);
            }
            var index = Mathf.Clamp((int)tier, 0, Terms.Length - 1);
            var name = LocalizationService.T("SpiritTrials.Tier." + tier);
            var stars = new string('★', SpiritTrialsService.StarsFor(tier));
            var rules = SpiritTrialsService.RulesFor(tier);
            if (_slipTitle != null)
                _slipTitle.text = LocalizationService.Format(rules == 0 ? "SpiritTrials.Slip.NoRule" : rules == 1 ? "SpiritTrials.Slip.OneRule"
                        : "SpiritTrials.Slip.Rules", "<color=#9e2a2b>{0}</color> · one 9×9 board, {1}, {2} rules drawn by the trial",
                    name, stars, rules);
            if (_slipBody != null)
                _slipBody.text = LocalizationService.Format("SpiritTrials.Slip.Body",
                    "HP {0} · Pencil {1} · finish under {2} for the time bonus. No XP, items or relics; the best score per tier is kept.",
                    Terms[index].Hp, Terms[index].Pencil, Terms[index].Par);
        }
    }
}
