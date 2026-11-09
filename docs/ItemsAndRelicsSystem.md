# Items & Relics System

**Version:** 2.13  
**Date:** 2026-11-09  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.13 | 2026-11-09 | implemented | Owner's picks on `docs/design/playtest-043b-proposals.html`: four items and two relics get an effect of their own (ITEM-REWORK-002, RELIC-REWORK-001), the Unbroken Thread is new (RELIC-THREAD-001). |
| 2.12 | 2026-11-08 | implemented | Owner's 0.4.3 playtest: the Cracked Teacup keeps the combo (RELIC-TEACUP-001); the five-relic row (HUD-RELIC-001). |
| 2.11 | 2026-11-08 | implemented | Owner's pick B after the 0.4.2 playtest (RM-231): the Pattern Scroll on a paper slip with a row per piece; the chosen piece's cells are gold on the board (ITEM-SCROLL-002). |
| 2.10 | 2026-11-07 | implemented | Owner's picks after the 0.4.2 playtests (2026-11-07): the item review (ITEM-REWORK-001); the class items worked only in code no button reached (GAP-125), the Solver never completed a box (GAP-126), the Reed Duelist could not be unlocked (GAP-123). |
| 2.9 | 2026-11-07 | implemented | Owner's picks after the 0.4.1 playtest on a Legion Go (2026-11-05) (F3): the fog lanterns are no reward any more; the market sells them where there is fog (ITEM-FOG-001). |
| 2.8 | 2026-11-04 | implemented | RM-104 (owner's ticks 2026-11-04, list `docs/design/rm104-content-proposals.html` and the trade round): 7 relics (RELIC-PACK-001, RELIC-TRADE-001), 4 items (ITEM-PACK-001, ITEM-TRADE-001) and 7 events (EVENT-PACK-001, EVENT-TRADE-001). The trades pay with HP, Max HP or pencil marks for a strong effect (the owner's wish). |
| 2.7 | 2026-11-03 | implemented | Owner (2026-11-03), on the proposals of OPEN_POINTS 2.0: the Pattern Scroll lists every drawn piece by its rule's name in the player's language, cell markers, cell pairs, outside clues, quadruples and extra regions included (ITEM-SCROLL-001). |
| 2.6 | 2026-11-03 | implemented | Spirit Trials start with one item in one slot (TRIAL-SESSION-001, owner 2026-11-03, GAP-113); Endless Zen keeps `ItemSlots = 0`. |
| 2.5 | 2026-10-27 | implemented | Owner request (2026-10-27): the concept files are merged into their specs. The class relic concept (v1.1) is in "Class relics: why and the owner's answers": the reason, the per-class table, the owner's answers and what is still open. The item part of the gold and items concept (why the counter items) is in "Counter Items". Its lever L3 is named in GoldEconomySystem 2.3. No rule changed. |
| 2.4 | 2026-10-26 | implemented | Owner picks (2026-10-25): a signature relic per class from Level 1 with a higher chance in offerings (RELIC-SIG-001/002, Single Stroke for the Reed Duelist), and four keystones offered after a Garden Run's first boss that bend a rule of the puzzle (RELIC-KEY-001/002, RM-114, the owner's answer: a relic may bend Sudoku's rules). |
| 2.3 | 2026-10-21 | implemented | Counter items, one per modifier family (ITEM-COUNTER-001..006), and shops that lean toward the counters of the board's rules (ITEM-COUNTER-007); lever L3 of GoldEconomySystem 2.3. |
| 2.2 | 2026-10-21 | implemented | Item base prices ×2.5: Normal 40, Rare 75, Epic 150 (ECON-BAL-001, lever L1 of GoldEconomySystem 2.3, owner pick after the 0.2.0 playtest). The Warding Flame's removed trial is kept in the save (`RunState.WardingFlameOnFloor`/`WardingFlameTrial`): it used to come back after Save & Quit at the gate. |
| 2.1 | 2026-10-07 | implemented | Item and relic reference tables regenerated from code (SPEC-MAC-001); standalone items documented as rolled (SPEC-CONF-035); multi-relic without cap, RELIC-SLOT-001 deprecated (SPEC-CONF-004); shop offer counts 2/2/3/3/4 (SPEC-CONF-005); reward reroll and Nothing gold (GAP-015, SPEC-AMB-007); full-bag purchase rule (GAP-036). |

## Overview

Garden Run uses two run-scoped inventory layers:

- **Items**: active consumables stored in `HeldItems`
- **Relics**: passive run effects stored in `HeldRelics`

Primary implementation references:

- `Assets/Scripts/Items/ItemService.cs`
- `Assets/Scripts/Economy/RelicService.cs`
- `Assets/Scripts/Economy/ShopService.cs`
- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/UI/HudViewController.cs`

## Items

### Inventory Model

- Item capacity is controlled by `RunState.ItemSlots`
- Items are stored in `RunState.HeldItems`
- Items are consumed through in-run item flow in `InRunController`

### Reward Slots

Reward slot rolling still follows star-based item rewards through `ItemService.RollSlots(...)` (slot counts and Nothing chances: GameDesignSpec §8; at least one slot always holds an item), with additional effects from:

- relic bonus slots
- positive floor effects
- curses
- harmony perks that modify reward behavior

Since 1.1 the reward screen has a **Reroll** button (a Reroll Token, otherwise `20 + 20n` gold; Nothing slots stay) and taking Nothing or Skip grants `5 × floor number` gold (GAP-015, SPEC-AMB-007).

### Shops

Shops currently sell **items only**.

`ShopService.BuildOffers(...)` builds item offers using:

- floor-based slot counts
- rarity roll by floor/class level
- item price scaling
- harmony and relic price modifiers

Offers per shop: 2 on Floors 1–2, 3 on Floors 3–4, 4 on Floor 5 (`ShopService.GetShopSlotCount`; ECON-SHOP-001 updated, SPEC-CONF-005). Buying with a full bag requires choosing an item to replace; without a slot nothing is bought or charged (GAP-036).

**ITEM-COUNTER-007 (2.3):** the shop leans toward the counter items of the board's rules: the floor's modifiers and, once
chosen, the boss's trials. Each slot offers one of those counters with a chance of 50 % (`ShopService.CounterItemChance`),
each counter at most once per shop, at its fixed rarity and that rarity's price. Floor 1 has no floor modifiers, so its
shops roll as before.

### Item review (ITEM-REWORK-001, 0.4.3)

| ID | Requirement |
|---|---|
| ITEM-REWORK-001 | **Golden Kintsugi Jar:** the next 3 mistakes of the puzzle write the right digit instead (no HP lost, the combo stays). **Bamboo Compass:** out of the game and the Codex (kept in the enum for old saves; `ItemService.IsRetired`). **Loaded Coin:** works from the bag (the next reward fills every Nothing slot, then it is spent); using it does nothing else. **Monk's Beads:** the next 5 correct digits heal 1 HP each. **Dim Lantern:** shows the floor's boss rules on the map. **Reed Pledge:** no pencil mark in the puzzle: +4 pencil, +30 gold, +25 XP; each mark costs 1 HP and breaks it. **Double or Quits:** at the puzzle's end 50 % its gold again, else -1 HP (never below 1), on its own line in the reward. **Annotated Folio:** notes in every cell with exactly two possible digits. **Worn Chisel:** 30 % off in every shop of the floor. **Survey Notes:** the next puzzle is a whole star easier. **Bamboo Scroll:** every tile of the floor shows its size and stars. **Koi Dragon Scale:** the two most-filled rows, columns or boxes (since ITEM-REWORK-002 the River Bell). **Pattern Scroll:** rolled and sold only on floors with rules. **Garden Rake:** removes every note the board's digits rule out, +1 pencil per 3. **Zen Sand Sifter:** writes the two possible digits into up to 4 cells that have exactly two. **Finder:** Normal 2 cells, Rare 4. **Solver:** by the cell's box as drawn (`RegionMap`): Normal the cell and 2 more of its box, Rare the whole box, Epic its box, row and column. The Reed Duelist's Codex goal counts no class items and no retired item (U1). |

### Fog lanterns (ITEM-FOG-001, 0.4.3)

| ID | Requirement |
|---|---|
| ITEM-FOG-001 | The Lantern of Clarity and the Moth Light are never rolled as rewards (on a board without fog they did nothing). The market offers them like the counter items, each at `CounterItemChance`, when the floor's or the boss's rules include Fog of War or an Inkblot or Fog of Memory curse is active (`CounterItems.FogAhead`, `FogItems`). |

### Counter Items (2.3, ITEM-COUNTER-001..006)

Why: in the 0.2.0 playtest most items helped a little with any puzzle ("restore 3 HP", "+4 pencil"), and few answered
the rule that was hard on this board. With two bag slots and no pressure, keeping items "for later" was the safe play.
A counter item answers one modifier family directly, and the shop offers the counters of the floor's rules
(ITEM-COUNTER-007). The levers and the playtest numbers are in GoldEconomySystem.

One item per modifier family answers the rules that are hard on this board (lever L3 in GoldEconomySystem). Each writes the solution's digits into cells its family's rules use, starting at the
selected cell when it belongs to them, else at the group closest to done. The digits are placed like a hand placement
(combo, pressure and fog react); an item that finds nothing to fill on the board is not used up. They are sold in shops
only (ITEM-COUNTER-007); rewards keep their pools. Filled cells count as hand placements (combo, relic counters);
a cell under Cell Lock is skipped. Because only shops sell them, and only for the floor's rules, the counter items do not
count toward the "every item discovered" codex unlock (`ProfileService`).

| ID | Item | Family | Rarity | Effect | Icon |
|---|---|---|---|---|---|
| ITEM-COUNTER-001 | Water Basin | Lines | Rare | Up to 3 empty cells of one line | `items/icon_water_basin` |
| ITEM-COUNTER-002 | Twin Pebbles | Dots and pairs | Normal | Both cells of one dot or pair mark (a half-done one first) | `items/icon_pebble` |
| ITEM-COUNTER-003 | Rice Bowl | Cages and arrows | Rare | Up to 3 empty cells of one cage or arrow (circle and path) | `items/icon_rice_bowl` |
| ITEM-COUNTER-004 | Moss Stone | Cells | Normal | 2 marked, paired or fogged cells | `items/icon_moss_stone` |
| ITEM-COUNTER-005 | Compass of Order | Whole board | Rare | The 2 empty cells with the fewest possible digits (row, column, region) on the diagonals, windows, extra regions and the inverted zone; under a whole-board rule without houses (Anti-Knight, Nonconsecutive ...) anywhere; nothing on a board without a whole-board rule | `items/icon_compass_of_order` |
| ITEM-COUNTER-006 | Sun Medallion | Outside clues | Rare | The first 2 empty cells one outside clue reads, or one quadruple's four cells | `items/icon_sun_medallion` |

### No-Progression Modes

Endless Zen sets `ItemSlots = 0`; a Spirit Trial has one slot with its starting item (TRIAL-SESSION-001,
[SpiritTrialsMode](SpiritTrialsMode.md)). Both skip progression rewards, so the item reward/shop loop does not apply there.

## Relics

### Inventory Model

Runtime relic storage is **multi-relic**, not single-slot, with no cap (decided 2026-10-07, SPEC-CONF-004; RELIC-SLOT-001 deprecated, RELIC-SLOT-002's "max 1" note removed).

Current state shape:

- primary runtime list: `RunState.HeldRelics`
- legacy compatibility fields: `HasRelic` and `HeldRelic`

The legacy fields are still maintained for backward compatibility, but current gameplay logic reads the list.

### Acquisition Paths

Relics can currently come from:

- starting relic assignment
- relic choice rewards
- relic nodes / events

Boss-adjacent relic rewards use rolled choice panels rather than automatic single-relic grants.

### Class relics: why and the owner's answers (2.5)

Why: each class had one exclusive relic, which enters the pool only from class Level 30 (CLASS-EXCL-001..003). Level 30
takes about 10,000 class XP, so most players never saw one, and a class played like the others apart from its passive.
A signature relic from Level 1 gives each class a relic that bends its own rule a little. The keystones (RM-114) bend
the rules further.

| Class | Passive | Signature (Level 1, RELIC-SIG-001) | Exclusive (Level 30, CLASS-EXCL) |
|---|---|---|---|
| Number Freak | zero-mistake puzzle: +1 reroll token | Counting Beads | Fortune's Ledger |
| Garden Monk | every 5 correct digits +1 HP | Prayer Beads | Temple Vow |
| Shrine Archivist | pencil mark: 25 % to reveal the digit | Ink Stone | Endless Archive |
| Koi Gambler | 33 % no HP lost on a mistake; 33 % +3 gold | Lucky Scale | Gilded Koi Scale |
| Stone Gardener | first item each puzzle not used up | Moss Pouch | Load-Bearing Stone |
| Lantern Seer | boss rules 30 % weaker; preview and a free reroll | Second Wick | Warding Flame |
| Reed Duelist | perfect puzzle without pencil: +2 pencil | Single Stroke | Dueling Reed |
| Quiet Cartographer | preview the next node after a perfect tile | Compass Needle | Accurate Map |

Owner's answers (picks of 2026-10-25, answers of 2026-10-26):

- **Two relics per class, not three:** the signature and the existing exclusive. The mastery relics of the first draft
  (Level 15) are dropped.
- **A higher chance, not a guarantee:** the signature relic is offered with a 35 % chance while it is not held (not
  guaranteed in the first offering).
- **Single Stroke for the Reed Duelist** instead of Bamboo Edge. **Ink Stone stays**, although it and Single Stroke
  bend Sudoku's rules the most.
- **Built together with the keystones** in one content update (RELIC-KEY-001/002).

Open: the class relic screens were not found in the code (checked 2026-10-27). The class scroll would list a class's relics
with silhouettes until their level; the Codex would get a "Class" filter; a class relic would carry the class's seal on
its plinth; an achievement would cover holding both relics of a class in one run. The balance check waits for a
playtest.

### Signature relics (2.4)

| ID | Requirement |
|---|---|
| RELIC-SIG-001 | Each class has one signature relic, open from class Level 1. It is not in the shared pool: a relic offering (relic node or boss table) for that class puts it among the choices with a 35 % chance while the player does not hold it. An offering holds at most one class relic: the signature first, else the Level 30 exclusive (25 %, CLASS-EXCL-004). Shown as Tier 2. |
| RELIC-SIG-002 | The effects below. Prayer Beads shield the first mistake of a puzzle while HP are full; Single Stroke returns `PlaceResult.TakenBack` (no mistake, no HP, the status line says so). |

| Class | Relic | Effect |
|---|---|---|
| Number Freak | Counting Beads | When the last copy of a digit is placed (the board holds it Size times), +1 Pencil up to the maximum. |
| Garden Monk | Prayer Beads | The first mistake of each puzzle costs no HP while HP are full. The mistake still counts for the grade. |
| Shrine Archivist | Ink Stone | Erasing pencil marks until one is left in a cell writes that digit, through the normal digit path (a wrong one is a mistake). |
| Koi Gambler | Lucky Scale | A puzzle finished with exactly one mistake pays double gold. |
| Stone Gardener | Moss Pouch | +1 bag slot. |
| Lantern Seer | Second Wick | Cursed tiles the player can reach next name their extra rule on the path (the rule they will bring: they are built one step deeper). |
| Reed Duelist | Single Stroke | While no pencil mark is on the board, the first wrong digit of each puzzle is taken back: not placed, not counted. |
| Quiet Cartographer | Compass Needle | The path shows the board size and stars of every tile on the floor (as Survey Notes and the Accurate Map do). |

### Keystones (2.4, RM-114)

| ID | Requirement |
|---|---|
| RELIC-KEY-001 | After the first boss of a Garden Run (not a tutorial, not the Monthly Walk) the offering table offers three of the four keystones instead of relics; the player takes one or leaves them. A keystone is held like a relic for the rest of the run and shows the tier "Keystone" in vermilion. |
| RELIC-KEY-002 | The four keystones bend a rule of the puzzle: |

| Keystone | Effect |
|---|---|
| Gilded Box | Every box (region) the player finishes pays 1 gold. |
| Open Margin | Pencil marks cost nothing on boards up to 6×6. |
| Steady Brush (`SteadyHand`) | Buying pencil costs 40 % less (RELIC-REWORK-001). |
| Cage Whisper | Cages with a hidden sum (the Hidden Killer rule) show its last digit: "?7". |

Icons: the twelve borrow existing game icons (`RelicService.GetIconFolder`) until they have art of their own.

### The RM-104 content (2.8)

The owner ticked R3 to R6, I3 and E2 to E4 from the first list and asked for trades: pay HP, Max HP or pencil marks
for a strong effect; of those they took three relics, three items and all four events (2026-11-04).

| ID | Requirement |
|---|---|
| RELIC-PACK-001 | **Cracked Bell** (Tier 2): −2 Max HP when taken (never below 1); every boss defeated gives +2 Max HP and heals 2. **Twin Koi** (Tier 3): every item that goes into the bag (reward, swap, shop, event, Seed Pouch) has one more charge. **Gold Leaf Brush** (Tier 3): a combo of 10 readies the next reward, once per puzzle; every item of it is one rarity higher (`ItemService.OneTierUp`: Normal to Rare as Empty Canvas, a tiered Rare item to Epic, a fixed Rare one to a random Epic; Epic stays); a reroll keeps the raise, taking or leaving the reward ends it. **Lantern of Daring** (Tier 2): a puzzle on the Risk route pays +50 % gold, on the Calm route −20 % (the route of the tile just solved). |
| RELIC-TRADE-001 | **Oni Pact** (Tier 4): every mistake costs 1 HP more; every puzzle pays +50 % gold and the reward shows one more plaque. **Smouldering Incense** (Tier 3): each puzzle start costs 1 HP (never below 1) and solves one box. **Dry Inkstone** (Tier 2): −5 max pencil when taken (never below 1); every combo of 5 heals 1 HP. |
| ITEM-PACK-001 | **Seed Pouch** (Rare): planted when used; after the next two solved puzzles a random Rare item goes into the bag (it waits while the bag is full), and the reward's tally names it. |
| ITEM-TRADE-001 | **Blood Ink** (Rare): costs 2 HP (needs more than 2), writes the correct digit in 3 random empty cells. **Ink Pact** (Rare): costs 6 pencil marks, completes the most-filled row, column or box. **Red Thread** (Rare): costs 1 Max HP (needs more than 1), a random relic of the floor's pool. An item whose price cannot be paid stays in the bag. |

All four items are in the Rare reward pool. Icons borrow game art like the signature relics (the Cracked Bell the
temple bell, the Red Thread the jade amulet on its cord; the Seed Pouch, Blood Ink and Ink Pact got copies of the bud,
the scroll stamp and the trade coin in `Resources/items`). The early garden (RUN-EARLY-001) keeps its ten entries; the
new content is open from the first run.

| ID | Requirement |
|---|---|
| ITEM-REFUND-001 | GAP-117: Koi Reflection, Temple Incense, the Moth Light and the Pattern Scroll are used up when picked; a choice left before any cell took effect (Esc, the Options, Save & Quit, a new puzzle) gives the item back, its charge or its slot, and the use is not counted. |
| ITEM-REWORK-002 | Owner's picks after the 0.4.3 playtest: items that did what another item did now do something of their own. **Chime of Notes** (`WindChime`), **River Bell** (`KoiDragonScale`), **Stone Oath** (`TempleStamp`), **Moth Light** (`GardenLantern`); effects in the item list. The enum names stay, so saves keep their items. |
| RELIC-REWORK-001 | The same for two relics: **Steady Brush** (`SteadyHand`): buying pencil costs 40 % less. **Void Petal**: the first mistake of each floor costs 3 pencil instead of HP. |
| RELIC-THREAD-001 | **Unbroken Thread** (Tier 3, owner's pick): a mistake halves the combo instead of ending it. A shield that keeps the combo or the River Bell goes first. |

### HUD

`HudViewController` renders relics from `HeldRelics`, including multiple relic buttons when the player has more than one.

## Codex and Discovery

Profile tracking includes:

- item discovery through codex entries
- relic discovery through `MetaProgressionState.DiscoveredRelics`

Implementation reference:

- `Assets/Scripts/Save/ProfileService.cs`

## Standalone Items

Bamboo Scroll (tiered), the Moth Light (Garden Lantern, Normal), Golden Koi and the Stone Oath (Temple Stamp, Rare) are **in the `ItemType` enum and in the roll pools** of shops and rewards (documented 2026-10-07, SPEC-CONF-035; the earlier "not yet wired" note and the name "Temple Seal" were wrong). Their effects are listed in the item table below.

## Explicit Non-Features

The current checked repo does **not** have:

- relics sold in shops
- a hard single-relic runtime cap
- progression rewards in Endless Zen or Spirit Trials

## Reference Tables (generated from code, 2026-10-07)

SPEC-MAC-001: the item and relic tables removed in the v2.0 rewrite, regenerated from `ItemService` and `RelicService` (English texts; the localized texts are in the catalogs). These are the acceptance definitions for each effect; `GameplayEffectContractTests` covers the effects.

### Items

Base shop price by rarity: Normal 40 gold, Rare 75 gold, Epic 150 gold (×2.5 since 2026-10-21, ECON-BAL-001; they were 15 / 30 / 60); shop price = base × (1 + 0.5 × floor index) × relic/class modifiers × Harmony surcharge (GoldEconomySystem).

| Item | Id | Rarity | Charges | Effect (by rarity where tiered: Epic / Rare / Normal) |
|------|----|--------|:------:|--------|
| Solver | `Solver` | Normal / Rare / Epic | 1 | Click a zone (row/column/box) to auto-complete it. / Fill a cell with its correct digit and auto-complete its box. / Fill a cell with its correct digit and highlight remaining candidates in its box. |
| Finder | `Finder` | Normal / Rare / Epic | 1 | Reveal ALL unsolved cells containing the selected digit. / Highlight {0} cell(s) matching the selected digit. |
| Ink Well | `InkWell` | Normal / Rare / Epic | 1 | Restore {0} pencil marks. |
| Meditation Stone | `MeditationStone` | Normal / Rare / Epic | 1 | Restore {0} HP. |
| Chime of Notes | `WindChime` | Normal | 1 | Writes every digit its row, column and box still allow (and the rules on the board) as a free note into every empty cell without notes. The player's own notes stay. Places no digit. |
| Pattern Scroll | `PatternScroll` | Normal / Rare / Epic | 1 | Select {0} mechanic instance(s) to solve using the solution. ESC cancels. The list names each piece by its rule, "German Whispers #1 · 4 cells", in the player's language (ITEM-SCROLL-001). A paper slip beside the board has one row per piece with its rule's colour and "3 empty of 4"; solved pieces are struck through and the chosen piece's cells are gold on the board. A click, the mouse wheel, the arrows or the D-pad choose; Solve, Enter or A writes it; B or Esc cancels. The digits are not shown beforehand: a cancelled scroll comes back (ITEM-REFUND-001), so a preview would be a free look at every solution (ITEM-SCROLL-002). |
| Koi Reflection | `KoiReflection` | Normal / Rare / Epic | 1 | Reveal candidates for every unsolved cell in the board's most ambiguous zone (row/column/box with most open cells). / Click {0} empty cell(s) to reveal their candidates. |
| Lantern of Clarity | `LanternOfClarity` | Normal / Rare / Epic | 1 | Disable fog for {0} moves. |
| Garden Rake | `GardenRake` | Normal | 1 | Clear all pencil marks in the selected row and column. |
| Offering Bowl | `OfferingBowl` | Normal | 1 | Sacrifice 1 HP to gain 30 gold. |
| Pruning Shears | `PruningShears` | Normal | 1 | Remove all invalid candidates from a cell, leaving only valid possibilities. |
| Zen Sand Sifter | `ZenSandSifter` | Normal | 1 | Highlight all cells with exactly two candidates. |
| Ginkgo Leaf | `GinkgoLeaf` | Rare | 1 | Undo the last mistake, restore the lost HP, and recover your combo. |
| Rice Paper Umbrella | `RicePaperUmbrella` | Rare | 1 | Block the next mistake penalty (2 charges). |
| Temple Incense | `TempleIncense` | Rare | 1 | Click {0} empty cell(s) to fill their correct answer. |
| River Bell | `KoiDragonScale` | Epic | 1 | This puzzle a mistake does not break the combo. At the end +2 gold per 5 combo. |
| Golden Kintsugi Jar | `GoldenKintsugiJar` | Epic | 1 | Fix all current mistakes on the board and reveal the correct digits for all affected cells. |
| Silk Fan | `SilkFan` | Epic | 2 | Generate {0} Shield. Shield absorbs HP damage before your health bar. While shielded, mistakes don't break your combo. Resets each puzzle. |
| Loaded Coin | `LoadedCoin` | Rare | 1 | Gain 1 Reroll Token. (NumberFreak exclusive) |
| Monk's Beads | `MonksBeads` | Rare | 1 | Your next 9 placements cost no pencil marks. (GardenMonk exclusive) |
| Annotated Folio | `AnnotatedFolio` | Rare | 1 | Reveal all candidates in a selected box. (ShrineArchivist exclusive) |
| Double or Quits | `DoubleOrQuits` | Rare | 1 | Double gold from this puzzle or lose 1 HP. (KoiGambler exclusive) |
| Worn Chisel | `WornChisel` | Rare | 1 | 30% discount at the next shop. (StoneGardener exclusive) |
| Dim Lantern | `DimLantern` | Rare | 1 | Remove fog from 3 random hidden cells permanently. (LanternSeer exclusive) |
| Reed Pledge | `ReedPledge` | Rare | 1 | Complete this puzzle using zero pencil marks for +2 HP. (ReedDuelist exclusive) |
| Survey Notes | `SurveyNotes` | Rare | 1 | Shows the board size and stars of every tile on this floor. (QuietCartographer exclusive) |
| Bamboo Scroll | `BambooScroll` | Normal / Rare / Epic | 1 | Reveal the star rating of all upcoming path nodes. |
| Moth Light | `GardenLantern` | Normal | 1 | Choose a box: its fog is gone for good. A box without fog keeps the choice open; a board without fog refuses the item. |
| Golden Koi | `GoldenKoi` | Rare | 1 | Grant +{0}g per star rating when this puzzle ends. |
| Stone Oath | `TempleStamp` | Rare | 1 | Costs 1 HP (refused at 1 HP). This puzzle pays 50 % more gold. |
| Bamboo Compass | `BambooCompass` | — | 1 | Highlight 2 cells where the next placement would likely violate an active constraint. |

### Relics

| Relic | Id | Tier | Effect |
|-------|----|------|--------|
| Smooth Pebble | `SmoothPebble` | Tier 1 | Start each puzzle with +2 pencil marks. |
| Cracked Teacup | `CrackedTeacup` | Tier 1 | Absorb the first mistake each puzzle: no HP lost and the combo holds (RELIC-TEACUP-001). |
| Wooden Comb | `WoodenComb` | Tier 1 | +1 Max HP. |
| Moss Token | `MossToken` | Tier 1 | Shop rerolls cost 25% less. |
| Koi Reflection | `KoiReflectionRelic` | Tier 2 | Reveal the correct candidates for 5 random empty cells at puzzle start (no auto-fill). |
| Monk's Charm | `MonkCharm` | Tier 2 | Every 5-hit combo: +5 gold AND +1 pencil. |
| Copper Tortoise | `CopperTortoise` | Tier 2 | Perfect puzzle completion grants +15 gold. |
| Jade Hairpin | `JadeHairpin` | Tier 2 | +1 item slot capacity. |
| Stone Sundial | `StoneSundial` | Tier 2 | +1 reward slot after each puzzle. |
| Sakura Seal | `SakuraSeal` | Tier 3 | Perfect-puzzle streak: +1 HP at streak >=2; +3 HP (reset) at streak >=5. |
| Crimson Fan | `CrimsonFan` | Tier 3 | Reduce all boss modifier intensities by one step. |
| Wisteria Branch | `WisteriaBranch` | Tier 3 | Heal 2 HP at the start of each floor. |
| Paper Crane | `PaperCrane` | Tier 3 | First item use each puzzle is free (no charge cost). |
| Porcelain Mask | `PorcelainMask` | Tier 3 | Elite reward rarity upgraded by one tier. |
| Unbroken Thread | `UnbrokenThread` | Tier 3 | A mistake halves the combo instead of ending it. No protection for HP. |
| Transmuted Sigil | `TransmutedSigil` | Tier 4 | +25% gold from puzzle completion. |
| Phoenix Feather | `PhoenixFeather` | Tier 4 | Prevent death once per run (heal to 1 HP). |
| Spirit Lantern | `SpiritLantern` | Tier 4 | Shop prices reduced by 20%. |
| Moonstone Compass | `MoonstoneCompass` | Tier 4 | Relic node tier boosted by +1. |
| Golden Root | `GoldenRoot` | Legendary | At floor start, gain 30% of the gold spent during the previous floor. |
| Silent Grid | `SilentGrid` | Legendary | Start each floor with 3 fresh mistake-shield charges (each absorbed mistake costs 0 HP). |
| Shifting Garden | `ShiftingGarden` | Legendary | Once per floor, a random given digit is relocated to another valid cell, keeping the puzzle uniquely solvable. |
| Eternal Lotus | `EternalLotus` | Legendary | Items are never consumed. |
| Dragon's Eye | `DragonsEye` | Legendary | Start each puzzle with one fully solved box. |
| Fortune's Ledger | `FortunesLedger` | Tier 3 (class exclusive) | Every 10 correct placements: +1 Reroll Token. Every 5-hit combo: +2 Reroll Tokens. (NumberFreak exclusive) |
| Temple Vow | `TempleVow` | Tier 3 (class exclusive) | When HP drops to 25% or below, heal 3 HP once per puzzle. (GardenMonk exclusive) |
| Endless Archive | `EndlessArchive` | Tier 3 (class exclusive) | After each correct placement, reveal one candidate in the same row. (ShrineArchivist exclusive) |
| Gilded Koi Scale | `GildedKoiScale` | Tier 3 (class exclusive) | Boosts Koi Gambler procs to 40%: lucky hit grants +5 gold, absorbed mistake grants +1 Reroll Token. (KoiGambler exclusive) |
| Load-Bearing Stone | `LoadBearingStone` | Tier 3 (class exclusive) | Each non-absorbed mistake generates 5 bonus gold at end of puzzle. (StoneGardener exclusive) |
| Warding Flame | `WardingFlame` | Tier 3 (class exclusive) | Once per run, automatically absorb one mistake (no HP loss). (LanternSeer exclusive) |
| Dueling Reed | `DuelingReed` | Tier 3 (class exclusive) | Completing a puzzle with zero pencil marks grants +20 bonus gold. (ReedDuelist exclusive) |
| Accurate Map | `AccurateMap` | Tier 3 (class exclusive) | Every floor: shows the board size and stars of every tile and guarantees one relic-tier reward node. (QuietCartographer exclusive) |
| Cracked Stone | `CrackedStone` | Tier 1 | [H2+] Each mistake after the first in a puzzle deals 0 HP (stone absorbs). Passive; unlimited. |
| Frosted Mirror | `FrostedMirror` | Tier 2 | [H5+] Once per mistake: spend 3 pencil to absorb 1 HP of damage (auto-triggers if pencil ≥ 3). At H5+ each hit after the first in a puzzle is partially reduced. |
| Void Petal | `VoidPetal` | Tier 3 | [H7+] The first mistake of each floor costs 3 pencil instead of HP. With fewer than 3 pencil the mistake costs HP and the petal waits for the next one. |
| Lantern of Void | `LanternOfVoid` | Legendary | [H9+] Boss modifier labels are always hidden (no auto-reveal on repeat). Earn +1 gold per active floor modifier on puzzle completion. |
