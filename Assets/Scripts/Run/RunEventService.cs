using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunEventService
    {
        private readonly Random _random;

        public RunEventService(int seed)
        {
            _random = new Random(seed);
        }

        public RunEvent BuildEvent(int floorIndex, int depth)
        {
            var events = GetEventPool(floorIndex);
            var picked = events[_random.Next(events.Count)];
            picked.Id = $"evt_{floorIndex}_{depth}_{_random.Next(10000)}";
            return picked;
        }

        public void ResolveChoice(RunState state, RunEvent runEvent, int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= runEvent.Options.Count) return;

            var option = runEvent.Options[optionIndex];
            state.CurrentGold = Math.Max(0, state.CurrentGold - option.GoldCost);
            state.CurrentHP = Math.Max(1, state.CurrentHP - option.HpCost);
            state.CurrentPencil = Math.Max(0, state.CurrentPencil - option.PencilCost);
        }

        private List<RunEvent> GetEventPool(int floorIndex)
        {
            return new List<RunEvent>
            {
                new RunEvent
                {
                    Title = "Wandering Merchant",
                    Description = "A merchant offers you a trade.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Trade Gold for HP", EffectDescription = "Pay 30 gold, gain 2 HP", GoldCost = 30 },
                        new EventOption { Label = "Decline", EffectDescription = "Nothing happens" }
                    }
                },
                new RunEvent
                {
                    Title = "Stone Garden Shrine",
                    Description = "A quiet shrine glows softly.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Pray", EffectDescription = "Sacrifice 1 HP for 40 gold", HpCost = 1 },
                        new EventOption { Label = "Pass by", EffectDescription = "Nothing happens" }
                    }
                },
                new RunEvent
                {
                    Title = "Fallen Ink Pot",
                    Description = "Spilled ink stains the ground.",
                    Options = new List<EventOption>
                    {
                        new EventOption { Label = "Gather ink", EffectDescription = "Gain 3 pencil marks, pay 15 gold", GoldCost = 15 },
                        new EventOption { Label = "Leave it", EffectDescription = "Nothing happens" }
                    }
                }
            };
        }
    }
}
