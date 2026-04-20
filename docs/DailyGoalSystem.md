# Daily Goal System

**Version:** 0.1 | **Date:** 2026-04-09 | **Status:** new

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-04-09 | new | Initial specification |

---

## Overview

Each day, the player is presented with **3 daily goals** — small, targeted challenges drawn from a seeded pool. Completing goals earns **Ink Stamps**, a cosmetic meta-currency, and small run-start bonuses. A streak counter tracks consecutive days with all 3 goals completed.

Goals are generated deterministically from the current date — no network connection required. All players playing on the same date see the same 3 goals.

---

## Main Menu Placement

A **Daily Walk** widget sits on the main menu, between the Play buttons and the Class/Meta panels. It shows:

```
┌─────────────────────────────────────────────────────┐
│  🖊  Daily Walk — Wednesday                          │
│                                                     │
│  ☐  Complete any puzzle                   +20 Gold  │
│  ☐  Defeat a boss                    +1 Reroll Tkn  │
│  ☐  Complete a puzzle with no mistakes  +1 🖊 Stamp │
│                                                     │
│  Streak: 4 days  ·  Stamps: 12                      │
└─────────────────────────────────────────────────────┘
```

- Checkboxes fill in as goals are completed mid-run or on run end
- Completed goals show a muted tick and their reward already claimed
- If all 3 are done: the widget header changes to `"Daily Walk — Complete ✓"` in gold

---

## Goal Seeding

The 3 goals for a given day are selected via:

```csharp
int seed = LocalDate.Year * 10000 + LocalDate.Month * 100 + LocalDate.Day;
var rng = new Random(seed);
// Pick 3 distinct goal indices from the pool (no duplicates same day)
```

Goal selection is weighted by tier so each day has a mix:
- Goal 1: always Tier 1 (Easy)
- Goal 2: Tier 2 (Medium)
- Goal 3: Tier 2 or Tier 3 (50/50 by day seed)

---

## Goal Pool (21 Goals)

### Tier 1 — Easy (completes in ≤ 1 partial run)

| ID | Goal | Reward |
|----|------|--------|
| `t1_any_puzzle` | Complete any puzzle | +20 Gold at next run start |
| `t1_visit_rest` | Visit a Rest node | +20 Gold at next run start |
| `t1_spend_gold` | Spend 50 gold in a single run | +20 Gold at next run start |
| `t1_collect_item` | Collect any item | +20 Gold at next run start |
| `t1_use_pencil` | Use pencil marks in a puzzle | +20 Gold at next run start |
| `t1_visit_shop` | Visit the Coffee & Ice Cream cart | +20 Gold at next run start |
| `t1_choose_risk` | Take the Risk route on any floor | +20 Gold at next run start |

### Tier 2 — Medium (requires meaningful play)

| ID | Goal | Reward |
|----|------|--------|
| `t2_no_mistake` | Complete a puzzle with no mistakes | +1 Reroll Token at next run start |
| `t2_defeat_boss` | Defeat any boss | +1 Reroll Token at next run start |
| `t2_collect_150g` | Collect 150 gold in a single run | +1 Reroll Token at next run start |
| `t2_use_2_items` | Use 2 items in a single run | +1 Reroll Token at next run start |
| `t2_floor2` | Reach Floor 2 | +1 Reroll Token at next run start |
| `t2_accept_cursed` | Accept a Cursed node | +1 Reroll Token at next run start |
| `t2_collect_relic` | Collect a relic | +1 Reroll Token at next run start |

### Tier 3 — Hard (requires skill or run length)

| ID | Goal | Reward |
|----|------|--------|
| `t3_full_hp_floor` | Complete any floor without losing HP | +1 Ink Stamp |
| `t3_no_pencil_puzzle` | Complete a puzzle using zero pencil marks | +1 Ink Stamp |
| `t3_boss_floor3` | Defeat the Floor 3 boss | +1 Ink Stamp |
| `t3_floor4` | Reach Floor 4 | +1 Ink Stamp |
| `t3_win_run` | Win a run (defeat the Floor 5 boss) | +2 Ink Stamps |
| `t3_no_mistake_boss` | Defeat a boss with no mistakes | +2 Ink Stamps |
| `t3_new_class` | Play as a class you haven't used in the last 7 days | +1 Ink Stamp |

---

## Goal Evaluation

Goals are checked at two points:
1. **During a run** — `DailyGoalService.EvaluateInRun(RunState, GoalId)` is called after each relevant action (puzzle completion, node arrival, gold change, etc.)
2. **On run end** — a final pass evaluates all incomplete goals against the completed run record

Each goal tracks a `bool Completed` per-goal-index in `DailyGoalState`. Completion is **permanent for the day** — a completed goal stays completed even if you start a new run.

Rewards are granted once, immediately when the goal is first completed:
- Gold reward: added to `RunState.CurrentGold` if a run is active, otherwise stored as `PendingStartGold` and applied at the next run start
- Reroll Token: added to `RunState.RerollTokens` if active, otherwise stored as `PendingStartRerollTokens`
- Ink Stamp: added to `DailyGoalState.TotalInkStamps` immediately

---

## Streak System

| Condition | Effect |
|-----------|--------|
| All 3 goals completed today | Streak increments by 1 |
| Any goal not completed by midnight | Streak resets to 0 |
| Streak = 7 | Award +3 Ink Stamps bonus, display title `"Dedicated Walker"` in main menu widget |
| Streak = 30 | Award +10 Ink Stamps bonus, display title `"Park Regular"` in main menu widget |
| Streak = 100 | Award +25 Ink Stamps bonus, display title `"The One Who Returns"` |

Streak titles are cosmetic — shown in the Daily Walk widget header only. The 7-day bonus repeats every 7 days of unbroken streak (not a one-time award).

---

## Ink Stamps (Cosmetic Currency)

Stamps are spent in a **Stamps Gallery** panel accessible from the main menu (small button in the corner of the Daily Walk widget). No gameplay impact — purely cosmetic:

| Cost | Reward |
|------|--------|
| 5 Stamps | Alternative park flavor text set for Rest and Event nodes |
| 10 Stamps | Alternative run-start/end dialog lines (second set) |
| 15 Stamps | Main menu background color palette variant ("Evening Walk" — warm amber tones) |
| 20 Stamps | Alternative park flavor text set #2 (rainy day / grey weather lines) |
| 30 Stamps | "Veteran's Bench" — a small cosmetic icon shown next to your class name in the class select panel |

Stamps are not consumed by purchases — they accumulate permanently as a visible score of dedication. Stamp *spending* unlocks cosmetics; the stamp total never decreases.

*(Design note: If stamp spending feels unintuitive given they don't decrease, rename to "Discovered" milestones — cosmetics unlock when the total stamps reaches the threshold, not spent.)*

---

## Data Model

### `DailyGoalState` (new, in `SaveModels.cs` → `ProfileSaveData`)

```csharp
[Serializable]
public sealed class DailyGoalState
{
    public string LastGoalDate;          // "yyyy-MM-dd" of the last day goals were generated
    public int[] TodayGoalIds;           // [3] indices into the full goal pool
    public bool[] TodayGoalCompleted;    // [3] completion flags

    public int CurrentStreak;
    public string LastStreakDate;        // last date a complete day was recorded

    public int TotalInkStamps;
    public List<int> UnlockedCosmetics; // indices of purchased cosmetics

    // Run-start pending rewards (applied at next LaunchRun)
    public int PendingStartGold;
    public int PendingStartRerollTokens;
}
```

### Daily refresh logic

On `GameBootstrap.Start()` (or when the main menu is shown):
```csharp
DailyGoalService.RefreshIfNewDay(goalState, LocalDate.Today);
```

`RefreshIfNewDay` checks `LastGoalDate != today.ToString()`. If different:
- Evaluate streak (did all 3 goals complete yesterday?)
- Generate new 3 goals for today using date seed
- Reset `TodayGoalCompleted` to `[false, false, false]`
- Save `LastGoalDate = today`

---

## Implementation Notes

| File | Change |
|------|--------|
| `Core/SaveModels.cs` | Add `DailyGoalState` to `ProfileSaveData` |
| `Run/DailyGoalService.cs` | **New.** Static service: `RefreshIfNewDay()`, `EvaluateInRun()`, `CompleteGoal()`, `GetTodayGoals()` |
| `UI/MainMenuBlueprintBuilder.cs` | Add Daily Walk widget panel in menu layout |
| `UI/MainMenuController.cs` | Wire `DailyGoalService` calls; apply pending rewards on `LaunchRun()` |
| `Bootstrap/GameBootstrap.cs` | Call `DailyGoalService.RefreshIfNewDay()` after profile load |
| `Run/RunDirector.cs` | Call `DailyGoalService.EvaluateInRun()` after puzzle complete, node arrive, gold change |

---

## Scope Boundaries

| In scope | Out of scope |
|----------|-------------|
| 3 daily goals, deterministic seed | Shared global leaderboard for daily goals |
| 21-goal pool, tier-weighted | Goal difficulty adjusting based on player class level |
| Streak counter and milestone bonuses | Streak protection / streak shields |
| Ink Stamps as cosmetic currency | Stamps purchasing gameplay advantages |
| Run-start gold/token rewards | Granting items or relics as daily rewards |
| Daily Walk widget on main menu | Push notifications / OS-level reminders |
