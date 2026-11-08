using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Run
{
    // [REQ: EVENT-PACK-001] [REQ: EVENT-TRADE-001] RM-104's events (owner's picks 2026-11-04): a wager, a fallen lantern, a
    // wandering monk, and four trades of HP, Max HP or pencil marks for something strong. A relic from an event is rolled
    // and taken by RunDirector.ResolveCurrentEventChoice (the relic pool is the run's).
    public sealed partial class RunEventService
    {
        private static readonly (string id, int optionCount)[] PackLocalizationSpecs =
        {
            ("koi_pond_wager", 3), ("fallen_lantern", 3), ("wandering_monk", 3),
            ("bone_carver", 3), ("ink_spring", 3), ("blood_lantern", 3), ("ghost_merchant", 3), (RunDirector.ChallengeEventKey, 3)
        };

        private static readonly string[] PackResultKeys =
        {
            "RunEvent.Result.MaxHpCost", "RunEvent.Result.MaxPencilGain", "RunEvent.Result.RelicGain",
            "RunEvent.Result.CurseRemoved", "RunEvent.Result.WagerWon", "RunEvent.Result.WagerLost", "RunEvent.Result.ItemUpgraded"
        };

        private static EventOption Walk(string id) => new EventOption
        {
            Label = T(EventOptionLabelKey(id, 2), "Walk on"), EffectDescription = T(EventOptionEffectKey(id, 2), "Nothing happens")
        };

        private static EventOption Option(string id, int index, string label, string effect, EventOption template)
        {
            template.HpCostBlocks = template.HpCost > 0;   // a trade is paid in full
            template.Label = T(EventOptionLabelKey(id, index), label);
            template.EffectDescription = T(EventOptionEffectKey(id, index), effect);
            return template;
        }

        private static RunEvent Event(string id, string title, string description, int minFloor, EventOption first, EventOption second) =>
            new RunEvent
            {
                Key = id, MinFloor = minFloor,
                Title = T(EventTitleKey(id), title),
                Description = T(EventDescriptionKey(id), description),
                Options = new List<EventOption> { first, second, Walk(id) }
            };

        private static IEnumerable<RunEvent> BuildPackEvents()
        {
            yield return Event(RunDirector.ChallengeEventKey, "The Hourglass Master",
                "A master sets a board on a flat stone and turns an hourglass. Solve it before the sand runs out, and a prize is yours.", 0,
                Option(RunDirector.ChallengeEventKey, 0, "Play for an item", string.Empty, new EventOption { Challenge = RunDirector.ChallengeItem }),
                Option(RunDirector.ChallengeEventKey, 1, "Play for a relic", string.Empty, new EventOption { Challenge = RunDirector.ChallengeRelic }));
            yield return Event("koi_pond_wager", "The Koi Pond Wager",
                "An old man feeds the koi and offers a bet: which fish rises first?", 0,
                Option("koi_pond_wager", 0, "Bet 20 gold", "Half and half: +50 gold or nothing", new EventOption { GoldCost = 20, WagerGold = 50 }),
                Option("koi_pond_wager", 1, "Feed the koi (10g)", "Pay 10 gold, +1 HP", new EventOption { GoldCost = 10, HpGain = 1 }));
            yield return Event("fallen_lantern", "The Fallen Lantern",
                "A stone lantern lies on its side across the path. Its oil is still warm.", 1,
                Option("fallen_lantern", 0, "Set it upright", "-1 HP, a random relic", new EventOption { HpCost = 1, GrantRandomRelic = true }),
                Option("fallen_lantern", 1, "Take the oil", "+10 pencil marks, +Inkblot curse", new EventOption { PencilGain = 10, CurseId = "inkblot" }));
            yield return Event("wandering_monk", "The Wandering Monk",
                "A monk with an empty bowl sits by the path. He offers to trade.", 0,
                Option("wandering_monk", 0, "Trade an item", "Your weakest item becomes a random one a rarity higher", new EventOption { UpgradeItem = true }),
                Option("wandering_monk", 1, "Meditate with him", "+2 HP, -5 pencil marks", new EventOption { HpGain = 2, PencilCost = 5 }));
            yield return Event("bone_carver", "The Bone Carver",
                "A carver works in bone by lantern light. He takes his pay in strength.", 1,
                Option("bone_carver", 0, "A relic for strength", "-2 Max HP, a random relic", new EventOption { MaxHpCost = 2, GrantRandomRelic = true }),
                Option("bone_carver", 1, "A charm for strength", "-1 Max HP, a random Rare item", new EventOption { MaxHpCost = 1, GrantRareItem = true }));
            yield return Event("ink_spring", "The Ink Spring",
                "Black water wells up between the stones, thick as ink.", 0,
                Option("ink_spring", 0, "Drink from it", "-2 HP, +10 pencil marks and +2 max pencil", new EventOption { HpCost = 2, PencilGain = 10, MaxPencilGain = 2 }),
                Option("ink_spring", 1, "Pour your ink in", "-8 pencil marks, +1 Max HP", new EventOption { PencilCost = 8, MaxHpGain = 1 }));
            var bloodLantern = Event("blood_lantern", "The Blood Lantern",
                "A red lantern burns without oil. It asks for blood to burn away a curse.", 0,
                Option("blood_lantern", 0, "Feed it 3 HP", "-3 HP, every curse is lifted", new EventOption { HpCost = 3, RemoveCurses = -1 }),
                Option("blood_lantern", 1, "Feed it 1 HP", "-1 HP, one curse is lifted", new EventOption { HpCost = 1, RemoveCurses = 1 }));
            bloodLantern.NeedsCurse = true;
            yield return bloodLantern;
            yield return Event("ghost_merchant", "The Ghost Merchant",
                "A merchant without a shadow pays gold for what lives in you.", 0,
                Option("ghost_merchant", 0, "Sell 2 HP", "-2 HP, +60 gold", new EventOption { HpCost = 2, GoldGain = 60 }),
                Option("ghost_merchant", 1, "Sell 6 pencil marks", "-6 pencil marks, +40 gold", new EventOption { PencilCost = 6, GoldGain = 40 }));
        }

        /// <summary>Null when the option can be paid for; otherwise the catalog key of the reason (the event screen shows it).</summary>
        public static string PackBlockKey(EventOption option, RunState state)
        {
            if (option.MaxHpCost > 0 && state.MaxHP <= option.MaxHpCost) return "InRun.Event.NeedMaxHp";
            if (option.RemoveCurses != 0 && (state.ActiveCurseIds == null || state.ActiveCurseIds.Count == 0)) return "InRun.Event.NoCurse";
            if (option.UpgradeItem && (state.HeldItems == null || state.HeldItems.Count == 0)) return "InRun.Event.NoItem";
            if (option.UpgradeItem && !state.HeldItems.Exists(i => i != null && i.Rarity != ItemRarity.Epic)) return "InRun.Event.NothingToRaise";
            if (option.GrantRareItem && state.HeldItems.Count >= state.ItemSlots) return "InRun.Event.BagFull";
            if (option.HpCostBlocks && state.CurrentHP <= option.HpCost) return "InRun.Event.NeedHp";
            return null;
        }

        // The RM-104 costs and gains, after the classic ones in ResolveChoice.
        private void ResolvePackOption(RunState state, EventOption option, List<string> parts)
        {
            _itemService.ReseedForDecision(_random.Next());   // items follow the choice's seed, like the wager
            if (option.MaxHpCost > 0)
            {
                state.MaxHP = Math.Max(1, state.MaxHP - option.MaxHpCost);
                state.CurrentHP = Math.Min(state.CurrentHP, state.MaxHP);
                parts.Add(LocalizationService.Format("RunEvent.Result.MaxHpCost", "-{0} Max HP", option.MaxHpCost));
            }
            if (option.MaxPencilGain > 0)
            {
                state.MaxPencil += option.MaxPencilGain;
                state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + option.MaxPencilGain);
                parts.Add(LocalizationService.Format("RunEvent.Result.MaxPencilGain", "+{0} max pencil", option.MaxPencilGain));
            }
            if (option.WagerGold > 0)
            {
                var won = _random.NextDouble() < 0.5;
                if (won) state.GainGold(option.WagerGold);
                parts.Add(won ? LocalizationService.Format("RunEvent.Result.WagerWon", "your fish rose first: +{0} gold", option.WagerGold)
                              : LocalizationService.T("RunEvent.Result.WagerLost", "another fish rose first"));
            }
            if (option.RemoveCurses != 0)
            {
                var removed = 0;
                while ((option.RemoveCurses < 0 || removed < option.RemoveCurses) && CurseService.TryRemoveCurse(state)) removed++;
                if (removed > 0) parts.Add(LocalizationService.Format("RunEvent.Result.CurseRemoved", "{0} curse(s) lifted", removed));
            }
            if (option.GrantRareItem && state.HeldItems.Count < state.ItemSlots)
                AddToBag(state, _itemService.AtLeastRare(_itemService.RollOneItem(2, 1), 1), parts);
            if (option.UpgradeItem && state.HeldItems.Count > 0)
            {
                var weakest = 0;
                for (var i = 1; i < state.HeldItems.Count; i++)
                    if (state.HeldItems[i] != null && (state.HeldItems[weakest] == null || state.HeldItems[i].Rarity < state.HeldItems[weakest].Rarity))
                        weakest = i;
                var before = state.HeldItems[weakest];
                var raised = _itemService.OneTierUp(before, 1);
                var after = raised == before ? before : RelicService.OnItemReceived(state, raised);   // Twin Koi only for a new item
                state.HeldItems[weakest] = after;
                if (before != null && after != null)
                    parts.Add(LocalizationService.Format("RunEvent.Result.ItemUpgraded", "{0} became {1}",
                        ItemService.GetItemName(before.Type), ItemService.GetItemName(after.Type)));
            }
        }

        private static void AddToBag(RunState state, ItemInstance item, List<string> parts)
        {
            if (item == null) return;
            state.HeldItems.Add(RelicService.OnItemReceived(state, item));   // Twin Koi
            parts.Add(LocalizationService.Format("RunEvent.Result.ItemGain", "+{0}", ItemService.GetItemName(item.Type)));
        }
    }
}
