# Meta Progression System

**Version:** 1.2 | **Date:** 2026-03-24 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.2 | 2026-03-24 | implemented | MetaProgressionPanelController shows all 8 classes (not just those with progress): unlocked classes show Level/Prestige/XP + base stats + passive; locked classes show greyed "Locked" text. Uses ClassCatalog.GetDefinition() for display data. |
| 1.1 | 2026-03-23 | implemented | Post-run orchestration via RecordRunAndGetNewUnlocks, CompletionService 4×25% formula, AscensionService TryPrestigeReset and MaxStarCap, prestige boss-gate, all class unlock thresholds corrected |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Meta progression encompasses all persistent systems that carry between runs: **class XP and leveling**, **class unlocks**, **ascension**, **prestige**, **codex discovery**, **achievements**, and **global completion tracking**. These systems provide long-term goals beyond individual run success.

---

## Class Unlock System

Classes are unlocked by meeting **cumulative cross-class conditions** tracked in `ClassUnlockProgress`:

| # | Class | Unlock Condition | Tracked Counter |
|:---:|-------|-----------------|-----------------|
| 1 | Number Freak | Default (always unlocked) | — |
| 2 | Garden Monk | Defeat 2 bosses | `CumulativeBossDefeats ≥ 2` |
| 3 | Shrine Archivist | Use 15 items | `ItemsUsed ≥ 15` |
| 4 | Koi Gambler | Collect 10 relics | `RelicsCollected ≥ 10` |
| 5 | Stone Gardener | Defeat 10 bosses | `CumulativeBossDefeats ≥ 10` |
| 6 | Lantern Seer | Collect 50,000 gold | `GoldCollected ≥ 50000` |
| 7 | Reed Duelist | Find every unique item once | `ItemCodex` — all item entries discovered |
| 8 | Quiet Cartographer | Clear full stage with no pencil use and no HP loss | `PerfectNoPencilStage` flag |

Counters are updated in `ProfileService.RecordRunAndGetNewUnlocks()` after every run. `ClassUnlockService.EvaluateUnlocks()` checks all conditions and returns newly unlocked class IDs.

### Unlock Progression Order

Typical unlock order based on natural play progression:
1. **Number Freak** — start
2. **Garden Monk** — ~2 runs (2 bosses)
3. **Shrine Archivist** — ~5 runs (15 items used)
4. **Koi Gambler** — ~10 runs (10 relics found)
5. **Stone Gardener** — ~10 runs (10 bosses defeated)
6. **Lantern Seer** — ~30 runs (50,000 gold accumulated)
7. **Reed Duelist** — ~50+ runs (full item codex discovery)
8. **Quiet Cartographer** — skill-gated (perfect no-pencil stage)

### Class Select Display

- Unlocked classes: selectable with full stat block, passive description, current level
- Locked classes: greyed out, show silhouette icon + unlock condition text
- Current class level shown as "X / Y XP" text (no progress bar)

---

## Class XP & Leveling

See [ClassXpProgressionSystem](ClassXpProgressionSystem_implemented.md) for the full XP curve and per-tile formula.

### Summary

- Per-class `TotalXp` stored; level derived at runtime via `DeriveLevel(totalXp)`
- Two-phase curve: L1–15 = `50 + (n-1)×10`, L16–40 = `190 + (n-15)×35`
- Total to L40: 16,860 XP; target ~80 runs
- XP committed to class at run end (no mid-run leveling)
- Level-up display only at game over / completion screen

### Per-Class Level Rewards

Each class has **12 milestone levels** (L3, L5, L7, L10, L12, L15, L20, L25, L30, L35, L38, L40) granting stat bonuses (+HP, +Pencil, +Reroll, +Slot, +Gold) and passive upgrades. See `ClassGardenProgressionService.GetStatBonuses()` for the full unlock schedule.

---

## Prestige System

Prestige allows players to reset a class's level in exchange for permanent cosmetic recognition and increasingly difficult boss gate requirements.

### Rules

- **Max prestige tier:** 9 (per class, not global)
- **Requirement:** Class must be Level 40
- **Boss gate:** Must have defeated `3 × (PrestigeTier + 1)` bosses cumulatively (from the archive counter)
- **On prestige:** Class `TotalXp` resets to 0, `PrestigeTier` increments by 1
- **Reward:** Prestige badge displayed on class select, cosmetic tier indicator

### Prestige Tier Boss Requirements

| Tier | Boss Defeats Required |
|:---:|:---:|
| 1 → 2 | 6 |
| 2 → 3 | 9 |
| 3 → 4 | 12 |
| 4 → 5 | 15 |
| 5 → 6 | 18 |
| 6 → 7 | 21 |
| 7 → 8 | 24 |
| 8 → 9 | 27 |

### Prestige Display

- Class select: prestige tier shown as star/badge count next to class name
- Meta progression panel: prestige history per class
- End-of-run: if prestige triggers, special animation plays after XP bar fills to L40

---

## Ascension System

Ascension is a **global** progression layer above class prestige:

### `AscensionService.ApplyAscension(meta)`

- Increments `AscensionLevel`
- Increases `MaxStarCap` by +1 (capped at 7)
- Sets `SeasonalChallengeUnlocked = true`

### `AscensionService.TryPrestigeReset(meta)`

Global prestige reset (distinct from per-class prestige):
- Requires `AscensionLevel > 0`
- Increments `PrestigeCount`
- Resets `AscensionLevel = 0`, `MaxStarCap = 5`
- Clears `PurchasedPermanentUpgrades`

### Seasonal Challenges

`BuildMonthlySeed(year, month)` generates a deterministic seed: `(year × 100) + month`. Seasonal challenges use this seed for monthly leaderboard puzzles, unlocked after first ascension.

---

## Completion Tracker

`CompletionService.Recalculate()` evaluates 4 global completion checks:

| Check | Condition | Weight |
|-------|-----------|:---:|
| All Sizes × All Stars Cleared | Each board size (5–9) completed at each star (1–6) = 30 combos | 25% |
| All Modifiers Cleared | All 15 `BossModifierId` values in `BossClearsByModifier` | 25% |
| All 8 Classes at Level 30+ | All 8 classes reached Level 30 or higher | 25% |
| All Relics Discovered | All relics in codex marked Discovered | 25% |

```
GlobalCompletionPercent = (checks_passed / 4) × 100%
```

Completion percentage is displayed on the meta progression panel.

---

## Mastery & Boss Records

`MasteryAchievementState` tracks detailed boss clear records:

| Record | Tracked Data |
|--------|-------------|
| Boss clears by modifier | `BossClearsByModifier` — Dictionary<BossModifierId, int> |
| High-star boss clears | 4★ and 5★ boss victories |
| No-HP-loss boss clears | Boss defeated without taking damage |
| Dual-modifier boss clears | Floor 2+ with 2+ simultaneous modifiers |
| 9×9 5★ clears | Via mastery service |
| No-item runs | Full run without using any items |
| Hidden dual modifier boss | Unlocked when 9+ distinct modifiers cleared |

---

## Codex

The **Item & Relic Codex** tracks persistent discovery across all runs:

- Entry marked **Discovered** on first acquisition (picking up item or relic)
- Seeing in shop without buying does **not** count
- Unified list: items and relics in one codex
- Undiscovered entries show "???" with silhouette icon
- Full discovery feeds into Reed Duelist unlock condition (items only)
- Codex accessible from main menu "Items" button

---

## Achievement System

`SteamAchievementService` evaluates unlock conditions after each run.

### Named Achievements (9)

| ID | Name | Condition | Tier |
|----|------|-----------|------|
| `first_puzzle` | First Steps | Complete 1 puzzle | Beginner |
| `first_boss` | Garden Guardian | Defeat a boss | Beginner |
| `combo_20` | Flow State | Reach 20 combo streak | Intermediate |
| `dual_modifier_clear` | Double Trouble | Clear a dual-modifier boss | Intermediate |
| `perfect_boss` | Flawless Victory | Defeat a boss with no mistakes | Advanced |
| `endless_depth_20` | Deep Meditation | Reach Endless Zen depth 20 | Advanced |
| `no_hp_loss_run` | Untouched | Complete a run with 0 mistakes | Expert |
| `hidden_dual_boss` | Inner Harmony | Clear a dual-modifier boss (hidden) | Hidden |
| `chaos_monk_unlock` | Last Stand | Clear dual-modifier boss with 1 HP remaining | Hidden |

### Tutorial Achievements

Tutorial mode can unlock achievements for:
- Complete a puzzle at every board size (5×5 through 9×9)
- Complete a puzzle with each of the 15 modifiers
- Complete the full board×star grid
- Complete a 7★ puzzle
- Complete a 9×9 7★ puzzle with at least one modifier

Tutorial achievements grant **no gameplay progression** (no XP, no gold, no class unlocks).

### Full Achievement List (52 Achievements)

> **Review status:** Proposed — all achievements need balance review.

#### Beginner (1–12)

| # | ID | Name | Condition |
|:---:|-----|------|-----------|
| 1 | `first_puzzle` | First Steps | Complete 1 puzzle |
| 2 | `first_boss` | Garden Guardian | Defeat a boss |
| 3 | `first_floor` | Bamboo Gate | Complete Floor 1 |
| 4 | `first_item` | Tool of the Trade | Use an item for the first time |
| 5 | `first_relic` | Keepsake | Acquire a relic for the first time |
| 6 | `gold_100` | Pocket Change | Earn 100 gold in a single run |
| 7 | `pencil_saver` | Ink Miser | Complete a puzzle using 0 pencil marks |
| 8 | `class_unlock_1` | New Perspective | Unlock your second class |
| 9 | `risk_route` | Brave Path | Choose the Risk route for the first time |
| 10 | `shop_purchase` | Window Shopping | Buy an item from the shop |
| 11 | `combo_5` | Getting Started | Reach a 5-placement combo streak |
| 12 | `tutorial_complete_1` | Student | Complete a tutorial puzzle at any size |

#### Intermediate (13–28)

| # | ID | Name | Condition |
|:---:|-----|------|-----------|
| 13 | `combo_20` | Flow State | Reach 20 combo streak |
| 14 | `dual_modifier_clear` | Double Trouble | Clear a dual-modifier boss (Floor 2+) |
| 15 | `floor_3` | Deep Garden | Reach Floor 3 |
| 16 | `floor_5` | Summit Seeker | Reach Floor 5 |
| 17 | `first_victory` | Garden Cleared | Complete a full 5-floor run |
| 18 | `gold_1000` | Prosperous | Earn 1,000 gold in a single run |
| 19 | `class_unlock_4` | Versatile | Unlock 4 classes |
| 20 | `level_10` | Apprentice | Reach Level 10 with any class |
| 21 | `relic_swap` | Hard Choice | Swap a relic at a relic node |
| 22 | `star_4_clear` | Stargazer | Complete a 4★ puzzle |
| 23 | `star_5_clear` | Constellation | Complete a 5★ puzzle |
| 24 | `9x9_clear` | Full Grid | Complete a 9×9 puzzle |
| 25 | `modifier_5_seen` | Well Traveled | Encounter 5 different modifiers across runs |
| 26 | `all_sizes_played` | Size Explorer | Complete a puzzle at every board size (5–9) |
| 27 | `risk_3_floors` | Daredevil | Choose Risk route on 3 floors in a single run |
| 28 | `epic_item` | Purple Glow | Acquire an Epic rarity item |

#### Advanced (29–42)

| # | ID | Name | Condition |
|:---:|-----|------|-----------|
| 29 | `perfect_boss` | Flawless Victory | Defeat a boss with no mistakes |
| 30 | `endless_depth_20` | Deep Meditation | Reach Endless Zen depth 20 |
| 31 | `level_20` | Journeyman | Reach Level 20 with any class |
| 32 | `level_30` | Master | Reach Level 30 with any class |
| 33 | `all_modifiers_seen` | Modifier Scholar | Encounter all 15 modifiers across runs |
| 34 | `star_6_clear` | Void Walker | Complete a 6★ puzzle |
| 35 | `gold_5000_run` | Golden Garden | Earn 5,000 gold in a single run |
| 36 | `no_item_victory` | Purist | Complete a full run without using any items |
| 37 | `class_unlock_8` | Full Roster | Unlock all 8 classes |
| 38 | `triple_modifier` | Triple Threat | Clear a boss with 3 simultaneous modifiers |
| 39 | `all_stars_all_sizes` | Completionist | Clear every board size × star combination (30 combos) |
| 40 | `perfect_floor` | Flawless Floor | Complete an entire floor with no mistakes |
| 41 | `codex_50` | Collector | Discover 50% of all items and relics |
| 42 | `prestige_1` | Reborn | Prestige a class for the first time |

#### Expert (43–50)

| # | ID | Name | Condition |
|:---:|-----|------|-----------|
| 43 | `no_hp_loss_run` | Untouched | Complete a full run with 0 mistakes |
| 44 | `level_40` | Enlightened | Reach Level 40 with any class |
| 45 | `all_classes_30` | Garden Council | Reach Level 30 with all 8 classes |
| 46 | `endless_depth_50` | Eternal Patience | Reach Endless Zen depth 50 |
| 47 | `five_modifier_boss` | Quintuple | Clear a Floor 5 boss (5 simultaneous modifiers) |
| 48 | `full_codex` | Encyclopedist | Discover 100% of all items and relics |
| 49 | `spirit_grandmaster` | Trial Champion | Complete a Grandmaster Spirit Trial with score ≥ 1500 |
| 50 | `prestige_5` | Ascended | Reach Prestige Tier 5 with any class |

#### Hidden (51–52)

| # | ID | Name | Condition |
|:---:|-----|------|-----------|
| 51 | `hidden_dual_boss` | Inner Harmony | Clear a dual-modifier boss (discovered during play) |
| 52 | `chaos_monk_unlock` | Last Stand | Clear dual-modifier boss with exactly 1 HP remaining |

---

## Profile Statistics

`ProfileStats` tracks cumulative statistics across all runs:

| Stat | Description |
|------|-------------|
| TotalRuns | Total runs started |
| BossClears | Total boss victories |
| AverageMistakes | Running average across all runs |
| FastestTime | Best completion time for a full run |
| HighestEndlessDepth | Best Endless Zen depth achieved |

### Mode Unlocks via Stats

| Mode | Unlock Condition |
|------|-----------------|
| Endless Zen | `TotalRuns ≥ 10` |
| Spirit Trials | `SpiritTrialsUnlocked` flag (set via ascension or achievement) |

---

## Data Flow — Post-Run Update

`ProfileService.RecordRunAndGetNewUnlocks(RunResult)`:

```
1. Skip if tutorial run
2. Apply XP → ClassGardenProgressionService.ApplyRun()
   └── Updates TotalXp, derives level, checks prestige
3. Evaluate class unlocks → ClassUnlockService.EvaluateUnlocks()
   └── Checks all 8 conditions, returns newly unlocked classes
4. Update stats (TotalRuns, BossClears, etc.)
5. Check Endless Zen unlock (TotalRuns ≥ 10)
6. Record boss mastery (modifier, stars, no-HP, dual modifier)
7. Check hidden dual modifier unlock (9+ modifiers cleared)
8. Record 9×9 5★ clear
9. Record no-item run
10. Recalculate completion → CompletionService.Recalculate()
11. Evaluate achievements → SteamAchievementService.EvaluateUnlocks()
12. Return List<ClassId> of newly unlocked classes
```

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Meta/AscensionService.cs` | Ascension, global prestige reset, seasonal seeds |
| `Assets/Scripts/Meta/CompletionService.cs` | 4-check global completion tracker |
| `Assets/Scripts/Meta/ClassGardenProgressionService.cs` | XP curve, level derivation, per-class unlock schedules, prestige |
| `Assets/Scripts/Meta/SteamAchievementService.cs` | Achievement definitions and unlock evaluation |
| `Assets/Scripts/Classes/ClassUnlockService.cs` | Class unlock condition evaluation |
| `Assets/Scripts/Save/ProfileService.cs` | Post-run update orchestrator |
| `Assets/Scripts/Economy/XpService.cs` | Per-tile XP calculation, level curve |
| `Assets/Scripts/Core/RuntimeModels.cs` | MetaProgressionState, ClassUnlockProgress, MasteryAchievementState |

---

## Open Issues

### OPEN-A — Ascension Reward Design
**Status:** Deferred
**Description:** Ascension currently only raises MaxStarCap and unlocks seasonal challenges. Additional ascension rewards (cosmetics, difficulty modifiers) are deferred to post-launch.



---

---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Code File | Verified Classes | Status |
|--------|-------|-----------|-----------------|--------|
| REQ-META-010 | TITLE | `—` | — | ✗ missing |
| REQ-META-011 | ClassUnlockSystem | `Assets\Scripts\Classes\ClassUnlockService.cs` | ClassUnlockService | ✓ verified |
| REQ-META-012 | ClassXpProgressionSystem | `Assets\Scripts\Run\RunDirector.cs` | RunDirector | ✓ verified |
| REQ-META-013 | AscensionSystem | `Assets\Scripts\Meta\AscensionService.cs` | AscensionService | ✓ verified |
| REQ-META-014 | PrestigeSystem | `Assets\Scripts\Save\ProfileService.cs` | — | ✓ verified |
| REQ-META-015 | CompletionTracker | `Assets\Scripts\Meta\CompletionService.cs` | CompletionService | ✓ verified |
| REQ-META-016 | MasteryBossRecords | `Assets\Scripts\Boss\BossService.cs` | BossService | ✓ verified |
| REQ-META-017 | CodexSystem | `Assets\Scripts\Economy\RelicService.cs` | RelicService | ✓ verified |
| REQ-META-018 | AchievementSystem | `Assets\Scripts\Meta\SteamAchievementService.cs` | SteamAchievementService | ✓ verified |
