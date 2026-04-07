# Curse System

**Version:** 0.2 | **Date:** 2026-04-07 | **Status:** under review

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.2 | 2026-04-07 | under review | Design decisions resolved: Elite failure removed as source (HP=0 = run over). Stacking is unlimited. Rest node hosts removal. Expanded pool to 15 curses (floor-tiered). 4 new curse-bearing items added. Error-free puzzle gives 25% auto-clear chance. Curse panel visible on both screens (compact HUD style). |
| 0.1 | 2026-04-07 | new | Initial draft — open design questions flagged for review |

---

## Overview

Curses are **persistent negative effects** applied to the player's run. Unlike regular difficulty scaling, curses are *unexpected*, *named*, and *displayed to the player* — they feel like things that happened to you, not just stat adjustments.

Curses accumulate across a run and are shown in the **Curse Panel** in the HUD. They persist until cleared (see Removal section) or until the run ends.

This system is **separate** from the Cursed Node mechanic — that is an opt-in per-puzzle risk/reward choice with no persistent effect.

---

## How Curses Are Acquired

### 1. Run Event Choices
Some event options offer a high-value reward in exchange for accepting a curse:
- *"The Ink Peddler"* — "Trade your pencil reserves: –8 Pencil, +60 Gold, gain a Curse."
- *"The Tempting Shrine"* — "Pray deeply: +3 HP, +2 Item Slots (this puzzle only), gain a Curse."

Curse events always display the curse name before the player confirms, so it is an informed choice.

### 2. Curse-Bearing Items
Four unique items have a curse as their explicit cost. These items are powerful enough that the tradeoff is meaningful:

| Item | Effect | Curse Applied |
|------|--------|---------------|
| **Cracked Teacup** | During puzzle: reveal 3 random solution cells instantly | `hollow_pencil` |
| **Demon Mask** | Before a puzzle: skip it entirely (no XP or gold) | `phantom_pain` |
| **Forbidden Scroll** | During puzzle: show the full solution board for 4 seconds | `fog_of_memory` |
| **Blood Pact Sigil** | Immediately gain 60 gold | `price_gouge` |

These are unique items (not tiered) that can appear in item reward screens and shops. They are clearly labelled with the curse cost in the item tooltip.

### 3. Rest Node (when curses are active)
When the player has at least one active curse, the Rest node's 3-way choice becomes a **4-way choice** — the standard three options (Heal / +Pencil / +Reroll Token) are joined by a fourth option:
- **"Cleanse a Curse"** — Remove the oldest active curse. No other cost.

If no curses are active, the Rest node shows the standard 3 options only.

---

## Curse Pool — 15 Curses

Curses are divided into three severity tiers. Floor-based rolling restricts which tier is available based on current floor.

| Floor | Tiers Available |
|:---:|---|
| 1 | Mild only |
| 2 | Mild + Medium |
| 3–5 | All tiers |

### Mild (available from Floor 1)

| ID | Name | Effect |
|----|------|--------|
| `hollow_pencil` | **Hollow Pencil** | All pencilmarks cost **2 Pencil** instead of 1 |
| `fog_of_memory` | **Fog of Memory** | At puzzle start, the board size label and star rating are hidden |
| `misfortune` | **Misfortune** | Item reward screens have **−1 slot** (min 1 slot always shown) |
| `bad_luck` | **Bad Luck** | Nothing slot probability **+15%** on all reward screens |
| `price_gouge` | **Price Gouge** | All shop items cost **+35% more** (rounded up) |

### Medium (available from Floor 2)

| ID | Name | Effect |
|----|------|--------|
| `inkblot` | **Inkblot** | At puzzle start, **3 random cells** are fogged (revealed on correct entry, like Fog of War) |
| `crumbling_focus` | **Crumbling Focus** | Max Pencil reduced by **4** immediately on application |
| `sealed_eyes` | **Sealed Eyes** | The modifier description box is **hidden** during boss puzzles |
| `blurred_sight` | **Blurred Sight** | Placed pencilmarks **disappear** 1.5 seconds after entry (effect is still valid; player must remember) |
| `restless_inventory` | **Restless Inventory** | After each completed puzzle, one random item in the bag loses **1 charge** (if 0 charges, it is discarded) |

### Nasty (available from Floor 3+)

| ID | Name | Effect |
|----|------|--------|
| `phantom_pain` | **Phantom Pain** | Wrong placements cost **2 HP** instead of 1 |
| `weight_of_stone` | **Weight of Stone** | Max HP reduced by **2** (immediately applied, min 1) |
| `trembling_hand` | **Trembling Hand** | The **first placement** in each puzzle costs 1 HP regardless of correctness |
| `fraying_thread` | **Fraying Thread** | Lose **1 Max HP** at the start of each new floor (cumulative; min 1) |
| `double_or_nothing` | **Double or Nothing** | Each item reward slot has a **20% chance** to be re-rolled to Nothing, regardless of star rating |

---

## Stacking Rules

Curses are **unlimited** — there is no cap on how many can be active simultaneously. In practice, a 5-floor run naturally limits accumulation to roughly 5–8 curses across all events and items.

- **No duplicates:** A curse already active cannot be re-applied. Rolling a duplicate re-rolls until a different curse is selected.
- **No floor-carry restriction:** Curses persist across all 5 floors regardless of when they were applied.

---

## Curse Removal

| Method | Removes | Notes |
|--------|---------|-------|
| Rest Node "Cleanse a Curse" option | 1 curse (oldest) | Only shown when curses are active; replaces the 4th slot |
| Error-free puzzle completion | 1 curse (oldest) | **25% chance** after each puzzle completed with zero mistakes |
| Future "Purification" item | 1 curse | Targeted — player chooses which curse |
| Specific relic effect (TBD) | TBD | Passive or active trigger |

### Error-Free Auto-Clear Detail
After completing any puzzle (normal, elite, or boss) with **zero mistakes**:
- Roll a 25% chance
- On success: remove the oldest active curse
- Show a brief notification: *"Your focus dispels [Curse Name]"* (gold text, fades out in 2s)
- **GardenMonk** class passive increases this chance to **50%** (the monk's discipline is reflected here)

---

## Curse Panel UI

Visible on **both the path overview screen and during puzzle solving**.

### Layout
- Anchored to the **top-right of the HUD strip**, immediately right of the item/relic counters
- Compact default state: a small muted-red label `"Curses (N)"` — takes up ~80px width
- **Tap / click** to expand a dropdown listing all active curses:
  - Each entry: `[Name] — short description` on one line, red-tinted badge background
  - Scrollable if more than 4 active (rare)
- **During puzzle solving:** the label stays visible but does not obscure the board. Dropdown expands upward into the HUD area (not over the grid).
- If no curses active: label reads `"Curses (0)"` in muted grey, not clickable.

---

## Data Model Changes

### `RuntimeModels.cs` — `RunState`
```csharp
// New fields (JsonUtility-compatible)
public List<string> ActiveCurseIds = new List<string>(); // IDs of active curses
```

### `CurseService.cs` — replace stub
```csharp
public sealed class CurseService
{
    // Query
    public bool HasActiveCurse(RunState state);
    public List<CurseDefinition> GetActiveCurses(RunState state);
    public bool IsActive(RunState state, string curseId);

    // Mutation
    public void ApplyCurse(RunState state, string curseId);
    public bool TryRemoveCurse(RunState state, string curseId = null); // null = remove oldest
    public CurseDefinition RollCurse(int floorIndex, RunState state);  // floor-tiered, no duplicates

    // Called after each error-free puzzle completion
    public bool TryAutoCleanseOnPerfect(RunState state, ClassId classId, Random rng);
}
```

### `CurseDefinition` — new plain C# class
```csharp
public sealed class CurseDefinition
{
    public string Id;
    public string Name;
    public string Description;    // one line, shown in panel
    public CurseSeverity Severity; // Mild, Medium, Nasty
    public int MinFloor;           // 0, 1, or 2 (floor index, not number)
}

public enum CurseSeverity { Mild, Medium, Nasty }
```

---

## Integration Points

| Location | Change |
|----------|--------|
| `RunEventService.ResolveChoice()` | Call `CurseService.ApplyCurse()` for event options with a curse |
| `RunDirector.PlaceNumber()` | `phantom_pain` → 2× HP; `trembling_hand` → 1 HP on first placement per puzzle |
| `RunDirector.CompleteLevelAndGrantRewards()` | `misfortune` → −1 slot; `restless_inventory` → drain 1 charge from random item; call `TryAutoCleanseOnPerfect()` if zero mistakes |
| `RunDirector.AdvanceToNextFloor()` | `fraying_thread` → −1 MaxHP |
| `ShopService.BuildOffers()` | `price_gouge` → ×1.35 all prices |
| `SudokuGenerator / board setup` | `inkblot` → mark 3 random non-given cells as fogged |
| `ItemService.UseItem()` | 4 curse-bearing items → call `ApplyCurse()` with their mapped curse |
| `InRunController.BuildItemRewardSlots()` | `bad_luck` → +0.15 to Nothing roll; `double_or_nothing` → 20% re-roll to Nothing per slot |
| `RewardViewController` | `blurred_sight` — start coroutine to fade pencilmarks after 1.5s |
| `RunDirector.AcceptRestHeal/Pencil/Reroll()` | Expose `AcceptRestCurseRemoval()` when curses active |
| `CursePanelController` | Replace stub: render `GetActiveCurses()`, expandable dropdown, compact HUD label |
| `RunAutoSaveCoordinator.Save()` | `ActiveCurseIds` already serialized via `RunState` — no extra change |
