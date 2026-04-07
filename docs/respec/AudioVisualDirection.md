# Audio & Visual Direction

**Version:** 1.4 | **Date:** 2026-03-24 | **Status:** implemented

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.4 | 2026-03-24 | implemented | Audio wiring completed: GameBootstrap creates MenuMusicController + RunAudioController. FloorMusicGenerator loop durations reduced from 120-300s to 30-48s for fast startup. Menu music plays on menu, stops on run launch. Color scheme: BtnColor cream (0.82,0.75,0.62), BtnTextColor dark (0.12,0.10,0.08), AccentColor gold (0.98,0.83,0.26). |
| 1.2 | 2026-03-23 | implemented | Game over and victory one-shot stingers added to FloorMusicGenerator |
| 1.1 | 2026-03-22 | implemented | All features implemented: ProceduralSfxLibrary (20+ SFX), FloorMusicGenerator (5 floor loops + boss layers + stingers), AmbientParticleController, AnimationHelper with timing constants, RunAudioController 4-channel audio with crossfade and ducking |
| 1.0 | 2026-03-21 | review | Initial specification |

---

## Overview

Run of the Nine uses a **minimalist Japanese garden aesthetic** with pixel art visuals and an ambient soundscape. The art direction prioritizes calm readability during gameplay and reserves visual intensity for reward moments and boss encounters.

---

## Visual Identity

### Palette

| Context | Colours | Usage |
|---------|---------|-------|
| Background | Calm greens, stone greys, warm cream | Garden floor themes, menus |
| Grid | Near-white cells, soft grey borders | Puzzle board, default state |
| Reward | Lantern gold, warm amber | Gold gains, level-ups, XP bar |
| Danger | Muted red, warm crimson | HP loss, mistake flash |
| Modifier overlays | Per-modifier unique hue (see BossMechanicsSystem) | In-puzzle constraint geometry |
| UI chrome | Semi-transparent dark `(0.05, 0.05, 0.08, 0.85)` | Panels, menus, modals |

### Pixel Art Specifications

| Asset Type | Resolution | Style |
|-----------|:---:|--------|
| Item icons | 16×16 | Japanese garden objects, rarity conveyed by colour temperature |
| Relic icons | 16×16 | Tier-based border glow (silver → gold → animated → prismatic) |
| Class portraits | 32×32 | Character silhouette with class-themed accent colour |
| Floor theme backgrounds | Tiling | Parallax-ready, 2–3 layers (ground, mid, sky) |
| Menu buttons | Procedural | Image alpha=0, hover=8% white, press=12% black |
| UI elements | Procedural | Built in C# at runtime, no prefabs |

### Art Inspirations

- Kyoto gardens (Ryoan-ji minimalism, Kinkaku-ji elegance)
- Traditional Japanese woodblock prints (Ukiyo-e colour sensibility)
- Pixel art games: Eastward, Celeste (clean readability at small resolution)

---

## Floor Theme Visuals

Each of the 5 garden floors has a distinct visual identity:

| Floor | Theme | Primary Palette | Key Visual Elements |
|:---:|-------|----------------|---------------------|
| 1 | Bamboo Courtyard | Greens, warm wood | Bamboo stalks, raked sand, stone lanterns |
| 2 | Moss Garden | Deep greens, grey stone | Moss-covered rocks, stepping stones, ferns |
| 3 | Koi Terrace | Blues, orange-gold | Water reflections, koi fish, wooden bridges |
| 4 | Stone Lantern Walk | Warm amber, grey | Lit lanterns, gravel paths, autumn leaves |
| 5 | Shrine Summit | White, gold, crimson | Torii gates, cherry blossoms, shrine architecture |

### Theme Assets per Floor

Each floor requires:
- `Sprite` — background tileable texture (tiled behind puzzle grid)
- `AudioClip` — ambient music loop (see Audio section)
- Colour tint for UI chrome (subtle per-floor variation)
- Transition cutscene artwork (Garden Cleared card)

---

## Ambient Effects

### Particle Systems

Subtle ambient particles add life without distracting from gameplay:

| Effect | Context | Max Concurrent |
|--------|---------|:---:|
| Falling petals | Floor 1, 5 (cherry blossoms) | 8 particles |
| Floating dust motes | Floor 2, 4 (moss, lantern glow) | 6 particles |
| Water ripples | Floor 3 (koi terrace) | 4 particles |
| Lantern glow pulse | Floor 4 (stone lantern walk) | 2 particles |
| Light rays | Floor 5 (shrine summit) | 3 particles |

### VFX Rules

- **Maximum 2 non-critical visual layers** active simultaneously during gameplay
- Critical layers (modifier overlays, cell highlights) always render; ambient effects yield priority
- All particle effects respect the `ReduceMotion` accessibility toggle — disabled entirely when active
- `GraphicsSettingsModel.ParticleIntensity` (0.0–1.0) scales particle count and opacity

---

## Sound Design

### Philosophy

The audio design follows a **natural materials tonal palette**: wood, wind, water, stone. Sounds should feel like they belong in a physical garden space.

### Music

| Context | Style | Duration | Loop |
|---------|-------|:---:|:---:|
| Main Menu | Ambient koto/shamisen, gentle rhythm | 90–120s | Yes |
| Floor 1 | Light bamboo flute, wind chimes | 120–180s | Yes |
| Floor 2 | Soft rain, distant temple bell | 120–180s | Yes |
| Floor 3 | Flowing water, gentle koto | 120–180s | Yes |
| Floor 4 | Crackling fire, low strings | 120–180s | Yes |
| Floor 5 | Full ensemble (koto, shakuhachi, taiko), building intensity | 180–240s | Yes |
| Boss puzzle | Intensified floor theme + tension layer (deeper drums, dissonance) | 120s | Yes |
| Endless Zen | Minimal ambient drone, no melody — pure focus | 300s | Yes |
| Spirit Trials | Rhythmic percussion, timer urgency | 90s | Yes |
| Game Over | Fading melody, single bell toll | 15s | No |
| Victory | Triumphant koto flourish, temple bell | 20s | No |

### Menu Music Style

`AudioSettingsModel.MenuMusicStyleIndex` allows the player to select between menu music variants (dropdown in Options). Exact styles TBD based on asset production.

### SFX Palette

| Action | Sound | Category |
|--------|-------|----------|
| Cell select | Soft wood tap | UI |
| Number place (correct) | Stone click, satisfying | Gameplay |
| Number place (wrong) | Dull thud, brief dissonance | Gameplay |
| Pencil mark | Light brush stroke | Gameplay |
| Pencil erase | Paper scrape | Gameplay |
| HP loss | Low bell toll | Feedback |
| Combo streak (5+) | Ascending chime sequence | Feedback |
| Combo break | Falling water drop | Feedback |
| Item use | Wooden drawer open/close | UI |
| Item reward appear | Gentle wind chime | UI |
| Gold gain | Coin clink (ceramic, not metallic) | Feedback |
| Level complete | Temple bell + wind gust | Feedback |
| Boss gate open | Deep drum hit, gate creak | Transition |
| Floor transition | Wind sweep, distant chime | Transition |
| Modifier activate | Low resonant hum | Gameplay |
| Fog reveal | Whisper/breath sound | Gameplay |
| XP bar fill | Rising tone, smooth | UI |
| Level up | Bright chime cascade | UI |
| Achievement unlock | Distinctive metallic ring | UI |
| Menu button hover | Soft wood creak | UI |
| Menu button click | Stone tap | UI |
| Error/blocked action | Muted buzz | UI |

### Audio Mixing

| Channel | Default Volume | Purpose |
|---------|:---:|---------|
| Master | 1.0 | Global volume cap |
| Music | 1.0 | Background music loops |
| SFX | 1.0 | Gameplay sound effects |
| UI | 1.0 | Menu interactions, notifications |

All volumes stored in `AudioSettingsModel` and clamped to `[0.0, 1.0]` during save sanitization.

### Audio Rules

- **No sudden loud sounds** — all SFX are mixed to stay below the music layer
- **Correct placement sound** should be more satisfying than wrong placement — positive reinforcement
- **Boss music** layers tension on top of the floor theme (additive, not replacement)
- **Endless Zen** uses deliberately minimal audio — the mode is about focus and flow
- **MuteAll toggle** (`AudioSettingsModel.MuteAll`) silences all audio immediately

---

## Animation Standards

### Timing

| Animation | Duration | Easing |
|-----------|:---:|--------|
| Cell highlight on select | 100ms | Ease-out |
| Number placement | 150ms | Ease-in-out |
| Wrong placement flash | 200ms (red flash → fade) | Linear |
| HP bar decrease | 300ms | Ease-out |
| Pencil bar decrease | 200ms | Ease-out |
| XP bar fill (end-of-run) | 1500ms per level | Ease-in-out |
| Level-up burst | 500ms | Ease-out |
| Item reward appear | 300ms (scale 0 → 1) | Bounce |
| Relic reveal | 400ms (fade + scale) | Ease-out |
| Floor transition fade | 800ms fade-out + 800ms fade-in | Linear |
| Modifier legend isolate | 200ms dim/restore | Ease-in-out |
| Combo counter increment | 100ms (scale pulse 1.0 → 1.1 → 1.0) | Bounce |
| Menu panel open/close | 200ms | Ease-out |

### XP Bar Animation (End-of-Run)

The XP bar fills smoothly with a rising tone SFX. Skippable with **Space** key. The animation must feel satisfying even after a loss — it should communicate "you made progress" regardless of outcome.

If multiple levels are gained, the bar fills fully, flashes, resets to 0, and fills again for each subsequent level.

---

## Screen Shake

`GraphicsSettingsModel.ScreenShake` (bool, default true) controls subtle screen shake on:
- Wrong placement (2px, 100ms)
- Boss modifier activation (3px, 200ms)
- HP reaching critical (≤ 2) for the first time (2px, 150ms)

Disabled entirely when `ScreenShake = false` or `ReduceMotion = true`.

---

## Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/RuntimeModels.cs` | `AudioSettingsModel`, `GraphicsSettingsModel` |
| `Assets/Scripts/UI/PrototypeRunScreenController.cs` | In-puzzle animations, overlay rendering, combo display |
| `Assets/Scripts/UI/MainMenuController.cs` | Audio slider wiring, resolution dropdown |
| `Assets/Scripts/UI/InRunUiBlueprintBuilder.cs` | Runtime UI construction, sprite generation |
| `Assets/Scripts/Data/FloorThemeData.cs` | Floor theme Sprite + AudioClip references |

---

## Music Production Guide

Detailed composition briefs for all required music tracks. Target format: **44.1kHz 16-bit WAV**, loopable where indicated.

### Floor Themes

| Track | BPM | Key | Instruments | Mood | Duration |
|-------|:---:|-----|------------|------|:---:|
| **Floor 1 — Bamboo Courtyard** | 70 | D minor pentatonic | Shakuhachi (bamboo flute) lead, light finger-picked koto, wind chime accents, subtle wind pad | Gentle awakening, morning dew freshness | 120s loop |
| **Floor 2 — Moss Garden** | 60 | A minor | Soft rain texture bed, distant temple bell (every 30s), low koto drone, occasional frog chirp sample | Contemplative stillness, damp earth | 150s loop |
| **Floor 3 — Koi Terrace** | 75 | G major pentatonic | Flowing water texture, gentle koto arpeggios, soft shamisen plucks, occasional fish splash sample | Peaceful movement, sunlit water | 140s loop |
| **Floor 4 — Stone Lantern Walk** | 65 | E minor | Crackling fire texture, low cello/erhu drone, sparse taiko at pianissimo, autumn leaf rustle | Warm twilight, quiet resolve | 160s loop |
| **Floor 5 — Shrine Summit** | 80 | C minor → C major resolve | Full ensemble: koto melody, shakuhachi countermelody, shamisen rhythm, taiko pulse (building), temple bell at loop point | Majestic arrival, spiritual intensity | 180s loop |

### Context Tracks

| Track | BPM | Style | Duration |
|-------|:---:|-------|:---:|
| **Main Menu** | 55 | Ambient koto harmonics over wind pad, no rhythm section, meditative | 100s loop |
| **Boss Puzzle** | Floor BPM +10 | Tension layer added over floor theme: deeper taiko pattern, dissonant string tremolo, heartbeat low bass pulse. Layered on top of the active floor track. | 120s loop |
| **Endless Zen** | None (free tempo) | Ambient drone (sine + filtered noise), no melody, no rhythm. Occasional single koto note (random interval 20–40s). Pure focus soundscape. | 300s loop |
| **Spirit Trials** | 100 | Rhythmic percussion: wooden blocks, rim clicks, building urgency. Sparse shamisen stabs. Tension increases every 60s (add layer). | 90s loop |
| **Game Over** | Rubato | Single shakuhachi phrase descending, fading into silence. Final temple bell toll (sustained 5s reverb). | 15s one-shot |
| **Victory** | 85 | Triumphant koto flourish ascending, shamisen strum, full temple bell peal, taiko crescendo, resolving chord with wind chime tail. | 20s one-shot |
| **Floor Transition** | None | Wind sweep sound, distant chime ascending in pitch, brief silence, new floor theme fades in. | 5s one-shot |
| **Level Complete** | None | Single bright temple bell strike + gentle wind gust. Short and clean. | 3s one-shot |

### Music Layering Rules

- Boss music is **additive** — the floor theme continues playing, boss tension layer mixes on top
- When transitioning between floors, current track fades out (800ms), 200ms silence, new track fades in (800ms)
- Music volume ducks 20% during XP bar animation to let the rising tone SFX be audible

---

## SFX Production Guide

All SFX should use the **natural materials palette**: wood, stone, water, wind, ceramic, paper, metal (bells only). Avoid synthetic/electronic sounds. Target format: **44.1kHz 16-bit WAV**, mono, normalized to -6dB peak.

### Gameplay SFX

| ID | Sound | Source Suggestion | Duration | Priority |
|----|-------|-------------------|:---:|:---:|
| `sfx_cell_select` | Soft wood tap | Single chopstick tap on bamboo | 80ms | High |
| `sfx_number_correct` | Satisfying stone click | Two smooth river stones clicking together, bright | 150ms | High |
| `sfx_number_wrong` | Dull thud + brief dissonance | Wooden mallet on padded surface + single off-key string pluck | 200ms | High |
| `sfx_pencil_mark` | Light brush stroke | Calligraphy brush on washi paper, single stroke | 120ms | High |
| `sfx_pencil_erase` | Paper scrape | Fingertip sliding across textured paper | 100ms | Medium |
| `sfx_fog_reveal` | Whisper/breath | Soft exhale through bamboo tube, airy | 300ms | Medium |
| `sfx_modifier_activate` | Low resonant hum | Singing bowl strike, dampened quickly | 500ms | Medium |
| `sfx_combo_5` | Ascending chime (3 notes) | Wind chime, 3 ascending tones (C-E-G) | 400ms | High |
| `sfx_combo_10` | Ascending chime (5 notes) | Wind chime, 5 ascending tones (pentatonic scale) | 600ms | Medium |
| `sfx_combo_break` | Falling water drop | Single water droplet into still pool | 200ms | Medium |

### Resource SFX

| ID | Sound | Source Suggestion | Duration | Priority |
|----|-------|-------------------|:---:|:---:|
| `sfx_hp_loss` | Low bell toll | Temple bell, single strike, low register, 1s sustain | 1000ms | High |
| `sfx_hp_critical` | Warning tone | Two rapid bell strikes, slightly dissonant | 500ms | Medium |
| `sfx_pencil_depleted` | Dry scratch | Dry brush on rough stone, no ink | 150ms | Low |
| `sfx_gold_gain` | Ceramic coin clink | Two ceramic tokens tapping together (NOT metallic) | 120ms | Medium |

### UI SFX

| ID | Sound | Source Suggestion | Duration | Priority |
|----|-------|-------------------|:---:|:---:|
| `sfx_button_hover` | Soft wood creak | Gentle bamboo flex | 60ms | Medium |
| `sfx_button_click` | Stone tap | Finger tap on flat stone | 80ms | High |
| `sfx_error_blocked` | Muted buzz | Dampened string buzz, very short | 100ms | Medium |
| `sfx_item_use` | Drawer open/close | Wooden jewelry box lid, open and close | 250ms | Medium |
| `sfx_item_reward` | Wind chime | Single gentle wind chime tone | 300ms | Medium |
| `sfx_relic_reveal` | Crystal shimmer | Glass wind chime + brief reverb tail | 400ms | Medium |
| `sfx_xp_bar_fill` | Rising tone | Continuous bowed string ascending in pitch, smooth | 1500ms | High |
| `sfx_level_up` | Bright chime cascade | 4-note descending wind chime cascade (high → low) + shimmer | 800ms | High |
| `sfx_achievement` | Metallic ring | Small temple bell, bright, clean strike | 500ms | Medium |
| `sfx_boss_gate_open` | Deep drum + gate | Single taiko hit + wooden gate sliding open | 600ms | Medium |
| `sfx_floor_transition` | Wind sweep | Sustained wind through bamboo grove, ascending pitch | 1200ms | Medium |
| `sfx_menu_open` | Paper unfold | Scroll or paper unrolling | 200ms | Low |
| `sfx_menu_close` | Paper fold | Scroll or paper rolling up | 150ms | Low |
| `sfx_shop_purchase` | Ceramic exchange | Two ceramic pieces placed on wood surface | 200ms | Low |
| `sfx_reroll` | Dice rattle | Wooden dice in ceramic cup, brief shake | 300ms | Low |

### SFX Mixing Rules

- All SFX play below music volume (SFX peak at -6dB relative to music)
- `sfx_number_correct` should be noticeably more pleasant than `sfx_number_wrong` — positive reinforcement
- No more than 3 SFX playing simultaneously; newest sound takes priority
- `MuteAll` bypasses all audio immediately

---

## Open Issues

No open issues remaining.


---

## Requirements Traceability

<!-- AUTO-GENERATED by SPICE pipeline. Do not edit manually. -->

| REQ-ID | Title | Linked Systems |
|--------|-------|----------------|
| REQ-UI-027 | 1. **AudioVisualDirection | RunDirector, GraphicsSettingsModel, AudioSettingsModel** |
| REQ-UI-028 | 2. **ArtIdentity | GraphicsSettingsModel** |
| REQ-UI-029 | 3. **VisualEfficiency | RunDirector, GraphicsSettingsModel** |
| REQ-UI-030 | 4. **AmbientEffects | GraphicsSettingsModel, AudioSettingsModel** |
| REQ-UI-031 | 5. **SoundDesignPhilosophy | AudioSettingsModel** |
| REQ-UI-032 | 6. **MusicSelectionAndStyle | AudioSettingsModel** |
| REQ-UI-033 | 7. **SoundFXPalette | AudioSettingsModel** |
| REQ-UI-034 | 8. **AudioMixingAndVolumeControls | RunDirector, AudioSettingsModel** |
| REQ-UI-035 | 9. **AnimationStandardsForGameplayFeedback | GraphicsSettingsModel** |
| REQ-UI-036 | 10. **AnimationsInEndOfRunExperience | AudioSettingsModel, GraphicsSettingsModel** |
