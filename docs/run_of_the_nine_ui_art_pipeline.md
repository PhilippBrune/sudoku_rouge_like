# Run of the Nine --- UI Art Pipeline Document

## 1. Purpose

This document defines the **production rules for all UI icons, sprite
atlases, colors, and layout spacing** for the Sudoku Roguelike *Run of
the Nine*.\
The goal is to guarantee **visual consistency**, **engine-ready asset
organization**, and **efficient iteration** during development.

Current integration target in this repo:

- Icon assets live in `Assets/Resources/GeneratedIcons`.
- Generate/update placeholder icon set from Unity menu:
	- `Tools/Run of the Nine/Generate Pixel Icon Set`

------------------------------------------------------------------------

# 2. File Naming Convention

All assets must follow this naming scheme.

Format:

icon\_`<category>`{=html}*`<name>`{=html}*`<state>`{=html}.png

Examples:

icon_menu_start_idle.png\
icon_menu_start_hover.png\
icon_item_solver_normal.png\
icon_item_solver_rare.png\
icon_item_solver_epic.png\
icon_ui_hp.png\
icon_ui_gold.png

Categories:

menu\
item\
ui\
grid\
rarity\
class

------------------------------------------------------------------------

# 3. Pixel Specifications

Base icon size:

64 × 64 pixels

Grid assets:

5×5 board → 320 × 320\
6×6 board → 384 × 384\
7×7 board → 448 × 448\
8×8 board → 512 × 512\
9×9 board → 576 × 576

Tile size recommendation:

64 px per tile

------------------------------------------------------------------------

# 4. Sprite Atlas Layout

Recommended atlas size:

2048 × 2048

Atlas Structure:

Row 1 --- Menu Icons\
Row 2 --- UI Icons\
Row 3 --- Items (Normal)\
Row 4 --- Items (Rare)\
Row 5 --- Items (Epic)\
Row 6 --- Class Icons\
Row 7 --- Puzzle Interaction Icons\
Row 8 --- Grid Icons

Padding between sprites:

4 px

------------------------------------------------------------------------

# 5. Kyoto Garden Color Palette

Primary Colors

Garden Green #4E7A64

Deep Moss #2F4F3E

Soft Stone #D6D3C4

Lantern Gold #D4AF37

Water Blue #4A6C8C

Cherry Blossom Accent #F1A7B5

Shadow #1B2A2F

------------------------------------------------------------------------

# 6. Rarity Color System

Normal Border color: Stone Gray

Rare Border color: Soft Blue Glow

Epic Border color: Gold Lantern Glow

Accessibility Rule: Rarity must be visible via:

color\
border pattern\
icon glow

------------------------------------------------------------------------

# 7. UI Spacing Rules

Main Menu Button Size

Width: 480 px\
Height: 64 px

Spacing Between Buttons

24 px

Menu Vertical Margin

Top margin: 120 px\
Bottom margin: 80 px

Icon Padding Inside Buttons

Left padding: 16 px

------------------------------------------------------------------------

# 8. Main Menu Layout Structure

Menu order:

Start Game\
Resume Game\
Tutorial\
Meta Progression\
Game Modes\
Items\
Options\
Credits\
Quit

Visual Hierarchy:

Start Game → strongest highlight\
Resume Game → secondary highlight\
Other buttons → neutral stone color

Hover State:

button brightens\
lantern glow appears\
petal particles drift

------------------------------------------------------------------------

# 9. UI Icons List

HP\
Gold\
Pencil Marks\
XP\
Level\
Reroll Token\
Item Slot\
Locked Slot\
Boss Warning\
Puzzle Complete\
Mistake

------------------------------------------------------------------------

# 10. Item Icons

Solver\
Finder\
Reveal\
Mist Shield\
Focus Charm\
Time Lantern\
Harmony Stone\
Sudoku Compass

Each item must exist in:

Normal\
Rare\
Epic

------------------------------------------------------------------------

# 11. Class Icons

Number Freak (Unlocked)

Future classes placeholder:

Garden Monk\
Puzzle Sage\
Lantern Keeper\
Stone Guardian

------------------------------------------------------------------------

# 12. Grid Icon Style

Sudoku grids must resemble:

Zen sand garden patterns\
Stone tile borders\
Subtle moss edges

Visual rules:

clean lines\
high contrast digits\
soft shadows

------------------------------------------------------------------------

# 13. UI Animation Rules

Hover Animation

scale 1.00 → 1.05

Click Animation

scale 1.05 → 0.95 → 1.00

Completion Animation

gold particles\
soft bell sound\
lantern glow

------------------------------------------------------------------------

# 14. Particle Effects

Falling Sakura Petals

spawn every 4 seconds

Water Ripple

trigger on puzzle completion

Lantern Glow

active on epic items

------------------------------------------------------------------------

# 15. Export Settings

File format

PNG

Transparency

Enabled

Color mode

RGBA

------------------------------------------------------------------------

# 16. Engine Import Settings

Unity / Godot

Sprite Mode: Multiple\
Filter Mode: Point (no blur)\
Compression: None\
Pixels Per Unit: 64

------------------------------------------------------------------------

# 17. Asset Folder Structure

Assets/ UI/ Icons/ Atlases/ Items/ Classes/ Grids/ Effects/

------------------------------------------------------------------------

# 18. Future Expansion

Reserved space in atlas for:

Seasonal icons\
New relics\
New classes\
Additional puzzle modifiers
