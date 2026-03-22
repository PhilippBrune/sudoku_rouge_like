# Main Menu Items Collection System

This document defines the persistent **Items Discovery Archive** for **Run of the Nine**.

## 1) Purpose

The **Items** menu is a codex (not an inventory) showing discovered:
- Relics
- Consumables
- Cursed items
- Legendary items
- Boss-exclusive rewards

Primary goals:
- Meta progression feedback
- Completion tracking
- Strategy planning
- Long-term engagement
- Achievement integration support

## 2) Main Menu Structure (Updated)

Main menu contains:
- Start Game
- Tutorial
- Resume Game
- Items
- Options
- Language (EN/DE)
- Sound Settings
- Graphics Settings
- End Game

## 3) Items Menu Core Layout

Top section:
- Completion count (`discovered / total`)
- Active filter label

Middle section:
- Grid/list display of entries

Bottom/right details:
- Selected item detail panel
- Roll-system help tooltip

## 4) Filter Tabs

Implemented tabs:
- All
- Relics
- Consumables
- Cursed
- Legendary
- Boss Rewards
- Class-Specific

## 5) Display States

Each item supports:
- **Undiscovered**: hidden name `???`, silhouette behavior, unlock hint visible
- **Discovered**: full name/details visible
- **Mastered**: optional elevated state for completion styling

## 6) Detail Panel Fields

For selected item:
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

## 7) Persistent Data Model

Serialized in save via `MetaProgressionState.ItemCodex`:
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

## 8) Item Roll System Visibility

Items menu includes help text for:
- Slot roll scaling by difficulty
- Nothing-slot mechanic
- Replacement/reroll constraints

## 9) Rarity Framework (Current)

Current codex supports rarity labels for:
- Common
- Rare
- Epic
- Legendary
- Cursed-style entries via type/category

Future visual coding can map these labels to themed frames/glows.

## 10) Completion Milestones (Roadmap)

Planned incentives:
- 25% cosmetic decoration
- 50% new background theme
- 75% unique lantern
- 100% secret class or legendary relic

## 11) UX Requirements

Current implementation supports:
- Mouse clicks for filters and navigation
- Keyboard-ready extension points (next/prev bindings can be added)

Roadmap:
- Controller navigation
- Scroll-grid virtualization
- Smooth tab transitions
- New-item indicator

## 12) Game System Integration

Codex data is persistent and designed to sync with:
- Run economy loop
- Class progression
- Relic/cursed systems
- Achievement scaffolding

## 13) Visual Direction

Target style remains:
- Serene Japanese garden mood
- Soft natural tones
- Wooden/parchment inspired paneling (theme layer)

## 14) Strategic Unlock Rule

Adopted design:
- Unlock on discovery
- Mastered on win/use criteria

## 15) Future Compatibility

The data model supports:
- Seasonal/event items
- Version-safe additions
- Balance patch evolution
- DLC-compatible extension via save data versioning

## 16) Intended Outcome

The Items codex should:
- Encourage experimentation
- Reduce RNG frustration through visibility
- Support build theorycrafting
- Increase completionist retention
