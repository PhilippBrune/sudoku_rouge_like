using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class RunEventService
    {
        private readonly CurseService _curseService;

        public RunEventService(CurseService curseService)
        {
            _curseService = curseService;
        }

        public RunEvent RollEvent(Random random, RunState runState)
        {
            var rareBonus = _curseService.GetRareEventBonusChance(runState);
            var roll = random.NextDouble();

            if (roll < 0.33)
                return BuildSacrificeEvent();
            if (roll < 0.66 + rareBonus)
                return BuildRiskAmplificationEvent();
            return BuildResourceTradeEvent();
        }

        public bool ResolveChoice(RunState runState, RunEvent runEvent, string optionId)
        {
            if (runState == null || runEvent == null || string.IsNullOrWhiteSpace(optionId))
                return false;

            if (runEvent.Category == EventCategory.Sacrifice)
            {
                if (optionId == "sacrifice_hp")
                {
                    runState.CurrentHP = Math.Max(1, runState.CurrentHP - 2);
                    return true;
                }

                if (optionId == "sacrifice_relic")
                {
                    // Trade current relic for +1 max HP permanently (spec: trade events)
                    if (!runState.HasRelic)
                        return false;

                    runState.HasRelic = false;
                    runState.HeldRelic = null;
                    runState.MaxHP += 1;
                    runState.CurrentHP += 1;
                    return true;
                }
            }
            else if (runEvent.Category == EventCategory.RiskAmplification)
            {
                if (optionId == "risk_double_reward")
                {
                    runState.RunNotes.Add("Next puzzle elite and rewards doubled.");
                    return true;
                }

                if (optionId == "take_curse_skip")
                {
                    _curseService.ApplyCurse(runState, CurseType.MinorCurse);
                    runState.RunNotes.Add("Next combat skipped via curse.");
                    return true;
                }
            }
            else if (runEvent.Category == EventCategory.ResourceTrade)
            {
                if (optionId == "gold_for_combo")
                {
                    if (runState.CurrentGold < 35)
                        return false;

                    // Trade gold for Monk Charm relic (single slot)
                    runState.CurrentGold -= 35;
                    runState.HasRelic = true;
                    runState.HeldRelic = new RelicInstance
                    {
                        Id = RelicId.MonkCharm,
                        Tier = RelicTier.Tier2,
                        UsesRemaining = -1
                    };
                    return true;
                }

                if (optionId == "relic_for_hp")
                {
                    if (!runState.HasRelic)
                        return false;

                    runState.HasRelic = false;
                    runState.HeldRelic = null;
                    runState.MaxHP += 1;
                    runState.CurrentHP += 1;
                    return true;
                }
            }

            return false;
        }

        private static RunEvent BuildSacrificeEvent()
        {
            var runEvent = new RunEvent
            {
                EventId = "ancient_statue_focus",
                Category = EventCategory.Sacrifice,
                Prompt = "An ancient statue demands focus."
            };
            runEvent.Options.Add(new RunEventOption { OptionId = "sacrifice_hp", Label = "Lose 2 HP", Tradeoff = "Remove one active modifier." });
            runEvent.Options.Add(new RunEventOption { OptionId = "sacrifice_relic", Label = "Trade current relic", Tradeoff = "Gain +1 max HP permanently." });
            return runEvent;
        }

        private static RunEvent BuildRiskAmplificationEvent()
        {
            var runEvent = new RunEvent
            {
                EventId = "storm_clouds_gather",
                Category = EventCategory.RiskAmplification,
                Prompt = "Storm clouds gather over the garden."
            };
            runEvent.Options.Add(new RunEventOption { OptionId = "risk_double_reward", Label = "Elite next puzzle", Tradeoff = "Double rewards." });
            runEvent.Options.Add(new RunEventOption { OptionId = "take_curse_skip", Label = "Take a curse", Tradeoff = "Skip next combat." });
            return runEvent;
        }

        private static RunEvent BuildResourceTradeEvent()
        {
            var runEvent = new RunEvent
            {
                EventId = "wandering_monk_exchange",
                Category = EventCategory.ResourceTrade,
                Prompt = "A wandering monk offers an exchange."
            };
            runEvent.Options.Add(new RunEventOption { OptionId = "gold_for_combo", Label = "Pay 35 gold", Tradeoff = "Gain Monk Charm relic." });
            runEvent.Options.Add(new RunEventOption { OptionId = "relic_for_hp", Label = "Trade current relic", Tradeoff = "Gain +1 max HP permanently." });
            return runEvent;
        }
    }
}
