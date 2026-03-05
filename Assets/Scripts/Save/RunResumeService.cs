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
            runState.Level = envelope.ActiveRunState.Level;
            runState.CurrentNodeIndex = envelope.ActiveRunState.CurrentNodeIndex;
            runState.RerollTokens = envelope.ActiveRunState.RerollTokens;
            runState.ItemSlots = envelope.ActiveRunState.ItemSlots;
            runState.PencilPurchasesThisRun = envelope.ActiveRunState.PencilPurchasesThisRun;
            runState.RerollsThisRun = envelope.ActiveRunState.RerollsThisRun;
            runState.CurrentHeatScore = envelope.ActiveRunState.CurrentHeatScore;
            runState.PeakHeatScore = envelope.ActiveRunState.PeakHeatScore;

            for (var i = 0; i < envelope.ActiveRunState.Inventory.Count; i++)
            {
                runState.Inventory.Add(envelope.ActiveRunState.Inventory[i]);
            }

            for (var i = 0; i < envelope.ActiveRunState.RelicIds.Count; i++)
            {
                runState.RelicIds.Add(envelope.ActiveRunState.RelicIds[i]);
            }

            for (var i = 0; i < envelope.ActiveRunState.RouteHistory.Count; i++)
            {
                runState.RouteHistory.Add(envelope.ActiveRunState.RouteHistory[i]);
            }

            for (var i = 0; i < envelope.ActiveRunState.NodePath.Count; i++)
            {
                runState.NodePath.Add(envelope.ActiveRunState.NodePath[i]);
            }

            if (envelope.ActivePuzzle == null)
            {
                return true;
            }

            return runDirector.TryRestorePuzzleSaveState(envelope.ActivePuzzle);
        }
    }
}
