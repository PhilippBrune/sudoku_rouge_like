# E2E & Automated Test Report

**Review date:** 2026-10-01 · **Companion docs:** [FULL_GAME_REVIEW](FULL_GAME_REVIEW.md) · [GAME_GAPS](GAME_GAPS.md) · [REQUIREMENT_TRACEABILITY](REQUIREMENT_TRACEABILITY.md) · [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md)

## 1. Environment and method

| Item | Value |
|---|---|
| Code under test | branch `feature/game-ui-refactor` @ `20d5f59` + uncommitted working tree (`Assets/Scripts/Core/InputRemapService.cs`, `ProjectSettings.asset`) |
| Engine | Unity 6000.3.10f1, Windows 11, batchmode for tests; Windows 64-bit player built fresh for the review (`-buildWindows64Player`, "Build Finished, Result: Success") |
| Test framework | Unity Test Framework 1.6.0 (NUnit), EditMode + PlayMode |
| Runtime driving | Built player launched windowed; screenshots and input sent to the game's own window handle (process-targeted helper; no other application received input) |
| Save safety | The `LocalLow/Kratossos/sudoku_rouge_like` folder was backed up before testing and restored byte-for-byte afterwards; the PlayerPrefs key created by the build was removed (none existed before) |

Three layers of evidence:

1. **Baseline suites:** the project's own EditMode/PlayMode tests (§2.1).
2. **Diagnostic tests D01–D25:** temporary EditMode fixtures (`Assets/Scripts/Tests/EditMode/ReviewDiagnostics*Tests.cs`, marked `TEST INFRASTRUCTURE`, `[Explicit]`, category `ReviewDiagnostics`). They drive the **real** runtime services (`RunDirector`, `BossService`, `PuzzleGenerationService`, save services …) with fixed seeds, record actual behaviour, and assert the **current spec**. A failing diagnostic is a finding, not a test bug. Raw output: [`evidence/diagnostics_batch{1,2,3}_probe.txt`](evidence/).
3. **Built-player E2E E01–E21:** real game, real input, real saves (§3). Screenshots: [`evidence/screens/`](evidence/screens/).

> **Test gate note:** the documented runner `tools/run-unity-tests.ps1` passes `-quit` together with `-runTests`, so Unity exits before running any test and writes no XML ([GAP-010](GAME_GAPS.md#gap-010)). All runs below invoked Unity directly with the same arguments minus `-quit`.

---

## 2. Automated tests executed

### 2.1 Baseline suites

| Suite | Total | Passed | Failed | Result |
|---|---:|---:|---:|---|
| EditMode (project tests) | 307 | 262 | 45 | FAIL |
| PlayMode (project tests) | 8 | 3 | 5 | FAIL |

Failures by cause ([full list](evidence/baseline_test_results.txt)):

| Cause | Tests | Gap |
|---|---|---|
| Gameplay defects | `RunFlowAcceptanceTests.GardenRun_…` (expected board size 6, was 0), `…BossFlow_…` (`IsBoss` false), `GameplayEffectContractTests.GinkgoLeaf_…` (HP 10 vs 9), `SaveDurabilityReadinessTests.ResumeService_FlushesPendingAutosaveBeforeReading`, `SaveWriteQueueTests.Flush_…` | GAP-009, GAP-035, GAP-010 |
| PlayMode UI | In-run options panel does not open (×2), `BtnZenRecords` not interactable, `MuteAll` missing, German keys missing ("Start Run", …) | GAP-019, GAP-029 |
| Generated-menu NRE in EditMode | `ConditionalButtonStateTests` ×2, `DropdownCompatibilityTests`, `GeneratedUiRebuildTests` | GAP-010 |
| Missing `docs/release/*` | 8 tests (`PlatformReleaseReadiness`, `StorePlatformAsset`, `ReleaseReadinessStatus`, `LegalReleaseReadiness`) | SPEC-PROC-005 |
| Source-text contract drift | ~20 tests asserting strings in source/docs (wiring tokens, credits, CI docs, banner, victory screen, splash) | GAP-002, GAP-010 |
| Budgets / registers | `ControllerSizeBudgetTests` ×3 (RunDirector 3911 > 3350 lines), technical-debt marker, asset provenance root | GAP-010 |
| Audio assets | `AudioAssetServiceTests` ×2 (`menu_loop.wav` has no RIFF header; missing AudioSource restore) | GAP-030 |

### 2.2 Diagnostic tests (real services, fixed seeds)

Totals: batch 1 17 tests, batch 2 5 tests, batch 3 4 tests; 26 diagnostics in all. They pass only where the code meets the current spec (NUnit results in `TestResults/review-2026-10-01/Diagnostics*-results.xml`).

| Test | Seed(s) | Steps | Expected (spec) | Actual | Result | Evidence |
|---|---|---|---|---|---|---|
| **D01** Determinism | 424200 ×2, 424201 | Start run; first puzzle; solve; reward; advance 4 floors; boss choices; shop | Same seed → identical path, puzzle, modifiers, rewards | Identical traces for the same seed; different seed differs | **PASS** | probe1 |
| **D02** Generation matrix | 9000+size·100+variant·10+stars | `CreatePuzzle` for 5×5–9×9 × variants 0–3 × 1–6★; check solution, regions, givens, missing count, uniqueness | Valid boards; missing = round(N²·(s+3)/10) | All valid and contiguous, counts exact. Uniqueness: many ≥2 solutions (even 9×9 v0 1★); 9×9 6★ has 8 givens. Raw time up to **20.3 s** (9×9 jigsaw) | **PASS** (validity) / finding (uniqueness, time) | probe1 |
| **D02b** Star density | — | `MissingPercentForStars(1..7)` | 0.4 … 0.9, 7★ = 1.0 | 0.40 … 0.90, 1.00 | **PASS** | probe1 |
| **D03** Each modifier, single | 31337+id | Custom-puzzle run per modifier at min size and 9×9; check overlay, solution conflicts, completion | Generates; solution satisfies rules; level completes | 29/30 OK; **AntiBishop never generates on 9×9** (4 s timeout); 0 solution conflicts | **FAIL** (AntiBishop) | probe1 |
| **D04** Boss/floor counts | 5150+H | H0/H7/H10 runs; floors 1–5: floor mods, shown choices, UI-required picks | Floor mods 0,1,2,3,4; shown 3,3,4,5,6; picks 2,2,3,4,5; shown = picks + 1 | Floor mods **0,1,1,1,2**; F1 **2 shown / 1 pick**; H7/H10 shown exceeds picks by 2–3 | **FAIL** | probe1 |
| **D05** Boss injection (sync path) | 1001, 2002, 3003 | Per floor: choose picks, `BuildLevelConfig(boss)`, `StartLevel`, solve, complete | All chosen + floor mods active and drawn; boss board in floor range; XP entry flagged boss | F5 board **NULL** for 2/3 seeds; F3 **Thermo overlay missing**; XP entries 0×0, not boss | **FAIL** | probe1 |
| **D06** Path graph | 11, 22, 33, 44, 55, 66 | Build floors 1–5; BFS reachability; route lengths; cross-links; node before boss | Connected; calm lengths per table; exactly one cross-connection | Connected, no dead ends; lengths OK; **2–8 cross-link nodes/floor** | **FAIL** (cross-links) | probe1 |
| **D07** Class stats | 777 | Start run per class; NumberFreak at L10 | GDS roster; ClassXp level schedule (L10 HP 14, Pencil 13) | Roster = GDS (Koi +1 HP from relic); L10 **HP 15 / Pencil 12** | **FAIL** (schedule) | probe1 |
| **D08** XP curve & isolation | — | `XpTable`; add XP to 3 classes; tile formula | 16,860 XP to L40; classes isolated; tile XP per formula | All correct; excess XP retained (spec: discarded); entry metadata empty | **PASS** (curve) / finding | probe1 |
| **D09** Run-end commit & unlocks | 9090 | Real run tile → `RecordRunAndGetNewUnlocks` (slot 2 backed up/restored) | XP only to played class; Garden Monk after 2 bosses; completion records 6x6_s1 | XP and unlock OK; completion key **"0x0_s0"** | **FAIL** (completion) | probe1 |
| **D10** Reward slots | 4243–4248 | 3,000 rolls per 1–6★ at L1 and L15 | Slots 2/3/3/4/5; Nothing 25/22/18/15/12 %; no Epic below L15 | Slots exact; 1★ Nothing 21.6 %; Epic@L1 = 0 | **FAIL** (minor, 1★ rate) | probe1 |
| **D11** Gold & prices | 31 | `GoldTable`, shop prices, rest heal | GDS formulas; rest +2 HP | Gold/prices exact; rest heal **+3** (MaxHP/3) | **PASS** (gold) / finding | probe1 |
| **D12** Save/resume round trip | 6060, 7070, 8080 | Mid-puzzle save → resume via `LaunchResume` sequence; boss-gate save → resume | Exact state incl. modifiers; same boss choices | Board/state/graph/pencil OK; **modifiers & overlay lost**; **boss choices differ** | **FAIL** | probe1 |
| **D13** Harmony weights | 1234 | 300 level configs at H0 vs H10 | Star/grid distributions differ | Size histograms differ (extra floor modifiers bump sizes), but the **star histograms are identical** | NUnit PASS (assertion too weak); **finding confirmed** | probe1 |
| **D14** Custom puzzle matrix | 4045–4049 | Sizes 5–9 × 1/6/7★, jigsaw | Board generated; 7★ 0 givens; no progression | 7★ OK (7×7, 8×8); **9×9 jigsaw → no board** | **FAIL** | probe1 |
| **D15** HP loss | 5555 | Wrong digit ×13 via `RunDirector` | HP decreases | HP unchanged; penalty lives in UI layer | **FAIL** → finding (GAP-035) | probe1 |
| **D16** Pressure frequency | 20000–20199 | 200 configs per floor × node type | F2 12 %, F3 30 %, F4 55 %, F5 80 % | F2 **0 %**, F3 12 %, F4 30 %, F5 55 % | Probe (passes by design) → **finding**: one floor late | probe1 |
| **D17** Resume then play | 6161, 6162 | Exact `GameBootstrap.LaunchResume` sequence, then place and complete | Play continues | **NullReferenceException** on placement and completion | **FAIL** | probe2 |
| **D18** Boss async (UI path) | 1001…8008 | 8 seeds × 5 floors, `StartLevelAsync` + `BoardSize≥8`; retry on failure | All succeed with chosen mods | 39/40 succeed; **1 permanent failure** (retry identical); **8/40 drop or miss mods**; 8 waits of 24–30 s | **FAIL** | probe2 |
| **D19** Placement semantics | 300–329 | Enter a non-solution digit that fits the rules | Treated as wrong (GDS/BossMechanics) | **30/30 accepted as Correct** | **FAIL** (spec conflict) | probe2 |
| **D20** Late-floor reliability | 9100…9177 | 12 seeds × floors 4–5 × 3 nodes, async | All generate | **7/~70 fail** (9×9 irregular), up to 20 s | **FAIL** | probe2 |
| **D21** Rule semantics | 5000+id | Per rule: solved board, test wrong digits per cell | Rejects wrong digits at constrained cells; accepts solution | 29/29 rule assertions OK (AntiBishop no board) | Assertions **PASS**; NUnit marked Failed only because of the expected AntiBishop timeout error log | probe2 |
| **D22** Endless Zen | 41000–41007 | Start Zen; depths 5/12/25 | Boards generate; functional modifiers | **10/32 no board**; 27/48 draws inert | **FAIL** | probe3 |
| **D23** Spirit Trials | 52000–52004 | 4 tiers × 5 seeds | Boards generate; tier table; functional mods | Tier table OK; **7/20 no board**; 16 inert/pressure draws | **FAIL** | probe3 |
| **D24** Monthly Walk | year·100+month | 24 months 2026–2027 | All months generate | **5/24 fail incl. 2026-10** | **FAIL** | probe3 |
| **D25** Shop/rest/relic/cursed | 7331 | Buy, full bag, reroll, rest, relic choice, cursed config, emergency heal | Spec values | Shop OK; full-bag API charges gold; rest +3 HP / +4 pencil; relic +1; cursed +1 mod ×1.5 | **FAIL** (rest, API) | probe3 |

**Totals:** baseline 315 tests (265 passed, 50 failed). Diagnostics 26 tests: NUnit 7 passed (D01, D02, D02b, D08, D11, D13, D16) and 19 failed. D21's assertions all held (its failure is a logged generation timeout), and D13/D16 passed but still document findings. The failures *are* the review findings.

---

## 3. Built-player E2E sessions

The player was built from the current working tree and run windowed (it opened full-screen at 1920×1080 per saved options). Four sessions, about 60 screenshots; the curated set is in [`evidence/screens/`](evidence/screens/). Each session's log was archived.

| ID | Flow / Test | Seed | Steps | Expected | Actual | Result | Evidence |
|---|---|---|---|---|---|---|---|
| **E01** | Fresh install startup | — | Empty save folder and PlayerPrefs → launch | Profile select → first-run setup → basics prompt | Main menu directly. Setup shown only on the **2nd** launch | **FAIL** | E01, E01b |
| **E02** | Main menu | — | Inspect | Start, Resume (when a run exists), Tutorial, Meta, Modes, Items, Options, Credits, Quit | All present except "Tutorial" (→ "Custom Puzzle"); Resume disabled correctly; Daily/Monthly not on the menu; button heights inconsistent | PARTIAL | E01 |
| **E03** | Class select | — | Start Game | Unlocked/locked classes, stats, passive, level, unlock condition; seed select; irregular toggle | Stats/passive/"Lv1 XP 0/50"/unlock list OK. Locked names unreadable; **no seed field**; toggle state invisible; tooltip overlap; Harmony text clipped | PARTIAL | E03 |
| **E04** | Run confirm | — | Start Run | Summary; irregular default ON | Summary OK; "Irregular puzzles: off"; **no overwrite warning** with an active run | PARTIAL | s34 |
| **E05** | Garden overview F1 | 67968 | Begin | Fully visible lanes, cross-link, player marker, F1 sizes 5–6 | Lanes, legend, floor effect banner OK; tiles beyond first are "?"; first tiles **7×7**; boss **8×8**; no player marker | PARTIAL | E05 |
| **E06** | Puzzle screen | 67968 | Enter tile | Board, HUD, numpad, timers | Works; digits > N disabled; 29 givens / 49 = 1★ density OK | **PASS** | E06 |
| **E07** | Non-solution digit | 67968 | Enter 1 where the solution is 2 (no rule conflict) | Wrong placement (per GDS) | "Placed 1", "Combo ×2", no HP loss | **FAIL** (spec conflict) | E07 |
| **E08** | Conflicting digits | 67968 | Enter a 5 in a row containing 5, twice | −1 HP each (relic may absorb) | 1st absorbed by Cracked Teacup (by design), 2nd −1 HP; red highlight | **PASS** | E08 |
| **E09** | Save & Quit mid-puzzle → Resume | 67968 | Save & Quit → Resume → numpad; then repeat after a full app restart | Exact state; play continues | Board restored but **every entry throws NullReferenceException** (both cases); star/conflict highlight lost; puzzle timer reset | **FAIL (CRITICAL)** | E09, [stack](evidence/e2e_resume_nre_stack.txt) |
| **E10** | Menu after broken resume | 67968 | Save & Quit → click Start/Options/Quit | Responsive menu | Whole menu unresponsive (mouse + keyboard); force close needed. Not reproduced without the prior exception | **FAIL** (1 repro) | session 2 log |
| **E11** | Language (German) | — | Select Deutsch in first-run setup; tour screens | Entire UI German, correct glyphs | Main menu stayed English until restart; mojibake ("ZurÃ¼ck", "LautstÃ¤rke", "Klasse wÃ¤hlen"); many in-run strings English | **FAIL** | E11, E11b |
| **E12** | Game Modes | — | Open | Harmony, Endless/Spirit (locked), Daily, Monthly | All present; Monthly "Der alte Baumhain · 9×9 ★★★★ · Anti-Springer" | **PASS** | E12 |
| **E13** | Monthly Walk 2026-10 | 202610 | Start | 9×9 4★ Antiknight puzzle | Log `BudgetExceeded`, yet "Launched Seasonal Challenge"; **stale 7×7 board from the old run**; Save & Quit refused; options Save & Quit no-op → force close | **FAIL** | E13, E13b, [log](evidence/e2e_monthly_walk_log.txt) |
| **E14** | Solve → reward | 87864 | Solve 7×7 2★ risk tile | Gold 45×1.4 = 63; XP 60×1.2+25 = 97; 3 slots; reroll option | **+63 G, +97 XP, 3 slots**; no reroll button; top gold updates only after closing | **PASS** (formulas) / FAIL (reroll) | E14 |
| **E15** | Shop | 87864 | Enter shop, buy, leave | Floor-1 offers, prices 15/30, reroll 15 | 2 offers (30 G / 15 G), reroll 15 G, gold 83 → 53 | **PASS** | E15 |
| **E16** | Pre-boss + full bag | 87864 | Solve 6×6 2★ elite; pick item with full bag | +42 G, +79 XP; replace-or-nothing | **+42 G, +79 XP**, "Nichts" slots, replace dialog | **PASS** | E16 |
| **E17** | Boss gate → boss puzzle | 87864 | Open gate, pick, confirm | ≥ 3 options/2 picks (table), "???" for unseen, intensity label, chosen modifier active | **2 options / 1 pick**; "???" OK; no intensity label; Difference Kropki applied and visible; 8×8 | PARTIAL | E17, E17b |
| **E18** | Boss defeated → continue | 87864 | Solve boss; click Continue | Stats, Continue, relic choice, transition | Empty stats, **blank button**; relic panel hidden; stuck. Save & Quit → Resume shows relic panel. Boss gold +91 correct | **FAIL (CRITICAL)** | E18, E18b, E18c |
| **E19** | Relic accept → Floor 2 | 87864 | Accept Crimson Fan | Relic added; Floor 2 | Floor 2 "Moss Garden" loads; **relic not added** (HUD Relics 1) | **FAIL** | E19 |
| **E20** | Floor 2 puzzle | 87864 | Enter 7×7 ★★★ tile | Puzzle as previewed, with floor modifier | Floor modifier Nonconsecutive active; puzzle is **8×8** (preview 7×7) | PARTIAL | E20 |
| **E21** | Star rolls F1 | 87864 | Player log | Low stars on F1 | "Floor 0: variant 6★ (base 1★, 20% roll)" | Finding | session 4 log |

Not reached in E2E (blocked by GAP-001/002 or out of time): floors 3–5, final boss and victory, game over and the XP bar animation, level-up, class unlock notification, Items & Relic Codex screen, Meta Progression screen, Custom Puzzle play, Sudoku Basics tutorial play, controller input, item use in a puzzle.

---

## 4. Coverage of the requested flows

| Flow (brief §9) | Covered by | Outcome |
|---|---|---|
| **Main flow**: menu → class → garden → tile → solve → reward → path → boss → mechanic → boss puzzle → complete → next garden | E01–E06, E14–E20, D05, D18 | Works up to the boss; **blocked after boss** (GAP-002) and on resume (GAP-001); next garden only via workaround |
| **Tutorial flow**: size → stars → mechanics → puzzle → solve → completion | D14 (service level), RunFlowAcceptanceTests.TutorialRun (pass) | Service-level OK except 9×9 jigsaw; UI flow not driven in the build |
| **Class flow**: select → run → XP → game over → distribution → level up → unlock | D07–D09, E03, E14/E16 | XP formulas and per-class storage verified; unlock (Garden Monk) verified at service level; game-over screen and animation **not reached** |
| **Item flow**: receive → available → archive → use → effect | E14–E16, D10, D25, GameplayEffectContractTests | Receive / bag / replace / shop verified; reroll missing; archive and in-puzzle use not driven in E2E |
| **Path flow**: start → calm/risk → crossover → availability → boss gate | D06, E05, E14–E17 | Connectivity verified; spec conflicts (hidden tiles, many crossovers, no marker) |
| **Boss flow**: reach → ≥2 choices → "???" → names when known → select → start → modifier applied | D04, D05, D18, E17 | "???" verified; F1 shows exactly 2; injection verified for 1 E2E seed but **fails for 8/40** in the async path |

## 5. Determinism (brief §10)

| Check | Result | Evidence |
|---|---|---|
| Same seed → same path, puzzle, modifiers, rewards, shop (fresh runs) | **PASS** | D01 |
| Different seed → different run | **PASS** | D01 |
| Same seed across save/resume | **FAIL**: boss choices, reward/shop/relic rolls restart | D12, [GAP-018](GAME_GAPS.md#gap-018) |
| Player-entered seed | **Not possible** (no UI) | [GAP-020](GAME_GAPS.md#gap-020) |
| Tutorial / Custom Puzzle | Non-deterministic by design (`UtcNow.Ticks` seed) | TutorialModeSystem v1.6 |

## 6. Save/Load matrix (brief §16)

| State | Save | Quit | Restart | Resume | Exact state? | Evidence |
|---|---|---|---|---|---|---|
| Garden (between tiles) | autosave after tile | ✓ | ✓ (sessions 2–4) | ✓ | HP, gold, items, node, floor ✓ | E09 setup, E18 workaround |
| During puzzle | autosave per correct placement | ✓ | ✓ | ✓ | Board ✓, pencil ✓; **modifiers, overlay, rules, pressure, mistakes ✗ → NRE** | D12, D17, E09 |
| During boss (puzzle) | same as puzzle | — | — | — | Same defect (boss config not persisted) | D17 (code path identical) |
| Boss gate (before choice) | ✓ | ✓ | — | ✓ | **Choices re-rolled** | D12 |
| After boss (relic choice) | ✓ | ✓ | — | ✓ | Relic panel appears; relic not granted after accept | E18c, E19 |
| Reward screen / shop | not persisted | — | — | — | Rolls not persisted (re-roll on rebuild) | D12 (reward re-roll per call), static |
| Class progression | at run end | — | — | — | XP/unlock persisted | D09 |
| Corrupted save / version compatibility | — | — | — | — | Not tested in this review (existing durability tests: 9/11 pass) | baseline |

## 7. Not testable in this review, and why

| Area | Reason |
|---|---|
| Audio (music per context, SFX, mixing, mute) | No audio capture; assets lack RIFF headers; no audio log lines ([GAP-030](GAME_GAPS.md#gap-030)) |
| Controller input | No gamepad attached for this session; existing `InputRemapServiceTests` pass |
| Performance profiling (PERF-CAP-001..007) | Needs Unity Profiler captures; only wall-clock generation times were measured |
| Visual theme or pixel-art consistency judgements | Screenshots reviewed; no reference art spec with measurable criteria |
| Floors 3–5, victory, game over in the build | Blocked by GAP-001/GAP-002 within the session time; covered at service level by D04–D06, D18 |
| Ticket-based requirements | `tickets/` and `testcases/` folders do not exist |

## 8. Cleanup performed

- Player save folder restored to its exact pre-test content (`save_profile_2.json`, byte-compared). Saves written by the E2E sessions were archived in the scratchpad only.
- `HKCU\Software\Kratossos\sudoku_rouge_like` (created by the build) exported to the scratchpad, then deleted. The empty parent key `HKCU\Software\Kratossos` was left in place.
- The review build lives outside the repository (session scratchpad). `build/windows` was not touched.
- Diagnostic test files remain in `Assets/Scripts/Tests/EditMode/ReviewDiagnostics*Tests.cs`, marked as test infrastructure, `[Explicit]`, category `ReviewDiagnostics`. Promote individual cases into permanent tests (see Required Tests in GAME_GAPS) or delete the files.

---

## 9. Addendum: fix verification (2026-10-05)

After the review, GAP-001, GAP-002, GAP-003/004/005/007 and GAP-010 were fixed on request ([fix log](GAME_GAPS.md#fix-log-2026-10-05)). This section records how the fixes were verified. Method as in §1, with two differences: the Editor had the project open, so tests and the player build ran from a mirror of the working tree (`%TEMP%\sr_testcopy`); and the player was driven by scripted mouse input to the game window, with digits taken from the autosave's solution.

### 9.1 Automated tests

| Suite | Review baseline | After the fixes |
|---|---|---|
| EditMode | 307 tests: 262 passed, 45 failed (and `tools/run-unity-tests.ps1` produced no results at all) | **351 tests: 315 passed, 0 failed, 36 skipped** (26 `[Explicit]` review diagnostics, 9 waiting for `docs/release/*`, 1 waiting for SPEC-PROC-007) |
| PlayMode | 8 tests: 3 passed, 5 failed | **9 tests: 9 passed** (new: `BossClearedFlowPlayModeTests`) |

The E2E run below found one more defect (autosave dead after the first launch in a session). Its regression test, `ResumeMidPuzzleTests.SecondRunInSameSession_AutosavesAndKeepsProfileWritesFromBetweenRuns`, was run against the unfixed `RunMapController` first and failed there ("second run must autosave — Expected: 7102 But was: 7101"). It passes with the fix. Both suites were re-run after that fix; the counts above are from that run.

### 9.2 Generator benchmark

Standalone harness, 9×9, 8 seeds per case, 6 s deadline ([before](evidence/generator_benchmark_before.txt), [after](evidence/generator_benchmark_after.txt)):

| Case | Before p50 / p95 | After p50 / p95 |
|---|---|---|
| Irregular (v2), 90 % missing, no rules | 2163 / 5512 ms | **57 / 78 ms** |
| Irregular (v2), 40 % missing, no rules | 99 / 1534 ms | 5 / 24 ms |
| Rectangular, 90 % missing, Anti-Knight | 2940 ms / timeout (1 of 8) | 154 / 315 ms |
| Rectangular, 90 % missing, Nonconsecutive-diagonal | 5934 ms / timeout (3 of 8) | 197 / 313 ms |
| Irregular or jigsaw + any global structural rule | timeout on every seed | timeout on 6–8 of 8 seeds → the game now uses rectangular regions for these (SPEC-AMB-014) |
| Anti-Bishop, any region type | timeout on every seed | timeout on every seed → excluded from the pools (SPEC-CONF-038) |

Level pipeline (EditMode, `GenerationReliabilityTests`): 15 boss puzzles across floors 1–5 with player-chosen modifiers in 7.8 s total; all 24 Monthly Walk months from 2026-01 to 2027-12 in 0.7 s, including the 5 months that failed in the review (E13).

Review diagnostic D14 re-run on 2026-10-05 (Custom Puzzle on jigsaw regions, sizes 5–9 at 1★, 6★ and 7★): all 13 valid combinations generate, including 9×9 at 1★/6★/7★, which had no board in the review. 5×5 and 6×6 at 7★ with Killer Cages are rejected by the setup validation (Killer Cages needs at least 7×7), the same as in the review. The diagnostic picks that invalid input; this is not a defect.

### 9.3 Built-player sessions

Fresh Windows build of the fixed tree, 1920×1080. Session G: one Garden Run (seed 91529, Number Freak, Garden Path H0). Before every puzzle: Save & Quit, then Resume, so each puzzle was played on a resumed run (7 resumes in one session). Session H: Monthly Walk on a fresh profile. Log excerpt: [e2e_fix_verification_log.txt](evidence/e2e_fix_verification_log.txt). Neither log contains an exception or error line (115 autosaves in total).

| ID | Flow / Test | Steps | Expected | Actual | Result | Evidence |
|---|---|---|---|---|---|---|
| **F01** | Resume mid-puzzle (GAP-001) | 6×6 tile → 4 digits → Save & Quit → Resume → 2 digits → finish | Exact state; play continues; level completes | Resume button shows "Number Freak · Floor 1 · HP 10/10"; board identical; digits accepted, combo continues, autosave after resume; puzzle completes: +50 G, +70 XP, perfect | **PASS** | [F01a](evidence/screens/F01a_resume_offered_after_save_quit.jpg), [F01b](evidence/screens/F01b_resumed_board_identical.jpg), [F01c](evidence/screens/F01c_placements_after_resume.jpg), [F01d](evidence/screens/F01d_resumed_puzzle_completes.jpg) |
| **F02** | Monthly Walk 2026-10 (GAP-004) | Game Modes → Monthly Walk → Begin → 1 digit → Save & Quit → Begin → 1 digit → Save & Quit | 9×9 4★ Anti-Knight; the attempt is saved and resumed | Log "Using rectangular regions … Antiknight"; puzzle shown; save holds the board, level config and Anti-Knight; second Begin logs "Resumed Seasonal Challenge" with the digit in place; second digit saved | **PASS** | [F02a](evidence/screens/F02a_monthly_walk_2026-10.jpg), [F02b](evidence/screens/F02b_monthly_walk_resumed.jpg) |
| **F03** | Boss → Continue → relic → Floor 2 (GAP-002), with a boss resume | Rest, shop, 4 puzzles → boss gate (pick 1 of 2) → boss 8×8 2★ Greater/Less → Save & Quit → Resume → solve → Claim Reward → relic → Accept | Chevrons survive resume; labelled button; relic choice visible; relic added; Floor 2 | Chevrons and modifier text identical after resume; "Boss Defeated!" with stats and a labelled "Claim Reward ->"; relic choice (3 relics + Leave) over the path, not under the board; Copper Tortoise added (HUD Relics 2); Floor 2 "Moss Garden"; floor saved | **PASS** | [F03a](evidence/screens/F03a_boss_resumed_with_modifier.jpg), [F03b](evidence/screens/F03b_boss_defeated_claim_reward.jpg), [F03c](evidence/screens/F03c_relic_choice_visible.jpg), [F03d](evidence/screens/F03d_floor2_relics_2.jpg) |
| **F04** | Reward rolls after resume (GAP-018, not in scope) | Reward screen of 4 puzzles, each completed after a resume | Different rolls | Same two items (Garden Lantern, Koi Reflection) four times | **FAIL** (known, GAP-018) | [F04](evidence/screens/F04_same_reward_after_resume.jpg) |
| **F05** | Codex discovery persistence | Claim an item, read the save; autosave, read again; accept a boss relic | Discoveries persist | `ItemCodex [GardenLantern]` → `[]` after the next autosave; the relic never reached `DiscoveredRelics` | **FAIL** → new [GAP-037](GAME_GAPS.md#gap-037) | save file reads |
| **F06** | Second launch in one session (found while testing F01) | First build of this pass: Monthly Walk, then Garden Run in the same session; place digits | Autosave after each correct placement | No `[AutoSave]` line in the whole session; the save had no active puzzle, so Save & Quit would have lost both runs. Fixed (`RunMapController.BindAutoSave`). After the fix, all 7 resumes in session G and the Monthly Walk resume in session H autosaved | **FAIL → FIXED** | session logs, regression test (§9.1) |

### 9.4 Save/Load matrix after the fixes (update of §6)

| State | Before | After | Evidence |
|---|---|---|---|
| During puzzle | Board only; NullReferenceException on input | Board, rules, overlay, counters, fog and pressure restored; level completes | F01, `ResumeMidPuzzleTests` |
| During boss puzzle | Boss config lost | Chosen modifiers and overlay restored | F03, `ResumeBossPuzzle_KeepsChosenBossModifiersActive` |
| Monthly Walk attempt | Stale board, save refused | Saved and resumed without regenerating | F02 |
| Any run after the first launch in a session | Never autosaved | Autosaves | F01, F02, F06, regression test |
| Boss gate, reward, shop, relic rolls | Re-rolled after resume | Unchanged (GAP-018) | F04 |
| Codex discoveries during a run | (not checked in the review) | Lost on the next autosave | F05, GAP-037 |

### 9.5 Not verified in this pass

- Listening check of the repaired WAV files (GAP-030): the authored pack now loads, but nobody has listened to it.
- Endless Zen and Spirit Trials in the build: locked on a fresh profile; covered by EditMode tests only.
- A resume between the boss and the relic choice (the GAP-021 path).
- Controller or keyboard focus on the new boss-cleared button: asserted in the PlayMode test, not driven in the build.
- Fresh-install flow (GAP-006, not in scope). With slot 2 present and no PlayerPrefs, the first launch showed first-time setup and the basics prompt; that is not the review's empty-folder case.

### 9.6 Cleanup

As in §8: after each session the save folder was restored to its pre-test content (`save_profile_2.json`, byte-compared), and `HKCU\Software\Kratossos\sudoku_rouge_like` was deleted, leaving the empty parent key as before. The build and the session logs are in the session scratchpad. The test mirror `%TEMP%\sr_testcopy` can be deleted.

## 10. Addendum: release 1.1.0 (2026-10-07)

For release 1.1.0 every remaining gap and every specification recommendation was implemented ([GAME_GAPS 1.1 fix log](GAME_GAPS.md#fix-log-2026-10-07-release-110), [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md)). Method as in §9: tests and the player build ran from a mirror of the working tree because the Editor had the project open; the build was driven by window-handle input and screenshots.

### 10.1 Automated tests

| Suite | After 2026-10-05 | Release 1.1.0 |
|---|---|---|
| EditMode | 351 tests: 315 passed, 0 failed, 36 skipped | **492 tests: 466 passed, 0 failed, 26 skipped** (2026-10-13) (the `[Explicit]` review diagnostics; the 10 tests that waited for `docs/release/*` and SPEC-PROC-007 now run) |
| PlayMode | 9 of 9 passed | **25 of 25 passed** (new: `InRunUiPlayModeTests` 13, `StartupFlowPlayModeTests`, `AudioClipPlayModeTests`, home widgets in `GeneratedUiPlayModeTraversalTests`) |

`StartupFlowPlayModeTests` was run against the unfixed `MainMenuController` first and failed there. The pressure PlayMode tests found GAP-039; implementing the debuffs found GAP-040; the class roster test found GAP-041; resume determinism found GAP-038.

### 10.2 Built-player session (smoke test)

Fresh Windows build of the 1.1 tree, 1920×1080, empty save folder and no PlayerPrefs (the review's fresh-install case). Session B: profile select → first-run setup → basics prompt → Garden Run as Number Freak with entered seed 12345 → one puzzle with two wrong digits → Save & Quit → Resume → finish → reward reroll → claim → Save & Quit → new run over the saved one (Abandon & Begin). Log excerpt: [e2e_release_110_log.txt](evidence/e2e_release_110_log.txt); no exception in the whole log, all 49 authored clips loaded.

| ID | Flow / Test | Expected | Actual | Result | Evidence |
|---|---|---|---|---|---|
| **B01** | Fresh install (GAP-006) | Profile select first | Profile select shown; log `Startup step: ProfileSelect`. **Defect found:** empty slots showed the raw key `Profile.EmptySlot` (German text only). Fixed: English and fr/es/pt text, plus `LocalizationCatalogQualityTests.KeyOnlyLookups_HaveEnglishText` (found 4 more such keys, all fixed); re-checked as B23 | **PASS after fix** | [B01](evidence/screens/B01_profile_select_raw_key.jpg), [B23](evidence/screens/B23_profile_select_fixed.jpg) |
| **B02** | First-run setup (SPEC-CONF-032) | Five languages, readable toggles | English, Deutsch, Français, Español, Português (BR); toggles drawn as boxes; legal notice; gamepad hint | **PASS** | — |
| **B03** | Basics prompt and home widgets (GAP-006, GAP-028) | Basics prompt after setup; Daily/Monthly widgets on the main menu | "Yes, show me / Skip" prompt; widgets: "0/3 goals today · streak 0", "The Old Tree Grove · Resets in 25 days", each with Open | **PASS** | [B03](evidence/screens/B03_basics_prompt_and_widgets.jpg) |
| **B04** | Class screen (GAP-023, GAP-041, SPEC-CONF-031) | Irregular ON by default; level schedule with L3 "+1 Pencil" | Toggle filled (ON); schedule lists "[L3] +1 Pencil" | **PASS** | [B05](evidence/screens/B05_class_select_irregular_on.jpg) |
| **B05** | Seed entry (GAP-020) | Optional seed; entered seed used | Confirm modal: "Irregular puzzles: ON", "Seed (optional)"; 12345 entered; log `seed=12345`; in-run menu "Seed: 12345" | **PASS** | [B06](evidence/screens/B06_confirm_seed_field.jpg), [B12](evidence/screens/B12_inrun_menu_seed.jpg) |
| **B06** | Path map (GAP-022, SPEC-CONF-010/030) | All tile types visible, one bridge per floor, player marker | No "?" tiles; exactly one ⇄ bridge with badges on both tiles; class marker on Start, then on the played tile. Cosmetic: the marker overlaps the lane label, and "Puzzle (Calm)" overlaps "Legend" at the bottom left | **PASS** (cosmetic notes) | [B08](evidence/screens/B08_path_map_marker_one_bridge.jpg), [B19](evidence/screens/B19_marker_moved.jpg) |
| **B07** | Wrong digit (SPEC-CONF-003, GAP-011/035) | A non-solution digit without a visible clash is rejected and costs HP | Irregular 6×6 1★. First wrong digit: "4 is wrong here", absorbed by the starting relic; second: "1 is wrong here. HP: 9", HP 10 → 9; digit not placed | **PASS** | [B11](evidence/screens/B11_wrong_digit_costs_hp.jpg) |
| **B08** | Mistake autosave (GAP-031) | Mistake survives Save & Quit | `[AutoSave] HP=9` right after the mistake; Resume button "Number Freak · Floor 1 · HP 9/10" | **PASS** | log |
| **B09** | In-run menu (GAP-020) | Seed, View Modifiers, Abandon Run | All present; View Modifiers lists "No modifiers are active" (Floor 1). **Defect found:** the list was see-through and its text overlapped the Options labels; made opaque, asserted in `InRunUiPlayModeTests.RunMenu_*` | **PASS after fix** | [B13](evidence/screens/B13_view_modifiers_transparent.jpg) |
| **B10** | Resume mid-puzzle (GAP-001, GAP-038) | Board, HP, run timer restored; play continues | HP 9/10, placed digit kept, run timer continued; 13 more digits accepted; puzzle completed | **PASS** | [B15](evidence/screens/B15_resumed_board.jpg) |
| **B11** | Digit styles (GAP-029) | Givens and player digits distinguishable | Bold ivory givens, blue player digits, dark digit on the selected cell | **PASS** | [B15](evidence/screens/B15_resumed_board.jpg) |
| **B12** | Reward and reroll (GAP-015) | +XP by the tile formula; reroll costs 20 + 20n gold; Nothing stays | +50 G, +45 XP (6×6 base 45, 1★, not perfect); Reroll (20g): gold 50 → 30, item changed, Nothing kept, next reroll 40 g and disabled at 30 g | **PASS** | [B17](evidence/screens/B17_rewards_reroll_button.jpg), [B18](evidence/screens/B18_after_reroll.jpg) |
| **B13** | Codex persistence (GAP-037) | Claimed item still in the codex after later autosaves | `ItemCodex: InkWell` present after several autosaves and a new run | **PASS** | save file read |
| **B14** | New run over a saved run (GAP-032) | Warning; abandoned run's XP kept | "Your saved run will end as a defeat. Its XP and progress are kept." with Abandon & Begin; new run started; Number Freak TotalXp 45; log `Abandoned saved run (class NumberFreak, XP 45)` | **PASS** | [B21](evidence/screens/B21_abandon_and_begin.jpg) |
| **B15** | Stability | No exceptions | 0 exceptions, 0 errors; startup steps logged; 49 clips loaded | **PASS** | log |

### 10.2b Display modes (2026-10-12/13)

Added on request: Windowed and Frameless Window (GameDesignSpec 1.7 §Display Modes, DISPLAY-MODE-001..006). Checked in a fresh 1.1 build on a 3440×1440 display (work area 3440×1392); window rectangles and styles were read with the Win32 API, screenshots show only the game window (the desktop is masked).

| ID | Flow / Test | Expected | Actual | Result | Evidence |
|---|---|---|---|---|---|
| **B16** | Options → Display Mode → Windowed (resolution 1280×720) | Framed window, 1280×720 client, centred | Outer 1296×759 at 1072,316 with title bar, client 1280×720; saved mode 2 | **PASS** | [B16](evidence/screens/B16_windowed_mode.jpg) |
| **B17** | Windowed → Frameless Window | No frame, 1280×720, centred | First pass: frame removed but Windows kept the old outer size (client 1296×759). Fixed: `WindowFrameService` checks client size and frame and re-applies (up to 3 times). After the fix: 1280×720 at 1080,336, no caption | **PASS after fix** | [B17](evidence/screens/B17_frameless_window_mode.jpg) |
| **B18** | Restart with Frameless Window saved; Alt+Enter twice | Starts frameless; fullscreen and back restores the frameless window | Starts 1280×720 frameless, centred; Alt+Enter → 3440×1440; Alt+Enter → 1280×720 frameless again | **PASS** | [B18](evidence/screens/B18_frameless_at_startup.jpg) |
| **B19** | Display Mode → Borderless Fullscreen | Borderless window covering the display | 3440×1440 at 0,0, no caption; saved mode 1 | **PASS** | [B19](evidence/screens/B19_borderless_fullscreen.jpg) |

**Defect found (GAP-042, fixed):** every dropdown list in the main-menu Options was shorter than its items, so the last option was hidden or cut off. In the 1.1 build the Display Mode list showed "Fullscreen", "Borderless" and half of "Windowed", which is likely why no window mode seemed to exist. After the fix the list shows all four modes on an opaque background.

### 10.3 Not verified in the build

- Boss gate, boss debuffs from Floor 3, modifier legend, pressure visuals, prestige and the Monthly Walk widget's Open target: covered by PlayMode/EditMode tests (`InRunUiPlayModeTests`, `BossDebuffTests`, `PrestigeTests`, `GeneratedUiPlayModeTraversalTests`), not played to in the build.
- Listening check of the audio (GAP-030); the music loops are 6.0 s mono.
- Controller-only navigation of the new 1.1 UI (seed field, home widgets, legend).
- French, Spanish and Portuguese in-run text (431 gameplay keys fall back to English, FUTURE-014).

### 10.4 Cleanup

The user's save folder was backed up before the session and restored afterwards (`save_profile_2.json`, byte-compared after each of the two sessions); `HKCU\Software\Kratossos\sudoku_rouge_like`, created by the player, was deleted, leaving the parent key as before.
