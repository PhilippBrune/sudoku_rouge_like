# Class XP & Level Progression System

**Version:** 1.1 | **Date:** 2026-04-15 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.2 | 2026-04-15 | implemented | Added requirement IDs (XP-TILE-*, XP-CURVE-*, XP-LOG-*, XP-COMMIT-*, META-XP-*, META-PRESTIGE-*) |
| 1.1 | 2026-03-23 | implemented | Prestige boss-gate check added, ProfileService.RecordRunAndGetNewUnlocks orchestration implemented, class unlock thresholds corrected to match spec |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Each playable class has its own independent XP counter and level, ranging from **Level 1 to Level 40**. XP is **persistent** — it carries over between runs and is never lost. Leveling up a class unlocks class-specific stat upgrades and perks for that class only. Players are encouraged to specialise by investing runs into a chosen class.

The XP curve is designed so that early levels are reached quickly (the "hook" period), while later levels require significantly more effort (the "plateau"), making Level 40 a genuine mark of mastery. The target is approximately **80 dedicated runs** to reach Level 40 from scratch.

---

## Class Archetypes & Unlock Conditions

There are **8 playable classes**. Each has a distinct gameplay identity and a specific unlock condition. The Number Freak is always available; all others must be unlocked through cumulative play.

| # | Class | Archetype | Unlock Condition |
|---|-------|-----------|-----------------|
| 1 | Number Freak | Balanced | Default — always unlocked |
| 2 | Garden Monk | Sustain | Defeat 2 bosses (cumulative, any class) |
| 3 | Shrine Archivist | Information | Use 15 items (cumulative, any class) |
| 4 | Koi Gambler | Volatility | Collect 10 relics (cumulative, any class) |
| 5 | Stone Gardener | Item Synergy | Defeat 10 bosses (cumulative, any class) |
| 6 | Lantern Seer | Boss Specialist | Collect 50,000 gold (cumulative, any class) |
| 7 | Reed Duelist | Tempo | Find every unique item at least once |
| 8 | Quiet Cartographer | Route Control | Clear an entire stage with zero pencil use and zero HP lost |

### Unlock Progression Order

The natural unlock order follows roughly: Number Freak → Garden Monk → Shrine Archivist → Koi Gambler → Stone Gardener → Lantern Seer → Reed Duelist / Quiet Cartographer (advanced variants, order may vary by player).

All unlock conditions are tracked globally across all classes and all runs. Unlocking a class does not reset its XP — a newly unlocked class starts at Level 1 with 0 XP.

---

## XP Curve

### Phase 1 — Linear Growth (Levels 1 → 15) `[XP-CURVE-001]`

Levels feel fast and rewarding. Each level costs a fixed amount more than the last, giving the player a sense of constant progress during their first sessions with a class.

```
XP_to_next(n) = 50 + (n − 1) × 10
```

| Level | XP to Next Level | Cumulative XP to Reach This Level |
|-------|:---:|:---:|
| 1  | 50  | 0     |
| 2  | 60  | 50    |
| 3  | 70  | 110   |
| 4  | 80  | 180   |
| 5  | 90  | 260   |
| 6  | 100 | 350   |
| 7  | 110 | 450   |
| 8  | 120 | 560   |
| 9  | 130 | 680   |
| 10 | 140 | 810   |
| 11 | 150 | 950   |
| 12 | 160 | 1,100 |
| 13 | 170 | 1,260 |
| 14 | 180 | 1,430 |
| 15 | 190 | 1,610 |

Total XP to reach Level 15: **1,610 XP**

### Phase 2 — Accelerated Growth (Levels 16 → 40) `[XP-CURVE-001]`

The pivot at Level 15: each subsequent level costs **35 XP more** than the previous one. The gap between levels widens progressively, creating the plateau effect. These levels reward dedicated play over many runs.

```
XP_to_next(n) = XP_to_next(n − 1) + 35      [for n ≥ 16]
```

| Level | XP to Next Level | Cumulative XP to Reach This Level |
|-------|:---:|:---:|
| 16 | 225  | 1,800  |
| 17 | 260  | 2,025  |
| 18 | 295  | 2,285  |
| 19 | 330  | 2,580  |
| 20 | 365  | 2,910  |
| 21 | 400  | 3,275  |
| 22 | 435  | 3,675  |
| 23 | 470  | 4,110  |
| 24 | 505  | 4,580  |
| 25 | 540  | 5,085  |
| 26 | 575  | 5,625  |
| 27 | 610  | 6,200  |
| 28 | 645  | 6,810  |
| 29 | 680  | 7,455  |
| 30 | 715  | 8,135  |
| 31 | 750  | 8,850  |
| 32 | 785  | 9,600  |
| 33 | 820  | 10,385 |
| 34 | 855  | 11,205 |
| 35 | 890  | 12,060 |
| 36 | 925  | 12,950 |
| 37 | 960  | 13,875 |
| 38 | 995  | 14,835 |
| 39 | 1,030 | 15,830 |
| 40 | MAX  | 16,860 |

Total XP to reach Level 40: **16,860 XP**

### Progression Phases Summary

| Phase | Levels | Flavour | Key feel |
|-------|--------|---------|----------|
| Hook | 1–15 | Fast, steady gains | "I level up every few runs" |
| Pivot | 15–20 | Noticeable slowdown begins | "Each level feels earned" |
| Plateau | 20–30 | Clear effort required | "I need to push harder" |
| Mastery | 30–40 | Long-term commitment | "Level 40 is a real achievement" |

---

## XP Persistence Model `[META-XP-001]` `[XP-CURVE-002]`

XP is stored as a single **total accumulated value** per class. There is no "current level XP" stored separately. Level is always derived at runtime by consuming the XP table.

### Derivation Algorithm

```
function DeriveLevel(totalXp):
    level = 1
    remaining = totalXp
    while level < 40 and remaining >= XP_to_next(level):
        remaining -= XP_to_next(level)
        level += 1
    return (level, remaining)    // remaining = progress into current level
```

### Adding XP from a Run

```
class.TotalXp += xpEarned
(class.Level, class.LevelProgress) = DeriveLevel(class.TotalXp)
```

**Example:** Level 1 character earns 100 XP.
- Level 1 → 2 costs 50 XP. Consumed. Level becomes 2.
- Level 2 → 3 costs 60 XP. Remaining = 50. Not enough to level again.
- Result: **Level 2, 50 / 60 XP** into the next level.

XP is never lost. XP earned on a run where the player dies is still applied.

---

## XP Earned per Run `[XP-TILE-001]`

XP is awarded at the end of each run (regardless of outcome — victory or game over). The formula rewards difficulty, clean play, and modifier engagement.

### Formula

```
RunXP = BaseXP × StarMultiplier + ModifierBonus + BossBonus + PerfectBonus
```

All intermediate values are floored to integers. Minimum total is **1 XP**.

### Base XP (per Puzzle Solved) `[XP-TILE-002]`

Summed across all tiles the player completed in the run:

| Board Size | Base XP Puzzel size |
|-----------|:---:|
| 5×5 | 30  |
| 6×6 | 45  |
| 7×7 | 60  |
| 8×8 | 90  |
| 9×9 | 120 |

### Star Multiplier (per Tile) `[XP-TILE-003]`

| Star Rating | Multiplier |
|------------|:---:|
| 1★ | ×1.0 |
| 2★ | ×1.2 |
| 3★ | ×1.5 |
| 4★ | ×1.5 |
| 5★ | ×2.0 |
| 6★ | ×2.0 |

### Modifier Bonus (per Boss Puzzle) `[XP-TILE-004]`

Applied once per boss tile:

```
ModifierBonus = activeMods × 15 XP
```

### Boss Tile Bonus `[XP-TILE-004]`

Boss puzzles award **×1.5** of their calculated tile XP.

### Perfect Bonus (per Tile) `[XP-TILE-004]`

Completing a puzzle with zero mistakes: **+25 XP** flat.

### XP Target Validation

With the target of **~80 runs to reach Level 40** (16,860 XP total), the average XP per run must be approximately **211 XP**. A typical mid-game run (mix of 6×6 to 8×8 tiles, 2–4★ average, occasional perfect clears, one boss with 1–3 modifiers) is expected to yield 180–250 XP, which aligns with the 80-run target. These values must be validated during playtesting and may need iteration.

### Example Run (Floor 1, Calm Route)

| Tile | Board | Stars | Base | Star Mult | Mods | Perfect | Subtotal |
|------|-------|-------|------|-----------|------|---------|----------|
| Puzzle | 6×6 | 2★ | 45 | ×1.2 | — | +25 | 79 |
| Rest | — | — | 0 | — | — | — | 0 |
| Puzzle | 7×7 | 3★ | 60 | ×1.5 | — | — | 90 |
| Elite | 8×8 | 4★ | 90 | ×1.5 | — | +25 | 160 |
| Boss Gate | 9×9 | 4★ | 120 | ×1.5 | 1 mod | — | ×1.5 = 195 + 15 mod = 210 |

**Total: 79 + 90 + 160 + 210 = 539 XP**

---

## End-of-Run XP Breakdown Screen `[XP-LOG-001]`

After every run, a **XP Breakdown panel** is displayed on the **game over screen** (on death) or **game completion screen** (on victory). This is the **only place** where XP gain and level-ups are shown — level-ups are never displayed mid-run or on the main menu.

The panel shows:

1. **Class and current level** before this run.
2. **Tile-by-tile XP listing** — each completed puzzle tile with its earned XP.
3. **Bonus lines:**
   - Modifier bonuses
   - Perfect tile bonuses
4. **Total XP earned this run** (highlighted).
5. **Level progress bar animation** — a bar fills smoothly from the pre-run XP position, animating through any level-ups earned. Each level-up triggers a distinct visual tick showing the new level number. The filling animation must feel smooth and satisfying even after a loss. **Pressing Space skips the animation** and jumps directly to the final state.
6. **Final level and XP into current level** after all awards are applied.

The screen must clearly communicate why the player earned the amount they did and make level-ups feel rewarding.

### Save & Quit Behaviour `[XP-COMMIT-001]` `[META-XP-004]`

- XP is only committed at run end (game over or completion). There is no mid-run XP award.
- The player can **Save & Quit** from the Garden Overview screen at any time. This saves run progress but does not award XP.
- On resuming a saved run, the class XP does not change until the run concludes.
- If the player exits mid-run via Save & Quit, no partial XP is shown anywhere.

---

## UI Placement

### Class Select Screen

Each class card shows:
- **Class name**
- **Current level** (e.g. "Level 12")
- **XP text** — displayed as `X / Y XP` showing progress within the current level (e.g. "45 / 170 XP"). **No progress bar** is shown on Class Select.
- **Next unlock preview** — one-line text showing what the next level-up grants (e.g. "Next: +5 Max HP at Level 13")

### Meta Progression Screen

A dedicated **Class Levels** section shows:
- All 8 classes in a scrollable list.
- Each row: class icon, name, level badge, **XP text** displayed as `X / Y XP` (same format as Class Select — **no progress bar**).
- Clicking/selecting a class expands a **Unlocks Timeline** — a vertical list of all levels with their granted upgrades, ticked off for completed levels and shown as locked for future ones.
- Overall "Most Progressed Class" summary at the top.

### Game Over / Game Completion Screen

The **XP progress bar** is shown **only** on this screen, as part of the End-of-Run XP Breakdown panel (see above). This is the sole place where an animated XP bar appears.

---

## Class Base Stats & Level Unlock Schedules

Each class has unique base stats and a full unlock schedule from Level 2 to Level 40. Unlocks trigger on level-up at the end of a run (shown on the game over / completion screen). Unlocks apply to the **next run** started after they are earned — they do not apply retroactively to the current run.

Unlock types include: base stat boosts (+HP, +Pencil, +Item Slot, +Reroll Token), starting gold, starting items, passive upgrades, and cosmetics.

---

### 1. Number Freak (Balanced)

**Base Stats:** HP 10 / Pencil 10 / Item Slots 2 / Reroll Tokens 1

| Level | Unlock |
|:---:|--------|
| 3  | +1 Pencil |
| 5  | +10 Starting Gold |
| 7  | +2 HP |
| 10 | +2 Pencil, +2 HP |
| 13 | +10 Starting Gold |
| 15 | +1 Reroll Token |
| 18 | +1 Pencil |
| 20 | +1 Item Slot |
| 25 | +2 HP, +2 Pencil |
| 30 | Start run with 1 Rare Solver |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Golden Number frame |

---

### 2. Garden Monk (Sustain)

**Base Stats:** HP 14 / Pencil 5 / Item Slots 1
**Passive:** Every 5 correct placements, +1 HP
**Restriction:** Cannot buy Pencil mid-level

| Level | Unlock |
|:---:|--------|
| 3  | +1 Pencil |
| 5  | +2 HP |
| 7  | +10 Starting Gold |
| 10 | +1 Item Slot |
| 13 | +2 HP |
| 15 | Passive upgrade: every 4 correct placements → +1 HP (was 5) |
| 18 | +1 Pencil |
| 20 | +2 HP, +1 Reroll Token |
| 25 | +1 Pencil |
| 30 | Start run with 1 Meditation Stone |
| 35 | +2 HP |
| 40 | Cosmetic: Blooming Monk frame |

---

### 3. Shrine Archivist (Information)

**Base Stats:** HP 8 / Pencil 15 / Item Slots 2
**Passive:** First pencil use in each cell is free (does not consume a charge)

| Level | Unlock |
|:---:|--------|
| 3  | +2 Pencil |
| 5  | +1 HP |
| 7  | +10 Starting Gold |
| 10 | +1 Reroll Token |
| 13 | +2 Pencil |
| 15 | +1 HP, +1 Item Slot |
| 18 | +10 Starting Gold |
| 20 | Passive upgrade: first 2 pencil uses per cell are free (was 1) |
| 25 | +2 Pencil, +1 HP |
| 30 | Start run with 1 Pattern Scroll |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Scroll Archive frame |

---

### 4. Koi Gambler (Volatility)

**Base Stats:** HP 9 / Pencil 8 / Item Slots 2
**Passive:** 25% chance a wrong placement costs no HP; 25% chance a correct placement grants +1 Gold

| Level | Unlock |
|:---:|--------|
| 3  | +1 HP |
| 5  | +15 Starting Gold |
| 7  | +1 Pencil |
| 10 | Passive upgrade: correct placement gold chance → 30% (was 25%) |
| 13 | +1 HP |
| 15 | +1 Item Slot |
| 18 | +15 Starting Gold |
| 20 | Passive upgrade: wrong placement no-HP-loss chance → 30% (was 25%) |
| 25 | +2 Pencil, +1 HP |
| 30 | Start run with 1 Lucky Charm |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Golden Koi frame |

---

### 5. Stone Gardener (Item Synergy)

**Base Stats:** HP 11 / Pencil 8 / Item Slots 3
**Passive:** First item used each level is not consumed

| Level | Unlock |
|:---:|--------|
| 3  | +1 Pencil |
| 5  | +1 HP |
| 7  | +1 Item Slot |
| 10 | +10 Starting Gold |
| 13 | +2 Pencil |
| 15 | +1 HP, +1 Reroll Token |
| 18 | +1 Item Slot |
| 20 | Passive upgrade: first 2 items used each level are not consumed (was 1) |
| 25 | +2 HP |
| 30 | Start run with 1 Harmony Charm |
| 35 | +1 Pencil, +1 HP |
| 40 | Cosmetic: Mossy Stone frame |

---

### 6. Lantern Seer (Boss Specialist)

**Base Stats:** HP 7 / Pencil 12 / Item Slots 2
**Passive:** Boss modifiers are 20% weaker (intensity reduction)

| Level | Unlock |
|:---:|--------|
| 3  | +1 Pencil |
| 5  | +1 HP |
| 7  | +10 Starting Gold |
| 10 | Passive upgrade: boss modifiers 25% weaker (was 20%) |
| 13 | +2 Pencil |
| 15 | +1 HP, +1 Reroll Token |
| 18 | +1 Item Slot |
| 20 | +2 HP |
| 25 | Passive upgrade: boss modifiers 30% weaker (was 25%) |
| 30 | Start run with 1 Lantern of Clarity |
| 35 | +2 Pencil, +1 HP |
| 40 | Cosmetic: Spirit Lantern frame |

---

### 7. Reed Duelist (Tempo)

**Base Stats:** HP 8 / Pencil 7 / Item Slots 2 / Reroll Tokens 1
**Passive:** Completing a puzzle with no mistakes and no pencil use grants +2 Pencil charges for the run

| Level | Unlock |
|:---:|--------|
| 3  | +1 Pencil |
| 5  | +1 HP |
| 7  | +1 Reroll Token |
| 10 | +10 Starting Gold |
| 13 | +2 Pencil |
| 15 | Passive upgrade: perfect no-pencil bonus → +3 Pencil (was +2) |
| 18 | +1 HP |
| 20 | +1 Item Slot |
| 25 | +2 HP, +1 Pencil |
| 30 | Start run with 1 Tea of Focus |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Cutting Reed frame |

---

### 8. Quiet Cartographer (Route Control)

**Base Stats:** HP 9 / Pencil 10 / Item Slots 2 / Reroll Tokens 1
**Passive:** After completing a tile with no mistakes, preview the board size and star rating of unrevealed adjacent tiles

| Level | Unlock |
|:---:|--------|
| 3  | +1 Pencil |
| 5  | +10 Starting Gold |
| 7  | +1 HP |
| 10 | +1 Reroll Token |
| 13 | +1 Pencil, +1 HP |
| 15 | Passive upgrade: preview is always active (no longer requires perfect tile) |
| 18 | +10 Starting Gold |
| 20 | +1 Item Slot |
| 25 | +2 Pencil, +1 HP |
| 30 | Start run with 1 Compass of Order |
| 35 | +2 HP |
| 40 | Cosmetic: Ink Map frame |

---

## Prestige System `[META-PRESTIGE-001]`

After reaching Level 40, a class can enter a **prestige cycle** that resets its level while granting permanent bonuses.

### Prestige Rules

Prestige can trigger only when **all** of the following are true:

- Current class level is **40**
- Boss archive count meets the tier gate: `ArchiveBossesDefeated >= (PrestigeTier + 1) × 3`
- Current prestige tier is below the cap (**9**)

### On Prestige

- Prestige tier **+1**
- Class level reset to **1**
- Current XP reset to **0**
- Passive tier **+1** (permanent bonus carried across prestige cycles)

### Prestige Cap

Maximum prestige tier is **9**. After tier 9, no further prestige is possible. The class remains at Level 40 permanently.

### Excess XP

XP earned beyond Level 40 (before prestige is triggered) is **discarded**. Players should be prompted to prestige when eligible rather than accumulating wasted XP.

### Archive Tracking

Persistent archive counters are maintained for prestige gating and progression UI:

- Total runs
- Seeds bloomed (victories)
- Bosses defeated
- Perfect runs
- Total XP earned

Per-class archive entries store:

- Class id
- Level
- Current XP
- Prestige tier
- Total XP earned (lifetime, across all prestige cycles)

---

## Implementation Reference

### Data Model (`RuntimeModels / SaveFileService`)

```
class ClassProgressionEntry {
    ClassId   Id
    int       TotalXp          // single persistent value, never decremented
    int       PrestigeTier     // 0–9
    // Level and LevelProgress are derived, not stored
}
```

### Key Methods (`XpService / ClassGardenProgressionService`)

| Method | Responsibility |
|--------|---------------|
| `XpService.CalculateRunXp(tiles)` | Returns `XpBreakdown` with per-tile detail and total |
| `ClassGardenProgressionService.ApplyRun(classId, xpBreakdown)` | Adds XP to persistent total; fires level-up events |
| `ClassGardenProgressionService.DeriveLevel(classId)` | Returns `(level, progressXp, xpToNext)` |
| `ClassGardenProgressionService.XpToNext(level)` | Returns XP needed for one level; uses two-phase formula |
| `ClassGardenProgressionService.TryPrestige(classId)` | Checks prestige conditions; resets level if met |

### Save Behaviour

- `TotalXp` and `PrestigeTier` are saved to the profile save file after each run completes.
- No mid-run save of XP (XP is only committed at run end).
- On resume from a saved run, the class XP does not change until the run concludes.

---

## Open Issues
### OPEN-B — XP Formula Tuning
**Status:** Needs playtesting
**Description:** The base XP values (30 / 45 / 60 / 90 / 120 per board size), star multipliers, and bonus values are initial estimates targeting ~80 runs to Level 40. They have not been validated against actual run lengths or the level curve. These values must be tuned during playtesting and may need iteration.
**Action needed:** Playtest pass with XP logging before locking formula.
