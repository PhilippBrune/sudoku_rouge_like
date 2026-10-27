# Gold Economy System

**Version:** 2.3  
**Date:** 2026-10-27  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.3 | 2026-10-27 | implemented | Owner request (2026-10-27): the gold and items concept (v1.2) is merged here: "Why and the levers" with the playtest numbers, the lever status (L1 to L6), the owner's picks and the open balance check. Its item part is in ItemsAndRelicsSystem (Counter Items). No rule changed. Its file is removed. |
| 2.2 | 2026-10-21 | implemented | Owner's 0.2.0 playtest ("gold and items are not needed"), levers L1, L2, L5 and L6 of the gold and items concept: item prices ×2.5 (Normal 40, Rare 75, Epic 150; ECON-BAL-001); puzzle gold −20 % (ECON-BAL-002); gold at the boss gate: strike a trial for 60 × floor number, lower the intensity for 40 × floor number (ECON-GATE-001/002); gold kept at the end scores (SCORE-GOLD-001, GameDesignSpec §9c). The late-floor mistake cost and the quarter heal (ECON-BAL-003/004) are in GameDesignSpec §5.1 and §7. |
| 2.1 | 2026-10-07 | implemented | Pencil purchase and item-reward reroll implemented and documented (SPEC-CONF-029, GAP-015/016); Nothing/Skip gold (SPEC-AMB-007); gold collected definition (SPEC-AMB-006, GAP-027); shop offer counts and full-bag rule (SPEC-CONF-005, GAP-036). |

## Overview

Gold is a run-scoped resource used in the Garden Run economy. No-progression modes bypass most or all of this loop.

Primary implementation references:

- `Assets/Scripts/Economy/GoldTable.cs`
- `Assets/Scripts/Economy/ShopService.cs`
- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/Economy/RelicService.cs`
- `Assets/Scripts/Run/CurseService.cs`

## Why and the levers (2.3)

**Why.** In the owner's 0.2.0 playtest (2026-10-20) the feel was "it feels like gold and items are not needed". The run
reached the Floor 2 boss with 212 gold and an item bag that was rarely touched. The baseline before 2.2 (the concept's
numbers):

| | Floor 1 | Floor 3 | Floor 5 |
|---|---:|---:|---:|
| Gold per puzzle (`base × (1 + 0.2 × stars)`) | 36–42 (6×6, 1–2★) | 63–104 (7×7 2★ to 8×8 3★) | 160–200 (9×9, 3–5★) |
| Puzzles per floor on a typical route | 5–6 | 5–6 | 5–6 |
| Gold per floor (puzzles only) | ≈ 250 | ≈ 500 | ≈ 1 000 |
| Item prices before 2.2 (`15 / 30 / 60 × (1 + 0.5 × floor)`) | 15 / 30 / 60 | 30 / 60 / 120 | 45 / 90 / 180 |
| Shop offers | 2 | 3 | 4 |

- **Income outran every sink.** One floor paid for the whole shop several times over. Pencil, the emergency heal and the
  rerolls were small next to that.
- **Nothing forced a purchase.** HP absorbed a floor's mistakes at Harmony 0–3 (1 HP each), the rest node healed a third,
  and Pencil refilled.
- **Items solved problems the player rarely had** (ItemsAndRelicsSystem, Counter Items).
- **Gold had no value at the end.** Unspent gold neither scored nor carried over (L5).

**Levers and their status.**

| # | Lever | Built as | Status |
|---|---|---|---|
| L1 | Prices against income: item prices ×2.5 (Normal 40, Rare 75, Epic 150, still × floor); puzzle gold −20 %. A shop visit should use most of a floor's gold. | ECON-BAL-001, ECON-BAL-002 | built 2026-10-21 |
| L2 | Gold at the boss gate: strike a trial (60 × floor) or lower the intensity one step (40 × floor). | ECON-GATE-001, ECON-GATE-002 | built 2026-10-21 |
| L3 | Counter items, one per modifier family; shops lean toward the floor's counters. | ITEM-COUNTER-001..007 (ItemsAndRelicsSystem 2.3) | built 2026-10-21 |
| L4 | Things worth saving for: +1 bag slot once per run (150 g); +1 max HP once per floor at the rest (80 g, instead of a bowl); a relic offer in every second shop (120 g+). | none | **not picked, not built** |
| L5 | Gold counts at the end: a point per 5 gold left, at most 15 %. | SCORE-GOLD-001 | built 2026-10-21 |
| L6 | Pressure items relieve: from Floor 4 a mistake costs at least 2 HP; the rest heals a quarter. | ECON-BAL-003, ECON-BAL-004 | built 2026-10-21 |

**Owner's picks (2026-10-20):** "L1 + L2 + L5 first, L3 as the content follow-up, and L6". The concept's reasoning: L1
makes gold scarce, L2 and L5 give it two clear uses (shape the boss, or bank it for the score), and none of them adds
content; L3 makes items exciting rather than just affordable; L6 only if runs are still too safe after L1. L2 has one
change from the concept: a struck trial also lowers the boss's picks by one (the gate offers one trial more than the boss
takes, so removing an offer alone would buy nothing).

Screens of the built levers: the gate with its purse and a struck trial, the Floor 4 entry, the market with the new
prices and the market with a counter item (`docs/design/layout-proposals/after_boss_gate.jpg`,
`after_boss_gate_struck.jpg`, `after_floor_entry_floor4.jpg`, `after_shop.jpg`, `after_shop_counters.jpg`).

**Open.**

- **L4** is not picked. It stays a lever with the numbers above.
- **Balance check** (waits for the next playtest). An EditMode balance test with seeded runs and scripted shopping
  reports the targets: average gold left at the end of a won run under 80 (today several hundred); at least 70 % of
  shop visits end with a purchase; at least 4 items used per run. The owner's playtests confirm the feel.

## Puzzle Gold

Base puzzle gold is calculated through `GoldTable.CalculatePuzzleGold(boardSize, stars)`.

Current formula:

```text
BaseGoldForBoardSize(boardSize) * (1.0 + stars * 0.2) * 0.8
```

The factor 0.8 (`GoldTable.PuzzleGoldFactor`, ECON-BAL-002) is lever L1 (see "Why and the levers"): one floor's
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

At the boss gate the purse is shown beside two once-per-gate actions (GoldEconomySystem lever L2):

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
