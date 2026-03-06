# Main Menu Items Collection System

This document defines the persistent **Items Discovery Archive** for **Run of the Nine**.

## Purpose

The **Items** menu serves as a catalogue, not an inventory, showing discovered items such as:
- Relics
- Consumables
- Cursed items
- Legendary items
- Boss-exclusive rewards

The purpose of this system is to provide feedback on meta progression, tracking completion, and facilitating strategy planning, long-term engagement, and achievement support.

## Main Menu Structure

The main menu includes:
- Start Game
- Tutorial
- Resume Game
- Items
- Options
- Language (EN/DE)
- Sound Settings
- Graphics Settings
- End Game

## Items Menu Core Layout

The top section of the Items menu displays:
- A count of discovered items (`discovered / total`)
- The active filter label

The middle section displays:
- A grid/list display of items

The bottom/right details panel shows:
- Detailed information about the selected item
- A help tooltip for the roll system

## Filter Tabs

The following filter tabs are implemented:
- All
- Relics
- Consumables
- Cursed
- Legendary
- Boss Rewards
- Class-Specific

## Display States

Each item supports three states:
- **Undiscovered**: The item is hidden with a silhouette behavior and an unlock hint is visible.
- **Discovered**: The full name/details of the item are visible.
- **Mastered**: For completion-based items, an optional elevated state for completion styling is supported.

## Detail Panel Fields

The selected item displays the following fields:
- Name
- Type
- Rarity
- Description
- Effect Formula
- Synergy Tags
- Discovery Date
- Times Used
- Wins With Item
- Best Run Depth

## Persistent Data Model

Items are serialized in save via `MetaProgressionState.ItemCodex`, including:
- `ItemID`
- `Name`
- `Type`
- `RarityTier`
- `UnlockCondition`
- `Description`
- `EffectFormula`
- `SynergyTags`
- `Discovered`
- `Mastered`
- `TimesPicked`
- `TimesWon`
- `TimesUsed`
- `BestRunDepth`
- `DiscoveredDate`

Plus codex versioning:
- `ItemCodexState.SaveDataVersion`

## Item Roll System Visibility

The items menu includes help text for:
- Slot roll scaling by difficulty
- Nothing-slot mechanic
- Replacement/reroll constraints

## Rarity Framework

The current codex supports rarity labels for:
- Common
- Rare
- Epic
- Legendary
- Cursed-style entries via type/category

Future visual coding can map these labels to themed frames/glows.

## Completion Milestones

Planned incentives include:
- 25% cosmetic decoration
- 50% new background theme
- 75% unique lantern
- 100% secret class or legendary relic

## UX Requirements

The current implementation supports:
- Mouse clicks for filters and navigation
- Keyboard-ready extension points (next/prev bindings can be added)

Roadmap includes:
- Controller navigation
- Scroll-grid virtualization
- Smooth tab transitions
- New-item indicator

## Game System Integration

Codex data is persistent and designed to sync with:
- Run economy loop
- Class progression
- Relic/cursed systems
- Achievement scaffolding

## Visual Direction

The target style remains:
- Serene Japanese garden mood
- Soft natural tones
- Wooden/parchment inspired paneling (theme layer)

## Strategic Unlock Rule

Adopted design includes:
- Unlock on discovery
- Mastered on win/use criteria

## Future Compatibility

The data model supports:
- Seasonal/event items
- Version-safe additions
- Balance patch evolution
- DLC-compatible extension via save data versioning

## Intended Outcome

The Items codex should:
- Encourage experimentation
- Reduce RNG frustration through visibility
- Support build theorycrafting
- Increase completionist retention


