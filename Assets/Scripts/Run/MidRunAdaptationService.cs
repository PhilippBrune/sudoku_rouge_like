using System;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    public sealed class MidRunAdaptationService
    {
        public bool TryTransformRelics(RunState runState, Random random)
        {
            if (runState == null || runState.RelicIds.Count < 2)
            {
                return false;
            }

            runState.RelicIds.RemoveAt(runState.RelicIds.Count - 1);
            runState.RelicIds.RemoveAt(runState.RelicIds.Count - 1);

            var cursed = random.NextDouble() < 0.2;
            var generated = cursed ? "relic_cursed_t4_transmuted" : "relic_utility_t4_transmuted";
            runState.RelicIds.Add(generated);
            return true;
        }

        public void ApplyTemporaryMutation(RunState runState, AdaptationMutationType mutation, int nodes)
        {
            if (runState == null)
            {
                return;
            }

            runState.ActiveMutation = mutation;
            runState.MutationNodesRemaining = Math.Max(1, nodes);
        }

        public void TickMutationNode(RunState runState)
        {
            if (runState == null || runState.ActiveMutation == AdaptationMutationType.None)
            {
                return;
            }

            runState.MutationNodesRemaining--;
            if (runState.MutationNodesRemaining <= 0)
            {
                runState.ActiveMutation = AdaptationMutationType.None;
                runState.MutationNodesRemaining = 0;
            }
        }

        public bool TryRiskyRebuild(RunState runState)
        {
            if (runState == null || runState.RiskyRebuildUsed)
            {
                return false;
            }

            runState.RelicIds.Clear();
            runState.RelicIds.Add("relic_legend_shifting_garden");
            runState.RelicIds.Add("relic_legend_golden_root");
            runState.CurrentHP = 1;
            runState.RiskyRebuildUsed = true;
            return true;
        }

        public bool TryRerouteModifier(MetaProgressionState meta, BossModifierId remove, BossModifierId add)
        {
            if (meta == null || remove == add)
            {
                return false;
            }

            meta.PurchasedPermanentUpgrades.Add($"reroute_{remove}_to_{add}");
            return true;
        }
    }
}
