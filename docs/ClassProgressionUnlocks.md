# Class Progression Unlocks

**Version:** 0.2 | **Date:** 2026-04-18 | **Status:** new

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 0.2 | 2026-04-18 | new | Added Starting Relics section (relic system rework integration) |
| 0.1 | 2026-04-09 | new | Initial specification |

---

## Starting Relics

As part of the Relic System Rework, the old "+X relic slot" cap is removed. Relics held during a run are now **unlimited**. Instead, classes gain a **"Start with X relics"** stat — relics pre-loaded into the bag at run start.

### L1 Baseline

All classes start with **1 relic** at Level 1 (a random T1 relic rolled fresh each run).

**Exception — KoiGambler:** Starts with **2 relics** at L1. Their identity is maximum variance per run; two random starting relics amplify that unpredictability without requiring level investment.

### Level Milestone Unlock Schedule

Each "+1 Starting Relic" milestone adds one additional **random T1 relic** to the run's starting bag. The L30 class-exclusive relics (Fortune's Ledger, Temple Vow, etc.) are **pool-injection-only** — they appear as discoverable choices during the run (relic rewards, shop) but are never pre-loaded.

| Class | L1 | +1 at level | Max starting relics |
|---|---|---|---|
| NumberFreak | 1 | L10, L20 | 3 |
| GardenMonk | 1 | L20 | 2 |
| ShrineArchivist | 1 | L10 | 2 |
| KoiGambler | **2** | L10 | 3 |
| StoneGardener | 1 | L10, L20 | 3 |
| LanternSeer | 1 | L20 | 2 |
| ReedDuelist | 1 | L20 | 2 |
| QuietCartographer | 1 | L10 | 2 |

**Rationale for class variation:**
- **GardenMonk / LanternSeer / ReedDuelist** — Lean, pressure-focused classes. Their second random relic unlocks at L20 to keep early-game focused on survival/tempo mechanics.
- **ShrineArchivist / QuietCartographer** — Information-heavy classes. Second relic at L10 (early) because a relic at run start amplifies information-gathering immediately; they skip L20 in favour of the named L30 starter.
- **StoneGardener / NumberFreak** — "Stuff accumulation" identity. Both unlock L10 and L20 random slots, reaching max 4 at L30.
- **KoiGambler** — Starts with 2 and gains a L10 slot; no L20 slot (variance is front-loaded, not late-game scaled).

### Implementation Notes

- `ClassDefinition.BaseStartingRelics` — field added (default 1; KoiGambler = 2)
- Milestone strings `"+1 Starting Relic"` parsed by `RunArchetypeService` regex
- `RelicService.AssignStartingRelics(state, count)` — rolls `count` distinct random T1 relics; class-exclusive relics are never pre-loaded here
- Starting relics are added to `RunState.HeldRelics` before the run begins

---

## Overview

Each class has two **exclusive milestone unlocks** — a unique item at Level 15 and a unique relic at Level 30. These add to the discoverable pool only when playing that specific class, at or above the required level. Unlike the existing milestone stat bonuses (+HP, +Pencil), these unlocks change *what choices are available*, not just how strong the player is.

The goal is to make leveling a class feel like opening a new build space rather than passively getting stronger.

---

## How It Works

### Pool Injection

- A new `ClassId ClassExclusive` field is added to `ItemDefinition` and `RelicDefinition`.
- A new `int ClassUnlockLevel` field (15 or 30) is added alongside it.
- `ItemService.RollSlots()` and `ShopService.BuildOffers()` filter out class-exclusive items unless:
  - `RunState.SelectedClass == item.ClassExclusive`
  - `ClassGardenProgressionService.GetLevel(RunState.SelectedClass) >= item.ClassUnlockLevel`
- `RelicService.RollRelicChoices()` applies the same filter for exclusive relics.
- Discovery is recorded in the codex on first acquisition (same as any other item/relic).

### Unlock Display

On the class select panel, the locked L15 item and L30 relic are shown as silhouettes with the label:
- `"Unlocks at Level 15: ???"` → revealed as `"[Item Name]"` once achieved
- `"Unlocks at Level 30: ???"` → revealed as `"[Relic Name]"` once achieved

On the meta progression panel, unlocked exclusives are listed under each class alongside their level milestone.

---

## The 16 Exclusive Unlocks

### 1. Number Freak — Balanced / Reroll-focused

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Loaded Coin** | On the next item reward screen, one Nothing slot is automatically removed and replaced with a real item roll. |
| L30 | Relic (T3) | **Fortune's Ledger** | After every 10th correct placement in the run, gain 1 Reroll Token. Counter resets between puzzles. |

*Design note:* Loaded Coin synergises with the class passive (extra Reroll Token each run) by guaranteeing more real options to reroll among. Fortune's Ledger rewards consistent, mistake-free play — the Number Freak's identity.

---

### 2. Garden Monk — Sustain / Streak-healer

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Monk's Beads** | Activate during a puzzle. The next 5 consecutive correct placements each restore 1 HP (capped at MaxHP). Any mistake during those 5 cancels the effect. |
| L30 | Relic (T3) | **Temple Vow** | When HP drops to ≤ 25% of MaxHP, the next correct placement restores 2 HP. Triggers at most once per puzzle. |

*Design note:* Monk's Beads extends the passive streak-heal in a concentrated form. Temple Vow creates a "clutch recovery" moment — consistent with the Monk's "survive through pressure" identity. Note: the Monk cannot buy Pencil mid-level, so HP recovery is the primary survival tool.

---

### 3. Shrine Archivist — Information / Pencil-heavy

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Annotated Folio** | At puzzle start, automatically places correct pencil mark candidates in all empty cells that have exactly 2 valid candidates. Pencil cost is waived for these marks. |
| L30 | Relic (T4) | **Endless Archive** | Pencil marks no longer consume Pencil units. MaxPencil remains tracked for display; it simply cannot decrease from pencil mark use. |

*Design note:* Annotated Folio gives an information head-start on every board — very powerful on harder boards where hidden singles are the entry point. Endless Archive is the Archivist's "dream state" — pure information, zero resource cost. Appropriate at L30 given the class's high natural pencil already.

---

### 4. Koi Gambler — Volatility / Gold-focused

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Double or Quits** | Before the item reward screen is shown, gamble your current gold: 50% chance to double it, 50% chance to halve it (floored to 0). The roll is shown before the item screen opens. |
| L30 | Relic (T3) | **Gilded Koi Scale** | Lucky proc chance increases from 25% to 35% for both effects (wrong placement = 0 HP, correct placement = +1 gold). The gold proc now grants +2 gold instead of +1. |

*Design note:* Double or Quits is pure Gambler flavour — high stakes, informed risk (you see the result before managing items). Gilded Koi Scale amplifies both sides of the passive without changing its nature, making the Gambler feel more "tuned in" at mastery level.

---

### 5. Stone Gardener — Item Synergy / High slots

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Worn Chisel** | All shop items on the current floor cost 30% less. Applied immediately when used; effect lasts until the current floor ends. |
| L30 | Relic (T4) | **Load-Bearing Stone** | When an item would be discarded after use (charges drop to 0), there is a 30% chance it retains 1 charge instead. |

*Design note:* Worn Chisel creates a strong "shop floor" decision — when do you use it? Load-Bearing Stone synergises with the class passive (first item use per puzzle is free) by keeping items in play longer. A high-slot Gardener can now sustain a full bag across more floors.

---

### 6. Lantern Seer — Boss Specialist / Low HP

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Dim Lantern** | Reveals the exact modifier list and intensity for the current floor's boss before you reach the Boss Gate. Shown as a brief overlay panel, dismissed on input. |
| L30 | Relic (T3) | **Warding Flame** | Once per run, at any Boss Gate, you may remove one modifier from the selection pool before making your choice. The removed modifier is discarded. |

*Design note:* Dim Lantern gives the Seer forward-planning ability — they can adjust item usage and route decisions knowing exactly what boss is coming. Warding Flame is build-defining at L30: the Seer, who already weakens boss mods by 20%, can also remove the worst one entirely, enabling otherwise impossible boss configurations.

---

### 7. Reed Duelist — Tempo / No-pencil reward

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Reed Pledge** | Activating this commits you to completing the current puzzle with no pencil marks. On success: +4 Pencil, +30 Gold, +25 bonus XP. If you use any pencil mark before completion: take -1 HP immediately and lose all bonuses. Item is consumed either way. |
| L30 | Relic (T3) | **Dueling Reed** | Solving any puzzle with zero pencil marks AND zero mistakes grants +2 Pencil and +1 Reroll Token. |

*Design note:* Reed Pledge is a voluntary high-stakes bet that amplifies the Duelist's passive. Dueling Reed rewards the "clean solve" loop on every qualifying puzzle — a relic that rewards mastery of the class's core challenge.

---

### 8. Quiet Cartographer — Route Control / Methodical

| Level | Type | Name | Effect |
|-------|------|------|--------|
| L15 | Item (Unique) | **Survey Notes** | Reveals the node type AND star rating of all unvisited nodes on the current floor's path (both Calm and Risk routes). Hidden nodes become fully visible for the rest of the floor. |
| L30 | Relic (T3) | **Accurate Map** | Once per floor, after selecting a node to travel to, you may redirect to any adjacent same-route node instead. The redirect must be made before the node begins. |

*Design note:* Survey Notes gives full path information — the Cartographer's dream. Accurate Map gives literal path control: the only class in the game that can change their mind about a node mid-floor. Fits the class identity of "previewing and planning" taken to its logical extreme.

---

## Data Model Changes

### `ItemType` enum
Add 8 new values: `LoadedCoin`, `MonksBeads`, `AnnotatedFolio`, `DoubleOrQuits`, `WornChisel`, `DimLantern`, `ReedPledge`, `SurveyNotes`

### `RelicId` enum
Add 8 new values: `FortunesLedger`, `TempleVow`, `EndlessArchive`, `GildedKoiScale`, `LoadBearingStone`, `WardingFlame`, `DuelingReed`, `AccurateMap`

### `ItemDefinition` / item catalog entry (new fields)
```csharp
public ClassId ClassExclusive; // ClassId.None = available to all
public int ClassUnlockLevel;   // 0 = no restriction, 15 or 30
```

### `RelicDefinition` (same new fields)
```csharp
public ClassId ClassExclusive;
public int ClassUnlockLevel;
```

### `ClassGardenProgressionService`
No change — `GetLevel(classId)` already exists and derives level from `TotalXp`.

---

## Integration Points

| Location | Change |
|----------|--------|
| `ItemService.GetCatalog()` | Add 8 new item definitions with `ClassExclusive` and `ClassUnlockLevel` set |
| `ItemService.RollSlots()` | Pre-filter: exclude items where `ClassExclusive != None && (SelectedClass != ClassExclusive OR Level < ClassUnlockLevel)` |
| `ShopService.BuildOffers()` | Same filter |
| `RelicService` catalog | Add 8 new relic definitions |
| `RelicService.RollRelicChoices()` | Same filter as ItemService |
| `ClassSelectViewController` | Show exclusive unlock slots with silhouette until unlocked |
| `MetaProgressionPanelController` | List exclusive unlocks per class alongside milestone rewards |
| `ItemEffectHandler` / `RunDirector` | Implement the 8 new item effects (see below) |

---

## Item Effect Implementation Notes

| Item | Hook | Key Logic |
|------|------|-----------|
| **Loaded Coin** | `BuildItemRewardSlots()` | Before rolling, force one Nothing slot to re-roll until non-Nothing |
| **Monk's Beads** | `RunDirector.PlaceNumber()` | Start a 5-placement countdown; each correct += 1 HP; any mistake cancels |
| **Annotated Folio** | Puzzle start | Scan board for cells with exactly 2 valid candidates; add both as pencil marks for free |
| **Double or Quits** | Pre-reward screen | `rng.NextDouble() < 0.5` → gold ×2 else ÷2; show result panel first |
| **Worn Chisel** | `ShopService.BuildOffers()` | Apply 0.70× price multiplier when `WornChiselActive` flag set in `RunState` |
| **Dim Lantern** | On use (path map) | Show boss modifier list from `BossService.GetFloorModifiers(floorIndex)` |
| **Reed Pledge** | `RunDirector.TryAddPencilMark()` + completion | Flag `PledgeActive`; on pencil → -1 HP + cancel; on completion → grant bonuses |
| **Survey Notes** | On use (path map) | Set `RunState.AllNodesRevealed = true` for current floor; `RunMapController` renders all nodes |

---

## Scope Boundaries

| In scope | Out of scope |
|----------|-------------|
| 16 exclusive unlocks (8 items + 8 relics) | Synergy tooltips between class unlocks and existing items |
| Class select silhouette display | Animated unlock ceremony in class select |
| Meta panel listing | Sorting/filtering codex by class |
| Pool injection at item/relic roll time | Retroactive discovery for players already at L15/L30 (auto-discovered on next qualifying roll) |
