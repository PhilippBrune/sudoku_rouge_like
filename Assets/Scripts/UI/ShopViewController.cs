using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Items;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-009] The shop as the market stall (layout-proposals.md, "Shop"): the goods stand on the counter of
    /// the stall painting with wooden price tags; the chosen one opens on a paper slip with its rule, Buy and the gold
    /// after buying; the purse (gold, reroll tokens, bag slots) hangs on a paper tag. Reroll and Leave stay at the
    /// bottom. Object names (Offer_i, ShopBuyBtn, ShopRerollBtn, Offer_skip) are kept for wiring and tests.
    /// </summary>
    public sealed class ShopViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _pathPanel;

        private GameObject _shopPanel;
        private Transform _goods;
        private Image _slip;
        private List<ShopOffer> _shopOffers;
        private readonly List<Button> _offerButtons = new List<Button>();
        private int _selectedShopOffer = -1;
        private Button _shopBuyButton;
        private Button _rerollButton;
        private Button _leaveButton;
        private Transform _purse;
        private bool _swapOpen;

        // Delegate to show bag-swap panel (supplied by InRunController)
        private Action<ItemInstance, Action<int>, Action> _showSwapPanel;

        public Action<string> OnStatusChanged;
        public Action OnShopClosed;

        /// <summary>Non-null and active while the shop (or any sub-panel) is open; used by InRunController for overlay controller focus.</summary>
        public GameObject ActivePanel { get; private set; }

        private const float GoodsBottom = 0.4926f;
        private const float GoodsTop = 0.7222f;
        private const float GoodsWidth = 0.1135f;
        private static readonly Color StallWood = new Color(0.60f, 0.38f, 0.20f, 1f);
        private static readonly Color BoxWood = new Color(0.30f, 0.21f, 0.14f, 1f);

        public void Configure(RunMapController map, GameObject pathPanel,
            Action<ItemInstance, Action<int>, Action> showSwapPanel)
        {
            _map = map;
            _pathPanel = pathPanel;
            _showSwapPanel = showSwapPanel;
            _shopOffers = new List<ShopOffer>();
        }

        public void ShowShop()
        {
            var run = _map?.Run;
            if (run == null || _pathPanel == null) return;

            _shopOffers.Clear();
            _shopOffers.AddRange(run.BuildShopOffers());
            BuildStall();
            RebuildGoods();
            ActivePanel = _shopPanel;
            FadeInPanel(_shopPanel);
        }

        // SH-1: keyboard 1-3 shortcut focuses and previews the given offer slot
        public void SelectSlotByIndex(int i)
        {
            if (_shopPanel == null || !_shopPanel.activeSelf || _swapOpen || i < 0 || i >= _offerButtons.Count) return;
            var btn = _offerButtons[i];
            if (btn == null || !btn.IsInteractable()) return;
            EventSystem.current?.SetSelectedGameObject(btn.gameObject);
            SelectShopOffer(i);
        }

        // ── the stall ──

        private void BuildStall()
        {
            CloseStall();
            var screen = ScrollUiKit.Scene(_pathPanel, "ShopPanel", "bg_shop", new Color(0.86f, 0.86f, 0.82f, 1f));
            _shopPanel = screen.gameObject;

            var sign = ScrollUiKit.Fill(screen.transform, "ShopSign", new Vector2(0.4339f, 0.8935f), new Vector2(0.5661f, 0.963f),
                new Color(0.45f, 0.28f, 0.15f, 1f), ScrollUiKit.Rounded(8f));
            AddShadow(sign.gameObject, 6f);
            var rim = ScrollUiKit.Area(sign.transform, "Rim", Vector2.zero, Vector2.one);
            rim.offsetMin = new Vector2(4f, 4f);
            rim.offsetMax = new Vector2(-4f, -4f);
            ScrollUiKit.InkFrame(rim, new Color(ScrollUiKit.Gold.r, ScrollUiKit.Gold.g, ScrollUiKit.Gold.b, 0.7f), 2f);
            ScrollUiKit.Label(sign.transform, "Title", T("InRun.Shop.Sign"), 44, TextAnchor.MiddleCenter, ScrollUiKit.PaperText,
                Vector2.zero, Vector2.one, 24);

            var counter = ScrollUiKit.Fill(screen.transform, "ShopCounter", new Vector2(0.1302f, 0.4259f), new Vector2(0.8698f, 0.4815f),
                StallWood, ScrollUiKit.Rounded(6f));
            AddShadow(counter.gameObject, 8f);
            _goods = ScrollUiKit.Area(screen.transform, "ShopGoods", Vector2.zero, Vector2.one);

            _slip = ScrollUiKit.PaperTag(screen.transform, "ShopSlip", new Vector2(0.6823f, 0.1806f), new Vector2(0.9531f, 0.4074f), -1f);
            _purse = ScrollUiKit.PaperTag(screen.transform, "ShopPurse", new Vector2(0.0469f, 0.2037f), new Vector2(0.2188f, 0.3935f), 1f).transform;

            _rerollButton = ScrollUiKit.RimButton(screen.transform, "ShopRerollBtn", new Vector2(0.387f, 0.037f), new Vector2(0.4885f, 0.0852f),
                "", 20, ScrollUiKit.Gold, ScrollUiKit.OfferingFocus, ScrollUiKit.OfferingText);
            _rerollButton.onClick.AddListener(Reroll);
            _leaveButton = ScrollUiKit.RimButton(screen.transform, "Offer_skip", new Vector2(0.499f, 0.037f), new Vector2(0.6125f, 0.0852f),
                T("InRun.Shop.Leave"), 20, ScrollUiKit.Gold, ScrollUiKit.OfferingFocus, ScrollUiKit.OfferingText);
            _leaveButton.onClick.AddListener(CloseShop);
        }

        private void RebuildGoods()
        {
            if (_shopPanel == null) return;
            foreach (Transform child in _goods) DestroyNow(child.gameObject);
            _offerButtons.Clear();
            _selectedShopOffer = -1;

            var count = Mathf.Min(3, _shopOffers.Count);
            const float step = 0.2253f;
            for (var i = 0; i < count; i++)
            {
                var centre = 0.5013f + (i - (count - 1) * 0.5f) * step;
                _offerButtons.Add(BuildGood(i, _shopOffers[i], centre));
            }

            RefreshPurse();
            RefreshReroll();
            RefreshSlip();
            WireNavigation();
            var first = _offerButtons.Count > 0 ? _offerButtons[0] : _leaveButton;
            EventSystem.current?.SetSelectedGameObject(first.gameObject);
            if (_offerButtons.Count > 0) SelectShopOffer(0);
        }

        // One good on the counter: a dark wooden box with the item, a price tag, the name and rarity below it.
        private Button BuildGood(int index, ShopOffer offer, float centre)
        {
            var box = ScrollUiKit.Fill(_goods, $"Offer_{index}", new Vector2(centre - GoodsWidth * 0.5f, GoodsBottom),
                new Vector2(centre + GoodsWidth * 0.5f, GoodsTop), BoxWood, ScrollUiKit.Rounded(14f));
            box.raycastTarget = true;
            AddShadow(box.gameObject, 6f);
            var frame = ScrollUiKit.Area(box.transform, "Frame", Vector2.zero, Vector2.one);
            ScrollUiKit.InkFrame(frame, ScrollUiKit.Gold, 3f);
            frame.gameObject.SetActive(false);
            var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box;
            // The box is dimmed a little at rest and brightens under the pointer or the controller focus.
            var colors = ScrollUiKit.StateColors(new Color(0.86f, 0.86f, 0.86f, 1f));
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;

            var item = offer.Item;
            if (item != null)
                ScrollUiKit.Icon(box.transform, "Icon", "items/icon_" + ItemService.GetIconName(item.Type),
                    new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.90f), Color.white);
            else
                ScrollUiKit.Label(box.transform, "Icon", "?", 60, TextAnchor.MiddleCenter, ScrollUiKit.Gold, Vector2.zero, Vector2.one);

            var tag = ScrollUiKit.Fill(box.transform, "PriceTag", new Vector2(0.70f, -0.13f), new Vector2(1.12f, 0.10f),
                new Color(0.88f, 0.72f, 0.48f, 1f), ScrollUiKit.PriceTag());
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            AddShadow(tag.gameObject, 3f);
            ScrollUiKit.Label(tag.transform, "Price", F("InRun.Shop.PriceTag", offer.Price), 22, TextAnchor.MiddleCenter,
                ScrollUiKit.WoodTileText, new Vector2(0.24f, 0f), Vector2.one, 12, FontStyle.Italic);

            var name = ScrollUiKit.Label(_goods, $"OfferName_{index}",
                item != null ? ItemService.GetItemName(item.Type) : T("InRun.Shop.Mystery"), 28, TextAnchor.MiddleCenter,
                ScrollUiKit.PaperText, new Vector2(centre - 0.10f, 0.4485f), new Vector2(centre + 0.10f, 0.4805f), 16);
            AddTextShadow(name);
            if (item != null)
                AddTextShadow(ScrollUiKit.Label(_goods, $"OfferRarity_{index}", RarityLabel(item.Rarity).ToUpperInvariant(), 15,
                    TextAnchor.MiddleCenter, RarityOnWood(item.Rarity), new Vector2(centre - 0.10f, 0.4290f),
                    new Vector2(centre + 0.10f, 0.4485f), 10));

            var idx = index;
            button.onClick.AddListener(() => SelectShopOffer(idx));
            // SH-2: controller D-pad focus previews the offer
            var trigger = box.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            entry.callback.AddListener(_ => SelectShopOffer(idx));
            trigger.triggers.Add(entry);
            return button;
        }

        private void SelectShopOffer(int idx)
        {
            if (idx < 0 || idx >= _offerButtons.Count) return;
            _selectedShopOffer = idx;
            for (var i = 0; i < _offerButtons.Count; i++)
            {
                var frame = _offerButtons[i] != null ? _offerButtons[i].transform.Find("Frame") : null;
                if (frame != null) frame.gameObject.SetActive(i == idx);
            }
            RefreshSlip();
        }

        // The chosen good on the paper slip: where we are, its name and rule, Buy and the gold after buying.
        private void RefreshSlip()
        {
            if (_slip == null) return;
            foreach (Transform child in _slip.transform)
                if (child.name != "Cord") DestroyNow(child.gameObject);
            _shopBuyButton = null;

            var run = _map?.Run;
            var state = run?.State;
            if (state == null) return;
            ScrollUiKit.Label(_slip.transform, "Eyebrow",
                F("InRun.Shop.SlipEyebrow", state.CurrentFloor + 1, FloorThemeData.GetFloorName(state.CurrentFloor)).ToUpperInvariant(),
                15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.05f, 0.82f), new Vector2(0.96f, 0.95f), 10);

            var offer = _selectedShopOffer >= 0 && _selectedShopOffer < _shopOffers.Count ? _shopOffers[_selectedShopOffer] : null;
            var item = offer?.Item;
            ScrollUiKit.Label(_slip.transform, "ItemName",
                item != null ? ItemService.GetItemName(item.Type) : T(offer == null ? "InRun.Shop.PickHint" : "InRun.Shop.Mystery"), 34,
                TextAnchor.MiddleLeft, ScrollUiKit.Ink, new Vector2(0.05f, 0.62f), new Vector2(0.96f, 0.82f), 20);
            if (item != null)
                ScrollUiKit.Label(_slip.transform, "Rule", ItemService.GetItemDescription(item.Type, item.Rarity), 20, TextAnchor.UpperLeft,
                    ScrollUiKit.Ink, new Vector2(0.05f, 0.36f), new Vector2(0.96f, 0.61f), 13);

            var canAfford = offer != null && state.CurrentGold >= offer.Price;
            _shopBuyButton = ScrollUiKit.PillButton(_slip.transform, "ShopBuyBtn", new Vector2(0.055f, 0.08f), new Vector2(0.40f, 0.30f),
                offer != null ? F("InRun.Shop.BuyFor", offer.Price) : T("InRun.Shop.Buy"), 22, ScrollUiKit.Vermilion, Color.white, 24f);
            _shopBuyButton.interactable = item != null && canAfford;
            _shopBuyButton.onClick.AddListener(TryBuySelectedOffer);
            if (offer != null)
                ScrollUiKit.Label(_slip.transform, "After",
                    canAfford ? F("InRun.Shop.GoldAfter", state.CurrentGold, state.CurrentGold - offer.Price) : T("InRun.Shop.NotEnoughGold"),
                    18, TextAnchor.MiddleLeft, canAfford ? ScrollUiKit.InkSoft : ScrollUiKit.Vermilion,
                    new Vector2(0.43f, 0.08f), new Vector2(0.97f, 0.30f), 12);
            WireNavigation();
        }

        // The purse: gold, reroll tokens and the bag's slots (held items, empty slots dashed).
        private void RefreshPurse()
        {
            if (_purse == null) return;
            foreach (Transform child in _purse)
                if (child.name != "Cord") DestroyNow(child.gameObject);
            var state = _map?.Run?.State;
            if (state == null) return;

            ScrollUiKit.Label(_purse, "Header", T("InRun.Shop.Purse").ToUpperInvariant(), 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.10f, 0.82f), new Vector2(0.92f, 0.95f), 10);
            ScrollUiKit.ValueRow(_purse, "Gold", new Vector2(0.10f, 0.60f), new Vector2(0.92f, 0.78f), T("InRun.Reward.Row.Gold"),
                state.CurrentGold.ToString(), 21, ScrollUiKit.Ink);
            ScrollUiKit.ValueRow(_purse, "Tokens", new Vector2(0.10f, 0.40f), new Vector2(0.92f, 0.58f), T("InRun.Shop.Row.Tokens"),
                state.RerollTokens.ToString(), 21, ScrollUiKit.Ink);

            var slots = Mathf.Clamp(state.ItemSlots, 0, 6);
            for (var i = 0; i < slots; i++)
            {
                var x = 0.10f + i * 0.145f;
                var cell = ScrollUiKit.Area(_purse, $"BagSlot_{i}", new Vector2(x, 0.08f), new Vector2(x + 0.125f, 0.33f));
                var item = i < state.HeldItems.Count ? state.HeldItems[i] : null;
                if (item != null)
                    ScrollUiKit.Icon(cell, "Icon", "items/icon_" + ItemService.GetIconName(item.Type), Vector2.zero, Vector2.one, Color.white);
                ScrollUiKit.InkFrame(cell, new Color(ScrollUiKit.InkSoft.r, ScrollUiKit.InkSoft.g, ScrollUiKit.InkSoft.b, 0.55f), 1.5f);
            }
        }

        private void RefreshReroll()
        {
            if (_rerollButton == null) return;
            var run = _map?.Run;
            var tokens = run?.State.RerollTokens ?? 0;
            var cost = run != null ? run.GetShopRerollCost() : 15;
            var label = _rerollButton.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
                label.text = tokens > 0
                    ? LocalizationService.FormatPlural(tokens, "InRun.Shop.Reroll.Token.One", "InRun.Shop.Reroll.Token.Many",
                        "Reroll ({0} token)", "Reroll ({0} tokens)")
                    : F("InRun.Shop.Reroll.Gold", cost);
            _rerollButton.interactable = run != null && (tokens > 0 || run.State.CurrentGold >= cost);
        }

        private void WireNavigation()
        {
            for (var i = 0; i < _offerButtons.Count; i++)
                _offerButtons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _offerButtons[i - 1] : null,
                    selectOnRight = i < _offerButtons.Count - 1 ? _offerButtons[i + 1] : null,
                    selectOnDown = _shopBuyButton != null && _shopBuyButton.interactable ? _shopBuyButton : (Selectable)_rerollButton
                };
            var centre = _offerButtons.Count > 0
                ? _offerButtons[_selectedShopOffer >= 0 ? _selectedShopOffer : 0]
                : null;
            if (_shopBuyButton != null)
                _shopBuyButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = centre, selectOnDown = _rerollButton };
            if (_rerollButton != null)
                _rerollButton.navigation = new Navigation
                    { mode = Navigation.Mode.Explicit, selectOnUp = (Selectable)_shopBuyButton ?? centre, selectOnRight = _leaveButton };
            if (_leaveButton != null)
                _leaveButton.navigation = new Navigation
                    { mode = Navigation.Mode.Explicit, selectOnUp = (Selectable)_shopBuyButton ?? centre, selectOnLeft = _rerollButton };
        }

        // ── actions ──

        private void Reroll()
        {
            var run = _map?.Run;
            if (run == null) return;
            if (!run.RerollShop()) { OnStatusChanged?.Invoke(T("InRun.Shop.NotEnoughGold")); return; }
            _shopOffers.Clear();
            _shopOffers.AddRange(run.CurrentShopOffers);
            RebuildGoods();
        }

        private void TryBuySelectedOffer()
        {
            var run = _map?.Run;
            if (run == null || _selectedShopOffer < 0 || _selectedShopOffer >= _shopOffers.Count) return;
            var offer = _shopOffers[_selectedShopOffer];
            if (offer?.Item == null) return;

            if (run.State.CurrentGold < offer.Price) { OnStatusChanged?.Invoke(T("InRun.Shop.NotEnoughGold")); return; }

            if (run.IsBagFull())
            {
                // Bag full: the swap panel asks which item to give up; gold is paid only when the swap happens.
                // The index is captured so later clicks cannot change which offer is bought.
                var selectedOfferIdx = _selectedShopOffer;
                _swapOpen = true;
                _showSwapPanel?.Invoke(
                    offer.Item,
                    slotToReplace =>
                    {
                        _swapOpen = false;
                        // Pays and swaps in one step; nothing is charged if the swap cannot happen (GAP-036).
                        if (!run.TryPurchaseShopOffer(selectedOfferIdx, slotToReplace))
                        {
                            OnStatusChanged?.Invoke(T("InRun.Shop.NotEnoughGold"));
                            return;
                        }
                        RecordItem(offer.Item);
                        CloseShop();
                    },
                    () =>
                    {
                        // The swap layer hides itself; the stall stays open with the focus back on Buy.
                        _swapOpen = false;
                        if (_shopBuyButton != null) EventSystem.current?.SetSelectedGameObject(_shopBuyButton.gameObject);
                    });
                return;
            }

            if (!run.TryPurchaseShopOffer(_selectedShopOffer)) { OnStatusChanged?.Invoke(T("InRun.Shop.NotEnoughGold")); return; }
            RecordItem(offer.Item);
            CloseShop();
        }

        private static void RecordItem(ItemInstance item) =>
            new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordItemDiscovery(item.Type);

        private void CloseShop()
        {
            if (_shopPanel == null) return;
            _shopOffers.Clear();
            CloseStall();
            OnShopClosed?.Invoke();
        }

        /// <summary>Closes the stall without leaving through it; used when the run ends while it is open.</summary>
        public void CloseScenes()
        {
            _swapOpen = false;
            _shopOffers?.Clear();
            CloseStall();
        }

        private void CloseStall()
        {
            _offerButtons.Clear();
            _selectedShopOffer = -1;
            _shopBuyButton = null;
            if (_shopPanel != null) DestroyNow(_shopPanel);
            _shopPanel = null;
            ActivePanel = null;
        }

        // ── helpers ──

        private void FadeInPanel(GameObject panel)
        {
            // No ?? here: GetComponent returns Unity's "missing" object, not null, when the component is absent.
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            if (isActiveAndEnabled) StartCoroutine(AnimationHelper.FadeIn(cg, AnimationHelper.MenuPanelDuration));
            else cg.alpha = 1f;
        }

        private static void AddShadow(GameObject go, float depth)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -depth);
        }

        private static void AddTextShadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // Destroy is deferred to the end of the frame; hide and rename first so a rebuild finds no old objects.
        private static void DestroyNow(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.name += "_old";
            Object.Destroy(go);
        }

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);

        // Rarity in a colour that stays readable on the counter's wood (the tag colours are too dark there).
        private static Color RarityOnWood(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Epic => new Color(1.00f, 0.86f, 0.42f, 1f),
            ItemRarity.Rare => new Color(0.72f, 0.82f, 1.00f, 1f),
            _ => new Color(0.96f, 0.88f, 0.72f, 1f)
        };

        private static string RarityLabel(ItemRarity rarity) =>
            LocalizationService.T($"Item.Rarity.{rarity}", rarity.ToString());
    }
}
