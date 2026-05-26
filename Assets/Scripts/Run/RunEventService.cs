using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Items;

namespace SudokuRoguelike.Run
{
    public sealed class RunEventService
    {
        private readonly Random _random;
        private readonly ItemService _itemService;

        private static readonly (string id, int optionCount)[] EventLocalizationSpecs =
        {
            ("quiet_bench", 2),
            ("ice_cream_cart", 2),
            ("spilled_ink_bottle", 2),
            ("lost_wallet", 3),
            ("wishing_fountain", 2),
            ("friendly_stray", 2),
            ("street_musician", 3),
            ("maintenance_shed", 2),
            ("ink_peddler", 2),
            ("rare_flower_patch", 3),
            ("tempting_shrine", 2)
        };

        public RunEventService(int seed)
        {
            _random = new Random(seed);
            _itemService = new ItemService(seed ^ 0xE4E5);
        }

        public RunEvent BuildEvent(int floorIndex, int depth)
        {
            var events = GetEventPool(floorIndex);
            var picked = events[_random.Next(events.Count)];
            picked.Id = $"evt_{floorIndex}_{depth}_{_random.Next(10000)}";
            return picked;
        }

        public static IEnumerable<string> GetLocalizationKeys()
        {
            yield return "RunEvent.Result.GoldCost";
            yield return "RunEvent.Result.HpCost";
            yield return "RunEvent.Result.PencilCost";
            yield return "RunEvent.Result.GoldGain";
            yield return "RunEvent.Result.HpGain";
            yield return "RunEvent.Result.PencilGain";
            yield return "RunEvent.Result.MaxHpGain";
            yield return "RunEvent.Result.ItemGain";
            yield return "RunEvent.Result.CurseGain";
            yield return "RunEvent.Result.NothingHappened";
            yield return "RunEvent.Result.Done";
            yield return "RunEvent.Result.Separator";
            yield return "RunEvent.OptionLabelWithEffect";

            for (var i = 0; i < EventLocalizationSpecs.Length; i++)
            {
                var spec = EventLocalizationSpecs[i];
                yield return EventTitleKey(spec.id);
                yield return EventDescriptionKey(spec.id);
                for (var optionIndex = 0; optionIndex < spec.optionCount; optionIndex++)
                {
                    yield return EventOptionLabelKey(spec.id, optionIndex);
                    yield return EventOptionEffectKey(spec.id, optionIndex);
                }
            }
        }

        /// <summary>
        /// Applies costs AND rewards for the chosen option, including random item grants.
        /// Returns a description string for the UI to display.
        /// </summary>
        public string ResolveChoice(RunState state, RunEvent runEvent, int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= runEvent.Options.Count) return string.Empty;

            var option = runEvent.Options[optionIndex];
            var parts = new List<string>();

            // ── Costs ──
            if (option.GoldCost > 0)
            {
                state.CurrentGold = Math.Max(0, state.CurrentGold - option.GoldCost);
                parts.Add(LocalizationService.Format("RunEvent.Result.GoldCost", "-{0} gold", option.GoldCost));
            }
            if (option.HpCost > 0)
            {
                state.CurrentHP = Math.Max(1, state.CurrentHP - option.HpCost);
                parts.Add(LocalizationService.Format("RunEvent.Result.HpCost", "-{0} HP", option.HpCost));
            }
            if (option.PencilCost > 0)
            {
                state.CurrentPencil = Math.Max(0, state.CurrentPencil - option.PencilCost);
                parts.Add(LocalizationService.Format("RunEvent.Result.PencilCost", "-{0} pencil", option.PencilCost));
            }

            // ── Rewards ──
            if (option.GoldGain > 0)
            {
                state.CurrentGold += option.GoldGain;
                parts.Add(LocalizationService.Format("RunEvent.Result.GoldGain", "+{0} gold", option.GoldGain));
            }
            if (option.HpGain > 0)
            {
                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + option.HpGain);
                parts.Add(LocalizationService.Format("RunEvent.Result.HpGain", "+{0} HP", option.HpGain));
            }
            if (option.PencilGain > 0)
            {
                state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + option.PencilGain);
                parts.Add(LocalizationService.Format("RunEvent.Result.PencilGain", "+{0} pencil", option.PencilGain));
            }
            if (option.MaxHpGain > 0)
            {
                state.MaxHP += option.MaxHpGain;
                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + option.MaxHpGain);
                parts.Add(LocalizationService.Format("RunEvent.Result.MaxHpGain", "+{0} Max HP", option.MaxHpGain));
            }
            if (option.GrantRandomItem)
            {
                var slots = _itemService.RollSlots(2, 1, 0);
                ItemInstance granted = null;
                for (var i = 0; i < slots.Count; i++) { if (slots[i] != null) { granted = slots[i]; break; } }
                if (granted != null && state.HeldItems.Count < state.ItemSlots)
                {
                    state.HeldItems.Add(granted);
                    parts.Add(LocalizationService.Format("RunEvent.Result.ItemGain", "+{0}", ItemService.GetItemName(granted.Type)));
                }
            }

            if (!string.IsNullOrEmpty(option.CurseId))
            {
                // [REQ: CURSE-ACQUIRE-001] Event choices with a CurseId apply a curse as their cost/trade (Ink Peddler, Tempting Shrine)
                if (CurseService.ApplyCurse(state, option.CurseId))
                {
                    var def = CurseService.GetDefinition(option.CurseId);
                    parts.Add(LocalizationService.Format(
                        "RunEvent.Result.CurseGain",
                        "+{0} (curse)",
                        def != null ? def.Name : option.CurseId));
                }
            }

            return parts.Count > 0
                ? string.Join(LocalizationService.T("RunEvent.Result.Separator", ", "), parts)
                : LocalizationService.T("RunEvent.Result.NothingHappened", "Nothing happened.");
        }

        // ── Event Pool ──────────────────────────────────────────────────────────

        private List<RunEvent> GetEventPool(int floorIndex)
        {
            var all = BuildFullEventPool();
            var filtered = new List<RunEvent>();
            foreach (var e in all)
                if (e.MinFloor <= floorIndex) filtered.Add(e);
            return filtered.Count > 0 ? filtered : all; // fallback: full pool if nothing qualifies
        }

        private static string EventTitleKey(string eventId) => $"RunEvent.{eventId}.Title";
        private static string EventDescriptionKey(string eventId) => $"RunEvent.{eventId}.Description";
        private static string EventOptionLabelKey(string eventId, int optionIndex) => $"RunEvent.{eventId}.Option{optionIndex}.Label";
        private static string EventOptionEffectKey(string eventId, int optionIndex) => $"RunEvent.{eventId}.Option{optionIndex}.Effect";
        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private List<RunEvent> BuildFullEventPool()
        {
            return new List<RunEvent>
            {
                // ── Park bench (rest) ──
                new RunEvent
                {
                    Title = T(EventTitleKey("quiet_bench"), "A Quiet Bench"),
                    Description = T(EventDescriptionKey("quiet_bench"), "A weathered wooden bench sits in a sunny patch. You could sit for a moment."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("quiet_bench", 0), "Rest a while"), EffectDescription = T(EventOptionEffectKey("quiet_bench", 0), "+2 HP"), HpGain = 2 },
                        new EventOption { Label = T(EventOptionLabelKey("quiet_bench", 1), "Keep walking"), EffectDescription = T(EventOptionEffectKey("quiet_bench", 1), "Nothing happens") }
                    }
                },

                // ── Street vendor / ice cream stand ──
                new RunEvent
                {
                    Title = T(EventTitleKey("ice_cream_cart"), "Ice Cream Cart"),
                    Description = T(EventDescriptionKey("ice_cream_cart"), "A colourful cart hums with a small freezer. The vendor waves you over."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("ice_cream_cart", 0), "Buy a scoop (20g)"), EffectDescription = T(EventOptionEffectKey("ice_cream_cart", 0), "Pay 20 gold, gain 3 HP"), GoldCost = 20, HpGain = 3 },
                        new EventOption { Label = T(EventOptionLabelKey("ice_cream_cart", 1), "Just browse"), EffectDescription = T(EventOptionEffectKey("ice_cream_cart", 1), "Nothing happens") }
                    }
                },

                // ── Fallen ink pot → pencil ──
                new RunEvent
                {
                    Title = T(EventTitleKey("spilled_ink_bottle"), "Spilled Ink Bottle"),
                    Description = T(EventDescriptionKey("spilled_ink_bottle"), "An artist's ink bottle has tipped over near a park path. Still mostly full."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("spilled_ink_bottle", 0), "Gather ink (15g)"), EffectDescription = T(EventOptionEffectKey("spilled_ink_bottle", 0), "+5 pencil marks, pay 15 gold"), GoldCost = 15, PencilGain = 5 },
                        new EventOption { Label = T(EventOptionLabelKey("spilled_ink_bottle", 1), "Leave it"), EffectDescription = T(EventOptionEffectKey("spilled_ink_bottle", 1), "Nothing happens") }
                    }
                },

                // ── Lost wallet ──
                new RunEvent
                {
                    Title = T(EventTitleKey("lost_wallet"), "Lost Wallet"),
                    Description = T(EventDescriptionKey("lost_wallet"), "A worn leather wallet lies on the path. Someone's name is inside."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("lost_wallet", 0), "Return it honestly"), EffectDescription = T(EventOptionEffectKey("lost_wallet", 0), "+1 Max HP (gratitude)"), MaxHpGain = 1 },
                        new EventOption { Label = T(EventOptionLabelKey("lost_wallet", 1), "Keep the cash"), EffectDescription = T(EventOptionEffectKey("lost_wallet", 1), "+35 gold, -1 HP (guilt)"), GoldGain = 35, HpCost = 1 },
                        new EventOption { Label = T(EventOptionLabelKey("lost_wallet", 2), "Leave it alone"), EffectDescription = T(EventOptionEffectKey("lost_wallet", 2), "Nothing happens") }
                    }
                },

                // ── Coin fountain ──
                new RunEvent
                {
                    Title = T(EventTitleKey("wishing_fountain"), "Wishing Fountain"),
                    Description = T(EventDescriptionKey("wishing_fountain"), "A small fountain glints with dropped coins. A sign reads: 'Wishes: 10g'."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("wishing_fountain", 0), "Toss a coin and wish"), EffectDescription = T(EventOptionEffectKey("wishing_fountain", 0), "+2 HP or +20g (random)"), GoldCost = 10, HpGain = 2 },
                        new EventOption { Label = T(EventOptionLabelKey("wishing_fountain", 1), "Skip the fountain"), EffectDescription = T(EventOptionEffectKey("wishing_fountain", 1), "Nothing happens") }
                    }
                },

                // ── Stray dog ──
                new RunEvent
                {
                    Title = T(EventTitleKey("friendly_stray"), "Friendly Stray"),
                    Description = T(EventDescriptionKey("friendly_stray"), "A friendly stray trots up and drops something at your feet - an old item bag."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("friendly_stray", 0), "Take the bag"), EffectDescription = T(EventOptionEffectKey("friendly_stray", 0), "Gain a random item"), GrantRandomItem = true },
                        new EventOption { Label = T(EventOptionLabelKey("friendly_stray", 1), "Offer a kind nod and leave"), EffectDescription = T(EventOptionEffectKey("friendly_stray", 1), "+1 HP"), HpGain = 1 }
                    }
                },

                // ── Street musician ──
                new RunEvent
                {
                    Title = T(EventTitleKey("street_musician"), "Street Musician"),
                    Description = T(EventDescriptionKey("street_musician"), "A busker plays a calm tune. A small crowd has gathered."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("street_musician", 0), "Tip generously (25g)"), EffectDescription = T(EventOptionEffectKey("street_musician", 0), "+3 HP (renewed energy)"), GoldCost = 25, HpGain = 3 },
                        new EventOption { Label = T(EventOptionLabelKey("street_musician", 1), "Listen for free"), EffectDescription = T(EventOptionEffectKey("street_musician", 1), "+1 HP"), HpGain = 1 },
                        new EventOption { Label = T(EventOptionLabelKey("street_musician", 2), "Walk past"), EffectDescription = T(EventOptionEffectKey("street_musician", 2), "Nothing happens") }
                    }
                },

                // ── Park supply chest ──
                new RunEvent
                {
                    Title = T(EventTitleKey("maintenance_shed"), "Maintenance Shed"),
                    Description = T(EventDescriptionKey("maintenance_shed"), "The park maintenance shed is unlocked and a window is open. Useful supplies inside."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("maintenance_shed", 0), "Borrow some supplies"), EffectDescription = T(EventOptionEffectKey("maintenance_shed", 0), "+6 pencil marks, -1 HP"), PencilGain = 6, HpCost = 1 },
                        new EventOption { Label = T(EventOptionLabelKey("maintenance_shed", 1), "Leave it alone"), EffectDescription = T(EventOptionEffectKey("maintenance_shed", 1), "Nothing happens") }
                    }
                },

                // ── Ink peddler (curse trade) ──
                new RunEvent
                {
                    Title = T(EventTitleKey("ink_peddler"), "The Ink Peddler"),
                    Description = T(EventDescriptionKey("ink_peddler"), "A shady vendor offers to swap your pencil reserves for a fat coin purse. The ink smells strange. He calls it a 'binding deal'."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("ink_peddler", 0), "Take the trade (gain Hollow Pencil curse)"),
                            EffectDescription = T(EventOptionEffectKey("ink_peddler", 0), "-8 Pencil, +60 gold, +Hollow Pencil curse"),
                            PencilCost = 8, GoldGain = 60, CurseId = "hollow_pencil" },
                        new EventOption { Label = T(EventOptionLabelKey("ink_peddler", 1), "Walk away"),
                            EffectDescription = T(EventOptionEffectKey("ink_peddler", 1), "Nothing happens") }
                    }
                },

                // ── Rare flower ──
                new RunEvent
                {
                    Title = T(EventTitleKey("rare_flower_patch"), "Rare Flower Patch"),
                    Description = T(EventDescriptionKey("rare_flower_patch"), "A cluster of unusual flowers blooms in a sheltered corner."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("rare_flower_patch", 0), "Pick one carefully"), EffectDescription = T(EventOptionEffectKey("rare_flower_patch", 0), "+40 gold (sell to florist)"), GoldGain = 40 },
                        new EventOption { Label = T(EventOptionLabelKey("rare_flower_patch", 1), "Leave them to grow"), EffectDescription = T(EventOptionEffectKey("rare_flower_patch", 1), "+1 Max HP"), MaxHpGain = 1 },
                        new EventOption { Label = T(EventOptionLabelKey("rare_flower_patch", 2), "Take a sketch and move"), EffectDescription = T(EventOptionEffectKey("rare_flower_patch", 2), "Nothing happens") }
                    }
                },

                // ── Tempting Shrine (Floor 3+, curse trade) ──
                new RunEvent
                {
                    Title = T(EventTitleKey("tempting_shrine"), "The Tempting Shrine"),
                    MinFloor = 2, // floor index 2 = floor 3
                    Description = T(EventDescriptionKey("tempting_shrine"), "A small stone shrine sits off the path. Moss-covered and old. Something about it feels like a bargain."),
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = T(EventOptionLabelKey("tempting_shrine", 0), "Leave an offering and ask for strength (gain Weight of Stone curse)"),
                            EffectDescription = T(EventOptionEffectKey("tempting_shrine", 0), "+3 HP, +Weight of Stone curse"),
                            HpGain = 3, CurseId = "weight_of_stone" },
                        new EventOption { Label = T(EventOptionLabelKey("tempting_shrine", 1), "Just look and leave"),
                            EffectDescription = T(EventOptionEffectKey("tempting_shrine", 1), "Nothing happens") }
                    }
                }
            };
        }
    }
}
