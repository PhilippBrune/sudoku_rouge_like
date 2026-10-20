using SudokuRoguelike.Core;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.UI
{
    // [REQ: ITEM-COUNTER-001..006] Counter items in the bag (gold-and-items concept L3): they write correct digits into
    // the cells of their family's rules (RunDirector.CounterItems.cs). An item that finds nothing is not used up.
    public sealed partial class InRunController
    {
        private void UseCounterItem(int slotIndex, ItemInstance item)
        {
            var run = _map?.Run;
            if (run?.CurrentBoard == null) { SetStatus(T("No active board.")); return; }
            var name = ItemService.GetItemName(item.Type);
            var effect = run.ApplyItemEffect(item, _selectedRow, _selectedCol);
            if (effect.Kind != ItemEffectResult.ResultKind.CellsFilled)
            {
                SetStatus(LocalizationService.Format("Item.Status.CounterNothing", "{0} finds nothing to fill on this board.", name));
                return;
            }
            run.TryUseItem(slotIndex);
            SetStatus(LocalizationService.Format("Item.Status.CounterFilled", "{0}: filled {1} cell(s).", name, effect.Cells.Count));
            RenderBoard();
        }
    }
}
