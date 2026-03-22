# Endless Zen Mode

**Version:** 1.0 | **Date:** 2026-03-21 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

Endless Zen is an **infinite relaxed progression mode** focused on sustained play without run failure pressure. Unlike Garden Run (5-floor roguelike) and Spirit Trials (timed competitive), Endless Zen offers an open-ended session where difficulty gradually increases through star progression and modifier stacking.

---

## Unlock Condition

Endless Zen is unlocked when `MetaProgressionState.EndlessZenUnlocked = true`. This flag is set when:
- `ProfileStats.TotalRuns ≥ 10` (evaluated in `ProfileService.RecordRunAndGetNewUnlocks()`)

---

## Session Structure

| Parameter | Value |
|-----------|-------|
| Board Size | 9×9 (always) |
| Difficulty | Diff5 (always) |
| Star Rating | Scales with depth: `Clamp(1 + depth/4, 1, 5)` |
| Timer | None |
| Gold | Minimal — no shop, no spending |
| Items | No item drops, no item rewards |
| Relics | No relic nodes |
| XP | No class XP earned |
| Save & Resume | No — session must be completed in one sitting |
| Run Failure | HP = 0 ends the session |

---

## Depth Progression

Each level is one puzzle. Depth starts at 0 and increments after each solved puzzle.

### Star Scaling

```
Stars = Clamp(1 + floor(depth / 4), 1, 5)
```

| Depth | Stars | Missing % | 9×9 Givens |
|:---:|:---:|:---:|:---:|
| 0–3 | 1★ | 40% | 49 |
| 4–7 | 2★ | 50% | 41 |
| 8–11 | 3★ | 60% | 33 |
| 12–15 | 4★ | 70% | 25 |
| 16+ | 5★ | 80% | 17 |

Stars cap at 5★ — no 6★ in Endless Zen.

### Modifier Scaling

Modifiers are added as depth increases, stacking constraint complexity:

| Depth | Modifier Cap | Effect |
|:---:|:---:|--------|
| 0–9 | 1 modifier | Single constraint overlay |
| 10–19 | 2 modifiers | Dual constraints, moderate complexity |
| 20+ | 3 modifiers | Triple constraints, maximum pressure |

Formula: `ModifierCap(depth) = depth < 10 ? 1 : depth < 20 ? 2 : 3`

---

## Modifier Pool

All **15 modifiers** are eligible in Endless Zen. Board size restrictions still apply, but since Endless Zen is always 9×9, all modifiers meet the minimum size requirement.

| # | Modifier | Type |
|:---:|----------|------|
| 1 | Parity Lines | Line |
| 2 | Difference Kropki | Dot |
| 3 | Dutch Whispers | Line |
| 4 | Renban Lines | Line |
| 5 | Ratio Kropki | Dot |
| 6 | Killer Cages | Arithmetic |
| 7 | Arrow Sums | Arithmetic |
| 8 | Fog of War | Visibility |
| 9 | German Whispers | Line |
| 10 | Palindrome | Line |
| 11 | Thermo | Line |
| 12 | Between Lines | Line |
| 13 | Even Odd | Cell-Level |
| 14 | Nonconsecutive | Global Negative |
| 15 | Antiknight | Global Negative |

Modifiers are selected randomly (seeded) per level without duplicates. A new set is rolled for each depth.

Nonconsecutive and Antiknight (no visible geometry) display a **text indicator in the HUD** so the player knows the global constraint is active.

---

## Level Generation

`EndlessZenService.BuildLevel(int depth, int seed)` generates each level:

1. Board size = 9, Difficulty = Diff5
2. Stars = `Clamp(1 + depth/4, 1, 5)`
3. MissingPercent via `StarDensityService.MissingPercentForStars(stars)`
4. IsBoss = false (no boss gates in Endless Zen)
5. Select `ModifierCap(depth)` random modifiers from pool (no duplicates)
6. Region variant = random (0–3, respects AllowIrregularPuzzles setting)

Seed is derived from the session seed + depth index for deterministic generation.

---

## Resources

| Resource | Value | Notes |
|----------|:---:|-------|
| HP | Class base HP | Starts at class default, no restoration between levels |
| Pencil | Class base Pencil | Replenished partially after each level (+3 per level) |
| Items | None | No item inventory, no item rewards |
| Relics | None | No relic slot |
| Gold | None | No gold economy |

### HP Attrition

HP attrition is the core session limiter. Between levels, the player receives **+1 HP** (flat, regardless of class). This prevents ultra-short sessions for low-HP classes while maintaining long-term pressure — even skilled players will eventually run out of HP.

Class passives that restore HP (e.g. Garden Monk's heal every 5 correct placements) still function within each puzzle.

---

## Scoring & High Score

### Score Formula

```
EndlessScore = DepthReached
```

The primary metric is **depth** (number of puzzles completed). Secondary metrics are tracked for display:

| Metric | Description |
|--------|-------------|
| Depth Reached | Primary score — number of puzzles completed |
| Total Correct Placements | Sum of all correct digit placements |
| Total Mistakes | Sum of all wrong placements |
| Session Duration | Total time from first puzzle to session end |
| Peak Star Rating | Highest star level reached |
| Peak Modifier Count | Most simultaneous modifiers faced |

### High Score Tracking

`ProfileStats.HighestEndlessDepth` stores the all-time best depth. This is updated in `ProfileService.RecordRunAndGetNewUnlocks()`.

### Achievements

| Achievement | Condition |
|------------|-----------|
| Deep Meditation | Reach Endless Zen depth 20 |
| Eternal Patience | Reach Endless Zen depth 50 |

---

## Session Flow

```
1. Player selects Endless Zen from Game Modes menu
2. Class selection (same as Garden Run — class passives apply)
3. First puzzle generates (9×9, 1★, 1 modifier)
4. Player solves puzzle
5. On completion:
   a. Depth increments
   b. +1 HP restored, +3 Pencil restored
   c. Brief depth counter animation
   d. Next puzzle generates with updated stars/modifiers
6. On HP = 0:
   a. Session ends
   b. Results screen:
      - Depth Reached (large display)
      - Session Duration
      - Mistakes Made
      - Personal best comparison
      - New high score highlight if applicable
   c. Update ProfileStats
7. Return to Game Modes menu
```

### No Pause-Save

Endless Zen has no save & resume. Closing the game or returning to menu ends the session. This is intentional — the mode rewards sustained focus in a single sitting.

---

## Differences from Garden Run

| Aspect | Garden Run | Endless Zen |
|--------|-----------|-------------|
| Floors | 5 fixed floors | Infinite depth |
| Path routing | Calm/Risk dual paths | Single linear progression |
| Boss gates | Yes (modifier choice) | No — modifiers assigned automatically |
| Items & relics | Full economy | Disabled |
| Gold & shops | Active | Disabled |
| XP rewards | Yes (per-tile) | No |
| HP healing | Between floors + items | +1 HP per level + class passives |
| Save & resume | Yes | No |
| Board sizes | 5×5 → 9×9 | 9×9 only |
| Star range | 1–6★ | 1–5★ |
| Session length | ~45–90 min | Until HP = 0 |

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Run/EndlessZenService.cs` | `BuildLevel()`, `ModifierCap()`, modifier pool |
| `Assets/Scripts/Core/RuntimeModels.cs` | `MetaProgressionState.EndlessZenUnlocked`, `ProfileStats.HighestEndlessDepth` |
| `Assets/Scripts/Save/ProfileService.cs` | Unlock evaluation, high score recording |
| `Assets/Scripts/Meta/SteamAchievementService.cs` | Endless-specific achievements |

---

## Steam Leaderboard

Endless Zen has a global Steam leaderboard for depth ranking, reusing the anti-cheat infrastructure from Spirit Trials.

### Leaderboard

| Leaderboard | Steam Name | Ranking | Tiebreaker |
|-------------|-----------|---------|------------|
| Endless Zen | `endless_zen_depth` | DepthReached (desc) | TotalMistakes (asc) |

### Score Submission

On session end:
1. Upload `DepthReached` as the leaderboard score
2. Attach `details[]`: sessionDurationMs, totalMistakes, totalCorrectPlacements, peakStarRating, peakModifierCount, classId
3. Upload method: `KeepBest` — only improves the player's entry if the new depth exceeds the previous

### Anti-Cheat

Reuses the Spirit Trials move log system:
- Each puzzle records a timestamped move log
- Move logs for the full session are hashed (SHA-256) and submitted with the score
- Replay verification: move logs can be replayed against seeded puzzles to verify depth
- Flagging: sessions with suspicious patterns (< 5s per puzzle, zero pencil on 5★) are flagged for review

### Display

The Endless Zen results screen shows:
- **Personal rank** (global position)
- **Top 10** entries
- **Nearby entries** (5 above, 5 below)
- **Friends leaderboard** (Steam friends)

---

## Open Issues

No open issues remaining.
