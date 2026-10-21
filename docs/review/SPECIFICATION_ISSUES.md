# Specification Issues

**Review date:** 2026-10-01 · **Scope:** all `docs/*.md` (52 files, ~8,800 lines) plus `REQUIREMENT_MAP.md`
**Resolution pass:** 2026-10-07, release 1.1.0 ([status](#0-resolution-status-release-110)) · **Companion docs:** [FULL_GAME_REVIEW](FULL_GAME_REVIEW.md) · [GAME_GAPS](GAME_GAPS.md) · [REQUIREMENT_TRACEABILITY](REQUIREMENT_TRACEABILITY.md) · [E2E_TEST_REPORT](E2E_TEST_REPORT.md)

No conflict was resolved silently. Each entry names the documents involved, their version and date, what each one says, what the code does, which source *appears* current, and a recommendation. **The decisions are yours.** Where code follows neither document, a code change or a spec update is needed either way.

Each contradiction ends with two **Suggestions** (added 2026-10-05). The one marked *(recommended)* is a proposal with its reasoning, not a decision; nothing in the code or specs was changed to follow it. **Update 2026-10-07:** for release 1.1.0 the owner asked to implement every recommendation; each entry now ends with a **Resolution (1.1)** line naming what changed and the evidence.

Recency used for "appears current": the version/date header, then the change-history date, then whether the newer document explicitly supersedes the older one. Several 2026-04-27 "v2.0" documents say they were rewritten "to match the current implementation", which makes them descriptive rather than normative (see [SPEC-PROC-002](#spec-proc-002)).

Contents: [Contradictions](#1-contradictions) · [Ambiguities](#2-ambiguous-requirements) · [Obsolete requirements](#3-obsolete--outdated-requirements) · [Missing acceptance criteria](#4-missing-acceptance-criteria) · [Process and documentation hygiene](#5-process-and-documentation-hygiene)

---

## 0. Resolution status (release 1.1.0)

| Group | Entries | Resolved in 1.1 | Open |
|---|---:|---:|---|
| Contradictions | 41 | 41 | SPEC-CONF-039 decided 2026-10-20, SPEC-CONF-041 on 2026-10-21. Owner sign-offs requested: SPEC-CONF-008 balance values, SPEC-PROC-007 legal wording. |
| Ambiguities | 14 | 14 | Listening check for SPEC-AMB-011 (GAP-030). |
| Obsolete requirements | 13 rows | 13 | — |
| Missing acceptance criteria | 11 | 9 | SPEC-MAC-005 (deferred, FUTURE-011), SPEC-MAC-011 (profiler session). |
| Process | 7 | 5 | SPEC-PROC-002 partly; SPEC-PROC-006 needs nothing. |

Where a recommendation was doc-only but the matching gap asked for code (SPEC-CONF-021 / GAP-026), the code was changed and the spec documents it. Deferred work is recorded in `docs/future-scope-decision-register.md` (FUTURE-009..016).

## 1. Contradictions

### SPEC-CONF-001
**Floor modifier count per floor**
- **CONFLICT:** BOSS-FLOOR-001 (BossMechanicsSystem v2.1/2.2, 2026-04-27; GameDesignSpec v1.5 §10; PathSystem v1.5 §Floor Modifier Activation) vs code.
- All three specs: Floors 1–5 → **0, 1, 2, 3, 4** floor modifiers ("Floor N has N−1").
- Code (`BossService.BaseFloorModifierCounts`): **0, 1, 1, 1, 2**. The comment in code is tagged `[REQ: BOSS-FLOOR-001]`.
- Likely current: specs (no document records the rebalance). The code values look like a deliberate rebalance for generation budget.
- **Recommendation:** Decide. If 0/1/1/1/2 is intended, bump all three specs and add a change-history entry; otherwise change the code. → [GAP-012](GAME_GAPS.md#gap-012)
- **Suggestions:**
  1. **Keep the code's 0, 1, 1, 1, 2 and update BOSS-FLOOR-001 in BossMechanicsSystem, GDS §10 and PathSystem.** *(recommended)* No code change. Difficulty still rises through stars, board size and boss picks, and generation load stays where it was tested.
  2. **Implement the specs' 0, 1, 2, 3, 4 in `BossService.BaseFloorModifierCounts`.** Floors 4–5 then carry 3–4 floor modifiers plus boss picks (up to 9 on the final boss). Do this only after the GAP-003 generation work, and re-run the late-floor generation matrix.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Code kept (0/1/1/1/2); BOSS-FLOOR-001 updated in BossMechanicsSystem 2.3, GDS 1.6 §10 and PathSystem 1.6. Evidence: `HarmonyEffectsTests.BossOptionsAndFloorBonus_MatchSpecTable`.

### SPEC-CONF-002
**Boss options shown / picks per floor**
- **CONFLICT:** REQ-BOSS-100 table (BossMechanicsSystem §Choice Scaling, PathSystem §Choice Scaling: F1 3/2, F2 3/2, F3 4/3, F4 5/4, F5 6/5; "N+1 options (min 3), picks N (min 2)") vs GameDesignSpec §10 ("Floor N → pick N from N+1", no minimum, F1 = 1 of 2) vs BossMechanicsSystem v1.3 history ("Floor N offers N+2, player picks N+1 (0-based)").
- Code: `floor0+2` shown / `floor0+1` picks (F1 1-of-2). The comment claims "min 3 / min 2", but the minimum isn't applied.
- Likely current: the table (repeated in two specs). The code matches the older v1.3 rule.
- **Recommendation:** Pick one rule, update `GetBossModifierCounts` or the table, and remove the min-clause wording that disagrees. → [GAP-012](GAME_GAPS.md#gap-012)
- **Suggestions:**
  1. **Keep the code rule (floor+2 shown, floor+1 picked: 2/1, 3/2, 4/3, 5/4, 6/5) and rewrite the tables in BossMechanicsSystem, PathSystem and GDS §10.** *(recommended)* The Floor 1 boss stays gentle (one modifier), which fits GDS §15 "low difficulty in first runs". Drop the "min 3 / min 2" wording.
  2. **Adopt the spec table (F1 3/2 … F5 6/5, minimum 3 shown and 2 picked) in `GetBossModifierCounts`.** Every boss gets one more modifier on Floor 1, and the gate always excludes exactly one option. Update the Harmony bonus rules (SPEC-CONF-025) at the same time.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Floor N shows N + 1 options and picks N (2/1, 3/2, 4/3, 5/4, 6/5), plus the Harmony bonus; tables rewritten in BossMechanicsSystem 2.3, PathSystem 1.6, GDS 1.6; the "min 3 / min 2" wording and code comment removed (GAP-012). Evidence: `HarmonyEffectsTests`, `BalanceBudgetContractTests`.

### SPEC-CONF-003
**What counts as a wrong placement**
- **CONFLICT:** GEN-VALID-001 (REQUIREMENT_MAP: "Placement valid if satisfies all constraints (not compared to solution)") vs BossMechanicsSystem §Validation Path ("`RunDirector.PlaceNumber()` checks `Solution[row,col] == value`") vs GameDesignSpec §5.1 ("Wrong number placement: −1 HP"), plus SudokuGenerationPipeline §Solvability ("any placement matching the stored solution is accepted").
- Code: rule-based, so any digit that doesn't conflict with placed digits is "Correct"; the fog path compares with the solution; `SudokuBoard.IsComplete` accepts any valid completion.
- Likely current: unclear. The code follows GEN-VALID-001, and three specs say otherwise.
- **Recommendation:** Define one rule. Note that solution-based correctness requires unique puzzles. *Correction (2026-10-05):* D02 measured `SudokuGenerator.CreatePuzzle`, which the game never calls. Every in-game puzzle comes from `CreatePuzzleWithUniquenessCheck`, which removes a clue only while the base-rule solution stays unique, so run, mode and boss puzzles are unique; only the Custom Puzzle 7★ empty grid is not. → [GAP-011](GAME_GAPS.md#gap-011)
- **Suggestions:**
  1. **Solution-based: any digit that differs from the stored solution is wrong and costs 1 HP, even without a visible conflict.** *(recommended)* This needs every puzzle to have exactly one solution. Run puzzles already remove clues only while base-rule uniqueness holds (`CreatePuzzleWithUniquenessCheck`), so the remaining work is the Custom Puzzle 7★ path. The fog path already compares against the solution. Matches GDS §5.1 and BossMechanicsSystem.
  2. **Rule-based (current code): a digit is wrong only when it conflicts with placed digits or active modifier rules, and any valid completion wins.** Rewrite GDS §5.1 ("conflicting number") and BossMechanicsSystem §Validation Path, and align the fog path. Players get less immediate feedback and can paint themselves into a corner without an HP cost.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. A digit that differs from the solution is wrong: it is rejected and costs HP (Harmony cost). Custom Puzzle 7★ boards stay rule-based. GDS 1.6, BossMechanicsSystem 2.3, SudokuGenerationPipeline 1.6, TutorialModeSystem 1.9 (GAP-011, GAP-035). Evidence: `PlacementRulesTests`.

### SPEC-CONF-004
**One relic or many**
- **CONFLICT:** RELIC-SLOT-001 (REQUIREMENT_MAP "Single relic slot: `HasRelic + HeldRelic`"; also stated in project memory) vs ItemsAndRelicsSystem v2.0 (2026-04-27: "multi-relic, not single-slot"; "no hard single-relic runtime cap") and GameDesignSpec §7 ("`HeldRelics` … multiple relics"). RELIC-SLOT-002 says "currently max 1 entry enforced by game logic".
- Code: multi-relic list (starting relics can be 2 for Koi Gambler; relic choices add more).
- Likely current: ItemsAndRelicsSystem v2.0.
- **Recommendation:** Mark RELIC-SLOT-001 deprecated; fix RELIC-SLOT-002's "max 1" note.
- **Suggestions:**
  1. **Deprecate RELIC-SLOT-001 and keep the multi-relic list (code, ItemsAndRelicsSystem v2.0, GDS §7).** *(recommended)* Fix RELIC-SLOT-002's "max 1 entry" note, and decide whether there is a cap (for example none, or 3).
  2. **Enforce a single relic slot again: accepting a relic replaces the held one.** Needs a replace/keep dialog, changes Koi Gambler (2 starting relics), and invalidates saves holding more than one relic.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. RELIC-SLOT-001 deprecated in REQUIREMENT_MAP; multi-relic without a cap documented in ItemsAndRelicsSystem 2.1 and GDS 1.6.

### SPEC-CONF-005
**Shop contents**
- **CONFLICT:** ECON-SHOP-001 (REQUIREMENT_MAP and memory: "3 items + optional relic per shop") vs ItemsAndRelicsSystem v2.0 / GoldEconomySystem v2.0 ("Shops sell items only"; "floor-based slot counts") vs GameDesignSpec §7 ("Relics … not sold in the Shop").
- Code: 2/2/3/3/4 items for floors 1–5, no relic (D11, E15).
- Likely current: v2.0 documents.
- **Recommendation:** Update ECON-SHOP-001 to the floor table; record slot counts in GoldEconomySystem.
- **Suggestions:**
  1. **Shops sell items only, 2/2/3/3/4 slots on floors 1–5 (code). Update ECON-SHOP-001 and add the slot table to GoldEconomySystem.** *(recommended)* No code change; matches GDS §7 ("relics are not sold").
  2. **Add one optional relic offer per shop (original ECON-SHOP-001).** Needs relic pricing per tier and an exception in GDS §7. It also competes with boss relic choices as the relic source.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Items only, 2/2/3/3/4 slots; ECON-SHOP-001 updated, slot table in GoldEconomySystem 2.1 and ItemsAndRelicsSystem 2.1. Evidence: `SpecAlignmentTests.Shop_OffersTwoThreeOrFourItemsByFloor`.

### SPEC-CONF-006
**Endless Zen rules**
- **CONFLICT:** GameDesignSpec v1.5 §13 ("Unlocked after completing a full 5-floor run"; "All board sizes"; "Depth ≥ 10: 2 random modifiers"; "XP leaderboard focus") vs EndlessZenMode v2.0 (2026-04-27: unlock at `TotalRunsStarted ≥ 10`, always 9×9, caps 1/2/3 at depth 0–9/10–19/20+, XP disabled) and MetaProgressionSystem (`TotalRuns ≥ 10`).
- Code: matches EndlessZenMode v2.0 (D22).
- Likely current: EndlessZenMode v2.0.
- **Recommendation:** Update GDS §13. Also decide whether "runs started" (including abandoned runs) is the intended unlock metric.
- **Suggestions:**
  1. **EndlessZenMode v2.0 is the source (unlock at 10 runs started, always 9×9, modifier caps 1/2/3, XP disabled). Reduce GDS §13 to a link.** *(recommended)* Also decide whether abandoned runs count toward "runs started".
  2. **Implement the GDS rules: unlock after a full 5-floor run, all board sizes, 2 modifiers from depth 10, XP leaderboard.** Code changes in `EndlessZenService` and `ProfileService`. The leaderboard would reuse the Steam leaderboard service.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. EndlessZenMode 2.1 is the source; GDS §13 is a link. Metric: runs counted at the run result, so abandoned runs count (they are recorded as defeats, GAP-032). Evidence: `SpecAlignmentTests.EndlessZen_UnlocksWithTheTenthRecordedRun`.

### SPEC-CONF-007
**Spirit Trials unlock**
- **CONFLICT:** TRIAL-UNLOCK-001 (REQUIREMENT_MAP: "full 5-floor Garden Run OR Ascension Level 1") vs SpiritTrialsMode v2.0 ("after the player has defeated at least one boss") vs MetaProgressionSystem §Mode Unlocks ("set via ascension or achievement").
- Code: `ProfileService` sets `SpiritTrialsUnlocked`; ascension does not exist (SPEC-CONF-026).
- **Recommendation:** One rule, in one document; update the map row.
- **Suggestions:**
  1. **Unlock after the first boss defeat (SpiritTrialsMode v2.0). Update TRIAL-UNLOCK-001 and MetaProgressionSystem.** *(recommended)* Reachable in a first session and needs no Ascension. Check that `ProfileService` uses exactly this trigger.
  2. **Unlock after a full 5-floor Garden Run (REQUIREMENT_MAP), dropping the Ascension alternative until Ascension exists.** Makes Spirit Trials a late-game reward. Update SpiritTrialsMode v2.0 and the unlock code.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. First boss defeat, confirmed in `ProfileService`; SpiritTrialsMode 2.1, MetaProgressionSystem 1.4, TRIAL-UNLOCK-001 updated. Evidence: `SpecAlignmentTests.SpiritTrials_UnlockWithTheFirstBossDefeat`.

### SPEC-CONF-008
**Class roster: base stats, passives, unlock condition**
- **CONFLICT:** GameDesignSpec v1.5 §6.1 (2026-04-07) vs ClassXpProgressionSystem v1.2 (2026-04-15) vs code (`ClassCatalog`).

| Class | GDS v1.5 | ClassXp v1.2 | Code |
|---|---|---|---|
| Reed Duelist base | HP 9 / P 9 / S 2 | HP 8 / P 7 / S 2 / +1 reroll | HP 9 / P 9 / S 2 |
| Quiet Cartographer base | HP 12 / P 6 / S 1 | HP 9 / P 10 / S 2 / +1 reroll | HP 12 / P 6 / S 1 |
| Quiet Cartographer unlock | clear a stage, no pencil, no HP loss | same | text: "full run (all 5 floors + boss) with zero mistakes" |
| Shrine Archivist passive | first pencil per cell free | same | 25 % chance a pencil mark auto-reveals the digit |
| Koi Gambler passive | 25 % no-HP / 25 % +1 gold | same | 33 % / +3 gold, 2 starting relics |
| Lantern Seer passive | 20 % weaker | 20 % (25 % @L10, 30 % @L25) | 30 % weaker + preview + free reroll |
| Number Freak passive | +1 reroll token (balanced) | — | +1 reroll token after a zero-mistake puzzle |
| Level unlocks (NF L10) | — | HP 14 / Pencil 13 | HP 15 / Pencil 12 (D07) |

- Likely current: ClassXp v1.2 is newer than GDS, but the code follows GDS for stats and neither for passives.
- **Recommendation:** Make ClassXpProgressionSystem the single source for classes, reduce GDS §6 to a link, then align `ClassCatalog`. → [GAP-023](GAME_GAPS.md#gap-023)
- **Suggestions:**
  1. **The code is the source: document `ClassCatalog`'s current stats and passives (Koi 33 % / +3 gold / 2 relics, Lantern 30 % + preview, …) in ClassXpProgressionSystem, and reduce GDS §6 to a link.** *(recommended)* No gameplay change; needs a balance sign-off on the documented values. The level schedule (Number Freak L10 15/12) is documented as it is.
  2. **ClassXpProgressionSystem v1.2 is the source: change `ClassCatalog`, `ClassPassiveService` and `RunArchetypeService` to its stats, passives and level schedule.** A balance change for Reed Duelist, Quiet Cartographer and five passives, so existing players' classes play differently.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. ClassXpProgressionSystem 1.3 generated from `ClassCatalog`; GDS §6 is a link; "base (+bonus)" display (GAP-023). Found and fixed on the way: GAP-041. **Owner action:** balance sign-off on the documented values.

### SPEC-CONF-009
**Floor themes and names**
- **CONFLICT:** PathSystem v1.5 §Floor Themes (Cherry Blossom Garden, Bamboo Forest, Zen Rock Garden, Koi Pond Garden, Mountain Temple) vs GameDesignSpec §10 and BossMechanicsSystem §Floor Progression Arc (Bamboo Courtyard, Moss Garden, Koi Terrace, Stone Lantern Walk, Shrine Summit).
- Code: `FloorThemeData.FloorNames` uses the GDS names (E05/E19).
- **Recommendation:** Update PathSystem.
- **Suggestions:**
  1. **Keep the code/GDS names (Bamboo Courtyard, Moss Garden, Koi Terrace, Stone Lantern Walk, Shrine Summit) and update PathSystem.** *(recommended)* Doc-only; matches backgrounds, narrative and localization.
  2. **Rename the floors to PathSystem's themes (Cherry Blossom Garden …).** Touches `FloorThemeData`, `ParkNarrativeService`, all five language catalogs and possibly background art.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. PathSystem 1.6 uses the code/GDS floor names.

### SPEC-CONF-010
**Cross-connections between lanes**
- **CONFLICT:** MAP-CROSS-001 (PathSystem v1.5: "exactly one cross-connection … the graph does not allow multiple crossings"; "Cross-link tiles retain their original type") and GameDesignSpec §10 ("One cross-edge per floor") vs code.
- Code: `RunGraphService.InsertCrossLinks` creates `floor+1` bridges (2–8 cross-link nodes, D06) and overwrites the node type with `NodeType.CrossLink` ("Bridge" tile with no puzzle).
- Likely current: unclear, since the code was clearly redesigned.
- **Recommendation:** Decide, then update PathSystem (anchor rules, visuals, reachability) or the code. → [GAP-022](GAME_GAPS.md#gap-022)
- **Suggestions:**
  1. **Keep the code design (floor+1 bridge nodes of type `CrossLink`, no puzzle) and rewrite MAP-CROSS-001 in PathSystem and GDS §10.** No code change. The Calm/Risk lane choice becomes less binding, because players can switch several times.
  2. **Restore the spec: exactly one cross-connection per floor, and tiles keep their type.** *(recommended)* Change `RunGraphService.InsertCrossLinks`. Graphs change for every seed, so version the floor graph in saved runs (regenerate only on a new floor).
- **Resolution (1.1, 2026-10-07):** Suggestion 2 (implemented). Exactly one cross-link per floor between the middle-lane tiles; tiles keep their type; `FloorGraphVersion` keeps 1.0 saves on their old layout until the next floor (GAP-022). Evidence: `RunGraphTests`, `InRunUiPlayModeTests.PathMap_*`.

### SPEC-CONF-011
**Line adjacency (4- vs 8-directional)**
- **CONFLICT inside BossMechanicsSystem:** §Line Constraints ("orthogonally adjacent (4-directional) … Diagonal adjacency is not yet implemented"; v1.9 correction) vs §Geometry Generation and §Self-Crossing Detection ("All line generators use 8-directional adjacency").
- **Recommendation:** Finish the v1.9 correction across the document.
- **Suggestions:**
  1. **State 8-directional adjacency throughout BossMechanicsSystem and remove the v1.9 "diagonal not yet implemented" sentence.** *(recommended)* Matches the code: every line generator steps through `ModifierGeometryGenerator.Dirs`, which includes diagonals.
  2. **Restrict lines to 4-directional steps, as the v1.9 correction says.** Changes line geometry for every seed, and diagonal shapes (some thermos, palindromes) disappear.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. BossMechanicsSystem 2.3 states 8-directional adjacency throughout.

### SPEC-CONF-012
**Entropy minimum board size**
- **CONFLICT inside BossMechanicsSystem:** Extended pool table ("Min Size 7×7"), extended library (7×7) vs §Floor Modifier Rolling and §Modifier Eligibility ("Entropy requires exactly 9×9").
- Code: 9.
- **Recommendation:** State 9×9 consistently.
- **Suggestions:**
  1. **State 9×9 as the Entropy minimum everywhere (matches the code).** *(recommended)* Entropy groups the digits 1–3/4–6/7–9, which only works with nine digits.
  2. **Support Entropy on 7×7 and 8×8 with redefined digit groups.** Needs new rule semantics, generator support and tests, for little gameplay value.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. 9×9 everywhere (BossMechanicsSystem 2.3, SudokuGenerationPipeline 1.6).

### SPEC-CONF-013
**Modifier minimum board sizes**
- **CONFLICT:** BossMechanicsSystem v2.2 (only Dutch Whispers, Anti-Bishop, Distance ≥ 2, Full Kropki, Fortress need 6×6; German Whispers and Killer Cages 7×7; all others any size) vs `BossService.ModifierData`.
- Code differences: Nonconsecutive 6, Antiknight 6, Palindrome 6, Arrow Sums 6, Thermo 6, Between Lines 6, Antiking 6, Nonconsec. Diagonal 6, Consecutive Line 6, Slow Thermo 6, Modular Regions 7, Anti-Bishop **9**. Modular Regions is also excluded from all pools "by design decision" (not in any spec). Undocumented blocked pairs: RatioKropki+DistanceGe2, DistanceGe2+NonconsecDiagonal.
- **Recommendation:** Copy the code table into the spec with reasons, or change the code. → [GAP-012](GAME_GAPS.md#gap-012)
- **Suggestions:**
  1. **Copy the code table (`BossService.ModifierData`) into BossMechanicsSystem with the reason for each size, the Modular Regions exclusion and the two blocked pairs.** *(recommended)* Doc-only. The larger minimums exist for generation reliability.
  2. **Relax the code to the spec sizes.** Only after a per-modifier generation matrix at 5×5/6×6 passes. Expect more generation failures on small boards.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Eligibility table from `BossService.ModifierData` with reasons, blocked pairs and the Modular Regions exclusion in BossMechanicsSystem 2.3; boss pools are now size-aware (SPEC-CONF-028).

### SPEC-CONF-014
**"Floor Progression Arc" table**
- **CONFLICT inside BossMechanicsSystem:** §Floor Progression Arc lists "Boss Modifiers Active 1, 2, 3, 4, 5" and intensity per **floor** (Low … VeryHigh) vs §Choice Scaling (totals 2, 3, 5, 7, 9) and §Boss Modifier Intensity (intensity per **run number**).
- **Recommendation:** Delete or rewrite the arc table.
- **Suggestions:**
  1. **Delete the Floor Progression Arc table and point to §Choice Scaling and §Boss Modifier Intensity.** *(recommended)* Removes the third, conflicting copy of the same data.
  2. **Rewrite the arc as a summary derived from the code constants (floor modifiers, boss picks, intensity by run number).** Keeps an overview table, but it must be maintained whenever the constants change.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Floor Progression Arc table removed (BossMechanicsSystem 2.3).

### SPEC-CONF-015
**7★ requires a modifier**
- **CONFLICT:** GEN-REMOVE-003 and GameDesignSpec v1.4 history ("7-star requires ≥1 modifier"), SudokuGenerationPipeline §7★ Requires Modifiers vs TutorialModeSystem v1.8 §7★ ("No modifier requirement is enforced"; multiple solutions acceptable).
- **Recommendation:** Decide which rule the Custom Puzzle UI enforces.
- **Suggestions:**
  1. **Keep the code's rule: `TutorialModeService.TryValidateCustomSetup` already rejects 7★ without a mode, as GEN-REMOVE-003 and GDS require; update TutorialModeSystem v1.8.** *(recommended)* An empty grid without modifiers offers nothing to deduce.
  2. **Allow 7★ without modifiers as a free-fill practice mode and update GEN-REMOVE-003, GDS and SudokuGenerationPipeline.** No validation change. Under a solution-based rule (SPEC-CONF-003, suggestion 1), 7★ must then accept any valid fill.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Code rule kept; TutorialModeSystem 1.9 and SudokuGenerationPipeline 1.6 updated. Evidence: `SpecAlignmentTests.CustomPuzzle_SevenStars_NeedsAtLeastOneMode`.

### SPEC-CONF-016
**Undo**
- **CONFLICT:** GameDesignSpec v1.4 history ("Hint/Undo features removed") vs code (`InputRemapService`: `UndoLastDigit = Z`, "Ctrl+Z handled in InRunController").
- **Recommendation:** Either document Undo (with cost and limits) or remove the binding.
- **Suggestions:**
  1. **Remove the Undo binding (Z / Ctrl+Z) and the undo code path, as GDS v1.4 decided.** *(recommended)* Keeps mistakes meaningful in an HP-based roguelike.
  2. **Keep Undo and specify it: what it reverts, whether HP or combo is refunded, any limit or cost.** Needs a GDS section and tests. Unlimited free undo would make the HP system trivial.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Undo binding and code path removed. Evidence: `PlacementRulesTests.Undo_IsNotAnInputAction`.

### SPEC-CONF-017
**Rest node values**
- **CONFLICT:** GameDesignSpec §9b (+2 HP / +3 Pencil / +1 Reroll) vs code (`MaxHP/3` HP, +4 pencil). CurseSystem adds the 4th option (Cleanse) consistently. → [GAP-024](GAME_GAPS.md#gap-024)
- **Suggestions:**
  1. **Document the code values (heal `MaxHP/3`, +4 pencil) in GDS §9b.** *(recommended)* The heal scales with the class's HP pool (8–12), so Rest stays relevant on later floors. State whether a reroll is still granted.
  2. **Change the code to the GDS values (+2 HP, +3 pencil, +1 reroll).** A smaller, flat heal makes Rest weaker for high-HP classes.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. GDS 1.6 §Rest Node: heal max(1, MaxHP / 3) (halved at H10), +4 pencil, +1 reroll token, Cleanse (GAP-024). Evidence: `RestNodeValuesTests`.

### SPEC-CONF-018
**Tutorial region variants**
- **CONFLICT:** TutorialModeSystem §Region Layout ("Three grid layout variants … 0, 1, or 3") vs GDS §3.3 and SudokuGenerationPipeline (4 variants), and project memory ("dropdown index 2 → variant 3").
- **Recommendation:** Document the dropdown → variant mapping explicitly.
- **Suggestions:**
  1. **Document the Custom Puzzle dropdown mapping (0 → variant 0, 1 → 1, 2 → 3) in TutorialModeSystem and say why variant 2 is not offered.** *(recommended)* Doc-only.
  2. **Offer all four region variants in the dropdown.** One more UI option, plus generation tests for variant 2 at every size.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. TutorialModeSystem 1.9 documents the dropdown → variant mapping and why variant 2 is not offered.

### SPEC-CONF-019
**Size of the modifier pool (15 vs 30)**
- **CONFLICT:** BossMechanicsSystem says both "All 15 modifiers" (Overview, Floor Modifier Rolling, Modifier Eligibility, Descriptions, Equal Weights, Endless Zen) and "Active Modifier Pool (30 Implemented)". PathSystem ("All 15 modifiers are eligible"), GameDesignSpec §11, BOSS-MOD-001 ("15 boss modifiers (IDs 0–14)") and MetaProgression completion ("All 15 BossModifierId values") all say 15.
- Code: 30 player modifiers (IDs 0–29), plus 64 enum values for future content.
- **Recommendation:** Replace every "15" with the authoritative list (preferably a generated table).
- **Suggestions:**
  1. **Replace every "15 modifiers" with "30 player modifiers (IDs 0–29)" and generate the modifier table from `BossService.ModifierData`.** *(recommended)* One source of truth. Also fixes MetaProgression completion ("all 15").
  2. **Keep a curated core pool of 15 in the docs and declare IDs 15–29 an extended pool with its own eligibility.** The code would have to distinguish the two pools, which it does not today.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. "30 player modifiers (IDs 0–29)" in GDS, BossMechanicsSystem, PathSystem, MetaProgressionSystem and REQUIREMENT_MAP; completion counts the 29 clearable modifiers (GAP-009).

### SPEC-CONF-020
**Pressure UI requirement status**
- **CONFLICT:** PressureMechanicsSpec v0.7 (2026-04-15: "All requirements now ✅", incl. PRESSURE-UI-004) vs REQUIREMENT_MAP (PRESSURE-UI-004/005/008/009/010/011 ❌, UI-003 ⚠️).
- **Recommendation:** Re-verify (none were verified in this review) and make the two consistent.
- **Suggestions:**
  1. **Verify PRESSURE-UI-003..011 with PlayMode tests, then set both documents from the evidence.** *(recommended)* Settles the disagreement on facts.
  2. **Downgrade PressureMechanicsSpec to REQUIREMENT_MAP's ❌/⚠️ now and verify later.** Quick and honest, but leaves the UI unverified.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. PRESSURE-UI-001..011 verified on the rendered board by `InRunUiPlayModeTests`; PressureMechanicsSpec 0.8 and the map set from that evidence. The tests found GAP-039 (fixed).

### SPEC-CONF-021
**Debuff spec status**
- **CONFLICT:** BossDebuffEffects v1.3 header "Status: implemented" vs its own table (DEBUFF-DEF-006/009/010/016 ❌) and the absence of any delivery rule (see SPEC-AMB-002).
- **Suggestions:**
  1. **Set BossDebuffEffects to "under review" and list DEBUFF-DEF-006/009/010/016 and the missing delivery rule (SPEC-AMB-002) as open.** *(recommended)* Doc-only; the status then matches reality.
  2. **Implement the four missing debuffs and a delivery rule (for example one debuff per boss on floors 4–5), then keep "implemented".** Gameplay work plus balancing (GAP-026).
- **Resolution (1.1, 2026-10-07):** Implemented instead of documented as open (owner default: where a gap asks for code, implement and document; GAP-026). Delivery DEBUFF-DELIV-001..004; statuses from `BossDebuffTests`; PencilPoison, FogPulse, NumberBlind, DigitShift descoped to FUTURE-010 (BossDebuffEffects 1.4).

### SPEC-CONF-022
**Spec header status vs requirement map**
- **CONFLICT:** DailyGoalSystem v0.1, SeasonalChallengeSpec v0.1 and ClassProgressionUnlocks v0.2 headers say **"new"** (2026-04-09/18), while REQUIREMENT_MAP marks DAILY-GOAL, SEASON and CLASS-EXCL as "✅ Implemented".
- **Suggestions:**
  1. **Set each spec header from evidence: "implemented" only where tests or E2E confirm it; mark the rest ⚠️ in REQUIREMENT_MAP.** *(recommended)* Applies the CLAUDE.md rule "never mark complete without verification".
  2. **Downgrade the REQUIREMENT_MAP rows to the spec headers' "new" until verified.** Simpler, but understates features that do exist (Daily goals, Monthly Walk).
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Spec headers bumped with a history row stating the evidence; REQUIREMENT_MAP statuses synced from the matrix with an Evidence column (SPEC-PROC-003).

### SPEC-CONF-023
**Version header vs change history**
- BossMechanicsSystem header **v2.1**, history row **v2.2** (2026-04-27). ClassXpProgressionSystem header **v1.1**, history row **v1.2**. The project rule says to bump the header with each history row.
- **Suggestions:**
  1. **Bump the headers to the newest history rows (BossMechanicsSystem v2.2, ClassXpProgressionSystem v1.2).** *(recommended)* Doc-only, a few minutes.
  2. **Add a test that compares each `docs/*.md` header version with its newest Change History row.** Prevents the drift from recurring; can be combined with the first suggestion.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Headers match the newest history rows (BossMechanicsSystem 2.3, ClassXpProgressionSystem 1.3).

### SPEC-CONF-024
**Harmony XP multiplier**
- **CONFLICT:** HarmonyDifficultySystem §4.7 (XP multiplier ×1.0–×2.0, unconditional) vs code (`ProfileService`: multiplier only on **victory**, plus a flat `HarmonyLevel × 50` bonus).
- **Recommendation:** Document the victory-only rule and the flat bonus, or change the code. → [GAP-008](GAME_GAPS.md#gap-008)
- **Suggestions:**
  1. **Document the code: the XP multiplier applies only on victory, plus a flat `HarmonyLevel × 50` bonus.** No code change; rewards finishing high-Harmony runs.
  2. **Apply the multiplier on every run end (victory or defeat) and drop the flat bonus, as §4.7 says.** *(recommended)* Failed high-Harmony runs become worth more, which suits a ladder players climb gradually. Changes XP pacing (~80 runs to L40).
- **Resolution (1.1, 2026-10-07):** Suggestion 2. Multiplier on every run end, flat bonus removed (HarmonyDifficultySystem 1.1). Evidence: `HarmonyEffectsTests.XpMultiplier_AppliesOnDefeatToo_WithoutFlatBonus`.

### SPEC-CONF-025
**Harmony tables vs code tables**
- §4.4: H3–H5 "+1 on floors 2–4"; code base value 0 for H3–H5, and the floor check is `floorIndex >= 2` (floors 3–5).
- Boss options bonus H4: spec 0, code 1.
- §4.5 H10 grid weights {0,0,0,25,75} vs code {0,0,5,25,70}.
- Most of the table is not consumed at all ([GAP-008](GAME_GAPS.md#gap-008)).
- **Suggestions:**
  1. **HarmonyDifficultySystem is the source: fix the code values (H3–H5 +1 on floors 2–4, H4 boss options 0, H10 grid weights) while wiring the unused fields (GAP-008).** *(recommended)* One pass over `HarmonyDifficultyService` and its consumers.
  2. **Copy the code values into the spec, then wire the unused fields.** Keeps current behaviour for the few fields that are already consumed.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Code aligned to the HarmonyDifficultySystem tables while wiring every field (GAP-008); §4.8 documents how they apply. Evidence: `HarmonyEffectsTests`.

### SPEC-CONF-026
**Monthly Walk unlock and Ascension**
- **CONFLICT:** SEASON-UNLOCK-001 (REQUIREMENT_MAP "Unlocked after first ascension") and MetaProgressionSystem §Seasonal Challenges ("unlocked after first ascension") vs SeasonalChallengeSpec (prominent main-menu panel, no unlock gate) vs code (always available in Game Modes; no ascension system exists).
- **Recommendation:** Remove the ascension gate from the map, or build ascension ([GAP-017](GAME_GAPS.md#gap-017)).
- **Suggestions:**
  1. **Remove the Ascension gate: Monthly Walk stays available as it is now. Update SEASON-UNLOCK-001 and MetaProgressionSystem.** *(recommended)* Doc-only; matches SeasonalChallengeSpec and the build.
  2. **Build Ascension (GAP-017) and gate Monthly Walk behind the first ascension.** Large feature; hides a retention feature from most players.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. No Ascension gate; SEASON-UNLOCK-001, MetaProgressionSystem 1.4 and SeasonalChallengeSpec 0.2 updated; Ascension deferred (FUTURE-009).

### SPEC-CONF-027
**Placement of Daily/Monthly widgets**
- DailyGoalSystem ("Daily Walk widget on main menu") and SeasonalChallengeSpec ("Prominent main menu panel with countdown") vs the implementation (buttons inside Game Modes, E12). → [GAP-028](GAME_GAPS.md#gap-028)
- **Suggestions:**
  1. **Move the Daily Walk widget and the Monthly Walk panel (with countdown) onto the main menu, as both specs say.** *(recommended)* These are retention features and only work when visible.
  2. **Keep them inside Game Modes, update both specs, and add a main-menu badge when a new daily goal or month is available.** Less UI work; the badge keeps them discoverable.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Daily Walk and Monthly Walk widgets (with countdown) on the main menu (GAP-028). Evidence: `GeneratedUiPlayModeTraversalTests.HomeWidgets_*`.

### SPEC-CONF-028
**Boss board size**
- GameDesignSpec §10 floor grid ranges (F1 5–6 …) and BossMechanics §Floor Progression Arc vs code (`RunMapController`: boss `BoardSize = Max(size, 8)`), so Floor 1 bosses are 8×8 (E17). → [GAP-013](GAME_GAPS.md#gap-013)
- **Suggestions:**
  1. **Drop the `Max(size, 8)` boss clamp so bosses follow the floor's size range, and filter the boss pool by board size instead.** *(recommended)* Floor 1 bosses become 5×5–6×6 as GDS §10 says. Depends on size-aware pools (SPEC-CONF-013).
  2. **Document "boss boards are at least 8×8" in GDS §10 and BossMechanicsSystem.** No code change, but the Floor 1 difficulty spike stays.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. `Max(size, 8)` removed; boss pools filtered by board size; preview equals the generated puzzle (GAP-013). Evidence: `NodeConfigPreviewTests`.

### SPEC-CONF-029
**Pencil purchase and item-reward reroll dropped silently**
- GameDesignSpec §5.2/§5.3/§8 (buy pencil 20+20n; reroll eligible reward slots 20+20n; locked-slot rules) vs GoldEconomySystem v2.0 ("reflects the current runtime code path"; neither mechanic mentioned) and the code (neither exists). → [GAP-015](GAME_GAPS.md#gap-015), [GAP-016](GAME_GAPS.md#gap-016)
- **Suggestions:**
  1. **Implement both mechanics as GDS specifies (pencil purchase and reward reroll, each 20 + 20n gold) and add them to GoldEconomySystem.** *(recommended)* `GoldTable` already has both costs, and gold needs sinks. GAP-015, GAP-016.
  2. **Remove both from GDS and record the decision in its Change History.** No code work, but gold accumulates faster without sinks.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Both implemented (20 + 20n gold) and documented in GoldEconomySystem 2.1 and GDS 1.6 (GAP-015, GAP-016). Evidence: `EconomyActionTests`.

### SPEC-CONF-030
**Path tile visibility**
- MAP-ROUTE-001/002 ("Fully visible to the player from floor entry — no hidden tiles") vs the implementation (tiles beyond the first are "?", E05). → [GAP-022](GAME_GAPS.md#gap-022)
- **Suggestions:**
  1. **Show every tile from floor entry (remove the "?" masking), as MAP-ROUTE-001/002 say.** *(recommended)* Route planning between the lanes is the core path decision.
  2. **Keep fog-of-path and specify the reveal rule (for example tiles next to visited ones) in PathSystem.** Adds discovery, but players choose lanes blind.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Every tile type visible from floor entry; details on reachable and visited tiles (PathSystem 1.6). Survey Notes / Accurate Map now reveal tile details (ClassProgressionUnlocks 0.3).

### SPEC-CONF-031
**Irregular puzzles default**
- GameDesignSpec §3.3 / `RunState.AllowIrregularPuzzles = true` (default true) vs class-select UI (confirm modal shows "Irregular puzzles: off" by default, E04).
- **Suggestions:**
  1. **Default the class-select toggle and confirm modal to ON, matching GDS §3.3 and `RunState.AllowIrregularPuzzles`.** *(recommended)* Small UI fix (part of GAP-029).
  2. **Change the spec and the `RunState` default to OFF for gentler onboarding.** Irregular puzzles become opt-in; update GDS §3.3.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Toggle and confirm modal default to ON. Evidence: `GeneratedUiPlayModeTraversalTests.HomeWidgets_OpenDailyAndMonthly_AndIrregularPuzzlesDefaultOn`.

### SPEC-CONF-032
**Languages**
- GameDesignSpec §14 ("Language (English / German)"), startup-experience ("English or German language selection"), development.md (lists only en/de catalogs) vs shipped catalogs `en/de/fr/es/pt` (commit 598f876). First-run setup still offers only EN/DE (E01b).
- **Suggestions:**
  1. **Update GDS §14, startup-experience and development.md to all five shipped languages, and offer all five in first-run setup.** *(recommended)* Matches what ships (commit 598f876).
  2. **Hide FR/ES/PT until they are reviewed, and keep the docs at EN/DE.** Avoids shipping unreviewed translations.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. GDS 1.6, startup-experience and development.md list five languages; first-run setup offers all five. Evidence: `StartupExperienceReadinessTests`.

### SPEC-CONF-033
**Tutorial entry naming**
- GameDesignSpec §14 Main Menu ("Tutorial") and TutorialModeSystem ("Main Menu → Tutorial button") vs build ("Custom Puzzle" / "Eigenes Rätsel"; the basics tutorial lives in Options as "Sudoku-Anleitung").
- **Suggestions:**
  1. **Keep "Custom Puzzle" (it is a sandbox), update GDS §14 and TutorialModeSystem, and document that Sudoku Basics is offered at first launch and in Options.** *(recommended)* Doc-only; the current names describe what each entry does.
  2. **Rename the main-menu entry back to "Tutorial".** Matches the old docs, but "Tutorial" then points to a sandbox rather than a lesson.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. "Custom Puzzle" kept; GDS 1.6 and TutorialModeSystem 1.9 updated; Sudoku Basics documented as first-launch prompt and Options entry.

### SPEC-CONF-034
**XP beyond Level 40**
- ClassXpProgressionSystem §Excess XP ("discarded") vs code (`TotalXp` keeps growing; level capped at 40, D08).
- **Suggestions:**
  1. **Discard XP above Level 40, as ClassXpProgressionSystem says: cap `TotalXp` at the L40 threshold when committing.** *(recommended)* Small code change; check that prestige resets still work.
  2. **Keep accumulating as a lifetime-XP statistic and document it; the level stays capped at 40.** No code change, and the number is useful as a stat.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. `TotalXp` capped at the Level 40 total when committing; prestige still resets to 0. Evidence: `HarmonyEffectsTests.XpAboveLevel40_IsDiscarded`, `PrestigeTests`.

### SPEC-CONF-035
**Standalone items**
- ItemsAndRelicsSystem v2.0 ("not yet wired into the ItemType enum roll pool … Temple Seal") vs code (BambooScroll, GardenLantern, GoldenKoi and **TempleStamp** are in the enum and roll pools).
- **Suggestions:**
  1. **Update ItemsAndRelicsSystem: list BambooScroll, GardenLantern, GoldenKoi and TempleStamp as roll-pool items, with effects and rarity.** *(recommended)* Doc-only; matches the code.
  2. **Remove the four items from the roll pools until they are specified.** Shrinks the item variety players see now.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Standalone items documented as roll-pool items in ItemsAndRelicsSystem 2.1 (tables generated from code).

### SPEC-CONF-036
**Requirement map claims and paths**
- REQUIREMENT_MAP marks META-ASCEND-002..004 ✅ in `Assets/Scripts/Meta/AscensionService.cs`, which does not exist.
- It cites `Assets/Scripts/Core/SudokuGenerator.cs` and `Core/ModifierGeometryGenerator.cs` (actually `Assets/Scripts/Sudoku/`).
- XP rows cite `RunDirector.cs` for `XpService`/`XpTable` (actually `Economy/`).
- INPUT-REMAP-004 cites `InputRemapService.cs` for `KeybindingsPanelController` (actually `UI/`).
- BossMechanicsSystem and PathSystem reference `PrototypeRunScreenController.cs`, which no longer exists.
- **Suggestions:**
  1. **Fix the wrong paths and claims by hand now, and set META-ASCEND-002..004 to ❌.** *(recommended)* Doc-only.
  2. **Generate REQUIREMENT_MAP's code column from the `// [REQ: ID]` tags, and add a test that every cited path exists.** Stops the drift for good; can follow the first suggestion.
- **Resolution (1.1, 2026-10-07):** Suggestion 1 done, suggestion 2 partly: paths and claims fixed in REQUIREMENT_MAP (Sudoku/, Economy/, UI/), META-ASCEND-001..003 ❌ (deferred); META-ASCEND-004 points at `SeasonalChallengeService.BuildSeed`; stale `PrototypeRunScreenController` references replaced. Not done: generating the code column from tags.

### SPEC-CONF-037
**Sudoku Basics tutorial steps**
- REQUIREMENT_MAP TUTO-BASICS-STEP-001/006 cite FirstTimePlayTutorial **v1.1** ("7 named steps", "Step 6 … digit 2"). FirstTimePlayTutorial is now **v2.0** and no longer defines steps ("Verified design intent from code/comments"). Project memory describes an 11-step controller with different teaching cells.
- **Recommendation:** Specify the steps again (they are the acceptance criteria for the onboarding).
- **Suggestions:**
  1. **Write the 11 steps of `SudokuBasicsTutorialController` (step, target cell, expected action, correct digit) into FirstTimePlayTutorial v2.1 as acceptance criteria.** *(recommended)* Documents what ships and makes it testable.
  2. **Restore FirstTimePlayTutorial v1.1's 7 steps and change the controller to match.** Changes the onboarding players see.
- **Resolution (1.1, 2026-10-07):** Suggestion 1. The shipped controller has 15 steps (not 11); all 15 are in FirstTimePlayTutorial 2.1 with target cell, action and digit. Evidence: `SudokuBasicsStepsTests`.

### SPEC-CONF-038
**Anti-Bishop cannot be satisfied on its eligible board sizes** *(added 2026-10-05)*
- **CONFLICT:** BossMechanicsSystem defines Anti-Bishop as "No two cells on the same diagonal (any length) may contain the same digit", minimum 6×6. The code allowed it only on 9×9 (`ModifierData` minimum 9).
- With row and column uniqueness, every digit must then form an n-queens solution, and the n digits must split the board into n disjoint queen solutions. That partition exists only for n ≡ 1 or 5 (mod 6), i.e. 5×5 and 7×7 (with regions also respected). It cannot exist for 6×6, 8×8 or 9×9. This matches the benchmark: every 9×9 seed timed out before and after the generator fix.
- Code (fix pass, GAP-003): `BossService.IsSupportedForGeneratedRun` now returns false for Anti-Bishop, so it is never offered, rolled, kept from older saves, or selectable in Custom Puzzle.
- **Recommendation:** Redefine or relocate the rule before re-enabling it.
- **Suggestions:**
  1. **Redefine Anti-Bishop as "no repeat on a diagonal within distance 2" (a bishop-range variant of Anti-King) and allow it from 6×6.** *(recommended)* Keeps the name and theme, generates like the other global rules, and needs only a rule/generator change plus tests.
  2. **Keep the full-diagonal rule but allow it only on 5×5 and 7×7 boards whose regions admit a queen partition.** Faithful to the current text, but it needs verified region templates and would rarely appear (only those two sizes).
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Anti-Bishop = no repeat on a diagonal within distance 2, from 6×6, back in the generated pool. Evidence: `BalanceBudgetContractTests.AntiBishop_IsEligibleFromSixBySix`, `GenerationReliabilityTests`.

### SPEC-CONF-039
**Prestige reward** *(added 2026-10-07)*
- **CONFLICT:** ClassXpProgressionSystem v1.3 §On Prestige ("Passive tier **+1**, permanent bonus carried across prestige cycles") vs MetaProgressionSystem v1.4 META-PRESTIGE-005 ("Reward: prestige badge … cosmetic tier indicator").
- Code (`ClassGardenProgressionService.TryPrestige`, 1.1): resets `TotalXp` to 0 and raises `PrestigeTier`; no passive changes. The tier is shown in the Meta Progression panel.
- Likely current: unclear. Prestige was unreachable before 1.1 (GAP-017), so neither reward was ever observed.
- **Recommendation:** Decide the reward before players reach Level 40 in large numbers (about 80 runs).
- **Suggestions:**
  1. **Keep the reward cosmetic and remove "Passive tier +1" from ClassXpProgressionSystem.** *(recommended)* Matches the code and keeps balance unchanged; no save change.
  2. **Implement passive tiers: define the per-class effect of each tier (1–9), apply it in `ClassPassiveService`, and test it.** Gives prestige a gameplay reason; needs a design for eight passives × nine tiers.
- **Resolution (1.1, 2026-10-07):** Open, owner decision. 1.1 ships the cosmetic reward (both specs say so in their 1.1 rows).
- **Resolution (2026-10-20):** Owner decision, a third option: the badge plus **+1 starting gold per prestige tier** for runs with that class (at most +9). "Passive tier +1" is dropped. ClassXpProgressionSystem 1.4, MetaProgressionSystem 1.10 (META-PRESTIGE-005); code `ClassGardenProgressionService.PrestigeStartingGold`, applied in `RunArchetypeService.CreateRunState`. Evidence: `PrestigeTests.PrestigeTier_AddsOneStartingGoldPerTier`.

### SPEC-CONF-040
**Steam achievement names and visibility** *(added 2026-10-14)*
- **CONFLICT:** HarmonyDifficultySystem v1.1 §9 Achievements names the eight achievements First Harmony, Deep Roots, Halfway to Void, Beyond the Garden, Borderless Garden, Void Garden, Void Perfection (hidden) and Master of Harmony (hidden). The live Steamworks configuration of App 4858470 (checked 2026-10-14, published with 1.0) uses the same API names but the display names First Awakening, Moss and Gravel, Water Reflection, Lantern Glow, Torii Gateway, Summit Ascent, Tranquil Perfection and Master of the Garden; only `harmony_10_perfect` is hidden.
- Code: `SteamAchievementService` uses only the API names, which match. The game never shows the display names; players see them in Steam.
- Likely current: the portal. It is what players have seen since 1.0, and its descriptions match the conditions in §9.
- **Recommendation:** Keep the portal and update §9 to the live names and visibility.
- **Suggestions:**
  1. **Update HarmonyDifficultySystem §9 to the live display names, and mark only `harmony_10_perfect` hidden.** *(recommended)* No portal or code change; the spec describes what players see.
  2. **Rename the achievements in Steamworks to the §9 names and hide `harmony_all_classes`.** Restores the spec's naming, but renames achievements players already know and needs a publish.
- **Resolution (0.1.5, 2026-10-14):** Suggestion 1, on the owner's request. HarmonyDifficultySystem 1.2 §9 lists the live names and descriptions; only `harmony_10_perfect` is hidden (`harmony_all_classes` is Expert tier). No portal or code change.

### SPEC-CONF-041
**The Meta Progression screen: XP bar and "Most Progressed Class"** *(added 2026-10-21)*
- **CONFLICT:** ClassXpProgressionSystem v1.5 §UI Placement (and MetaProgressionSystem v1.12 META-UNLOCK-005, the class screen's "no progress bar") says the Meta Progression screen shows each class's XP as "X / Y XP" text with **no progress bar**, that the bar appears **only** on the end-of-run screen, and that the screen has an overall "Most Progressed Class" summary at the top. The owner's pick of 2026-10-20 (layout-proposals.md, UI-STYLE-019, the garden of classes) shows an **XP stroke** on the chosen class's scroll and no "Most Progressed Class" summary.
- Code (2026-10-21): built as the picked mockup: the XP stroke (for a locked class the same stroke shows the way to its unlock) and no summary; the totals along the bottom take its place. The class screen keeps text only (META-UNLOCK-005).
- Likely current: the owner's pick (newer, and about this screen); the spec's line predates the redesign.
- **Recommendation:** Confirm the pick and update §UI Placement.
- **Suggestions:**
  1. **Keep the stroke and the totals, and update ClassXpProgressionSystem §UI Placement to the garden of classes.** *(recommended)* Matches what was picked and built; the end screen keeps its animated bar.
  2. **Show the XP as text only on the scroll and add the "Most Progressed Class" line.** Follows the spec; changes the picked design.
- **Resolution (2026-10-21):** Suggestion 1 (owner request 2026-10-21: "analyze, plan and fix"). ClassXpProgressionSystem 1.6 §UI Placement describes the garden of classes (the still XP stroke and the totals; the animated bar stays on the end screen); MetaProgressionSystem 1.13 META-UNLOCK-005 says it is about the class screen.

---

## 2. Ambiguous requirements

### SPEC-AMB-001
**6★ reward slots and Nothing chance**
GDS §8 and ITEM-SLOT-001/002 only define 1★–5★. Code falls back to 5 slots / 12 %. Also "Nothing … 25 %" is per slot, but the code guarantees at least one real item, so the measured 1★ rate is 21.6 %. State whether the guarantee rule is intended.
- **Resolution (1.1, 2026-10-07):** GDS 1.6 has the reward slot / Nothing table including 6★ and the guarantee rule. Evidence: `HarmonyEffectsTests.NothingChance_*`, `EconomyActionTests`.

### SPEC-AMB-002
**Debuff delivery**
BossDebuffEffects defines effects but not *when* a debuff is attached to a puzzle (boss phase? floor? curse?). Result: implemented but unreachable in Garden Run ([GAP-026](GAME_GAPS.md#gap-026)).
- **Resolution (1.1, 2026-10-07):** Decided and implemented: DEBUFF-DELIV-001..004 (see SPEC-CONF-021).

### SPEC-AMB-003
**Fairness with non-unique puzzles**
"Puzzles are not required to be logically solvable — items and relics compensate" (GDS §15, BossMechanics). Undefined: what the player should experience when a rule-valid digit later turns out wrong, and whether 9×9 6★ (8 givens, never unique) is intentional.
- **Resolution (1.1, 2026-10-07):** Moot: in-game puzzles are unique (correction of 2026-10-05); with SPEC-CONF-003 a wrong digit is defined against the unique solution. Custom 7★ boards stay rule-based.

### SPEC-AMB-004
**Generation failure handling**
No requirement says what happens when a puzzle cannot be generated (retry with a new seed? degrade? inform the player?). The implementation rolls back to the map and retries the identical seed ([GAP-003](GAME_GAPS.md#gap-003)).
- **Resolution (1.1, 2026-10-07):** SudokuGenerationPipeline 1.6 documents the fallback chain, salted retry and the Monthly Walk failure message.

### SPEC-AMB-005
**What resumes from which screen**
SaveLoadArchitecture §What Can Resume lists conditions, but not the expected state when saving at the boss gate (are choices kept?), on the reward screen (rolled slots?), in the shop (offers?), or in the relic choice. The code re-rolls all of these ([GAP-018](GAME_GAPS.md#gap-018)).
- **Resolution (1.1, 2026-10-07):** SaveLoadArchitecture 2.1 lists what resumes from which screen; rolled options are saved (GAP-018/021).

### SPEC-AMB-006
**"Collect 50,000 gold"**
Collected vs held vs earned is undefined ([GAP-027](GAME_GAPS.md#gap-027)).
- **Resolution (1.1, 2026-10-07):** Gold collected = every gain during the run (GoldEconomySystem 2.1, GAP-027). Evidence: `EconomyActionTests.CollectedGold_*`.

### SPEC-AMB-007
**"Nothing grants a small gold bonus"**
GDS §8. The amount is undefined, and no gold for "Nichts"/"Überspringen" was observed in E14/E16.
- **Resolution (1.1, 2026-10-07):** Nothing (and Skip) grant 5 × floor gold, counted as collected (GDS 1.6, GoldEconomySystem 2.1). Evidence: `EconomyActionTests.NothingReward_*`.

### SPEC-AMB-008
**Floor transition sequence**
PathSystem lists "Garden Cleared card (~2–3 s) → cutscene → fade-in", with no acceptance criteria for content, skip input or controller handling. It wasn't observed in E19 (the transition happened through the resume workaround).
- **Resolution (1.1, 2026-10-07):** PathSystem 1.6 documents the floor transition sequence.

### SPEC-AMB-009
**Endless Zen "full BossModifierId pool"**
EndlessZenMode v2.0 literally specifies the full enum, which includes future-content IDs without rules, debuffs and pressure mechanics ([GAP-007](GAME_GAPS.md#gap-007)). This is almost certainly unintended, but the spec currently requires it.
- **Resolution (1.1, 2026-10-07):** Endless Zen draws from the generatable player modifiers (EndlessZenMode 2.1, BossMechanicsSystem 2.3).

### SPEC-AMB-010
**Starting a new run while one is active; abandoning**
No requirement covers overwrite confirmation, recording the abandoned run (XP? stats?) or an Abandon action ([GAP-020](GAME_GAPS.md#gap-020), [GAP-032](GAME_GAPS.md#gap-032)).
- **Resolution (1.1, 2026-10-07):** Starting a run over a saved one warns and offers "Abandon & Begin"; the in-run menu has Abandon Run; an abandoned run is recorded as a defeat (GDS 1.6, SaveLoadArchitecture 2.1, GAP-020/032). Evidence: `RunSeedAndAbandonTests`, `InRunUiPlayModeTests.RunMenu_*`.

### SPEC-AMB-011
**Audio**
AudioVisualDirection describes moods, layering and mixing, but no testable criteria: which clip plays in which context, ducking amount, loop points, volume curve, mute-when-unfocused behaviour.
- **Resolution (1.1, 2026-10-07):** AudioVisualDirection 1.5 has a clip-per-context table generated from `AudioAssetService`. Evidence: `AudioClipPlayModeTests`. Listening check still open (GAP-030).

### SPEC-AMB-012
**Performance budgets vs UX**
performance-generation-budget sets budgets, but nothing says what the UI must show while waiting (24–30 s boss waits were observed) or the maximum acceptable wait.
- **Resolution (1.1, 2026-10-07):** performance-generation-budget.md ties each budget to what the player sees (status text, fallback, stay on menu).

### SPEC-AMB-013
**"Perfect" definition**
XP Perfect bonus ("zero mistakes"), the Healing Path effect ("mistake-free puzzle") and curse auto-cleanse (`PerfectSoFar && Mistakes == 0`) each define "perfect" slightly differently. Clarify whether absorbed mistakes (relic, umbrella) break "perfect".
- **Resolution (1.1, 2026-10-07):** GDS 1.6 defines "perfect" (no wrong digit entered; absorbed mistakes still count) for XP, Healing Path and curse cleanse.

### SPEC-AMB-014
**Global structural modifiers on irregular regions** *(added 2026-10-05)*
The fix pass (GAP-003) makes levels with a global structural modifier (Nonconsecutive, Anti-Knight, Anti-King, Nonconsecutive Diagonal, Distance ≥ 2, Entropy) use the rectangular region layout of the same size. Reason: on the irregular 9×9 templates every benchmarked seed timed out for each of these rules, before and after the generator speed-up, so those combinations never produced a puzzle. GEN-REGION-003 says only that runs may use all four variants.
- **Suggestions:**
  1. **Document the rule in SudokuGenerationPipeline (§Region Map) and BossMechanicsSystem (§Modifier Eligibility).** *(recommended)* Matches what the generator can deliver.
  2. **Build structural-rule-compatible irregular templates and verify them with a generation benchmark before allowing the combination again.**
- **Resolution (1.1, 2026-10-07):** Suggestion 1. Documented in SudokuGenerationPipeline 1.6 §Region Map, BossMechanicsSystem 2.3 §Modifier Eligibility and TutorialModeSystem 1.9.

---

## 3. Obsolete / outdated requirements

| ID / item | Where | Why obsolete | Recommendation | Resolution (1.1) |
|---|---|---|---|---|
| <a id="spec-obs-001"></a>**REQ-GENERAL-013..029** | GameDesignSpec §Requirements Traceability | Auto-generated by the local "SPICE" pipeline: titles like "TITLE", "verified" means only "a file exists" (e.g. HP → `RelicService.cs`) | Delete the table; GDS requirements are now covered by canonical IDs and the `REQ-*-1xx` IDs created by this review | Archived (`docs/archive/SPICE_REQUIREMENT_TABLES.md`) |
| **REQ-BOSS-001..035** | BossMechanicsSystem | Auto-generated duplicates ("VALIDATION_PATH_5", linked to non-existent `EventManager`, `RenderManager`) | Delete; keep BOSS-MOD/INT/FLOOR | Archived |
| **REQ-SUDOKU-076..117** | SudokuGenerationPipeline | Auto-generated (e.g. REQ-SUDOKU-076 titled "TITLE") | Delete; keep GEN-* | Archived |
| **REQ-UI-018..036**, **REQ-META-010..018**, **REQ-SAVE-018**, **REQ-TUTORIAL-173** | AccessibilitySpec, AudioVisualDirection, MetaProgression, map | Same pipeline; the map itself calls them "stubs pending update" | Delete or convert to canonical IDs | REQ-UI-027..036 and REQ-META-010..018 archived; REQ-UI-018..026, REQ-SAVE-018 and REQ-TUTORIAL-173 had no tables left (mentions removed or kept as history) |
| **RELIC-SLOT-001** | REQUIREMENT_MAP | Superseded by multi-relic (SPEC-CONF-004) | Mark deprecated | Marked deprecated |
| **SAVE-DUAL-001** | REQUIREMENT_MAP | "Dual-file architecture" superseded by one envelope per slot (SaveLoadArchitecture v2.0) | Mark deprecated | Marked deprecated |
| **BOSS-MOD-001** | REQUIREMENT_MAP | "15 boss modifiers (IDs 0–14)"; the pool is 30 | Update | Updated to 30 |
| **TUTO-BASICS-STEP-001/002/006** | REQUIREMENT_MAP | Refer to spec v1.1 | Re-specify (SPEC-CONF-037) | Re-specified (FirstTimePlayTutorial 2.1) |
| **ECON-SHOP-001** | REQUIREMENT_MAP | "3 items + optional relic" (SPEC-CONF-005) | Update | Updated |
| **GDS §13 Endless Zen**, **GDS §14 Options language list**, **GDS §6.1 roster** | GameDesignSpec | Superseded by newer mode, localization and class docs | Reduce GDS to links | GDS 1.6 links to the mode, class and localization docs |
| **ParkStoryDraft.md** | docs | Header "superseded" | Move to an archive folder | Moved to `docs/archive/` |
| **FUTURE-005..008** | future-scope-decision-register | Out of scope by decision | Keep as register (status OUTDATED in matrix) | Kept; FUTURE-009..016 added |
| GDS links to `ClassXpProgressionSystem_implemented.md`, `PathSystem_GardenOverview_implemented.md` | GameDesignSpec §6, §9, §10, §17 | Files were renamed (no `_implemented` suffix) | Fix links | Links fixed (GDS, MetaProgressionSystem) |
| Project memory: "Spec files (docs/respec/)" | memory | Specs now live directly in `docs/` | Memory updated by this review | Done |

---

## 4. Missing acceptance criteria

| Ref | Area | What is missing | Resolution (1.1) |
|---|---|---|---|
| <a id="spec-mac-001"></a>SPEC-MAC-001 | ItemsAndRelicsSystem v2.0 | The item table (effects per rarity, prices, charges) and relic table (35 `RelicId`s, tiers, effects, weights) were removed in the v2.0 rewrite. No testable definition remains for any item or relic effect. | Done: item and relic tables regenerated from code (ItemsAndRelicsSystem 2.1). |
| SPEC-MAC-002 | FirstTimePlayTutorial v2.0 | Step list, target cells and expected actions (see SPEC-CONF-037). | Done: 15-step table (FirstTimePlayTutorial 2.1), `SudokuBasicsStepsTests`. |
| SPEC-MAC-003 | CurseSystem | Per-curse numeric effects are described, but there are no acceptance tests and no stacking examples; 15 curse IDs (CURSE-POOL) have no behavioural criteria. | Done: `CurseEffectTests` (CurseSystem 1.2). |
| SPEC-MAC-004 | PathSystem | Node-type roll weights per floor/route and the cursed-node weights are prose only ("early floors weight 2, mid 4–6"). | Done: node-type weight table (PathSystem 1.6). |
| SPEC-MAC-005 | GameDesignSpec §4 Controls | Multi-select (CTRL/SHIFT, select-all, invert, double-click tool) has no expected outcomes. Only single-cell selection exists in the build. | Deferred: single-cell selection only; multi-select is FUTURE-011. |
| SPEC-MAC-006 | AccessibilitySpec | "Recolours", "boosts contrast" have no measurable thresholds (contrast ratios, colour pairs). | Done: contrast table with measured ratios (AccessibilitySpec 2.1). |
| SPEC-MAC-007 | AudioVisualDirection | See SPEC-AMB-011. | Done with SPEC-AMB-011 (AudioVisualDirection 1.5). |
| SPEC-MAC-008 | MetaProgressionSystem achievements (52) | Conditions listed, but no requirement IDs and no test mapping. | Documented: only the Harmony achievements exist (HARMONY-ACHIEVE-001, `SteamAchievementServiceTests`); the 52-item list is marked as a proposal (MetaProgressionSystem 1.4, FUTURE-016). |
| SPEC-MAC-009 | Save/Load | No compatibility requirement (which old versions must load, migration tests, corrupted-file behaviour beyond `.bak`). | Done: compatibility rules incl. 1.0 → 1.1 (SaveLoadArchitecture 2.1). |
| SPEC-MAC-010 | Determinism | "Same seed → same run" has no scope (are shop offers, events, curses and relic choices included? across save/resume?). | Done: determinism scope across resume (SudokuGenerationPipeline 1.6), `DeterminismAcrossResumeTests`. |
| SPEC-MAC-011 | Performance | `performance-profiler-results.md` PERF-CAP-001..007 are all "PENDING"; no budgets are tied to observable UI behaviour. | Open: PERF-CAP-001..007 need a profiler session on target hardware (owner). |

---

## 5. Process and documentation hygiene

### SPEC-PROC-001
**Two incompatible ID schemes**
Canonical `<SYSTEM>-<CATEGORY>-NNN` IDs (REQUIREMENT_MAP) coexist with auto-generated `REQ-<CAT>-NNN` IDs from the local SPICE pipeline (`ai/`, gitignored). The pipeline reads `docs/respec/*_implemented.md`, which no longer exists. **Recommendation:** Retire the pipeline output; keep one scheme. This review created new IDs as `REQ-<SYS>-1xx` (numbered from 100 to avoid collisions); renaming them into the canonical scheme is fine as long as the matrix is updated.
**Resolution (1.1, 2026-10-07):** The SPICE tables (REQ-BOSS-001..035, REQ-GENERAL-013..029, REQ-META-010..018, REQ-SUDOKU-076..117, REQ-UI-027..036) moved to `docs/archive/SPICE_REQUIREMENT_TABLES.md`. REQ-UI-018..026, REQ-SAVE-018..039 and REQ-TUTORIAL-173..202 had no tables left, only mentions: the REQUIREMENT_MAP notes were removed, the TutorialModeSystem 1.8 history row stays as history. All these IDs stay reserved. The review's `REQ-<SYS>-1xx` IDs are listed in REQUIREMENT_MAP §Review-Created Requirements.

### SPEC-PROC-002
**Specs rewritten to match the code**
Nine documents dated 2026-04-27 (v2.0: SaveLoad, ItemsAndRelics, GoldEconomy, EndlessZen, SpiritTrials, FirstTimePlayTutorial, Accessibility, InputAndProfiles, UnityImplementation) state that they "reflect the current runtime code path". That turns requirements into descriptions: behaviour silently dropped from the code (pencil purchase, reward reroll, item tables) vanished from the spec without a decision record. **Recommendation:** Keep a design-intent section separate from "current implementation notes", and record removals as explicit decisions.
**Resolution (1.1, 2026-10-07):** Partly. Removals of 1.1 are recorded as decisions (future-scope-decision-register FUTURE-009..016) and every bumped spec has a history row naming the decision. Mechanics the v2.0 rewrites had dropped (pencil purchase, reward reroll, item tables) are back in the specs. A separate design-intent section was not added to every v2.0 document.

### SPEC-PROC-003
**Status claims without runtime evidence**
REQUIREMENT_MAP marks almost everything ✅, including MISSING items (Ascension) and BROKEN ones (resume, completion). The project rule says "never mark a feature complete without runtime/E2E verification". **Recommendation:** Add an "Evidence" column (test name or E2E report ID) and only allow ✅ when it is filled.
**Resolution (1.1, 2026-10-07):** REQUIREMENT_MAP has an Evidence column; ✅ only with a tagged passing test or an E2E/diagnostic ID; statuses synced from the regenerated matrix; ◻️ marks code without evidence. `RequirementTraceabilityTests` rejects undocumented test tags.

### SPEC-PROC-004
**Encoding damage in docs**
GameDesignSpec (79 affected lines), BossMechanicsSystem (178) and REQUIREMENT_MAP (116) contain mojibake ("â€”", "Ã—", "â˜…", "âœ…"). It's the same double-encoding problem as `de.json` ([GAP-019](GAME_GAPS.md#gap-019)), probably from PowerShell writes using the ANSI code page. **Recommendation:** Re-encode, and enforce UTF-8 in scripts (`-Encoding utf8`).
**Resolution (1.1, 2026-10-07):** GameDesignSpec, BossMechanicsSystem, REQUIREMENT_MAP and de.json re-encoded; `docs/*.md` is free of double-encoded text (the remaining hits in `docs/review/` are quotes). `LocalizationCatalogQualityTests` guards the catalogs and production `.cs` files.

### SPEC-PROC-005
**Missing referenced docs**
`docs/release/*.md` (platform readiness checklist, release manifest, release status, store asset checklist) are referenced by 8 tests and missing from the repo. `docs/plan/PressureMechanicsPlan.md` is referenced by REQUIREMENT_MAP and also missing.
**Resolution (1.1, 2026-10-07):** `docs/release/` created (release manifest, platform readiness checklist, store asset checklist, readiness status); the 9 release tests run and pass. The `docs/plan/PressureMechanicsPlan.md` reference was removed from REQUIREMENT_MAP.

### SPEC-PROC-006
**Ticket and test-case folders**
The review brief names `tickets/`, `tickets/ai-spec/`, `tickets/to_review/` and `testcases/`. None exists in the repository (the related `requirements/` and `retired/` folders are gitignored). Ticket-based requirements could therefore not be reviewed. If they exist elsewhere, they should be added or linked.
**Resolution (1.1, 2026-10-07):** No action possible: the folders do not exist. Nothing changed.

### SPEC-PROC-007
**Credits claim no Steamworks binaries, but they are committed** *(added 2026-10-05)*
`docs/Credits.md` and the in-game credits (`Credits.Body.*`) state "No Steamworks SDK binaries are bundled in this repository". Commit 0826fec added Steamworks.NET 2025.163.0 including `steam_api.dll`, `steam_api64.dll` and `steam_api.bundle` under `Assets/com.rlabrecque.steamworks.net/Plugins/`. `CreditsLegalContentTests.CreditsMarkdownAndInGamePanel_SourceDoNotClaimBundledSteamworksSdk` is now skipped with this reason instead of enforcing a false statement.
- **Suggestions:**
  1. **Update the credits and third-party notices for Steamworks.NET (MIT) and the Steamworks SDK redistributables (Valve SDK licence), then update the test to assert the new notice.** *(recommended)* Required anyway for a Steam release.
  2. **Remove the binaries from the repository and fetch them in the build pipeline, keeping the current notice.** Keeps the repository free of SDK binaries; needs a build step.
**Resolution (1.1, 2026-10-07):** Suggestion 1. Credits, third-party notices and in-game credits name Steamworks.NET (MIT, licence copy in `docs/legal/third-party/`) and the Valve redistributables; `CreditsLegalContentTests` and `LegalReleaseReadinessTests` assert the notice. **Owner action:** legal review of the wording.