using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Items;
using SudokuRoguelike.Save;
using SudokuRoguelike.Sudoku;

namespace SudokuRoguelike.Run
{
    // Boss gate choices, events, relic rewards and the curse API.
    // Split out of RunDirector.cs to keep it within its size budget (docs/controller-extraction-plan.md).
    public sealed partial class RunDirector
    {
        // ── Boss Modifier Choice ──

        public List<BossModifierId> RollBossModifierChoices()
        {
            var minSize = FloorThemeData.GetMinBoardSize(State.CurrentFloor);
            _bossService.ReseedForDecision(DecisionSeed(DecisionPurpose.BossChoices));
            BossModifierChoices = _bossService.RollBossChoices(State.CurrentFloor, State.ActiveFloorModifiers, minSize, State.HarmonyLevel);
            return BossModifierChoices;
        }

        /// <summary>The debuff this floor's boss carries (Garden Run, Floor 3 onward), or null.
        /// Depends only on the run seed and floor, so the map, the gate and the boss puzzle agree.</summary>
        // [REQ: REQ-BOSS-108] GAP-026 / SPEC-CONF-021
        public BossModifierId? GetBossDebuffForCurrentFloor()
        {
            if (State == null || State.Mode != GameMode.GardenRun || State.CurrentFloor < BossService.FirstBossDebuffFloor)
                return null;
            unchecked
            {
                var h = Mix((uint)State.Seed * 0x9E3779B1u);
                h = Mix(h ^ (uint)State.CurrentFloor * 0x85EBCA77u);
                h = Mix(h ^ (uint)DecisionPurpose.BossDebuff * 0x165667B1u);
                var intensity = BossService.IntensityForRunNumber(State.RunNumber);
                return BossService.PickBossDebuff(intensity, new System.Random((int)h));
            }
        }

        // [REQ: HARMONY-BOSS-002] Same Harmony bonus as RollBossChoices, so shown − picks is always 1 (GAP-008).
        public void GetBossModifierCounts(out int shown, out int picks)
        {
            BossService.GetBossModifierCounts(State.CurrentFloor, out shown, out picks,
                HarmonyDifficultyService.GetBossOptionsBonus(State.HarmonyLevel));
        }

        public void ChooseBossModifiers(List<BossModifierId> chosen)
        {
            State.ChosenBossModifiers.Clear();
            State.HasChosenBossModifier = false;
            if (chosen == null) return;

            for (var i = 0; i < chosen.Count; i++)
            {
                var modifier = chosen[i];
                if (!BossService.IsSupportedForGeneratedRun(modifier)
                    || !BossService.CanCombineModifiers(State.ChosenBossModifiers, modifier))
                {
                    Debug.LogWarning(
                        $"[RunDirector] Ignored incompatible boss modifier selection: {modifier}.");
                    continue;
                }

                State.ChosenBossModifiers.Add(modifier);
                State.SeenBossModifiers.Add(modifier);
            }

            // Keep legacy singular field in sync for save compat
            if (State.ChosenBossModifiers.Count > 0)
            {
                State.HasChosenBossModifier = true;
                State.ChosenBossModifierId = State.ChosenBossModifiers[0];
            }

            Debug.Log(
                $"[ModifierDiscovery] Boss choice confirmed: chosen={ModifierDiscoveryService.Describe(State.ChosenBossModifiers)}, " +
                $"runSeen={ModifierDiscoveryService.Describe(State.SeenBossModifiers)}");
            Debug.Log($"[RunDirector] ChooseBossModifiers: [{string.Join(", ", State.ChosenBossModifiers)}]");
        }

        /// <summary>Legacy single-choice shortcut (picks 1 modifier).</summary>
        public void ChooseBossModifier(BossModifierId chosen)
        {
            ChooseBossModifiers(new List<BossModifierId> { chosen });
        }

        // ── Events ──

        public RunEvent BuildCurrentEvent()
        {
            _eventService.ReseedForDecision(DecisionSeed(DecisionPurpose.Event));
            CurrentEvent = _eventService.BuildEvent(State.CurrentFloor, State.Depth);
            return CurrentEvent;
        }

        /// <returns>Human-readable description of what was gained/lost.</returns>
        public string ResolveCurrentEventChoice(int optionIndex)
        {
            if (CurrentEvent == null) return string.Empty;
            _eventService.ReseedForDecision(DecisionSeed(DecisionPurpose.EventChoice, optionIndex));
            var summary = _eventService.ResolveChoice(State, CurrentEvent, optionIndex);
            CurrentEvent = null;
            return summary;
        }

        // ── Relic ──

        public RelicInstance RollRelicReward()
        {
            var tierBonus = RelicService.GetRelicNodeTierBonus(State);
            _relicService.ReseedForDecision(DecisionSeed(DecisionPurpose.RelicReward));
            return _relicService.RollRelic(State.CurrentFloor, tierBonus);
        }

        /// <summary>Roll distinct choices for this panel; copies may still be acquired across different nodes.</summary>
        public List<RelicInstance> RollRelicChoices(int count = 3)
        {
            var tierBonus = RelicService.GetRelicNodeTierBonus(State);
            var classId = State.IsSeasonalChallenge ? (ClassId)0 : State.ClassId;
            _relicService.ReseedForDecision(DecisionSeed(DecisionPurpose.RelicChoices));
            RolledRelicChoices = _relicService.RollRelicChoices(State.CurrentFloor, count, tierBonus,
                classId, GetCurrentClassLevel());
            return RolledRelicChoices;
        }

        /// <summary>Accept the relic at the given choice index (from RolledRelicChoices).</summary>
        public void AcceptRelicChoice(int index)
        {
            if (RolledRelicChoices == null || index < 0 || index >= RolledRelicChoices.Count) return;
            AcceptRelicChoice(RolledRelicChoices[index]);
        }

        // [REQ: REQ-ITEM-104] Accepts the relic instance the player picked on the panel, independent of the
        // RolledRelicChoices list, which may be gone or re-rolled after a resume (GAP-021).
        public void AcceptRelicChoice(RelicInstance chosen)
        {
            if (chosen == null) return;
            AcceptRelic(chosen);
            RolledRelicChoices = null;
        }

        public void AcceptRelic(RelicInstance relic)
        {
            State.HeldRelics.Add(relic);
            // Keep legacy fields in sync for serialization compatibility
            State.HasRelic = true;
            State.HeldRelic = State.HeldRelics[0];
            RelicService.ApplyPickupPassives(State, relic);
            if (DailyGoals != null && !State.IsSeasonalChallenge)
                DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerRelicCollected);
        }

        /// <summary>Called when the player accepts a Cursed node offer.</summary>
        public void OnCursedNodeAccepted()
        {
            if (DailyGoals == null || State.IsSeasonalChallenge) return;
            DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerAcceptedCursed);
        }

        // ── Curse API ── [REQ: CURSE-DATA-002]

        // [REQ: CURSE-INT-001] [REQ: CURSE-ACQUIRE-001] [REQ: CURSE-ACQUIRE-002] ApplyCurse: unified entry point for all curse acquisition (events, items, rest trades)
        public bool ApplyCurse(string curseId) => CurseService.ApplyCurse(State, curseId);
        public bool RemoveCurse(string curseId = null) => CurseService.TryRemoveCurse(State, curseId);
        public CurseDefinition RollCurse()
        {
            _curseService.ReseedForDecision(DecisionSeed(DecisionPurpose.CurseRoll));
            return _curseService.RollCurse(State.CurrentFloor, State);
        }
        public List<CurseDefinition> GetActiveCurses() => CurseService.GetActiveCurses(State);
        public bool HasAnyCurse() => CurseService.HasActiveCurse(State);
    }
}
