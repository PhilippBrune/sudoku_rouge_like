using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    // Run economy: shop, rest node, pencil purchase, reward reroll and the Nothing reward.
    // Split out of RunDirector.cs to keep it within its size budget (docs/controller-extraction-plan.md).
    public sealed partial class RunDirector
    {
        // ── Shop ──

        public List<ShopOffer> BuildShopOffers()
        {
            var priceMultiplier = RelicService.GetShopPriceMultiplier(State)
                                * CurseService.GetShopPriceMultiplier(State)
                                * (State.WornChiselActive ? 0.70f : 1.0f);
            var classLevel = GetCurrentClassLevel();
            var shopClassId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
            _shopService.ReseedForDecision(DecisionSeed(DecisionPurpose.ShopOffers, State.ShopRerollCount));
            // [REQ: ITEM-COUNTER-007] The shop leans toward items that counter the floor's rules and the chosen boss trials.
            var rules = new List<BossModifierId>();
            if (State.ActiveFloorModifiers != null) rules.AddRange(State.ActiveFloorModifiers);
            if (State.ChosenBossModifiers != null) rules.AddRange(State.ChosenBossModifiers);
            CurrentShopOffers = _shopService.BuildOffers(State.CurrentFloor, classLevel, priceMultiplier, State.HarmonyConfig.ShopSurcharge,
                shopClassId, Items.CounterItems.ForRules(rules));
            return CurrentShopOffers;
        }

        public int GetShopRerollCost() => 15 + State.ShopRerollCount * 5;

        public bool RerollShop()
        {
            // Consume a reroll token first (free); fall back to gold if none remain.
            if (State.RerollTokens > 0)
            {
                State.RerollTokens--;
                State.ShopRerollCount++;
                BuildShopOffers();
                return true;
            }

            var cost = GetShopRerollCost();
            if (State.CurrentGold < cost) return false;
            State.CurrentGold -= cost;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldSpentThisRun += cost;
                if (State.GoldSpentThisRun >= 50)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
            }
            State.ShopRerollCount++;
            BuildShopOffers();
            return true;
        }

        // [REQ: REQ-ITEM-101] A full bag needs a slot to replace; without one the purchase is refused before any gold
        // is taken (GAP-036: the gold used to be charged for an item that was then dropped).
        public bool TryPurchaseShopOffer(int offerIndex, int replaceSlot = -1)
        {
            if (CurrentShopOffers == null || offerIndex < 0 || offerIndex >= CurrentShopOffers.Count) return false;

            var offer = CurrentShopOffers[offerIndex];
            if (offer.IsSold || State.CurrentGold < offer.Price) return false;
            var needsSlot = offer.Item != null && IsBagFull();
            if (needsSlot && (replaceSlot < 0 || replaceSlot >= State.HeldItems.Count)) return false;

            State.CurrentGold -= offer.Price;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldSpentThisRun += offer.Price;
                if (State.GoldSpentThisRun >= 50)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
            }
            offer.IsSold = true;
            if (needsSlot)
                ReplaceItemInInventory(replaceSlot, offer.Item);
            else
                AddItemToInventory(offer.Item);
            return true;
        }

        public bool TryBuyEmergencyHeal()
        {
            var price = ShopService.EmergencyHealPrice(State.PencilPurchaseCount);
            if (State.CurrentGold < price) return false;

            State.CurrentGold -= price;
            if (DailyGoals != null && !State.IsSeasonalChallenge)
            {
                State.GoldSpentThisRun += price;
                if (State.GoldSpentThisRun >= 50)
                    DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
            }
            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + 1);
            State.PencilPurchaseCount++;
            return true;
        }

        // ── Rest Node Options ──

        // [REQ: REQ-RUN-104] Rest heals MaxHP/4 (ECON-BAL-004; MaxHP/3 before, SPEC-CONF-017). [REQ: HARMONY-FLAG-004] H10
        // halves it, rounded up.
        public int GetRestHealAmount()
        {
            // [REQ: ECON-BAL-004] A quarter of max HP (it was a third; gold-and-items concept L6), at least 1.
            var heal = Math.Max(1, State.MaxHP / 4);
            return Harmony.RestHealHalved ? (heal + 1) / 2 : heal;
        }

        public void AcceptRestHeal()
        {
            State.CurrentHP = Math.Min(State.MaxHP, State.CurrentHP + GetRestHealAmount());
        }

        /// <summary>Pencil marks the rest's "fresh ink" gives.</summary>
        public const int RestPencilBoost = 4;

        public void AcceptRestPencilBoost()
        {
            State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + RestPencilBoost);
        }

        /// <summary>Removes the oldest active curse at the rest node. Returns false if no curses are active.</summary>
        // [REQ: CURSE-INT-010] [REQ: CURSE-REMOVE-001] Rest node cleanse: removes oldest active curse
        public bool AcceptRestCurseRemoval() => CurseService.TryRemoveCurse(State);

        public void AcceptRestRerollShop()
        {
            State.RerollTokens++;
        }

        // ── Pencil purchase, reward reroll, Nothing reward ──

        // [REQ: REQ-ECON-100] Buy pencil during a puzzle (GDS §5.2/§5.3, GAP-016): +3 Pencil (up to the maximum) for
        // 20 + 20 × pencil purchases this run. Garden Monk cannot buy pencil mid-level (GDS §6).
        public const int PencilPerPurchase = 3;

        public int GetPencilBuyCost() => GoldTable.PencilBuyCost(State.PencilBoughtCount);

        public bool CanBuyPencil()
        {
            if (State == null || CurrentBoard == null || CurrentLevelState == null || IsLevelComplete) return false;
            if (State.TutorialMode || State.IsSeasonalChallenge || State.DisableProgressionRewards) return false;
            if (!ClassPassiveService.CanBuyPencilMidLevel(State)) return false;
            return State.CurrentPencil < State.MaxPencil && State.CurrentGold >= GetPencilBuyCost();
        }

        public bool TryBuyPencil()
        {
            if (!CanBuyPencil()) return false;
            var cost = GetPencilBuyCost();
            State.CurrentGold -= cost;
            State.GoldSpentThisRun += cost;
            State.PencilBoughtCount++;
            State.CurrentPencil = Math.Min(State.MaxPencil, State.CurrentPencil + PencilPerPurchase);
            return true;
        }

        // [REQ: REQ-ITEM-100] Reroll the item reward (GDS §8, GAP-015): a reroll token first, otherwise
        // 20 + 20 × reward rerolls this run in gold. Only slots holding an item are rerolled; Nothing slots stay locked.
        public int GetRewardRerollCost() => GoldTable.RerollCost(State.RewardRerollCount);

        public bool CanRerollReward() =>
            State != null && RolledItemSlots != null && RolledItemSlots.Exists(slot => slot != null)
            && (State.RerollTokens > 0 || State.CurrentGold >= GetRewardRerollCost());

        public List<ItemInstance> RerollRewardSlots()
        {
            if (!CanRerollReward()) return null;
            if (State.RerollTokens > 0)
                State.RerollTokens--;
            else
            {
                var cost = GetRewardRerollCost();
                State.CurrentGold -= cost;
                State.GoldSpentThisRun += cost;
            }
            State.RewardRerollCount++;

            var stars = CurrentLevelConfig?.Stars ?? 1;
            var classId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
            _itemService.ReseedForDecision(DecisionSeed(DecisionPurpose.ItemReward, State.RewardRerollCount));
            for (var i = 0; i < RolledItemSlots.Count; i++)
                if (RolledItemSlots[i] != null)
                    RolledItemSlots[i] = _itemService.RollOneItem(stars, GetCurrentClassLevel(), classId);
            return RolledItemSlots;
        }

        // [REQ: REQ-ITEM-100] Choosing Nothing on the reward screen grants a small gold bonus: 5 gold × floor number
        // (GDS §8; amount defined 2026-10-06, SPEC-AMB-007).
        public int GetNothingRewardGold() => 5 * (State.CurrentFloor + 1);

        public int TakeNothingReward()
        {
            RolledItemSlots = null;
            if (State.IsSeasonalChallenge || State.DisableProgressionRewards) return 0;
            var gold = GetNothingRewardGold();
            State.GainGold(gold);
            return gold;
        }
    }
}
