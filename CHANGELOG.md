# Changelog

## Unreleased (after v0.1.5)

### Fixed

- Region borders could show the previous puzzle's regions when two puzzles of the same size followed each other, so an irregular board looked unsolvable. The board now always shows its own regions.

### Changed

- French, Spanish and Brazilian Portuguese: items, relics, boss modifiers, classes, story texts and in-run messages are now translated.

## 0.1.5 — 5th iteration (2026-10-14)

The fifth Early Access build. It fixes the progression blockers players hit in the previous builds, finishes several systems that were only half-wired, and makes the rules match what the game tells you.

### Highlights

- **Resume works.** Save & Quit in the middle of a puzzle, come back later, and keep playing: board, modifiers, pressure and counters are restored.
- **Runs can be finished.** After a non-final boss you can claim your reward and continue to the next floor.
- **Faster, reliable puzzles.** Boards generate in well under a second where they used to take 20–30 seconds or fail.
- **Harmony difficulty does what it says.** Every Harmony level now changes HP cost, gold, boss choices, board sizes and rewards as described.
- **Boss debuffs.** From Floor 3, each Garden Run boss carries one debuff, shown on the boss gate.

### New

- Daily Walk and Monthly Walk widgets on the main menu, with your streak and the time left in the month.
- Enter a seed when starting a run; the in-run menu shows the run seed and has **View Modifiers** and **Abandon Run**.
- Starting a new run while one is saved now warns you and offers **Abandon & Begin**.
- Modifier legend on the board: tap an entry to highlight its overlay for 3 seconds.
- Buy pencil during a puzzle (+3 pencil, 20 gold and 20 more each time; not for Garden Monk).
- Reroll item rewards with a reroll token or gold (20 gold and 20 more each time).
- Prestige a Level 40 class from the Meta Progression screen (with confirmation).
- The path map shows every tile from the start, your position, and exactly one bridge (⇄) between the lanes per floor.
- The class screen shows base stats with your level bonus, for example "HP 10 (+2)".
- First-run setup offers all five languages.
- Display modes: **Windowed** and **Frameless Window** join Fullscreen and Borderless Fullscreen. Windows open centred and sized to fit your screen.

### Changed

- **Wrong digits:** a digit that does not match the solution is now a mistake even when it does not visibly clash. It is rejected and costs HP. Custom 7★ puzzles keep the old rule-based check.
- **Undo removed.** Mistakes count, as in an HP-based run.
- **Anti-Bishop** returns: no digit may repeat on a diagonal within two cells, from 6×6 boards.
- **Boss boards** follow the floor's board-size range, and the map preview always matches the puzzle you get.
- **Early floors** stay readable: puzzles on floors 1–2 are at most one star above the floor's default.
- **Harmony XP:** the Harmony XP multiplier now applies to every run end, including defeats; the old flat bonus is gone.
- **XP above Level 40** is no longer stored.
- Rewards that roll "Nothing" give a little gold (5 × floor), counted as gold collected.
- "Collect 50,000 gold" counts all gold you collect, not the gold you still hold at the end of a run.
- The daily streak resets after a missed day.
- "Allow irregular puzzles" is on by default.
- Digits are easier to tell apart: givens are bold ivory, your digits are blue.
- Pressure mechanics can appear from Floor 2, one floor earlier than before.

### Fixed

- A fresh install now shows profile selection, first-run setup and the Sudoku Basics prompt.
- On a new PC (or after a reinstall) the game opens the profile you played last instead of an empty one, even when your profile is in slot 2 or 3.
- Codex discoveries and option changes made during a run are no longer lost at the next autosave.
- Save & Quit can no longer re-roll boss choices, rewards or relic choices.
- Accepted boss relics are always added.
- Mistakes are saved immediately; quitting after a mistake no longer undoes it.
- The XP breakdown and run statistics keep the tiles played before a resume.
- Pressure countdowns and highlights no longer vanish after tapping a cell.
- Item Block (boss debuff) now really blocks items; before, the item worked and stayed in your bag.
- The shop no longer charges for an item that does not fit in your bag.
- The Reed Pledge bonus is credited to the right tile.
- Number Freak gets its Level 3 "+1 Pencil".
- Boss modifiers you clear now count towards completion.
- German text displays correctly everywhere; more of the interface is translated, including the profile screen in all five languages.
- Monthly Walk generates for every month, and a failed start no longer traps you.
- Option dropdowns show all their entries (the last one used to be cut off).
- Endless Zen and Spirit Trials only use modifiers that work.

### Known issues

- French, Spanish and Brazilian Portuguese: item, relic, boss, class and story texts are still in English.
- The in-game Options panel keeps its older look.
- Music tracks are short loops.

### Saves from earlier builds

Your profiles and runs from earlier builds load, and saves stay where they were (`AppData\LocalLow\Kratossos\sudoku_rouge_like`), so Steam Cloud keeps syncing them. The game window is now titled "Run of the Nine". Profiles from pre-release test builds are copied over automatically on first start. A run saved in an earlier build keeps its old path layout until you reach the next floor. Completion and codex entries that earlier builds failed to record cannot be restored; they fill in as you play.

---

### Technical notes (for the team, not for the store announcement)

- Gap fixes: GAP-001–GAP-005, GAP-007, GAP-010 (fix pass 2026-10-05) and GAP-006, GAP-008, GAP-009, GAP-011–GAP-037 (2026-10-07); new defects found and fixed: GAP-038–GAP-041. Partly open: GAP-017 (Ascension deferred), GAP-019 (fr/es/pt), GAP-029 (Options restyle), GAP-030 (listening check). See `docs/review/GAME_GAPS.md`, both fix logs.
- Specification decisions: every recommendation in `docs/review/SPECIFICATION_ISSUES.md` applied (resolution line per entry); open: SPEC-CONF-039 (prestige reward), owner sign-offs for SPEC-CONF-008 and SPEC-PROC-007.
- Deferred: Ascension, four boss debuffs, multi-cell selection, Accurate Map redirect, legend polish, fr/es/pt gameplay text, in-game Options restyle, achievement list (`docs/future-scope-decision-register.md`, FUTURE-009..016).
- Traceability: `docs/REQUIREMENT_MAP.md` has an Evidence column; ✅ only with a passing tagged test or an E2E check. `docs/review/REQUIREMENT_TRACEABILITY.md` regenerated.
- Tests: EditMode 498 (472 passed, 0 failed, 26 skipped `[Explicit]` review diagnostics); PlayMode 25/25. Build smoke tests: `docs/review/E2E_TEST_REPORT.md` §10 (B01–B19, B23–B27, all pass; B20–B22 belong to a withdrawn build).
- Clean-up: SPICE requirement tables archived (`docs/archive/`), stray `docs/*.md.meta` removed, mockup images moved to `docs/art-source/ui-mockups/`, unused `InRunInputController` removed, `build.app/` ignored, controller size budgets lowered (InRunController 4920, MainMenuBlueprintBuilder 3010, RunDirector 2960).
- `bundleVersion` 0.1.5 (Steam build "v0.1.5 (5th iteration)"; the review documents call this change set release 1.1); Steam App ID 4858470 in `SteamManager` (2026-10-14). Company/product stay Kratossos / sudoku_rouge_like (save folder and Auto-Cloud root of the live 1.0 build, SAVE-FOLDER-001); window title "Run of the Nine" set by `WindowFrameService` (DISPLAY-MODE-007); one-time copy of pre-release saves (SAVE-MIGRATE-001); build as `sudoku_rouge_like.exe`, the live launch option.
