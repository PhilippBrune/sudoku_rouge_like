# Changelog

## Unreleased

## 0.2.2 beta: 8th iteration (2026-10-25)

A playtest build for the password beta branch. It makes the end of a run the start of the next one: Walk again,
what the run came close to and what it moved, and the stakes of a choice in numbers. Three Harmony perks now do what
they say, and the D-pad moves through the menus as they look. Five floors per run, as before.

### New

- **Walk again:** every run end (the Garden Run, Endless Zen, Spirit Trials, the Monthly Walk) has Walk again as its first button: the next run starts at once with the same class, mode, Harmony and perk.
- **This run:** a lost Garden Run says how close it came ("So close: the Floor 3 boss, 9 cells from the end") next to your best floor with the class, and lists what the run moved: unlocks brought closer, the Codex, the next Harmony level.
- **Stakes in numbers:** a cursed tile shows the gold and XP it pays played normally and cursed; the boss gate says what the fight pays and what striking a trial costs in XP.
- **The run's length** on the run-end banner. The game keeps its last 200 runs' lengths on your PC (nothing is sent anywhere).
- **Codex:** filter the items by rarity, and see which class an item belongs to ("Best paired with").
- **Game Modes:** Spirit Trials' tiers with their best scores on its slip; a tier goes straight to choosing a class.
- **Licence texts** at the end of the Credits, and in a Licenses folder with every build.

### Fixed

- Scholar's Burden now gives its free Rare Ink Well (two from Harmony VIII) and its +5 pencil, and never adds a bag slot instead of taking one.
- Moonshade Offering's +2 pencil counts, and its price (the first reward's first slot is empty) is paid.
- Empty Canvas's rewards are Rare or better, as the perk says.
- The D-pad on the main menu's screens moves along each screen's rows and columns (the Codex's Down goes one row down), and an arrow key no longer moves twice.
- In a run, the D-pad moves the cursor on the board, the path and the bag, and LT/RT scroll the path and switch bag slots, on Xbox controllers and every controller Steam Input turns into one (Steam Deck, Legion Go). Before, only the stick moved the cursor.

## 0.2.1 — 7th iteration (2026-10-21)

The seventh Early Access build, made from the 0.2.0 playtest. The menus join the ink-and-paper look (Options, the Codex,
the classes, Select Class, Game Modes and the results of every mode), gold and items matter more, every puzzle gets a
grade, and the options do what they say. The main menu keeps its look.

### Highlights

- **Choose your class at a glance.** Select Class shows every class as a seal (level and prestige stars, or what unlocks
  it), the chosen class on a scroll with what the run starts with, and the run's settings on a strip: Harmony and its
  perk (LB/RB, Y), irregular puzzles, the seed and the week's seed. **Begin the run** starts it: no second window.
- **Game Modes as five curtains.** Garden Run, Endless Zen, Spirit Trials, Daily Walk and Monthly Walk hang side by side,
  each with its state (your Harmony, best depth, best score, today's goals, the month's garden and days left); a locked
  mode says what opens it. Harmony set here is the same as on Select Class.
- **Results on a scroll.** Endless Zen, Spirit Trials and the Monthly Walk end like a Garden Run: the record large
  (deepest descent, the tier's score, the month's grade) with your best and a "New best" stamp, the numbers on a tally
  paper.
- **A grade for every puzzle**, **gold that matters** and **counter items** (below).

### Fixed (0.2.0 playtest)

- The reward's tally shows the puzzle's real time (it said 00:00).
- Number keys work on the reward, shop, rest and event screens (they did nothing), and with more than three rewards the plaques shrink to fit instead of running off the screen; the key hint names every plaque.
- Lines, dots and cage marks stay under the digits on every board (from the second puzzle of a run on they covered them).
- Every mistake counts toward the run score when it happens, also in the puzzle that ends the run.
- The trial the Warding Flame removed at a boss gate stays removed after Save & Quit.
- Options do what they say: Highlight Errors, Show Digits Left (was Show Candidate Count), Jump to Next Empty Cell (was Cursor Snap) and Double-Tap to Confirm now act; Auto Pencil Cleanup also clears the box and can be turned off; Screen Shake off stops the shake; Master Volume no longer counts twice (50 % played at 25 %); Board Brightness and Board Contrast redraw the board at once; Text Size scales the board's digits; Colorblind Mode uses colour-blind-safe line colours instead of tinting the cells. "Confirm Wrong Placement" is gone (it would have given the answer away), and "Screen Reader" is now called Status Captions.
- Numbers follow the game's language, not the PC's region: an English game on a German PC shows "XP ×1.4" and
  "50,000" (it showed "1,4" and "50.000").
- Reduce Motion also stops the pulsing path nodes and boss node, the digit pops and the other scale pulses.
- The builds include the licence texts of the font and of Steamworks.NET (`Licenses/`).

### New

- **The garden shows through the path:** the floor's painting stays bright round the path scroll and shows through its thin paper; titles and labels sit on small paper slips. With High Contrast the paper stays opaque.
- **Entering a floor** shows the floor's painting for two seconds with its name on a hanging banner, then the path scroll unrolls (a click or any key skips it).
- **Pencil mode in indigo:** the number pad turns indigo with paper-white digits, and the pencil marks are indigo too.
- **A grade for every puzzle:** Perfect (no mistake) +5 %, Clean (one) ±0, Rough (two or three) −2 %, Messy (four or more, or the puzzle that ends the run) −5 %. The end of a run shows one stamp per puzzle, floor by floor. This replaces the flat −2 % per mistake.
- **Gold at the boss gate:** strike one of the offered trials (the boss then takes one trial fewer) or lower the boss's intensity one step, each once per gate.
- **Gold kept counts:** every 5 gold left at the end of a run adds a point to the run score (up to 15 %).
- **Six counter items**, one for each family of rules: Water Basin (lines), Twin Pebbles (dots and pairs), Rice Bowl (cages and arrows), Moss Stone (marked cells), Compass of Order (whole-board rules) and Sun Medallion (clues outside the grid). Each writes correct digits into the cells its rules use, and shops lean toward the ones that answer the floor's and the boss's rules.

- **Options as a hand scroll** with a bookmark per page (LB/RB turn the page), one setting per line, switches as stamps with a short note on what each does, and Reset this page. Accessibility is now a page of the Options, with the preview board beside it; the run's Options have the same pages plus a Run page.
- **Meta Progression as the garden of classes:** every class on a plaque (locked ones show what unlocks them and how far you are), the chosen class on a scroll with its level, runs, best floor, XP, passive and every level reward ticked off, prestige stars and the prestige button, and your collection's totals.
- **The Codex as a collector's book:** bookmarks for Items, Relics, Rules and the new Curses page, each with its count; found entries in colour, the rest as silhouettes, a filter for found and not yet found, and the chosen entry on the right page with how often you found and used it, where it comes from and what finding them all unlocks.

### Changed

- The main menu's "Items & Relic Codex" is called "Codex", like its screen.
- Class Level 30 now really starts every run with the class's item, and Level 40 gives the class its frame (round its plaque in Meta Progression and its badge in a run; it stays after prestige). Three Level 30 items that did not exist are now Golden Koi (Koi Gambler), Rice Paper Umbrella (Stone Gardener) and Temple Incense (Reed Duelist).
- The class level rewards are shown in your language, and the Koi Gambler's and Lantern Seer's passive steps name the values the game uses (35 %, 35 %/40 %).
- **Input Lock** in Options → Gameplay: the short wait after choosing a cell can be turned off or set from 0.10 to 1.00 s (0.25 s by default).
- Digits are taken 0.25 s after a cell is chosen (was 0.5 s), and a placed digit highlights every cell with that digit.
- After a boss, the numbers show the boss fight and the whole floor side by side.
- The main menu shows the game's version.
- Custom Puzzle: a Random button fills the tray with as many random rules as you choose (1–5).
- Gold matters more: items cost 2.5 times as much (Normal 40, Rare 75, Epic 150 before the floor's markup) and puzzles pay a fifth less gold.
- From Floor 4 a mistake costs at least 2 HP, and the rest heals a quarter of your HP instead of a third.

## 0.2.0 — 6th iteration (2026-10-20)

The sixth Early Access build. Every screen of a run is redrawn in ink and paper, the puzzle rules grow from 30 to 71, and the Custom Puzzle screen offers all of them. It follows 0.1.5; testers of the 0.2.0 beta find the changes since then at the end of this section.

### Highlights

- **The whole run in ink and paper.** The garden path is a hand scroll on every floor; the puzzle hangs as a scroll in front of the floor's painting, with HP and Pencil as paper lanterns and the number pad as wooden tiles. The boss gate is a trial scroll, a beaten boss leaves an offering table, rewards hang as ema plaques, the relic node is a shrine, the shop a market stall, the rest a low table with tea bowls, an event a short hand scroll, a cursed tile a sealed talisman, and the end of a run a scroll with a seal for every floor.
- **41 new puzzle rules, 71 in all.** Lines (Nonconsecutive, High-Low, Nabner, Entropic, Modular, Zipper, Fast and Ambiguous Thermo, Region Sum Line, K-Whispers, 10-Line, Sum Line), dots and pairs (Full White and Full Black Kropki, Difference ≥ 3, Ratio 1:3, Distance Pairs), cages and arrows (Hidden Killer, Product, Difference, Ratio and Renban Cages, Arrow Average, Double Arrow, Pill Arrow), cells (Multiples of 3, Mirror Cells, Opposite Sums, Counting Circles, Distance Cells), the whole board (X-Sudoku, Hyper Sudoku, Disjoint Groups, Extra Region, Magic Square, Inverted Zone) and clues outside the grid (Skyscrapers, Sandwich, X-Sums, Little Killer, Quadruples). They appear in runs, at boss gates, in Endless Zen and Spirit Trials, and count toward the codex and completion.
- **Custom Puzzle as a rule book.** One tab per family (LB/RB switch tabs), every rule as a tile with its icon, up to five rules in the tray under the book. A rule that does not fit the board size or the rules you chose says why.
- **Lines in the colours puzzlers know.** German Whispers green, Dutch Whispers orange, Renban pink, Palindrome grey, thermometers grey with a bulb, Parity blue, drawn as bands through the cells.
- **Seed of the week.** One Garden Run seed per week for everyone, from the start-run screen.

### Fixed

- A chosen modifier could be missing from the board while its rule was listed (for example white dots under Nonconsecutive, Even/Odd next to Prime Cells, Palindrome on busy 9×9 boards). Every chosen modifier now gets its own marks; Nonconsecutive and Difference Kropki no longer appear together.
- Lines and arrows could cross themselves in an X (Whispers on 9×9, Parity Lines on 5×5, Double Arrows), so their order could not be read. No line or arrow crosses itself now.
- A boss puzzle could reuse a board prepared before the boss choice even when Entropy was picked at the gate, so correct digits could show as conflicts.
- A cleared cursed puzzle never added its +10 % to the run score. It does now, and the end screen shows it.
- The run score's breakdown did not add up to the score: the bonuses were multiplied one after another. They are added as the rules say, and every row shows its points.
- Region borders could show the previous puzzle's regions when two puzzles of the same size followed each other.
- Boss puzzles play their stage's boss music.
- Q (or controller A) on the screen after a boss no longer ends the run.
- A killer cage is one dashed outline around all its cells.
- With a full bag, keeping your bag returns you to the rewards instead of losing them.
- Pencil marks in a cage's corner cell no longer hide under the cage sum.

### Changed

- **Prestige** now also gives +1 starting gold per prestige tier for runs with that class (at most +9), next to the badge.
- **Rest:** choose a bowl, then drink (click it again or press its key 1–4 again). Each slip shows the value after it, for example "HP 8 → 11". Cleanse appears only while a curse is on you.
- **Events:** a choice you cannot pay for is greyed with the reason ("You need 25 gold (you have 18)"); after a choice the result is shown and Continue closes the event.
- **Cursed tiles** show the extra rule with its icon and text, the puzzle's size and stars, what you gain, what you risk and the curses already on you. B declines.
- **Rewards, relic node and shop:** choose, then take; one Take button per relic; the shop shows the gold you will have after buying.
- The in-game Options look and work like the main-menu Options, with Accessibility and Run tabs; every control can be reached with a controller.
- French, Spanish and Brazilian Portuguese cover items, relics, boss modifiers, classes, story texts and in-run messages.
- Seeds from earlier versions (also shared seeds and past Seeds of the week) play differently in 0.2.0, because the pool of rules grew and lines are drawn differently. Saved runs keep their boards.
- Completion % counts the new rules, so it drops until you clear them.
- Run scores come out slightly different from 0.1.5 for the same run (bonuses added instead of multiplied). The Steam leaderboard is unchanged: it ranks the run's XP.

### Since the 0.2.0 beta (for testers)

- 28 more rules: cages and arrows, outside clues, extra regions and the B6 set, so all 71 are in.
- The rest, event, cursed-tile and run-end screens and the Custom Puzzle rule book are new.
- Events in the beta opened behind the garden path, so you could not see them; they open on top now.
- Fixes for crossing lines, the Entropy boss board, the cursed bonus and the run score (see Fixed), and Prestige's starting gold.

## 0.2.0 beta — 6th iteration (2026-10-19)

A beta for testers before the next Early Access update: the whole run in a new ink-and-paper look, 13 new puzzle rules, and fixes for rules that silently went missing.

### Highlights

- **A new look for the whole run.** The garden path is an ink hand scroll on every floor; the puzzle hangs as a scroll in front of the floor's painting, with HP and Pencil as paper lanterns and the number pad as wooden tiles. The boss gate is a trial scroll, a beaten boss leaves an offering table, rewards hang as ema plaques, the relic node is a shrine and the shop is a market stall.
- **13 new modifiers.** Nonconsecutive Line, High-Low Line, Nabner Line, Entropic Line, Modular Line, Zipper Line, Fast Thermo, Ambiguous Thermo, Full White Kropki, Full Black Kropki, Difference ≥ 3, Multiples of 3 and Mirror Cells. They appear in runs, at boss gates and in Endless Zen and Spirit Trials, and count toward the codex and completion.
- **Lines in the colours puzzlers know.** German Whispers green, Dutch Whispers orange, Renban pink, Palindrome grey, thermometers grey with a bulb, Parity blue; lines are drawn as bands through the cells, the way puzzle setters draw them.
- **Seed of the week.** One Garden Run seed per week for everyone, from the start-run screen.

### Fixed

- A chosen modifier could be missing from the board while its rule was listed (for example white dots under Nonconsecutive, Even/Odd next to Prime Cells, Palindrome on busy 9×9 boards). Every chosen modifier now gets its own marks; Nonconsecutive and Difference Kropki no longer appear together.
- Region borders could show the previous puzzle's regions when two puzzles of the same size followed each other, so an irregular board looked unsolvable.
- Boss puzzles now play their stage's boss music.
- Q (or controller A) on the screen after a boss no longer ends the run.
- A killer cage is one dashed outline around all its cells (before, it could look like several cages).
- With a full bag, the swap choice now blocks the screen behind it; keeping your bag returns you to the rewards instead of losing them.
- Pencil marks in a cage's corner cell no longer hide under the cage sum.

### Changed

- The in-game Options look and work like the main-menu Options, with Accessibility and Run tabs. Every control can be reached with a controller.
- Rewards: choose a plaque, then take it (click it again, press its key again or use Take). "Take nothing" shows the gold it gives.
- Relic node: one Take button per relic, as after a boss.
- Shop: the slip shows the gold you will have after buying; Buy stays off when you cannot afford it.
- Custom Puzzle tooltips use the same translated rule texts as the game.
- French, Spanish and Brazilian Portuguese: items, relics, boss modifiers, classes, story texts and in-run messages are translated.
- Seeds from earlier versions (also shared seeds and past Seeds of the week) play differently in 0.2.0, because the pool of modifiers grew. Saved runs keep their modifiers.
- Completion % counts the 13 new modifiers, so it drops a little until you clear them.

### Known

- The Custom Puzzle screen still offers the first 30 modifiers; the 13 new ones come to it with a new layout.

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
