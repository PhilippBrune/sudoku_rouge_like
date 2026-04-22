using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the shop panel: displays offers, handles selection and purchase.
    /// </summary>
    public sealed class ShopViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _pathPanel;

        private GameObject _shopPanel;
        private Text _shopSummary;
        private List<ShopOffer> _shopOffers;
        private int _selectedShopOffer = -1;
        private Button _shopBuyButton;

        // Delegate to show bag-swap panel (supplied by InRunController)
        private Action<ItemInstance, Action<int>, Action> _showSwapPanel;

        public Action<string> OnStatusChanged;
        public Action OnShopClosed;

        /// <summary>Non-null and active while the shop (or any sub-panel) is open; used by InRunController for overlay controller focus.</summary>
        public GameObject ActivePanel { get; private set; }

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
            if (run == null) return;

            BuildShopPanel();
            _shopOffers.Clear();
            _shopOffers.AddRange(run.BuildShopOffers());
            RebuildShopButtons();
            if (_shopPanel != null)
            {
                _shopPanel.SetActive(true);
                ActivePanel = _shopPanel;
                FadeInPanel(_shopPanel);
                InRunUiFactory.SelectFirstInteractable(_shopPanel);
            }
        }

        private void BuildShopPanel()
        {
            if (_shopPanel != null || _pathPanel == null) return;
            _shopPanel = InRunUiFactory.CreateOverlayPanel(_pathPanel.transform, "ShopPanel", "Shop");
            var shopBase = InRunUiFactory.PanelBg;
            _shopPanel.GetComponent<Image>().color = new Color(shopBase.r, shopBase.g, shopBase.b, 0.72f);
            InRunUiFactory.AddPanelBackground(_shopPanel.transform, "bg_shop");
            _shopSummary = _shopPanel.transform.Find("Summary")?.GetComponent<Text>();
            _shopPanel.SetActive(false);
        }

        private void RebuildShopButtons()
        {
            if (_shopPanel == null) return;
            InRunUiFactory.ClearNamedChildren(_shopPanel.transform, "Offer_");
            _selectedShopOffer = -1;
            _shopBuyButton = null;

            var s = _map?.Run?.State;
            if (_shopSummary != null && s != null)
                _shopSummary.text = $"{ResourceHeader(s)}  |  Click an item to preview, then press Buy.";

            var offerCount = Mathf.Min(3, _shopOffers.Count);
            var offerBtns = new Button[offerCount];
            for (var i = 0; i < offerCount; i++)
            {
                var offer = _shopOffers[i];
                var label = offer.Item != null
                    ? $"{ItemService.GetItemName(offer.Item.Type)}\n{offer.Price}g"
                    : $"Offer {offer.Price}g";
                var btn = InRunUiFactory.CreatePanelButton(_shopPanel.transform, $"Offer_{i}",
                    new Vector2(0.08f + i * 0.29f, 0.29f), new Vector2(0.08f + i * 0.29f + 0.26f, 0.54f), label);
                if (offer.Item != null)
                    InRunUiFactory.SetButtonIcon(btn, ItemService.GetIconName(offer.Item.Type), false, "items");
                var idx = i;
                btn.onClick.AddListener(() => SelectShopOffer(idx));
                offerBtns[i] = btn;
            }

            // Buy button (disabled until an offer is selected)
            var buyBtn = InRunUiFactory.CreatePanelButton(_shopPanel.transform, "Offer_buy",
                new Vector2(0.35f, 0.17f), new Vector2(0.65f, 0.26f), "Buy");
            buyBtn.interactable = false;
            buyBtn.gameObject.name = "ShopBuyBtn";
            buyBtn.onClick.AddListener(TryBuySelectedOffer);
            _shopBuyButton = buyBtn;

            // Explicit D-pad nav: left/right between offers, down to Buy
            for (var i = 0; i < offerCount; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0)            nav.selectOnLeft  = offerBtns[i - 1];
                if (i < offerCount-1) nav.selectOnRight = offerBtns[i + 1];
                nav.selectOnDown = buyBtn;
                offerBtns[i].navigation = nav;
            }
            // Buy: up goes to middle offer (or first if only one)
            var buyNav = buyBtn.navigation;
            buyNav.mode = Navigation.Mode.Explicit;
            buyNav.selectOnUp = offerBtns[offerCount > 1 ? offerCount / 2 : 0];
            buyBtn.navigation = buyNav;

            var run2      = _map?.Run;
            var tokens    = run2?.State.RerollTokens ?? 0;
            var rerollCost = run2 != null ? run2.GetShopRerollCost() : 15;
            var rerollLabel = tokens > 0
                ? $"Reroll ({tokens} token{(tokens != 1 ? "s" : "")})"
                : $"Reroll ({rerollCost}g)";
            var rerollBtn = InRunUiFactory.CreatePanelButton(_shopPanel.transform, "Offer_reroll",
                new Vector2(0.05f, 0.04f), new Vector2(0.30f, 0.14f), rerollLabel);
            rerollBtn.gameObject.name = "ShopRerollBtn";
            // Teal tint signals "free reroll available"; default button color otherwise.
            if (tokens > 0)
            {
                var img = rerollBtn.GetComponent<Image>();
                if (img != null) img.color = InRunUiFactory.RerollFreeColor;
            }
            rerollBtn.onClick.AddListener(() =>
            {
                var run = _map?.Run;
                if (run == null) return;
                if (!run.RerollShop()) { OnStatusChanged?.Invoke("Not enough gold."); return; }
                _shopOffers.Clear();
                _shopOffers.AddRange(run.CurrentShopOffers);
                RebuildShopButtons();
            });

            var skip = InRunUiFactory.CreatePanelButton(_shopPanel.transform, "Offer_skip",
                new Vector2(0.70f, 0.04f), new Vector2(0.95f, 0.14f), "Skip");
            skip.onClick.AddListener(() =>
            {
                _shopOffers.Clear();
                _selectedShopOffer = -1;
                ActivePanel = null;
                InRunUiFactory.HidePanel(_shopPanel);
                OnShopClosed?.Invoke();
            });
        }

        private void SelectShopOffer(int idx)
        {
            _selectedShopOffer = idx;
            var offer = (idx >= 0 && idx < _shopOffers.Count) ? _shopOffers[idx] : null;

            // Update summary with item description
            if (_shopSummary != null && offer != null)
            {
                var s = _map?.Run?.State;
                var header = s != null ? $"{ResourceHeader(s)}  |  " : "";
                var desc = offer.Item != null
                    ? $"{header}{ItemService.GetItemName(offer.Item.Type)} ({offer.Item.Rarity}) \u2014 {offer.Price}g\n{ItemService.GetItemDescription(offer.Item.Type, offer.Item.Rarity)}"
                    : $"{header}Offer \u2014 {offer.Price}g";
                _shopSummary.text = desc;
            }

            // Highlight selected, un-highlight others; enable Buy button.
            // Set Image.color directly — btn.colors.normalColor is multiplied with the
            // image's dark base color, making gold highlights nearly invisible.
            for (var i = 0; i < Mathf.Min(3, _shopOffers.Count); i++)
            {
                var offerGo = _shopPanel?.transform.Find($"Offer_{i}");
                if (offerGo == null) continue;
                var img = offerGo.GetComponent<Image>();
                if (img != null)
                    img.color = (i == idx) ? GamePalette.AccentGold : InRunUiFactory.BtnColor;
            }

            if (_shopBuyButton != null)
                _shopBuyButton.interactable = true;
        }

        /// <summary>Builds the gold + token header shown in the shop summary line.</summary>
        private static string ResourceHeader(RunState s) =>
            $"Gold: {s.CurrentGold}  |  Tokens: {s.RerollTokens}";

        private void TryBuySelectedOffer()
        {
            var run = _map?.Run;
            if (run == null || _selectedShopOffer < 0 || _selectedShopOffer >= _shopOffers.Count) return;
            var offer = _shopOffers[_selectedShopOffer];
            if (offer == null || offer.Item == null) return;

            // Gold check before proceeding
            if (run.State.CurrentGold < offer.Price) { OnStatusChanged?.Invoke("Not enough gold."); return; }

            if (offer.Item != null && run.IsBagFull())
            {
                // Bag is full — show swap panel; deduct gold only after confirmed.
                // Capture the index as a local so later UI interactions cannot
                // mutate _selectedShopOffer and cause the wrong offer to be purchased.
                var selectedOfferIdx = _selectedShopOffer;
                _showSwapPanel?.Invoke(
                    offer.Item,
                    slotToReplace =>
                    {
                        if (!run.TryPurchaseShopOffer(selectedOfferIdx))
                        {
                            OnStatusChanged?.Invoke("Not enough gold.");
                            return;
                        }
                        run.ReplaceItemInInventory(slotToReplace, offer.Item);
                        new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordItemDiscovery(offer.Item.Type);
                        _shopOffers.Clear();
                        _selectedShopOffer = -1;
                        ActivePanel = null;
                        InRunUiFactory.HidePanel(_shopPanel);
                        OnShopClosed?.Invoke();
                    },
                    () => { /* no-op — swap panel hiding is handled by ShowBagSwapPanel wrapper */ });
                return;
            }

            if (!run.TryPurchaseShopOffer(_selectedShopOffer)) { OnStatusChanged?.Invoke("Not enough gold."); return; }
            var purchasedItem = offer.Item;
            _shopOffers.Clear();
            _selectedShopOffer = -1;
            ActivePanel = null;
            InRunUiFactory.HidePanel(_shopPanel);
            if (purchasedItem != null)
                new ProfileService(new SaveFileService(SaveProfileService.ActiveSlot)).RecordItemDiscovery(purchasedItem.Type);
            OnShopClosed?.Invoke();
        }

        private void FadeInPanel(GameObject panel)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(AnimationHelper.FadeIn(cg, AnimationHelper.MenuPanelDuration));
        }
    }
}
