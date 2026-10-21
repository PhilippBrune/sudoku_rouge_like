# Meta Progression System

**Version:** 1.12 | **Date:** 2026-10-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.12 | 2026-10-21 | implemented | The Meta Progression screen is the garden of classes (UI-STYLE-019, owner pick): new META-UNLOCK-006 (each locked class shows its goal with its progress), META-STATS-001 (Garden Runs and best floor per class). META-XP-006 now says where the schedule lives (ClassCatalog) and that it is shown in the player's language; the Koi Gambler's and Lantern Seer's passive steps name the values the game uses. The XP stroke on this screen is logged as SPEC-CONF-041 (ClassXpProgressionSystem says no bar). |
| 1.11 | 2026-10-21 | implemented | The Codex as a collector's book (UI-STYLE-018, owner pick): bookmarks Items, Relics, Rules and Curses. New: the curses met in any run are recorded for the Curses page (META-CODEX-008), and each item counts how often it was found and used (META-CODEX-009). META-CODEX-005 is verified by a test. |
| 1.10 | 2026-10-20 | implemented | Owner decisions: the prestige reward is the badge plus +1 starting gold per tier (SPEC-CONF-039, META-PRESTIGE-005); Ascension is retired, because Harmony already is the post-win difficulty ladder and prestige the per-class goal (GAP-017, FUTURE-009): META-ASCEND-001..003 descoped, the unused save fields stay for old saves, OPEN-A closed. |
| 1.9 | 2026-10-19 | implemented | Catalog batch B5 (BossMechanicsSystem 2.12) adds 5 modifiers to the codex and completion: 70 clearable modifiers. |
| 1.8 | 2026-10-19 | implemented | Catalog batch B4 (BossMechanicsSystem 2.11) adds 5 modifiers to the codex and completion: 65 clearable modifiers. |
| 1.7 | 2026-10-19 | implemented | Catalog batch B6 (BossMechanicsSystem 2.10) adds 11 modifiers to the codex and completion: 60 clearable modifiers. |
| 1.6 | 2026-10-19 | implemented | Catalog batch B3 (BossMechanicsSystem 2.9) adds 7 modifiers to the codex and completion: 49 clearable modifiers. |
| 1.5 | 2026-10-19 | implemented | Owner decision: the 13 catalog modifiers (BossMechanicsSystem 2.8) count toward the codex and completion: 42 clearable modifiers (completion drops a little for existing players). |
| 1.4 | 2026-10-07 | implemented | 1.1 review decisions (2026-10-07): Ascension marked deferred, META-ASCEND-001..003 reserved (SPEC-CONF-026/036, GAP-017); Monthly Walk always available, no ascension gate (SPEC-CONF-026); Spirit Trials unlock after the first boss defeat (SPEC-CONF-007); completion counts 29 clearable modifiers (SPEC-CONF-019, GAP-009); prestige button as built (GAP-017); prestige reward contradiction logged as SPEC-CONF-039; SPICE table archived (SPEC-OBS-001). Achievements: only the Harmony achievements are implemented; the named and full lists are marked as proposals (SPEC-MAC-008, FUTURE-016); run-end step 11 names `EvaluateHarmonyAchievements`. |
| 1.3 | 2026-04-14 | implemented | Added requirement IDs (META-UNLOCK, META-XP, META-PRESTIGE, META-ASCEND, META-COMPLETE, META-MASTERY, META-CODEX, META-ACHIEVE, META-STATS). |
| 1.2 | 2026-03-24 | implemented | MetaProgressionPanelController shows all 8 classes (not just those with progress): unlocked classes show Level/Prestige/XP + base stats + passive; locked classes show greyed "Locked" text. Uses ClassCatalog.GetDefinition() for display data. |
| 1.1 | 2026-03-23 | implemented | Post-run orchestration via RecordRunAndGetNewUnlocks, CompletionService 4×25% formula, AscensionService TryPrestigeReset and MaxStarCap, prestige boss-gate, all class unlock thresholds corrected |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Meta progression encompasses all persistent systems that carry between runs: **class XP and leveling**, **class unlocks**, **prestige**, **codex discovery**, **achievements**, and **global completion tracking**. These systems provide long-term goals beyond individual run success.

---

## Class Unlock System

**[META-UNLOCK-001]** Classes are unlocked by meeting **cumulative cross-class conditions** tracked in `ClassUnlockProgress`:

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

**[META-UNLOCK-002]** Counters are updated in `ProfileService.RecordRunAndGetNewUnlocks()` after every run. `ClassUnlockService.EvaluateUnlocks()` checks all conditions and returns newly unlocked class IDs.

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

**[META-UNLOCK-003]** Unlocked classes: selectable with full stat block, passive description, current level.
**[META-UNLOCK-004]** Locked classes: greyed out, show silhouette icon + unlock condition text.
**[META-UNLOCK-005]** Current class level shown as "X / Y XP" text (no progress bar). (The Meta Progression screen's XP stroke from the picked garden of classes, UI-STYLE-019, differs from this line and from ClassXpProgressionSystem §UI Placement: SPEC-CONF-041, open for the owner.)

### Meta Progression Display (the garden of classes, UI-STYLE-019)

**[META-UNLOCK-006]** Each locked class shows its unlock goal with its progress, on its plaque and on the scroll: bosses defeated (2 or 10), items used (15), relics collected (10), gold collected (50,000), items found (the Codex without the counter items, META-CODEX-006), or "a run with no mistakes" (done or not). The progress never shows more than the goal. Goals and progress come from `ClassUnlockService` (`GetProgress`, the same thresholds `CheckNewUnlocks` uses).

---

## Class XP & Leveling

See [ClassXpProgressionSystem](ClassXpProgressionSystem.md) for the full XP curve and per-tile formula.

### Summary

**[META-XP-001]** Per-class `TotalXp` stored; level derived at runtime via `DeriveLevel(totalXp)`.
**[META-XP-002]** Two-phase curve: L1–15 = `50 + (n-1)×10`, L16–40 = `190 + (n-15)×35`.
**[META-XP-003]** Total to L40: 16,860 XP; target ~80 runs.
**[META-XP-004]** XP committed to class at run end (no mid-run leveling).
**[META-XP-005]** Level-up display only at game over / completion screen.

### Per-Class Level Rewards

**[META-XP-006]** Each class has **12 milestone levels** granting stat bonuses (+HP, +Pencil, +Reroll, +Slot, +Gold, +Starting Relic) and passive upgrades; the schedule is `ClassCatalog.GetAllUnlocks` (listed in ClassXpProgressionSystem) and the stat bonuses are applied by `RunArchetypeService.GetLevelBonuses`. The class screen and the garden of classes show each entry in the player's language (`ClassUnlockText`). Not yet granted: the Level 30 starting items and the Level 40 frames (GAP-063).

---

## Profile Statistics per Class

**[META-STATS-001]** Each class counts its **Garden Runs** and keeps its **best floor** (1–5, the floor the run ended on). Only Garden Runs count, not the tutorial, Endless Zen, Spirit Trials or the Monthly Walk. Counted from 2026-10-21 on; older saves start at 0, and the screen leaves out a count of 0.

## Prestige System

Prestige allows players to reset a class's level in exchange for permanent cosmetic recognition and increasingly difficult boss gate requirements.

### Rules

**[META-PRESTIGE-001]** Max prestige tier: 9 (per class, not global).
**[META-PRESTIGE-002]** Requirement: Class must be Level 40.
**[META-PRESTIGE-003]** Boss gate: Must have defeated `3 × (PrestigeTier + 1)` bosses cumulatively.
**[META-PRESTIGE-004]** On prestige: Class `TotalXp` resets to 0, `PrestigeTier` increments by 1.
**[META-PRESTIGE-005]** Reward: the prestige badge (tier indicator) and **+1 starting gold per tier** for runs with that class, at most +9 (owner decision 2026-10-20, SPEC-CONF-039). The class passive does not change.

As built (1.1, GAP-017): level-40 classes show their prestige status in the Meta Progression panel and a **Prestige** button with a two-step confirmation once the boss gate is met (ClassXpProgressionSystem §Prestige in the UI). The panel summary shows the total prestige tiers instead of the never-built Ascension level.

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

## Ascension System — retired

**Status: retired (owner decision 2026-10-20, GAP-017, FUTURE-009).** Harmony (HarmonyDifficultySystem) is the post-win difficulty ladder and prestige is the per-class long-term goal, so a separate global Ascension layer is not built. META-ASCEND-001..003 are descoped (🗄️ in `REQUIREMENT_MAP.md`); `MetaProgressionState.AscensionLevel`, `PrestigeCount` and `MaxStarCap` stay unused in the save model so older saves load unchanged.

- 🗄️ [META-ASCEND-001] Ascension as a global layer above class prestige.
- 🗄️ [META-ASCEND-002] `ApplyAscension`: +1 AscensionLevel, +1 MaxStarCap.
- 🗄️ [META-ASCEND-003] `TryPrestigeReset`: global prestige reset.

### Seasonal Challenges (Monthly Walk)

**[META-ASCEND-004]** `BuildMonthlySeed(year, month)` generates a deterministic seed: `(year × 100) + month`. The Monthly Walk uses this seed. It is **always available** — there is no ascension gate (decided 2026-10-07, SPEC-CONF-026; SEASON-UNLOCK-001 updated) — and since 1.1 it has its own widget on the main menu (SeasonalChallengeSpec).

---

## Completion Tracker

**[META-COMPLETE-001]** `CompletionService.Recalculate()` evaluates 4 global completion checks:

| Check | Condition | Weight |
|-------|-----------|:---:|
| All Sizes × All Stars Cleared | Each board size (5–9) completed at each star (1–6) = 30 combos | 25% |
| All Modifiers Cleared | All 70 clearable player modifiers (IDs 0–29 except Modular Regions, which is never rolled, plus the catalog batches 30–32, 37, 38, 40–46, 48, 51–53, 58–61, 63–70, 72–74, 76–84, 93) recorded as cleared; a modifier counts when a boss with it is defeated (GAP-009, SPEC-CONF-019) | 25% |
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

**[META-CODEX-001]** The **Item & Relic Codex** tracks persistent discovery across all runs:

- **[META-CODEX-002]** Entry marked **Discovered** on first acquisition (picking up item or relic)
- **[META-CODEX-003]** Seeing in shop without buying does **not** count
- **[META-CODEX-004]** Unified list: items and relics in one codex
- **[META-CODEX-005]** Undiscovered entries show "???" with silhouette icon
- **[META-CODEX-006]** Full discovery feeds into Reed Duelist unlock condition (items only)
- **[META-CODEX-007]** Codex accessible from main menu "Items" button
- **[META-CODEX-008]** Curses met in any run (taken, also when cleansed later) are recorded and shown on the Codex's Curses page. They are merged into the save at the run's end and on every autosave, so a run saved and quit to the menu shows them too. Old saves start with none. A curse id this build does not know stays in the save (a newer build may have added it); the Codex lists only the curses of the catalog.
- **[META-CODEX-009]** Each item's entry counts how often it was **found** (every pickup from a reward or the shop) and **used**. Uses are counted once, at the run's end (autosaves do not count them); an item the Codex has not found (a starting item, the tutorial's) is not counted, so a count never reveals an unknown item. Entries from before the counters show "found once", and their next pickup counts as the second.

The Codex is shown as a collector's book (layout-proposals.md, UI-STYLE-018): bookmarks Items, Relics, Rules (the boss modifiers) and Curses with their counts, a filter (All, Found, Not yet found), the entries as tiles and the chosen entry on the right page with the collection's goal.

---

## Achievement System

`SteamAchievementService` evaluates unlock conditions after each run.

**Implementation status (1.1, SPEC-MAC-008):** only the Harmony achievements are granted (HARMONY-ACHIEVE-001: `SteamAchievementService.EvaluateHarmonyAchievements`, tested by `SteamAchievementServiceTests`). The named and full lists below are design proposals: the game grants none of them yet, so they carry no requirement IDs. Each one needs an ID, a condition hook and a test before it ships, and the matching achievement must exist in the Steamworks portal (FUTURE-016).

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
| Spirit Trials | `SpiritTrialsUnlocked` flag, set after the first boss defeat (decided 2026-10-07, SPEC-CONF-007; TRIAL-UNLOCK-001) |

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
11. Evaluate Harmony achievements → SteamAchievementService.EvaluateHarmonyAchievements()
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
**Status:** Closed (2026-10-20): Ascension is retired (§Ascension System).



---

---

## Requirements Traceability

The canonical requirement IDs for this document are listed in `docs/REQUIREMENT_MAP.md`. The auto-generated `REQ-*` table from the retired SPICE pipeline was moved to `docs/archive/SPICE_REQUIREMENT_TABLES.md` (2026-10-07, SPEC-OBS-001); its IDs are kept there and are not used by code or tests.
