# Run of the Nine --- Icon Generation Guide 

This document outlines the specifications for image prompts and file naming conventions for generating all icons used in the Run of the Nine game. The goal is to maintain visual consistency, pixel art clarity, and easy asset management.

## Assumptions/Clarifications

1. "Pixel???art" in the source document is interpreted as "Pixel Art".
2. The term "Fooocus" in the source document is unclear. The term "Fooocus" was not specified in the context provided.
3. The term "pixel art 64x64" in the source document is understood to mean "64x64 pixel art".

## Global Style Settings

All icons in the game will adhere to the following style settings:

- Style: Pixel Art
- Resolution: 64x64 pixels
- Shape: Clean silhouette
- Shading: Minimal
- Palette: Soft Kyoto garden color palette, consisting of calm greens, stone grays, and lantern gold accents
- Lighting: Soft top light with a slight glow for magical items
- Background: Transparent with no shadows outside icon bounds

## Naming Convention

Icons are named using the following convention: `icon_<category>_<name>_<rarity>.png`

Examples:

- `icon_relic_combo_common.png`
- `icon_relic_lantern_legendary.png`
- `icon_class_gardener.png`
- `icon_ui_settings.png`

## Icon Categories

### Puzzle System Icons

- Main Icon: `icon_puzzle_main.png`
  - Prompt: A pixel art puzzle piece icon, centered puzzle piece, clean geometric shape, top-left placeholder circle for puzzle size number, bottom-right placeholder area for stars rating, using minimal zen garden palette, stone gray puzzle piece, soft green accent background, 64x64 pixel art game icon.
- Star Rating: `icon_puzzle_star_empty.png` and `icon_puzzle_star_filled.png`
  - Prompt: A pixel art small golden star icon, simple UI symbol, using temple lantern gold palette, minimal pixel art 64x64.

### Class Icons

- Gardener: `icon_class_gardener.png`
  - Prompt: A pixel art zen gardener symbol with rake and leaf, Kyoto garden aesthetic, calm green palette, 64x64 pixel art.
- Monk: `icon_class_monk.png`
  - Prompt: A pixel art meditation monk silhouette, lotus pose icon, zen temple theme, 64x64 pixel art.
- Architect: `icon_class_architect.png`
  - Prompt: A pixel art architect symbol with compass and puzzle grid, stone and gold palette, minimal pixel art icon, 64x64.

### Relic Icons

- Lantern Relic: `icon_relic_lantern_epic.png`
  - Prompt: A pixel art japanese stone lantern glowing softly, temple lantern aesthetic, warm golden glow, 64x64 pixel art.
- Bonsai Relic: `icon_relic_bonsai_common.png`
  - Prompt: A pixel art bonsai tree relic icon, small zen garden bonsai, calm green palette, minimal pixel art 64x64.
- Koi Relic: `icon_relic_koi_rare.png`
  - Prompt: A pixel art koi fish icon, red and white koi, zen water garden theme, clean pixel art 64x64.

### Curse Icons

- Blindness Curse: `icon_curse_blindness.png`
  - Prompt: A pixel art closed eye symbol with dark mist, subtle purple cursed glow, minimal pixel art 64x64.
- Locked Slot Curse: `icon<´¢£beginÔûüofÔûüsentence´¢£>_curse_lock.png`
  - Prompt: A pixel art lock icon with cracked stone background, curse symbol, pixel art 64x64.

### UI Icons

- Play: `icon_ui_play.png`
  - Prompt: A pixel art torii gate shaped play symbol, zen temple style UI icon, 64x64 pixel art.
- Items: `icon_ui_items.png`
  - Prompt: A pixel art relic chest icon, small wooden chest with gold clasp, zen treasure aesthetic, pixel art 64x64.
- Settings: `icon_ui_settings.png`
  - Prompt: A pixel art stone gear icon, minimal zen mechanical symbol, 64x64 pixel art.

## Rarity Frames

- Common: `frame_rarity_common.png`
  - Prompt: A pixel art stone item frame, simple gray border, zen minimal style.
- Rare: `frame_rarity_rare.png`
  - Prompt: A pixel art decorative blue item frame, minimal corners.
- Epic: `frame_rarity_epic.png`
  - Prompt: A pixel art ornate gold frame with subtle glow.
- Legendary: `frame_rarity_legendary.png`
  - Prompt: A pixel art glowing ornate gold frame with falling petals.

## Folder Structure

- `/icons`: Root folder for all icons
- `/icons/classes`: Folder for class icons
- `/icons/relics`: Folder for relic icons
- `/icons/curses`: Folder for curse icons
- `/icons/ui`: Folder for UI icons
- `/icons/puzzle`: Folder for puzzle icons
- `/icons/frames`: Folder for rarity frames

## Export Settings

- Format: PNG
- Resolution: 64x64
- Background: Transparent
- Scaling: Nearest neighbor

## Future Icons

This guide does not provide specifications for future icon categories:

- Boss icons
- Event icons
- Puzzle modifiers
- Garden progression icons
- Ascension icons


