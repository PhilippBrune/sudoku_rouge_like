using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: ITEM-REFUND-001] GAP-117 (0.4.1 playtest on a Legion Go): Koi Reflection, Temple Incense, Wind Chime and the
    // Pattern Scroll are used up when picked, before their cells are chosen. Left before anything happened (Esc, the
    // Options, Save & Quit, a new puzzle), the item comes back; once a cell took effect the use stands.
    public sealed partial class InRunController
    {
        private ItemInstance _pendingItem;
        private int _pendingItemSlot, _pendingItemCharges, _pendingItemUses;

        private void RememberPendingItem(int slotIndex, ItemInstance item, int uses)
        {
            _pendingItem = item;
            _pendingItemSlot = slotIndex;
            _pendingItemCharges = item?.Charges ?? 0;
            _pendingItemUses = uses;
        }

        private void RefundUntouchedPendingItem()
        {
            var item = _pendingItem;
            _pendingItem = null;
            if (item == null) return;
            var untouched = (_templeIncensePending && _templeIncenseChargesLeft == _pendingItemUses)
                            || (_koiReflectionPending && _koiReflectionChargesLeft == _pendingItemUses)
                            || (_patternScrollPending && _patternScrollChargesLeft == _pendingItemUses)
                            || _windChimePending || _windChimeChoosingHouses;
            if (!untouched) return;
            _map?.Run?.RefundItem(_pendingItemSlot, item, _pendingItemCharges);
            SetStatus(LocalizationService.Format("Item.Status.Returned", "{0} is back in your bag.", Items.ItemService.GetItemName(item.Type)));
        }
    }
}
