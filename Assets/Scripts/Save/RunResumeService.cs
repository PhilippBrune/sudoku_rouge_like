using SudokuRoguelike.Core;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.Save
{
    public sealed class RunResumeService
    {
        public bool TryResumeFromSave(RunDirector runDirector, SaveFileEnvelope envelope)
        {
            if (envelope?.ActiveRunState == null)
            {
                return false;
            }

            runDirector.StartRun(
                envelope.ActiveRunState.ClassId,
                envelope.ActiveRunState.Mode,
                envelope.ActiveRunState.Depth,
                meta: envelope.MetaProgress);

            var runState = runDirector.RunState;
            runState.CurrentHP = envelope.ActiveRunState.CurrentHP;
            runState.MaxHP = envelope.ActiveRunState.MaxHP;
            runState.CurrentPencil = envelope.ActiveRunState.CurrentPencil;
            runState.MaxPencil = envelope.ActiveRunState.MaxPencil;
            runState.CurrentGold = envelope.ActiveRunState.CurrentGold;
            runState.CurrentXP = envelope.ActiveRunState.CurrentXP;
            runState.CurrentNodeIndex = envelope.ActiveRunState.CurrentNodeIndex;
            runState.RerollTokens = envelope.ActiveRunState.RerollTokens;
            runState.ItemSlots = envelope.ActiveRunState.ItemSlots;
            runState.PencilPurchasesThisRun = envelope.ActiveRunState.PencilPurchasesThisRun;
            runState.RerollsThisRun = envelope.ActiveRunState.RerollsThisRun;

            for (var i = 0; i < envelope.ActiveRunState.Inventory.Count; i++)
            {
                runState.Inventory.Add(envelope.ActiveRunState.Inventory[i]);
            }

            // Restore single relic slot
            runState.HasRelic = envelope.ActiveRunState.HasRelic;
            runState.HeldRelic = envelope.ActiveRunState.HeldRelic;

            for (var i = 0; i < envelope.ActiveRunState.RouteHistory.Count; i++)
            {
                runState.RouteHistory.Add(envelope.ActiveRunState.RouteHistory[i]);
            }

            for (var i = 0; i < envelope.ActiveRunState.NodePath.Count; i++)
            {
                runState.NodePath.Add(envelope.ActiveRunState.NodePath[i]);
            }

            // Restore boss modifier selection
            runState.HasChosenBossModifier = envelope.ActiveRunState.HasChosenBossModifier;
            runState.ChosenBossModifierId = envelope.ActiveRunState.ChosenBossModifierId;

            // Restore seen modifiers from serialized list → runtime HashSet
            for (var i = 0; i < envelope.ActiveRunState.SeenBossModifierList.Count; i++)
                runState.SeenBossModifiers.Add(envelope.ActiveRunState.SeenBossModifierList[i]);

            runState.AllowIrregularPuzzles = envelope.ActiveRunState.AllowIrregularPuzzles;

            // Restore floor progression
            runState.CurrentFloor = envelope.ActiveRunState.CurrentFloor;
            runState.TotalFloors = envelope.ActiveRunState.TotalFloors;

            // Restore multi-modifier boss selections
            for (var i = 0; i < envelope.ActiveRunState.ChosenBossModifiers.Count; i++)
                runState.ChosenBossModifiers.Add(envelope.ActiveRunState.ChosenBossModifiers[i]);

            // Rebuild graph for restored floor (StartRun always builds floor 0)
            if (runState.CurrentFloor > 0)
                runDirector.RebuildCurrentFloorGraph();

            if (envelope.ActivePuzzle == null)
            {
                return true;
            }

            return runDirector.TryRestorePuzzleSaveState(envelope.ActivePuzzle);
        }
    }
}
