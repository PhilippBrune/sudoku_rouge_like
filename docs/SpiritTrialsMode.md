# Spirit Trials Mode

**Version:** 1.2 | **Date:** 2026-04-14 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.2 | 2026-04-14 | implemented | Added requirement IDs (TRIAL-UNLOCK, TRIAL-SESSION, TRIAL-TIER, TRIAL-SCORE, TRIAL-LEAD, TRIAL-RES). |
| 1.1 | 2026-03-23 | implemented | All tiers use 9×9 boards per spec, scoring system with base points + speed multiplier + bonuses, full 15-modifier pool, per-tier HP and pencil resources |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Spirit Trials is a **timed competitive mode** focused on speed-solving under constraint pressure. Unlike Garden Run (long-form roguelike) and Endless Zen (infinite relaxed progression), Spirit Trials offers short, high-intensity sessions with a scoring system designed for leaderboard competition.

---

## Unlock Condition

**[TRIAL-UNLOCK-001]** Spirit Trials is unlocked when `MetaProgressionState.SpiritTrialsUnlocked = true`. This flag is set via:
- Completing a full 5-floor Garden Run, OR
- Reaching Ascension Level 1

---

## Session Structure

**[TRIAL-SESSION-001]** Each Spirit Trials session is a **single puzzle** with fixed parameters:

| Parameter | Value |
|-----------|-------|
| Board Size | 9×9 (always) |
| Base Star Rating | 3★ (60% missing) |
| Region Variant | Random (0–3, respects irregular toggle) |
| Timer | Active — counts up from 0:00 |
| Gold | Disabled — no gold earned |
| Items | Limited — player starts with 1 random Normal item |
| XP | Disabled — no class XP earned |
| Relics | Disabled — no relic slot |
| Save & Resume | Disabled — session must be completed in one sitting |

**[TRIAL-SESSION-002]** Timer starts on first cell interaction (not on puzzle display).

---

## Modifier Selection

**[TRIAL-MOD-001]** Each Spirit Trials session includes **1 or 2 modifiers** selected from the full pool of 15:

### Difficulty Tiers

**[TRIAL-TIER-001]** The player chooses a difficulty tier before starting:

| Tier | Modifiers | Star Rating | Description |
|------|:---:|:---:|-------------|
| Apprentice | 0 | 3★ | Pure speed solve, no constraints |
| Adept | 1 (random) | 3★ | One modifier adds complexity |
| Master | 1 (random) | 4★ | Higher star + one modifier |
| Grandmaster | 2 (random) | 5★ | Maximum pressure |

**[TRIAL-TIER-002]** The player cannot choose which modifier(s) — they are assigned randomly (seeded). Board size restrictions apply (German Whispers/Killer Cages excluded if size < 7, but since Spirit Trials is always 9×9, all 15 are eligible).

---

## Scoring System

### Base Points

**[TRIAL-SCORE-001]** Base points formula:
```
BasePoints = CellsToFill × PointsPerCell
```

| Tier | Cells to Fill | Points per Cell | Max Base |
|------|:---:|:---:|:---:|
| Apprentice | ~49 (3★ 9×9) | 10 | 490 |
| Adept | ~49 (3★ 9×9) | 15 | 735 |
| Master | ~57 (4★ 9×9) | 20 | 1,140 |
| Grandmaster | ~65 (5★ 9×9) | 30 | 1,950 |

### Speed Multiplier

**[TRIAL-SCORE-002]** Speed multiplier formula:
```
SpeedMultiplier = max(0.5, 2.0 − (ElapsedSeconds / ParTime))
```

| Tier | Par Time | 2× Speed (≤0s) | 1× Speed | 0.5× Floor |
|------|:---:|:---:|:---:|:---:|
| Apprentice | 300s (5 min) | Instant | 300s | 600s+ |
| Adept | 420s (7 min) | Instant | 420s | 840s+ |
| Master | 540s (9 min) | Instant | 540s | 1080s+ |
| Grandmaster | 720s (12 min) | Instant | 720s | 1440s+ |

The multiplier interpolates linearly from 2.0 (instant solve) to 1.0 (par time) to 0.5 (double par time). It never drops below 0.5.

### Constraint Bonus

**[TRIAL-SCORE-003]** Active modifiers add a flat bonus to the final score:

| Modifier Count | Bonus |
|:---:|:---:|
| 0 | +0 |
| 1 | +100 |
| 2 | +300 |

### Mistake Penalty

**[TRIAL-SCORE-004]** Each mistake subtracts points and costs HP:

```
MistakePenalty = MistakeCount × PenaltyPerMistake
```

| Tier | Penalty per Mistake | HP |
|------|:---:|:---:|
| Apprentice | −20 | 5 |
| Adept | −30 | 4 |
| Master | −50 | 3 |
| Grandmaster | −75 | 2 |

HP = 0 ends the session. The puzzle is not completed and scores 0.

### Pencil Efficiency Bonus

If the player completes the puzzle using fewer pencil marks than a threshold:

```
PencilBonus = max(0, (PencilThreshold − PencilMarksUsed) × 5)
```

| Tier | Pencil Threshold | Max Bonus |
|------|:---:|:---:|
| All tiers | 30 marks | 150 |

Using 0 pencil marks = +150 bonus. Using 30+ marks = +0. This rewards decisive, confident play.

### Final Score Formula

**[TRIAL-SCORE-005]** Final score formula:
```
FinalScore = floor((BasePoints × SpeedMultiplier) + ConstraintBonus + PencilBonus − MistakePenalty)
```

**[TRIAL-SCORE-006]** Minimum score: 0 (never negative).

---

## Leaderboard Structure

### Per-Tier Leaderboards

Each difficulty tier has its own leaderboard:

| Leaderboard | Ranking By | Tiebreaker |
|-------------|-----------|------------|
| Apprentice | FinalScore (desc) | ElapsedTime (asc) |
| Adept | FinalScore (desc) | ElapsedTime (asc) |
| Master | FinalScore (desc) | ElapsedTime (asc) |
| Grandmaster | FinalScore (desc) | ElapsedTime (asc) |

### Personal Best Tracking

**[TRIAL-LEAD-001]** `ProfileStats` stores per-tier personal bests:
- Best score
- Best time
- Best no-mistake clear time
- Total sessions played per tier

### Seasonal / Monthly Leaderboard

**[TRIAL-LEAD-002]** When `SeasonalChallengeUnlocked = true` (after first ascension):
- A monthly seed is generated via `AscensionService.BuildMonthlySeed(year, month)`
- All players on that month's leaderboard solve the **same puzzle** (same seed, same modifiers)
- Seasonal leaderboard is separate from the main per-tier boards
- Resets on the 1st of each month

---

## Session Flow

```
1. Player selects Spirit Trials from Game Modes menu
2. Choose difficulty tier (Apprentice / Adept / Master / Grandmaster)
3. Puzzle generates (9×9, star per tier, random modifier(s), seeded)
4. Timer starts on first cell interaction (not on puzzle display)
5. Player solves under HP + Pencil + Timer pressure
6. On completion OR HP = 0:
   a. Calculate FinalScore
   b. Display results screen:
      - Final Score (large, animated counter)
      - Time taken
      - Mistakes made
      - Pencil marks used
      - Speed multiplier achieved
      - Personal best comparison (new record highlight)
   c. Update ProfileStats and leaderboard
7. Return to Spirit Trials menu (play again or exit)
```

---

## Resource Constraints

| Resource | Apprentice | Adept | Master | Grandmaster |
|----------|:---:|:---:|:---:|:---:|
| HP | 5 | 4 | 3 | 2 |
| Pencil | 15 | 12 | 10 | 8 |
| Items | 1 Normal | 1 Normal | 1 Rare | 1 Rare |
| Rerolls | 0 | 0 | 0 | 0 |

No shops, no gold, no item rewards. The starting item is the player's only tactical resource.

### Starting Item Mode

Before each Spirit Trial, the player chooses a **starting item mode**:

| Mode | Starting Item | Description |
|------|--------------|-------------|
| **Random** | 1 random item (Normal or Rare per tier) | Full RNG — tests adaptability |
| **Solver Start** | 1 Rare Solver | Guaranteed cell-fill tool — consistent strategic option |

Both modes are tracked on the same leaderboard (item mode is recorded but does not split rankings).

---

## Modifiers in Spirit Trials

All 15 modifiers are eligible (9×9 board meets all size requirements). Modifier assignment is random per session seed but deterministic — same seed always produces same modifier(s).

For Grandmaster tier (2 modifiers), the same spatial separation and layering strategy from [BossMechanicsSystem](BossMechanicsSystem.md) applies, including the modifier legend panel.

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Run/EndlessZenService.cs` | Reference architecture (Spirit Trials follows similar pattern) |
| `Assets/Scripts/Meta/AscensionService.cs` | Monthly seed generation for seasonal leaderboard |
| `Assets/Scripts/Core/RuntimeModels.cs` | `MetaProgressionState.SpiritTrialsUnlocked`, `ProfileStats` |
| `Assets/Scripts/Save/ProfileService.cs` | Stats recording, mode unlock evaluation |

---

## Steam Leaderboard Integration

### API Usage

Spirit Trials uses the Steamworks `SteamUserStats` API for global leaderboard ranking:

```
Initialization:
    SteamUserStats.FindOrCreateLeaderboard("spirit_apprentice",
        ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
        ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric)
    // Repeat for adept, master, grandmaster, seasonal_YYYY_MM

Score Upload:
    SteamUserStats.UploadLeaderboardScore(leaderboard,
        ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
        finalScore, details[])
    // details[] contains: elapsedTimeMs, mistakeCount, pencilMarksUsed, modifierBitmask

Score Download:
    SteamUserStats.DownloadLeaderboardEntries(leaderboard,
        ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, 100)
    // Also: k_ELeaderboardDataRequestGlobalAroundUser for player's rank context
```

### Leaderboard Names

| Leaderboard | Steam Name | Reset |
|-------------|-----------|-------|
| Apprentice | `spirit_apprentice` | Never |
| Adept | `spirit_adept` | Never |
| Master | `spirit_master` | Never |
| Grandmaster | `spirit_grandmaster` | Never |
| Seasonal | `spirit_seasonal_{YYYY}_{MM}` | New board each month |

### Display

The Spirit Trials results screen shows:
- **Personal rank** (global position)
- **Top 10** entries with names and scores
- **Nearby entries** (5 above and 5 below player's rank)
- **Friends leaderboard** (Steam friends only, if available)

---

## Anti-Cheat & Integrity

### Move Log Verification

Each Spirit Trial session records a **timestamped move log** stored alongside the score submission:

```
MoveLog entry: { timestamp_ms, row, col, value, action (place/pencil/undo) }
```

The move log is hashed (SHA-256) and submitted as part of the leaderboard `details[]` array. This enables:

1. **Replay verification:** The move log can be replayed against the seeded puzzle to verify the final board state matches the claimed score.
2. **Timing validation:** Move timestamps must be monotonically increasing and within plausible human speed (minimum 200ms between placements).
3. **Consistency check:** The puzzle seed + move log must produce the exact final score submitted.

### Server-Side Validation (Future)

For high-stakes seasonal leaderboards, server-side validation would:
1. Receive the move log + puzzle seed from the client
2. Replay all moves against a server-generated puzzle
3. Verify the final score matches
4. Reject entries that fail verification

For initial release, validation is client-side only. Server-side is planned for post-launch if competitive integrity issues arise.

### Flagging

Entries with suspicious patterns are flagged for review:
- Solve time < 30 seconds for any tier
- Zero pencil marks on Grandmaster (statistically improbable)
- Move timestamps with no variation (bot-like)

Flagged entries remain on the leaderboard but are marked with a review icon.

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
| REQ-RUN-075 | TITLE | `—` | — | ✗ missing |
