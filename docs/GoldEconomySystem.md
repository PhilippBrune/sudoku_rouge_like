# Gold Economy System

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

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
BaseGoldForBoardSize(boardSize) * (1.0 + stars * 0.2)
```

Board-size base values:

| Size | Base Gold |
|---|---:|
| 5 | 20 |
| 6 | 30 |
| 7 | 45 |
| 8 | 65 |
| 9 | 100 |

Runtime modifiers can then adjust the result through:

- cursed board multipliers
- positive floor effects
- relic effects

## Spending

### Shop Purchases

Shops sell items only. Offer prices are built with:

```text
BasePrice * (1 + floorIndex * 0.5) * priceMultiplier * harmonySurcharge
```

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

## Mode Boundaries

Gold progression is skipped in no-progression modes because their runs use `DisableProgressionRewards = true`.

That applies to:

- Endless Zen
- Spirit Trials
- tutorial runs
- seasonal challenge reward flow

## Notes On Drift

Older design text referred to different reroll/heal curves and a broader shop economy. This spec reflects the current runtime code path in `RunDirector` and `ShopService`.
