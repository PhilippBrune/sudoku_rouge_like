# Icon Creation Pipeline (Placeholder -> Production)

## Purpose

This project includes an editor tool that generates a placeholder icon set, a packed atlas, and a CSV mapping file to bootstrap UI icon usage. Final visual targets come from the illustrated production sheet (`icon_production_sheet.png`), not a pixel-art final style.

## Generator Script

- `Assets/Scripts/Editor/PixelIconSetGenerator.cs`

## What It Produces

- Individual icon textures (default 64x64)
- Combined atlas (default 2048x2048)
- Mapping CSV (`icon_name, atlas_rect` style metadata)
- Canonical output folder: `Assets/Resources/GeneratedIcons`

## How To Run

1. Open the project in Unity Editor.
2. Run `Tools/Run of the Nine/Generate Pixel Icon Set`.
3. Wait for asset import to complete.
4. Verify generated files in `Assets/Resources/GeneratedIcons`.

## Suggested Usage

1. Generate placeholders first.
2. Wire sprites into runtime UI image slots.
3. Replace placeholders with illustrated production-sheet assets over time.

## Icon Themes Covered

The generator includes broad categories intended for this project's roguelike Sudoku UI:
- Path and node states
- Combat/event encounter markers
- Item rarity and category markers
- Economy/reward symbols
- Status and utility indicators

## Notes

- Generated icons are production placeholders, not final art direction.
- Keep naming stable so replacing art does not break references.
- Runtime loading expects assets in `Assets/Resources/GeneratedIcons`.
- The legacy nested copy under `Assets/Scripts/My project/Assets/Resources/GeneratedIcons` is no longer the source of truth.
