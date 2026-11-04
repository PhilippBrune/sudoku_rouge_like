using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Run
{
    // RM-104 content (owner's picks 2026-11-04) that needs the run's seeded services: the reward's rarity (Empty Canvas,
    // Gold Leaf Brush), the Seed Pouch's harvest and the relics that events and the Red Thread give.
    public sealed partial class RunDirector
    {
        public const int SeedPouchPuzzles = 2;

        // [REQ: HARMONY-PERK-005] Empty Canvas: every item in a reward is Rare or better (GAP-069).
        // [REQ: RELIC-PACK-001] Gold Leaf Brush: after a combo of 10 every item of the next reward is one rarity higher; the
        // flag goes when that reward is taken or left (ResolveRewardRoll), so a reroll keeps it.
        private void ApplyRewardRarity()
        {
            if (RolledItemSlots == null || (!State.EmptyCanvasActive && !State.GoldLeafReady)) return;
            var classId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
            for (var i = 0; i < RolledItemSlots.Count; i++)
            {
                if (RolledItemSlots[i] == null) continue;
                var index = i;
                Func<ItemType, bool> taken = type =>
                    RolledItemSlots.Exists(other => other != null && other != RolledItemSlots[index] && other.Type == type);
                if (State.EmptyCanvasActive)
                    RolledItemSlots[i] = _itemService.AtLeastRare(RolledItemSlots[i], GetCurrentClassLevel(), classId, taken);
                if (State.GoldLeafReady)
                    RolledItemSlots[i] = _itemService.OneTierUp(RolledItemSlots[i], GetCurrentClassLevel(), classId, taken);
            }
        }

        // A solved puzzle: the class challenges count it, and every planted Seed Pouch grows one step.
        private void OnPuzzleSolvedContent(LevelState level, LevelConfig config)
        {
            Meta.ClassChallenges.OnPuzzleSolved(State, level, config);   // [REQ: CLASS-CHALLENGE-001]
            HarvestSeedPouches();
        }

        public const int SeedPouchesAtOnce = 3;

        /// <summary>[REQ: ITEM-PACK-001] Plants a Seed Pouch: a Rare item after the next two solved puzzles. At most three grow
        /// at once (an item that is never used up could plant one after another); false when the garden is full.</summary>
        public bool PlantSeedPouch()
        {
            State.SeedPouchTimers ??= new List<int>();
            if (State.SeedPouchTimers.Count >= SeedPouchesAtOnce) return false;
            State.SeedPouchTimers.Add(SeedPouchPuzzles);
            return true;
        }

        // A ripe pouch waits in the ground while the bag is full.
        private void HarvestSeedPouches()
        {
            LastHarvest = null;   // only this puzzle's harvest is named
            var timers = State.SeedPouchTimers;
            if (timers == null) return;
            for (var i = timers.Count - 1; i >= 0; i--)
            {
                if (timers[i] > 0) timers[i]--;
                if (timers[i] > 0 || IsBagFull()) continue;
                _itemService.ReseedForDecision(DecisionSeed(DecisionPurpose.ItemReward, 0x5EED + i));
                var classId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
                var item = _itemService.AtLeastRare(_itemService.RollOneItem(2, GetCurrentClassLevel(), classId),
                    GetCurrentClassLevel(), classId);
                AddItemToInventory(item);
                LastHarvest = item;
                timers.RemoveAt(i);
            }
        }

        /// <summary>The item the last Seed Pouch gave (for the puzzle's status line); null when none ripened.</summary>
        public ItemInstance LastHarvest { get; set; }

        /// <summary>[REQ: ITEM-REFUND-001] GAP-117: an item that waited for cells and was left before anything happened comes
        /// back as it was: its charge, or into its bag slot when the use had emptied it; the use is not counted.</summary>
        public void RefundItem(int slotIndex, ItemInstance item, int charges)
        {
            if (item == null) return;
            item.Charges = charges;
            if (!State.HeldItems.Contains(item))
                State.HeldItems.Insert(Math.Min(Math.Max(0, slotIndex), State.HeldItems.Count), item);
            State.ItemsUsedThisRun = Math.Max(0, State.ItemsUsedThisRun - 1);
            if (CurrentLevelState != null)
                CurrentLevelState.ItemsUsedThisLevel = Math.Max(0, CurrentLevelState.ItemsUsedThisLevel - 1);
            var types = State.ItemTypesUsedThisRun;
            if (types != null && types.Count > 0) types.RemoveAt(types.Count - 1);
        }

        /// <summary>[REQ: ITEM-TRADE-001] [REQ: EVENT-PACK-001] A random relic of the floor's pool, taken at once (the Red
        /// Thread, the Fallen Lantern, the Bone Carver).</summary>
        public RelicInstance TakeRandomRelic()
        {
            // Salted with the relics held, so a second Red Thread in the same puzzle rolls again (stable across a resume).
            _relicService.ReseedForDecision(DecisionSeed(DecisionPurpose.RelicReward, 1 + (State.HeldRelics?.Count ?? 0)));
            var relic = _relicService.RollRelic(State.CurrentFloor, RelicService.GetRelicNodeTierBonus(State));
            if (relic != null) AcceptRelic(relic);
            return relic;
        }
    }
}
