using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // The rule book: one tab per family, the family's rules as tiles in six columns over both pages, and a line under
    // the tiles that reads out the focused rule (or why it cannot be chosen).
    public sealed partial class CustomPuzzleScreen
    {
        private sealed class Tile
        {
            public BossModifierId Id;
            public Image Paper;
            public Image Enso;
            public CanvasGroup Group;
        }

        private const int Columns = 6;
        // Tile grid on the 1920×1080 mockup: first tile at (560, 150), 190×130, steps of 206 and 146.
        private const float TileX0 = 0.2917f, TileW = 0.0990f, TileStepX = 0.1073f;
        private const float TileTop0 = 0.8611f, TileH = 0.1204f, TileStepY = 0.1352f;
        private static readonly Color TilePaper = new Color(0.97f, 0.94f, 0.86f, 1f);
        private static readonly Color TileChosen = new Color(0.99f, 0.92f, 0.78f, 1f);
        private static readonly Color TabWood = new Color(0.55f, 0.36f, 0.20f, 1f);

        private RectTransform _screen, _page;
        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly Dictionary<ModifierFamily, Image> _tabs = new Dictionary<ModifierFamily, Image>();
        private ModifierFamily _family = ModifierFamily.Lines;
        private Text _hint;

        public ModifierFamily CurrentFamily => _family;

        public void ShowFamily(ModifierFamily family)
        {
            // A controller or keyboard focus on a tile moves to the new page's first tile instead of being lost.
            var focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var tileHadFocus = focus != null && _page != null && focus.transform.IsChildOf(_page);
            _family = family;
            BuildTiles();
            RefreshTabs();
            ShowDefaultHint();
            if (tileHadFocus && _tiles.Count > 0) EventSystem.current.SetSelectedGameObject(_tiles[0].Paper.gameObject);
        }

        /// <summary>LB / RB: the previous or next family (wraps).</summary>
        public void StepFamily(int delta)
        {
            var order = ModifierFamilies.Order;
            var i = System.Array.IndexOf(order, _family);
            ShowFamily(order[(i + delta + order.Length) % order.Length]);
            if (_tiles.Count > 0) EventSystem.current?.SetSelectedGameObject(_tiles[0].Paper.gameObject);
        }

        private void Update()
        {
            if (_setup == null) return;
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton4))) StepFamily(-1);
            if (Input.GetKeyDown(PadButtons.Map(KeyCode.JoystickButton5))) StepFamily(1);
        }

        private void BuildBook(RectTransform screen)
        {
            _screen = screen;
            BuildTabs(screen);
            var cover = ScrollUiKit.Fill(screen, "CustomBook", new Vector2(0.2708f, 0.2639f), new Vector2(0.9469f, 0.9111f),
                new Color(0.36f, 0.22f, 0.12f, 1f), ScrollUiKit.Rounded(10f));
            var shadow = cover.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var paper = ScrollUiKit.Fill(cover.transform, "Pages", Vector2.zero, Vector2.one, ScrollUiKit.Paper);
            paper.rectTransform.offsetMin = new Vector2(14f, 14f);
            paper.rectTransform.offsetMax = new Vector2(-14f, -14f);
            ScrollUiKit.Fill(paper.transform, "PageLight", new Vector2(0f, 0.4f), new Vector2(0.5f, 1f), new Color(1f, 1f, 1f, 0.2f),
                ScrollUiKit.Glow());
            ScrollUiKit.Fill(paper.transform, "Spine", new Vector2(0.4968f, 0f), new Vector2(0.5032f, 1f), new Color(0.62f, 0.50f, 0.36f, 0.8f));
            // The tiles sit on the canvas grid of the mockup, so they are children of the screen, drawn over the book.
            _page = ScrollUiKit.Area(screen, "BookTiles", Vector2.zero, Vector2.one);
            _hint = ScrollUiKit.Label(screen, "RuleHint", string.Empty, 18, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(TileX0, 0.284f), new Vector2(0.93f, 0.322f), 11);
            ShowFamily(_family);
        }

        // One tab per family with its rule count; the open tab is paper, the others wood.
        private void BuildTabs(RectTransform screen)
        {
            var x = 0.2708f;
            foreach (var family in ModifierFamilies.Order)
            {
                var label = $"{ModifierFamilies.LocalizedName(family)} <size=15>{ModifierFamilies.Get(family).Count}</size>";
                var tab = ScrollUiKit.Fill(screen, $"FamilyTab_{family}", new Vector2(x, 0.9065f), new Vector2(x, 0.9519f), TabWood,
                    ScrollUiKit.Rounded(8f));
                tab.raycastTarget = true;
                var text = ScrollUiKit.Label(tab.transform, "Label", label, 20, TextAnchor.MiddleCenter, ScrollUiKit.PaperText,
                    Vector2.zero, Vector2.one);
                text.supportRichText = true;
                var width = (text.preferredWidth + 44f) / 1920f;
                tab.rectTransform.anchorMax = new Vector2(x + width, 0.9519f);
                x += width + 0.003f;
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = tab;
                button.transition = Selectable.Transition.None;
                var f = family;
                button.onClick.AddListener(() => ShowFamily(f));
                _tabs[family] = tab;
            }
        }

        private void RefreshTabs()
        {
            foreach (var pair in _tabs)
            {
                var open = pair.Key == _family;
                pair.Value.color = open ? ScrollUiKit.Paper : TabWood;
                var text = pair.Value.transform.Find("Label")?.GetComponent<Text>();
                if (text != null) text.color = open ? ScrollUiKit.Ink : ScrollUiKit.PaperText;
            }
        }

        private void BuildTiles()
        {
            foreach (var tile in _tiles) ScrollUiKit.DestroyNow(tile.Paper.gameObject);
            _tiles.Clear();
            var members = ModifierFamilies.Get(_family);
            for (var i = 0; i < members.Count; i++)
                _tiles.Add(BuildTile(i, members[i]));
            RefreshTiles();
        }

        private Tile BuildTile(int index, BossModifierId id)
        {
            var col = index % Columns;
            var row = index / Columns;
            var x = TileX0 + col * TileStepX;
            var top = TileTop0 - row * TileStepY;
            var paper = ScrollUiKit.Fill(_page, $"RuleTile_{id}", new Vector2(x, top - TileH), new Vector2(x + TileW, top), TilePaper,
                ScrollUiKit.Rounded(8f));
            paper.raycastTarget = true;
            var edge = paper.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.82f, 0.74f, 0.60f, 1f);
            edge.effectDistance = new Vector2(1.5f, -1.5f);
            var button = paper.gameObject.AddComponent<Button>();
            button.targetGraphic = paper;
            button.colors = ScrollUiKit.StateColors(Color.white);

            var enso = ScrollUiKit.Fill(paper.transform, "Enso", new Vector2(-0.09f, -0.22f), new Vector2(1.09f, 1.22f),
                new Color(0.74f, 0.20f, 0.15f, 0.9f), ScrollUiKit.Enso());
            ScrollUiKit.Icon(paper.transform, "Icon", IconPath(id), new Vector2(0.33f, 0.46f), new Vector2(0.67f, 0.92f), Color.white);
            ScrollUiKit.Label(paper.transform, "Name", BossService.GetModifierName(id), 19, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.40f), 12);
            if (ModifierFamilies.IsCatalogAddition(id))
            {
                var badge = ScrollUiKit.Fill(paper.transform, "New", new Vector2(0.70f, 0.83f), new Vector2(0.97f, 0.97f),
                    new Color(0.16f, 0.25f, 0.52f, 1f), ScrollUiKit.Rounded(8f));
                ScrollUiKit.Label(badge.transform, "Text", T("CustomPuzzle.Book.New", "New").ToUpperInvariant(), 13, TextAnchor.MiddleCenter,
                    Color.white, Vector2.zero, Vector2.one, 9);
            }
            button.onClick.AddListener(() => OnTileClicked(id));
            AddHover(paper.gameObject, id);
            return new Tile { Id = id, Paper = paper, Enso = enso, Group = paper.gameObject.AddComponent<CanvasGroup>() };
        }

        private void RefreshTiles()
        {
            foreach (var tile in _tiles)
            {
                var chosen = _setup.IsSelected(tile.Id);
                var available = _setup.CanSelect(tile.Id, out _);
                tile.Enso.enabled = chosen;
                tile.Paper.color = chosen ? TileChosen : TilePaper;
                var edge = tile.Paper.GetComponent<Outline>();
                edge.effectColor = chosen ? ScrollUiKit.Vermilion : new Color(0.82f, 0.74f, 0.60f, 1f);
                tile.Group.alpha = available ? 1f : 0.4f;
            }
        }

        // A rule that does not fit stays focusable: choosing it says why instead.
        private void OnTileClicked(BossModifierId id)
        {
            if (_setup.ToggleModifier(id)) return;
            _setup.CanSelect(id, out var reason);
            _hint.text = reason;
            _hint.color = ScrollUiKit.Vermilion;
        }

        private void AddHover(GameObject target, BossModifierId id)
        {
            var trigger = target.AddComponent<EventTrigger>();
            void On(EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(action);
                trigger.triggers.Add(entry);
            }
            On(EventTriggerType.PointerEnter, _ => ShowRule(id));
            On(EventTriggerType.Select, _ => ShowRule(id));
            On(EventTriggerType.PointerExit, _ => ShowDefaultHint());
            On(EventTriggerType.Deselect, _ => ShowDefaultHint());
        }

        private void ShowRule(BossModifierId id)
        {
            if (_hint == null) return;
            var available = _setup.CanSelect(id, out var reason);
            _hint.text = available
                ? $"<b>{BossService.GetModifierName(id)}</b> · {BossService.GetModifierDescription(id)}"
                : reason;
            _hint.supportRichText = true;
            _hint.color = available ? ScrollUiKit.Ink : ScrollUiKit.Vermilion;
        }

        private void ShowDefaultHint()
        {
            if (_hint == null) return;
            _hint.text = F("CustomPuzzle.Book.Hint", "Hover or focus a rule to read it · LB / RB switch the family · up to {0} rules",
                Tutorial.TutorialModeService.CustomModifierLimit);
            _hint.color = ScrollUiKit.InkSoft;
        }
    }
}
