# Main Menu Items Collection System

This document outlines the persistent **Items Discovery Archive** for the **Run of the Nine** game.

## 1) Purpose

The **Items** menu serves as a reference for the player's progression, tracking:
- Collected relics
- Consumables
- Cursed items
- Legendary items
- Boss-exclusive rewards

Its primary goals include:
- Feedback on meta progression
- Tracking game completion
- Strategy planning
- Lifelong engagement
- Achievement support

## 2) Main Menu Structure 

The main menu contains:
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

The layout of the Items menu consists of:
- A top section displaying the completion count (`discovered / total`) and active filter label
- A middle section displaying a grid/list of entries
- A bottom/right detail panel for selected item details and help tooltip

## 4) Filter Tabs

Several tabs have been implemented for filtering:
- All
- Relics
- Consumables
- Cursed
- Legendary
- Boss Rewards
- Class-Specific

## 5) Display States

Each item has three states:
- **Undiscovered**: hidden name `???`, silhouette behavior, unlock hint visible
- **Discovered**: full name/details visible
- **Mastered**: for completion styling

## 6) Detail Panel Fields

For each selected item, the following fields are displayed:
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

Additionally, a versioning system for the codex:
- `ItemCodexState.SaveDataVersion`

## 8) Item Roll System Visibility

Help text is provided for:
- Slot roll scaling by difficulty
- Nothing-slot mechanic
- Replacement/reroll constraints

## 9) Rarity Framework

The current codex supports rarity labels for:
- Common
- Rare
- Epic
- Legendary
- Cursed-style entries via type/category

Visual coding can map these labels to themed frames/glows.

## 10) Completion Milestones

Planned incentives include:
- 25% cosmetic decoration
- 50% new background theme
- 75% unique lantern
- 100% secret class or legendary relic

## 11) UX Requirements

The current implementation supports:
- Mouse clicks for filters and navigation
- Keyboard-ready extension points (next/prev bindings can be added)

Roadmap includes:
- Controller navigation
- Scroll-grid virtualization
- Smooth tab transitions
- New-item indicator

## 12) Game System Integration

The codex data is persistent and is designed to sync with:
- Run economy loop
- Class progression
- Relic/cursed systems
- Achievement scaffolding

## 13) Visual Direction

The target style remains:
- Serene Japanese garden mood
- Soft natural tones
- Wooden/parchment inspired paneling (theme layer)

## 14) Strategic Unlock Rule

The adopted design:
- Unlock on discovery
- Mastered on win/use criteria

## 15) Future Compatibility

The data model supports:
- Seasonal/event items
- Version-safe additions
- Balance patch evolution
- DLC-compatible extension via save data versioning

## 16) Intended Outcome

The Items codex aims to:
- Encourage experimentation
- Reduce RNG frustration through visibility
- Support build theorycrafting
- Increase completionist retention


