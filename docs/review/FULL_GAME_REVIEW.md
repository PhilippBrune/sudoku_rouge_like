# Run of the Nine — Full Game Review & Gap Analysis

**Review date:** 2026-10-01
**Code under review:** branch `feature/game-ui-refactor` @ `20d5f59` plus the uncommitted working tree (`InputRemapService.cs`, `ProjectSettings.asset`). Unity 6000.3.10f1, fresh Windows 64-bit player build.
**Deliverables:** this document · [GAME_GAPS](GAME_GAPS.md) (actionable gaps) · [REQUIREMENT_TRACEABILITY](REQUIREMENT_TRACEABILITY.md) (500 requirements) · [E2E_TEST_REPORT](E2E_TEST_REPORT.md) · [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md) · [`evidence/`](evidence/)

This is a review. No gameplay code was changed. The only repository additions are these documents, the evidence folder, and three clearly marked, `[Explicit]` diagnostic test fixtures used to collect runtime evidence (§14).

> **Update 2026-10-05:** after the review, GAP-001, GAP-002, GAP-003/004/005/007 and GAP-010 were fixed on request and verified in tests and in a fresh build. See [§15](#15-addendum-fix-pass-2026-10-05) and the [GAME_GAPS fix log](GAME_GAPS.md#fix-log-2026-10-05). Sections 1–14 describe the state on 2026-10-01.
>
> **Update 2026-10-07 (release 1.1.0):** every remaining gap fix and every specification recommendation was implemented, the repository was cleaned up and release 1.1.0 prepared. See [§16](#16-addendum-release-110-2026-10-07).

---

## 1. Executive summary

A fresh Garden Run **works end-to-end through the first floor**: class select, garden map, puzzles, rewards, shop, pre-boss, boss gate with "???" modifiers, and a boss puzzle with the chosen modifier visible. Core rules are solid at the service level:

- the generator always produces valid boards for 5×5–9×9 and all four region variants, with exact star densities;
- every one of the 29 generatable modifier rules accepts the stored solution and rejects wrong digits;
- the XP curve, per-class XP storage, gold formulas, shop prices and reward-slot counts match the specs;
- runs are deterministic for a given seed.

Three defects stop the game from being completed in normal play (P0):

1. **[GAP-001](GAME_GAPS.md#gap-001): resuming a run saved mid-puzzle crashes every digit entry** (NullReferenceException), both in-session and after restarting the game. The game autosaves after every correct placement, so this is the normal Save & Quit path.
2. **[GAP-002](GAME_GAPS.md#gap-002): after a non-final boss the player cannot continue.** The Continue button renders without a label, and the relic-choice panel stays hidden behind the solved board. Only Save & Quit → Resume reveals it.
3. **[GAP-003](GAME_GAPS.md#gap-003): puzzle generation fails or stalls on 9×9 irregular boards and structural modifiers.** A failed boss is retried with the identical seed, so it fails forever (1 of 40 bosses; ~10 % of late-floor puzzles). Successful bosses sometimes take 24–30 s.

Major P1 issues:

- this month's **Monthly Walk can't generate**: it shows the previous run's board and traps the player;
- **chosen boss modifiers are silently dropped** in 20 % of bosses;
- a **fresh install skips profile and first-run setup**;
- **Endless Zen and Spirit Trials** mostly draw inert "future" modifiers and often fail to generate;
- **Harmony difficulty** is largely unwired;
- the **test gate itself does not run tests** (`-quit` with `-runTests`), and 50 of 315 existing tests fail.

On the specification side, the documents disagree in about 37 places. Several 2026-04-27 rewrites describe the code rather than requirements, and 116 of the 436 requirement IDs are auto-generated placeholders. Only **1 of 436** spec requirements is referenced by any automated test.

---

## 2. Final report

| Metric | Value |
|---|---:|
| **Total requirements** | **500** (436 found in `docs/` + 64 new `REQ-<SYS>-1xx` created for un-ID'd requirements) |
| Implemented (verified) | 72 |
| Partially implemented | 16 |
| Broken | 37 |
| Missing | 21 |
| Contradicted | 25 |
| Outdated | 126 (116 legacy auto-generated IDs, 4 future-scope, 6 superseded) |
| Untestable | 203 (194 not verified: coverage gap; 9 infrastructure-limited) |

Of the **374 current (non-outdated) requirements**, 72 (19 %) are verified as implemented. 208 (56 %) have neither an automated test nor runtime evidence. Most of the 194 items marked UNTESTABLE (coverage gap) are in the heavily specified Pressure (47), Curse (48) and Debuff (34) families.

| Critical gaps | Count | Gaps |
|---|---:|---|
| **P0** game cannot be completed | 3 | GAP-001, GAP-002, GAP-003 |
| **P1** major system broken | 6 | GAP-004, GAP-005, GAP-006, GAP-007, GAP-008, GAP-010 |
| **P2** important feature incomplete | 14 | GAP-009, 011–021, 023, 030 |
| P3 minor | 11 | GAP-022, 024–028, 031–035 |
| P4 polish | 2 | GAP-029, GAP-036 |

| Automated tests executed | Total | Passed | Failed |
|---|---:|---:|---:|
| Project EditMode suite | 307 | 262 | 45 |
| Project PlayMode suite | 8 | 3 | 5 |
| Review diagnostics (D01–D25) | 26 | 7 | 19 (findings by design) |
| **Sum** | **341** | **272** | **69** |

Built-player E2E scenarios (E01–E21): 21 executed. 5 pass, 1 pass with a finding (E14: formulas correct, reroll missing), 6 partial, 8 fail, and 1 observation only (E21).

**Top recommended next steps**
1. **Fix resume (GAP-001):** persist the level config, rebuild constraint engine, overlay and pressure on restore, and add a legacy-save fallback. This is the most common path to a broken run.
2. **Fix the boss-cleared flow (GAP-002):** build the Continue button with the standard factory (font and localization), show the relic panel on top, and cover it with a PlayMode test. Without it no run reaches Floor 2.
3. **Make generation never block (GAP-003, GAP-004, GAP-005, GAP-007):** salted-seed retries with defined degradation, faster 9×9 irregular/structural generation, pool gating (AntiBishop, Entropy + irregular, full-enum draws), and treat chosen modifiers as mandatory.
4. **Restore the test gate (GAP-010):** remove `-quit` from `tools/run-unity-tests.ps1`, triage the 50 failures, isolate tests from player saves, and link tests to requirement IDs. Promote the review diagnostics into permanent tests.
5. **Resolve the specification conflicts** ([SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md)): placement semantics, floor/boss counts, class roster, cross-links, shop, Endless/Spirit unlocks. Retire the 116 placeholder IDs, and make REQUIREMENT_MAP ✅ require an evidence link.

---

## 3. Scope, sources and method

**Sources, in the order requested:**
1. Requirement and spec documents: all 52 `docs/*.md` (~8,800 lines), including `REQUIREMENT_MAP.md`.
2. Design documentation: GameDesignSpec, AudioVisualDirection, narrative and startup docs.
3. Tickets/specs: `tickets/`, `tickets/ai-spec/`, `tickets/to_review/` and `testcases/` **do not exist** in the repository ([SPEC-PROC-006](SPECIFICATION_ISSUES.md#spec-proc-006)).
4. Current tests: 48 test files (EditMode 45, PlayMode 3).
5. Implementation: `Assets/Scripts` (Bootstrap, Run, Boss, Sudoku, Economy, Items, Meta, Save, UI …).
6. Running game behaviour: a fresh build.

When documents contradicted each other, the conflict was recorded, not resolved ([SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md)). The newest-looking source is named, but no decision was taken.

**Evidence layers** (details in the [E2E report](E2E_TEST_REPORT.md)):
- **Baseline:** the project's own Unity test suites, run in batchmode.
- **D01–D25:** diagnostic EditMode tests that drive the real `RunDirector`, `BossService`, `PuzzleGenerationService`, profile and save services with fixed seeds, and assert the current spec. These show what code actually does at runtime without the UI.
- **E01–E21:** the built player, operated through its own window (screenshots, mouse and keyboard), with the player log captured.
- **Static analysis:** used only to find root causes or to prove absence (no caller or consumer). Static findings are labelled as such.

**Rule applied:** a requirement is `IMPLEMENTED` only with runtime evidence (a passing test or an observed behaviour). An annotation `// [REQ: ID]` or a matching method name is never sufficient.

**Limitations:**
- Floors 3–5, victory, game over and the XP animation were not reached in the built player (blocked by GAP-001/002); they are covered at service level only.
- No gamepad or audio capture was available.
- Profiler captures (PERF-CAP) were not run.
- Each E2E scenario was performed once or twice; reproduction counts are stated.

---

## 4. Requirement inventory

- **436 IDs** in `docs/`, in two schemes:
  - canonical `<SYSTEM>-<CATEGORY>-NNN` (e.g. `PRESSURE-TICK-005`, `DEBUFF-DEF-012`, `XP-TILE-002`);
  - **116 legacy `REQ-<CAT>-NNN` IDs** generated by the local "SPICE" pipeline, with placeholder titles such as "TITLE" and "verified" meaning only "a file exists". These are marked `OUTDATED`.
- **64 new IDs** (`REQ-SUD/BOSS/PATH/RUN/ITEM/ECON/CLASS/XP/SAVE/UI/AUDIO/MODE/HARM/TEST/PERF/TUT-1xx`) cover requirements stated in prose without IDs (mostly GameDesignSpec and the review brief). They are numbered from 100 so they cannot collide with existing IDs; legacy `REQ-BOSS` stops at 035, `REQ-UI` at 036, `REQ-SAVE` at 018. Existing IDs were preserved.
- Each requirement row in the matrix has an ID, description, source document and section, system, code reference, runtime evidence, automated test, status, and gap link. Priority and acceptance criteria for gap items are in [GAME_GAPS](GAME_GAPS.md).

| System | Impl. | Partial | Broken | Missing | Contradicted | Outdated | Untestable | Total |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Sudoku Engine & Generation | 9 | 0 | 1 | 0 | 5 | 0 | 3 | 18 |
| Boss Modifiers & Boss Flow | 5 | 1 | 2 | 3 | 4 | 1 | 1 | 17 |
| Boss Debuffs | 2 | 0 | 0 | 4 | 0 | 0 | 34 | 40 |
| Pressure Mechanics | 3 | 0 | 2 | 0 | 0 | 0 | 47 | 52 |
| Roguelike Run & Path | 10 | 1 | 2 | 2 | 5 | 0 | 5 | 25 |
| Classes & XP | 7 | 1 | 2 | 1 | 4 | 0 | 4 | 19 |
| Meta Progression | 7 | 7 | 1 | 4 | 0 | 0 | 9 | 28 |
| Items, Relics & Economy | 6 | 2 | 1 | 2 | 2 | 1 | 4 | 18 |
| Curses | 0 | 0 | 0 | 0 | 0 | 0 | 48 | 48 |
| Save / Load | 6 | 1 | 3 | 0 | 0 | 1 | 2 | 13 |
| Onboarding & Tutorial | 6 | 1 | 4 | 0 | 0 | 3 | 3 | 17 |
| Input, Profiles & Accessibility | 2 | 0 | 1 | 0 | 0 | 0 | 8 | 11 |
| Game Modes (Endless, Spirit, Monthly, Daily) | 5 | 2 | 8 | 0 | 2 | 0 | 19 | 36 |
| Harmony Difficulty | 4 | 0 | 0 | 5 | 2 | 0 | 7 | 18 |
| UI / UX, Localization & Audio | 0 | 0 | 7 | 0 | 1 | 0 | 2 | 10 |
| Performance & Test Infrastructure | 0 | 0 | 3 | 0 | 0 | 0 | 7 | 10 |
| Future Scope Register | 0 | 0 | 0 | 0 | 0 | 4 | 0 | 4 |
| Legacy auto-generated IDs | 0 | 0 | 0 | 0 | 0 | 116 | 0 | 116 |
| **Total** | **72** | **16** | **37** | **21** | **25** | **126** | **203** | **500** |

---

## 5. System-by-system review

Each subsection follows the same pattern: what the spec requires, what the code does, what was observed at runtime, and the verdict.

### 5.1 Sudoku engine
- **Spec:** 5×5–9×9; star density `(s+3)×0.1` (1–6★, 7★ tutorial-only); four region variants; seeded determinism; HP/pencil resources; Undo removed (GDS v1.4).
- **Code and runtime:**
  - Solved boards are valid for every size × variant × star combination, regions are contiguous, and missing-cell counts are exact (D02, D02b).
  - 9×9 irregular generation is heavy-tailed: 6–20 s raw (D02), failing in the sync path for Custom Puzzle 9×9 jigsaw (D14) and in late floors (D20). → [GAP-003](GAME_GAPS.md#gap-003)
  - **Uniqueness is not guaranteed** outside Spirit Trials and Monthly Walk; even 9×9 1★ boards can have 2+ solutions, and 9×9 6★ (8 givens) never has a unique solution.
  - **Placement is judged by rule conflicts, not the solution:** 30/30 rule-valid wrong digits were accepted (D19, E07). The specs conflict on this. → [GAP-011](GAME_GAPS.md#gap-011)
  - HP loss is applied by the UI, not by `RunDirector` ([GAP-035](GAME_GAPS.md#gap-035)). In the build, conflicting digits cost HP and a relic absorbed one as designed (E08).
  - Undo is still bound (Z / Ctrl+Z) despite the spec ([SPEC-CONF-016](SPECIFICATION_ISSUES.md#spec-conf-016)).
  - Pencil consumption and multi-select controls were not verified (single-cell selection only).
- **Verdict:** generation correctness ✅; generation reliability ❌ (P0); placement semantics ⚠️ (spec decision needed).

### 5.2 Sudoku modifiers (30 player modifiers)
- **Spec:** 30 modifiers (BossMechanics v2.2; many places still say 15) with geometry, rules, rendering, legend and descriptions.
- **Runtime:**
  - Every generatable modifier produced geometry that the stored solution satisfies, and its rule rejects wrong digits at constrained cells (D03, D21).
  - **AntiBishop never generates on 9×9** (its only eligible size in code).
  - In real boss configs, modifiers were dropped or missing overlays in 8 of 40 bosses (D18) → [GAP-005](GAME_GAPS.md#gap-005).
  - Rendering was verified for Difference Kropki (white dots, rule text) and Nonconsecutive (rule text) in the build (E17, E20).
  - The legend panel with tap-to-isolate is missing ([GAP-025](GAME_GAPS.md#gap-025)).
  - Minimum board sizes and pool rules differ from the spec in about 12 places ([GAP-012](GAME_GAPS.md#gap-012)).
  - Endless Zen and Spirit Trials draw from 110 enum values, 64 of which have no implementation ([GAP-007](GAME_GAPS.md#gap-007)).
- **Combination with other modifiers:** solvable by construction (geometry derived from the solution). Combined budgets fail (D05/D18/D20).

### 5.3 Pressure mechanics
- **Spec:** placement-based countdown, haunted cell, crumbling region and wave, on elite, pre-boss and boss nodes from Floor 2 (12/30/55/80 %).
- **Runtime:**
  - Rolls occur only on the right node types and never on Floor 1 (D16), but **one floor late**: Floor 2 is never affected and Floor 5 caps at 55 % ([GAP-014](GAME_GAPS.md#gap-014)).
  - Pressure IDs appear in boss configs (D05/D18).
  - Init, tick, expiry and UI rendering (47 requirements) were **not verified**; there is no automated test.
  - Pressure state is lost on resume (PRESSURE-SAVE-002 broken through GAP-001).

### 5.4 Roguelike run and path
- **Spec:** 5 floors; Calm/Risk lanes; one crossover; elite before boss; positive floor effects; cursed and rest nodes; floor transition.
- **Runtime:**
  - The graph is connected with no dead ends on 30 floors; lane lengths match the table; both lanes end in a pre-boss (D06).
  - **2–8 cross-link "bridge" nodes** per floor instead of exactly one, and tiles beyond the first are hidden as "?" (E05). → [GAP-022](GAME_GAPS.md#gap-022)
  - The floor effect banner works (E05); the cursed node adds +1 modifier ×1.5 (D25); rest values differ ([GAP-024](GAME_GAPS.md#gap-024)).
  - The floor transition is blocked by GAP-002.
  - Board sizes exceed floor ranges, and the map preview differs from the generated puzzle (E05/E20, [GAP-013](GAME_GAPS.md#gap-013)).
  - No seed entry, View Seed or Abandon ([GAP-020](GAME_GAPS.md#gap-020)).

### 5.5 Garden overview (UI)
- **Present** ([E05 screenshot](evidence/screens/E05_garden_floor1.jpg)):
  - floor progress bar and a HUD line (area, HP, pencil, relics, floor effect);
  - two lanes with name and tile count ("Garden Walk · 9 tiles", "Briar Crossing · 7 tiles") and a cross-link line;
  - the Boss Gate tile;
  - a legend of all 8 node types;
  - Save & Quit, and a status prompt.
- **Missing or wrong:**
  - No player marker.
  - Tiles beyond the first are hidden.
  - The HUD line stays English when German is selected (E14).
  - Tile labels are very small and low-contrast at 1080p.
  - A tile preview didn't match its generated puzzle (E20).
- **Save/resume:** the map state (node, path, floor, modifiers) is restored correctly (D12), apart from the resume bugs.

### 5.6 Boss system
- **Runtime:**
  - The number of choices is floor+2 shown / floor+1 picked (1-of-2 on Floor 1), against the spec table of 2-of-3 ([GAP-012](GAME_GAPS.md#gap-012)).
  - At Harmony 7/10 the gate shows more options than the rule allows ([GAP-008](GAME_GAPS.md#gap-008)).
  - Unseen modifiers show "???" (E17).
  - The chosen modifier was injected and visible on the E2E seed, but **dropped or missing in 20 % of D18 bosses** ([GAP-005](GAME_GAPS.md#gap-005)).
  - Floor + boss modifiers stack as specified when not dropped (D18).
  - Intensity scales by run number; no title label ([GAP-025](GAME_GAPS.md#gap-025)).
  - Boss boards are forced to 8×8 or larger ([GAP-013](GAME_GAPS.md#gap-013)).
  - 1 of 40 boss puzzles could never be generated ([GAP-003](GAME_GAPS.md#gap-003)).
  - Boss defeat flow is broken ([GAP-002](GAME_GAPS.md#gap-002)).
  - Multi-stage bosses (GDS "final 3-phase boss") were not observed; the code uses a single boss puzzle per floor.
  - Debuffs are never delivered ([GAP-026](GAME_GAPS.md#gap-026)).

### 5.7 Classes
- **Runtime:**
  - Base stats at run start match the GameDesignSpec roster (D07).
  - Passives and some stats contradict ClassXpProgressionSystem, which is newer ([SPEC-CONF-008](SPECIFICATION_ISSUES.md#spec-conf-008)).
  - The level schedule differs: Number Freak at L10 starts with HP 15 / Pencil 12; the spec says 14 / 13.
  - Class select shows level, XP "0/50" and the unlock list, but only base stats ("HP:10"), not "HP 10 (+5)" ([GAP-023](GAME_GAPS.md#gap-023)).
  - Locked classes are nearly unreadable ([GAP-029](GAME_GAPS.md#gap-029)).
  - Unlock evaluation works (Garden Monk after 2 bosses, D09); the gold-based unlock counts held gold ([GAP-027](GAME_GAPS.md#gap-027)).

### 5.8 XP
- **Verified:**
  - Curve: 16,860 XP to L40, level boundaries (D08, XpTableTests 27/27).
  - Per-tile formula: build +97 / +79 XP matched hand calculation (E14/E16).
  - Per-class storage and commit at run end, including on defeat (D09).
- **Broken:**
  - Tile entries carry no size, stars or boss flag, so the end-of-run breakdown is wrong and completion tracking is stuck ([GAP-009](GAME_GAPS.md#gap-009)).
  - Reed Pledge bonus is booked to the wrong tile ([GAP-034](GAME_GAPS.md#gap-034)).
- **Not verified:** the game-over XP animation, level-up display and unlock notification (screens not reached).
- **Spec issues:** excess XP above L40 is kept (spec: discarded); the spec's worked example is arithmetically wrong (210 vs 285).

### 5.9 Items and relics
- **Verified:**
  - Reward slot counts by stars (D10; 3 slots at 2★ in E14).
  - Epic gate at L15 (D10).
  - "Nothing" slots shown (E16); full-bag replace dialog (E16).
  - Shop purchase (E15).
  - Item effect contracts 13/14 pass (Ginkgo Leaf fails because the HP penalty lives in the UI, [GAP-035](GAME_GAPS.md#gap-035)).
  - Relic choice offers 3 and adds 1 at service level (D25).
- **Missing:** item-reward reroll ([GAP-015](GAME_GAPS.md#gap-015)).
- **Broken:** an accepted boss relic was not granted in the build ([GAP-021](GAME_GAPS.md#gap-021)).
- **Not opened:** the Items & Relic Codex.
- ItemsAndRelicsSystem v2.0 no longer contains any item or relic effect table, so there are no acceptance criteria ([SPEC-MAC-001](SPECIFICATION_ISSUES.md#spec-mac-001)).

### 5.10 Economy
- **Verified:** gold per puzzle `base × (1 + 0.2 s)` (D11; +63, +42, +91 in the build), floor-scaled shop prices, shop reroll 15 + 5n, emergency heal.
- **Missing:** pencil purchase during a level ([GAP-016](GAME_GAPS.md#gap-016)), and the reward reroll's 20 + 20n cost.
- **Other:** the Harmony gold multiplier is not applied ([GAP-008](GAME_GAPS.md#gap-008)). Gold carried across nodes within the run as expected (83 → 53 after a purchase, E15).

### 5.11 Save system
- **Verified:** three slot files with `.bak`; autosave after each correct placement; run state (HP, gold, items, relics, node, floor, floor modifiers, chosen boss modifiers, seen list) round-trips (D12); profile and class XP persisted (D09); tutorial saves skipped (existing test).
- **Broken:**
  - Mid-puzzle resume is unplayable ([GAP-001](GAME_GAPS.md#gap-001)).
  - Rolled options are re-rolled on resume ([GAP-018](GAME_GAPS.md#gap-018)).
  - Mistakes are not autosaved ([GAP-031](GAME_GAPS.md#gap-031)).
  - Tests write into the real save folder ([GAP-010](GAME_GAPS.md#gap-010)).
- **Not tested:** version compatibility with old saves, and corrupted-file handling beyond the existing durability tests (9/11 pass).

### 5.12 UI / UX
Screens reviewed in the build: main menu, class select, run confirm, garden overview, puzzle, reward, shop, boss gate, boss puzzle, boss defeated, relic choice, Options (main and in-game), Game Modes, Monthly Walk, first-run setup, basics prompt.

| Area | Finding | Gap |
|---|---|---|
| Startup | Fresh install skips profile and first-run setup | GAP-006 |
| Localization | `de.json` mojibake (60 strings); many hard-coded English strings; language not applied live; first-run setup offers only EN/DE of 5 languages | GAP-019 |
| Flow dead-ends | Boss defeated (blank Continue); failed Monthly Walk (no exit); menu unresponsive after the resume crash | GAP-002, GAP-004, GAP-001 |
| Readability | Locked classes, invisible toggle states, tooltip leak, clipped text, inconsistent button sizes, two different Options styles | GAP-029 |
| Input | Mouse and keyboard digit entry work; controller not tested | — |
| Resolution / fullscreen | The player opened full-screen at 1920×1080 from saved options. Layout holds at 1080p, but small text (see §5.13). Other resolutions not tested | — |

Animations (combo label, highlights) were observed. Pressure and boss animations were not.

### 5.13 Visual review
The art direction fits the Japanese-garden theme. Every screen has an illustrated background (torii, cherry blossoms and lanterns on the main menu; a bamboo forest on Floor 1). Menu buttons, legend entries and reward cards carry icons, and cell digits are readable. Weaknesses visible in the screenshots ([E01](evidence/screens/E01_fresh_install_main_menu.jpg), [E05](evidence/screens/E05_garden_floor1.jpg), [E14](evidence/screens/E14_reward_screen.jpg)):
- Secondary text is very small at 1080p: map tile labels, the legend, reward card descriptions, the reward summary.
- Translucent dark panels over busy backgrounds reduce contrast (map lanes, hidden tiles, locked classes).
- Main menu buttons have inconsistent heights.
- Two visual styles for Options.
- Given and player-placed digits look identical.
- Header labels mix "7x7 1*" and "7×7 ★".

This is a judgement against the art direction and the screenshots. The specs give no measurable visual criteria, such as minimum text size or contrast ratios ([SPEC-MAC-006](SPECIFICATION_ISSUES.md#4-missing-acceptance-criteria)).

### 5.14 Audio
Audio could not be judged by ear. All music and SFX files are `.wav` without a RIFF header, two audio tests fail, and the player log contains no audio messages. Volume, mute and music-style controls are present in Options. **Untestable in this review; high risk** ([GAP-030](GAME_GAPS.md#gap-030)).

### 5.15 Performance and technical
- **Runtime exceptions:** the resume NullReferenceException (GAP-001). No other exceptions in four sessions of player logs.
- **Generation stalls:** 24–30 s boss waits; 20 s late-floor waits; 4 s sync timeouts in Endless Zen, Spirit Trials and Monthly Walk.
- **Background pre-baking** of node puzzles is active and logs per node.
- **Architecture:** controller size budgets are exceeded (`RunDirector` 3,911 lines vs a 3,350 budget; `InRunController` 5,205); game rules leak into the UI layer (GAP-035).
- Steamworks init fails outside Steam (expected, logged).
- Frame-time, GC and memory profiling were not performed (PERF-CAP-001..007 pending).

### 5.16 Game modes and meta
- **Monthly Walk:** fails for 5 of 24 months, including the current one ([GAP-004](GAME_GAPS.md#gap-004)).
- **Endless Zen and Spirit Trials:** inert modifiers and generation failures ([GAP-007](GAME_GAPS.md#gap-007)); tier and depth tables are correct (D22/D23).
- **Daily goals:** inside Game Modes rather than on the menu; streak ignores skipped days ([GAP-028](GAME_GAPS.md#gap-028)).
- **Ascension** doesn't exist, and per-class prestige is unreachable ([GAP-017](GAME_GAPS.md#gap-017)).
- **Completion** can reach at most 50 % ([GAP-009](GAME_GAPS.md#gap-009)).
- **Harmony:** only HP/pencil/slot/reroll penalties, shop surcharge, positive-effect odds, T1 starting relic and the victory XP multiplier take effect ([GAP-008](GAME_GAPS.md#gap-008)).
- **Curses (48 requirements):** none verified; no automated tests. Code hooks exist.

---

## 6. Gap analysis (key requirements)

The complete table is in [REQUIREMENT_TRACEABILITY](REQUIREMENT_TRACEABILITY.md). "Test" is the automated-test state; "D/E" marks review-only evidence.

| Req ID | Requirement | Code | Runtime | Test | Status | Severity |
|---|---|---|---|---|---|---|
| REQ-SAVE-100 / SAVE-RESUME-001 | Resume mid-puzzle restores a playable puzzle | Partial | No (NRE) | None | BROKEN | CRITICAL |
| REQ-BOSS-107 / REQ-UI-107 | Continue after non-final boss → relic → next floor | Yes | No | Failing wiring tests | BROKEN | CRITICAL |
| REQ-SUD-101 / REQ-PERF-100 | Generation succeeds within budget | Partial | No (D18/D20/D22–D24) | Static readiness test only | BROKEN | CRITICAL |
| SEASON-PUZZLE-001 | Monthly puzzle every month | Yes | No (5/24) | None | BROKEN | HIGH |
| REQ-BOSS-101 | Chosen boss modifier applied | Yes | Partly (8/40 dropped) | Failing acceptance test | BROKEN | HIGH |
| REQ-UI-100 / INPUT-PROFILE-001 | First launch: profile → setup → basics | Yes | No (E01) | FirstRunSetupPlayModeTests passes (modal in isolation) | BROKEN | HIGH |
| REQ-MODE-101 / TRIAL-TIER-002 | Modes draw functional modifiers | No | No | None | BROKEN | HIGH |
| HARMONY-BOSS-001/002 | Boss modifier scaling under Harmony (exactly one exclusion) | Partial | No (D04) | None (BossService budget test passes, UI not covered) | CONTRADICTED | HIGH |
| REQ-HARM-100/101 | Harmony scaling tables applied | Mostly not | No (D13) | None | MISSING | HIGH |
| REQ-TEST-100 | Test runner executes tests | Bug | No | — | BROKEN | HIGH |
| XP-LOG-001 / META-COMPLETE-001 | Per-tile breakdown; completion | Partial | No | Failing acceptance test | BROKEN | MEDIUM |
| REQ-SUD-103 / GEN-VALID-001 | Definition of a wrong placement | Rule-based | D19/E07 | None | CONTRADICTED | MEDIUM |
| BOSS-FLOOR-001 / REQ-BOSS-100 | Floor and boss counts per table | Different | D04/E17 | Passing (service) | CONTRADICTED | MEDIUM |
| REQ-PATH-101/102, REQ-BOSS-105 | Sizes in floor range; preview = puzzle | Different | E05/E20 | None | CONTRADICTED/BROKEN | MEDIUM |
| PRESSURE-ROLL-003 | Pressure odds per floor | Off-by-one | D16 | None | BROKEN | MEDIUM |
| REQ-ITEM-100 | Reward reroll | No | E14 | None | MISSING | MEDIUM |
| REQ-ECON-100 | Pencil purchase | No | — | None | MISSING | MEDIUM |
| META-ASCEND-001..004 | Ascension | No | — | None | MISSING | MEDIUM |
| REQ-RUN-103 | Determinism across resume | No | D12 | None | BROKEN | MEDIUM |
| REQ-UI-101/102 | Localization quality | Partial | E11 | Catalog test passes (blind to encoding); German traversal fails | BROKEN | MEDIUM |
| REQ-RUN-100/101 | Seed entry, pause menu | No / partial | E03/E06 | None | MISSING / PARTIAL | MEDIUM |
| REQ-ITEM-104 | Accepted relic granted | Yes | No (E19) | Service test passes | BROKEN | MEDIUM |
| REQ-CLASS-100..103 | Class data and display | Different | D07/E03 | Partial | CONTRADICTED/MISSING | MEDIUM |
| REQ-AUDIO-100 | Music and SFX play | Assets invalid? | Not audible | Failing | UNTESTABLE | MEDIUM |
| MAP-CROSS-001, MAP-ROUTE-001/002 | One crossover; visible tiles | Different | D06/E05 | None | CONTRADICTED | LOW |
| REQ-RUN-102 | Same seed → same run | Yes | D01 | None | IMPLEMENTED | — |
| REQ-PATH-103/104 | Graph connectivity; elite before boss | Yes | D06 | None | IMPLEMENTED | — |
| GEN-REGION-001/002, GEN-REMOVE-001/002 | Valid regions; star density | Yes | D02/D02b | Regions: PuzzleGenerationServiceTests; density: none | IMPLEMENTED | — |
| XP-CURVE-001/002, META-XP-001..004 | XP curve; per-class storage; commit | Yes | D08/D09 | XpTableTests | IMPLEMENTED | — |
| ECON-REWARD-001, SHOP-PRICE-001 | Gold and prices | Yes | D11/E14–E16 | None | IMPLEMENTED | — |
| REQ-BOSS-102 | Unseen modifiers show "???" | Yes | E17 | ModifierDiscoveryPersistenceTests | IMPLEMENTED | — |

---

## 7. Bug classification

Every issue has ID, severity, requirement, system, description, expected, actual, reproduction, evidence and suggested fix in [GAME_GAPS](GAME_GAPS.md). Summary:

| ID | Severity | Requirement | System | One-line description |
|---|---|---|---|---|
| GAP-001 | CRITICAL | REQ-SAVE-100 | Save | Resume mid-puzzle → NRE on every input |
| GAP-002 | CRITICAL | REQ-BOSS-107 | Boss/UI | Blank Continue, hidden relic panel after boss |
| GAP-003 | CRITICAL | REQ-SUD-101 | Generation | Failures and stalls; identical-seed retries block forever |
| GAP-004 | HIGH | SEASON-PUZZLE-001 | Monthly Walk | Fails for 5/24 months; stale board; trapped |
| GAP-005 | HIGH | REQ-BOSS-101 | Boss | Chosen modifiers dropped or missing overlay |
| GAP-006 | HIGH | REQ-UI-100 | Startup | Fresh install skips profile and first-run setup |
| GAP-007 | HIGH | REQ-MODE-101 | Endless/Spirit | Inert modifiers; generation failures |
| GAP-008 | HIGH | HARMONY-* | Harmony | Mostly unwired; wrong pick count |
| GAP-010 | HIGH | REQ-TEST-100 | Test infra | Gate doesn't run; 50 failures; real saves touched |
| GAP-009 | MEDIUM | XP-LOG-001 | XP/Meta | Empty tile metadata; completion stuck |
| GAP-011 | MEDIUM | REQ-SUD-103 | Engine | Placement semantics conflict; non-unique puzzles |
| GAP-012 | MEDIUM | BOSS-FLOOR-001 | Boss | Counts and min sizes differ from spec |
| GAP-013 | MEDIUM | REQ-PATH-102 | Path | Sizes out of range; preview mismatch |
| GAP-014 | MEDIUM | PRESSURE-ROLL-003 | Pressure | Odds one floor late |
| GAP-015 | MEDIUM | REQ-ITEM-100 | Items | No reward reroll |
| GAP-016 | MEDIUM | REQ-ECON-100 | Economy | No pencil purchase |
| GAP-017 | MEDIUM | META-ASCEND | Meta | Ascension missing; prestige unreachable |
| GAP-018 | MEDIUM | REQ-RUN-103 | Run | Resume re-rolls choices |
| GAP-019 | MEDIUM | REQ-UI-101 | Localization | Mojibake, untranslated strings |
| GAP-020 | MEDIUM | REQ-RUN-100 | Run | No seed, View Seed or Abandon |
| GAP-021 | MEDIUM | REQ-ITEM-104 | Relics | Accepted relic not granted |
| GAP-023 | MEDIUM | REQ-CLASS-* | Classes | Conflicting class data; no bonus display |
| GAP-030 | MEDIUM | REQ-AUDIO-100 | Audio | Invalid WAV assets; unverified |
| GAP-022 | LOW | MAP-* | Path | Hidden tiles, many crossovers, no marker |
| GAP-024 | LOW | REQ-RUN-104 | Run | Rest values |
| GAP-025 | LOW | REQ-BOSS-103/104 | Boss UI | No intensity label or legend |
| GAP-026 | LOW | REQ-BOSS-108 | Debuffs | No delivery; 4 missing |
| GAP-027 | LOW | REQ-ECON-101 | Unlocks | Held gold counted |
| GAP-028 | LOW | DAILY-GOAL-003/004 | Daily/Monthly | Placement; streak gap |
| GAP-031 | LOW | REQ-SAVE-101 | Save | Mistakes not autosaved |
| GAP-032 | LOW | — | Run | New run overwrites active run silently |
| GAP-033 | LOW | GDS §15 | Balance | Floor-1 6★ rolls |
| GAP-034 | LOW | XP-LOG-001 | XP | Reed Pledge bonus misattributed |
| GAP-035 | LOW | REQ-SUD-103 | Architecture | HP penalty in UI layer |
| GAP-029 | COSMETIC | REQ-UI-103 | UI | Readability and polish |
| GAP-036 | LOW | REQ-ITEM-101 | Shop API | Gold charged when the item can't be stored |

---

## 8. Prioritization

Ranked by gameplay and technical impact, not preference:

- **P0 (game cannot be completed):** GAP-001 (any mid-puzzle save breaks the run), GAP-002 (no normal progression past Floor 1), GAP-003 (deterministic generation failures block nodes or the final boss).
- **P1 (major system broken):** GAP-004 (a headline mode unplayable this month), GAP-005 (the core boss promise fails 1 in 5), GAP-006 (first-time experience and legal notice skipped), GAP-007 (two modes unreliable and meaningless), GAP-008 (the difficulty ladder doesn't scale), GAP-010 (without a working gate, regressions go unseen).
- **P2 (important features incomplete or contradicted):** GAP-009, 011–021, 023, 030.
- **P3 (minor):** GAP-022, 024–028, 031–035.
- **P4 (polish):** GAP-029, 036.

---

## 9. Regression analysis

Fixes in these clusters interact, so change them together and test them together.

**A. Boss modifiers + Sudoku generation + puzzle validation + star difficulty** (GAP-003, 005, 011, 012, 013, 014, 033)
- Changing floor-modifier counts or boss pick counts (spec alignment) raises the number of simultaneous constraints on 9×9 and makes the GAP-003 failures more frequent. Fix generation reliability **before** aligning counts.
- Enforcing uniqueness (if solution-based correctness is chosen) multiplies generation cost, especially at 5–6★ on 9×9, so it needs the GAP-003 performance work and the Monthly/Spirit budget.
- The board-size clamp (boss ≥ 8) partly exists to make structural modifiers generate. Removing it for spec compliance requires pool gating by size.
- Any new RNG draw in `BuildLevelConfig` or the generator changes every seed downstream (shared-sequence RNG) and breaks save regeneration. Combine this with GAP-018's per-decision RNG.
- Pressure fixes (GAP-014) add pressure to Floor 2 elites, which affects difficulty curves and Daily goals.

**B. Class XP + save system + game over + class selection** (GAP-001, 009, 017, 023, 027, 034)
- Adding `BoardSize`/`Stars`/`IsBoss` to `TileXpEntry` changes the end screen and completion keys. Old profiles keep the bogus `"0x0_s0"` key; decide whether to migrate (remove it) on load.
- A class-roster decision changes `ClassCatalog` stats, which affects the run-start state of active saves. Resumed runs keep their stored stats, which is fine, but unlock text changes may confuse.
- Prestige UI resets `TotalXp`. Make sure the end-of-run XP commit and the level-derived unlocks don't double-apply around a prestige.
- Gold-collected tracking (GAP-027) changes unlock speed for Lantern Seer retroactively from the next run.

**C. Garden paths + save system + player position + boss transition** (GAP-001, 002, 018, 021, 022)
- Fixing the boss-cleared flow touches `AdvanceFloorAfterBossCleared` and the relic panel parentage; the resume path currently "works around" it. Test both in-session and resumed.
- Persisting rolled options (GAP-018) changes the save model. Add fields with defaults and keep `RestoreTransientState` migrations.
- Changing cross-link behaviour (spec decision) changes graph generation for every seed, so in-progress saved runs would see a different graph on resume. Version the graph generator, or regenerate only on new floors.
- Restoring the level config on resume (GAP-001) must use the same deterministic config the map previews (GAP-013), otherwise resumed puzzles differ.

**D. Localization + generated UI rebuild** (GAP-019, 006, 029): rebuilding the menu after a language change re-creates buttons. The wiring registry and controller navigation (first-selected) must be re-applied (baseline `GeneratedUiRebuildTests` already fails).

---

## 10. Requirement contradictions (summary)

37 contradictions are documented in [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md#1-contradictions). The most consequential:

- **CONFLICT SPEC-CONF-003:** GEN-VALID-001 (valid if no rule conflict) vs BossMechanicsSystem validation path (`Solution == value`) vs GDS §5.1 (wrong number −1 HP). **Recommendation:** define one rule.
- **CONFLICT SPEC-CONF-001/002:** floor-modifier counts 0,1,2,3,4 (three specs) vs 0,1,1,1,2 (code); boss choices 3/2 (table) vs N from N+1 (GDS) vs 2/1 (code).
- **CONFLICT SPEC-CONF-008:** class stats and passives, GDS v1.5 vs ClassXp v1.2 vs code.
- **CONFLICT SPEC-CONF-006/007:** Endless Zen and Spirit Trials unlock and rules across GDS, mode specs and map.
- **CONFLICT SPEC-CONF-010/030:** one crossover with visible tiles (PathSystem) vs multiple bridges and hidden tiles (code).
- **CONFLICT SPEC-CONF-019:** "15 modifiers" vs "30 modifiers" across five specs.
- **CONFLICT SPEC-CONF-015:** 7★ requires a modifier (GDS, GEN-REMOVE-003) vs no requirement (TutorialModeSystem).

---

## 11. Automated test coverage

| Coverage of the 374 current requirements | Count | Share |
|---|---:|---:|
| Covered by a passing automated test | 40 | 11 % |
| Covered by an automated test that currently fails | 9 | 2 % |
| Verified only by this review at runtime (D## / E##) | 101 | 27 % |
| Checked only by static analysis in this review | 16 | 4 % |
| Not tested at all | 208 | 56 % |

The method is in the [traceability matrix summary](REQUIREMENT_TRACEABILITY.md#summary). "Covered" means an existing test exercises the behaviour, not that it references the requirement ID: only 1 of 436 spec IDs is referenced from any test.

**Covered best:** constraint rules (28 tests), XP table (27), puzzle generation service (16), generated-UI wiring and registry (static), save durability.

**Not covered at all:** curses (48 requirements), pressure (52), debuffs (40), class passives (except Garden Monk), Endless/Spirit/Monthly/Daily, the boss flow beyond one acceptance test, resume of an in-progress puzzle, startup on a clean profile.

**Cannot currently be automated (and why):**

| Area | Why |
|---|---|
| Audio quality | Needs an audio capture/analysis harness |
| Visual and layout quality | Needs screenshot baselines; PlayMode harnesses don't render the full game scene |
| Controller | Needs input-device simulation (Input System test fixtures are not set up) |
| Performance | Needs Profiler capture in CI |
| Real first-launch experience | Needs an injectable persistent-data path (currently hard-wired to `Application.persistentDataPath`; see GAP-010) |

Everything else (all gameplay rules, generation, save/resume, flows through `RunDirector` and the UI controllers) is automatable today with the existing EditMode/PlayMode harnesses. The 26 review diagnostics show how.

---

## 12. Determinism

The game supports deterministic seeds internally: the same seed reproduces path, puzzles, modifiers, rewards and shop for fresh runs (D01). Gaps:
- **Players cannot enter or see a seed** ([GAP-020](GAME_GAPS.md#gap-020)).
- **Save/resume breaks the sequence:** service RNGs restart and options re-roll ([GAP-018](GAME_GAPS.md#gap-018)).
- Shop item IDs use `Guid.NewGuid()` (cosmetic for gameplay, but saves differ between identical runs).
- Generation fallbacks are deterministic, which is why a failing seed fails forever ([GAP-003](GAME_GAPS.md#gap-003)).
- Custom Puzzle and tutorial are intentionally non-deterministic.

---

## 13. Repository changes made by this review

| Path | What |
|---|---|
| `docs/review/*.md` | The five review documents |
| `docs/review/evidence/` | Diagnostic probe logs, baseline test summary, E2E log excerpts, 23 downscaled screenshots |
| `Assets/Scripts/Tests/EditMode/ReviewDiagnosticsTests.cs`, `ReviewDiagnostics2Tests.cs`, `ReviewDiagnostics3Tests.cs` (+ `.meta`) | **Test infrastructure:** temporary diagnostics, `[Explicit]`, category `ReviewDiagnostics`; they back up and restore any save slot they touch. Promote individual cases or delete. |
| `TestResults/review-2026-10-01/` | Raw XML results and Unity logs (gitignored) |

Not modified: any production script, scene, asset, spec document, `tools/`, or `build/`. The player's save folder and PlayerPrefs were restored after E2E testing (see the [E2E report §8](E2E_TEST_REPORT.md#8-cleanup-performed)). Separately requested in the same session: `CLAUDE.md` and project skills under `.claude/skills/`.

---

## 14. How to reproduce

```powershell
# Baseline suites (until GAP-010 is fixed, call Unity directly, without -quit)
& "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath C:\Users\Philipp\Documents\sudoku_rouge_like -runTests -testPlatform EditMode `
  -testResults TestResults\EditMode-results.xml -logFile TestResults\EditMode-unity.log

# Review diagnostics: [Explicit] fixtures run only when selected by name. Replace the
# results/log paths and add, per fixture:
#   -testFilter SudokuRoguelike.Tests.ReviewDiagnosticsTests    (likewise ...Diagnostics2Tests, ...Diagnostics3Tests)
```

Batchmode needs the project closed in the Editor. The diagnostics were last run with `-testFilter` before `[Explicit]` was added. After that change the test assembly was compile-checked (`dotnet build GameTests.EditMode.csproj`, 0 errors) but not re-run, because the Editor had the project open.

E2E: build the player (`-buildWindows64Player`), launch it, and follow the steps in the [E2E report §3](E2E_TEST_REPORT.md#3-built-player-e2e-sessions). Seeds 67968 (sessions 1–2) and 87864 (session 4) reproduce the observed boards. The active puzzle's solution can be read from `save_profile_<slot>.json → ActivePuzzle.Solution` after the first correct placement.

---

## 15. Addendum: fix pass (2026-10-05)

Fixed on request: GAP-001, GAP-002, GAP-003/004/005/007 and GAP-010. Root causes and changes are in the [GAME_GAPS fix log](GAME_GAPS.md#fix-log-2026-10-05); verification is in the [E2E report §9](E2E_TEST_REPORT.md#9-addendum-fix-verification-2026-10-05). Nothing was committed.

| Gap | Verified by | Result |
|---|---|---|
| GAP-001 Resume mid-puzzle | `ResumeMidPuzzleTests` (6); build: 7 resumes in one run (F01, F03) | Board, rules, overlay and counters restored; play and level completion continue |
| GAP-002 After the boss | `BossClearedFlowPlayModeTests`; build F03 | Labelled "Claim Reward", relic choice visible, relic added, Floor 2 |
| GAP-003/005 Generation | `GenerationReliabilityTests`; benchmark; build | 15 boss puzzles with chosen modifiers in 7.8 s; irregular 9×9 p50 2.2 s → 57 ms |
| GAP-004 Monthly Walk | 24/24 months in EditMode; build F02 | 2026-10 generates, saves and resumes |
| GAP-007 Endless / Spirit | `GenerationReliabilityTests` (EditMode only, modes locked on a fresh profile) | Only modifiers with rules are drawn; sessions start with a board |
| GAP-010 Test gate | runner + full suites | EditMode 351: 315 passed, 0 failed, 36 skipped. PlayMode 9/9. 36 spec IDs now referenced from tests (review: 1) |

**Found during the verification:**

- Autosave stopped after the first launch of a session: every later run, resume or Monthly Walk in that session saved nothing, so Save & Quit lost it. Fixed as part of GAP-001 (`RunMapController.BindAutoSave`), with a regression test that fails on the old code.
- New [GAP-037](GAME_GAPS.md#gap-037), not fixed: item and relic codex discoveries made during a run are overwritten by the next autosave; in-run option changes most likely are too.
- [GAP-018](GAME_GAPS.md#gap-018) confirmed in the build: the first reward after every Resume repeats the same items.
- [GAP-021](GAME_GAPS.md#gap-021): on the normal path (no resume) the boss relic is added. The resume path was not re-tested.

**Behaviour changes to be aware of:**

- The authored audio pack now loads (its WAV headers were broken, so the game had used procedural fallback sounds). Nobody has listened to it yet.
- Global structural modifiers (Anti-Knight, Anti-King, Nonconsecutive and similar) always use rectangular regions, including in Custom Puzzle. Irregular regions with these rules never generated. Open decision: [SPEC-AMB-014](SPECIFICATION_ISSUES.md#spec-amb-014).
- Anti-Bishop is no longer offered in generated runs; it cannot be satisfied on its sizes. Open decision: [SPEC-CONF-038](SPECIFICATION_ISSUES.md#spec-conf-038).
- Tests now write saves to a temp folder (`SaveFileService.RootDirectoryOverride`), never to the player's save slots.

**Traceability after the fix pass:** IMPLEMENTED 72 → 96, BROKEN 37 → 14, of 500 requirements ([matrix](REQUIREMENT_TRACEABILITY.md)). Only rows touched by the fixes were re-assessed.

**Still waiting for the owner:** the 38 contradictions in [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md), each with two suggestions (most important: SPEC-CONF-003, whether a digit that breaks no rule but is not the solution counts as wrong); the new items SPEC-CONF-038, SPEC-AMB-014 and SPEC-PROC-007 (the credits say no Steamworks binaries ship, but they are committed).

**Running the tests now:** `tools/run-unity-tests.ps1 -TestPlatform EditMode` (and `PlayMode`) with the project closed in the Editor; `-TestFilter` and `-TestCategory` select tests, and the [Explicit] review diagnostics run only when named. While the Editor is open, mirror the repository and pass `-ProjectPath` to the copy. The note in §14 about calling Unity directly no longer applies.

## 16. Addendum: release 1.1.0 (2026-10-07)

On request, all remaining suggested fixes in [GAME_GAPS](GAME_GAPS.md#fix-log-2026-10-07-release-110) and all *(recommended)* suggestions in [SPECIFICATION_ISSUES](SPECIFICATION_ISSUES.md#0-resolution-status-release-110) were implemented, the specs were updated (version bump and history row each), the repository was cleaned up, and release 1.1.0 was prepared. Nothing was committed, pushed or uploaded.

**Defaults chosen where the request left a choice:**

- Version 1.1.0 (`bundleVersion`).
- Ascension is deferred (FUTURE-009), as SPEC-CONF-026/036 recommend; GAP-017's second part (a prestige button) was built.
- Where a spec recommendation was doc-only but its gap asked for code (SPEC-CONF-021 / GAP-026), the code was changed and the spec documents it.

**Results:**

| Measure | After 2026-10-05 | Release 1.1.0 |
|---|---|---|
| EditMode | 351: 315 passed, 0 failed, 36 skipped | 492: 466 passed, 0 failed, 26 skipped (2026-10-13) |
| PlayMode | 9/9 | 25/25 |
| Matrix: BROKEN | 14 | 0 (CONTRADICTED: 0) |
| Matrix: IMPLEMENTED | 96 of 500 | 228 of 414 (115 SPICE pipeline IDs were archived, SPEC-OBS-001) |
| Spec IDs referenced from tests | 36 | 130 of 350 (plus 49 of the 64 review IDs) |
| Open gaps | 30 | 4 partly open (GAP-017 Ascension, GAP-019 fr/es/pt text, GAP-029 Options restyle, GAP-030 listening check) |

Remaining matrix rows: 3 MISSING (META-ASCEND-001..003, deferred), 9 PARTIALLY_IMPLEMENTED, 19 OUTDATED (archived or deferred IDs), and 155 UNTESTABLE, of which 147 are coverage gaps (code present, no test or E2E check). REQUIREMENT_MAP marks those ◻️ instead of ✅.

**Found while implementing (all fixed):** [GAP-038](GAME_GAPS.md#gap-038) (XP log and analytics lost on resume), [GAP-039](GAME_GAPS.md#gap-039) (pressure visuals lost on refresh), [GAP-040](GAME_GAPS.md#gap-040) (Item Block left the item reusable), [GAP-041](GAME_GAPS.md#gap-041) (Number Freak L3 pencil never applied). The build smoke test ([E2E report §10](E2E_TEST_REPORT.md#10-addendum-release-110-2026-10-07)) found a raw localization key on the profile screen and a see-through View Modifiers panel; both fixed and re-tested.

**Gameplay rule changes players will notice:** wrong digits are judged against the solution and rejected; Undo is gone; Harmony XP applies on defeat too, without the flat bonus; XP above Level 40 is discarded; floors 1–2 stay within one star of their default; boss boards follow the floor's size range; one debuff per boss from Floor 3; one bridge per floor; pencil purchase and reward reroll; Nothing rewards give 5 × floor gold. Player-facing notes: `CHANGELOG.md`.

**Repository clean-up:** SPICE requirement tables archived; `ParkStoryDraft.md` archived; stray `docs/*.md.meta` removed; mockup images moved to `docs/art-source/ui-mockups/`; unused `InRunInputController` (a debug script that placed random digits on Space) removed; `build.app/` ignored; encoding damage repaired in docs, README and the German catalog, with a guard test; controller size budgets lowered after seven partial-class extractions. The heat-system stubs and `Assets/Scripts/Generated/` named in older notes no longer existed. The `tools/_*` scripts were kept: they are a documented, test-pinned tool inventory.

**Owner decisions and actions before upload** (also in `docs/release/platform-release-manifest.md`):

1. **PRG-PLAYER-METADATA (release blocker):** the working tree has company `Kratossos` / product `sudoku_rouge_like`; the build reviewed by Steam (825201c) used `DefaultCompany` / `RunOfTheNine`. These decide the executable name and the save folder, so a change moves existing players' saves. Not changed.
2. Real Steam App ID (`steam_appid.txt` holds 480, Valve's test app) and portal setup: stats, leaderboard, achievements, Auto-Cloud.
3. New store screenshots (the boss gate, digit styles, toggles and path map changed).
4. [SPEC-CONF-039](SPECIFICATION_ISSUES.md#spec-conf-039): prestige reward, cosmetic (shipped) or "passive tier +1".
5. Sign-offs: class balance values (SPEC-CONF-008), legal wording of the Steamworks notices (SPEC-PROC-007).
6. French, Spanish and Portuguese: translate 431 gameplay keys each, or label them partial on the store page.
7. Listening check; the music loops are 6.0 s mono.
