# Class XP & Level Progression System

**Version:** 1.7 | **Date:** 2026-10-22 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.7 | 2026-10-22 | implemented | Owner sign-off of the class balance values (SPEC-CONF-008): the base stats, passives, unlock conditions and level schedules in this document stand as documented. No value changed. |
| 1.6 | 2026-10-21 | implemented | GAP-063 fixed (owner request 2026-10-21: "analyze, plan and fix"): the Level 30 starting item is given at run start (META-XP-007) and the Level 40 frame is shown (META-XP-008, kept after prestige). Three Level 30 items that did not exist are replaced by existing items that fit the class: Koi Gambler **Golden Koi** (was "Lucky Charm"), Stone Gardener **Rice Paper Umbrella** (was "Harmony Charm"), Reed Duelist **Temple Incense** (was "Tea of Focus"). §UI Placement describes the garden of classes (SPEC-CONF-041 decided: the XP stroke and the totals, no "Most Progressed Class" line). |
| 1.5 | 2026-10-21 | implemented | Schedule texts follow the code (SPEC-CONF-008: the code is the source): Koi Gambler L10 "correct gold chance 35%", L20 "no-HP-loss chance 35%"; Lantern Seer L10 "modifiers 35% weaker", L25 "40% weaker" (`ClassPassiveService`). The L30 starting items and L40 frames are listed but not granted (GAP-063). The Meta Progression screen's XP stroke (UI-STYLE-019) differs from "UI Placement" below: SPEC-CONF-041. |
| 1.4 | 2026-10-20 | implemented | Owner decision (SPEC-CONF-039): the prestige reward is the badge plus **+1 starting gold per prestige tier** for runs with that class (at most +9); "Passive tier +1" is dropped. |
| 1.3 | 2026-10-07 | implemented | 1.1 review decisions (2026-10-07): class roster, passives, unlock conditions and level schedules regenerated from ClassCatalog, the decided source (SPEC-CONF-008, GAP-023); "base (+bonus)" display; Number Freak L3 "+1 Pencil" fix (D07); XP above Level 40 discarded (SPEC-CONF-034); prestige button with confirmation in Meta Progression (GAP-017); "Passive tier +1" on prestige marked undecided (new SPEC-CONF-039); header now matches the newest history row (SPEC-CONF-023). |
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
| 8 | Quiet Cartographer | Route Control | Complete a full run (all 5 floors + boss) with zero mistakes (code; SPEC-CONF-008) |

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

The garden of classes (UI-STYLE-019, owner pick 2026-10-20; SPEC-CONF-041 decided 2026-10-21):
- The 8 classes as plaques: level and prestige stars, or the unlock goal with its progress for a locked class; the Level 40 frame round a class that has reached it.
- The chosen class on a scroll: level, runs and best floor, an **XP stroke** with `X / Y XP` and "to Level N" (for a locked class the stroke shows the way to its unlock), the passive, and the **Unlocks Timeline** — every milestone level with its rewards, ticked off for completed levels — then prestige.
- The collection's totals along the bottom (classes, relics, codex, highest Harmony, completion) instead of a "Most Progressed Class" line.

### Game Over / Game Completion Screen

The **animated XP progress bar** is shown **only** on this screen, as part of the End-of-Run XP Breakdown panel (see above). The Meta Progression screen shows a still XP stroke (SPEC-CONF-041).

---

## Class Base Stats & Level Unlock Schedules

Generated from `Assets/Scripts/Classes/ClassCatalog.cs` (2026-10-07). The code is the source of truth for base stats, passives, unlock conditions and level schedules (decided 2026-10-07, SPEC-CONF-008); GameDesignSpec §6 links here. `ClassRosterTests` pins the base stats and checks that every stat bonus in a schedule is applied at run start.

Unlocks trigger on level-up at the end of a run and apply to the **next run**. Stat bonuses (`+N HP`, `+N Pencil`, `+N Item Slot`, `+N Reroll Token`, `+N Starting Gold`, `+N Starting Relic`) are summed by `RunArchetypeService.GetLevelBonuses`; the class screen and the start-run confirmation show them as "base (+bonus)" (GAP-023). Classes start with 0 reroll tokens unless a level grants them.

Fix in 1.1: Number Freak's L3 entry read "+ 1 Pencil" and was never applied; Number Freak now starts L10 runs with HP 14 / Pencil 13 (review diagnostic D07).

| Class | HP | Pencil | Item slots | Starting relics | Passive | Unlock condition |
|-------|:--:|:------:|:----------:|:---------------:|---------|------------------|
| Number Freak | 10 | 10 | 2 | 1 | After completing a puzzle with zero mistakes: +1 Reroll Token. | Default (always unlocked) |
| Garden Monk | 14 | 5 | 1 | 1 | Every 5 correct placements: +1 HP. Level 15+: every 4. | Defeat 2 bosses |
| Shrine Archivist | 8 | 15 | 2 | 1 | Each pencil mark placed has a 25% chance to auto-reveal the correct digit for that cell. | Use 15 items |
| Koi Gambler | 9 | 8 | 2 | 2 | 33% wrong costs 0 HP; 33% correct grants +3 Gold. | Collect 10 relics |
| Stone Gardener | 11 | 8 | 3 | 1 | First item used each puzzle node is not consumed. | Defeat 10 bosses |
| Lantern Seer | 7 | 12 | 2 | 1 | Boss modifiers are 30% weaker. Before each boss puzzle, preview all active modifiers and reroll one for free. | Collect 50,000 gold |
| Reed Duelist | 9 | 9 | 2 | 1 | Perfect no-pencil puzzle: +2 Pencil. Every 5 such puzzles in a run: +1 HP. | Find every unique item |
| Quiet Cartographer | 12 | 6 | 1 | 1 | Preview next node's star & board size after a perfect tile. If you then choose that node, its difficulty is reduced by ½ star (min 1★). | Complete a full run (all 5 floors + boss) with zero mistakes |

### 1. Number Freak (Balanced)

**Base Stats:** HP 10 / Pencil 10 / Item Slots 2 / Starting Relics 1  
**Passive:** After completing a puzzle with zero mistakes: +1 Reroll Token.

| Level | Unlock |
|:---:|--------|
| 3 | +1 Pencil |
| 5 | +10 Starting Gold |
| 7 | +2 HP |
| 10 | +2 Pencil, +2 HP, +1 Starting Relic |
| 13 | +10 Starting Gold |
| 15 | +1 Reroll Token |
| 18 | +1 Pencil |
| 20 | +1 Item Slot, +1 Starting Relic |
| 25 | +2 HP, +2 Pencil |
| 30 | Start: Rare Solver |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Golden Number frame |

---

### 2. Garden Monk (Sustain)

**Base Stats:** HP 14 / Pencil 5 / Item Slots 1 / Starting Relics 1  
**Passive:** Every 5 correct placements: +1 HP. Level 15+: every 4.

| Level | Unlock |
|:---:|--------|
| 3 | +1 Pencil |
| 5 | +2 HP |
| 7 | +10 Starting Gold |
| 10 | +1 Item Slot |
| 13 | +2 HP |
| 15 | Passive: heal every 4 placements (was 5) |
| 18 | +1 Pencil |
| 20 | +2 HP, +1 Reroll Token, +1 Starting Relic |
| 25 | +1 Pencil |
| 30 | Start: Meditation Stone |
| 35 | +2 HP |
| 40 | Cosmetic: Blooming Monk frame |

---

### 3. Shrine Archivist (Information)

**Base Stats:** HP 8 / Pencil 15 / Item Slots 2 / Starting Relics 1  
**Passive:** Each pencil mark placed has a 25% chance to auto-reveal the correct digit for that cell.

| Level | Unlock |
|:---:|--------|
| 3 | +2 Pencil |
| 5 | +1 HP |
| 7 | +10 Starting Gold |
| 10 | +1 Reroll Token, +1 Starting Relic |
| 13 | +2 Pencil |
| 15 | +1 HP, +1 Item Slot |
| 18 | +10 Starting Gold |
| 20 | Passive: 2 free pencil uses/cell |
| 25 | +2 Pencil, +1 HP |
| 30 | Start: Pattern Scroll |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Scroll Archive frame |

---

### 4. Koi Gambler (Volatility)

**Base Stats:** HP 9 / Pencil 8 / Item Slots 2 / Starting Relics 2  
**Passive:** 33% wrong costs 0 HP; 33% correct grants +3 Gold.

| Level | Unlock |
|:---:|--------|
| 3 | +1 HP |
| 5 | +15 Starting Gold |
| 7 | +1 Pencil |
| 10 | Passive: correct gold chance 35%, +1 Starting Relic |
| 13 | +1 HP |
| 15 | +1 Item Slot |
| 18 | +15 Starting Gold |
| 20 | Passive: no-HP-loss chance 35% |
| 25 | +2 Pencil, +1 HP |
| 30 | Start: Golden Koi |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Golden Koi frame |

---

### 5. Stone Gardener (Item Synergy)

**Base Stats:** HP 11 / Pencil 8 / Item Slots 3 / Starting Relics 1  
**Passive:** First item used each puzzle node is not consumed.

| Level | Unlock |
|:---:|--------|
| 3 | +1 Pencil |
| 5 | +1 HP |
| 7 | +1 Item Slot |
| 10 | +10 Starting Gold, +1 Starting Relic |
| 13 | +2 Pencil |
| 15 | +1 HP, +1 Reroll Token |
| 18 | +1 Item Slot |
| 20 | Passive: 2 free items/level, +1 Starting Relic |
| 25 | +2 HP |
| 30 | Start: Rice Paper Umbrella |
| 35 | +1 Pencil, +1 HP |
| 40 | Cosmetic: Mossy Stone frame |

---

### 6. Lantern Seer (Boss Specialist)

**Base Stats:** HP 7 / Pencil 12 / Item Slots 2 / Starting Relics 1  
**Passive:** Boss modifiers are 30% weaker. Before each boss puzzle, preview all active modifiers and reroll one for free.

| Level | Unlock |
|:---:|--------|
| 3 | +1 Pencil |
| 5 | +1 HP |
| 7 | +10 Starting Gold |
| 10 | Passive: modifiers 35% weaker |
| 13 | +2 Pencil |
| 15 | +1 HP, +1 Reroll Token |
| 18 | +1 Item Slot |
| 20 | +2 HP, +1 Starting Relic |
| 25 | Passive: modifiers 40% weaker |
| 30 | Start: Lantern of Clarity |
| 35 | +2 Pencil, +1 HP |
| 40 | Cosmetic: Spirit Lantern frame |

---

### 7. Reed Duelist (Tempo)

**Base Stats:** HP 9 / Pencil 9 / Item Slots 2 / Starting Relics 1  
**Passive:** Perfect no-pencil puzzle: +2 Pencil. Every 5 such puzzles in a run: +1 HP.

| Level | Unlock |
|:---:|--------|
| 3 | +1 Pencil |
| 5 | +1 HP |
| 7 | +1 Reroll Token |
| 10 | +10 Starting Gold |
| 13 | +2 Pencil |
| 15 | Passive: perfect no-pencil → +3 Pencil |
| 18 | +1 HP |
| 20 | +1 Item Slot, +1 Starting Relic |
| 25 | +2 HP, +1 Pencil |
| 30 | Start: Temple Incense |
| 35 | +1 Reroll Token |
| 40 | Cosmetic: Cutting Reed frame |

---

### 8. Quiet Cartographer (Route Control)

**Base Stats:** HP 12 / Pencil 6 / Item Slots 1 / Starting Relics 1  
**Passive:** Preview next node's star & board size after a perfect tile. If you then choose that node, its difficulty is reduced by ½ star (min 1★).

| Level | Unlock |
|:---:|--------|
| 3 | +1 Pencil |
| 5 | +10 Starting Gold |
| 7 | +1 HP |
| 10 | +1 Reroll Token, +1 Starting Relic |
| 13 | +1 Pencil, +1 HP |
| 15 | Passive: preview always active |
| 18 | +10 Starting Gold |
| 20 | +1 Item Slot |
| 25 | +2 Pencil, +1 HP |
| 30 | Start: Compass of Order |
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
- Prestige badge (tier shown in the Meta Progression panel and on the HUD) and **+1 starting gold per prestige tier** for every run with that class, at most +9 at tier 9 (owner decision 2026-10-20, SPEC-CONF-039; `ClassGardenProgressionService.PrestigeStartingGold`). The class passive does not change.

### Prestige Cap

Maximum prestige tier is **9**. After tier 9, no further prestige is possible. The class remains at Level 40 permanently.

### Excess XP

XP earned beyond Level 40 (before prestige is triggered) is **discarded**: `ClassGardenProgressionService.AddXp` caps `TotalXp` at the Level 40 threshold (16,860 XP) when a run is committed (implemented 1.1, SPEC-CONF-034; `HarmonyEffectsTests.XpAboveLevel40_IsDiscarded`). Players should be prompted to prestige when eligible rather than accumulating wasted XP.

### Prestige in the UI (as built, 1.1)

The Meta Progression panel shows, for every level-40 class, what prestige still needs ("Prestige needs 6 boss defeats (4/6)") or a **Prestige** button when the class is eligible. The first press changes the label to a confirmation ("Confirm: reset to Level 1?"); the second press prestiges and saves the profile (`ProfileService.TryPrestigeClass`, GAP-017). The tier gate uses the cumulative boss defeats of all classes (`ClassUnlockProgress.BossesDefeated`). The in-run class badge shows the prestige tier. Evidence: `PrestigeTests`.

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
