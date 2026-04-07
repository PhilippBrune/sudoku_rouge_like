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

        // Delegate to show bag-swap panel (supplied by InRunController)
        private Action<ItemInstance, Action<int>, Action> _showSwapPanel;

        public Action<string> OnStatusChanged;
        public Action OnShopClosed;

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
            if (_shopPanel != null) _shopPanel.SetActive(true);
        }

        private void BuildShopPanel()
        {
            if (_shopPanel != null || _pathPanel == null) return;
            _shopPanel = InRunUiFactory.CreateOverlayPanel(_pathPanel.transform, "ShopPanel", "Shop");
            _shopSummary = _shopPanel.transform.Find("Summary")?.GetComponent<Text>();
            _shopPanel.SetActive(false);
        }

        private void RebuildShopButtons()
        {
            if (_shopPanel == null) return;
            InRunUiFactory.ClearNamedChildren(_shopPanel.transform, "Offer_");
            _selectedShopOffer = -1;

            var s = _map?.Run?.State;
            if (_shopSummary != null && s != null)
                _shopSummary.text = $"Gold: {s.CurrentGold}  |  Click an item to preview, then press Buy.";

            for (var i = 0; i < Mathf.Min(3, _shopOffers.Count); i++)
            {
                var offer = _shopOffers[i];
                var label = offer.Item != null
                    ? $"{ItemService.GetItemName(offer.Item.Type)}\n{offer.Price}g"
                    : $"Offer {offer.Price}g";
                var btn = InRunUiFactory.CreatePanelButton(_shopPanel.transform, $"Offer_{i}",
                    new Vector2(0.08f + i * 0.29f, 0.40f), new Vector2(0.08f + i * 0.29f + 0.26f, 0.70f), label);
                if (offer.Item != null)
                    InRunUiFactory.SetButtonIcon(btn, ItemService.GetIconName(offer.Item.Type));
                var idx = i;
                btn.onClick.AddListener(() => SelectShopOffer(idx));
            }

            // Buy button (disabled until an offer is selected)
            var buyBtn = InRunUiFactory.CreatePanelButton(_shopPanel.transform, "Offer_buy",
                new Vector2(0.35f, 0.26f), new Vector2(0.65f, 0.36f), "Buy");
            buyBtn.interactable = false;
            buyBtn.gameObject.name = "ShopBuyBtn";
            buyBtn.onClick.AddListener(TryBuySelectedOffer);

            var skip = InRunUiFactory.CreatePanelButton(_shopPanel.transform, "Offer_skip",
                new Vector2(0.35f, 0.13f), new Vector2(0.65f, 0.23f), "Skip");
            skip.onClick.AddListener(() =>
            {
                _shopOffers.Clear();
                _selectedShopOffer = -1;
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
                var goldTxt = s != null ? $"Gold: {s.CurrentGold}  |  " : "";
                var desc = offer.Item != null
                    ? $"{goldTxt}{ItemService.GetItemName(offer.Item.Type)} ({offer.Item.Rarity}) \u2014 {offer.Price}g\n{ItemService.GetItemDescription(offer.Item.Type, offer.Item.Rarity)}"
                    : $"{goldTxt}Offer \u2014 {offer.Price}g";
                _shopSummary.text = desc;
            }

            // Highlight selected, un-highlight others; enable Buy button
            for (var i = 0; i < Mathf.Min(3, _shopOffers.Count); i++)
            {
                var offerGo = _shopPanel?.transform.Find($"Offer_{i}");
                if (offerGo == null) continue;
                var btn = offerGo.GetComponent<Button>();
                if (btn == null) continue;
                var cols = btn.colors;
                cols.normalColor = (i == idx) ? new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.45f) : InRunUiFactory.BtnColor;
                btn.colors = cols;
            }

            var buyGo = _shopPanel?.transform.Find("ShopBuyBtn");
            if (buyGo != null)
            {
                var buyBtn = buyGo.GetComponent<Button>();
                if (buyBtn != null) buyBtn.interactable = true;
            }
        }

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
                // Bag is full — show swap panel; deduct gold only after confirmed
                _showSwapPanel?.Invoke(
                    offer.Item,
                    slotToReplace =>
                    {
                        if (!run.TryPurchaseShopOffer(_selectedShopOffer))
                        {
                            OnStatusChanged?.Invoke("Not enough gold.");
                            return;
                        }
                        run.ReplaceItemInInventory(slotToReplace, offer.Item);
                        new ProfileService(new SaveFileService()).RecordItemDiscovery(offer.Item.Type);
                        _shopOffers.Clear();
                        _selectedShopOffer = -1;
                        InRunUiFactory.HidePanel(_shopPanel);
                        OnShopClosed?.Invoke();
                    },
                    () => { /* abort — just hide swap panel, shop remains */ });
                return;
            }

            if (!run.TryPurchaseShopOffer(_selectedShopOffer)) { OnStatusChanged?.Invoke("Not enough gold."); return; }
            var purchasedItem = offer.Item;
            _shopOffers.Clear();
            _selectedShopOffer = -1;
            InRunUiFactory.HidePanel(_shopPanel);
            if (purchasedItem != null)
                new ProfileService(new SaveFileService()).RecordItemDiscovery(purchasedItem.Type);
            OnShopClosed?.Invoke();
        }
    }
}
