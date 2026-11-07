using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-044] The bag-full screen (playtest 0.4.1, owner's pick B of docs/design/layout-proposals/bag_swap): two
    // paper cards, the new item against the one from the bag that would go, and the bag below as a row of chips. Choosing a
    // chip (the D-pad, the arrows, the pointer) shows its card; pressing it or Swap swaps, Keep my bag (or B) keeps the bag.
    public sealed partial class RewardViewController
    {
        private Image _swapCardIcon;
        private Text _swapCardName, _swapCardText, _swapCardEyebrow;
        private Image _swapCardRarity;
        private Text _swapCardRarityText;
        private int _swapChosen;

        private void BuildBagSwapScene(Transform layer, ItemInstance newItem)
        {
            var state = _map?.Run?.State;
            var held = state?.HeldItems ?? new List<ItemInstance>();
            var slots = Mathf.Max(held.Count, state?.ItemSlots ?? 0);
            ScrollUiKit.Banner(layer, "SwapBanner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "SwapEyebrow", F("InRun.BagSwap.Eyebrow", held.Count, slots).ToUpperInvariant(),
                "SwapHeading", F("InRun.BagSwap.Heading", ItemService.GetItemName(newItem.Type)), 40);

            ItemCard(layer, "SwapNewCard", 0.156f, T("InRun.BagSwap.New").ToUpperInvariant(), newItem, out _, out _, out _, out _, out _);
            ScrollUiKit.Label(layer, "SwapArrow", "⇄", 64, TextAnchor.MiddleCenter, ScrollUiKit.Gold,
                new Vector2(0.45f, 0.42f), new Vector2(0.55f, 0.54f));
            ItemCard(layer, "SwapBagCard", 0.573f, string.Empty, held.Count > 0 ? held[0] : null,
                out _swapCardEyebrow, out _swapCardIcon, out _swapCardName, out _swapCardRarity, out _swapCardText);
            _swapCardRarityText = _swapCardRarity.GetComponentInChildren<Text>();

            var chips = new Button[held.Count];
            var width = 0.062f;
            var gap = 0.010f;
            var left = 0.5f - (held.Count * width + (held.Count - 1) * gap) / 2f;
            for (var i = 0; i < held.Count; i++)
            {
                var x = left + i * (width + gap);
                var chip = ScrollUiKit.Fill(layer, $"SwapSlot_{i}", new Vector2(x, 0.165f), new Vector2(x + width, 0.275f),
                    new Color(0.85f, 0.67f, 0.45f, 1f), ScrollUiKit.Rounded(12f));
                if (held[i] != null)
                    ScrollUiKit.Icon(chip.transform, "Icon", "items/icon_" + ItemService.GetIconName(held[i].Type),
                        new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Color.white);
                var button = chip.gameObject.AddComponent<Button>();
                button.targetGraphic = chip;
                button.colors = ScrollUiKit.StateColors(Color.white);
                var index = i;
                button.onClick.AddListener(() => _onSwapSlotChosen?.Invoke(index));
                chip.gameObject.AddComponent<FocusNotifier>().OnFocus = () => ShowSwapChoice(index);
                var hover = chip.gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => EventSystem.current?.SetSelectedGameObject(chip.gameObject));
                hover.triggers.Add(enter);
                chips[i] = button;
            }

            var swap = ScrollUiKit.RimButton(layer, "SwapConfirm", new Vector2(0.39f, 0.065f), new Vector2(0.49f, 0.12f),
                T("InRun.BagSwap.Swap"), 22, ScrollUiKit.Vermilion, ScrollUiKit.OfferingFocus, ScrollUiKit.OfferingText);
            swap.onClick.AddListener(() => _onSwapSlotChosen?.Invoke(_swapChosen));
            var keep = ScrollUiKit.RimButton(layer, "SwapAbort", new Vector2(0.51f, 0.065f), new Vector2(0.63f, 0.12f),
                T("InRun.BagSwap.KeepBag"), 22, ScrollUiKit.Gold, ScrollUiKit.OfferingFocus, ScrollUiKit.OfferingText);
            keep.onClick.AddListener(() => _onSwapAborted?.Invoke());
            var hint = ScrollUiKit.Label(layer, "SwapHint", T("InRun.BagSwap.Hint"), 18, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.79f, 0.66f, 1f), new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.055f), 12);
            DeviceHint.Attach(hint, "InRun.BagSwap.Hint");

            // The chips step left and right; down leads to Swap and Keep my bag, up from them back to the chosen chip.
            for (var i = 0; i < chips.Length; i++)
                chips[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? chips[i - 1] : null,
                    selectOnRight = i < chips.Length - 1 ? chips[i + 1] : null,
                    selectOnDown = swap
                };
            var up = chips.Length > 0 ? chips[0] : null;
            swap.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = keep, selectOnUp = up };
            keep.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = swap, selectOnUp = up };
            ShowSwapChoice(0);
            EventSystem.current?.SetSelectedGameObject(chips.Length > 0 ? chips[0].gameObject : keep.gameObject);
        }

        // One paper card: the eyebrow, the icon, the name, the rarity pill and what the item does.
        private static void ItemCard(Transform layer, string name, float x, string eyebrow, ItemInstance item, out Text eyebrowText,
            out Image icon, out Text title, out Image rarity, out Text text)
        {
            var card = ScrollUiKit.Fill(layer, name, new Vector2(x, 0.315f), new Vector2(x + 0.271f, 0.80f), ScrollUiKit.Paper);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);
            eyebrowText = ScrollUiKit.Label(card.transform, "Eyebrow", eyebrow, 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.96f), 11);
            icon = ScrollUiKit.Icon(card.transform, "Icon", item != null ? "items/icon_" + ItemService.GetIconName(item.Type) : null,
                new Vector2(0.36f, 0.58f), new Vector2(0.64f, 0.86f), Color.white);
            title = ScrollUiKit.Label(card.transform, "Name", item != null ? ItemService.GetItemName(item.Type) : string.Empty, 34,
                TextAnchor.MiddleCenter, ScrollUiKit.Ink, new Vector2(0.06f, 0.46f), new Vector2(0.94f, 0.57f), 18);
            var rarityValue = item?.Rarity ?? ItemRarity.Normal;
            rarity = ScrollUiKit.Pill(card.transform, "Rarity", new Vector2(0.36f, 0.385f), new Vector2(0.64f, 0.44f),
                RarityLabel(rarityValue).ToUpperInvariant(), ScrollUiKit.RarityColor(rarityValue), ScrollUiKit.RarityTextColor(rarityValue), 15);
            text = ScrollUiKit.Label(card.transform, "Text", item != null ? ItemService.GetItemDescription(item.Type, item.Rarity) : string.Empty,
                21, TextAnchor.UpperCenter, ScrollUiKit.Ink, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.36f), 13);
        }

        // The right card shows the bag's item at `index` (the one that would go).
        private void ShowSwapChoice(int index)
        {
            var held = _map?.Run?.State?.HeldItems;
            if (held == null || index < 0 || index >= held.Count || _swapCardName == null) return;
            _swapChosen = index;
            var item = held[index];
            _swapCardEyebrow.text = F("InRun.BagSwap.FromBag", index + 1, held.Count).ToUpperInvariant();
            _swapCardName.text = item != null ? ItemService.GetItemName(item.Type) : string.Empty;
            _swapCardText.text = item != null ? ItemService.GetItemDescription(item.Type, item.Rarity) : string.Empty;
            var sprite = item != null ? InRunUiFactory.LoadResourceSprite("items/icon_" + ItemService.GetIconName(item.Type)) : null;
            _swapCardIcon.sprite = sprite;
            _swapCardIcon.color = sprite != null ? Color.white : Color.clear;
            var rarity = item?.Rarity ?? ItemRarity.Normal;
            _swapCardRarity.color = ScrollUiKit.RarityColor(rarity);
            if (_swapCardRarityText != null)
            {
                _swapCardRarityText.text = RarityLabel(rarity).ToUpperInvariant();
                _swapCardRarityText.color = ScrollUiKit.RarityTextColor(rarity);
            }
        }
    }
}
