# Layout proposals: garden path, puzzle screen and boss screens

**Version:** 1.1 | **Date:** 2026-10-15 | **Status:** review (path and puzzle decided; boss screens waiting for the owner's pick; nothing is built yet)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-15 | review | Owner decisions: garden path **B · Hand scroll**, the same on all five floors (UI-STYLE-001); puzzle screen **B · Hanging scroll** (UI-STYLE-002); the background paintings keep their corner mark (UI-STYLE-003). Added: the hand scroll on every floor, a 9×9 check of the hanging scroll, and three proposals each for the boss modifier choice (boss gate) and the "Boss Defeated!" screen. |
| 1.0 | 2026-10-15 | new | Owner request: the garden path and the puzzle layout should feel more rounded and closer to the game's theme. Three proposals each, as 1920×1080 mockups made from the game's own art and font. |

The mockups are HTML pages in `layout-proposals/src/`, built from `Assets/Resources` (backgrounds, node, boss, modifier, debuff, item and relic icons, `MainFont.ttf`) with shared example data (`src/common.js`): the five floors, a valid 6×6 and 9×9 puzzle, a Floor 3 boss gate (pick 3 of 4, one modifier not discovered yet, highest intensity, Row Wipe debuff) and the relics offered after that boss. Modifier and relic texts are the game's own. `layout-proposals/render.ps1` renders them with headless Chrome (variants such as each floor come from `src/<name>.variants.txt`). The "before" images come from the game itself (`LayoutScreenshotDiagnostics`, an `[Explicit]` PlayMode test). Boss names other than the avatar file names are working titles; the game does not name its bosses yet.

## Decisions (2026-10-15)

| ID | Requirement | Status |
|---|---|---|
| UI-STYLE-001 | The garden path is an ink hand scroll (emaki) between two wooden rollers on **every floor**, with the same frame, ensō nodes, brush routes (ink = calm, vermilion = risk), seal-stamp boss and paper status tag. Per floor only the content changes: the floor's painting as a faint wash on the paper and blurred behind the scroll, the title "Floor N of five · name", the floor marks, the route and the boss seal. | new, to build |
| UI-STYLE-002 | The puzzle screen is a hanging scroll: the board in ink with thick region borders that follow the region map (every size from 5×5 to 9×9 and irregular regions), HP and Pencil as paper lanterns, items and relics as ema plaques, the number pad as wooden tiles, the modifier rules as pinned paper slips, the floor's painting behind. | new, to build |
| UI-STYLE-003 | The background paintings keep the bottom-right corner mark (owner decision); new UI does not need to hide it. | decided |

## Garden path

### Before

![Garden path today](layout-proposals/before_path.jpg)

### B · Hand scroll (chosen) — all five floors

![The hand scroll on floors 1 to 5](layout-proposals/path_b_all_floors.jpg)

One scroll design for every floor. Floor 1 [full size](layout-proposals/path_b_hand_scroll_floor1.jpg) · [2](layout-proposals/path_b_hand_scroll_floor2.jpg) · [3](layout-proposals/path_b_hand_scroll_floor3.jpg) · [4](layout-proposals/path_b_hand_scroll_floor4.jpg) · [5](layout-proposals/path_b_hand_scroll_floor5.jpg).

- Route names moved into the bottom line ("Ink: the Garden Walk, calm · Vermilion: the Wild Crossing, risk, more gold"), so labels never collide on busy floors.
- **In Unity:** a scroll frame (paper, brocade bands, rollers), an ensō ring sprite per node, brush-stroke texture on the route lines, a seal sprite for the boss, a paper tag for the stats; each floor's painting is shown twice (blurred behind, faint grey wash on the paper).

The other proposals stay for reference: [A · Stepping stones](layout-proposals/path_a_stepping_stones.jpg), [C · Zen garden](layout-proposals/path_c_zen_garden.jpg).

## Puzzle screen

### Before

![Puzzle screen today](layout-proposals/before_puzzle.jpg)

### B · Hanging scroll (chosen)

![Hanging scroll, 6×6](layout-proposals/puzzle_b_hanging_scroll.jpg)

**9×9 check:** the concern in 1.0 was room for 9×9 at 1080p. It fits with 68 px cells and space to spare:

![Hanging scroll, 9×9 on Shrine Summit](layout-proposals/puzzle_b_hanging_scroll_9x9.jpg)

- **In Unity:** scroll and rod sprites, lantern sprites with a fill mask (HP, Pencil), an ema plaque sprite for bag and relics, wooden tile buttons, paper slip panels; the grid becomes ink lines with thicker region borders (the existing border logic, GEN-REGION-004).

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

### B · Trial scroll (recommended)

![Boss gate B](layout-proposals/gate_b_trial_scroll.jpg)

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

### A · Victory scroll (recommended)

![Boss defeated A](layout-proposals/defeat_a_victory_scroll.jpg)

The hanging scroll of the puzzle screen, unrolled for the win: "The Shrine Oni falls" in large type, the boss mask greyed under a "Defeated" stamp, the four numbers as an ink table, the floor marks and the next floor, and "Choose your relic". The HP and Pencil lanterns stay on the left, bag and relics as ema plaques on the right.

- **For:** the same objects as during the puzzle, so the win reads as the end of that scroll; everything the next choice needs is visible.
- **Against:** the beautiful boss-cleared painting is only behind the scroll.

### B · Offering table

![Boss defeated B](layout-proposals/defeat_b_offering.jpg)

Keeps the boss-cleared painting, adds a paper banner and a paper stat tag, and puts the three relic offerings on wooden stands right here, so you choose your relic without a second screen.

- **For:** one screen and one click less; the reward is the hero.
- **Against:** changes the flow (today: boss-cleared screen, then relic choice); small stats.

### C · Seal on the painting

![Boss defeated C](layout-proposals/defeat_c_seal_on_painting.jpg)

The boss-cleared painting stays full-screen; a large vermilion seal with the boss mask stamps "Defeated", the four numbers sit in round paper medallions, the floor stones show the progress.

- **For:** the least UI; the painting and the stamp moment carry it.
- **Against:** the painting is the same for every boss; less tied to the scrolls.

## Recommendation and next step

For one look across the in-run screens: **gate B (trial scroll)** continues the garden path's hand scroll, and **defeated A (victory scroll)** continues the puzzle's hanging scroll. Gate A and defeated B are the strongest alternatives (A for drama, B for one screen less). Pick one per screen; then I build the garden path, the puzzle screen and both boss screens in Unity, starting with the shared parts (scroll frame, ensō, seal, lanterns, ema plaques), each with screenshots and tests, and add the boss-screen IDs here.

## Rendering

```powershell
docs/design/layout-proposals/render.ps1                  # all mockups and variants -> PNG (1920x1080)
docs/design/layout-proposals/render.ps1 -Only "gate_*"
```

The committed images are JPG copies; the PNGs are not committed.
