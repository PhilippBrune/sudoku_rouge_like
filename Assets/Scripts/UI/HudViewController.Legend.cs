using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: REQ-BOSS-104] [REQ: UI-STYLE-042] The legend is the rule slip itself (owner's pick A, 0.3.1 playtest: the
    // legend box opened at a different place for every number of rules): each rule is a row with its overlay colour, its
    // name and what it asks. Tapping a row shows only that rule's geometry for 3 s; tapping it again shows all again.
    public sealed partial class HudViewController
    {
        private static readonly Color NoGeometrySwatch = new Color(0.55f, 0.55f, 0.55f, 0.9f);

        private BossModifierId? _legendIsolated;
        private readonly List<(BossModifierId Id, Outline Highlight)> _legendEntries = new List<(BossModifierId, Outline)>();

        // One row per rule on the slip, from the top; the slip keeps its place and size.
        private void BuildRuleRows(IReadOnlyList<BossModifierId> mods)
        {
            ScrollUiKit.DestroyChildrenNow(_modifierInfoBox.transform, "LegendEntry_");
            _legendEntries.Clear();
            _legendIsolated = null;
            GetComponent<BoardViewController>()?.IsolateModifier(null);
            var area = ScrollUiKit.Area(_modifierInfoBox.transform, "LegendEntry_Area", Vector2.zero, Vector2.one);
            area.offsetMin = new Vector2(12f, 10f);
            area.offsetMax = new Vector2(-10f, -16f);
            for (var i = 0; i < mods.Count; i++)
            {
                var id = mods[i];
                var row = new GameObject("LegendEntry_" + id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                row.transform.SetParent(area, false);
                var rowRt = (RectTransform)row.transform;
                rowRt.anchorMin = new Vector2(0f, 1f - (i + 1f) / mods.Count);
                rowRt.anchorMax = new Vector2(1f, 1f - (float)i / mods.Count);
                rowRt.offsetMin = new Vector2(0f, 2f);
                rowRt.offsetMax = new Vector2(0f, -2f);
                var bg = row.GetComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0f);   // a hit area; the paper shows through
                var button = row.GetComponent<Button>();
                button.targetGraphic = bg;
                button.transition = Selectable.Transition.None;
                var highlight = row.GetComponent<Outline>();
                highlight.effectColor = ScrollUiKit.Gold;
                highlight.effectDistance = new Vector2(2f, -2f);
                highlight.enabled = false;

                var swatch = ScrollUiKit.Fill(row.transform, "Swatch", new Vector2(0f, 0.70f), new Vector2(0.11f, 0.86f),
                    BoardViewController.TryGetLegendColor(id, out var color) ? color : NoGeometrySwatch, ScrollUiKit.Rounded(6f));
                swatch.raycastTarget = false;
                ScrollUiKit.Label(row.transform, "Name", BossService.GetModifierName(id), 17, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                    new Vector2(0.14f, 0.58f), new Vector2(1f, 1f), 10);
                ScrollUiKit.Label(row.transform, "Rule", BossService.GetModifierDescription(id, _map?.Run?.CurrentBoard?.Size ?? 9), 14, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                    new Vector2(0f, 0f), new Vector2(1f, 0.58f), 9);

                button.onClick.AddListener(() =>
                {
                    OnLegendEntryTapped(id);
                    // GAP-091: the rules that have no lines show their effect around the chosen cell instead.
                    if (id == BossModifierId.Nonconsecutive || id == BossModifierId.Antiknight) OnModifierTapped?.Invoke(id);
                });
                _legendEntries.Add((id, highlight));
            }
        }

        private void OnLegendEntryTapped(BossModifierId id)
        {
            var board = GetComponent<BoardViewController>();
            _legendIsolated = _legendIsolated == id ? (BossModifierId?)null : id;
            board?.IsolateModifier(_legendIsolated);
            foreach (var (entryId, highlight) in _legendEntries)
                if (highlight != null) highlight.enabled = _legendIsolated == entryId;
            if (_legendIsolated.HasValue) StartCoroutine(ClearLegendHighlightAfter(_legendIsolated.Value));
        }

        private IEnumerator ClearLegendHighlightAfter(BossModifierId id)
        {
            yield return new WaitForSeconds(BoardViewController.IsolationSeconds);
            if (_legendIsolated != id) yield break;
            _legendIsolated = null;
            foreach (var (_, highlight) in _legendEntries)
                if (highlight != null) highlight.enabled = false;
        }
    }
}
