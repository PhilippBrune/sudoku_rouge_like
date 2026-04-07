# Path System & Garden Overview

**Version:** 1.4 | **Date:** 2026-04-07 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.4 | 2026-04-07 | implemented | Added Cursed node type (opt-in risk/reward encounter) and Positive Floor Effect system (one random floor-wide bonus per floor). Cursed nodes appear at low-medium weight on all routes. Player chooses Accept or Decline; Accept applies ×1.5 gold+XP multipliers and one extra random modifier. Four positive floor effects: Bounty (+40% gold), LuckyItems (+1 reward slot), PencilRefill (+2 pencil per puzzle), HealingPath (+1 HP per mistake-free puzzle). |
| 1.3 | 2026-03-29 | implemented | Per-route lane boxes (Slay the Spire 2 style): single PathBg replaced with two separate lane background boxes — CalmLane (cool floor-tinted, blue-shifted) and RiskLane (warm, orange-shifted). Each box is sized to tightly fit its route's nodes with 40 px padding. Lane labels "Calm Path" / "Risk Path" shown in top-left corner of each box. Start and Boss nodes remain outside the lane boxes. `DrawRouteLane()` helper added to `PrototypeRunScreenController`. |
| 1.2 | 2026-03-29 | implemented | Garden path visual rework: path nodes and edges are now drawn inside a floor-colored background box (PathBg). PathBg anchors 2–98% of the PathOverlay area; color = floorTint×0.28 at alpha 0.82 with a brighter floor-tinted Outline (effectColor = floorTint×0.85, alpha 0.70). Edge color brightened to floorTint×0.75. Fresh non-tutorial runs auto-start the first available puzzle node via ApplyResumeState (detects empty NodePath + no CurrentBoard, calls TryAdvanceToNodeAndStartPuzzle). |
| 1.1 | 2026-03-23 | implemented | Floor modifier system: floors 2–5 apply persistent modifiers to all puzzles. Boss choice pool excludes active floor modifiers. Updated Boss Encounter section and added Floor Modifier Activation subsection. Implemented via RollFloorModifiers, BuildEligiblePool, multi-select boss gate UI. |
| 1.0 | 2026-03-21 | implemented | Initial specification |

---

## Overview

The Garden Overview screen is the central navigation hub of a run. The player walks through a **Japanese garden** — each garden represents one **floor** of the run. There are **5 floors** in total. Each floor is a self-contained garden with its own visual theme, background art, and music.

Within each floor, the player navigates a branching path from a **shared start tile** through either the **Calm Route** or the **Risk Route**, ultimately arriving at a **shared Boss Gate tile**. Defeating the boss advances the player to the next floor.

---

## Screen Layout

The Garden Overview screen is divided into three main areas:

### Top — Meta HUD
Displays run-level information in a compact row:
- HP: current / max
- Gold: current
- Pencil: current / max
- Items: count
- Relics: count
- Active class passive (short description)
- Current Floor indicator (e.g. "Garden 2 / 5")

This mirrors the information shown in the current prototype path overview screenshot (HP, Gold, Pencil, Items, Relics, Passive).

### Center — Path Canvas
The main area. Contains the animated garden path graph for the current floor. Two route lanes branch out from the shared start and converge at the Boss Gate. The layout is organic and evocative of a hand-drawn garden map (see Path Layout Algorithm).

### Bottom — Save & Quit
A "Save & Quit" button (keyboard shortcut `Q`) is always available in the corner.

---

## Path Structure per Floor

Each floor graph contains:

### Shared Start Tile
- A single entry tile on the **left edge** of the screen.
- Both the Calm Route and the Risk Route begin from this node.
- The player icon spawns here at the start of each floor.
- The player must pick their first tile from the two first nodes of each route.

### Calm Route (Upper Lane)
- Longer path; length scales with floor number (see Path Generation Rules).
- Lower difficulty: smaller board sizes, lower star counts.
- May include more Rest, Relic, and Shop tiles.
- **Fully visible to the player from floor entry — no hidden tiles.**
- Visually on the **upper half** of the canvas (Y < 0.5).

### Risk Route (Lower Lane)
- Shorter path; length range scales with floor number (see Path Generation Rules).
- Higher difficulty: larger board sizes, higher star counts.
- Fewer safe tiles; more Puzzle and Elite tiles.
- **Fully visible to the player from floor entry — no hidden tiles.**
- Visually on the **lower half** of the canvas (Y > 0.5).

### Mid-Path Connection (Cross-Link Tiles)
- Each floor has exactly **one cross-connection** linking one tile on the Calm Route to one tile on the Risk Route.
- **Anchor selection:** Calm anchor = tile at index `floor(CalmLength / 2)`; Risk anchor = tile at index `floor(RiskLength / 2)`. The edge is drawn as a smooth bezier curve between these two positions regardless of any depth-index mismatch.
- **Cross-link tiles retain their original type.** A cross-link tile is still a Puzzle, Shop, Rest, or Relic tile — the player completes it normally. The `IsCrossLink` flag is set on the `RunNode` without overwriting `NodeType`. This means the tile has dual purpose: it functions as its type (e.g. Puzzle) *and* allows the player to switch lanes afterward.
- Cross-link tiles display a small **⇄ badge** in the top-left corner (gold/yellow, ~16 px) to indicate the lane-switch opportunity.
- Traversing the cross-edge moves the player to the other route from that point forward.
- Only one cross-connection per floor; the graph does not allow multiple crossings.
- The cross-edge is **only traversable when the player is standing on one of its two directly connected tiles**. Once the player moves past that tile the edge becomes unavailable.
- Visual: gold/yellow bezier curve, distinct from the orange route edges. Control points are offset toward canvas centre to produce a smooth inward arc.

### Shared Boss Gate Tile
- A single convergence tile on the **right edge** of the screen.
- Both routes lead here. The last tile on each route connects directly to the Boss Gate.
- Visual style: distinct warm/dark colour (maroon/brown), labelled "Boss Gate".
- Shows board size (top-left) and star difficulty (bottom-right).

---

## Player Icon

- The player icon represents the active class. **Each class will have its own sprite in a future pass.** Until class icons are implemented, the placeholder is a **clearly visible, high-contrast dot** (~24 px, bright white or gold filled circle). The dot must remain legible on all 5 floor backgrounds.
- `PlayerIconController` renders the dot procedurally (same technique as overlay circles). Swap for class sprite when class icons are produced.
- On floor entry the icon **enters from off-screen**, sliding in from the left edge of the canvas, and lands on the Shared Start Tile. This entry animation plays once per floor transition.
- When the player selects a tile, the icon animates along the connecting edge to land on the new tile (~0.4 s lerp/tween).
- The icon always rests on the last completed (or currently active) tile.
- The icon does not move during puzzle solving — only on the path overview screen after a tile choice.

### Tile and Edge Visibility States

All tiles and edges are always drawn. Their visual state updates dynamically every time the player moves:

| State | Appearance | Condition |
|-------|-----------|-----------|
| **Available** | Full colour, normal opacity | Reachable forward tile the player can choose next |
| **Visited** | Desaturated / dimmed | Already traversed by the player |
| **Unreachable** | Greyed out | Can no longer be reached from the current position |

**Reachability rules:**
- **Forward tiles are never greyed.** Only visited or no-longer-reachable tiles/edges are greyed.
- A tile is unreachable if all paths leading to it are either already passed or cut off by the player's current route.
- **At the cross-edge tile:** both forward branches (Calm continuation and Risk continuation) remain available and selectable simultaneously — neither is greyed. Once the player leaves the cross-edge tile in either direction, the unchosen forward branch becomes greyed.
- The cross-edge itself is greyed whenever the player is not standing on one of its two anchor tiles.

---

## Tile Types

All tiles show:
- **Top-left**: board size (e.g. `7x7`), where applicable.
- **Center**: tile type label and icon.
- **Bottom-right**: star difficulty (e.g. `3★`), where applicable.

### Puzzle
- Standard Sudoku encounter.
- Shows board size (top-left) and star rating (bottom-right).

### Relic
- Grants the player a random relic reward.
- No board size or star rating displayed.

### Rest
- Allows the player to recover HP or pencil charges.
- No board size or star rating displayed.

### Shop
- Spend gold on items or relics.
- No board size or star rating displayed.

### Elite (Pre-Boss)
- A harder puzzle encounter that always precedes the Boss Gate.
- Always the **last tile on each route** before the Boss Gate.
- Shows board size (top-left) and star rating (bottom-right).
- Labelled "ElitePuzzle" or "PreBoss".

### Boss Gate
- Triggers the Boss encounter sequence.
- Shows board size and star rating.
- Completing the Boss Gate puzzle advances the player to the next floor (or ends the run on Floor 5).

### Cursed
- An opt-in risk/reward node. The player is presented with an **Accept / Decline** panel before the puzzle loads.
- **Accept**: puzzle runs with `IsCursed = true` — applies **×1.5 gold multiplier** and **×1.5 XP multiplier**, and adds **one extra random modifier** (drawn from the 15-modifier pool, stacked on any existing floor modifiers).
- **Decline**: a standard puzzle of the same board size/star rating starts with no extra modifiers or multipliers.
- No board size or star rating is shown on the tile card before the choice is made (tile just shows the "Cursed" label and icon).
- Accepted cursed puzzles are tracked for the run score breakdown (`CursedPuzzlesAccepted`).
- **Roll weights** (in `RunGraphService`): early floors weight 2, mid floors 4–6, late floors 6–8, proportionally adjusted within the node-type pool.

---

## Positive Floor Effect

Each floor rolls **one random positive effect** (or None) when the player enters it. The effect applies to **all puzzles on that floor** — normal, elite, and boss.

| Effect | Description |
|--------|-------------|
| `None` | No bonus effect this floor |
| `Bounty` | All completed puzzles grant **+40% gold** |
| `LuckyItems` | All item reward screens get **+1 extra slot** |
| `PencilRefill` | Each completed puzzle restores **+2 Pencil** |
| `HealingPath` | Each mistake-free puzzle restores **+1 HP** |

- Effect is rolled seeded from `RunState.Seed + floorIndex` for determinism.
- Stored in `RunState.HasPositiveFloorEffect` + `ActivePositiveFloorEffect`.
- Shown on the path screen as a banner (e.g. `"★ Bounty Floor (+40% gold)"`), returned by `RunMapController.GetPositiveFloorEffectLabel()`.
- `None` has equal probability to each named effect (i.e. 20% each).

---

## Floor Modifier Activation

Starting from Floor 2, modifiers are applied to **all puzzles** on the floor (normal, elite, and boss). These are rolled automatically when the player enters the floor and persist until the floor is cleared.

| Floor | Floor Modifiers | Effect |
|:---:|:---:|--------|
| 1 | 0 | No floor modifiers — gentle introduction |
| 2 | 1 | One modifier active on all puzzles |
| 3 | 2 | Two modifiers on all puzzles |
| 4 | 3 | Three modifiers on all puzzles |
| 5 | 4 | Four modifiers on all puzzles |

Formula: Floor N has **N−1** floor modifiers.

- Rolled from the full pool of 15 with equal probability.
- Board size restrictions apply (German Whispers and Killer Cages require ≥ 7×7).
- Floor modifiers from previous floors do not exclude the current floor's pool.
- Stored in `RunState.ActiveFloorModifiers`; persisted in save data.
- A **floor modifier banner** is shown on floor entry listing the new modifiers.

---

## Boss Encounter: Modifier Choice

When the player reaches the Boss Gate, a **modifier selection screen** appears before the puzzle loads. Boss-chosen modifiers are **stacked on top of** the floor modifiers.

### Modifier Pool
- **All 15 modifiers are eligible at every floor** — there is no floor-based restriction.
- The random draw is uniform across the full pool, **excluding modifiers already active as floor modifiers** (no duplicates within a floor).
- Option count and required choice count scale with floor (see table below).

### Choice Scaling by Floor

| Floor | Boss Options Shown | Player Picks | Floor Mods + Boss Mods = Total Active |
|:---:|:---:|:---:|:---:|
| 1 | 3 | 2 | 0 + 2 = **2** |
| 2 | 3 | 2 | 1 + 2 = **3** |
| 3 | 4 | 3 | 2 + 3 = **5** |
| 4 | 5 | 4 | 3 + 4 = **7** |
| 5 | 6 | 5 | 4 + 5 = **9** |

Boss choice formula: Floor N offers **N+1** options (min 3), player picks **N** (min 2). All floor modifiers + boss-chosen modifiers are applied simultaneously to the boss puzzle.

### Unknown Modifiers
- If the player has **never encountered** a modifier in a previous run, its option card displays as **"???"** with the description hidden.
- Once a modifier has been played at least once, it shows its full name, rules, and colour on future runs.

### Option Card Layout
Each modifier option card shows:
- Modifier name (or "???" if unseen).
- Short rule description (or "???" if unseen).
- Visual colour swatch matching its in-puzzle overlay colour.
- A selection checkbox / highlight when picked.

The player must confirm their selection before the boss puzzle loads. **Once confirmed, the selection is locked — there is no review step or take-back.**

### Modifier Description Box (In-Puzzle)
- A **description box** is shown directly **under the "Mode" button** on the numpad panel (anchored at `(0.74, 0.08)–(0.97, 0.27)`).
- The box lists the name and short rule description for each active modifier (floor modifiers + boss modifiers if applicable).
- **Visibility**: the box is hidden (`SetActive(false)`) when no modifiers are active (Floor 1 non-boss puzzles). It is shown (`SetActive(true)`) whenever the current puzzle has one or more active modifiers — including floor-modified puzzles on Floors 2–5.
- **Update behaviour**: the description text refreshes on every call to `BuildOrRefreshSudokuBoard`, not just when overlays are first built. This ensures it updates correctly when a new puzzle loads with different modifiers.

### Boss Modifier Overlay Generation
- Some modifiers (e.g. RatioKropki, Thermo, ArrowSums) require specific numeric relationships in the board solution. The overlay generator may fail to place geometry if the solution doesn't support the constraint.
- **Retry strategy**: up to **5 board regenerations** × **12 seed retries** per board. If the first board's solution can't accommodate a modifier, the puzzle is regenerated with a new seed to produce a different solution. This ensures all chosen modifiers appear in the final puzzle.
- `HasAllModifiersPresent()` validates that every active modifier has at least one overlay element.

---

## Floor Themes

Each of the 5 floors has a unique Japanese garden theme reflected in the background art, ambient music, and visual palette of the path canvas. All background `Sprite` and `AudioClip` assets are defined per theme and referenced by `FloorThemeData` (floor index 0–4).

| Floor | Garden Theme | Visual Palette | Music Mood |
|-------|-------------|----------------|------------|
| 1 | Cherry Blossom Garden (Sakura-en) | Soft pinks, pale greens, white petals | Calm, airy; light shakuhachi flute |
| 2 | Bamboo Forest (Take-no-mori) | Deep greens, gold, cool shadows | Rhythmic; bamboo percussion, breeze |
| 3 | Zen Rock Garden (Karesansui) | Grey stone, white raked sand, moss | Sparse, meditative; low koto tones |
| 4 | Koi Pond Garden (Koi-no-niwa) | Teal water, orange fish, lily pads | Flowing, serene; gentle water sounds |
| 5 | Mountain Temple (Yama-no-shinden) | Dark stone, lantern glow, mist | Tense, sacred; taiko drums, reverb |

Tile node cards and path edges use a consistent dark-panel style (as seen in the reference screenshot) so they remain legible across all backgrounds.

---

## Floor Transition Sequence

On boss defeat the following sequence plays before the next floor loads:

1. **"Garden Cleared" card** (~2–3 s) — shows the defeated garden name and a short summary line (tiles completed, relics gained).
2. **Cutscene panel** — a single fullscreen static image previewing the next garden's theme. No animation is required in the first implementation pass.
3. **Next floor canvas fade-in** — the new garden's path appears; the player icon slides onto the Shared Start Tile from off-screen.

---

## Progression Flow

```
[Class Select]
     │
     ▼
[Floor 1 — Start Tile (left)]  ← player icon slides in from left edge
     │ player picks Calm (upper) or Risk (lower) first tile
     ▼
[Tile → Puzzle / Relic / Rest / Shop]  ← tiles progress left → right
     │ completing a tile returns to path overview; icon moves right
     │ cross-link tiles also function as their type (Puzzle/Shop/etc.)
     ▼
[Elite Tile (PreBoss)]
     ▼
[Boss Gate (right edge) → Modifier Selection → Boss Puzzle]
     │ boss defeated → "Garden Cleared" card → cutscene panel
     ▼
[Floor 2 — New Garden]  ← ... repeats for floors 3, 4, 5 ...
     ▼
[Floor 5 Boss defeated → Victory / End Screen]
```

---

## Path Generation Rules

### Length Scaling per Floor

Each successive garden is slightly longer. The Elite (PreBoss) tile is **counted within** the Calm Route length.

| Floor | Calm Route Length (incl. Elite) | Risk Route Length Range |
|-------|:---:|:---:|
| 1 | 5–8  | Calm − 1 only |
| 2 | 6–9  | (Calm − 2) to (Calm − 1) |
| 3 | 7–10 | (Calm − 2) to (Calm − 1) |
| 4 | 8–11 | (Calm − 3) to (Calm − 2) |
| 5 | 9–12 | (Calm − 3) to (Calm − 2) |

- Calm Route length is randomised each run within the stated range for that floor.
- Risk Route length is randomised each run within the stated range. Minimum Risk Route length is `3` (at least one non-Elite tile + Elite tile + edge to Boss Gate).
- Both routes always have exactly **one Elite tile as their last node** before the Boss Gate.
- Cross-connection anchor indices: `floor(CalmLength / 2)` on Calm, `floor(RiskLength / 2)` on Risk.
- Board sizes and star ratings per tile are assigned by `RunDirector` / `RunGraphService` based on floor number and route type.
- Risk Route tiles are always harder than the Calm equivalent at the same depth (higher star count or larger board size).

### Save Granularity
Auto-save fires after each tile is completed. No additional save on floor completion is needed. The existing `RunAutoSaveCoordinator` per-tile behaviour is correct and sufficient.

---

## Path Layout Algorithm

Tile node positions are computed once at graph generation time, stored per node, and never recalculated during a session. This prevents flicker and ensures stable layout on resume.

### Coordinate System
- All positions are normalised to `[0, 1]` canvas space and scaled to `RectTransform` pixel coordinates at render time — fully resolution-independent.
- **X axis = horizontal progress** (left → right). `X = 0` is the left edge (Start), `X = 1` is the right edge (Boss Gate).
- **Y axis = lane separation**. `Y = 0.5` is the vertical centre. Calm Route sits in the upper half (`Y < 0.5`); Risk Route sits in the lower half (`Y > 0.5`).
- Canvas Y is inverted when converting to RectTransform pixel coordinates (canvas Y 0 = screen top).

### Horizontal Slot Assignment
- Total slots = `max(CalmLength, RiskLength) + 2` (slot 0 = Start, last slot = Boss Gate, one slot per route tile).
- Slot width = `1.0 / totalSlots`.
- Calm tiles fill slots `1 … CalmLength` evenly left-to-right.
- Risk tiles are distributed to span the same overall horizontal range as Calm, spaced proportionally if Risk is shorter.
- The Elite tile on each route always occupies the slot immediately before the Boss Gate.
- Shared Start Tile: fixed at `(slotWidth × 0.5, 0.5)` — left edge, vertical centre.
- Boss Gate: fixed at `(1.0 − slotWidth × 0.5, 0.5)` — right edge, vertical centre.

### Vertical Position with Seeded Jitter
- Calm base Y: `0.5 − laneOffset` (upper lane); Risk base Y: `0.5 + laneOffset` (lower lane).
- Per-tile vertical jitter: `±jitterMax`, seeded from `RunState.Seed + floorIndex + tileIndex` for determinism across sessions and on resume.

| Floor | `laneOffset` | `jitterMax` |
|-------|:---:|:---:|
| 1–2 | 0.18 | 0.04 |
| 3   | 0.22 | 0.05 |
| 4–5 | 0.26 | 0.06 |

### Cross-Edge Bezier
- Drawn as a cubic bezier between the two anchor tile canvas positions.
- Both control points are offset toward canvas centre (`x = 0.5`) by `0.10` normalised units to produce a smooth inward arc.

### Implementation Notes
- `RunGraphService` computes and stores a normalised `Vector2 CanvasPosition` on each `PathNode`.
- `RunMapController` scales these to pixel coordinates at layout time.
- Layout is computed once on floor entry and never rebuilt mid-session.

---

## UI / Visual Design Notes

- Tile node cards use the existing design: dark green panel, white text, top-left size label, center type label, bottom-right star rating. **Do not change this tile design.**
- Cross-connection edge: gold/yellow bezier curve, distinct from the orange route edges. Cross-link tiles show a ⇄ badge in the top-left corner.
- Route labels ("Calm Route" / "Risk Route"): floating text at the start of each lane (upper / lower), styled in the garden's ambient font.
- Boss Gate node: warm dark maroon background.
- Visited / unreachable state rendering updates reactively on every player move (see Tile and Edge Visibility States).

---

## Files to Create / Modify (Implementation Reference)

| File | Change |
|------|--------|
| `Assets/Scripts/Run/RunGraphService.cs` | 5-floor graph generation, left-to-right layout (`X=progress, Y=lane`), cross-link sets `IsCrossLink` flag without overwriting `NodeType` |
| `Assets/Scripts/Run/RunDirector.cs` | Floor progression; `RebuildCurrentFloorGraph()`; boss modifier overlay retry loop (5 boards × 12 seeds); floor-based intensity scaling |
| `Assets/Scripts/UI/RunMapController.cs` | Render graph, player icon animation, reachability greying, bezier cross-edge; `IsCrossLink` for lane lock release |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | Cross-link badge rendering (⇄); `RefreshBossModifierDescription()` on every board build; removed CrossLink early-return in `ChoosePath` |
| `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs` | Boss description box repositioned under numpad Mode button `(0.74,0.08)–(0.97,0.27)` |
| `Assets/Scripts/Core/RuntimeModels.cs` | `IsCrossLink` on `RunNode`; `CurrentFloor`, `TotalFloors`, `ChosenBossModifiers` on `RunState` |
| `Assets/Scripts/Boss/BossService.cs` | Per-floor option/choice count; uniform pool draw across all 15 modifiers |
| `Assets/Scripts/Save/RunResumeService.cs` | Restore `CurrentFloor`, `TotalFloors`, `ChosenBossModifiers` on resume; `RebuildCurrentFloorGraph()` for floor > 0 |
| New: `Assets/Scripts/UI/PlayerIconController.cs` | Procedural dot placeholder; off-screen entry animation; edge-following tween |
| New: `Assets/Scripts/Data/FloorThemeData.cs` | Maps floor index to background `Sprite`, music `AudioClip`, palette |
