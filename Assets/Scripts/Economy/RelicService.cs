using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Economy
{
    public sealed class RelicService
    {
        public bool TryAcquireRunRelic(RunState runState, string relicId, int price)
        {
            if (runState.CurrentGold < price || runState.RelicIds.Contains(relicId))
            {
                return false;
            }

            runState.CurrentGold -= price;
            runState.RelicIds.Add(relicId);
            return true;
        }

        public void ApplyRunRelicEffects(RunState runState)
        {
            if (runState.RelicIds.Count == 0) return;
            ApplySingleRelicEffect(runState, runState.RelicIds[runState.RelicIds.Count - 1]);
        }

        public void ApplySingleRelicEffect(RunState runState, string relic)
        {
            if (relic.Contains("hp"))
            {
                runState.MaxHP += 1;
                runState.CurrentHP += 1;
            }
            else if (relic.Contains("gold"))
            {
                runState.CurrentGold += 5;
            }
            else if (relic.Contains("pencil"))
            {
                runState.CurrentPencil += 2;
                runState.MaxPencil += 2;
            }
            else if (relic.Contains("sur"))
            {
                runState.MistakeShieldCharges += 1;
            }
            else if (relic.Contains("util"))
            {
                runState.MaxHP += 1;
                runState.CurrentGold += 3;
            }
            else if (relic.Contains("chaos"))
            {
                runState.CurrentGold += 8;
            }
        }

        public bool TryPurchasePermanentUpgrade(MetaProgressionState meta, string upgradeId, int essenceCost)
        {
            if (meta.GardenEssence < essenceCost || meta.PurchasedPermanentUpgrades.Contains(upgradeId))
            {
                return false;
            }

            meta.GardenEssence -= essenceCost;
            meta.PurchasedPermanentUpgrades.Add(upgradeId);
            return true;
        }
    }
}
