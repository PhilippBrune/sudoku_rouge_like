using System.Collections.Generic;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: EVENT-OUTCOME-001] Playtest 0.4.2: the Koi Pond Wager felt like nothing happened. After a choice the strips give
    // way to a slip that says what happened, in the event's words, and every number it changed (gold, HP, pencil, bag,
    // relics, curses) as before → after; the purse above the scroll shows the new numbers.
    public sealed partial class EventChoiceScreenController
    {
        private readonly struct Snapshot
        {
            public readonly int Gold, Hp, MaxHp, Pencil, Items, Relics, Curses;

            public Snapshot(RunState s)
            {
                Gold = s.CurrentGold; Hp = s.CurrentHP; MaxHp = s.MaxHP; Pencil = s.CurrentPencil;
                Items = s.HeldItems?.Count ?? 0; Relics = s.HeldRelics?.Count ?? 0; Curses = s.ActiveCurseIds?.Count ?? 0;
            }
        }

        private RectTransform _paper;

        private void ShowOutcome(string summary, Snapshot before, RunState state)
        {
            if (_paper == null || state == null) return;
            foreach (var strip in _strips) strip.gameObject.SetActive(false);
            var question = _paper.Find("EventQuestion");
            if (question != null) question.gameObject.SetActive(false);
            if (_resultText != null) _resultText.text = string.Empty;

            var after = new Snapshot(state);
            ScrollUiKit.Label(_paper, "EventOutcomeHeader", T("InRun.Event.Outcome", "WHAT HAPPENED").ToUpperInvariant(), 20,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.53f, 0.85f), new Vector2(0.95f, 0.90f), 13);
            ScrollUiKit.Label(_paper, "EventOutcomeText", string.IsNullOrEmpty(summary) ? T("RunEvent.Result.Done", "Done.") : Capitalize(summary),
                30, TextAnchor.UpperLeft, ScrollUiKit.Ink, new Vector2(0.53f, 0.66f), new Vector2(0.95f, 0.83f), 18);

            var lines = new List<string>();
            void Add(string key, string fallback, int was, int now)
            {
                if (was != now) lines.Add(Fb(key, fallback, was, now));
            }
            Add("InRun.Event.Delta.Gold", "Gold {0} → {1}", before.Gold, after.Gold);
            Add("InRun.Rest.After.Hp", "HP {0} → {1}", before.Hp, after.Hp);
            Add("InRun.Event.Delta.MaxHp", "Max HP {0} → {1}", before.MaxHp, after.MaxHp);
            Add("InRun.Rest.After.Pencil", "Pencil {0} → {1}", before.Pencil, after.Pencil);
            Add("InRun.Event.Delta.Items", "Items in the bag {0} → {1}", before.Items, after.Items);
            Add("InRun.Event.Delta.Relics", "Relics {0} → {1}", before.Relics, after.Relics);
            Add("InRun.Rest.After.Curses", "Curses {0} → {1}", before.Curses, after.Curses);
            if (lines.Count == 0) lines.Add(T("InRun.Event.Delta.None", "Nothing else changed."));
            for (var i = 0; i < lines.Count; i++)
            {
                var top = 0.64f - i * 0.065f;
                ScrollUiKit.Label(_paper, $"EventOutcomeDelta_{i}", lines[i], 26, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                    new Vector2(0.53f, top - 0.06f), new Vector2(0.95f, top), 15);
            }
            RefreshPurse(state);
        }

        private void RefreshPurse(RunState state)
        {
            if (_scene == null) return;
            foreach (var name in new[] { "EventFloor", "EventPurseIcon_0", "EventPurseIcon_1", "EventPurseIcon_2", "EventPurse_0", "EventPurse_1", "EventPurse_2" })
            {
                var old = _scene.transform.Find(name);
                if (old != null) ScrollUiKit.DestroyNow(old.gameObject);
            }
            BuildPurse(_scene.transform, state);
        }

        private static string Capitalize(string text) =>
            text.Length > 0 ? char.ToUpper(text[0], LocalizationService.Culture) + text.Substring(1) : text;

        private static string Fb(string key, string fallback, params object[] args) => LocalizationService.Format(key, fallback, args);
    }
}
