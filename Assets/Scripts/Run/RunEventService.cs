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
                parts.Add($"-{option.GoldCost} gold");
            }
            if (option.HpCost > 0)
            {
                state.CurrentHP = Math.Max(1, state.CurrentHP - option.HpCost);
                parts.Add($"-{option.HpCost} HP");
            }
            if (option.PencilCost > 0)
            {
                state.CurrentPencil = Math.Max(0, state.CurrentPencil - option.PencilCost);
                parts.Add($"-{option.PencilCost} pencil");
            }

            // ── Rewards ──
            if (option.GoldGain > 0)
            {
                state.CurrentGold += option.GoldGain;
                parts.Add($"+{option.GoldGain} gold");
            }
            if (option.HpGain > 0)
            {
                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + option.HpGain);
                parts.Add($"+{option.HpGain} HP");
            }
            if (option.PencilGain > 0)
            {
                state.CurrentPencil = Math.Min(state.MaxPencil, state.CurrentPencil + option.PencilGain);
                parts.Add($"+{option.PencilGain} pencil");
            }
            if (option.MaxHpGain > 0)
            {
                state.MaxHP += option.MaxHpGain;
                state.CurrentHP = Math.Min(state.MaxHP, state.CurrentHP + option.MaxHpGain);
                parts.Add($"+{option.MaxHpGain} Max HP");
            }
            if (option.GrantRandomItem)
            {
                var slots = _itemService.RollSlots(2, 1, 0);
                ItemInstance granted = null;
                for (var i = 0; i < slots.Count; i++) { if (slots[i] != null) { granted = slots[i]; break; } }
                if (granted != null && state.HeldItems.Count < state.ItemSlots)
                {
                    state.HeldItems.Add(granted);
                    parts.Add($"+{ItemService.GetItemName(granted.Type)}");
                }
            }

            if (!string.IsNullOrEmpty(option.CurseId))
            {
                if (CurseService.ApplyCurse(state, option.CurseId))
                {
                    var def = CurseService.GetDefinition(option.CurseId);
                    parts.Add($"+{(def != null ? def.Name : option.CurseId)} (curse)");
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "Nothing happened.";
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

        private List<RunEvent> BuildFullEventPool()
        {
            return new List<RunEvent>
            {
                // ── Park bench (rest) ──
                new RunEvent
                {
                    Title = "A Quiet Bench",
                    Description = "A weathered wooden bench sits in a sunny patch. You could sit for a moment.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Rest a while",    EffectDescription = "+2 HP",            HpGain = 2 },
                        new EventOption { Label = "Keep walking",    EffectDescription = "Nothing happens" }
                    }
                },

                // ── Street vendor / ice cream stand ──
                new RunEvent
                {
                    Title = "Ice Cream Cart",
                    Description = "A colourful cart hums with a small freezer. The vendor waves you over.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Buy a scoop (20g)",      EffectDescription = "Pay 20 gold, gain 3 HP",       GoldCost = 20, HpGain = 3 },
                        new EventOption { Label = "Just browse",            EffectDescription = "Nothing happens" }
                    }
                },

                // ── Fallen ink pot → pencil ──
                new RunEvent
                {
                    Title = "Spilled Ink Bottle",
                    Description = "An artist's ink bottle has tipped over near a park path. Still mostly full.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Gather ink (15g)",       EffectDescription = "+5 pencil marks, pay 15 gold", GoldCost = 15, PencilGain = 5 },
                        new EventOption { Label = "Leave it",               EffectDescription = "Nothing happens" }
                    }
                },

                // ── Lost wallet ──
                new RunEvent
                {
                    Title = "Lost Wallet",
                    Description = "A worn leather wallet lies on the path. Someone's name is inside.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Return it honestly",     EffectDescription = "+1 Max HP (gratitude)",        MaxHpGain = 1 },
                        new EventOption { Label = "Keep the cash",          EffectDescription = "+35 gold, -1 HP (guilt)",      GoldGain = 35, HpCost = 1 },
                        new EventOption { Label = "Leave it alone",         EffectDescription = "Nothing happens" }
                    }
                },

                // ── Coin fountain ──
                new RunEvent
                {
                    Title = "Wishing Fountain",
                    Description = "A small fountain glints with dropped coins. A sign reads: 'Wishes: 10g'.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Toss a coin and wish",   EffectDescription = "+2 HP or +20g (random)",       GoldCost = 10, HpGain = 2 },
                        new EventOption { Label = "Skip the fountain",      EffectDescription = "Nothing happens" }
                    }
                },

                // ── Stray dog ──
                new RunEvent
                {
                    Title = "Friendly Stray",
                    Description = "A scruffy dog trots up and drops something at your feet — an old item bag.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Take the bag",           EffectDescription = "Gain a random item",           GrantRandomItem = true },
                        new EventOption { Label = "Pat the dog and leave",  EffectDescription = "+1 HP",                        HpGain = 1 }
                    }
                },

                // ── Street musician ──
                new RunEvent
                {
                    Title = "Street Musician",
                    Description = "A busker plays a calm tune. A small crowd has gathered.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Tip generously (25g)",   EffectDescription = "+3 HP (renewed energy)",       GoldCost = 25, HpGain = 3 },
                        new EventOption { Label = "Listen for free",        EffectDescription = "+1 HP",                        HpGain = 1 },
                        new EventOption { Label = "Walk past",              EffectDescription = "Nothing happens" }
                    }
                },

                // ── Park supply chest ──
                new RunEvent
                {
                    Title = "Maintenance Shed",
                    Description = "The park maintenance shed is unlocked and a window is open. Useful supplies inside.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Borrow some supplies",   EffectDescription = "+6 pencil marks, -1 HP",       PencilGain = 6, HpCost = 1 },
                        new EventOption { Label = "Leave it alone",         EffectDescription = "Nothing happens" }
                    }
                },

                // ── Ink peddler (curse trade) ──
                new RunEvent
                {
                    Title = "The Ink Peddler",
                    Description = "A shady vendor offers to swap your pencil reserves for a fat coin purse. "
                        + "The ink smells strange. He calls it a 'binding deal'.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Take the trade (gain Hollow Pencil curse)",
                            EffectDescription = "–8 Pencil, +60 gold, +Hollow Pencil curse",
                            PencilCost = 8, GoldGain = 60, CurseId = "hollow_pencil" },
                        new EventOption { Label = "Walk away",
                            EffectDescription = "Nothing happens" }
                    }
                },

                // ── Rare flower ──
                new RunEvent
                {
                    Title = "Rare Flower Patch",
                    Description = "A cluster of unusual flowers blooms in a sheltered corner.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Pick one carefully",     EffectDescription = "+40 gold (sell to florist)",   GoldGain = 40 },
                        new EventOption { Label = "Leave them to grow",     EffectDescription = "+1 Max HP",                    MaxHpGain = 1 },
                        new EventOption { Label = "Take a photo and move",  EffectDescription = "Nothing happens" }
                    }
                },

                // ── Tempting Shrine (Floor 3+, curse trade) ──
                new RunEvent
                {
                    Title = "The Tempting Shrine",
                    MinFloor = 2, // floor index 2 = floor 3
                    Description = "A small stone shrine sits off the path. Moss-covered and old. "
                        + "Something about it feels like a bargain.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Leave an offering and ask for strength (gain Weight of Stone curse)",
                            EffectDescription = "+3 HP, +Weight of Stone curse",
                            HpGain = 3, CurseId = "weight_of_stone" },
                        new EventOption { Label = "Just look and leave",
                            EffectDescription = "Nothing happens" }
                    }
                }
            };
        }
    }
}
