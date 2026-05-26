# Asset Provenance Register

## Purpose

This register is the legal and production source of truth for runtime assets in `Assets/Resources/`. It records what is currently known about generated, authored, reserved, and future third-party assets.

This file does not replace the credits. It supports them by tracking asset origin, ownership status, and release readiness at a folder/category level. Individual exceptions or third-party replacements must be added as separate rows before release.

## Current Runtime Asset Inventory

Observed on 2026-05-11:

| Runtime path | Current files | Category | Current provenance | Release status |
| --- | ---: | --- | --- | --- |
| `Assets/Resources/background/` | 39 PNG | Background art | Generated/curated by Philipp Brune with Fooocus/AI-assisted prompt workflow. Prompt references are stored outside runtime Resources. | Needs final art review before release. |
| `Assets/Resources/boss/` | 5 PNG | Boss icons/art | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/class/` | 8 PNG | Class portraits/icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/cursed/` | 18 PNG | Curse icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/debuff/` | 9 PNG | Debuff icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/economy/` | 8 PNG | Economy/UI icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/GeneratedIcons/` | 1 CSV | Generated icon map | Project-authored metadata mapping generated icon output. | Keep if runtime-required; otherwise move outside `Resources`. |
| `Assets/Resources/items/` | 41 PNG | Item icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/legendary/` | 6 PNG | Legendary item/relic art | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/meta/` | 10 PNG | Meta progression icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/modifier/` | 99 PNG | Boss modifier icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/node/` | 10 PNG | Run-map node icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/relic/` | 29 PNG | Relic icons | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/ui/` | 29 PNG | UI icons/art | Generated/curated visual assets directed by Philipp Brune. | Needs final art review. |
| `Assets/Resources/audio/` | 49 WAV | Music, SFX, and UI audio | Project-owned procedural synthesis generated as authored candidate source audio. See `docs/audio-asset-pipeline.md`. | Candidate pack present; needs listening, mix, import, and platform compression validation before release. |
| `Assets/Resources/Fonts/` | 1 TTF | Production UI font | Roboto Regular by Google, licensed under Apache License 2.0. Notice: `docs/legal/third-party/Roboto-NOTICE.md`. See `docs/font-asset-pipeline.md`. | Candidate production font present; needs Unity import, layout, and upstream binary validation before release. |
| `Assets/Resources/BillingMode.json` | 1 JSON | Runtime config | Project-authored configuration. | Keep if runtime-required. |

Unity `.meta` files are excluded from asset counts but are required for Unity import identity.

## Prompt And Source References

Known prompt/reference files have been moved out of runtime `Resources` and currently live under `docs/art-source/background/`:

- `docs/art-source/background/AI_prompts.txt`
- `docs/art-source/background/AI_prompts_2.txt`
- `docs/art-source/background/background_generation_prompts.txt`

These files are source/provenance references, not player-facing runtime assets. Keep future prompt archives outside `Assets/Resources` unless runtime loading is explicitly required and documented.

## Non-Runtime Reserved Art

`Assets/_ReservedArt/` contains reserve/source art that is intentionally not loaded through `Resources`. Its current file-level classification is maintained in `docs/reserved-art-manifest.md`.

Reserved art must stay outside runtime loading paths until the owning gameplay content, localization, and release provenance are updated in the same change.

## Ownership And License Position

Current declared position:

- Game design, art direction, prompt direction, curation, and final selection: Philipp Brune.
- AI-assisted visual generation tool currently credited: Fooocus.
- Roboto Regular is bundled as `Assets/Resources/Fonts/MainFont.ttf` under Apache License 2.0. Notice: `docs/legal/third-party/Roboto-NOTICE.md`.
- Runtime audio WAV files under `Assets/Resources/audio/` are project-owned generated source assets, not third-party stock audio.
- Future third-party, contractor, stock, font, audio, store, or platform assets must be added to this register with source, author, license, and file path before release.

## Required Fields For Future Individual Assets

When adding an individual exception or third-party asset, add a row with:

| Field | Required content |
| --- | --- |
| Asset path | Exact repository path. |
| Category | Background, icon, UI, audio, font, VFX, marketing, platform, source, or config. |
| Source | Generated, authored in-house, third-party, contractor, stock, platform SDK, or unknown. |
| Tool/source reference | Tool name, prompt file, source URL, invoice, contract, or bundled license file. |
| Author/owner | Person, company, or project owner. |
| License | Commercial in-house, Apache 2.0, MIT, proprietary SDK, stock license, custom contract, or other. |
| Release status | Prototype, alpha, final, replace before release, or do not ship. |
| Notes | Attribution wording, restrictions, or validation needs. |

## Validation Policy

`AssetProvenanceRegisterTests` validates that runtime files in `Assets/Resources/` belong to registered roots and use expected extensions. It also checks that known background prompt files are explicitly accounted for. `ReservedArtManifestTests` validates the non-runtime reserved-art manifest.

This is not a full legal review. It is a guardrail against silent asset drift.
