# Runtime Resource Validation

## Purpose
This file records the automated coverage for ASSET-003 and ASSET-007. Dynamic icon mappings must resolve to shipped runtime sprites, and direct literal `Resources.Load` paths must resolve to committed runtime files.

## Covered Dynamic Mappings
- `ItemService.GetIconName(ItemType)` -> `Assets/Resources/items/icon_<name>.png`
- `RelicService.GetIconName(RelicId)` plus `RelicService.GetIconFolder(RelicId)` -> `Assets/Resources/relic|legendary/icon_<name>.png`
- `BossService.GetIconName(BossModifierId)` plus `BossService.GetIconFolder(BossModifierId)` -> `Assets/Resources/modifier|debuff/icon_<name>.png`
- `CurseService.GetIconName(curseId)` -> `Assets/Resources/cursed/icon_<name>.png`
- `ClassCatalog.GetIconName(ClassId)` -> `Assets/Resources/class/icon_<name>.png`

## Validation Contract
`DynamicResourceIconCoverageTests` verifies that every mapped ID:

- returns a non-empty icon name;
- resolves to a committed `.png` under `Assets/Resources`;
- has a Unity `.meta` file beside the PNG.

The same test fixture also scans direct literal `Resources.Load<T>("path")` calls under `Assets/Scripts` and verifies that each path resolves to a committed runtime resource with a Unity `.meta` file. Concatenated or service-driven paths are covered by the dynamic mapping tests above.

## Current Fallbacks
Boss debuff batch 2 uses explicit fallback art while unique icons are pending:

- `RadiusWipe` -> `debuff/icon_cross_wipe.png`
- `PencilScramble` -> `debuff/icon_pencil_drain.png`
- `GivenReveal` -> `debuff/icon_cell_lock.png`
- `RevealCost` -> `debuff/icon_gold_fine.png`
- `CellBlur` -> `debuff/icon_pencil_blind.png`

When unique art is added, update `BossService.GetIconName` and keep this test passing.

## Manual Release Check
Before a release candidate, run the EditMode test suite and spot-check the generated in-game screens that render these icons: item rewards, shop, relic rewards, boss gates, curse panel, class select, HUD inventory, and item codex.
