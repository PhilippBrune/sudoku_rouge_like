# Run Variation, Events, Curses, and Endgame Extensions

This document captures the newly integrated systems from the latest design extension.

## 1) Meaningful Run-to-Run Variation

### Run Archetypes
Implemented runtime archetype detection (`RunArchetypeService`) based on relic-category concentration and run-state pressure:

- Economy Build (`EconomyMerchantMonk`)
- Modifier Build (`ModifierRuleBender`)
- Survival Build (`SurvivalEnduringSage`)
- Combo Build (`ComboFlowMaster`)
- Secret archetype: `ChaosMonk`

### Relic Synergy Multipliers
Implemented in `RelicSynergyService`:

- 2 same-category relics => minor bonus
- 3 => major bonus
- 4+ => transformative bonus

Current gameplay effects include:
- Economy: global gold multiplier, interest carry support
- Survival: mistake shield charges
- Modifier: reduced effective modifier weight + reward multiplier
- Combo: first-mistake protection charge at high stack
- Chaos: controlled bonus scaling tied to entropy

### Legendary Run-Warping Relics
Shop now rolls `<5%` legendary relic IDs:
- `relic_legend_shifting_garden`
- `relic_legend<´¢£beginÔûüofÔûüsentence´¢£>_silent_grid`
- `relic_legend_golden_root`

Legendary hooks currently implemented:
- Shifting Garden => corrupted path behavior (+extra modifier pressure)
- Silent Grid => extra shield behavior (mistake cost mitigation)
- Golden Root => interest carry mechanic between nodes

## 2) Event System (Narrative-Lite)

Implemented in `RunEventService` with concise prompts + 2 choices per event.

### Categories
- Sacrifice
- Risk Amplification
- Resource Trade

### Design Properties
- Short text prompts
- Clear trade-off options
- No long dialogue trees
- Option resolution mutates run state directly

## 3) Curse Architecture

Implemented in `CurseService`.

### Curse Types
- `CursedRelicBacklash`
- `LockedItemSlot`
- `TemporaryBlindness`
- `IncreasedMistakePenalty`
- `MinorCurse`

### Scaling Behavior
- Curse stack increases heat multiplier
- Curse weight increases rare-event frequency
- Purification hook available (`TryPurifyRandomCurse`)

## 4) Difficulty Variance Within Runs

Implemented in `RunVarianceService` and integrated in `RunDirector.BuildLevelConfig`.

- Safe path band: 5%
- Risk path band: 15%
- Rare spike: 2% chance after elite path context
- Spike outcome: tier/star/modifier pressure increase

## 5) Mid-Run Adaptation Mechanics

Implemented in `MidRunAdaptationService` and exposed through `RunDirector`:

- Relic transformation node (`Destroy 2 -> generate higher tier`, cursed chance)
- Temporary class trait mutation (node-limited duration)
- Modifier rerouting API (meta strategic layer)
- Risky rebuild (once per run): discard relics, gain 2 legendary, HP -> 1

## 6) Failure Psychology Loop (Post-Run Analytics)

Implemented in `PostRunAnalyticsService` and surfaced via `EndScreenPresenter.BuildAnalyticsSummary`.

Captured analytics include:
- Heat curve history
- Mistake totals + per-puzzle peak
- Hardest puzzle signature (stars/modifier/tier)
- Modifier impact rating
- Generated improvement suggestion string

## 7) Rare Meta-Level Surprises

### Hidden Dual-Modifier Boss
- Boss definition added in `BossService.BuildHiddenDualModifierBoss`
- Exposed in `RunDirector.BuildHiddenDualModifierBoss`

### Secret Class Unlock
- Added `ClassId.ChaosMonk`
- Profile unlock condition implemented:
   - Hidden dual-boss pathway unlocked
   - Dual-modifier clear achieved with exactly 1 HP remaining

## 8) Risk Escalation Beyond Difficulty

Added stress-variant flagging in level config:
- Time Pressure
- Limited Pencil Marks
- Locked Rows
- Gradual Fog Creep

Current implementation sets/run-tags the stress variant for node-level systems and future UI rule overlays.

## 9) Endgame Loop Sustainability

Implemented through `AscensionService` and `ProfileService` endpoints:

- Ascension (`ApplyAscension`): raises cap progression and enables seasonal challenge state
- Prestige reset (`TryPrestigeReset`): optional reset with prestige count increment
- Seasonal challenge seed (`GetSeasonalChallengeSeed`): deterministic monthly seed

## Implementation References

- `Assets/Scripts/Run/RunDirector.cs`
- `Assets/Scripts/Run/RunArchetypeService.cs`
- `Assets/Scripts/Run/RunEventService.cs`
- `Assets/Scripts/Run/CurseService.cs`
- `Assets/Scripts/Run/RunVarianceService.cs`
- `Assets/Scripts/Run/MidRunAdaptationService.cs`
- `Assets/Scripts/Run/PostRunAnalyticsService.cs`
- `Assets/Scripts/Economy/RelicCatalogService.cs`
- `Assets/Scripts/Economy/RelicSynergyService.cs`
- `Assets/Scripts/Economy/ShopService.cs`
- `Assets/Scripts/Boss/BossService.cs`
- `Assets/Scripts/Save/ProfileService.cs`
- `Assets/Scripts/Meta/AscensionService.cs`
- `Assets/Scripts/UI/EndScreenPresenter.cs`


