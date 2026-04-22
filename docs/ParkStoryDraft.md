# Park Story Draft

**Version:** 1.0 | **Date:** 2026-04-09 | **Status:** implemented

---

## Concept

The player is a person who takes a walk through an old city park. The park is a place they return to often — but each walk is different. The sudoku grids represent the quiet mental puzzles they churn through as they stroll: solving little problems, focusing their thoughts, making sense of the world.

The run *is* the walk. The goal is to reach the park's far exit and sit on the final bench — peace earned through patience and perseverance.

---

## World Map — Node Theming

| Node Type | Park Metaphor | Visual |
|-----------|--------------|--------|
| **Normal Puzzle** | A path segment — you keep moving | Winding park path |
| **Elite Puzzle** | A steep hillside or overgrown shortcut | Tangled trail fork |
| **Boss** | A noisy event in the park (see below) | Animated disturbance marker |
| **Shop** | Coffee & ice cream stand | Cart with umbrella |
| **Rest** | A park bench in the shade | Wooden bench + tree |
| **Event** | An encounter with someone or something | Speech bubble / figure |
| **Boss Gate** | You hear the noise ahead; choose your path | Gate post with two signs |
| **Relic Node** | A trinket on the path — someone dropped it | Glint in grass |
| **Run Start** | You enter the park through the iron gate | Park entrance sign |
| **Run End** | The far bench at the edge of the park | Bench at sunset |

---

## Floor Themes

| Floor | Park Zone | Atmosphere |
|-------|-----------|------------|
| 1 | The Entrance Garden | Sunny, open lawns, first sights and sounds |
| 2 | The Pond Path | Ducks, reflections, a quiet bridge |
| 3 | The Old Tree Grove | Dense shade, roots underfoot, birdsong |
| 4 | The Fountain Plaza | Busier, pigeons, fountains, more people |
| 5 | The Far End | Quieter again — benches, last light, the gate out |

---

## Boss Encounters — Noisy Park Events

Bosses are intrusions on your quiet walk — you can't avoid them, but you can choose *how* to pass through.

| Floor | Boss Name | Scene | Modifier Flavour |
|-------|-----------|-------|-----------------|
| 1 | **The Playground** | Children chasing each other, screaming joyfully | Simple chaos — FogOfWar or EvenOdd |
| 2 | **The Wedding Party** | A large group blocking the path, music, confetti | Multiple overlapping constraints |
| 3 | **The Street Market** | Vendors calling, carts blocking the path, music | Palindromes, Thermos — structural rules |
| 4 | **The Protest March** | Chanting crowd moves through the plaza | Row/ColWipe debuffs, high intensity |
| 5 | **The Festival Stage** | A band plays loudly at the final plaza — the big final boss | All modifiers, VeryHigh intensity |

**Pre-boss dialog** — shown when approaching the boss gate:

> *You hear it before you see it — [Boss Name]. You could loop around the quieter side...*
>
> *(Left sign: "Take the scenic route" — less modifiers)*  
> *(Right sign: "Push straight through" — more modifiers, better reward)*

---

## Rest Node — The Bench

When the player reaches a bench, three options appear:

> *You sit down for a moment. The park hums quietly.*
>
> — **Breathe** → +N HP (restore based on MaxHP/3)  
> — **Fill your flask** → +4 pencil marks  
> — **Note something down** → +1 Reroll Token  
> — **Clear your head** *(only if curses active)* → Remove oldest curse

Bench flavor lines (rotate per floor):

- Floor 1: *"The morning air is cool. A pigeon watches you from a fence post."*
- Floor 2: *"The pond catches the light. Somewhere, a duck argues about nothing."*
- Floor 3: *"Old roots have lifted the path into small ridges. You rest your feet."*
- Floor 4: *"The fountain masks the noise from the plaza. You almost relax."*
- Floor 5: *"Almost there. The bench is worn smooth from years of use."*

---

## Shop Node — The Coffee & Ice Cream Cart

> *A cheerful vendor with a striped awning. The coffee smells like the right thing.*

The cart offers:
- 3 items (normal shop stock)
- Optional relic (if floor qualifies)
- An emergency heal: *"Borrow a biscuit"* — buy 1 HP for escalating gold

Flavor label on emergency heal button:
- *"One biscuit (just HP, no shame)"*

Shop entry dialog (rotates):
- *"What can I get you? I've got something for every occasion."*
- *"Busy day? I've seen a lot of people come through. You look like you need this."*
- *"Special today: whatever helps most."*

---

## Event Encounters — People in the Park

Events are chance encounters on the path. The player briefly interacts with someone and makes a choice.

### Encounter Framing

Each event opens with 1–2 lines of scene-setting, then shows the choices. No character names — they are anonymous park-goers.

| Event | Who You Meet |
|-------|-------------|
| Wishing Fountain | Nobody — just the fountain |
| Ice Cream Cart | The cart vendor |
| Spilled Ink Bottle | A sketching artist who dropped everything |
| Lost Wallet | Nobody — it's on the ground |
| Friendly Stray | A dog without a collar |
| Street Musician | A busker with a guitar case |
| Maintenance Shed | Nobody — it's just open |
| Rare Flower Patch | Nobody — just the flowers |
| The Ink Peddler | A shady figure under a tree |
| Quiet Bench *(bonus)* | Nobody — just the bench |

---

## Curse Events — Themed as Temptations

Curses should feel like bad decisions you walked into — not random punishment.

> **The Ink Peddler:**  
> *A wiry figure stands under a chestnut tree. "I'll give you 60 for that flask of ink. Special price, today only."*  
> — *"Deal"* → –8 Pencil, +60 gold, gain **Hollow Pencil** curse  
> — *"No thanks"* → Nothing

> **The Tempting Shrine** *(Floor 3+)*:  
> *A small stone shrine sits off the path. Moss-covered and old. Something about it feels like a bargain.*  
> — *"Leave an offering and ask for strength"* → +3 HP, gain **Weight of Stone** curse  
> — *"Just look and leave"* → Nothing

---

## Run Start & End Dialog

### Entering the Park

Shown once when a new run begins:

> *The iron gate creaks. The park opens up ahead — paths branching, benches waiting, the sound of leaves.*
>
> *You know this park. You've been here before. But every walk is different.*

Class-specific variant (example for GardenMonk):
> *You always start at the same spot by the entrance. The bench with the carved initials. You breathe in.*

### Reaching the End

On run completion (victory):
> *The far gate is ahead. The park stretches behind you — every path you took, every pause, every noise you pushed through.*
>
> *You made it through.*

On run failure (game over):
> *You sit down. Not on a bench — just on a low wall by the path. The park continues without you.*
>
> *Next time, maybe.*

---

## Implementation Notes

- All dialog is stored as `string[]` in `ParkNarrativeService.cs` — keyed by node type + floor index
- UI displays dialog as a brief 2–3 line text block at the top of each node screen, fading out after 4 seconds or on any input
- Boss gate dialog replaces the standard "Choose your modifier" header
- No voiced audio in v1 — text only
- Curse events added to `RunEventService` pool with `MinFloor` gating
- Rest bench flavor lines rotated via `floor % lines.Length` seed

---

## Open Questions

- Should the park have a name? (Candidates: Mori Park, The Old Walk, Rinkaen Gardens)
- Does the player character have a name or remain anonymous?
- Should floor transitions have a brief "you cross the bridge / enter the grove" transition line?
