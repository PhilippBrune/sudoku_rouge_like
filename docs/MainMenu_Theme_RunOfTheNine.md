# Main Menu Theme Design - Run of the Nine

## 1. Overall Aesthetic

Theme: Kyoto-inspired spiritual garden, calm and meditative, with illustrated signboard UI matching `main_menue.png` and icon language from `icons.png`.

Visual inspiration:
- Ryoan-ji (minimal rock garden)
- Kinkaku-ji (golden temple elegance)

Palette:
- Background: deep navy/stone gray (`#1C2833` to `#2C3E50`)
- Panels: lacquered blue-green (`#2B3D47`)
- Buttons: carved wood/slate blend (`#34495E` baseline, warmer highlights)
- Highlights: lantern gold (`#F9D342`) for hover/selection
- Accent: moss green (`#7FB069`), ember red (`#D35400`) for alerts/locked elements

Typography:
- Readable serif/sans pair with stronger title contrast
- Soft cream text for contrast
- Avoid pixel-font presentation for core menu readability

## 2. Layout and Composition

- Centered vertical button stack for primary actions
- Start flow includes dedicated class-select screen before entering path selection
- Header:
  - Title: `Run of the Nine`
  - Subtitle: `Sudoku Roguelike`
- Optional ambient layers behind header:
  - Falling petals
  - Soft mist
- Footer status line uses short live feedback (`Ready.` by default)

## 3. Button Design

- Shape: rounded rectangle (~16px corners)
- Surface: illustrated lacquered signboard style
- Hover:
  - lantern glow
  - slight scale-up (~1.05)
- Active:
  - slight press-in
  - shadow reduction

Required left icons (from `icons.png` mapping):
- Start Game: blooming bud
- Resume Game: scroll with ink stroke
- Tutorial: bamboo scroll
- Meta Progression: golden bloom
- Game Modes: garden lantern motif
- Items: chest
- Options: stone gear
- Credits: language/credits scroll
- Quit: torii gate lock

## 4. Background and Atmosphere

Layered parallax:
- Far: gradient mountain silhouette
- Mid: rock garden with moss stones and light water ripple
- Foreground: drifting sakura petals

Subtle particles:
- cherry blossom fall
- soft lantern flicker
- optional low fog

## 5. Sound and Ambience

- Ambient loop: water trickle, wind, distant temple bell
- Hover SFX: light bamboo chime
- Press SFX: soft stone tap

## 6. Accessibility

- High-contrast text over buttons
- Color-blind-safe state indicators
- Secondary cues for locked/highlighted states (icon + frame glow)

## 7. Animation Notes

- Background petals in 2-3 depth layers with different speeds
- Button glow cycle around 0.5s to 1s
- Subtle title shimmer to suggest reflected temple light

## 8. Art Direction Rule

- Replace temporary pixel placeholders with painterly/illustrated icons from `icons.png`.
- Keep icon names stable so runtime references remain unchanged.
- Avoid introducing new pixel-style UI components in menus or path signs.

## 9. Summary

This main menu style targets:
- calm spiritual tone
- clear hierarchy and interaction feedback
- compatibility with illustrated icon overlays and soft particles
- visual consistency with the garden-run gameplay aesthetic
