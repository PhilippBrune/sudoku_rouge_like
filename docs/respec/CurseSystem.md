# Curse System

**Version:** 0.1 | **Date:** 2026-04-07 | **Status:** new — awaiting review

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-04-07 | new | Initial draft — open design questions flagged for review |

---

## Overview

Curses are **persistent negative effects** applied to the player's run. Unlike regular difficulty scaling, curses are *unexpected*, *named*, and *displayed to the player* — they feel like things that happened to you, not just stat adjustments.

Curses accumulate across a run and are shown in the **Curse Panel** (accessible from the HUD). They persist until explicitly removed (see Removal section) or until the run ends.

This system is **separate** from the Cursed Node mechanic, which is an opt-in per-puzzle risk/reward choice with no persistent effect.

---

## How Curses Are Acquired

Curses are applied through the following sources:

### 1. Run Event Choices
Some event options offer a high-value reward in exchange for accepting a curse. Example:
- Event: "The Ink Peddler" — "Trade your pencil reserves for gold: –8 Pencil, +60 Gold, **gain a Curse**."

### 2. Elite Puzzle Failure
If the player fails an Elite tile (HP reaches 0 during the puzzle but is saved by Phoenix Feather, or a specific class passive triggers), a curse is applied as a consequence.
> ⚠️ **Open Question 1:** Should failing an Elite always apply a curse, or only in specific conditions (e.g. below a certain HP threshold)?

### 3. Specific Item Side Effects
Certain powerful items could have a curse as a cost. Example: "Cracked Teacup — use this puzzle: +40 Gold, gain **Hollow Luck** curse."
> ⚠️ **Open Question 2:** Which items (if any) should have curses as their downside? Should this be a small number of unique items, or a general mechanic?

### 4. Rest Node (Future Integration)
If the Curse Panel has any active curses, one of the three Rest node options could be replaced by **"Remove a Curse"** — giving the player a meaningful tradeoff between healing/pencil and curse removal.
> ⚠️ **Open Question 3:** Should curse removal always be on the Rest node (replacing one of the existing 3 choices), or should it be its own dedicated node type?

---

## Curse Pool (Draft)

Eight draft curses — names and effects are subject to change.

| ID | Name | Effect |
|----|------|--------|
| `hollow_pencil` | **Hollow Pencil** | All pencilmarks cost **2 Pencil** instead of 1 |
| `phantom_pain` | **Phantom Pain** | Wrong placements cost **2 HP** instead of 1 |
| `inkblot` | **Inkblot** | At the start of each puzzle, **3 random cells** are fogged (revealed on correct entry) |
| `weight_of_stone` | **Weight of Stone** | Max HP is reduced by **2** (min 1). Applied immediately. |
| `price_gouge` | **Price Gouge** | All shop items cost **+35% more** (rounded up) |
| `misfortune` | **Misfortune** | Item reward screens have **−1 slot** (min 1 slot always shown) |
| `trembling_hand` | **Trembling Hand** | The **first placement** in each puzzle always costs 1 HP, regardless of correctness |
| `fog_of_memory` | **Fog of Memory** | At puzzle start, the board size label and star rating are **hidden** |

> ⚠️ **Open Question 4:** Is 8 curses the right pool size? Should curses scale or change per floor (e.g. floor 4+ only rolls the nastier curses)?

---

## Stacking Rules

> ⚠️ **Open Question 5 (Critical):** How many curses can the player hold simultaneously?
>
> **Option A — Cap at 3:** Simple, prevents snowball, but players may ignore curses after the cap.
> **Option B — Unlimited:** More dramatic, can create very difficult late runs, risk of run-bricking.
> **Option C — Cap at 1:** Simple messaging. The game always has exactly 0 or 1 curse at a time. Easy to design around.
>
> **Current recommendation:** Cap at **2** as a middle ground — enough for combinatorial pressure, not enough to brick the run early.

- Duplicate curses do not stack — if a curse is already active, re-rolling the same curse re-rolls until a different one is selected.
- If all non-active curses have been applied, the oldest curse is extended/refreshed (effect remains, no new one added).

---

## Curse Removal

| Method | Removes | Cost |
|--------|---------|------|
| Rest Node (3rd option swap) | 1 curse | Replaces Heal / Pencil / Reroll choice |
| Future "Purification" item | 1 curse | Item consumed |
| Specific relic effect (TBD) | 1 curse | Passive or active trigger |
| Floor transition | None | Curses carry across all 5 floors |

> ⚠️ **Open Question 6:** Should any class passives grant curse immunity or automatic removal? (E.g. Garden Monk — every 5 correct placements has a small chance to shed a curse.)

---

## Curse Panel UI

The Curse Panel is already stubbed in `Assets/Scripts/UI/CursePanelController.cs`.

### Proposed layout:
- Anchored at the top-right of the HUD strip
- Shows a compact label: `"Curses (N)"` in a muted red
- Tapping/clicking opens a small dropdown listing active curses with their names and short descriptions
- Each active curse shown as a red badge: `[Hollow Pencil] All pencilmarks cost 2`
- Panel visible on both the path overview and the puzzle screen

> ⚠️ **Open Question 7:** Should the Curse Panel be visible during puzzle solving, or only on the path screen? Showing it during puzzles could cause clutter.

---

## Data Model Changes Required

### `RunState` (in `RuntimeModels.cs`)
```csharp
// New fields
public bool HasActiveCurse;
public List<string> ActiveCurseIds; // serializable list of curse ID strings
```

### `CurseService.cs` (currently a stub)
```csharp
public bool HasActiveCurse(RunState state);           // at least one curse active
public List<CurseDefinition> GetActiveCurses(RunState state);
public void ApplyCurse(RunState state, string curseId);
public bool TryRemoveCurse(RunState state, string curseId = null); // null = remove oldest
public CurseDefinition RollCurse(int floorIndex, RunState state); // excludes already-active
```

### `CurseDefinition` (new plain C# class)
```csharp
public class CurseDefinition
{
    public string Id;
    public string Name;
    public string Description;   // short, shown in panel
    public int MinFloor;         // earliest floor this curse can appear (default 0)
}
```

---

## Integration Points

| Location | Integration |
|----------|-------------|
| `RunEventService.ResolveChoice()` | Call `CurseService.ApplyCurse()` for event options that include a curse |
| `RunDirector.CompleteLevelAndGrantRewards()` | Check `hollow_pencil` to multiply pencil cost; check `misfortune` to subtract slot |
| `RunDirector.PlaceNumber()` | Check `phantom_pain` for 2× HP cost; check `trembling_hand` for first-move HP cost |
| `ShopService.BuildOffers()` | Check `price_gouge` for +35% price multiplier |
| `SudokuGenerator.CreatePuzzle()` (or board setup) | Check `inkblot` to mark 3 random cells as fogged |
| `RunDirector.AcceptRestHeal/Pencil/Reroll` | If curses active, expose `AcceptRestCurseRemoval()` option |
| `CursePanelController` | Replace stub with real list rendering from `CurseService.GetActiveCurses()` |

---

## Open Questions Summary

| # | Question | Needed Before |
|---|----------|---------------|
| 1 | Does Elite failure always curse the player? | Implementation |
| 2 | Which items have curses as their cost? | Implementation |
| 3 | Does curse removal go on Rest node or a dedicated node? | Implementation |
| 4 | Pool size — 8 curses enough? Floor-based restrictions? | Implementation |
| 5 | Max active curses at once — cap at 1, 2, 3, or unlimited? | **Critical — needed first** |
| 6 | Class passive interactions with curses? | Implementation |
| 7 | Curse panel visible during puzzle solving? | Implementation |

**Recommendation:** Decide question 5 first, then 1 and 3, then the rest.
