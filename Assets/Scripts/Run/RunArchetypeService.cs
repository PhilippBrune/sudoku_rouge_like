using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    public sealed class RunArchetypeService
    {
        public RunArchetype Evaluate(RunState runState)
        {
            if (runState == null)
            {
                return RunArchetype.Undefined;
            }

            var economy = runState.CurrentGold >= 120 ? 2 : runState.CurrentGold >= 60 ? 1 : 0;
            var survival = runState.CurrentHP <= 3 ? 2 : runState.MistakeShieldCharges > 0 ? 1 : 0;
            var modifier = 0;
            var combo = 0;

            // Single relic contributes to archetype classification
            if (runState.HasRelic)
            {
                var tier = RelicService.GetTier(runState.HeldRelic.Id);
                var tierWeight = tier >= RelicTier.Tier3 ? 2 : 1;

                switch (runState.HeldRelic.Id)
                {
                    case RelicId.GoldenRoot:
                    case RelicId.WoodenComb:
                    case RelicId.CopperTortoise:
                    case RelicId.SpiritLantern:
                    case RelicId.MossToken:
                        economy += tierWeight;
                        break;

                    case RelicId.CrackedTeacup:
                    case RelicId.WisteriaBranch:
                    case RelicId.PhoenixFeather:
                    case RelicId.SilentGrid:
                    case RelicId.SakuraSeal:
                        survival += tierWeight;
                        break;

                    case RelicId.CrimsonFan:
                    case RelicId.PorcelainMask:
                    case RelicId.MoonstoneCompass:
                    case RelicId.ShiftingGarden:
                        modifier += tierWeight;
                        break;

                    case RelicId.MonkCharm:
                    case RelicId.KoiReflectionRelic:
                    case RelicId.StoneSundial:
                    case RelicId.EternalLotus:
                    case RelicId.DragonsEye:
                        combo += tierWeight;
                        break;
                }
            }

            // Item usage patterns also contribute
            if (runState.ItemsUsedCount >= 5) combo++;
            if (runState.ItemsUsedCount >= 10) combo++;

            if (economy >= survival && economy >= modifier && economy >= combo) return RunArchetype.EconomyMerchantMonk;
            if (modifier >= survival && modifier >= combo) return RunArchetype.ModifierRuleBender;
            if (survival >= combo) return RunArchetype.SurvivalEnduringSage;
            return RunArchetype.ComboFlowMaster;
        }
    }
}
