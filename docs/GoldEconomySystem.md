# Gold Economy System

**Version:** 2.2  
**Date:** 2026-10-21  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.2 | 2026-10-21 | implemented | Owner's 0.2.0 playtest ("gold and items are not needed"), levers L1, L2, L5 and L6 of `design/gold-and-items-concept.md`: item prices ×2.5 (Normal 40, Rare 75, Epic 150; ECON-BAL-001); puzzle gold −20 % (ECON-BAL-002); gold at the boss gate: strike a trial for 60 × floor number, lower the intensity for 40 × floor number (ECON-GATE-001/002); gold kept at the end scores (SCORE-GOLD-001, GameDesignSpec §9c). The late-floor mistake cost and the quarter heal (ECON-BAL-003/004) are in GameDesignSpec §5.1 and §7. |
| 2.1 | 2026-10-07 | implemented | Pencil purchase and item-reward reroll implemented and documented (SPEC-CONF-029, GAP-015/016); Nothing/Skip gold (SPEC-AMB-007); gold collected definition (SPEC-AMB-006, GAP-027); shop offer counts and full-bag rule (SPEC-CONF-005, GAP-036). |

## Overview

Gold is a run-scoped resource used in the Garden Run economy. No-progression modes bypass most or all of this loop.

Primary implementation references:

- `Assets/Scripts/Economy/GoldTable.cs`
- `Assets/Scripts/Economy/ShopService.cs`
- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/Economy/RelicService.cs`
- `Assets/Scripts/Run/CurseService.cs`

## Puzzle Gold

Base puzzle gold is calculated through `GoldTable.CalculatePuzzleGold(boardSize, stars)`.

Current formula:

```text
BaseGoldForBoardSize(boardSize) * (1.0 + stars * 0.2) * 0.8
```

The factor 0.8 (`GoldTable.PuzzleGoldFactor`, ECON-BAL-002) is lever L1 of `design/gold-and-items-concept.md`: one floor's
puzzles used to pay for the whole shop several times over. A 6×6 one-star puzzle pays 29 gold, a 9×9 three-star one 128.

Board-size base values:

| Size | Base Gold |
|---|---:|
| 5 | 20 |
| 6 | 30 |
| 7 | 45 |
| 8 | 65 |
| 9 | 100 |

Puzzle gold is multiplied by the Harmony gold multiplier (`RunState.GlobalGoldMultiplier`). All gold income goes through `RunState.GainGold`, which counts the amount in `GoldCollectedThisRun`. Gold collected is what the "Collect 50,000 gold" class unlock and the profile's total gold earned count; spending never reduces it (decided 2026-10-07, SPEC-AMB-006, GAP-027).

Runtime modifiers can then adjust the result through:

- cursed board multipliers
- positive floor effects
- relic effects

## Spending

### Shop Purchases

Shops sell items only: 2 offers on Floors 1–2, 3 on Floors 3–4, 4 on Floor 5 (SPEC-CONF-005). Buying with a full bag requires choosing an item to replace; without one nothing is charged (GAP-036). Offer prices are built with:

```text
BasePrice * (1 + floorIndex * 0.5) * priceMultiplier * harmonySurcharge
```

Base prices (ECON-BAL-001, `GoldTable.BaseItemPrice`, the one price table): Normal **40**, Rare **75**, Epic **150**
(2.5 times the old 15 / 30 / 60). A Rare item costs 75 on Floor 1, 112 on Floor 2 and 150 on Floor 3.

### Shop Reroll

Current shop reroll cost:

```text
15 + (ShopRerollCount * 5)
```

### Emergency Heal

Current emergency-heal cost:

```text
round(25 * (1 + PencilPurchaseCount * 0.5))
```

Buying it restores 1 HP and increments `PencilPurchaseCount`.

### Pencil Purchase (1.1, GAP-016)

During a puzzle the "+3 Pencil" button buys 3 Pencil (up to the maximum):

```text
20 + 20 * PencilBoughtCount
```

Not offered when Pencil is full, in tutorials, in the Monthly Walk or for Garden Monk (GDS §5.2).

### Item Reward Reroll (1.1, GAP-015)

On the item reward screen, **Reroll** replaces every slot that holds an item (Nothing slots stay). It costs a Reroll Token if the player has one, otherwise:

```text
20 + 20 * RewardRerollCount
```

`RewardRerollCount` counts every reward reroll of the run, token or gold.

### Boss Gate (2.2, ECON-GATE-001/002)

At the boss gate the purse is shown beside two once-per-gate actions (gold-and-items concept L2):

- **Strike a trial** (ECON-GATE-001): `60 × floor number` gold (Floor 3: 180). The player then clicks the trial to strike;
  it leaves the offer and the boss takes one trial fewer (Floor 3: 2 of 3 instead of 3 of 4). The gate always offers one
  trial more than the boss takes, so removing an offer alone would buy nothing. Not on a floor whose boss takes a single
  trial (Floor 1).
- **Lower intensity** (ECON-GATE-002): `40 × floor number` gold lowers the boss puzzle's intensity one step (Very High →
  High → Medium → Low); not when it is Low. Only the boss puzzle uses the lowered intensity; its debuff stays the one the
  map and the gate showed.

Both are stored in the run (`RunState.BossTrialStruckOnFloor`, `BossTrialStruck`, `BossIntensityLoweredOnFloor`, as 1-based floor numbers so an older save reads as "nothing done"), so Save &
Quit at the gate keeps them, and count as gold spent.

### Gold Kept (2.2, SCORE-GOLD-001)

Gold still held when the run ends adds one point per 5 gold to the run score, at most 15 % of the base (GameDesignSpec
§9c), so saving gold has a goal too.

### Nothing / Skip Reward (1.1, SPEC-AMB-007)

Taking a Nothing slot or skipping the reward grants `5 * (floor index + 1)` gold (5 on Floor 1 … 25 on Floor 5); not in tutorials or the Monthly Walk.

## Mode Boundaries

Gold progression is skipped in no-progression modes because their runs use `DisableProgressionRewards = true`.

That applies to:

- Endless Zen
- Spirit Trials
- tutorial runs
- seasonal challenge reward flow

## Notes On Drift

Older design text referred to different reroll/heal curves and a broader shop economy. This spec reflects the current runtime code path in `RunDirector` (`RunDirector.Economy.cs`) and `ShopService`. The pencil purchase and item-reward reroll from GameDesignSpec §5.2/§5.3/§8, which this document had dropped silently, are implemented since 1.1 (decided 2026-10-07, SPEC-CONF-029; SPEC-PROC-002 asks to record such removals as decisions). Evidence: `EconomyActionTests`.
