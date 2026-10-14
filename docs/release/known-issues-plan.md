# Known Issues Plan — after v0.1.5

Status: **proposal** · 2026-10-14 · Format: ECC `planner`

The v0.1.5 release notes list three known issues. This plan says how each gets fixed, in which order, and how it is verified. Gap and requirement IDs follow `docs/review/GAME_GAPS.md` and `docs/REQUIREMENT_MAP.md`. IDs marked *proposed* do not exist in a spec yet; they are added to the named spec when the work starts, not before.

| # | Known issue | Gap / gate | Kind of work | Blocked on | Rough effort |
|---|---|---|---|---|---|
| 1 | The in-game Options panel keeps its older look | GAP-029 (last open item), FUTURE-015 | Code | Nothing | 2–3 days |
| 2 | French, Spanish and Brazilian Portuguese: item, relic, boss, class and story texts in English | GAP-019 follow-up, FUTURE-014, PRG-LOCALIZATION | Tooling + translation + review | Native review (owner decision: community or paid) | 1 day tooling, ~1 day per language, plus review time |
| 3 | Music tracks are short loops | GAP-030, PRG-AUDIO | Assets + a small code change | Where the music comes from (owner decision) | Code 1 day; assets depend on the source |

Recommended order: 1 now (no dependencies), 2 next (the AI pass can start right away, review runs in parallel), 3 as soon as the music source is decided; its code part (step 3.3) can go first.

---

# Implementation Plan 1: In-run Options in the main Options style

## Overview
The main-menu Options and the in-run Options are built by two different builders, so they look different. Move the main Options' controls into a shared factory and rebuild the in-run panel with it: same tabs, same controls, same look, plus a "Run" tab for the run-only actions.

## Requirements
- The in-run Options look and behave like the main Options: tabs Audio / Display / Gameplay, the same sliders, toggles (with a visible on/off state), dropdowns (all items visible, GAP-042) and reset buttons.
- A fourth tab **Run** holds the run-only items: run seed, View Modifiers, Abandon Run, Save & Quit.
- Fully usable with keyboard and controller (controller-first; Start opens, B/Back closes, as `CancelRouteContract` defines).
- Options changed during a run still survive the next autosave (GAP-037 must not regress).

## Facts (2026-10-14)
- Main Options: `MainMenuBlueprintBuilder.BuildOptionsPanel` (`Assets/Scripts/UI/MainMenuBlueprintBuilder.cs:592`): `MakePanel`, tab buttons `TabAudio` / `TabDisplay` / `TabGame` with sections `AudioSection` / `DisplaySection` / `GameSection`, private `BuildButton` (:2570) and `BuildDropdown` (:2610, GAP-042 sizing).
- In-run Options: `InRunUiBlueprintBuilder.BuildInGameOptionsPanel` (`Assets/Scripts/UI/InRunUiBlueprintBuilder.cs:414`): one flat page with hand-placed anchors and its own button/toggle/slider builders (`IGMuteWhenUnfocusedToggle`, `IGAltSymbolsToggle`, `BtnInGameOptionsModifiers`, `BtnInGameOptionsAbandon`, `BtnInGameOptionsSaveQuit`, `BtnInGameOptionsClose`).
- Both read and write through `OptionsController`.

## Architecture Changes
- New `Assets/Scripts/UI/OptionsUiFactory.cs`: panel shell, tab bar plus section switching, labelled slider, toggle with checkmark, dropdown, button, reset button; colours from `GamePalette`.
- `MainMenuBlueprintBuilder.BuildOptionsPanel` uses the factory (no visual change; the file shrinks, which helps its size budget).
- `InRunUiBlueprintBuilder.BuildInGameOptionsPanel` is rebuilt with the factory, with four tabs.

## Implementation Steps

### Phase 1: Shared factory, no visual change
1. **Extract the control builders** (File: `Assets/Scripts/UI/OptionsUiFactory.cs`, `MainMenuBlueprintBuilder.cs`)
   - Action: move the tab bar, section container, `BuildButton`, `BuildDropdown`, toggle and slider creation out of the main Options builder into static methods; the main builder calls them.
   - Why: one source for the look; the in-run panel cannot drift again.
   - Dependencies: none · Risk: Low (pure move; the PlayMode traversal test is the guard)
2. **Run the existing Options tests** (`GeneratedUiPlayModeTraversalTests.OptionsPanelTabsAndLanguageConfirmation_AreRuntimeNavigable`, `DisplayModeTests`, `OptionsPersistenceTests`)
   - Action: they must stay green unchanged.
   - Dependencies: step 1 · Risk: Low

### Phase 2: Rebuild the in-run panel
3. **Tabbed in-run Options** (File: `InRunUiBlueprintBuilder.cs`)
   - Action: build Audio / Display / Gameplay with the factory from the same `OptionsController` bindings; add the Run tab with seed text, View Modifiers, Abandon Run, Save & Quit; Back/Close at the bottom like the main panel.
   - Why: same look and the same controls in both places.
   - Dependencies: step 1 · Risk: Medium (object names are used by tests and `InRunController`; keep them or update the tests in the same change)
4. **Navigation** (Files: `InRunUiBlueprintBuilder.cs`, `InRunController.RunMenu.cs`)
   - Action: explicit `Navigation` from the tab bar into each section and back; the first control of a tab is selected when the tab opens; Start and B follow `CancelRouteContract`.
   - Dependencies: step 3 · Risk: Medium
5. **Spec** (File: `docs/GameDesignSpec.md`, Options section; `docs/REQUIREMENT_MAP.md`)
   - Action: add *proposed* `UI-OPT-001` (the in-run Options use the main Options' tabs, controls and style), `UI-OPT-002` (the Run tab and its actions), `UI-OPT-003` (keyboard/controller reachability); close GAP-029 and FUTURE-015.
   - Dependencies: step 3 · Risk: Low

## Testing Strategy
- PlayMode (`InRunUiPlayModeTests`): open the in-run Options, switch every tab, change one control per tab and read it back from `OptionsController`; the open display-mode dropdown shows all four items (the GAP-042 assertion); every control is reachable by navigation from the tab bar.
- Regression: change an option in-run, trigger an autosave, reload: the value is kept (GAP-037).
- Build (E2E): screenshots of main vs in-run Options at 1920×1080 and in a 1280×720 window; Save & Quit and Abandon Run from the Run tab.

## Risks & Mitigations
- **`InRunController` / `InRunUiBlueprintBuilder` size budgets** — the factory keeps the builder smaller than today; check the budget test.
- **Renamed objects break tests silently** — keep the existing names where possible; grep the tests for each name you change.

## Success Criteria
- [ ] Main and in-run Options are visually the same (screenshots in the E2E report).
- [ ] All Options PlayMode tests green, including the new in-run ones.
- [ ] GAP-029 FIXED, FUTURE-015 closed, the release notes no longer list the issue.

---

# Implementation Plan 2: French, Spanish and Brazilian Portuguese gameplay text

## Overview
The three languages translate the interface but not the gameplay content. Build an export/import path and a parity test first, translate with an AI pass that follows a glossary, then have native speakers review in the game.

## Requirements
- `fr.json`, `es.json`, `pt.json` contain every key that `de.json` has, with the same placeholders (`{0}`, `{1}`, rich-text tags).
- Game terms are consistent (glossary), sudoku variant names follow each language's usual terms, and Brazilian Portuguese is used for `pt`.
- No text overflows its UI element.

## Facts (2026-10-14)
- Catalogs in `Assets/Resources/Localization/`: en 779 keys, de 1,310, fr / es / pt 890 each.
- Missing per language compared with German: **421 keys** (the docs still say 431). Narrative 84, Relic 69, Item 67, Boss 58, Class 32, Harmony 14, RunConfirm 6, about 90 single status and UI strings. About **2,750 English words per language**.
- `ReleaseLocalizationReadinessTests` enforces complete German only; for fr/es/pt only the double-encoding check (`LocalizationCatalogQualityTests`) runs.
- English text for many content keys lives in code (the second argument of `T(key, english)` and the content providers), not in `en.json`.

## Architecture Changes
- New `tools/localization/` scripts: `export_missing` (per language: key, English source, German reference, category, placeholders, length hint) and `import_translations` (writes the catalogs, keeps key order and BOM).
- New `docs/localization-glossary.md`: fixed terms per language.
- New EditMode test `LocalizationParityTests`.

## Implementation Steps

### Phase 1: Tooling and guardrails
1. **Collect the English source for every key** (File: an `[Explicit]` EditMode diagnostic test or editor script)
   - Action: walk the same content providers `ReleaseFacingContentProviders_HaveGermanTranslations` uses and dump key → English for all keys German has.
   - Why: the English text is partly in code, so the catalogs alone are not enough.
   - Dependencies: none · Risk: Low
2. **Export / import scripts** (Files: `tools/localization/*.py`)
   - Action: export the missing keys per language as JSON for translation; the import validates placeholders and writes the catalogs.
   - Dependencies: step 1 · Risk: Low
3. **Glossary** (File: `docs/localization-glossary.md`)
   - Action: decide per language: Harmony, Garden Run, Floor, Pencil, Boss gate, relic and item names, class names (translate or keep), sudoku variants (Killer, Kropki, Thermo, Arrow, Anti-Knight, ...).
   - Dependencies: none · Risk: Medium (the names are what players read most)
4. **Parity test** (File: `Assets/Scripts/Tests/EditMode/LocalizationParityTests.cs`)
   - Action: (a) every German key exists in fr/es/pt, (b) placeholder sets match, (c) no empty values, (d) text identical to English only where allowlisted (proper names). It starts `[Explicit]` and becomes mandatory with step 7.
   - Requirement: *proposed* `LOC-PARITY-001..003` in `docs/localization-catalog.md`
   - Dependencies: none · Risk: Low

### Phase 2: Translation
5. **AI pass per language** (Files: catalogs)
   - Action: translate by category (Narrative, Relics, Items, Bosses, Classes, Harmony, UI strings) with glossary, German as a second reference and the length hint; import with validation.
   - Dependencies: steps 2, 3 · Risk: Medium (tone, game terms)
6. **Native review**
   - Action: one reviewer per language checks the exported pairs and in-game screenshots. Options: volunteers from the Discord "Translator" role (`docs/community/discord-server-setup.md`), or paid proofreading (~2,750 words per language).
   - Dependencies: step 5 · Risk: Medium (availability)
7. **UI fit pass**
   - Action: play in each language (or capture screenshots in a build): tooltips, boss gate descriptions, narrative panels, class screen. Fix overflow with a shorter text or a size change.
   - Dependencies: step 5 · Risk: Medium (French and Portuguese run 15–30 % longer than English)

### Phase 3: Release
8. **Close the gate**
   - Action: make `LocalizationParityTests` mandatory; update `docs/development.md` (counts), FUTURE-014, PRG-LOCALIZATION → READY; in Steamworks mark fr/es/pt with full interface support; remove the known issue from the notes.
   - Dependencies: steps 4–7 · Risk: Low

## Testing Strategy
- EditMode: `LocalizationParityTests`, the existing catalog quality tests, `GameplayContentServices_*` extended to fr/es/pt (no missing-key diagnostics).
- Build (E2E): screenshot pass per language of the main menu, class select, path map, boss gate, an item and a relic tooltip, the end screen.

## Risks & Mitigations
- **Quality of the AI pass** — glossary plus native review before release; the parity test only proves completeness, not quality.
- **Text overflow** — the length hint during translation, the screenshot pass after it.
- **Catalog drift** (new German keys without fr/es/pt) — the parity test fails the build once mandatory.

## Success Criteria
- [ ] 0 missing keys in fr/es/pt; placeholder parity green.
- [ ] Native review signed off per language.
- [ ] Screenshot pass without overflow; PRG-LOCALIZATION READY.

---

# Implementation Plan 3: Full-length music

## Overview
All 14 music files are 6-second mono loops, so the music repeats constantly. Replace them with full-length stereo loops as the existing production guide describes, make the boss layer start in sync with the floor track, and close the listening check.

## Requirements
- Lengths as in `docs/AudioVisualDirection.md` §Music Production Guide: menu ~100 s, floors 120–160 s, boss layers the same length and tempo as their floor track, shop/rest 60–90 s; stereo, 44.1 kHz.
- Seamless loop points (on a bar line, at zero crossings), consistent loudness against the SFX.
- The boss layer is additive and stays in sync with the floor track.

## Facts (2026-10-14)
- `Assets/Resources/audio/music/`: 14 WAV files, each 529,244 bytes = 6.0 s mono 44.1 kHz (menu, puzzle, shop, rest, five floors, five boss layers).
- `RunAudioController` starts the boss layer with `_bossSource.Play()` from position 0 (`RunAudioController.cs:330`), not at the floor track's position. With 6-second loops nobody hears it; with two-minute loops the layer would be out of time with the floor track.
- `AudioClipPlayModeTests.ReleaseClips_LoadWithAudioData` requires at least 5 s.
- Agents cannot hear audio: the listening check is the owner's.

## Owner decision: where the music comes from
| Source | Quality | Cost | Notes |
|---|---|---|---|
| Commissioned composer | Highest, unique | Highest | Brief exists (production guide); deliver stems for the boss layers |
| Licensed library tracks | Good, not unique | Low to medium | Needs a game-use license; boss layers hard to match |
| AI music generation | Varies | Low | Check the tool's commercial license; disclose in the Steam content survey and in the in-game AI notice |
| Procedural `FloorMusicGenerator`, rendered offline | Limited | None | Fast fallback; quality ceiling |

The technical steps below are the same for every source.

## Implementation Steps

### Phase 1: Specification and code
1. **Asset spec** (File: `docs/AudioVisualDirection.md` → 1.6)
   - Action: per track length, stereo, loop rule, loudness target (about −18 LUFS integrated for music beds, true peak ≤ −1 dBTP), file format (WAV masters outside the repo or in Git LFS; OGG or WAV in `Resources`), Unity import settings.
   - Requirement: *proposed* `AUDIO-MUSIC-001..004` (length and stereo, loop seam, boss-layer sync, import settings)
   - Dependencies: owner decision on the source · Risk: Low
2. **Import settings** (Files: the `.meta` of each music clip)
   - Action: Load Type Streaming, Compression Vorbis (quality about 70), Preload Audio Data off; SFX unchanged.
   - Why: two-minute stereo clips would otherwise sit decompressed in memory.
   - Dependencies: new files · Risk: Low
3. **Boss-layer sync** (File: `Assets/Scripts/UI/RunAudioController.cs`)
   - Action: start the boss layer at the floor track's position (`_bossSource.timeSamples = _musicSource.timeSamples % bossClip.samples`, started with `PlayScheduled` on the next DSP tick) with a short fade-in; log a warning if the floor and boss clip lengths differ.
   - Dependencies: none (can be done now) · Risk: Low

### Phase 2: Assets
4. **Produce or license the 14 loops** to the asset spec; render boss layers against their floor track.
5. **Replace the files** under `Assets/Resources/audio/music/` with the same names, so `AudioAssetService.RequiredClips` needs no change.

### Phase 3: Verification
6. **Tests**
   - PlayMode `AudioClipPlayModeTests`: music clips ≥ 60 s and 2 channels; each boss layer has the same sample count as its floor track.
   - EditMode: the import-settings test reads each music clip's `AudioImporter` (streaming, Vorbis).
   - PlayMode `RunAudioController`: after the boss layer starts, its `timeSamples` is within one DSP buffer of the floor track's.
7. **Listening checklist** (owner): loop seams per track, levels against SFX, boss-layer blend, menu music style option, volume sliders, mute, mute when unfocused. Closes GAP-030 and PRG-AUDIO.
8. **Build size check**: expect roughly +15–25 MB with Vorbis; record it in the E2E report.

## Risks & Mitigations
- **Licensing** — keep the license or contract next to the masters; credit in `docs/legal/`.
- **Loudness mismatch with the SFX** — one loudness target for all music; check during the listening pass.
- **Download size** — Vorbis plus streaming; masters stay out of the build.

## Success Criteria
- [ ] 14 loops at the spec length, stereo, seamless.
- [ ] Boss layer in sync (test green, heard in the listening check).
- [ ] GAP-030 FIXED, PRG-AUDIO READY, the known issue removed from the notes.
