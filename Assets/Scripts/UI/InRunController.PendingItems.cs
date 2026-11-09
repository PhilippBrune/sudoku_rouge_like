using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: ITEM-REFUND-001] GAP-117 (0.4.1 playtest on a Legion Go): Koi Reflection, Temple Incense, Moth Light and the
    // Pattern Scroll are used up when picked, before their cells are chosen. Left before anything happened (Esc, the
    // Options, Save & Quit, a new puzzle), the item comes back; once a cell took effect the use stands.
    public sealed partial class InRunController
    {
        private ItemInstance _pendingItem;
        private int _pendingItemSlot, _pendingItemCharges, _pendingItemUses;
        private bool _mothLightPending;

        private void RememberPendingItem(int slotIndex, ItemInstance item, int uses)
        {
            _pendingItem = item;
            _pendingItemSlot = slotIndex;
            _pendingItemCharges = item?.Charges ?? 0;
            _pendingItemUses = uses;
        }

        // [REQ: ITEM-REWORK-002] A box without fog keeps the choice open: the Moth Light is spent only where it lifted something.
        private void HandleMothLightClick(int r, int c)
        {
            var run = _map?.Run;
            if (run?.CurrentBoard == null) { _mothLightPending = false; return; }
            var lifted = run.LiftFogFromBox(r, c);
            if (lifted == 0) { SetStatus(T("Item.Status.MothLight.NoFogHere")); return; }
            _mothLightPending = false;
            _map?.SaveNow();
            SetStatus(Tf("Item.Status.MothLight.Done", lifted));
            RenderBoard();
            CheckGameOver();   // a wrong digit waiting under the fog is judged now
        }

        private void RefundUntouchedPendingItem()
        {
            var item = _pendingItem;
            _pendingItem = null;
            if (item == null) return;
            var untouched = (_templeIncensePending && _templeIncenseChargesLeft == _pendingItemUses)
                            || (_koiReflectionPending && _koiReflectionChargesLeft == _pendingItemUses)
                            || (_patternScrollPending && _patternScrollChargesLeft == _pendingItemUses)
                            || _mothLightPending;
            if (!untouched) return;
            _map?.Run?.RefundItem(_pendingItemSlot, item, _pendingItemCharges);
            SetStatus(LocalizationService.Format("Item.Status.Returned", "{0} is back in your bag.", Items.ItemService.GetItemName(item.Type)));
        }
    }
}
