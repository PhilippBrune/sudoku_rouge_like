# Gold Economy System

**Version:** 1.1 | **Date:** 2026-03-23 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-03-23 | implemented | GlobalGoldMultiplier field added to RunState, all gold formulas verified correct |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Gold is the primary **run-scoped currency** in Run of the Nine. It is earned by solving puzzles, spent on pencil refills, item rerolls, and shop purchases, and reset to zero at the start of each new run. The Golden Root legendary relic is the only mechanism that carries gold value between stages.

---

## Gold Acquisition

### Base Gold per Board Size

| Board Size | Base Gold |
|:---:|:---:|
| 5×5 | 20 |
| 6×6 | 30 |
| 7×7 | 45 |
| 8×8 | 65 |
| 9×9 | 100 |

### Puzzle Completion Reward Formula

```
Gold = BaseDifficultyGold × (1 + stars × 0.2)
```

| Stars | Multiplier | 5×5 Gold | 9×9 Gold |
|:---:|:---:|:---:|:---:|
| 1★ | ×1.2 | 24 | 120 |
| 2★ | ×1.4 | 28 | 140 |
| 3★ | ×1.6 | 32 | 160 |
| 4★ | ×1.8 | 36 | 180 |
| 5★ | ×2.0 | 40 | 200 |
| 6★ | ×2.2 | 44 | 220 |

### Additional Gold Sources

| Source | Amount | Condition |
|--------|:---:|-----------|
| Koi Gambler passive | +1g | 25% chance on each correct placement |
| Monk Charm relic streak | +2g | Every 5 correct placements in a row |
| Copper Tortoise relic | +5g | Completing a puzzle with no mistakes |
| Golden Root relic | 50% carry | Gold carries 50% interest between stages |
| Number Freak L5 bonus | +10g | Starting gold per run (class level reward) |

### Global Gold Multiplier

`RunState.GlobalGoldMultiplier` (default 1.0) scales all gold gains. Currently no modifier changes this, but the architecture supports it for future event or relic effects.

---

## Gold Spending

### Pencil Purchase (During Level)

Buy additional pencil charges mid-puzzle. Cost escalates per purchase within the run:

```
PencilBuyCost = 20 + (20 × timesPurchasedThisRun)
```

| Purchase # | Cost |
|:---:|:---:|
| 1st | 20g |
| 2nd | 40g |
| 3rd | 60g |
| 4th | 80g |
| 5th | 100g |

The escalating cost prevents Pencil refill loops — each additional purchase becomes increasingly expensive, forcing the player to manage pencil marks carefully.

### Item Reroll (Post-Level)

Reroll unpicked, non-Nothing slots in the item reward phase:

```
RerollCost = 20 + (20 × timesRerolledThisRun)
```

Same escalation curve as pencil purchases. Reroll counter and pencil purchase counter are **separate**.

### Shop Purchases

Shop nodes on the garden path sell items and relics. Prices scale with floor progression:

```
ShopPrice = BasePrice × (1 + Floor_Index × 0.5)
```

#### Item Base Prices

| Rarity | Base Price | Floor 3 | Floor 5 |
|--------|:---:|:---:|:---:|
| Normal | 15g | 30g | 45g |
| Rare | 30g | 60g | 90g |
| Epic | 60g | 120g | 180g |

**Relics are NOT sold in shops.** Relics are acquired exclusively through Relic tile nodes, elite tile rewards, and run events. See [ItemsAndRelicsSystem](ItemsAndRelicsSystem_implemented.md) for relic acquisition details.

### Shop Inventory Size per Floor

| Floor | Item Slots | Description |
|:---:|:---:|-------------|
| 1 | 2 items | Small selection, mostly Normal rarity |
| 2 | 2 items | Normal + occasional Rare |
| 3 | 3 items | Mixed Normal/Rare |
| 4 | 3 items | Rare-weighted, Epic eligible (if class L15+) |
| 5 | 4 items | Premium selection, higher Epic chance |

#### Spirit Lantern Relic

The **Spirit Lantern** relic (Tier 4) reduces all shop prices by 20%.

#### Moss Token Relic

The **Moss Token** relic (Tier 1) reduces reroll costs by 25%.

---

## Gold Economy Per Floor (Expected Range)

Estimated gold income per floor based on typical path length (6–10 tiles) and average star rating:

| Floor | Avg Stars | Avg Board | Tiles | Expected Gold Income | Key Expenses |
|:---:|:---:|:---:|:---:|:---:|--------------|
| 1 | 1–2★ | 5×5–6×6 | 5–8 | 120–200g | 1 pencil buy, 1 reroll |
| 2 | 2–3★ | 6×6–7×7 | 6–9 | 200–350g | 1–2 pencil buys, shop visit |
| 3 | 3–4★ | 7×7–8×8 | 7–10 | 350–550g | Shop purchases, rerolls |
| 4 | 3–5★ | 8×8–9×9 | 7–10 | 500–800g | 2+ pencil buys, premium shop |
| 5 | 4–6★ | 9×9 | 8–10 | 700–1100g | Max expense floor |

### Economy Balance Targets

- **Floor 1:** Player should afford 1 pencil buy and 1 item reroll comfortably
- **Floor 3:** Player should be able to buy 1 shop item OR save for Floor 4–5 premium items
- **Floor 5:** Player should have enough for 1–2 premium purchases if they saved, or be broke if they spent aggressively
- **Golden Root holder:** Roughly 30–50% more total gold over a full run due to carry interest

---

## Gold at Run End

Gold is **not preserved** between runs. At the end-of-run screen, total gold earned is displayed as a stat but has no further gameplay effect.

Gold does contribute to the cumulative `ClassUnlockProgress.GoldCollected` counter for class unlock conditions (Lantern Seer requires 50,000 cumulative gold).

---

## Sell Mechanics

**Item sell: Disabled.** Items cannot be sold for gold. They are permanent inventory additions until consumed.

**Relic sell: Disabled.** Relics cannot be sold. The player holds one relic at a time and can only swap when offered a new one.

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Run/RunDirector.cs` | Gold reward calculation on puzzle completion |
| `Assets/Scripts/Economy/ShopService.cs` | Shop inventory, pricing, floor scaling |
| `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.CurrentGold`, `PencilPurchasesThisRun`, `RerollsThisRun`, `GlobalGoldMultiplier` |
| `Assets/Scripts/Save/ProfileService.cs` | Cumulative gold tracking for class unlocks |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | Gold display, purchase UI, reroll UI |

---

### Nothing-Slot Behaviour

Selecting a Nothing slot in the item reward phase grants **nothing** — no gold bonus, no reward. It simply ends the reward phase. This is a pure sacrifice mechanic, not a gold-conversion opportunity.

---

## Open Issues

No open issues remaining.


---

---

---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Code File | Verified Classes | Status |
|--------|-------|-----------|-----------------|--------|
| REQ-SHOP-025 | REQ-SHOP-016 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-026 | REQ-SHOP-017 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-027 | REQ-SHOP-018 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-028 | REQ-SHOP-019 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-029 | REQ-SHOP-020 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-030 | REQ-SHOP-021 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-031 | REQ-SHOP-022 | `Assets\Scripts\Save\ProfileService.cs` | ProfileService | ✓ verified |
| REQ-SHOP-032 | REQ-SHOP-023 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-SHOP-033 | REQ-SHOP-024 | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
