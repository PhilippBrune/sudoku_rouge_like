# Class relics: a set for every class

**Version:** 1.0 | **Date:** 2026-10-25 | **Status:** new (concept for the owner's pick; nothing built)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-25 | new | Owner request: "Create a concept for class-specific relics." Suggested roadmap item RM-124; builds on the keystone answer of 2026-10-22 (a relic may bend Sudoku's rules). |

## 1. What exists

Every class already has **one** relic of its own (CLASS-EXCL-001..003, `RelicService.ClassExclusiveRelics`): Fortune's Ledger, Temple Vow, Endless Archive, Gilded Koi Scale, Load-Bearing Stone, Warding Flame, Dueling Reed, Accurate Map. Each is Tier 3 and enters the pool only from **class Level 30**. Level 30 takes about 10,000 class XP, so most players never see one, and a class plays like the others apart from its passive.

## 2. The proposal

Each class gets a set of **three** relics that follow its passive:

| Relic | Opens at | How it is offered |
|---|---|---|
| **Signature** | Class Level 1 | The first relic offering of a run (the first shrine or the first boss table) always has the class's signature relic as one of its three choices, until the player has taken it once. After that it joins the pool like other relics. |
| **Mastery** | Class Level 15 | In the pool, only for that class, Tier 2. |
| **Exclusive** (exists) | Class Level 30 | As today. |

An offering holds at most one class relic, so the other two choices stay open. The signature relics bend a rule a little; the mastery relics bend it more, in the spirit of the keystones (RM-114).

## 3. The sets (proposals; numbers to tune)

| Class (passive) | Signature (L1) | Mastery (L15) | Exclusive (L30, exists) |
|---|---|---|---|
| **Number Freak** (zero-mistake puzzle: +1 reroll token) | **Counting Beads:** when the last copy of a digit is placed, +1 pencil. | **Abacus of Nine:** the digit pad shows how many of each digit are left, and a finished digit's cells glow for a moment. | Fortune's Ledger |
| **Garden Monk** (every 5 correct digits +1 HP) | **Prayer Beads:** the first mistake of each puzzle costs no HP, if HP are full. | **Still Water:** at 1 HP every correct digit heals 1 HP, once per puzzle. | Temple Vow |
| **Shrine Archivist** (pencil mark: 25 % to reveal the digit) | **Ink Stone:** pencil marks in a cell with only one candidate left turn into the digit when the cell is chosen. | **Catalogue Seal:** at the start of each puzzle, one random 3×3 box (or region) shows all its candidates as pencil marks. | Endless Archive |
| **Koi Gambler** (33 % no HP lost on a mistake; 33 % +3 gold) | **Lucky Scale:** a puzzle finished with exactly 1 mistake pays double gold. | **Koi's Wager:** before a puzzle, bet 20 gold: a perfect solve returns 60, a mistake loses it. | Gilded Koi Scale |
| **Stone Gardener** (first item each puzzle not used up) | **Moss Pouch:** +1 bag slot. | **Mason's Rule:** an item used on a cell also checks the cells next to it. | Load-Bearing Stone |
| **Lantern Seer** (boss rules 30 % weaker, preview and a free reroll) | **Second Wick:** cursed tiles show their extra rule before the player chooses them on the path. | **Clear Glass:** one boss trial per gate is replaced by "no rule" (the gate shows which). | Warding Flame |
| **Reed Duelist** (perfect puzzle without pencil: +2 pencil) | **Bamboo Edge:** a puzzle solved with no pencil marks pays +25 % XP. | **Single Stroke:** while no pencil mark is on the board, the first wrong digit of a puzzle is undone instead of counted. | Dueling Reed |
| **Quiet Cartographer** (preview the next node after a perfect tile) | **Compass Needle:** the path shows the star count of every node on the floor. | **Folded Map:** once per floor, swap the board size of the next puzzle for one size smaller. | Accurate Map |

## 4. How a player sees them

- **Select Class:** the class scroll lists the class's three relics, the locked ones as silhouettes with their level.
- **Codex:** the Relics page gets a "Class" filter, and each class relic names its class ("Best paired with" exists for items).
- **The garden of classes:** the level path shows Level 15 and Level 30 relics (Level 30 is listed today).
- **Offer:** a class relic carries the class's seal on its plinth, so it is clear why it is there.

## 5. What it touches

- 16 new `RelicId` values, their effects in `RelicService` and the puzzle code, tier and class in `ClassExclusiveRelics` (the table gets an unlock level per relic).
- The offering rule (signature guaranteed in the first offering until taken once).
- Icons: 16 new relic icons in the style of the existing ones (`Assets/Resources/RelicIcons`); provenance register.
- Five catalogs: name and rule for each relic.
- Balance check at a playtest; achievements for "all three relics of a class in one run".

Effort: M to L (a week, most of it the 16 effects and their tests). Requirement IDs follow in ItemsAndRelicsSystem when the owner picks.

## 6. Questions for the owner

1. Three relics per class (signature, mastery, exclusive), or two (signature and the existing exclusive)?
2. The signature relic guaranteed in the first offering until taken once, or only a higher chance?
3. Which effects go too far? (Single Stroke and Ink Stone bend Sudoku's rules the most.)
4. Together with the keystones (RM-114): class relics first, keystones first, or both in one content update?
