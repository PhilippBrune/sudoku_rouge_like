# Jokers and Chaos Sudoku

**Version:** 0.1 | **Date:** 2026-11-09 | **Status:** new (a proposal for 0.5.0; nothing is built; the owner's answers to "Decisions for the owner" come first)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.1 | 2026-11-09 | new | Owner's request: a Joker the player writes into a cell (Chaos, Master, Confused), fitted into the game as a passive, item or relic; ideas for a more chaotic Sudoku that never breaks the rules. Roadmap RM-238, RM-239. |

## The idea in one paragraph

Today a cell always shows what it holds. This spec splits the two: the board keeps the **truth** (a Sudoku that obeys every rule and has one solution) and the player sees a **face**, which can be right, unknown or a lie. Chaos comes from what the player can trust, never from a broken rule.

## The truth rule (applies to everything below)

| ID | Requirement |
|----|-------------|
| CHAOS-INV-001 | The truth always obeys every active rule and is the puzzle's one solution. Every check (correct digit, mistakes, combo, completion, boss rules) reads the truth, never a face. |
| CHAOS-INV-002 | A chaos effect never asks the player to guess: the puzzle is solvable by logic from what can be trusted. A cell that can lie never counts as a clue in the generator's uniqueness check (SudokuGenerationPipeline). |
| CHAOS-INV-003 | Helpers read the visible board, so they neither leak nor repeat a lie: conflict highlight, same-digit highlight, number pad counts, the full-digit refusal (PLACE-RULE-001), Auto Pencil Cleanup and every candidate item (Chime of Notes, Koi Reflection, Zen Sand Sifter) treat a hidden cell as empty. |
| CHAOS-INV-004 | Every random choice comes from the run seed (REQ-RUN-102), so a Daily Walk is the same for everyone and Save & Quit changes nothing. Faces are saved with the puzzle. |

## Jokers

### What a Joker is

| ID | Requirement |
|----|-------------|
| JOKER-CORE-001 | The player writes a Joker into an empty, non-locked cell (the Master Joker also into a locked one). The cell now holds its true digit (CHAOS-INV-001): it is solved, cannot be overwritten or erased, and is never a mistake. |
| JOKER-CORE-002 | The three kinds differ only in the face (table below). |
| JOKER-CORE-003 | **Settling:** a Chaos or Confused Joker turns to its true digit when its row, column or box has no other open cell, and at the latest when the puzzle ends. It adds no information the player could not work out, and it gives a lie an end. |
| JOKER-CORE-004 | A Joker neither raises nor breaks the combo, and it counts as an item use for grades and codex goals. Under fog it may be written; the cell stays fogged. |

| Kind | The cell shows | Strength |
|------|----------------|----------|
| **Master Joker** (Meister-Joker) | The right digit | Solved and known: the clue helps its neighbours |
| **Confused Joker** (Verwirrter Joker) | `?` | Solved, but the digit stays unknown; no false clue |
| **Chaos Joker** (Chaos-Joker) | Any digit from the seed, the true one included, so the face says nothing | Solved, but the row looks wrong and the player has to remember not to believe it |

### Where it fits: an item (recommended)

| Option | For | Against |
|--------|-----|---------|
| **Item, recommended** | The three kinds are one ladder, and tiered items already are Normal, Rare, Epic (`ItemService.IsTiered`). Choosing a cell, Esc and the refund exist (ITEM-REFUND-001). Rewards, shop, codex and tooltips come for free. | One more item in a long list. |
| Relic | A permanent rule for the whole run. | A relic cannot be a choice per cell; it needs a charge system of its own. |
| Class passive | A class with an identity. | Eight classes are balanced and the Ninth belongs to the story. Only one class would see Jokers. |

So: **one item, Joker, in three rarities**, and later one relic that builds on it (JOKER-RELIC-001).

| ID | Requirement |
|----|-------------|
| JOKER-ITEM-001 | `ItemType.Joker` is tiered (appended at the end of the enum; saves store it as a number): Normal is the Chaos Joker, Rare the Confused Joker, Epic the Master Joker. |
| JOKER-ITEM-002 | One use: pick the item, click a cell. A filled or locked cell keeps the choice open; Esc gives the use back (ITEM-REFUND-001). The pad and the keyboard confirm like the other cell items. |
| JOKER-ITEM-003 | In the reward pools and the shop. Not in Spirit Trials. Allowed in the Daily and Monthly Walk (CHAOS-INV-004). |
| JOKER-BAL-001 | Starting values, for the playtest: see the table. The trade is cells against knowledge: the less a Joker tells, the more cells it solves. The Solver stays the item that also shows the digit and completes units. |
| JOKER-RELIC-001 | Second step, only after a playtest: **The Fox Mask** (Tier 3): a Chaos Joker that settles pays 3 more gold, and each puzzle starts with one Chaos Joker use. |

| Item | Rarity | Uses | Extra |
|------|--------|:----:|-------|
| Chaos Joker | Normal | 4 | +3 gold each time one settles |
| Confused Joker | Rare | 3 | none |
| Master Joker | Epic | 2 | Also writes into a locked cell |

### Theme and look

The garden's trickster is the **kitsune**, the fox spirit: the item is a small fox-mask tile, its art follows the game's icons. A Joker cell has a thin vermilion corner so it stays readable on a handheld. Mockups come before anything is built (project rule).

## A more chaotic Sudoku

Six effects, all under CHAOS-INV-001 to 004. They arrive as a new rule family, so the legend, the rule card, the Custom Puzzle book and the View Modifiers slip need no new screens.

| ID | Rule | What happens | Why the rules stay intact |
|----|------|--------------|---------------------------|
| CHAOS-RULE-001 | **Trickster's Ink** | A few given digits show a wrong digit until they settle. | They are not clues for the generator (CHAOS-INV-002). |
| CHAOS-RULE-002 | **Veiled Givens** | A few givens show `?`. | The same; the player works them out. |
| CHAOS-RULE-003 | **Fox Rain** | Every 8 correct digits a Chaos Joker lands on a random empty cell. | It holds the true digit. Free progress that clouds the board. |
| CHAOS-RULE-004 | **The Garden Turns** | Every 10 correct digits the whole board turns half a turn, with its regions, lines, marks and outside clues. | A turn maps a valid board to a valid board. Reduced motion: no animation, a short highlight. |
| CHAOS-RULE-005 | **Drifting Fog** | Fog moves from one box to another every 6 correct digits. | Fog only hides; digits under it are judged as today. |
| CHAOS-RULE-006 | **Shuffled Ink Pots** | The number pad's digits change places every 6 correct digits. Digit keys stay as they are. | Only the input changes. Off with the accessibility option that stops motion. |

| ID | Requirement |
|----|-------------|
| CHAOS-FAM-001 | The six are `ModifierFamily.Chaos`, with ids appended to `BossModifierId`. They are never rolled as floor rules or boss picks in a normal Garden Run. |
| CHAOS-TILE-001 | **The Fox Tile:** an optional tile on the path like the cursed tile (STAKES-001 shows what it pays): one or two chaos rules, 50 % more gold and XP. |
| CHAOS-MODE-001 | Later, only if the tile is liked: a Fox Walk mode on a shared seed where every puzzle has chaos rules, with its own board. |

## What the code needs

- The level state keeps a list of faces per cell (row, column, kind, shown digit, settled), saved with the puzzle. An old save has none.
- One place decides what a cell shows; `BoardViewController` and the helpers of CHAOS-INV-003 read it instead of `SudokuBoard.Cells`. `ConstraintEngine` keeps reading the truth.
- The Garden Turns needs one mapping for the cells, the region map and every overlay kind.
- Tests: the truth is never changed by a face; a Chaos Joker's face does not change which digits count as correct; no helper reveals a hidden digit; settling; save and resume keep the faces; the same seed gives the same faces; a board with Trickster's Ink is unique without the lying cells.

## Decisions for the owner

1. **Item as recommended,** or a relic or passive after all?
2. **Should a Chaos Joker be marked?** Recommended: a thin corner. Without a mark it is more chaotic, but on a full board even the player forgets which digit lies.
3. **The first slice of chaos rules.** Recommended: Trickster's Ink, Fox Rain and The Garden Turns.
4. **Where chaos appears first.** Recommended: the Fox Tile; the mode only if it is liked.
5. **Does the number pad count hidden digits?** Recommended: no (CHAOS-INV-003).
