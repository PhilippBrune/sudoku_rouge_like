# Items & Relics System

**Version:** 2.4  
**Date:** 2026-10-26  
**Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 2.4 | 2026-10-26 | implemented | Owner picks (2026-10-25): a signature relic per class from Level 1 with a higher chance in offerings (RELIC-SIG-001/002, `design/class-relics-concept.md`, Single Stroke for the Reed Duelist), and four keystones offered after a Garden Run's first boss that bend a rule of the puzzle (RELIC-KEY-001/002, RM-114, the owner's answer: a relic may bend Sudoku's rules). |
| 2.3 | 2026-10-21 | implemented | Counter items, one per modifier family (ITEM-COUNTER-001..006), and shops that lean toward the counters of the board's rules (ITEM-COUNTER-007); lever L3 of `design/gold-and-items-concept.md`. |
| 2.2 | 2026-10-21 | implemented | Item base prices ×2.5: Normal 40, Rare 75, Epic 150 (ECON-BAL-001, lever L1 of `design/gold-and-items-concept.md`, owner pick after the 0.2.0 playtest). The Warding Flame's removed trial is kept in the save (`RunState.WardingFlameOnFloor`/`WardingFlameTrial`): it used to come back after Save & Quit at the gate. |
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

### Counter Items (2.3, ITEM-COUNTER-001..006)

One item per modifier family answers the rules that are hard on this board (lever L3 of
`design/gold-and-items-concept.md`). Each writes the solution's digits into cells its family's rules use, starting at the
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

Endless Zen and Spirit Trials set `ItemSlots = 0` and skip progression rewards, so the item reward/shop loop does not apply there.

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

### Signature relics (2.4)

| ID | Requirement |
|---|---|
| RELIC-SIG-001 | Each class has one signature relic, open from class Level 1. It is not in the shared pool: a relic offering (relic node or boss table) for that class puts it among the choices with a 35 % chance while the player does not hold it. An offering holds at most one class relic: the signature first, else the Level 30 exclusive (25 %, CLASS-EXCL-004). Shown as Tier 2. |
| RELIC-SIG-002 | The effects below. Prayer Beads and Steady Hand share one shield per puzzle; Single Stroke returns `PlaceResult.TakenBack` (no mistake, no HP, the status line says so). |

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
| Steady Hand | The first mistake of each puzzle costs no HP (the mistake still counts for the grade). |
| Cage Whisper | Cages with a hidden sum (the Hidden Killer rule) show its last digit: "?7". |

Icons: the twelve borrow existing game icons (`RelicService.GetIconFolder`) until they have art of their own.

### HUD

`HudViewController` renders relics from `HeldRelics`, including multiple relic buttons when the player has more than one.

## Codex and Discovery

Profile tracking includes:

- item discovery through codex entries
- relic discovery through `MetaProgressionState.DiscoveredRelics`

Implementation reference:

- `Assets/Scripts/Save/ProfileService.cs`

## Standalone Items

Bamboo Scroll and Garden Lantern (tiered), Golden Koi and Temple Stamp (Rare) are **in the `ItemType` enum and in the roll pools** of shops and rewards (documented 2026-10-07, SPEC-CONF-035; the earlier "not yet wired" note and the name "Temple Seal" were wrong). Their effects are listed in the item table below.

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
| Wind Chime | `WindChime` | Normal / Rare / Epic | 1 | Click a cell to fill its row + column + box. / Choose any 2 of row/column/box, then click a cell to fill them. / Choose row, column, OR box, then click a cell to fill it. |
| Pattern Scroll | `PatternScroll` | Normal / Rare / Epic | 1 | Select {0} mechanic instance(s) to solve using the solution. ESC cancels. |
| Koi Reflection | `KoiReflection` | Normal / Rare / Epic | 1 | Reveal candidates for every unsolved cell in the board's most ambiguous zone (row/column/box with most open cells). / Click {0} empty cell(s) to reveal their candidates. |
| Lantern of Clarity | `LanternOfClarity` | Normal / Rare / Epic | 1 | Disable fog for {0} moves. |
| Garden Rake | `GardenRake` | Normal | 1 | Clear all pencil marks in the selected row and column. |
| Offering Bowl | `OfferingBowl` | Normal | 1 | Sacrifice 1 HP to gain 30 gold. |
| Pruning Shears | `PruningShears` | Normal | 1 | Remove all invalid candidates from a cell, leaving only valid possibilities. |
| Zen Sand Sifter | `ZenSandSifter` | Normal | 1 | Highlight all cells with exactly two candidates. |
| Ginkgo Leaf | `GinkgoLeaf` | Rare | 1 | Undo the last mistake, restore the lost HP, and recover your combo. |
| Rice Paper Umbrella | `RicePaperUmbrella` | Rare | 1 | Block the next mistake penalty (2 charges). |
| Temple Incense | `TempleIncense` | Rare | 1 | Click {0} empty cell(s) to fill their correct answer. |
| Koi Dragon Scale | `KoiDragonScale` | Epic | 1 | Complete the most-filled row, column, or box. |
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
| Garden Lantern | `GardenLantern` | Normal / Rare / Epic | 1 | Lift fog from the entire board for 15 seconds, then re-fog. |
| Golden Koi | `GoldenKoi` | Rare | 1 | Grant +{0}g per star rating when this puzzle ends. |
| Temple Stamp | `TempleStamp` | Rare | 1 | Protect your HP this puzzle - all wrong placements deal 0 damage. Consumed on completion. /  |
| Bamboo Compass | `BambooCompass` | — | 1 | Highlight 2 cells where the next placement would likely violate an active constraint. |

### Relics

| Relic | Id | Tier | Effect |
|-------|----|------|--------|
| Smooth Pebble | `SmoothPebble` | Tier 1 | Start each puzzle with +2 pencil marks. |
| Cracked Teacup | `CrackedTeacup` | Tier 1 | Absorb the first mistake each puzzle (no HP loss). |
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
| Void Petal | `VoidPetal` | Tier 3 | [H7+] Once per run: survive a lethal hit at 1 HP. Grants 1 free Normal Solver upon activation. |
| Lantern of Void | `LanternOfVoid` | Legendary | [H9+] Boss modifier labels are always hidden (no auto-reveal on repeat). Earn +1 gold per active floor modifier on puzzle completion. |
