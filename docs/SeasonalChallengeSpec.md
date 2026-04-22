# Seasonal Challenge

**Version:** 0.1 | **Date:** 2026-04-09 | **Status:** new

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-04-09 | new | Initial specification |

---

## Overview

The **Monthly Walk** is a fixed monthly challenge: a single curated puzzle — same board, same modifiers, same rules — for every player in the same calendar month. Players aim for the highest run score by minimising mistakes and maximising efficiency. Personal best is stored locally; no network connection required.

The challenge uses the existing `BuildMonthlySeed(year, month)` infrastructure (`seed = year × 100 + month`). What was missing is prominence — the challenge should be surfaced as a primary action on the main menu, not buried in settings.

---

## Main Menu Placement

The **Monthly Walk** button is placed directly on the main menu beneath the normal play buttons, styled distinctly:

```
┌─────────────────────────────────────────────────────┐
│  [  New Run  ]    [ Resume Run ]                    │
│                                                     │
│  ══════════════════════════════════════════════════ │
│  🏮  Monthly Walk — April 2026                      │
│      "The Fountain Plaza"  ·  9×9 ·  4★ ·  EvenOdd │
│      Your best:  1 840 pts  ·  Resets in  22 days   │
│  [  Begin Monthly Walk  ]                           │
│  ══════════════════════════════════════════════════ │
│                                                     │
│  [ Tutorial ]  [ Options ]  [ Profiles ]  [ Quit ]  │
└─────────────────────────────────────────────────────┘
```

The panel shows:
- Month name and year
- The challenge's named theme line (see Floor Themes below)
- Board size, star rating, active modifier(s) — as text labels
- Player's personal best score for this month (or `"Not yet attempted"`)
- Countdown to the next month's reset
- A single `[Begin Monthly Walk]` button

---

## Challenge Structure

Each monthly challenge is **one puzzle** — not a full 5-floor run.

| Parameter | Value | Source |
|-----------|-------|--------|
| Board size | 9×9 | Fixed — hardest size |
| Star rating | 4★ | Fixed — challenging but solvable |
| Region variant | Derived from seed (`seed % 4`) | Varies by month |
| Active modifiers | 1–2 modifiers, derived from seed | Varies by month |
| Modifier intensity | High (fixed) | Fixed |
| HP | 10 | Fixed — no class modifiers |
| Pencil | 12 | Fixed — no class modifiers |
| No class passive | True | Fixed — same conditions for all players |
| Gold | 0 | Not relevant — no shop |

### Monthly Theme Names

The challenge is given a named theme each month, derived from `seed % 12`:

| Index | Theme Name |
|:---:|---|
| 0 | The Entrance Garden |
| 1 | The Pond Path |
| 2 | The Old Tree Grove |
| 3 | The Fountain Plaza |
| 4 | The Far Bench |
| 5 | Morning Mist |
| 6 | Autumn Colours |
| 7 | After Rain |
| 8 | The Stone Bridge |
| 9 | The Ice Cream Cart |
| 10 | Lantern Festival |
| 11 | The Iron Gate |

These are cosmetic — shown in the main menu panel and the challenge header. No gameplay effect.

### Modifier Selection

From the seed, 1 or 2 modifiers are selected from the standard boss modifier pool (excluding debuffs, ID < 94):

```csharp
var eligible = BossService.BuildEligiblePool(/* no exclusions */);
int count = (seed % 3 == 0) ? 2 : 1; // 1 modifier in 2/3 of months, 2 in 1/3
// Shuffle eligible list with seed; take first `count` entries
```

The active modifiers are shown on the main menu panel and the in-puzzle modifier info box (same as boss puzzles). The `sealed_eyes` curse does NOT apply here.

---

## Scoring

Score is calculated immediately after the puzzle ends (completion or HP=0).

### Formula

```
BasePoints     = (9 × 9) × 100                     // 8 100 — max if every cell filled correctly
MistakePenalty = mistakes × 300                     // -300 per wrong placement
PencilBonus    = (PencilRemaining / MaxPencil) × 500 // up to +500 for pencil conservation
PerfectBonus   = (mistakes == 0) ? 1 000 : 0        // +1 000 for a perfect solve

RawScore = BasePoints − MistakePenalty + PencilBonus + PerfectBonus
FinalScore = max(RawScore, 0)
```

*Design rationale:* No time pressure — consistent with the game's identity. The skill expression comes from minimising mistakes, conserving pencil, and ideally solving without any errors.

### Score Grade

A letter grade is shown alongside the numeric score:

| Grade | Threshold |
|-------|-----------|
| S | FinalScore ≥ 8 500 |
| A | 6 500 – 8 499 |
| B | 4 500 – 6 499 |
| C | 2 500 – 4 499 |
| D | 1 – 2 499 |
| — | Did not complete (HP = 0) |

---

## Result Screen

After the challenge puzzle ends, a dedicated **Monthly Walk Result** screen is shown (replaces the normal end screen for this mode):

```
Monthly Walk — April 2026
"The Fountain Plaza"  ·  9×9 ·  4★ ·  EvenOdd (High)

─────────────────────────────────────
  Cells filled correctly:    76 / 81
  Mistakes made:                   2
  Pencil remaining:            4 / 12
  Perfect bonus:                   —
─────────────────────────────────────
  Score:  8 100 − 600 + 167 + 0 = 7 667      Grade: A
─────────────────────────────────────

  ★ Personal Best: 7 667  (new record!)
  Previous best:   6 230   (March 2026)

─────────────────────────────────────

  [ Play Again ]         [ Main Menu ]
```

- `[Play Again]` relaunches the exact same challenge (same seed — same board). Replays do not overwrite personal best — only improvements save.
- XP and gold are **not awarded** for Monthly Walk runs (it is not a standard run).

---

## Replay Rules

- The Monthly Walk can be replayed **unlimited times** within the month.
- Only the player's highest score for that month is stored.
- The challenge may be attempted with any class, but **class passives do NOT apply** — the challenge uses fixed HP/Pencil values.
- Curse effects, relic effects, and item effects do not apply — the puzzle starts clean each time.

---

## Countdown Display

The "Resets in X days" counter is computed at main menu display time:

```csharp
var today = LocalDate.Today;
var firstOfNext = new LocalDate(today.Year, today.Month, 1).AddMonths(1);
int daysLeft = (firstOfNext - today).Days;
string label = daysLeft == 1 ? "Resets in 1 day" : $"Resets in {daysLeft} days";
```

On the final day of the month the label reads `"Resets tomorrow"`. On the first day of a new month the panel updates immediately to show the new challenge on next main menu load.

---

## Data Model

### `SeasonalChallengeState` (new, in `SaveModels.cs` → `ProfileSaveData`)

```csharp
[Serializable]
public sealed class SeasonalChallengeState
{
    // Key: "YYYY-MM" (e.g. "2026-04")
    public List<string> BestScoreKeys   = new List<string>();
    public List<int>    BestScoreValues = new List<int>();
    // Parallel lists used instead of Dictionary for JsonUtility compatibility

    public int GetBest(int year, int month)
    {
        var key = $"{year:D4}-{month:D2}";
        var idx = BestScoreKeys.IndexOf(key);
        return idx >= 0 ? BestScoreValues[idx] : 0;
    }

    public void SetBest(int year, int month, int score)
    {
        var key = $"{year:D4}-{month:D2}";
        var idx = BestScoreKeys.IndexOf(key);
        if (idx >= 0) { if (score > BestScoreValues[idx]) BestScoreValues[idx] = score; }
        else { BestScoreKeys.Add(key); BestScoreValues.Add(score); }
    }
}
```

Only the current and previous month's scores are needed for display; older entries can accumulate harmlessly (max 12 entries per year).

### `RunState` — Monthly Walk flag

```csharp
public bool IsSeasonalChallenge; // suppresses XP/gold awards, class passives, relics, curses
```

---

## `SeasonalChallengeService` (new)

```csharp
public static class SeasonalChallengeService
{
    // Returns the seed for the given year+month
    public static int BuildSeed(int year, int month) => year * 100 + month;

    // Builds the fixed LevelConfig for the current month
    public static LevelConfig BuildChallengeConfig(int year, int month);

    // Builds the RunState for a monthly walk attempt
    public static RunState CreateChallengeRunState(int year, int month);

    // Calculates and returns the final score
    public static int CalculateScore(int cellsCorrect, int mistakes, int pencilRemaining, int maxPencil);

    // Returns the grade string ("S", "A", "B", "C", "D", or "—")
    public static string GetGrade(int score, bool completed);
}
```

`BuildChallengeConfig` uses the seed to derive board size (fixed 9×9), region variant (`seed % 4`), modifier count (`seed % 3 == 0 ? 2 : 1`), and modifier list (seeded shuffle of eligible pool).

`CreateChallengeRunState` returns a RunState with:
```
Mode = GameMode.SeasonalChallenge
IsSeasonalChallenge = true
CurrentHP = MaxHP = 10
CurrentPencil = MaxPencil = 12
CurrentGold = 0
DisableProgressionRewards = true
RunNumber = -1  // excluded from class progression
```

---

## Implementation Notes

| File | Change |
|------|--------|
| `Core/SaveModels.cs` | Add `SeasonalChallengeState` to `ProfileSaveData` |
| `Core/GameEnums.cs` | Add `GameMode.SeasonalChallenge` |
| `Core/RuntimeModels.cs` | Add `bool IsSeasonalChallenge` to `RunState` |
| `Run/SeasonalChallengeService.cs` | **New.** Static class: seed, config build, run state, scoring |
| `UI/MainMenuBlueprintBuilder.cs` | Add Monthly Walk panel (separator + theme label + stats + button) |
| `UI/MainMenuController.cs` | Add `LaunchSeasonalChallenge()`, `GetSeasonalBestScore()`, `UpdateSeasonalPanel()` |
| `UI/EndScreenViewController.cs` | Detect `IsSeasonalChallenge`; show Monthly Walk result layout instead of standard |
| `Run/RunDirector.cs` | When `IsSeasonalChallenge`: suppress XP commit, gold award, class passive, relic/item effects at puzzle start |
| `Bootstrap/GameBootstrap.cs` | Wire `MainMenuController.LaunchSeasonalChallenge()` button handler |
| `Save/SaveFileService.cs` | Serialize/deserialize `SeasonalChallengeState` in profile save |

---

## Open Questions

- Should completing the Monthly Walk with an S grade award Ink Stamps? (Suggested: S grade = +2 Stamps, first-time completion = +1 Stamp regardless of grade.)
- Should the previous month's challenge remain playable after reset, or lock immediately? (Suggested: lock immediately — scarcity gives urgency.)
- Should the theme name be shown on the in-puzzle HUD, or only on the result screen?

---

## Scope Boundaries

| In scope | Out of scope |
|----------|-------------|
| Single monthly puzzle, deterministic seed | Online leaderboard / server comparison |
| Personal best stored per month | Cross-profile comparison |
| Grade system (S/A/B/C/D) | Animated grade reveal |
| Replay unlimited within month | Replay of previous months after reset |
| Prominent main menu placement | Push notifications / calendar integration |
| No class passives / relics / curses | Per-class monthly high score variants |
