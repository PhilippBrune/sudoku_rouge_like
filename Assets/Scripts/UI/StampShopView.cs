using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Meta;
using UnityEngine;
using UnityEngine.UI;
using Look = SudokuRoguelike.Meta.InkStampShop.Look;
using Shelf = SudokuRoguelike.Meta.InkStampShop.Shelf;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-029] [REQ: DAILY-WALK-007] The ink stamp shop (owner 2026-10-26): three shelves on a scroll (board ink,
    /// class seal frames, path seals), each look as a small preview with its name and price; owned looks carry an "owned"
    /// stamp, the worn one a vermilion frame. The slip below says what the chosen look changes, with Back and Buy or Wear.
    /// Looks only: nothing here changes the game's balance.
    /// </summary>
    public sealed class StampShopView : MonoBehaviour
    {
        private const float ItemW = 0.104f, ItemH = 0.148f, ItemStep = 0.115f, ItemLeft = 0.133f;
        private static readonly float[] ShelfY = { 0.615f, 0.420f, 0.225f };
        private static readonly Color ItemPaper = new Color(0.90f, 0.84f, 0.69f, 1f);
        private static readonly Color Green = new Color(0.18f, 0.42f, 0.23f, 1f);

        private readonly List<(Look Look, Button Button)> _items = new List<(Look, Button)>();
        private DailyGoalState _state;
        private Action<DailyGoalState> _save;
        private Action _close;
        private Look _chosen;
        private Text _purse, _desc;
        private Button _action, _back;

        public static StampShopView Build(Transform parent, DailyGoalState state, Action<DailyGoalState> save, Action close)
        {
            var screen = ScrollUiKit.Fill(parent, "StampShop", Vector2.zero, Vector2.one, Color.black);
            screen.raycastTarget = true;
            ScrollUiKit.Painting(screen.transform, "Painting", "bg_daily_walk", new Color(0.42f, 0.42f, 0.40f, 1f));
            var view = screen.gameObject.AddComponent<StampShopView>();
            view._state = state ?? new DailyGoalState();
            view._save = save;
            view._close = close;
            ScrollUiKit.Banner(screen.transform, "Banner", new Vector2(0.318f, 0.870f), new Vector2(0.682f, 0.972f),
                "Eyebrow", LocalizationService.T("Daily Walk") + " · " + LocalizationService.T("StampShop.Eyebrow"),
                "Title", LocalizationService.T("StampShop.Title"), 42);
            var scroll = ScrollUiKit.Fill(screen.transform, "Scroll", new Vector2(0.109f, 0.167f), new Vector2(0.891f, 0.833f), ScrollUiKit.Paper);
            var shadow = scroll.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -12f);
            ScrollUiKit.BrocadeBand(scroll.rectTransform, "BrocadeTop", new Vector2(0f, 0.97f), Vector2.one, true);
            ScrollUiKit.BrocadeBand(scroll.rectTransform, "BrocadeBottom", Vector2.zero, new Vector2(1f, 0.03f), true);
            view._purse = ScrollUiKit.Label(screen.transform, "Purse", string.Empty, 22, TextAnchor.MiddleRight, ScrollUiKit.Ink,
                new Vector2(0.70f, 0.775f), new Vector2(0.87f, 0.815f), 14);
            view._purse.supportRichText = true;
            view.BuildShelves(screen.transform);
            view._desc = ScrollUiKit.Label(screen.transform, "Description", string.Empty, 19, TextAnchor.MiddleLeft, ScrollUiKit.PaperText,
                new Vector2(0.109f, 0.095f), new Vector2(0.68f, 0.150f), 12);
            view._back = ScrollUiKit.PillButton(screen.transform, "BtnShopBack", new Vector2(0.700f, 0.095f), new Vector2(0.765f, 0.150f),
                LocalizationService.T("Back"), 22, new Color(0.24f, 0.20f, 0.16f, 1f), ScrollUiKit.PaperText, 30f);
            view._back.onClick.AddListener(() => view._close?.Invoke());
            view._action = ScrollUiKit.PillButton(screen.transform, "BtnShopAction", new Vector2(0.772f, 0.095f), new Vector2(0.891f, 0.150f),
                string.Empty, 22, ScrollUiKit.Vermilion, Color.white, 30f);
            view._action.onClick.AddListener(view.Act);
            ScrollUiKit.Label(screen.transform, "Hint", LocalizationService.T("StampShop.Hint"), 17, TextAnchor.MiddleCenter,
                new Color(0.86f, 0.82f, 0.72f, 1f), new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.07f), 11);
            view.Choose(view._items[1].Look);
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null) events.SetSelectedGameObject(view._items[1].Button.gameObject);
            return view;
        }

        /// <summary>The look shown on the slip, for screenshots and tests.</summary>
        public Look Chosen => _chosen;

        private void BuildShelves(Transform screen)
        {
            var headers = new[] { "StampShop.Shelf.BoardInk", "StampShop.Shelf.SealFrame", "StampShop.Shelf.PathSeal" };
            for (var s = 0; s < headers.Length; s++)
            {
                ScrollUiKit.Label(screen, "ShelfHeader_" + s, LocalizationService.T(headers[s]).ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                    ScrollUiKit.InkSoft, new Vector2(ItemLeft, ShelfY[s] + ItemH + 0.016f), new Vector2(0.68f, ShelfY[s] + ItemH + 0.047f), 11);
                var i = 0;
                foreach (var look in InkStampShop.Catalog)
                {
                    if ((int)look.Shelf != s) continue;
                    var x = ItemLeft + i++ * ItemStep;
                    _items.Add((look, BuildItem(screen, look, new Vector2(x, ShelfY[s]), new Vector2(x + ItemW, ShelfY[s] + ItemH))));
                }
            }
        }

        private Button BuildItem(Transform screen, Look look, Vector2 min, Vector2 max)
        {
            var paper = ScrollUiKit.Fill(screen, "Look_" + look.Key, min, max, ItemPaper);
            paper.raycastTarget = true;
            var button = paper.gameObject.AddComponent<Button>();
            button.targetGraphic = paper;
            var colors = ScrollUiKit.StateColors(Color.white);
            colors.highlightedColor = colors.selectedColor = new Color(1f, 0.84f, 0.52f, 1f);   // focus reads as warm gold on the paper
            button.colors = colors;
            button.onClick.AddListener(() => Choose(look));   // Buy or Wear acts, so a stray click spends nothing
            paper.gameObject.AddComponent<FocusNotifier>().OnFocus = () => Choose(look);
            var preview = ScrollUiKit.Area(paper.transform, "Preview", new Vector2(0.12f, 0.36f), new Vector2(0.64f, 0.94f));
            switch (look.Shelf)
            {
                case Shelf.BoardInk: InkGrid(preview, look.Colour); break;
                case Shelf.SealFrame: SealRing(preview, look.Colour); break;
                default: PathSeal(preview, look); break;
            }
            ScrollUiKit.Label(paper.transform, "Name", InkStampShop.Name(look), 17, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.34f), 11);
            ScrollUiKit.Label(paper.transform, "Price", string.Empty, 16, TextAnchor.MiddleCenter, ScrollUiKit.Vermilion,
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f), 10);
            var owned = ScrollUiKit.Pill(paper.transform, "Owned", new Vector2(0.66f, 0.80f), new Vector2(0.98f, 0.93f),
                LocalizationService.T("StampShop.Owned"), new Color(0f, 0f, 0f, 0f), Green, 12);
            owned.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            ScrollUiKit.InkFrame(owned.transform, Green, 2f);
            var worn = ScrollUiKit.Fill(paper.transform, "Worn", Vector2.zero, Vector2.one, Color.clear);
            worn.rectTransform.offsetMin = new Vector2(-6f, -6f);
            worn.rectTransform.offsetMax = new Vector2(6f, 6f);
            ScrollUiKit.InkFrame(worn.transform, ScrollUiKit.Vermilion, 4f);
            return button;
        }

        // A corner of a board: given digits and lines in the ink.
        private static void InkGrid(RectTransform area, Color ink)
        {
            var frame = ScrollUiKit.Fill(area, "Grid", Vector2.zero, Vector2.one, ink);
            var digits = new[] { "3", "", "7", "", "5", "", "1", "", "9" };
            for (var i = 0; i < 9; i++)
            {
                float x0 = i % 3 / 3f, y0 = 1f - (i / 3 + 1) / 3f;
                var cell = ScrollUiKit.Fill(frame.transform, "Cell_" + i, new Vector2(x0, y0), new Vector2(x0 + 1f / 3f, y0 + 1f / 3f),
                    new Color(0.95f, 0.91f, 0.81f, 1f));
                cell.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
                cell.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
                ScrollUiKit.Label(cell.transform, "Digit", digits[i], 18, TextAnchor.MiddleCenter, ink, Vector2.zero, Vector2.one, 10,
                    FontStyle.Bold);
            }
        }

        // A class seal (the Garden Monk's) in the frame's colour.
        private static void SealRing(RectTransform area, Color ring)
        {
            var disc = ScrollUiKit.Spot(area, "Disc", new Vector2(0.5f, 0.5f), new Vector2(82f, 82f), new Color(0.94f, 0.89f, 0.78f, 1f),
                ScrollUiKit.Disc());
            ScrollUiKit.Icon(disc.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(ClassId.GardenMonk),
                new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Color.white);
            var r = ScrollUiKit.Fill(disc.transform, "Ring", Vector2.zero, Vector2.one, ring, ScrollUiKit.Ring());
            r.rectTransform.offsetMin = new Vector2(-6f, -6f);
            r.rectTransform.offsetMax = new Vector2(6f, 6f);
        }

        // A floor seal as the run end presses it: the first boss's mask on the seal's colour, square or round.
        private static void PathSeal(RectTransform area, Look look)
        {
            var seal = ScrollUiKit.Spot(area, "Seal", new Vector2(0.5f, 0.5f), new Vector2(78f, 78f), look.Colour,
                look.Round ? ScrollUiKit.Disc() : ScrollUiKit.Rounded(12f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, look.Round ? 0f : -6f);
            ScrollUiKit.Icon(seal.transform, "Mask", "boss/icon_" + FloorThemeData.GetBossKey(0), new Vector2(0.14f, 0.14f),
                new Vector2(0.86f, 0.86f), Color.white);
        }

        private void Choose(Look look)
        {
            _chosen = look;
            Refresh();
        }

        private void Refresh()
        {
            _purse.text = LocalizationService.Format("StampShop.Purse", "Ink stamps <color=#9E2A2B>{0}</color>", _state.TotalInkStamps);
            foreach (var (look, button) in _items)
            {
                var owns = InkStampShop.Owns(_state, look);
                button.transform.Find("Owned").gameObject.SetActive(owns);
                button.transform.Find("Worn").gameObject.SetActive(InkStampShop.IsWorn(_state, look));
                var price = button.transform.Find("Price").GetComponent<Text>();
                price.text = owns ? LocalizationService.T("StampShop.Owned") : LocalizationService.Format("StampShop.Price", "{0} stamps", look.Price);
                price.color = owns ? Green : ScrollUiKit.Vermilion;
            }
            _desc.text = InkStampShop.Name(_chosen) + ": " + InkStampShop.Description(_chosen);
            var label = _action.transform.Find("Label").GetComponent<Text>();
            if (InkStampShop.IsWorn(_state, _chosen))
            {
                label.text = LocalizationService.T("StampShop.Worn");
                _action.interactable = false;
            }
            else if (InkStampShop.Owns(_state, _chosen))
            {
                label.text = LocalizationService.T("StampShop.Wear");
                _action.interactable = true;
            }
            else
            {
                label.text = LocalizationService.Format("StampShop.Buy", "Buy for {0} stamps", _chosen.Price);
                _action.interactable = _state.TotalInkStamps >= _chosen.Price;
            }
            WireNavigation();
        }

        // Buy (spending stamps) or wear the chosen look, and save at once.
        private void Act()
        {
            if (InkStampShop.IsWorn(_state, _chosen)) return;
            if (InkStampShop.Owns(_state, _chosen)) InkStampShop.Wear(_state, _chosen);
            else if (!InkStampShop.TryBuy(_state, _chosen)) return;
            _save?.Invoke(_state);
            Refresh();
            // Wearing switches the button off, and Unity drops the focus with it: back to the look.
            var events = UnityEngine.EventSystems.EventSystem.current;
            var chosen = _items.Find(item => item.Look.Code == _chosen.Code).Button;
            if (events != null && chosen != null && !_action.interactable) events.SetSelectedGameObject(chosen.gameObject);
        }

        // Left and right along a shelf, up and down between shelves (same place, or the last look), down to Back and Buy.
        private void WireNavigation()
        {
            var act = _action.interactable ? _action : _back;
            var shelves = new List<List<Button>> { new List<Button>(), new List<Button>(), new List<Button>() };
            foreach (var (look, button) in _items) shelves[(int)look.Shelf].Add(button);
            for (var s = 0; s < shelves.Count; s++)
                for (var i = 0; i < shelves[s].Count; i++)
                    shelves[s][i].navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnLeft = i > 0 ? shelves[s][i - 1] : null,
                        selectOnRight = i + 1 < shelves[s].Count ? shelves[s][i + 1] : null,
                        selectOnUp = s > 0 ? shelves[s - 1][Math.Min(i, shelves[s - 1].Count - 1)] : null,
                        selectOnDown = s + 1 < shelves.Count ? shelves[s + 1][Math.Min(i, shelves[s + 1].Count - 1)] : act
                    };
            var last = shelves[shelves.Count - 1];
            _action.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = last[0], selectOnLeft = _back };
            _back.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnUp = last[0], selectOnRight = _action.interactable ? _action : null
            };
        }
    }
}
