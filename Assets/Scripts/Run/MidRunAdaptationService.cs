using System;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    public sealed class MidRunAdaptationService
    {
        public bool TryTransformRelics(RunState runState, Random random)
        {
            if (runState == null || !runState.HasRelic)
            {
                return false;
            }

            // Transmutation: upgrade current relic to a higher-tier variant
            var currentTier = RelicService.GetTier(runState.HeldRelic.Id);
            if (currentTier >= RelicTier.Tier4)
            {
                return false; // already high tier, no upgrade
            }

            // Replace with TransmutedSigil (Tier 4) as the transmutation result
            runState.HeldRelic = new RelicInstance
            {
                Id = RelicId.TransmutedSigil,
                Tier = RelicTier.Tier4,
                UsesRemaining = -1
            };
            runState.MaxHP += 1;
            runState.CurrentHP += 1;
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

            // Risky rebuild: grant Shifting Garden relic but drop HP to 1
            runState.HasRelic = true;
            runState.HeldRelic = new RelicInstance
            {
                Id = RelicId.ShiftingGarden,
                Tier = RelicTier.Legendary,
                UsesRemaining = -1
            };
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
