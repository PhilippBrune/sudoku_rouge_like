using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-008] The relic node as the offering table at the shrine (layout-proposals.md, "Relic node"): the
    // same offerings as after a boss (UI-STYLE-005), a banner naming the floor, the run's HP, gold and relic count on a
    // paper tag and the relics already held on another, so a duplicate is easy to spot. Take takes a relic at once;
    // "Leave them" takes none.
    public sealed partial class RewardViewController
    {
        private GameObject _relicShrine;
        private bool _relicChosen;

        public void ShowRelicChoicePanel(List<RelicInstance> choices, System.Action onDismissed = null)
        {
            var run = _map?.Run;
            if (run?.State == null || _pathPanel == null || choices == null || choices.Count == 0) return;
            var state = run.State;
            CloseRelicShrine();
            _relicChosen = false;

            var screen = ScrollUiKit.Scene(_pathPanel, "RelicChoicePanel", "bg_relic", new Color(0.62f, 0.60f, 0.56f, 1f));
            _relicShrine = screen.gameObject;

            ScrollUiKit.Banner(screen.transform, "RelicShrineBanner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "RelicShrineEyebrow", F("InRun.BossDefeated.Eyebrow", state.CurrentFloor + 1, state.TotalFloors,
                    FloorThemeData.GetFloorName(state.CurrentFloor)),
                "RelicShrineTitle", T("InRun.Relic.ShrineTitle"));
            BuildRunTag(screen.transform, state);
            BuildHeldRelicsTag(screen.transform, state);

            var prompt = ScrollUiKit.Label(screen.transform, "RelicShrinePrompt", T("InRun.Relic.Offers"), 30, TextAnchor.MiddleCenter,
                ScrollUiKit.PaperText, new Vector2(0.25f, 0.530f), new Vector2(0.75f, 0.568f), 18);
            AddShadow(prompt);

            var takes = new List<Button>();
            for (var i = 0; i < choices.Count; i++)
            {
                var relic = choices[i];
                var centre = 0.5f + (i - (choices.Count - 1) * 0.5f) * 0.266f;
                var take = ScrollUiKit.Offering(screen.transform, i, relic, centre, T("InRun.BossDefeated.Take"));
                take.gameObject.name = $"RelicBtn_{i}";
                take.onClick.AddListener(() => ChooseShrineRelic(() => AcceptRelic(relic), onDismissed));
                takes.Add(take);
            }

            var stay = ScrollUiKit.Label(screen.transform, "RelicShrineStay",
                T(choices.Count > 1 ? "InRun.Relic.OthersStay" : "InRun.Relic.OtherStays"), 20, TextAnchor.MiddleRight,
                new Color(0.86f, 0.82f, 0.70f, 1f), new Vector2(0.25f, 0.068f), new Vector2(0.528f, 0.104f), 13);
            AddShadow(stay);
            var leave = ScrollUiKit.RimButton(screen.transform, "BtnSkip", new Vector2(0.5354f, 0.067f), new Vector2(0.6125f, 0.105f),
                T("InRun.BossDefeated.Leave"), 18, new Color(0.62f, 0.55f, 0.42f, 1f), ScrollUiKit.OfferingFocus,
                ScrollUiKit.OfferingText);
            leave.onClick.AddListener(() => ChooseShrineRelic(LeaveRelics, onDismissed));

            for (var i = 0; i < takes.Count; i++)
                takes[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? takes[i - 1] : null,
                    selectOnRight = i < takes.Count - 1 ? takes[i + 1] : null,
                    selectOnDown = leave
                };
            leave.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = takes[takes.Count / 2] };

            ActivePanel = _relicShrine;
            FadeInPanel(_relicShrine);
            EventSystem.current?.SetSelectedGameObject(takes[takes.Count / 2].gameObject);
        }

        // One choice only: a double click cannot take two relics or take and leave.
        private void ChooseShrineRelic(System.Action choice, System.Action onDismissed)
        {
            if (_relicChosen) return;
            _relicChosen = true;
            try
            {
                choice?.Invoke();
            }
            catch (System.Exception e)
            {
                // Keep the shrine usable instead of leaving every button dead.
                Debug.LogException(e);
                _relicChosen = false;
                return;
            }
            CloseRelicShrine();
            onDismissed?.Invoke();
        }

        private void CloseRelicShrine()
        {
            if (_relicShrine == null) return;
            if (ActivePanel == _relicShrine) ActivePanel = null;
            DestroyNow(_relicShrine);
            _relicShrine = null;
        }

        private static void BuildRunTag(Transform screen, RunState state)
        {
            var tag = ScrollUiKit.PaperTag(screen, "RelicShrineRun", new Vector2(0.0615f, 0.6074f), new Vector2(0.2177f, 0.7704f),
                -1f, 26f);
            ScrollUiKit.Label(tag.transform, "Header", T("InRun.Relic.YourRun").ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.95f), 11);
            var rows = new (string key, string value)[]
            {
                ("InRun.Reward.Row.Hp", $"{state.CurrentHP} / {state.MaxHP}"),
                ("InRun.Reward.Row.Gold", state.CurrentGold.ToString()),
                ("InRun.Relic.Row.Relics", state.HeldRelics.Count.ToString())
            };
            for (var k = 0; k < rows.Length; k++)
            {
                var top = 0.76f - k * 0.235f;
                ScrollUiKit.ValueRow(tag.transform, $"Row_{k}", new Vector2(0.08f, top - 0.2f), new Vector2(0.92f, top),
                    T(rows[k].key), rows[k].value, 20, ScrollUiKit.Vermilion);
            }
        }

        private static void BuildHeldRelicsTag(Transform screen, RunState state)
        {
            var tag = ScrollUiKit.PaperTag(screen, "RelicShrineHeld", new Vector2(0.7708f, 0.6556f), new Vector2(0.9427f, 0.7704f), 1f);
            ScrollUiKit.Label(tag.transform, "Header", T("InRun.Relic.Held").ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.92f), 11);
            var held = state.HeldRelics;
            if (held == null || held.Count == 0)
            {
                ScrollUiKit.Label(tag.transform, "None", T("InRun.Relic.NoneHeld"), 18, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                    new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.60f), 12, FontStyle.Italic);
                return;
            }
            const int perRow = 6;
            var shown = Mathf.Min(held.Count, perRow * 2);
            for (var i = 0; i < shown; i++)
            {
                var relic = held[i];
                var row = i / perRow;
                var col = i % perRow;
                var x = 0.08f + col * 0.145f;
                var top = (held.Count > perRow ? 0.66f : 0.60f) - row * 0.30f;
                ScrollUiKit.Icon(tag.transform, $"Held_{i}", RelicService.GetIconFolder(relic.Id) + "/icon_" + RelicService.GetIconName(relic.Id),
                    new Vector2(x, top - (held.Count > perRow ? 0.28f : 0.46f)), new Vector2(x + 0.13f, top), Color.white);
            }
        }
    }
}
