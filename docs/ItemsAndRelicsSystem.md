# Items & Relics System

**Version:** 1.5 | **Date:** 2026-04-16 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.5 | 2026-04-16 | implemented | Inline requirement IDs added to all key sections (ITEM-SLOT-001/002, ITEM-EPIC-001, RELIC-SLOT-001, RELIC-ROLL-001, SHOP-PRICE-001). Stale auto-generated traceability table replaced. |
| 1.4 | 2026-04-07 | implemented | Shop sells items only — no relic slot in shop. Relics are acquired exclusively from Relic tile nodes, Elite tile rewards, and Run events. Removed "shop purchases" from the Relic Acquisition list. |
| 1.3 | 2026-04-07 | implemented | Relic node reward flow redesigned: instead of automatically granting a single relic, the node now presents a **3-of-3 choice panel** — three distinct relics are rolled and the player picks one (or leaves all). `RelicService.RollRelicChoices(floorIndex, count=3)` generates the pool with deduplication. A **"Leave"** button lets the player skip all choices. Discovery is recorded only for the chosen relic. |
| 1.2 | 2026-03-29 | implemented | Item Codex population wired: `ProfileService.RecordItemDiscovery(ItemType)` and `RecordRelicDiscovery(RelicId)` added. Called from `ClaimReward()` (puzzle rewards), `TryBuyOffer()` (shop purchases), and `HandleRelic()` (relic tile nodes). Codex entries use `ItemType.ToString()` as key; relics use `MetaProgressionState.DiscoveredRelics` list. |
| 1.1 | 2026-03-23 | implemented | Data models, RelicService, ItemService, ShopService verified implemented. Single relic slot, 23 relics, 18 items, tier weights, shop pricing all correct |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

The game has two distinct collectible categories during a run: **Consumables (Items)** and **Relics**. Both are acquired during runs and lost at run end. They serve fundamentally different roles — items are tactical single-use tools, relics are passive permanent-for-run effects.

---

## Consumables vs Relics

| Property | Consumables (Items) | Relics |
|----------|:---:|:---:|
| Usage | Single-use, consumed on activation | Permanent for the run |
| Effect | Active — player targets and confirms | Passive — always active once acquired |
| Power level | Lower, tactical | Higher, build-defining |
| Acquisition | Shop purchase, puzzle rewards, level-up starting items | Relic tile nodes, elite rewards, events |
| Stackability | Stack up to 2 charges | No duplicates (unless explicitly tagged stackable) |
| Inventory | Limited by Item Slots (class-dependent) | Single relic slot — player holds **one relic at a time** |

---

## Consumable Item System

### Item Slot System `[ITEM-SLOT-001]`

After completing a puzzle, the player is offered item reward slots based on the tile's star difficulty:

| Star Rating | Reward Slots |
|:---:|:---:|
| 1★ | 2 |
| 2★ | 3 |
| 3★ | 3 |
| 4★ | 4 |
| 5★ | 5 |

**Only ONE item may be selected per reward screen.** Unselected slots become inactive.

### "Nothing" Slot Rule

One slot may roll **Nothing**. If selected:
- Player gains a small gold bonus
- Counts as the chosen slot
- Slot becomes locked

Nothing acts as a controlled sacrifice mechanic — teaching the player that not every reward is worth taking.

### Nothing Slot Probability `[ITEM-SLOT-002]`

| Star Rating | Nothing Chance per Slot |
|:---:|:---:|
| 1★ | 25% |
| 2★ | 22% |
| 3★ | 18% |
| 4★ | 15% |
| 5★ | 12% |

Lower stars have higher Nothing probability (teaches sacrifice). Higher stars have lower Nothing probability to preserve agency in high-risk states.

### Reroll Rules

- Rerolling costs gold
- If a slot was already picked → disabled (cannot reroll)
- If a slot rolled Nothing → disabled
- Remaining unpicked, non-Nothing slots are rerollable

### Item Replacement Rule

- Enabled from **4★ difficulty onward**
- If inventory is full, an explicit replace UI is required
- The player chooses which existing item to discard
- Replaced item is **permanently lost** for the remainder of the run
- Replacement is irreversible

### Item Activation UI

- Click item → highlight valid targets on the board → confirm use
- Invalid targets provide soft visual feedback only (no error popup)

### Item Economy `[SHOP-PRICE-001]`

**Price Scaling:** Item costs in shops scale with floor progression:
```
ShopPrice = BasePrice × (1 + Floor_Index × 0.5)
```

**Base Prices by Rarity:**

| Rarity | Base Price (Floor 1) | Floor 3 Price | Floor 5 Price |
|--------|:---:|:---:|:---:|
| Normal | 15g | 30g | 45g |
| Rare | 30g | 60g | 90g |
| Epic | 60g | 120g | 180g |

**Sell Value:** Disabled. Items are permanent additions to the inventory until consumed.

**Item Penalty:** None. Using items is considered a tactical choice and does **not** reduce XP or star rating.

### Epic Item Availability `[ITEM-EPIC-001]`

- Epic items only enter the shop pool at **class Level 15**
- Spawn rate increases linearly from Level 15 to Level 40
- Example: ~2% chance at Level 15, rising to ~7% at Level 40
- Epic items remain rare rewards even for master players

---

## Consumable Item Table

Many items exist in **tiered variants** (Normal → Rare → Epic) with scaling power. The rarity determines both the effect strength and the UI border colour.

### Rarity Border Legend

| Rarity | UI Border |
|--------|-----------|
| Normal | 1px Silver (#C0C0C0) |
| Rare | 1px Gold (#FFD700) |
| Epic | 1px Cyan Glow (#00FFFF) or Purple Glow (#BF40BF) |

---

### Solver (Tiered)

Use item, then click an empty cell to fill the correct value. Higher tiers also fill valid neighbors.

| Rarity | Effect |
|--------|--------|
| Normal | Fill 1 correct cell |
| Rare | Fill 1 correct cell + 1 valid neighbor |
| Epic | Fill 1 correct cell + 2 valid neighbors |

---

### Finder (Tiered)

Use item, then click a filled cell to highlight all cells containing the same number.

| Rarity | Effect |
|--------|--------|
| Normal | Highlight 1 matching cell |
| Rare | Highlight 3 matching cells |
| Epic | Highlight 2 matching cells (intentionally lower — balance note: Epic Finder trades breadth for guaranteed accuracy on high-value targets) |

---

### Ink Well (Resource — Tiered)

Restores Pencil charges.

| Rarity | Effect |
|--------|--------|
| Normal | +3 Pencil |
| Rare | +6 Pencil |
| Epic | +10 Pencil |

---

### Meditation Stone (Resource — Tiered)

Restores HP.

| Rarity | Effect |
|--------|--------|
| Normal | +1 HP |
| Rare | +2 HP |
| Epic | +3 HP |

---

### Wind Chime (Resource — Tiered)

Refunds the last mistake (must be used within 3 moves of the error).

| Rarity | Effect |
|--------|--------|
| Normal | Undo 1 wrong input |
| Rare | Undo 1 wrong input + restore 1 HP |
| Epic | Undo 1 wrong input + restore 1 HP + reveal 1 correct cell |

---

### Pattern Scroll (Information — Tiered)

Highlights constraint conflicts on the board.

| Rarity | Effect |
|--------|--------|
| Normal | Highlight conflicts in 1 zone (row, column, or box) |
| Rare | Highlight conflicts in 2 zones |
| Epic | Highlight full conflict web (all connected zones) |

---

### Koi Reflection (Information — Tiered)

Reveals candidates for cells without consuming Pencil charges.

| Rarity | Effect |
|--------|--------|
| Normal | Reveal candidates for 1 cell |
| Rare | Reveal candidates for 2 cells |
| Epic | Reveal candidates for 3 cells |

---

### Lantern of Clarity (Information — Tiered)

Temporarily disables Fog of War modifier (only effective when Fog of War is active).

| Rarity | Effect |
|--------|--------|
| Normal | Disable fog for 3 moves |
| Rare | Disable fog for 6 moves |
| Epic | Disable fog for 10 moves |

---

### Unique Items (Non-Tiered)

These items exist at a single fixed rarity.

| Item | Rarity | Effect | Visual Feedback |
|------|--------|--------|----------------|
| Garden Rake | Normal | Highlights cells with only 2 candidates in the current row/column | Soft yellow highlight on eligible cells |
| Offering Bowl | Normal | Spend 5 HP to reveal the correct number for one cell | Cell reveals with a flash |
| Pruning Shears | Normal | Removes 1 impossible candidate from a 3×3 box | Candidate fades out |
| Zen Sand Sifter | Normal | Highlights Hidden Pairs in the current row | Pale blue highlight on paired cells |
| Ginkgo Leaf | Rare | Highlights all instances and constraints of a chosen number until placed | Golden glow on matching cells |
| Rice Paper Umbrella | Rare | Protects HP from the next 2 mistakes | Shield icon appears on HP bar |
| Temple Incense | Rare | Cells that are correct for a specific number pulse for 5 moves | White pulse (inner glow) on correct cells |
| Koi Dragon Scale | Epic | Instantly completes the most-filled line or box | Cells fill with a wave animation |
| Golden Kintsugi Jar | Epic | Highlights all current mistakes on the board in red (no HP penalty) | Red highlight on incorrect cells |
| Silk Fan | Epic | Swap the positions of two placed numbers | Cells swap with a slide animation |

---

### Visual Feedback Layering

When multiple items or relics are active simultaneously, each uses a distinct visual channel to prevent confusion:

| Effect Source | Visual Channel |
|--------------|---------------|
| Garden Rake | Soft yellow highlight |
| Zen Sand Sifter | Pale blue highlight |
| Temple Incense | White pulse (inner glow) |
| Solver / Finder | Brief cell flash (white → fade) |
| Pattern Scroll | Red conflict markers |
| Lantern of Clarity | Fog boundary glow (amber) |
| Relic passive effects | Tier-based border glow (see Relic Visual Feedback) |

---

## Item Power Budget

| Budget Tier | Typical Effect Value |
|:---:|----------------------|
| Low | +1 HP equivalent, reveal 1 cell |
| Medium | +2 HP equivalent, reveal 2–3 candidates |
| High | +3 HP equivalent, multi-cell strategic swing |

---

## Relic System

### Acquisition

Relics are acquired through:
- **Relic tile nodes** on the garden path — 3-of-3 choice panel (see Relic Node Reward Flow)
- **Elite tile rewards** (chance to drop a relic)
- **Run events** (event choices may grant relics)

Relics are **not available in the Shop**. The shop sells consumable items only.

### Single Relic Slot `[RELIC-SLOT-001]` `[RELIC-SLOT-002]`

The player can hold **one relic at a time**. When a new relic is offered while already holding one, a **choice screen** appears showing both relics side by side. The player must choose which relic to keep — the other is discarded permanently. This creates meaningful decision points throughout the run.

**Save data note:** `RunState` uses a `HeldRelics: List<RelicInstance>` field for serialization alongside the legacy `HasRelic / HeldRelic` fields. `RELIC-SLOT-002` — on resume, if `HasRelic=true` but `HeldRelics` is empty, the save is migrated automatically by `RestoreTransientState` (`RELIC-SLOT-003`).

### Relic Tiers

| Tier | Power Level | Base Price | Acquisition |
|------|-------------|:---:|-------------|
| Tier 1 | Low — minor passive | — | Common relic nodes, early floors |
| Tier 2 | Medium — noticeable effect | — | Mid-run relic nodes, elite rewards |
| Tier 3 | High — build-enabling | — | Late-run relic nodes, difficult elites |
| Tier 4 | Very high — run-defining | — | Special events, late floors |
| Legendary | Exceptional — unique named relics | — | Very rare elite rewards or high-floor events |

Shop prices scale with floor: `RelicShopPrice = BasePrice × (1 + Floor_Index × 0.5)`

### Relic Drop Rates

#### Relic Node Tile — Tier Weights by Floor `[RELIC-ROLL-001]`

Relic nodes always grant exactly one relic. The tier is rolled from the following weight table:

| Floor | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Legendary |
|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | 60% | 30% | 10% | 0% | 0% |
| 2 | 40% | 35% | 20% | 5% | 0% |
| 3 | 20% | 35% | 30% | 13% | 2% |
| 4 | 10% | 25% | 35% | 25% | 5% |
| 5 | 5% | 15% | 35% | 35% | 10% |

**Risk route bonus:** When a relic node is on the Risk route, shift one weight tier upward (e.g. Floor 2 Risk route uses Floor 3 weights).

#### Elite Tile — Relic Drop Chance

| Floor | Drop Chance |
|:---:|:---:|
| 1 | 15% |
| 2 | 20% |
| 3 | 25% |
| 4 | 30% |
| 5 | 40% |

When an elite drops a relic, the tier is rolled using the same floor weight table above.

### Stacking Rules

- **No duplicate relics** — each relic can only appear once per run (unless explicitly tagged stackable)
- Multiplicative relic stacking is **capped** to prevent runaway power scaling

### Complete Relic Table

> **Review status:** Proposed — all relics marked ⚠️ are new proposals pending balance review.

#### Tier 1 — Minor Passives

| Relic | Type | Effect | Status |
|-------|------|--------|--------|
| Smooth Pebble | Passive | +5 Pencil at the start of each puzzle | ✅ Approved |
| Cracked Teacup | Passive | First wrong placement each puzzle costs 0 HP | ✅ Approved |
| Wooden Comb | Passive | Nothing-slot gold bonus doubled | ✅ Approved |
| Moss Token | Passive | Shop reroll costs reduced by 25% | ✅ Approved |

#### Tier 2 — Noticeable Effects

| Relic | Type | Effect | Status |
|-------|------|--------|--------|
| Koi Reflection | Passive | +1 combo grace (combo doesn't break on first pause) | ✅ Existing |
| Monk Charm | Passive | Streak bonus: every 5 correct placements in a row grants +2 Gold | ✅ Existing |
| Copper Tortoise | Passive | +5 Gold bonus when completing a puzzle with no mistakes | ✅ Approved |
| Jade Hairpin | Active (1 use/puzzle) | Reveal all candidates in one row for free (no pencil cost) | ✅ Approved |
| Stone Sundial | Passive | +1 reward slot on puzzle completion | ✅ Approved |

#### Tier 3 — Build-Enabling

| Relic | Type | Effect | Status |
|-------|------|--------|--------|
| Sakura Seal | Passive | After completing a puzzle perfectly (no mistakes), the next puzzle starts with +3 HP | ✅ Approved |
| Crimson Fan | Passive | Boss modifier intensity reduced by one step (e.g. High → Medium) | ✅ Approved |
| Wisteria Branch | Passive | +2 HP at the start of each floor | ✅ Approved |
| Paper Crane | Active (2 uses/run) | Skip one puzzle tile on the path (move to the next tile without solving) | ✅ Approved |
| Porcelain Mask | Passive | Elite tiles drop items one rarity tier higher | ✅ Approved |

#### Tier 4 — Run-Defining

| Relic | Type | Effect | Status |
|-------|------|--------|--------|
| Transmuted Sigil | Passive | +1 Max HP, +3 Gold per puzzle completed | ✅ Existing |
| Phoenix Feather | Active (1 use/run) | When HP reaches 0, prevent death: fully restore HP and Pencil. Consumed after trigger. | ✅ Approved |
| Spirit Lantern | Passive | All shop items cost 20% less gold | ✅ Approved |
| Moonstone Compass | Passive | Relic node tiles always offer one tier higher than floor default | ✅ Approved |

#### Legendary — Exceptional Named Relics

| Relic | Type | Effect | Status |
|-------|------|--------|--------|
| Golden Root | Passive | Gold earned carries 50% interest between stages (e.g. 100g → +50g bonus next stage) | ✅ Existing |
| Silent Grid | Passive | +2 mistake shield charges per puzzle (first 2 wrong placements cost 0 HP) | ✅ Existing |
| Shifting Garden | Passive | Path variance +: extra branching tile added to each floor, opening alternate routes | ✅ Existing |
| Eternal Lotus | Passive | All items are never consumed — each item has infinite uses for the rest of the run | ✅ Approved |
| Dragon's Eye | Active (1 use/floor) | Reveal the full solution for 3 seconds, then hide it. Player must memorise. | ✅ Approved |

### Relic Visual Feedback

Each relic tier has a distinct **border glow** applied to the relic icon in the HUD and a **subtle board tint** while active, so the player always knows a relic is working:

| Tier | HUD Icon Border | Board Indicator |
|------|----------------|-----------------|
| Tier 1 | Thin silver outline | None — effect too minor for board overlay |
| Tier 2 | Soft gold outline | Faint gold shimmer on affected zone (row/col/box) when triggered |
| Tier 3 | Pulsing gold outline | Subtle ambient glow on board edges (warm amber) |
| Tier 4 | Bright animated gold border | Continuous soft particle effect near board corners |
| Legendary | Animated rainbow / prismatic border | Distinct unique visual per legendary relic (see below) |

The visual intensity scales with tier so the player can gauge relic power at a glance without reading text.

#### Legendary Relic — Unique Visual Effects

| Relic | HUD Effect | Board Effect |
|-------|-----------|-------------|
| Golden Root | Slow-spinning gold coin icon with metallic sheen | Tiny gold coin particles drift upward from completed cells |
| Silent Grid | Translucent blue shield overlay pulses on icon | Faint hexagonal shield pattern appears briefly when a mistake is absorbed |
| Shifting Garden | Icon subtly shifts/morphs between two states | Garden path edges shimmer with green-pink gradient when extra branch is active |
| Eternal Lotus | Lotus petals slowly open and close on icon | Soft pink petal particles float from items when used (indicating infinite use) |
| Dragon's Eye | Vertical slit pupil icon that blinks slowly | Board briefly flashes with a golden X-ray overlay when activated, fading to afterimage |

---

## Relic Node Reward Flow

When the player reaches a **Relic tile node** on the garden path, a **3-of-3 choice panel** is presented:

1. `RelicService.RollRelicChoices(floorIndex, count=3)` generates **three distinct relics** (no duplicates). Tier is drawn from the floor weight table with Risk route bonus applied if applicable.
2. The panel shows all three relics side by side — each card displays name, tier, and effect description.
3. The player picks **exactly one** relic, or presses **"Leave"** to skip all choices.
4. **If picking while already holding a relic:** a secondary swap panel appears:
   - **Left:** Currently held relic (name, tier, effect description)
   - **Right:** Chosen new relic
   - Player confirms keep/swap. The other is discarded.
5. Discovery is recorded only for the relic actually taken (not all three shown).
6. The path overview resumes after the panel closes.

**Implementation:** `RunDirector.RollRelicChoices()` delegates to `RelicService.RollRelicChoices()`; `RunDirector.AcceptRelicChoice(int index)` applies the chosen relic and handles swap logic. UI rendered in `PrototypeRunScreenController.ShowRelicChoicePanel()`.

---

## Run Event Relic Interactions

Run events can interact with the relic inventory:
- **Trade events:** Lose current relic → gain +1 max HP permanently
- **Transmutation events:** Replace current relic with a higher-tier variant (e.g. Transmuted Sigil)

---

## Item & Relic Codex

The game maintains a persistent **Codex** that tracks all items and relics the player has encountered across all runs. The codex is accessible from the main menu.

### Discovery Rules

- An entry is marked as **Discovered** on **first acquisition** (picking up the item or relic during a run)
- Seeing an item in a shop or reward screen without taking it does **not** count as discovery
- The codex tracks items and relics in a **single unified list**, sorted by type (Items first, then Relics) and rarity

### Codex Entry Display

Each entry shows:
- Item/Relic name
- Type label ("Item" or "Relic")
- Rarity tier
- Effect description
- Synergy tags
- Discovered status (undiscovered entries show as "???" with silhouette icon)

### Reed Duelist Unlock

The Reed Duelist class unlock condition ("find every unique item at least once") checks the codex — all item entries (not relics) must be marked Discovered.

---

## Synergy Tags

Items and relics share a unified synergy taxonomy for archetype classification:

| Synergy Tag | Description |
|------------|-------------|
| Information | Reveals hidden board state (candidates, mistakes, correct cells) |
| Recovery | Restores HP, grants shields, reduces mistake cost |
| Tempo | Speeds up puzzle solving, grants bonus actions |
| Constraint-control | Interacts with modifiers, reduces constraint difficulty |
| Risk-conversion | Converts risk into reward (gold, XP, power) |

The `RunArchetypeService` uses relic effects and item usage patterns to classify the player's run archetype (e.g. EconomyHoarder, SurvivalTank, ComboFlowMaster, ChaosMonk).

---

## Implementation Reference

### Key Files

| File | Responsibility |
|------|---------------|
| `Assets/Scripts/Economy/ShopService.cs` | Shop inventory generation, pricing, relic ID generation |
| `Assets/Scripts/Economy/RelicService.cs` | Relic acquisition, effect application, replacement choice |
| `Assets/Scripts/Run/RunArchetypeService.cs` | Run archetype scoring from relic and item usage |
| `Assets/Scripts/Run/RunEventService.cs` | Event-based relic trades and transmutation |
| `Assets/Scripts/Run/MidRunAdaptationService.cs` | Mid-run relic adaptation and transmutation |
| `Assets/Scripts/Core/GameEnums.cs` | `RelicTier` enum |
| `Assets/Scripts/Core/RuntimeModels.cs` | `RunState.RelicIds`, `ShopOffer.RelicId` |
| `Assets/Scripts/UI/ItemsMenuController.cs` | Codex (items + relics), item activation UI |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | In-run relic description display, relic node rewards |

### Data Model

```
RunState:
    List<string> RelicIds          // currently held relic (max 1 entry)
    int          ItemSlots         // class-dependent capacity
    List<string> HeldItems         // current consumable inventory

ShopOffer:
    string RelicId                 // null if item offer, set if relic offer
    int    Price

RelicTier enum: Tier1, Tier2, Tier3, Tier4, Legendary
```

---

## Art Prompts — Tiered Items (16×16 Pixel Art)

All prompts target a **16×16 pixel art** icon in Japanese garden aesthetic. Rarity is conveyed by colour temperature and detail level.

### Solver

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, single white quill pen on pale grey stone tablet, Japanese zen style, silver border, clean minimal` |
| Rare | `16x16 pixel art, golden quill pen with small sparkle, writing on warm parchment scroll, Japanese zen style, gold border` |
| Epic | `16x16 pixel art, glowing cyan quill pen trailing light, three small cells filling beneath it, Japanese zen style, cyan glow border` |

### Finder

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, simple magnifying glass with grey lens, single highlighted dot visible, Japanese zen style, silver border` |
| Rare | `16x16 pixel art, ornate gold magnifying glass, three glowing dots visible through lens, Japanese zen style, gold border` |
| Epic | `16x16 pixel art, crystal magnifying glass with purple glow, precise crosshair overlay, two bright target dots, Japanese zen style, purple glow border` |

### Ink Well

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, small ceramic ink pot with dark ink, single brush resting beside it, Japanese calligraphy style, silver border` |
| Rare | `16x16 pixel art, ornate gold-rimmed ink pot overflowing slightly, warm amber ink, Japanese calligraphy style, gold border` |
| Epic | `16x16 pixel art, glowing jade ink well with swirling luminous ink, magical wisps rising, Japanese calligraphy style, cyan glow border` |

### Meditation Stone

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, smooth grey river stone with faint heart symbol etched, zen garden setting, silver border` |
| Rare | `16x16 pixel art, polished rose quartz stone glowing softly warm pink, small plus symbol, zen garden style, gold border` |
| Epic | `16x16 pixel art, radiant white jade stone with golden heart aura, healing particles rising, zen garden style, cyan glow border` |

### Wind Chime

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, simple bamboo wind chime with single metal tube, gentle motion lines, Japanese garden style, silver border` |
| Rare | `16x16 pixel art, brass wind chime with three tubes and red tassel, small sparkle, Japanese garden style, gold border` |
| Epic | `16x16 pixel art, crystal wind chime with five glowing tubes, time-rewind spiral symbol, Japanese garden style, cyan glow border` |

### Pattern Scroll

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, partially unrolled paper scroll with single red line pattern, Japanese calligraphy style, silver border` |
| Rare | `16x16 pixel art, gold-clasped scroll showing intersecting red and blue grid lines, Japanese calligraphy style, gold border` |
| Epic | `16x16 pixel art, ancient glowing scroll fully unrolled, complex web of luminous constraint lines, Japanese calligraphy style, purple glow border` |

### Koi Reflection (Item)

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, small pond with single koi fish reflection and one ripple, Japanese garden style, silver border` |
| Rare | `16x16 pixel art, ornate koi pond with two fish and candidate numbers floating on water surface, gold border` |
| Epic | `16x16 pixel art, mystical koi pond glowing teal, three luminous fish revealing hidden numbers, Japanese garden style, cyan glow border` |

### Lantern of Clarity

| Rarity | Prompt |
|--------|--------|
| Normal | `16x16 pixel art, small paper lantern with dim warm glow, fog wisps retreating, Japanese garden style, silver border` |
| Rare | `16x16 pixel art, bronze Japanese lantern with bright amber flame, fog clearing in circle, gold border` |
| Epic | `16x16 pixel art, radiant crystal lantern blazing white light, fog completely dispelled in wide radius, Japanese garden style, cyan glow border` |

---

## Art Prompts — Relics (16×16 Pixel Art)

### Tier 1

| Relic | Prompt |
|-------|--------|
| Smooth Pebble | `16x16 pixel art, small round grey river pebble with faint pencil mark etched, zen garden sand background, thin silver outline` |
| Cracked Teacup | `16x16 pixel art, small Japanese tea cup with single golden crack (kintsugi), warm brown tea inside, thin silver outline` |
| Wooden Comb | `16x16 pixel art, simple wooden hair comb with five teeth, small gold coin beside it, Japanese traditional style, thin silver outline` |
| Moss Token | `16x16 pixel art, round stone coin covered in soft green moss, subtle shop bag icon, Japanese garden style, thin silver outline` |

### Tier 2

| Relic | Prompt |
|-------|--------|
| Koi Reflection | `16x16 pixel art, golden koi fish circling in calm water, faint combo counter symbol, Japanese art style, soft gold outline` |
| Monk Charm | `16x16 pixel art, small prayer bead bracelet with wooden beads and red thread, streak counter symbol, Japanese temple style, soft gold outline` |
| Copper Tortoise | `16x16 pixel art, small copper tortoise figurine with gold coin on shell, warm patina green, Japanese garden style, soft gold outline` |
| Jade Hairpin | `16x16 pixel art, ornate jade hairpin with small dangling pearl, faint row of numbers, Japanese geisha style, soft gold outline` |
| Stone Sundial | `16x16 pixel art, small stone sundial with shadow and plus symbol, warm sunlight, Japanese garden style, soft gold outline` |

### Tier 3

| Relic | Prompt |
|-------|--------|
| Sakura Seal | `16x16 pixel art, round wax seal with sakura flower imprint, soft pink glow with heart symbol, Japanese stationery style, pulsing gold outline` |
| Crimson Fan | `16x16 pixel art, partially open red fan with boss skull fading, Japanese war fan style, pulsing gold outline` |
| Wisteria Branch | `16x16 pixel art, purple wisteria branch with hanging flowers and small heart, soft ambient glow, pulsing gold outline` |
| Paper Crane | `16x16 pixel art, origami paper crane in flight with skip arrow below, soft white paper, Japanese style, pulsing gold outline` |
| Porcelain Mask | `16x16 pixel art, white porcelain Noh mask with one gold eye, small up-arrow for rarity, Japanese theater style, pulsing gold outline` |

### Tier 4

| Relic | Prompt |
|-------|--------|
| Transmuted Sigil | `16x16 pixel art, glowing alchemical sigil on dark stone, golden plus symbol and coin, mystical Japanese style, bright animated gold border` |
| Phoenix Feather | `16x16 pixel art, single burning orange-red feather with revival sparkle, rising from ashes, Japanese mythological style, bright animated gold border` |
| Spirit Lantern | `16x16 pixel art, floating ghostly paper lantern with blue-white flame, discount tag symbol, Japanese spirit festival style, bright animated gold border` |
| Moonstone Compass | `16x16 pixel art, glowing white moonstone set in compass housing, needle pointing upward (tier up), Japanese celestial style, bright animated gold border` |

### Legendary

| Relic | Prompt |
|-------|--------|
| Golden Root | `16x16 pixel art, twisted golden tree root growing from pile of coins, warm metallic glow, interest symbol, Japanese bonsai style, animated rainbow border` |
| Silent Grid | `16x16 pixel art, translucent blue-white shield with hexagonal grid pattern, two absorbed red marks, Japanese mystical style, animated rainbow border` |
| Shifting Garden | `16x16 pixel art, morphing garden path splitting into multiple branches, green-pink gradient shimmer, Japanese garden style, animated rainbow border` |
| Eternal Lotus | `16x16 pixel art, radiant white lotus flower with infinite symbol (∞) in center, soft pink petals glowing, Japanese sacred style, animated rainbow border` |
| Dragon's Eye | `16x16 pixel art, vertical dragon eye with golden slit pupil, brief flash of revealed grid behind, Japanese dragon art style, animated rainbow border` |

---

## Open Issues

### OPEN-A — Relic Effect Interaction Matrix
**Status:** Unresolved
**Description:** With 20 relics in the pool, edge-case interactions need to be mapped. Example: does Phoenix Feather trigger before or after Silent Grid's shield charges? Does Cracked Teacup's first-mistake-free stack with Silent Grid's shields?
**Blocker for:** `RelicService` effect resolution order, bug prevention.


---

---

## Requirements Traceability

| REQ-ID | Section | Code File | Status |
|--------|---------|-----------|--------|
| `ITEM-SLOT-001` | Item Slot System | `Assets/Scripts/Items/ItemService.cs` · `GetSlotCount` | ✅ |
| `ITEM-SLOT-002` | Nothing Slot Probability | `Assets/Scripts/Items/ItemService.cs` · `GetNothingChance` | ✅ |
| `ITEM-EPIC-001` | Epic Item Availability | `Assets/Scripts/Items/ItemService.cs` · `RollItemRarity` | ✅ |
| `RELIC-SLOT-001` | Single Relic Slot | `Assets/Scripts/Core/RuntimeModels.cs` · `RunState.HasRelic` | ✅ |
| `RELIC-ROLL-001` | Relic Node Tier Weights | `Assets/Scripts/Economy/RelicService.cs` · `RollRelic` | ✅ |
| `SHOP-PRICE-001` | Item Economy / Price Scaling | `Assets/Scripts/Items/ItemService.cs` · `GetShopPrice` | ✅ |
| `RELIC-SLOT-002` | Single Relic Slot — `HeldRelics` list | `Assets/Scripts/Core/RuntimeModels.cs` · `RunState.HeldRelics` | ✅ |
| `RELIC-SLOT-003` | Single Relic Slot — legacy migration on resume | `Assets/Scripts/Save/RunResumeService.cs` · `RestoreTransientState` | ✅ |
