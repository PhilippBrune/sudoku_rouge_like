# Items & Relics System

**Version:** 2.0  
**Date:** 2026-04-27  
**Status:** implemented

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

Reward slot rolling still follows star-based item rewards through `ItemService.RollSlots(...)`, with additional effects from:

- relic bonus slots
- positive floor effects
- curses
- harmony perks that modify reward behavior

### Shops

Shops currently sell **items only**.

`ShopService.BuildOffers(...)` builds item offers using:

- floor-based slot counts
- rarity roll by floor/class level
- item price scaling
- harmony and relic price modifiers

### No-Progression Modes

Endless Zen and Spirit Trials set `ItemSlots = 0` and skip progression rewards, so the item reward/shop loop does not apply there.

## Relics

### Inventory Model

Runtime relic storage is **multi-relic**, not single-slot.

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

### HUD

`HudViewController` renders relics from `HeldRelics`, including multiple relic buttons when the player has more than one.

## Codex and Discovery

Profile tracking includes:

- item discovery through codex entries
- relic discovery through `MetaProgressionState.DiscoveredRelics`

Implementation reference:

- `Assets/Scripts/Save/ProfileService.cs`

## Standalone Items (Active, Undocumented Until Now)

These four items are active in the icon manifest and have generated art. They are available as
normal-rarity shop drops and puzzle rewards. They are **not** yet wired into the ItemType enum
roll pool — add them when their effects are finalised.

| Item | Rarity | Effect |
|------|--------|--------|
| **Bamboo Scroll** | Normal | Reveals the star rating of the next puzzle before you commit to the path node. Single use. |
| **Garden Lantern** | Normal | Illuminates all fogged cells on the current board for 15 seconds, then re-fogs them. Single use. |
| **Golden Koi** | Rare | At the end of the current puzzle, grants +10 gold per star rating (e.g. 5★ → +50g). Single use. |
| **Temple Seal** | Rare | Locks your current HP total — any wrong placements during this puzzle cost 0 HP. Consumed at puzzle completion. Single use. |

## Explicit Non-Features

The current checked repo does **not** have:

- relics sold in shops
- a hard single-relic runtime cap
- progression rewards in Endless Zen or Spirit Trials
