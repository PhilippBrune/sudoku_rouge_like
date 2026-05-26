# Narrative Direction

## Current Direction

Run of the Nine is a Sudoku roguelike journey through five stylized Japanese garden spaces. The run is framed as a passage through living gardens, spirit gates, relic offerings, and boss thresholds rather than an everyday city-park walk.

This direction aligns with:

- `docs/GameDesignSpec.md`
- `docs/AudioVisualDirection.md`
- `docs/PathSystem_GardenOverview.md`
- `Assets/Scripts/Run/ParkNarrativeService.cs`
- `Assets/Scripts/Core/LocalizationService.cs`

## Floor Identity

1. Bamboo Courtyard: raked sand, bamboo gates, patient opening puzzles.
2. Moss Garden: koi pond, moss stones, quiet water rhythm.
3. Koi Terrace: bridges, black water, reflected patterns.
4. Stone Lantern Walk: lantern glow, autumn leaves, stronger pressure.
5. Shrine Summit: torii, mist, final offering, summit spirit.

## Narrative Rules

- Keep text short enough for generated UI panels.
- Prefer concrete garden imagery over exposition.
- Treat bosses as threshold spirits or garden guardians.
- Treat relics as offerings, charms, tokens, or found shrine objects.
- Keep class, relic, curse, and boss copy compatible with English and German localization.
- Avoid modern city-park details unless a future mode intentionally uses them as a variant.

## Runtime Narrative Hooks

- Floor and node flavor is served by `ParkNarrativeService.GetFloorEntryFlavor()` and `GetNodeFlavor()`.
- Cursed nodes use the same floor-aware flavor hook as puzzle, shop, rest, relic, event, and boss nodes, so curse risk is presented as a garden story beat before the mechanical confirmation panel appears.
- The first run intro card includes `ParkNarrativeService.GetClassIntroFlavor()` so each selected class enters the garden with a short identity beat.
- The victory/defeat screen uses the class-aware `ParkNarrativeService.GetRunEndFlavor(victory, classId)` overload, appending a class-specific ending line after the shared run conclusion.
- Narrative keys for floor, node, class intro, and class endings are exposed through `ParkNarrativeService.GetLocalizationKeys()` and covered by release localization tests.

## Localization Contract

Narrative runtime keys remain under `Narrative.*` to preserve existing UI calls and tests. English fallback text lives in `ParkNarrativeService`; translator-facing English and German text lives in `Assets/Resources/Localization/*.json` and is loaded by `LocalizationService`.

Before release, narrative text still needs runtime layout validation in German and at 150% accessibility font scale.
