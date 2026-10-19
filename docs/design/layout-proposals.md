# Layout proposals: garden path, puzzle screen and boss screens

**Version:** 1.6 | **Date:** 2026-10-19 | **Status:** review (built: garden path, puzzle screen, boss gate, offering table, reward, relic node, shop; proposed: Custom Puzzle, rest, event, cursed tile, run end)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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

### Custom Puzzle · A · Rule book (proposed UI-STYLE-012)

![Custom Puzzle A](layout-proposals/custom_a_rule_book.jpg)

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

### Rest (proposed UI-STYLE-013)

![Rest](layout-proposals/rest_tea_table.jpg)

Four tea bowls on a low table in the rest painting, each under a paper slip that says what it gives and the value after it ("HP 6 → 9"). Cleanse appears only while a curse is active (CURSE-ACQUIRE-004/005) and names the oldest curse. Keys 1–4 choose, the same key again drinks (as on the reward).

### Event (proposed UI-STYLE-014)

![Event](layout-proposals/event_story_scroll.jpg)

The event as a short hand scroll: title and story left, the choices as paper strips right. Each strip shows its cost and gain; a choice the player cannot pay is greyed with the reason ("You need 25 gold (you have 18)"). HP, gold and pencil sit above the scroll.

### Cursed tile (proposed UI-STYLE-015)

![Cursed tile](layout-proposals/cursed_ofuda.jpg)

A sealed talisman in the curse painting carries the extra modifier with its icon and rule, and the puzzle's size and stars. The reward (+50% gold and XP, +10% run score) and the risk (normal penalties) sit on two tags; the curses already on the player on a third. Accept the curse, or Decline to play the same tile as a normal puzzle (MAP-NODE-CURSED-003).

### Run end: victory and defeat (proposed UI-STYLE-016)

![Victory](layout-proposals/end_run_scroll.jpg)

![Defeat](layout-proposals/end_run_scroll_defeat.jpg)

One layout for both: the painting, a banner, five floor seals (stamped with each boss's mask up to the floor reached, the lost floor crossed out), the run on a tally scroll with the class's closing line, the class XP as a brush stroke with a level-up stamp, the run score as a short ledger (today's `BuildRunScoreBreakdown`), and new unlocks as ema plaques ("Nothing new this time" with the next goal on a defeat).

## Decisions and next step

Boss gate **B** and boss defeated **B** are built (2026-10-15); the garden path **B** (UI-STYLE-001), the puzzle screen **B** (UI-STYLE-002), the reward, the relic node and the shop (UI-STYLE-007/008/009) on 2026-10-19, all from the same scroll parts (`ScrollUiKit`). Still in the old style: rest, events, cursed node, defeat and victory screens, and the Custom Puzzle panel; their proposals (1.6) wait for the owner's pick.

## Rendering

```powershell
docs/design/layout-proposals/render.ps1                  # all mockups and variants -> PNG (1920x1080)
docs/design/layout-proposals/render.ps1 -Only "gate_*"
docs/design/layout-proposals/render.ps1 -Only "custom_*"
```

The committed images are JPG copies; the PNGs are not committed.
