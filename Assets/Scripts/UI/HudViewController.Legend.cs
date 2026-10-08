using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: REQ-BOSS-104] Modifier legend (GAP-025, BossMechanicsSystem §Modifier Legend Panel): a collapsible list of
    // the puzzle's modifiers with their overlay colour. Tapping an entry isolates that modifier's geometry for 3 s;
    // tapping it again restores all layers. The panel collapses after 5 s without interaction.
    public sealed partial class HudViewController
    {
        public const float LegendAutoCollapseSeconds = 5f;
        private static readonly Color LegendPanelBg = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        private static readonly Color NoGeometrySwatch = new Color(0.55f, 0.55f, 0.55f, 0.9f);

        private Button _legendButton;
        private GameObject _legendPanel;
        private List<BossModifierId> _legendModsRef;
        private BossModifierId? _legendIsolated;
        private Coroutine _legendCollapse;
        private readonly List<(BossModifierId Id, Outline Highlight)> _legendEntries = new List<(BossModifierId, Outline)>();

        private void EnsureModifierLegend()
        {
            var run = _map?.Run;
            var mods = run?.CurrentLevelConfig?.ActiveModifiers;
            var sealedEyes = run?.State != null && run.CurrentLevelConfig != null && run.CurrentLevelConfig.IsBoss
                             && SudokuRoguelike.Run.CurseService.IsActive(run.State, "sealed_eyes");
            if (_sudokuPanel == null || mods == null || mods.Count == 0 || sealedEyes)
            {
                if (_legendButton != null) _legendButton.gameObject.SetActive(false);
                if (_legendPanel != null) _legendPanel.SetActive(false);
                return;
            }

            if (_legendButton == null)
            {
                _legendButton = InRunUiFactory.CreateActionButton(_sudokuPanel.transform, "BtnModifierLegend",
                    new Vector2(0.74f, 0.848f), new Vector2(0.93f, 0.878f), T("Legend"), UiAction.Confirm);
                _legendButton.onClick.AddListener(ToggleLegend);
            }
            _legendButton.gameObject.SetActive(true);

            if (!ReferenceEquals(mods, _legendModsRef))
            {
                _legendModsRef = mods;
                _legendIsolated = null;
                if (_legendPanel != null) Destroy(_legendPanel);
                _legendPanel = null;
                _legendEntries.Clear();
            }
        }

        private void ToggleLegend()
        {
            if (_legendPanel != null && _legendPanel.activeSelf)
            {
                CollapseLegend();
                return;
            }
            if (_legendPanel == null) BuildLegendPanel();
            if (_legendPanel == null) return;
            _legendPanel.SetActive(true);
            RestartLegendCollapse();
        }

        private void BuildLegendPanel()
        {
            var mods = _legendModsRef;
            if (_sudokuPanel == null || mods == null) return;

            _legendPanel = new GameObject("ModifierLegendPanel", typeof(RectTransform), typeof(Image));
            _legendPanel.transform.SetParent(_sudokuPanel.transform, false);
            var rt = (RectTransform)_legendPanel.transform;
            const float rowHeight = 0.055f;
            rt.anchorMin = new Vector2(0.74f, Mathf.Max(0.42f, 0.845f - rowHeight * mods.Count - 0.01f));
            rt.anchorMax = new Vector2(0.93f, 0.845f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _legendPanel.GetComponent<Image>().color = LegendPanelBg;

            var rows = mods.Count;
            for (var i = 0; i < rows; i++)
            {
                var id = mods[i];
                var row = new GameObject("LegendEntry_" + id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                row.transform.SetParent(_legendPanel.transform, false);
                var rowRt = (RectTransform)row.transform;
                rowRt.anchorMin = new Vector2(0.03f, 1f - (i + 1f) / rows + 0.01f);
                rowRt.anchorMax = new Vector2(0.97f, 1f - (float)i / rows - 0.01f);
                rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;
                row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
                var highlight = row.GetComponent<Outline>();
                highlight.effectColor = InRunUiFactory.AccentGold;
                highlight.effectDistance = new Vector2(1.5f, -1.5f);
                highlight.enabled = false;

                var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                swatch.transform.SetParent(row.transform, false);
                var swRt = (RectTransform)swatch.transform;
                swRt.anchorMin = new Vector2(0.03f, 0.25f);
                swRt.anchorMax = new Vector2(0.13f, 0.75f);
                swRt.offsetMin = swRt.offsetMax = Vector2.zero;
                var swatchImage = swatch.GetComponent<Image>();
                swatchImage.color = BoardViewController.TryGetLegendColor(id, out var color) ? color : NoGeometrySwatch;
                swatchImage.raycastTarget = false;

                var label = InRunUiFactory.CreateText(row.transform, "Name", BossService.GetModifierName(id), 11,
                    TextAnchor.MiddleLeft, InRunUiFactory.TextColor);
                label.rectTransform.anchorMin = new Vector2(0.17f, 0f);
                label.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.raycastTarget = false;

                row.GetComponent<Button>().onClick.AddListener(() => OnLegendEntryTapped(id));
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
            RestartLegendCollapse();
        }

        private IEnumerator ClearLegendHighlightAfter(BossModifierId id)
        {
            yield return new WaitForSeconds(BoardViewController.IsolationSeconds);
            if (_legendIsolated != id) yield break;
            _legendIsolated = null;
            foreach (var (_, highlight) in _legendEntries)
                if (highlight != null) highlight.enabled = false;
        }

        private void RestartLegendCollapse()
        {
            if (_legendCollapse != null) StopCoroutine(_legendCollapse);
            _legendCollapse = StartCoroutine(CollapseLegendAfter(LegendAutoCollapseSeconds));
        }

        private IEnumerator CollapseLegendAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _legendCollapse = null;
            if (_legendPanel != null) _legendPanel.SetActive(false);
        }

        private void CollapseLegend()
        {
            if (_legendCollapse != null) StopCoroutine(_legendCollapse);
            _legendCollapse = null;
            if (_legendPanel != null) _legendPanel.SetActive(false);
        }
    }
}
