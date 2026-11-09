using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: REQ-RUN-101] In-run menu additions (GAP-020): the run seed, View Modifiers and Abandon Run.
    // The buttons are built by InRunUiBlueprintBuilder.BuildInGameOptionsPanel.
    public sealed partial class InRunController
    {
        private Text _abandonLabel;
        private bool _abandonArmed;
        private GameObject _modifierList;

        private void WireRunMenu()
        {
            var panel = _inGameOptionsPanel != null ? _inGameOptionsPanel.transform : null;
            if (panel == null) return;

            var modifiers = FindInPanel<Button>(panel, "BtnInGameOptionsModifiers");
            if (modifiers != null)
            {
                modifiers.onClick.RemoveAllListeners();
                modifiers.onClick.AddListener(ShowModifierList);
            }

            var abandon = FindInPanel<Button>(panel, "BtnInGameOptionsAbandon");
            if (abandon != null)
            {
                _abandonLabel = abandon.transform.Find("Label")?.GetComponent<Text>();
                abandon.onClick.RemoveAllListeners();
                abandon.onClick.AddListener(OnAbandonPressed);
            }
        }

        private void RefreshRunMenu()
        {
            var panel = _inGameOptionsPanel != null ? _inGameOptionsPanel.transform : null;
            var state = _map?.Run?.State;
            if (panel == null || state == null) return;

            var seed = FindInPanel<Text>(panel, "IGRunSeed");
            if (seed != null) seed.text = Tf("Seed: {0}", state.Seed);

            // Tutorials end through Save & Quit; they have no run result to record.
            var abandon = FindInPanel<Button>(panel, "BtnInGameOptionsAbandon");
            if (abandon != null) abandon.gameObject.SetActive(!state.TutorialMode);
            _abandonArmed = false;
            if (_abandonLabel != null) _abandonLabel.text = T("Abandon Run");
            if (_modifierList != null) Destroy(_modifierList);
        }

        // Two presses: the first asks for confirmation, the second ends the run as a defeat.
        private void OnAbandonPressed()
        {
            var run = _map?.Run;
            if (run?.State == null) return;
            if (!_abandonArmed)
            {
                _abandonArmed = true;
                if (_abandonLabel != null) _abandonLabel.text = T("Confirm: end this run?");
                return;
            }

            _abandonArmed = false;
            Debug.Log($"[InRunController] Abandon Run: floor {run.State.CurrentFloor + 1}, XP {run.GetTotalRunXp()}.");
            if (_inGameOptionsPanel != null) _inGameOptionsPanel.SetActive(false);
            ShowDefeatScreen(run);
        }

        // [REQ: UI-STYLE-046] Owner's pick A (0.4.3 playtest): a slip like the legend, colour, name and rule, grouped by floor, puzzle and boss.
        private void ShowModifierList()
        {
            var panel = _inGameOptionsPanel != null ? _inGameOptionsPanel.transform : null;
            if (panel == null) return;
            if (_modifierList != null) Destroy(_modifierList);

            // A child of the options panel, so closing the options (Back / B) also closes the list.
            _modifierList = new GameObject("ModifierList", typeof(RectTransform), typeof(Image));
            _modifierList.transform.SetParent(panel, false);
            Place(_modifierList.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            _modifierList.GetComponent<Image>().color = Color.black;   // opaque: the options must not show through (1.1 build smoke B13)
            ScrollUiKit.Painting(_modifierList.transform, "Backdrop", "bg_menu_pause", new Color(0.5f, 0.5f, 0.5f, 1f));

            var paper = ScrollUiKit.Fill(_modifierList.transform, "ModifierPaper", new Vector2(0.24f, 0.08f), new Vector2(0.76f, 0.92f), ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -10f);
            ScrollUiKit.Fill(paper.transform, "Band", new Vector2(0f, 0.993f), Vector2.one, ScrollUiKit.Vermilion);
            ScrollUiKit.Label(paper.transform, "Eyebrow", T("ModifierList.Eyebrow"), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.975f), 10);
            ScrollUiKit.Label(paper.transform, "Title", T("ModifierList.Title"), 40, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.835f), new Vector2(0.95f, 0.925f), 20);

            var scroll = BuildRuleColumn(paper.transform);
            var back = ScrollUiKit.PillButton(paper.transform, "BtnModifierListBack", new Vector2(0.35f, 0.025f), new Vector2(0.65f, 0.105f), T("Back"),
                22, ScrollUiKit.Wood, ScrollUiKit.WoodTileText, 26f);
            back.onClick.AddListener(() =>
            {
                Destroy(_modifierList);
                _modifierList = null;
                var reopen = FindInPanel<Button>(panel, "BtnInGameOptionsModifiers");
                if (reopen != null) EventSystem.current?.SetSelectedGameObject(reopen.gameObject);
            });
            EventSystem.current?.SetSelectedGameObject(back.gameObject);
            _modifierList.AddComponent<ArrowKeyScroll>().Target = scroll;
        }

        // The rules in a column that scrolls when a late Harmony floor or a large text size needs more room than the slip has.
        private ScrollRect BuildRuleColumn(Transform paper)
        {
            var viewport = ScrollUiKit.Area(paper, "Rules", new Vector2(0.05f, 0.125f), new Vector2(0.95f, 0.825f));
            viewport.gameObject.AddComponent<Image>().color = Color.clear;   // the wheel and a drag need something to land on
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = ScrollUiKit.Area(viewport, "RulesContent", new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = column.childForceExpandHeight = false;
            column.spacing = 12f;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var run = _map?.Run;
            var size = run?.CurrentBoard?.Size ?? 9;
            var shown = 0;
            void Group(string heading, System.Collections.Generic.IEnumerable<BossModifierId> ids)
            {
                var head = false;
                foreach (var id in ids)
                {
                    if (!head) { Heading(content, heading); head = true; }
                    RuleRow(content, id, size);
                    shown++;
                }
            }

            var floor = run?.State?.ActiveFloorModifiers;
            Group(T("ModifierList.Floor"), floor ?? new System.Collections.Generic.List<BossModifierId>());
            var puzzle = new System.Collections.Generic.List<BossModifierId>();
            if (run?.CurrentBoard != null && run.CurrentLevelConfig?.ActiveModifiers != null)
                foreach (var id in run.CurrentLevelConfig.ActiveModifiers)
                    if (floor == null || !floor.Contains(id)) puzzle.Add(id);
            Group(T("ModifierList.Puzzle"), puzzle);
            if (puzzle.Count == 0 && run?.GetBossDebuffForCurrentFloor() is { } debuff)
                Group(T("ModifierList.Boss"), new[] { debuff });
            if (shown == 0) Heading(content, T("No modifiers are active."));

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition = 1f;
            return scroll;
        }

        private static void Heading(Transform parent, string text)
        {
            var label = InRunUiFactory.CreateText(parent, "Heading", text.ToUpperInvariant(), 19, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion);
            label.raycastTarget = false;
        }

        private static void RuleRow(Transform parent, BossModifierId id, int boardSize)
        {
            var row = new GameObject($"Rule_{id}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var lines = row.AddComponent<VerticalLayoutGroup>();
            lines.childControlWidth = lines.childControlHeight = true;
            lines.childForceExpandWidth = lines.childForceExpandHeight = false;
            lines.padding = new RectOffset(60, 0, 0, 8);
            lines.spacing = 2f;

            var swatch = ScrollUiKit.Fill(row.transform, "Swatch", new Vector2(0f, 1f), new Vector2(0f, 1f),
                BoardViewController.TryGetLegendColor(id, out var color) ? color : ScrollUiKit.InkSoft, ScrollUiKit.Rounded(6f));
            swatch.rectTransform.pivot = new Vector2(0f, 1f);
            swatch.rectTransform.sizeDelta = new Vector2(44f, 12f);
            swatch.rectTransform.anchoredPosition = new Vector2(0f, -9f);
            swatch.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            RowText(row.transform, "Name", BossService.GetModifierName(id), 24, ScrollUiKit.Ink);
            RowText(row.transform, "Rule", BossService.GetModifierDescription(id, boardSize), 18, ScrollUiKit.InkSoft);
        }

        private static void RowText(Transform row, string name, string text, int size, Color color)
        {
            var label = InRunUiFactory.CreateText(row, name, text, size, TextAnchor.UpperLeft, color);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
        }

        // Arrow keys, the stick and the D-pad move a list that has no buttons to step through.
        private sealed class ArrowKeyScroll : MonoBehaviour
        {
            public ScrollRect Target;
            private const float PerSecond = 0.9f;

            private void Update()
            {
                var v = Input.GetAxis("Vertical");
                if (Target == null || Mathf.Abs(v) < 0.1f) return;
                Target.verticalNormalizedPosition = Mathf.Clamp01(Target.verticalNormalizedPosition + v * Time.unscaledDeltaTime * PerSecond);
            }
        }

        private static TComponent FindInPanel<TComponent>(Transform panel, string name) where TComponent : Component
        {
            foreach (var component in panel.GetComponentsInChildren<TComponent>(true))
                if (component.gameObject.name == name) return component;
            return null;
        }

        private static void Place(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
