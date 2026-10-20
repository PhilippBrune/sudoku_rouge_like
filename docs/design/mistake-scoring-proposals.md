# Proposals: how mistakes count toward the run score

**Version:** 1.0 | **Date:** 2026-10-20 | **Status:** new (proposal for the owner; the run score stays as GameDesignSpec §9c says until one is picked)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-20 | new | Owner's 0.2.0 playtest: "only the mistakes of the last puzzle count; every mistake should count, with a fitting weight for when it happens". The counting bug is fixed (GAP-059); this document proposes the weighting. Roadmap item RM-109. |

## Where it stands

- **Fixed (GAP-059):** every mistake of the run now counts the moment it happens. Before, mistakes reached the total only when a puzzle was solved, so the puzzle that ended a run (and every Endless Zen session's last board) was missing.
- **Today's rule (GameDesignSpec §9c):** each mistake takes 2 % of the base XP off the run score, at most 50 %. A mistake on a 5×5 one-star board weighs as much as one on a 9×9 five-star boss board.

## Proposals

### A · Weight by the puzzle's difficulty

A mistake costs less on a harder board: `2 % × weight`, weight from the board's stars (1★ ×1.5, 2★ ×1.25, 3★ ×1, 4★ ×0.75, 5★+ ×0.5). Boss boards count as one star harder.

- For: fair; an early careless slip costs more than a mistake under real pressure.
- Against: the rule is harder to read on the end screen.

### B · Weight by the floor

Mistakes on Floor 1 cost 3 %, Floor 2 2.5 %, Floor 3 2 %, Floor 4 1.5 %, Floor 5 1 %.

- For: simple to explain ("early floors should be clean").
- Against: ignores that some Floor 1 puzzles are hard and some Floor 5 puzzles are easy.

### C · Per-puzzle grades (recommended)

Each solved puzzle gets a grade from its own mistakes, and the grades replace the single mistake row:

| Grade | Mistakes on that puzzle | Run-score effect |
|---|---|---|
| Perfect | 0 | +5 % (as today's perfect bonus) |
| Clean | 1 | 0 |
| Rough | 2–3 | −2 % |
| Messy | 4+ | −5 % |

The puzzle that ends a run counts as Messy. The end screen shows a row of stamps, one per puzzle in order, so a player sees *when* the run went wrong: "Floor 3 was rough, the boss was perfect".

- For: every mistake counts, but one bad puzzle cannot sink a whole run; the result tells a story; it merges the perfect bonus and the mistake penalty into one idea.
- Against: a bigger change to the end screen; the cap (−50 %) is needed again for very long runs.

### D · Weight by the moment inside a puzzle

The first mistake on a board costs 1 %, every further one on the same board 3 %.

- For: forgives a single slip, punishes guessing.
- Against: invisible during play unless the HUD shows it.

## Example (the same run under each rule)

Base XP 400; six puzzles with 0, 1, 0, 3, 0, 2 mistakes, the 3 on a 3★ Floor 2 board, the 2 on the 4★ Floor 3 boss.

| Rule | Mistake effect | Run score |
|---|---|---|
| Today (−2 % per mistake) | −12 % | 400 × (1 + 0.15 perfect − 0.12) = 412 |
| A · Difficulty | −2 % × (1 + 3 × 1 + 2 × 0.5) = −10 % | 420 |
| B · Floor | −(2.5 + 3 × 2.5 + 2 × 2) % = −14 % | 404 |
| C · Grades | Perfect ×3 +15 %, Clean 0, Rough −2 %, Rough −2 % | 400 × 1.11 = 444 |
| D · Moment | −(1 + 1 + 3 + 3 + 1 + 3) % = −12 % | 412 |

## Decision for the owner

Pick A, B, C, D or a mix (C with A's star weights, for example). The chosen rule goes into GameDesignSpec §9c with requirement IDs and replaces `RunScoreBreakdown`'s mistake row; the end-screen ledger and its tests follow.
