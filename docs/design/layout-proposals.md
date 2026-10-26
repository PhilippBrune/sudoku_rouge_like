# Layout proposals: garden path, puzzle screen and boss screens

**Version:** 2.8 | **Date:** 2026-10-26 | **Status:** review (built: every Garden Run screen, Custom Puzzle, the floor entry and the washi window on the path, Pencil mode in indigo, the Options scroll, the Codex book, the garden of classes, Select Class, Game Modes and the results of Endless Zen, Spirit Trials and the Monthly Walk; proposed, not built: the main menu)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.8 | 2026-10-26 | review | The run-end ledger gets a time row for a won run on the seed of the week (SCORE-TIME-001). The Options' Controls page gets a Controller line: Automatic, X-input or D-input (INPUT-PAD-002). Screenshots below. |
| 2.7 | 2026-10-22 | review | Owner: "implement UI-STYLE-017 to 026 if not done". Built what the mockups had and the game left out: the Codex's rarity filter and "Best paired with" (UI-STYLE-018, META-CODEX-010) and the Spirit Trials tiers on the Game Modes slip (UI-STYLE-025). The main menu (UI-STYLE-023) stays unbuilt: the owner kept it (2026-10-21). With the one-more-run slice the run ends gained Walk again and "This run", the cursed tile and the boss gate their stakes in numbers. Screenshots below. |
| 2.6 | 2026-10-21 | review | Owner picks: **Select Class · Who walks the garden** (UI-STYLE-024) and **Game Modes · Five paths** (UI-STYLE-025) built; the main menu stays as it is (UI-STYLE-023 not built), only "Items & Relic Codex" is renamed "Codex"; Select Class has no confirmation window; Harmony is set on both screens. The results of Endless Zen, Spirit Trials and the Monthly Walk are in the run-end family now (UI-STYLE-026, owner request). Screenshots below. |
| 2.5 | 2026-10-21 | review | Owner request: review the main menu, Select Class and Game Modes and make mockups in the same family. One proposal each (proposed UI-STYLE-023 to 025), with today's screens from the game and the review notes; waiting for the owner's pick. |
| 2.4 | 2026-10-21 | review | Owner pick: garden flow **A · Washi window** as well (UI-STYLE-022, with D): built, screenshots below. The garden of classes shows the Level 40 frames (GAP-063 fixed). |
| 2.3 | 2026-10-21 | review | Meta Progression as the garden of classes built (UI-STYLE-019): plaques with level or unlock goal (META-UNLOCK-006), the scroll with runs and best floor (META-STATS-001) and the class's whole level path, the totals. The XP stroke differs from ClassXpProgressionSystem ("no bar"): SPEC-CONF-041, for the owner. Building it found GAP-063 (the Level 30 starting items and Level 40 frames are listed but not granted). Screenshots below. |
| 2.2 | 2026-10-21 | review | The Codex as a collector's book built (UI-STYLE-018), with a new Curses page (the game now records the curses met, META-CODEX-008) and how often each item was found and used (META-CODEX-009). Two parts of the mockup are left out: the Normal · Rare · Epic filter and "Best paired with", which no game data backs. Screenshots below. |
| 2.1 | 2026-10-21 | review | Options as a hand scroll with bookmarks built (UI-STYLE-017) in the main menu and in a run, after the Options audit (GAP-061). Screenshots below. |
| 2.0 | 2026-10-21 | review | Owner picks: Options, Codex and Meta Progression as proposed (UI-STYLE-017 to 019, being built), garden flow **D · Entering a floor** (UI-STYLE-021) and Pencil mode **A · Indigo ink** (UI-STYLE-020). D and A are built, with screenshots from the game below. |
| 1.9 | 2026-10-20 | review | Owner's 0.2.0 playtest: fixes built with screenshots (reward plaques fit and keys work, lines under the digits, the boss screen shows the floor, the version on the main menu, Custom Puzzle Random). New proposals waiting for the owner's pick: Options, Codex and Meta Progression (proposed UI-STYLE-017 to 019), four ways to bring the garden painting back to the path, and Pencil-mode colours for the number pad. |
| 1.8 | 2026-10-20 | implemented | Owner decision: build Custom Puzzle **A · Rule book** (UI-STYLE-012) and the rest, event, cursed-tile and run-end proposals as shown (UI-STYLE-013 to 016); built, with before/after screenshots below. The rule book keeps a third region choice ("Turned", the rotated boxes of 6×6 and 8×8) that the mockup left out, so no setting is lost. Rebuilding the event found GAP-052: the old event panel opened under the full-screen path. The run-end scroll is used for the Garden Run; the Monthly Walk, Spirit Trials and Endless Zen keep their text results. |
| 1.7 | 2026-10-19 | review | Built from the owner's decision for outside clues: UI-STYLE-010 (the grid shrinks to 88 % on a board with outside clues and the clues stand in a ring round it) and UI-STYLE-011 (quadruple circles on inner corners); screenshots below. |
| 1.6 | 2026-10-19 | review | Owner request: mockups for the Custom Puzzle screen with room for every modifier of the catalog plan (three proposals: A rule book, B list with live preview, C shrine wall), and one proposal each for the rest, event, cursed-tile and run-end screens (victory and defeat share a layout). Proposed IDs UI-STYLE-012 (Custom Puzzle) and UI-STYLE-013 to 016 wait for the owner's pick. |
| 1.5 | 2026-10-19 | implemented | Owner decision: build the three proposals for reward, relic node and shop as shown (UI-STYLE-007/008/009). All three are full-screen scenes beside the path panel (the path's buttons never draw over them, Options stays on top), built from the shared scroll parts (`ScrollUiKit.Scenes.cs`); the boss offering table and the relic shrine share one offering piece with a tier tag. Reward: choosing a plaque lifts it and names it on the Take button, choosing it again (click, key 1–3 or A) takes it. Line colours follow the convention puzzle setters use (GAP-048, `BossMechanicsSystem.md` 2.6). |
| 1.4 | 2026-10-19 | review | Garden path **B** (UI-STYLE-001) and puzzle screen **B** (UI-STYLE-002) built in Unity, with screenshots from the game. Puzzle screen: Parity Lines are blue as the spec says (they were teal, like Dutch Whispers; GAP-048), pencil marks move under the cage sum in a cage's corner cell, and the board text renders on whole pixels. |
| 1.3 | 2026-10-18 | review | Owner request: mockups for the next three screens in the same family: the reward after a puzzle (tally scroll and ema plaques), the relic node (the offering table at the shrine) and the shop (goods on the market stall). Proposed IDs UI-STYLE-007/008/009 wait for the owner's pick. Garden path B and puzzle B are being built. |
| 1.2 | 2026-10-15 | review | Owner decisions: boss gate **B · Trial scroll** and boss defeated **B · Offering table**, both built in Unity (UI-STYLE-004, UI-STYLE-005), with screenshots from the game. Owner report: the killer cage in the hanging-scroll mockup was split into one box per cell; a cage is now one outline around all its cells, in the mockups and in the game (UI-STYLE-006). |
| 1.1 | 2026-10-15 | review | Owner decisions: garden path **B · Hand scroll**, the same on all five floors (UI-STYLE-001); puzzle screen **B · Hanging scroll** (UI-STYLE-002); the background paintings keep their corner mark (UI-STYLE-003). Added: the hand scroll on every floor, a 9×9 check of the hanging scroll, and three proposals each for the boss modifier choice (boss gate) and the "Boss Defeated!" screen. |
| 1.0 | 2026-10-15 | new | Owner request: the garden path and the puzzle layout should feel more rounded and closer to the game's theme. Three proposals each, as 1920×1080 mockups made from the game's own art and font. |

The mockups are HTML pages in `layout-proposals/src/`, built from `Assets/Resources` (backgrounds, node, boss, modifier, debuff, item and relic icons, `MainFont.ttf`) with shared example data (`src/common.js`): the five floors, a valid 6×6 and 9×9 puzzle, a Floor 3 boss gate (pick 3 of 4, one modifier not discovered yet, highest intensity, Row Wipe debuff) and the relics offered after that boss. Modifier and relic texts are the game's own. `layout-proposals/render.ps1` renders them with headless Chrome (variants such as each floor come from `src/<name>.variants.txt`). The "before" images come from the game itself (`LayoutScreenshotDiagnostics`, an `[Explicit]` PlayMode test). Boss names other than the avatar file names are working titles; the game does not name its bosses yet.

## Decisions (2026-10-15)

| ID | Requirement | Status |
|---|---|---|
| UI-STYLE-001 | The garden path is an ink hand scroll (emaki) between two wooden rollers on **every floor**, with the same frame, ensō nodes, brush routes (ink = calm, vermilion = risk), seal-stamp boss and paper status tag. Per floor only the content changes: the floor's painting as a faint wash on the paper and blurred behind the scroll, the title "Floor N of five · name", the floor marks, the route and the boss seal. | built 2026-10-19 |
| UI-STYLE-002 | The puzzle screen is a hanging scroll: the board in ink with thick region borders that follow the region map (every size from 5×5 to 9×9 and irregular regions), HP and Pencil as paper lanterns, items and relics as ema plaques, the number pad as wooden tiles, the modifier rules as pinned paper slips, the floor's painting behind. | built 2026-10-19 |
| UI-STYLE-003 | The background paintings keep the bottom-right corner mark (owner decision); new UI does not need to hide it. | decided |
| UI-STYLE-004 | The boss gate is a trial scroll: the gate painting darkened behind a hand scroll with brocade bands and wooden rollers; the eyebrow "Floor N · floor name · The Gate", the title "The <boss>'s trials", "Choose N of M trials", the intensity with one dot per level (REQ-BOSS-103) and the boss's seal; one ink-framed panel per trial with icon, name and rule ("???" and "Not discovered yet" when hidden, HARMONY-FLAG-001), ticked with a vermilion ensō when chosen; along the bottom the boss's curse from Floor 3 (REQ-BOSS-108), "n of N chosen" and "Face the <boss>", which unlocks at N. Up to four trials in a row, five to eight in two rows. | built 2026-10-15 |
| UI-STYLE-005 | After a non-final boss, the offering table: the boss-cleared painting, a paper banner "Floor N of 5 · name" / "The <boss> is defeated", the fight's numbers on a paper tag (mistakes, HP lost, items used, pencil marks), the boss's greyed mask, and the relic offerings on plinths with their own Take button on the same screen, plus "Leave them". Taking or leaving loads the next floor; without offerings one Continue button does. The screen does not use the game-over panel, and it has no Options or Save & Quit (the puzzle shortcuts are off while it is open); the run is saved when the next floor loads. | built 2026-10-15 |
| UI-STYLE-007 | The reward after a puzzle as a tally scroll (result, gold, XP, HP, Pencil, the puzzle's numbers, a "Perfect" stamp) beside the item choices as ema plaques on a red cord, with Take, Reroll and Take nothing. | built 2026-10-19 |
| UI-STYLE-008 | The relic node as the offering table (UI-STYLE-005) at the shrine painting: three relics on plinths with tier, Take each, "Leave them", the run's HP, gold and held relics on paper tags. | built 2026-10-19 |
| UI-STYLE-009 | The shop as the market stall painting with the goods on the counter, wooden price tags, the chosen item on a paper slip with Buy and the gold after buying, the purse (gold, reroll tokens, bag slots), Reroll and Leave. | built 2026-10-19 |
| UI-STYLE-017 | Options as a hand scroll with bookmarks, main menu and run alike: the "Settings · Options" banner, one cloth bookmark per page above the paper (main menu: Audio, Display, Gameplay, Accessibility, Controls, Language; run: Audio, Display, Gameplay, Accessibility, Language, Run), LB/RB (PageUp/PageDown) turn the page; one setting per line with the label on the left, values as a brush stroke with a stone and the value in vermilion, switches as hanko stamps with a short note; a pinned slip on the right with the page's tip (the main menu's Accessibility page shows the preview board there, with Text Size on the page); "Reset this page" and Back under the scroll. The separate Accessibility panel is gone. | built 2026-10-21 |
| UI-STYLE-018 | The Codex as a collector's book: the "Your collection · Codex" banner, a wooden book with two paper pages, bookmarks Items, Relics, Rules and Curses with their counts (LB/RB, PageUp/PageDown); the left page has the filter All · Found · Not yet found (Y) and the tiles, found ones in colour, the rest as ink silhouettes with "???"; the right page shows the chosen entry: icon, rarities (or tier, family, severity), its rule, how often it was found and used, where it comes from and when it was found, the collection's goal (for items the Reed Duelist unlock) and a "found" stamp with the share. Back under the book. | built 2026-10-21 |
| UI-STYLE-019 | Meta Progression as the garden of classes: the eight classes as ema plaques on two red cords (level and prestige stars; locked ones dark, with a silhouette and their unlock goal and progress), the chosen class on a hanging scroll (level, runs and best floor, the XP stroke or the way to its unlock, the passive, every milestone of its level path ticked off up to its level, prestige stars and their gold, the prestige button at Level 40), the collection's totals along the bottom, Back. | built 2026-10-21 |
| UI-STYLE-020 | Pencil mode in indigo ink: the number pad tiles and the switch's Pencil half turn indigo with paper-white digits, and the pencil marks on the board are indigo too. | built 2026-10-21 |
| UI-STYLE-021 | Entering a floor of a Garden Run: two seconds of the floor's painting with its name on a hanging banner and the path rolled up below, then the path scroll unrolls. A click or any key skips it; once per floor, not on a resumed floor; Reduce Motion skips the unroll. | built 2026-10-21 |
| UI-STYLE-022 | Garden flow A · Washi window (with D): on the garden path the floor's painting stays sharp and only lightly dimmed round the scroll and shows through the scroll's thin paper; the floor's title, the first-pick note, the route names, the floor marks, the legend and every node label sit on paper slips so they stay readable. High Contrast keeps the opaque paper and the dark, blurred room. | built 2026-10-21 |
| UI-STYLE-024 | Select Class · Who walks the garden: the class painting dimmed, eight class seals (icon, name, level and prestige stars, the Level 40 frame; a locked class dark with its unlock goal and progress), the chosen class on a hanging scroll (level line, HP, Pencil, Bag, Relics and Gold as "base (+bonus)", passive, the next level rewards, the optional seed and the week's seed), the run's settings on a strip (Harmony stones with the level's rule and the perk for a Garden Run, a line on the mode for Endless Zen and Spirit Trials, irregular puzzles), Back and **Begin the run**; no confirmation window ("Abandon & Begin" and a warning when a saved run would end). | built 2026-10-21 |
| UI-STYLE-025 | Game Modes · Five paths: five noren curtains from a wooden beam (Garden Run, Endless Zen, Spirit Trials, Daily Walk, Monthly Walk) with icon, name, a line on the mode and its state on a strip (Harmony, best depth, best score, goals and streak, the month's garden and days left; a locked mode dark with what opens it); Endless Zen's Records under its curtain; the focused mode's slip below (the Garden Run's Harmony stones, rule, next level and perk; the others' numbers) with Back and the way in. Harmony is the same setting as on Select Class. | built 2026-10-21 |
| UI-STYLE-026 | The results of Endless Zen, Spirit Trials and the Monthly Walk in the run-end family: the mode's painting, a banner with the mode and the outcome, the numbers on a tally paper with a closing line, the record (deepest descent, the tier's score, the month's grade) large on a dark panel with the personal best and a "New best" stamp, notes on a slip (ink stamps, the month's countdown), "Back to the garden gate". | built 2026-10-21 |
| UI-STYLE-006 | A killer cage is one dashed outline around all its cells: inset into the cells, joined across them (no line between two cells of the same cage) and closed at every corner. Same rule in the game (`CageOutline`) and in the mockups (`cageRuns` in `src/common.js`). | built 2026-10-15 |

## Garden path

### Before

![Garden path today](layout-proposals/before_path.jpg)

### B · Hand scroll (chosen, built) — all five floors

![The hand scroll on floors 1 to 5](layout-proposals/path_b_all_floors.jpg)

**As built (UI-STYLE-001, 2026-10-19):** the five floors in the game. Puzzle tiles show the board size with the stars on a second line, so busy floors stay readable; the boss seal sits at the end of the scroll; bridge tiles on the calm route carry their label above. The route content is a pixel-perfect sub-canvas, so the node labels render sharp.

![The hand scroll in the game, floors 1 to 5](layout-proposals/after_path_all_floors.jpg)

One scroll design for every floor. Floor 1 [full size](layout-proposals/path_b_hand_scroll_floor1.jpg) · [2](layout-proposals/path_b_hand_scroll_floor2.jpg) · [3](layout-proposals/path_b_hand_scroll_floor3.jpg) · [4](layout-proposals/path_b_hand_scroll_floor4.jpg) · [5](layout-proposals/path_b_hand_scroll_floor5.jpg).

- Route names moved into the bottom line ("Ink: the Garden Walk, calm · Vermilion: the Wild Crossing, risk, more gold"), so labels never collide on busy floors.
- **In Unity:** a scroll frame (paper, brocade bands, rollers), an ensō ring sprite per node, brush-stroke texture on the route lines, a seal sprite for the boss, a paper tag for the stats; each floor's painting is shown twice (blurred behind, faint grey wash on the paper).

The other proposals stay for reference: [A · Stepping stones](layout-proposals/path_a_stepping_stones.jpg), [C · Zen garden](layout-proposals/path_c_zen_garden.jpg).

## Puzzle screen

### Before

![Puzzle screen today](layout-proposals/before_puzzle.jpg)

### B · Hanging scroll (chosen)

![Hanging scroll, 6×6](layout-proposals/puzzle_b_hanging_scroll.jpg)

**Killer cages (1.2):** the 1.1 mockups drew a dashed box around every cell of a cage, so a two-cell cage looked like two cages. A cage is one outline around all its cells (UI-STYLE-006); the mockups now show an L-shaped three-cell cage. The game had the same weakness in a smaller form (the cage border was drawn cell by cell with gaps at each joint) and draws one outline too now:

![Killer cages in the game: domino, T, L, square and S shapes](layout-proposals/after_cages.jpg)

**9×9 check:** the concern in 1.0 was room for 9×9 at 1080p. It fits with 68 px cells and space to spare:

![Hanging scroll, 9×9 on Shrine Summit](layout-proposals/puzzle_b_hanging_scroll_9x9.jpg)

- **In Unity:** scroll and rod sprites, lantern sprites with a fill mask (HP, Pencil), an ema plaque sprite for bag and relics, wooden tile buttons, paper slip panels; the grid becomes ink lines with thicker region borders (the existing border logic, GEN-REGION-004).

**As built (UI-STYLE-002, 2026-10-19):** 6×6 on Floor 1 (Dutch Whispers and Parity Lines), 7×7 on Floor 2 (German Whispers and Killer Cages), an irregular 7×7 on Floor 3 (Thermo) and 9×9 on Floor 5 (Killer Cages and Kropki dots). The scroll names the floor and hangs in front of its painting. HP and Pencil are paper lanterns that glow from the bottom with the value (blue while a shield is up); Buy Pencil is a small wooden tile under the Pencil lantern. Items hang on wooden plaques and empty slots are dark sockets; relics sit below. The number pad is wooden tiles: a digit fades when it is used up or larger than the board. Solve | Pencil is a two-part switch. The rules are a pinned paper slip, with the Legend tag above it. Save & Quit and Options are round buttons with their key under them. The digits keep at least 4.5:1 contrast on every plain cell state (`PuzzleScrollPaletteTests`). Parity Lines are blue as specified, so they no longer look like Dutch Whispers (GAP-048). Pencil marks in a cage's corner cell sit under the cage sum. The board text renders on whole pixels.

![The hanging scroll in the game: 6×6, 7×7 with cages, irregular 7×7, 9×9](layout-proposals/after_puzzle_all.jpg)

The screenshot test sets each puzzle's modifiers itself, so the floor-modifier line under the board names the run's own floor modifiers, not the ones drawn. Full size: [6×6](layout-proposals/after_puzzle_6x6.jpg) · [7×7 with cages](layout-proposals/after_puzzle_7x7_cages.jpg) · [irregular 7×7](layout-proposals/after_puzzle_7x7_irregular.jpg) · [9×9](layout-proposals/after_puzzle_9x9.jpg).

Reference: [A · Tea table](layout-proposals/puzzle_a_tea_table.jpg), [C · Night focus](layout-proposals/puzzle_c_night_focus.jpg).

## Boss modifiers (boss gate)

### Before

![Boss gate today](layout-proposals/before_boss_gate.jpg)

Today the gate is a see-through overlay on the puzzle screen: the modifier choices are small grey panels with tiny text, and the selection state, intensity and debuff are hard to read.

All three proposals show the same content: floor and boss, pick N of N+1, each modifier with icon and rule (an undiscovered one as "???"), the intensity (REQ-BOSS-103), the floor's boss debuff (REQ-BOSS-108), how many are chosen, and a confirm button that unlocks at N.

### A · Talismans at the gate

![Boss gate A](layout-proposals/gate_a_ofuda.jpg)

The modifiers hang as paper talismans (ofuda) from the gate's beam over the dark gate painting; chosen ones get a vermilion "Chosen" stamp. The boss stands on a dark side panel with its mask, intensity flames, the debuff and what you bring in (HP, Pencil, bag).

- **For:** very readable; the boss feels present; the darker mood marks the gate as a turning point.
- **Against:** a third visual family next to the two scrolls.

### B · Trial scroll (chosen, built)

![Boss gate B](layout-proposals/gate_b_trial_scroll.jpg)

**As built (UI-STYLE-004):** Floor 3 with two trials picked, and Floor 5 with six trials in two rows:

![Boss gate in the game, Floor 3](layout-proposals/after_boss_gate.jpg)

![Boss gate in the game, Floor 5](layout-proposals/after_boss_gate_floor5.jpg)

The same hand scroll as the garden path, in the boss's colours: ink-framed trial panels, chosen ones circled with a vermilion ensō and tick, the boss as a seal, the debuff as a vermilion margin note next to the confirm button.

- **For:** one continuous look from the path into the gate, so it reuses the hand-scroll parts (frame, ensō, seal); calm and readable.
- **Against:** less dramatic than A or C.

### C · A hand of trial cards

![Boss gate C](layout-proposals/gate_c_fan_of_cards.jpg)

The modifiers are cards fanned out over a lacquer tray under the boss's mask; chosen cards rise and get a red "Chosen" band. The undiscovered modifier is a face-down card.

- **For:** the most playful and tactile; a strong "choose your fate" moment, and it works well with a controller (left/right through the hand).
- **Against:** tilted text is harder to read; a 5-card hand (floor 5: 6 options, pick 5) needs a wider fan.

## Boss defeated

Here "Boss defeat screen" is the screen after you beat a boss ("Boss Defeated!", then the relic choice). If you meant the screen when a boss beats you (game over), say so and I'll do that one too.

### Before

![Boss defeated today](layout-proposals/before_boss_defeated.jpg)

Today it is the boss-cleared painting with a small yellow title, four small stat lines and a plain button; the relic choice follows as a separate screen.

### A · Victory scroll

![Boss defeated A](layout-proposals/defeat_a_victory_scroll.jpg)

The hanging scroll of the puzzle screen, unrolled for the win: "The Shrine Oni falls" in large type, the boss mask greyed under a "Defeated" stamp, the four numbers as an ink table, the floor marks and the next floor, and "Choose your relic". The HP and Pencil lanterns stay on the left, bag and relics as ema plaques on the right.

- **For:** the same objects as during the puzzle, so the win reads as the end of that scroll; everything the next choice needs is visible.
- **Against:** the beautiful boss-cleared painting is only behind the scroll.

### B · Offering table (chosen, built)

![Boss defeated B](layout-proposals/defeat_b_offering.jpg)

**As built (UI-STYLE-005):** the relic choice is on this screen; the separate relic screen after a boss is gone. "Leave them" keeps the old option to take no relic.

![Boss defeated in the game](layout-proposals/after_boss_defeated.jpg)

Keeps the boss-cleared painting, adds a paper banner and a paper stat tag, and puts the three relic offerings on wooden stands right here, so you choose your relic without a second screen.

- **For:** one screen and one click less; the reward is the hero.
- **Against:** changes the flow (today: boss-cleared screen, then relic choice); small stats.

### C · Seal on the painting

![Boss defeated C](layout-proposals/defeat_c_seal_on_painting.jpg)

The boss-cleared painting stays full-screen; a large vermilion seal with the boss mask stamps "Defeated", the four numbers sit in round paper medallions, the floor stones show the progress.

- **For:** the least UI; the painting and the stamp moment carry it.
- **Against:** the painting is the same for every boss; less tied to the scrolls.

## More screens (proposals, 2026-10-18)

The next three screens a player sees most, in the same ink-and-paper family. Each keeps today's content and controls (keys 1–3, D-pad, Reroll with a token or gold, Skip/Leave) and only changes the look.

### Reward after a puzzle (UI-STYLE-007)

![Reward after a puzzle](layout-proposals/reward_ema.jpg)

The puzzle's result hangs as a narrow tally scroll on the left (the hanging scroll of the puzzle screen, rolled up); the item choices hang as ema plaques on a red cord over the reward painting. Rarity is a coloured tag (Normal, Rare in indigo, Epic in gold), not a coloured button. The chosen plaque lifts and glows; the main button names the item.

**As built (2026-10-19):** "Nothing" slots are blank plaques; Take nothing names its gold; keys 1–3 choose, the same key again takes.

![Reward in the game](layout-proposals/after_reward.jpg)

### Relic node (UI-STYLE-008)

![Relic node](layout-proposals/relic_shrine.jpg)

The offering table from the boss reward, at the shrine painting, so both relic choices look the same. Tags show the run's HP and gold and the relics already held, so a duplicate is easy to spot.

**As built (2026-10-19):** one Take per relic (as after a boss; the old two-step "pick, then Accept" is gone).

![Relic shrine in the game](layout-proposals/after_relic_shrine.jpg)

### Shop (UI-STYLE-009)

![Shop](layout-proposals/shop_stall.jpg)

The stall painting is the scene: the goods sit on the counter with wooden price tags; the chosen one opens on a paper slip with its rule and "142 → 82 gold" so the cost is clear before buying. The purse shows gold, reroll tokens and the bag's free slots (a full bag leads to the existing swap panel).

**As built (2026-10-19):** Buy is off and the slip says "Not enough gold" when the gold is short.

![Market in the game](layout-proposals/after_shop.jpg)

## Custom Puzzle and the last old-style screens (proposals, 1.6)

The Custom Puzzle panel lists 30 hand-written toggles; the catalog plan ends at 71 player modifiers in six families (lines, dots and pairs, cages and arrows, cells, whole board, outside clues), so the screen needs a layout that grows. All three proposals keep today's settings (board size, difficulty, regions, resource mode, class), the limit of five rules, the rule text on hover or focus, and the start button. Families and icons come from `src/modifiers.js`.

### Custom Puzzle · A · Rule book (UI-STYLE-012, chosen, built)

![Custom Puzzle A](layout-proposals/custom_a_rule_book.jpg)

Built (2026-10-20), before and after:

![Custom Puzzle before](layout-proposals/before_custom_puzzle.jpg)

![Custom Puzzle after](layout-proposals/after_custom_puzzle.jpg)

The settings on a paper slip; the rules in a book with one tab per family (LB/RB switch tabs); the largest family (Lines, 22) fits one spread of six columns. Chosen rules get the vermilion ensō and sit as chips in the tray under the book.

- **For:** one family at a time stays calm; the tray shows the whole choice at once; controller-friendly (tabs plus a grid).
- **Against:** a rule in another family is one tab away; no search.

### Custom Puzzle · B · List with a live preview

![Custom Puzzle B](layout-proposals/custom_b_scroll_list.jpg)

One scrolling list with family headers, a search field and filter chips (family, "New"); the hanging scroll on the right draws a 6×6 sample with the chosen rules' marks and their rule texts.

- **For:** fastest for a player who knows the rule's name; the preview shows how rules look together before the puzzle starts.
- **Against:** a long list is the least "game-like"; the preview needs a small board renderer outside the puzzle screen.

### Custom Puzzle · C · Shrine wall

![Custom Puzzle C](layout-proposals/custom_c_ema_wall.jpg)

Every rule is a small ema plaque on a wooden wall, one shelf per family; the chosen rules hang on the red cord at the top; the settings sit on the offering box below. Long shelves end in "+N ›" and scroll sideways.

- **For:** all six families visible at once; the strongest look.
- **Against:** small plaques make long names tight; sideways scrolling per shelf.

### Rest (UI-STYLE-013, built)

![Rest](layout-proposals/rest_tea_table.jpg)

Built (2026-10-20), before and after:

![Rest before](layout-proposals/before_rest.jpg)

![Rest after](layout-proposals/after_rest.jpg)

Four tea bowls on a low table in the rest painting, each under a paper slip that says what it gives and the value after it ("HP 6 → 9"). Cleanse appears only while a curse is active (CURSE-ACQUIRE-004/005) and names the oldest curse. Keys 1–4 choose, the same key again drinks (as on the reward).

### Event (UI-STYLE-014, built)

![Event](layout-proposals/event_story_scroll.jpg)

Built (2026-10-20). Before: the old event panel opened under the path, so only the path shows (GAP-052). After:

![Event before](layout-proposals/before_event.jpg)

![Event after](layout-proposals/after_event.jpg)

The event as a short hand scroll: title and story left, the choices as paper strips right. Each strip shows its cost and gain; a choice the player cannot pay is greyed with the reason ("You need 25 gold (you have 18)"). HP, gold and pencil sit above the scroll.

### Cursed tile (UI-STYLE-015, built)

![Cursed tile](layout-proposals/cursed_ofuda.jpg)

Built (2026-10-20), before and after:

![Cursed tile before](layout-proposals/before_cursed.jpg)

![Cursed tile after](layout-proposals/after_cursed.jpg)

A sealed talisman in the curse painting carries the extra modifier with its icon and rule, and the puzzle's size and stars. The reward (+50% gold and XP, +10% run score) and the risk (normal penalties) sit on two tags; the curses already on the player on a third. Accept the curse, or Decline to play the same tile as a normal puzzle (MAP-NODE-CURSED-003).

### Run end: victory and defeat (UI-STYLE-016, built)

![Victory](layout-proposals/end_run_scroll.jpg)

![Defeat](layout-proposals/end_run_scroll_defeat.jpg)

One layout for both: the painting, a banner, five floor seals (stamped with each boss's mask up to the floor reached, the lost floor crossed out), the run on a tally scroll with the class's closing line, the class XP as a brush stroke with a level-up stamp, the run score as a short ledger (today's `BuildRunScoreBreakdown`), and new unlocks as ema plaques ("Nothing new this time" with the next goal on a defeat).

Built (2026-10-20) for the Garden Run. Before (victory) and after (victory, then a defeat on Floor 2):

![Run end before](layout-proposals/before_run_end_victory.jpg)

![Victory after](layout-proposals/after_run_end_victory.jpg)

![Defeat after](layout-proposals/after_run_end_defeat.jpg)

## Clue ring and corner clues (UI-STYLE-010, UI-STYLE-011, built 2026-10-19)

Owner decision: a board with outside clues shrinks (about 12 %) so the clues fit round it; other boards keep their size. Skyscrapers and quadruples, and a Little Killer board:

![Skyscrapers and quadruples](layout-proposals/after_catalog_skyscrapers_quadruples.jpg)

![Little Killer](layout-proposals/after_catalog_little_killer.jpg)

## 0.2.0 playtest: fixes and new proposals (1.9, 2026-10-20)

### Built (fixes and small wishes)

**Reward with many plaques (GAP-056):** more than three plaques shrink to fit the cord right of the tally; the key hint names every plaque ("1–5"), the keys 1–9 work on every scene over the path, and the tally shows the puzzle's time (GAP-055). Before (playtest) and after:

![Reward before](layout-proposals/before_reward_five.jpg)

![Reward after](layout-proposals/after_reward_five.jpg)

**Lines under the digits (GAP-058):** from the second board of a run on, lines were drawn over the digits. Before (playtest) and after (a second 8×8 board with Dutch Whispers):

![Lines before](layout-proposals/before_lines_over_digits.jpg)

![Lines after](layout-proposals/after_lines_second_board.jpg)

**Boss fight and floor (REQ-BOSS-107):** the offering table's tag shows the boss fight and the whole floor side by side:

![Boss defeated](layout-proposals/after_boss_defeated.jpg)

**Main menu version (UI-MENU-VERSION-001)** in the bottom-left corner, and the **Custom Puzzle Random** button ("Random: 3", − and + set how many rules) in the tray:

![Main menu](layout-proposals/after_main_menu.jpg)

![Custom Puzzle](layout-proposals/after_custom_puzzle.jpg)

### Proposed: Options, Codex, Meta Progression (proposed UI-STYLE-017 to 019)

Today's screens (from the game):

![Options before](layout-proposals/before_options.jpg)

![Codex before](layout-proposals/before_codex.jpg)

![Meta Progression before](layout-proposals/before_meta_progression.jpg)

**Options as a hand scroll with bookmarks (UI-STYLE-017):** one cloth bookmark per page (Audio, Display, Gameplay, Accessibility, Controls, Language; LB/RB turn the page), volumes as brush strokes with a stone, switches as hanko stamps, a slip on the right that explains or previews the page. The Accessibility page previews the board with the chosen settings.

![Options audio](layout-proposals/options_scroll_audio.jpg)

![Options accessibility](layout-proposals/options_scroll_a11y.jpg)

**As built (UI-STYLE-017, 2026-10-21):** the main menu and the run use the same scroll and controls. Each switch carries a short note on what it does (the Options audit, GAP-061, made every option act as named). The run's Options have a Language page that says where to change it and a Run page (seed, View Modifiers, Save & Quit, Abandon Run). "Pencil marks" from the mockup is not a setting and was left out.

![Options, Audio](layout-proposals/after_options.jpg)

![Options, Accessibility with the preview](layout-proposals/after_options_a11y.jpg)

![Options in a run, Gameplay](layout-proposals/after_options_inrun.jpg)

**Codex as a collector's book (UI-STYLE-018):** in the family of the Custom Puzzle rule book: tabs Items, Relics, Rules, Curses with their counts; found entries in colour, the rest as ink silhouettes; the chosen entry on the right page with its rarities, effect, how often it was found and used, and the "find all" goal with its class unlock.

![Codex](layout-proposals/codex_book.jpg)

**As built (UI-STYLE-018, 2026-10-21):** as in the mockup, with the game's own counts. The Curses page needed the game to remember the curses met (META-CODEX-008: every curse taken, also when cleansed later), and the item page's "Found 6 times · Used 4 times" needed two counters (META-CODEX-009: every pickup from a reward or the shop, every use, counted at the run's end). Items found before this build show "found once". Left out: the Normal · Rare · Epic filter (rarity belongs to an item in a run, not to the codex entry) and "Best paired with" (no game data says which class an item suits). The D-pad goes from the bookmarks to the filter, through the tiles and down to Back. The shots use a profile about half way through its collections.

![Codex, Items with Meditation Stone chosen](layout-proposals/after_codex.jpg)

![Codex, Curses](layout-proposals/after_codex_curses.jpg)

**Meta Progression as the garden of classes (UI-STYLE-019):** the eight classes as ema plaques (locked ones dark, with their unlock goal and progress); the chosen class on a hanging scroll with its level stroke, passive, unlock path and prestige; the collection totals along the bottom.

![Meta Progression](layout-proposals/meta_class_garden.jpg)

**As built (UI-STYLE-019, 2026-10-21):** as in the mockup, with the game's data. The level path lists every milestone of the class (12 levels; the exclusive item at 15 and relic at 30 join their level's row, "???" until reached), not the mockup's five sample rows. "41 runs · best floor 5" needed two counters per class (META-STATS-001, counted from this build on; a count of 0 is left out). The codex total is the share of all four Codex collections; "highest Harmony" is the highest level unlocked. A locked class's scroll shows its unlock goal on the stroke. The XP stroke follows the pick, not ClassXpProgressionSystem's "no bar" line: SPEC-CONF-041 asks the owner to confirm. GAP-063: the Level 30 starting items and Level 40 frames the path lists are not granted by the game.

![The garden of classes, Number Freak chosen](layout-proposals/after_meta_progression.jpg)

![A locked class chosen](layout-proposals/after_meta_progression_locked.jpg)

### The garden's flow on the path: D and A chosen (UI-STYLE-021, UI-STYLE-022)

The old path crossed the bright floor painting; the hand scroll covers it with paper and darkens the rest. Four ways to bring the garden back were proposed (they can be combined, D with any of A–C); the owner picked **D**, and then **A** as well (2026-10-21).

**As built, A (UI-STYLE-022, 2026-10-21):** the painting behind the scroll is the sharp floor painting (it was blurred) under a light shade, and the paper is thin enough to show it; the faint wash printed on the paper is gone, since the painting itself shows through. Everything written on the paper sits on a paper slip: the floor's title, the first-pick note, the route names, the floor marks, the legend and each node's label (a pill the size of its text). With High Contrast on, the path keeps the opaque paper and the dark, blurred room of before.

![Washi window, Floor 1](layout-proposals/after_path_washi_floor1.jpg)

![Washi window, Floor 2](layout-proposals/after_path_washi_floor2.jpg)

![Washi window, Floor 4](layout-proposals/after_path_washi_floor4.jpg)

**As built (2026-10-21):** after the class card the first floor is entered, and every later floor after its boss. The painting stays for two seconds (a click or any key skips it), then it fades while the path scroll opens from its middle. A resumed floor is not entered again.

![Entering Floor 1](layout-proposals/after_floor_entry_floor1.jpg)

![Entering Floor 2](layout-proposals/after_floor_entry_floor2.jpg)

The four proposals:

- **A · Washi window:** the painting stays bright around the scroll and shows through thin paper.
- **B · Garden below:** a shorter scroll at the top; the lower third shows the painting, with stepping stones leading into it.
- **C · Painted scroll:** the floor's painting is printed on the paper in ink and wash; the room around it stays dim.
- **D · Entering a floor:** two seconds of the full painting with the floor's name on a hanging banner, then the scroll unrolls.

![Flow A](layout-proposals/path_flow_a.jpg)

![Flow B](layout-proposals/path_flow_b.jpg)

![Flow C](layout-proposals/path_flow_c.jpg)

![Flow D](layout-proposals/path_flow_d.jpg)

### Pencil mode in colour: A · Indigo ink chosen (UI-STYLE-020)

The number pad changes colour in Pencil mode, and the pencil marks take the same colour. Four candidates were proposed next to today's wooden tiles; the owner picked **A · Indigo ink** (it matches the blue Pencil lantern and reads well against both the wood and the paper).

![Pencil colours](layout-proposals/pencil_numpad_colours.jpg)

**As built (2026-10-21):** before (the wooden tiles stayed the same in both modes) and after. Used-up digits and digits larger than the board fade in indigo too.

![Pencil mode before](layout-proposals/before_pencil_mode.jpg)

![Pencil mode after](layout-proposals/after_pencil_mode.jpg)

## Main menu, Select Class and Game Modes (proposals, 2.5)

The last three menu screens still in the old look: dark panels with button lists. Today's screens, from the game:

![Main menu before](layout-proposals/before_main_menu.jpg)

![Select Class before](layout-proposals/before_class_select.jpg)

![Game Modes before](layout-proposals/before_game_modes.jpg)

**Review notes:**

- **Main menu.** Ten equal bars; Resume is a dimmed bar even without a saved run; the Daily and Monthly Walk widgets are dark boxes with small text; "Items & Relic Codex" is called "Codex" on its own screen; a "Ready." status line sits under the menu. The debug switch only shows in the Editor and development builds.
- **Select Class.** The class texts are 10–12 px; a locked class shows its passive but not what unlocks it (Meta Progression does); the level path is one dense text block; the run's settings (Harmony with its perk, irregular puzzles) sit in small controls under it, and Start Run opens a second confirmation window with the stats the screen could show itself.
- **Game Modes.** A text block explains the modes, and the buttons below say "(Locked)" without saying how to open the mode; the Harmony control is the same as on Select Class; "XP x1,0" shows a decimal comma in the English text (GAP-065: numbers follow the PC's region, not the game's language).

### Main menu · The gate (proposed UI-STYLE-023, not built: the owner keeps the main menu)

![Main menu, the gate](layout-proposals/main_menu_gate.jpg)

The main menu painting stays bright. The title hangs as a lacquer signboard under the torii's beam; the menu is a hanging scroll in the gate, one line per entry with its icon, the chosen one circled by a vermilion ensō. Resume appears only with a saved run, at the top, with where the run stands ("Floor 3 · Number Freak") and a "Saved" seal; the rules group the entries (play, collection, settings). The Daily Walk (today's goals as three marks, the streak, ink stamps) and the Monthly Walk (its garden, days left, best score) hang as paper tags on the right, each with Open. The profile in the top-left corner, the version in the bottom-left; no status line.

### Select Class · Who walks the garden (UI-STYLE-024, built)

![Select Class, who walks the garden](layout-proposals/class_select_seals.jpg)

The classes are round seals over the class painting: icon, name, level and prestige stars, the Level 40 frame round the seal (META-XP-008); a locked class is dark with its unlock goal and progress, as in the garden of classes. The chosen class hangs on a scroll with what the run starts with: HP, Pencil, bag slots, relics and gold, each "base (+bonus)"; its passive; the next level rewards. The run's settings sit on a strip along the bottom: the Harmony stones with the level's rule, the perk, irregular puzzles as a stamp switch, Back and **Begin the run**. The scroll shows what the confirmation window shows today, so Begin starts the run directly.

### Game Modes · Five paths (UI-STYLE-025, built)

![Game Modes, five paths](layout-proposals/game_modes_noren.jpg)

The five modes hang as noren curtains from a wooden beam over the modes painting: icon, name, one line on what it is, and its own state on a paper strip (Garden Run: the Harmony level; Endless Zen: best depth; Daily Walk: goals and streak; Monthly Walk: its garden and days left). A locked mode is dark and says what opens it ("Defeat your first boss"). The chosen mode's settings sit on a paper slip below: for the Garden Run the Harmony stones (reached ones light, the chosen one vermilion), the level's rule and the next level's condition, the perk; then Back and "Choose a class". Endless Zen's slip carries its Records, Spirit Trials' its tiers.

**Owner's answers (2026-10-21):** 1. "Items & Relic Codex" is renamed "Codex" (the only change to the main menu). 2. Select Class has no confirmation window. 3. Harmony stays on both screens (one setting: a change on either shows on the other).

### As built (UI-STYLE-024, UI-STYLE-025)

![Select Class, before](layout-proposals/before_class_select.jpg)
![Select Class, as built](layout-proposals/after_class_select.jpg)
![Select Class for Endless Zen, as built](layout-proposals/after_class_select_endless.jpg)

Select Class as in the mockup. The seed field and the week's seed moved from the confirmation window onto the scroll, under the class; the warning before a saved run is abandoned sits above the strip. For Endless Zen and Spirit Trials the strip shows a line on the mode instead of Harmony.

![Game Modes, before](layout-proposals/before_game_modes.jpg)
![Game Modes, as built](layout-proposals/after_game_modes.jpg)
![Game Modes with the Daily Walk chosen](layout-proposals/after_game_modes_daily.jpg)

Game Modes as in the mockup. A click (or A) on a curtain enters the mode as the old buttons did; focus or the pointer shows its slip. Endless Zen's Records hang as a small pill under its curtain. Spirit Trials' slip lists each tier's best score; the tiers are chosen on the next screen as before.

### The results of the other modes (UI-STYLE-026, built)

![Spirit Trials result, before](layout-proposals/before_result_spirit_trials.jpg)
![Spirit Trials result, as built](layout-proposals/after_result_spirit_trials.jpg)
![Endless Zen result, as built](layout-proposals/after_result_endless_zen.jpg)
![Monthly Walk result, as built](layout-proposals/after_result_monthly_walk.jpg)

Built at the owner's request without a separate mockup, from the run-end scroll's parts: the mode's own painting, the banner (mode and outcome: "Depth 14", "Trial passed", "Grade A"), the numbers on a tally paper with a closing line, the record large on the dark panel with the personal best and a gold "New best" ensō, the Monthly Walk's ink stamps and countdown on a slip, and "Back to the garden gate".

## The parts left out, built (2.7, 2026-10-22)

**Codex (UI-STYLE-018):** the rarity filter on the Items page (Every rarity · Normal · Rare · Epic; an item shows under every rarity it can have) and "Best paired with" (the class whose Level 30 runs start with the item, or that it is exclusive to). Before (2.2) and after, filtered to Rare:

![Codex before](layout-proposals/after_codex.jpg)
![Codex with the rarity filter](layout-proposals/after_codex_rarity.jpg)

**Game Modes (UI-STYLE-025):** Spirit Trials' slip carries its four tiers with their best scores; a tier goes straight to Select Class. Before (2.6, the scores as a line of text) and after:

![Game Modes, Spirit Trials before](layout-proposals/after_game_modes.jpg)
![Game Modes, Spirit Trials with its tiers](layout-proposals/after_game_modes_trials.jpg)

**The one-more-run slice on the run ends** (`docs/design/one-more-run-concept.md` 1.1): Walk again first, "This run" with the near miss and what the run moved, the run's length in the banner; the cursed tile and the boss gate show their stakes in numbers. Before and after:

![Run end before](layout-proposals/after_run_end_defeat.jpg)
![Run end with Walk again and This run](layout-proposals/after_one_more_run_defeat.jpg)
![A won run: what it moved](layout-proposals/after_one_more_run_victory.jpg)
![Spirit Trials result with Walk again](layout-proposals/after_result_spirit_trials_walk_again.jpg)
![Cursed tile before](layout-proposals/after_cursed.jpg)
![Cursed tile with the stakes](layout-proposals/after_cursed_stakes.jpg)
![Boss gate before](layout-proposals/after_boss_gate.jpg)
![Boss gate with the stakes](layout-proposals/after_boss_gate_stakes.jpg)

**Credits:** the licence texts at the end (LEGAL-LICENSE-001, GAP-068):

![Credits with the licence texts](layout-proposals/after_credits_licenses.jpg)

## The seed of the week scores its pace (2.8, 2026-10-26)

A won run on the seed of the week has one more ledger row, "Week's seed in 38 min (+22%)" (SCORE-TIME-001). The same
win before (a normal run) and after (the seed of the week, 38 minutes):

![Run end, a normal run](layout-proposals/before_run_end_weekly.jpg)
![Run end, the seed of the week](layout-proposals/after_run_end_weekly.jpg)

## Controller: X-input or D-input (2.8, 2026-10-26)

Options → Controls gets one more line, "Controller", that cycles Automatic, X-input (Xbox) and D-input (INPUT-PAD-002);
the tip says when to choose D-input. Before and after:

![Controls before](layout-proposals/before_options_controls.jpg)
![Controls with the Controller line](layout-proposals/after_options_controls.jpg)

## Decisions and next step

Boss gate **B** and boss defeated **B** are built (2026-10-15); the garden path **B** (UI-STYLE-001), the puzzle screen **B** (UI-STYLE-002), the reward, the relic node and the shop (UI-STYLE-007/008/009) on 2026-10-19; the Custom Puzzle rule book **A** and the rest, event, cursed-tile and run-end screens (UI-STYLE-012 to 016) on 2026-10-20, all from the same scroll parts (`ScrollUiKit`). Every screen of a Garden Run is now in this style; on 2026-10-21 the Options scroll, the Codex book, the garden of classes, Select Class, Game Modes and the results of Endless Zen, Spirit Trials and the Monthly Walk (UI-STYLE-017 to 026) followed. The main menu stays as it is (owner, 2026-10-21).

## Rendering

```powershell
docs/design/layout-proposals/render.ps1                  # all mockups and variants -> PNG (1920x1080)
docs/design/layout-proposals/render.ps1 -Only "gate_*"
docs/design/layout-proposals/render.ps1 -Only "custom_*"
```

The committed images are JPG copies; the PNGs are not committed.
