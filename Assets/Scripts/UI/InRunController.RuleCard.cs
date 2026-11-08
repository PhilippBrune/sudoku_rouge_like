using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: RULE-CARD-001] Paul's playtest of 0.4.2 (the owner's pick FB1): "show the rules before the puzzle, so you do not
    // read them in the game". A puzzle with a rule this profile has never been shown opens with a card for it: its name and
    // what it asks, while the board shows only that rule's marks. One card per new rule; the puzzle's clock starts after the
    // last. Known rules stay on the slip only. Rules met before 0.4.3 count as known (RulesExplainedSeeded).
    public sealed partial class InRunController
    {
        private GameObject _ruleCard;
        private readonly List<BossModifierId> _ruleCardQueue = new List<BossModifierId>();
        private int _ruleCardTotal;

        private bool IsRuleCardOpen => _ruleCard != null;

        private void ShowNewRuleCards()
        {
            var run = _map?.Run;
            var mods = run?.CurrentLevelConfig?.ActiveModifiers;
            if (run?.State == null || run.State.TutorialMode || _ruleCard != null || mods == null || mods.Count == 0) return;
            if (Run.CurseService.IsActive(run.State, "sealed_eyes") && run.CurrentLevelConfig.IsBoss) return;   // the slip is hidden too
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            if (!save.HasSaveFile()) return;
            var envelope = save.Load();
            var meta = envelope.MetaProgress ??= new MetaProgressionState();
            meta.RulesExplained ??= new List<BossModifierId>();
            var changed = false;
            if (!meta.RulesExplainedSeeded)
            {
                foreach (var known in meta.DiscoveredBossModifiers ?? new List<BossModifierId>())
                    if (!mods.Contains(known) && !meta.RulesExplained.Contains(known)) meta.RulesExplained.Add(known);
                meta.RulesExplainedSeeded = changed = true;
            }
            _ruleCardQueue.Clear();
            foreach (var id in mods)
                if (!meta.RulesExplained.Contains(id)) _ruleCardQueue.Add(id);   // marked when its card is dismissed
            if (changed) save.Save(envelope);
            if (_ruleCardQueue.Count == 0) return;
            _ruleCardTotal = _ruleCardQueue.Count;
            BuildRuleCard();
        }

        private void BuildRuleCard()
        {
            if (_sudokuPanel == null || _ruleCardQueue.Count == 0) return;
            var id = _ruleCardQueue[0];
            // The bag and the relics are built on the first refresh of a puzzle: done now, or they would lie over the card.
            if (_map?.Run != null) _hudView?.Refresh(_map.Run.State, _map.Run.CurrentLevelConfig);
            var layer = ScrollUiKit.Fill(_sudokuPanel.transform, "RuleCardLayer", Vector2.zero, Vector2.one, new Color(0.04f, 0.03f, 0.02f, 0.45f));
            layer.raycastTarget = true;   // the board waits under the card
            _ruleCard = layer.gameObject;
            var card = ScrollUiKit.Fill(layer.transform, "RuleCard", new Vector2(0.035f, 0.20f), new Vector2(0.300f, 0.80f), ScrollUiKit.Paper);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -10f);
            var t = card.transform;
            ScrollUiKit.Fill(t, "Band", new Vector2(0f, 0.97f), Vector2.one, ScrollUiKit.Vermilion);

            var index = _ruleCardTotal - _ruleCardQueue.Count + 1;
            ScrollUiKit.Label(t, "Eyebrow", LocalizationService.Format("InRun.RuleCard.Eyebrow", "NEW RULE · {0} OF {1}", index, _ruleCardTotal)
                .ToUpperInvariant(), 18, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.92f), 11);
            ScrollUiKit.Fill(t, "Swatch", new Vector2(0.08f, 0.76f), new Vector2(0.16f, 0.82f),
                BoardViewController.TryGetLegendColor(id, out var colour) ? colour : new Color(0.55f, 0.55f, 0.55f, 0.9f), ScrollUiKit.Rounded(6f));
            ScrollUiKit.Label(t, "Name", BossService.GetModifierName(id), 40, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                new Vector2(0.19f, 0.73f), new Vector2(0.95f, 0.85f), 22);
            ScrollUiKit.Label(t, "Rule", BossService.GetModifierDescription(id, _map?.Run?.CurrentBoard?.Size ?? 9), 26, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.71f), 15);
            ScrollUiKit.Label(t, "Hint", LocalizationService.T("InRun.RuleCard.Hint", "The board shows where it applies. The rule stays on the slip beside it."),
                19, TextAnchor.UpperLeft, ScrollUiKit.InkSoft, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.34f), 12);
            var last = _ruleCardQueue.Count == 1;
            var next = ScrollUiKit.PillButton(t, "BtnRuleCardNext", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.16f),
                last ? LocalizationService.T("InRun.RuleCard.Start", "Start the puzzle") : LocalizationService.T("InRun.RuleCard.Next", "Next rule"),
                24, ScrollUiKit.Vermilion, Color.white, 24f);
            next.onClick.AddListener(AdvanceRuleCard);
            EventSystem.current?.SetSelectedGameObject(next.gameObject);
            _boardView?.IsolateModifier(id, 3600f);
        }

        private void AdvanceRuleCard()
        {
            if (_ruleCard != null) ScrollUiKit.DestroyNow(_ruleCard);
            _ruleCard = null;
            if (_ruleCardQueue.Count > 0)
            {
                MarkRuleExplained(_ruleCardQueue[0]);
                _ruleCardQueue.RemoveAt(0);
            }
            if (_ruleCardQueue.Count > 0) { BuildRuleCard(); return; }
            _boardView?.IsolateModifier(null);
            _hudView?.StartPuzzleTimer(_map?.Run?.State?.PuzzleClockSeconds ?? 0f);   // the puzzle's clock starts after the cards
        }

        private static void MarkRuleExplained(BossModifierId id)
        {
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            if (!save.HasSaveFile()) return;
            var envelope = save.Load();
            var meta = envelope.MetaProgress ??= new MetaProgressionState();
            meta.RulesExplained ??= new List<BossModifierId>();
            if (meta.RulesExplained.Contains(id)) return;
            meta.RulesExplained.Add(id);
            save.Save(envelope);
        }

        /// <summary>Closes the card without counting its rule as shown (Save &amp; Quit with a card open).</summary>
        private void CloseRuleCard()
        {
            if (_ruleCard == null) return;
            ScrollUiKit.DestroyNow(_ruleCard);
            _ruleCard = null;
            _ruleCardQueue.Clear();
            _boardView?.IsolateModifier(null);
        }
    }
}
