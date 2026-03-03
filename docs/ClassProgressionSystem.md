# Class Progression System (Implemented)

This document maps the implemented class unlock architecture to the design requirement.

## Core Philosophy

- Start with one unlocked class only: `NumberFreak`
- New classes unlock from milestone achievements
- Unlock order follows skill/difficulty curve
- Locked classes are shown in class select with requirement text and hidden passive (`???`)

## Unlock Roadmap

1. **Number Freak**
   - Default unlocked

2. **Garden Monk**
   - Clear a Tier 1/2 boss
   - Reach run count 3 (non-tutorial)

3. **Shrine Archivist**
   - Clear Tier 3+ boss
   - Solve an 8x8 4★ board

4. **Koi Gambler**
   - Win with less than 3 HP remaining
   - OR complete Koi Path route

5. **Stone Gardener**
   - Defeat Tier 4+ boss
   - Reach Heat Score >= 5.0

6. **Lantern Seer**
   - Clear German Whispers boss
   - Clear multi-stage boss

7. **Chaos Monk** (Secret)
   - Unlock hidden dual-modifier boss pathway (via broad modifier mastery)
   - Clear dual-modifier boss with exactly 1 HP remaining

## Skill Tier Metadata

- Tier 1: Number Freak (Low / Beginner)
- Tier 2: Garden Monk (Low / Early)
- Tier 3: Shrine Archivist (Medium / Intermediate)
- Tier 4: Koi Gambler (Medium / Adaptive)
- Tier 5: Stone Gardener (High / Advanced)
- Tier 6: Lantern Seer (High / Expert)
- Tier 7: Chaos Monk (High / Expert)

## Main Class Select Logic

Implemented card behavior:
- Unlocked: full data visible (stats + passive)
- Locked: greyed card + unlock requirement + passive hidden as `???`

## Technical Mapping

- Unlock evaluator: `Assets/Scripts/Classes/ClassUnlockService.cs`
- Class-select card model: `Assets/Scripts/Classes/ClassSelectService.cs`
- Class metadata (tier/complexity/passive): `Assets/Scripts/Classes/ClassCatalog.cs`
- Milestone payload in run result: `Assets/Scripts/Core/RuntimeModels.cs`
- Profile progression persistence + unlock trigger on run record: `Assets/Scripts/Save/ProfileService.cs`
- Run-side milestone hooks and lock enforcement: `Assets/Scripts/Run/RunDirector.cs`

## Safeguards

- `RunDirector.StartRun(...)` blocks advanced class starts unless unlocked in meta progression.
- Tutorial runs are excluded from class progression unlock tracking.
- Secret unlock integration:
   - `MetaProgressionState.HiddenDualModifierBossUnlocked`
   - `MetaProgressionState.ChaosMonkUnlocked`
   - Run-result condition: dual-modifier clear with 1 HP left
