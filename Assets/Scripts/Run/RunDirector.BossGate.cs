using System;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;

namespace SudokuRoguelike.Run
{
    // Gold at the boss gate (gold-and-items concept L2, owner pick after the 0.2.0 playtest): once per gate the player may
    // pay to strike one offered trial (60 gold × floor number) and to lower the boss's intensity one step (40 gold × floor
    // number). A struck trial leaves the offer and the boss takes one trial fewer (the gate always offers one more than
    // it takes, so removing an offer alone would buy nothing). Both are kept in RunState for the floor, so Save & Quit at
    // the gate neither loses nor repeats them.
    public sealed partial class RunDirector
    {
        public const int BossStrikeGoldPerFloor = 60;
        public const int BossCalmGoldPerFloor = 40;

        /// <summary>[REQ: ECON-GATE-001] 60 gold × floor number (Floor 3: 180).</summary>
        public int GetBossStrikeCost() => BossStrikeGoldPerFloor * (State.CurrentFloor + 1);

        /// <summary>[REQ: ECON-GATE-002] 40 gold × floor number (Floor 3: 120).</summary>
        public int GetBossCalmCost() => BossCalmGoldPerFloor * (State.CurrentFloor + 1);

        // The run stores floor NUMBERS (1-based; 0 = none), so a save from before these fields reads as "nothing done".
        private int FloorNumber => State.CurrentFloor + 1;

        public bool BossTrialStruckThisFloor => State != null && State.BossTrialStruckOnFloor == FloorNumber;

        public bool BossIntensityLoweredThisFloor => State != null && State.BossIntensityLoweredOnFloor == FloorNumber;

        /// <summary>Gold buys nothing at the gate outside a Garden Run (tutorial, Monthly Walk).</summary>
        public bool BossGateGoldAllowed =>
            State != null && State.Mode == GameMode.GardenRun && !State.TutorialMode && !State.IsSeasonalChallenge;

        /// <summary>The trial struck at this floor's gate, or null.</summary>
        public BossModifierId? StruckBossTrial => BossTrialStruckThisFloor ? State.BossTrialStruck : (BossModifierId?)null;

        /// <summary>[REQ: ECON-GATE-001] Trials the boss takes: the floor's count, one fewer once a trial was struck.</summary>
        public int GetBossPicksRequired()
        {
            GetBossModifierCounts(out _, out var picks);
            return BossTrialStruckThisFloor ? Math.Max(1, picks - 1) : picks;
        }

        /// <summary>
        /// [REQ: ECON-GATE-001] A trial can be struck once per gate while the boss takes at least two and at least two are
        /// still offered (one always stays), and the purse holds the price.
        /// </summary>
        public bool CanStrikeBossTrial(int offeredCount = int.MaxValue)
        {
            if (!BossGateGoldAllowed || BossTrialStruckThisFloor || offeredCount < 2 || State.CurrentGold < GetBossStrikeCost())
                return false;
            GetBossModifierCounts(out _, out var picks);
            return picks > 1;
        }

        public bool TryStrikeBossTrial(BossModifierId trial, int offeredCount = int.MaxValue)
        {
            if (!CanStrikeBossTrial(offeredCount)) return false;
            SpendGateGold(GetBossStrikeCost());
            State.BossTrialStruckOnFloor = FloorNumber;
            State.BossTrialStruck = trial;
            return true;
        }

        /// <summary>The Warding Flame relic removes <paramref name="trial"/> from this gate, once per run; kept in the save.</summary>
        public bool TryUseWardingFlame(BossModifierId trial)
        {
            if (State == null || !RelicService.TryWardingFlame(State)) return false;
            State.WardingFlameOnFloor = FloorNumber;
            State.WardingFlameTrial = trial;
            return true;
        }

        /// <summary>Removes the trials this floor's gate already took away (Warding Flame, a paid strike) from an offer.</summary>
        public void ApplyGateRemovals(System.Collections.Generic.List<BossModifierId> offered)
        {
            if (offered == null || State == null) return;
            if (State.WardingFlameOnFloor == FloorNumber) offered.Remove(State.WardingFlameTrial);
            if (BossTrialStruckThisFloor) offered.Remove(State.BossTrialStruck);
        }

        /// <summary>[REQ: ECON-GATE-002] The boss puzzle's intensity: the run's, one step lower when it was paid for.</summary>
        public BossModifierIntensity GetBossIntensity()
        {
            var intensity = BossService.IntensityForRunNumber(State.RunNumber);
            return BossIntensityLoweredThisFloor && intensity > BossModifierIntensity.Low ? intensity - 1 : intensity;
        }

        public bool CanLowerBossIntensity() =>
            BossGateGoldAllowed && !BossIntensityLoweredThisFloor
            && BossService.IntensityForRunNumber(State.RunNumber) > BossModifierIntensity.Low
            && State.CurrentGold >= GetBossCalmCost();

        public bool TryLowerBossIntensity()
        {
            if (!CanLowerBossIntensity()) return false;
            SpendGateGold(GetBossCalmCost());
            State.BossIntensityLoweredOnFloor = FloorNumber;
            return true;
        }

        private void SpendGateGold(int cost)
        {
            State.CurrentGold = Math.Max(0, State.CurrentGold - cost);
            State.GoldSpentThisRun += cost;
            if (DailyGoals != null && State.GoldSpentThisRun >= 50)
                DailyGoalService.EvaluateInRun(DailyGoals, State, CurrentLevelState, DailyGoalService.TriggerSpentGold50);
        }
    }
}
