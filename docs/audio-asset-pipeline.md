# Audio Asset Pipeline

## Current Status

The game now includes an authored candidate audio pack under
`Assets/Resources/audio/`. The clips were generated from the project-owned
procedural synthesis direction and committed as WAV files matching every
release-required `AudioAssetService` resource path.

Runtime audio still keeps its fallback behavior because `AudioAssetService`
first attempts to load authored clips from `Assets/Resources/audio/` and then
falls back to the existing procedural generators if a clip is missing:

- Music fallback: `FloorMusicGenerator`
- SFX/UI fallback: `ProceduralSfxLibrary`
- Runtime consumers: `RunAudioController`, `MenuMusicController`
- Menu music style selection: `AudioSettingsModel.MenuMusicStyleIndex` maps to
  Garden Theme, Bamboo Courtyard, and Rest Garden through `AudioAssetService`.

This keeps prototype and alpha builds functional while creating a stable
contract for future replacement or remastering.

## Folder Contract

Authored audio clips should be imported below:

- `Assets/Resources/audio/music/`
- `Assets/Resources/audio/sfx/`
- `Assets/Resources/audio/ui/`

Unity `Resources.Load<AudioClip>` paths are defined in `AudioAssetService`. File extensions must not be included in code. For example, the path `audio/ui/button_click` should resolve to an imported clip such as:

```text
Assets/Resources/audio/ui/button_click.wav
Assets/Resources/audio/ui/button_click.ogg
```

## Required Clip IDs

The current production audio contract is the `AudioClipId` enum in `Assets/Scripts/UI/AudioAssetService.cs`. Every enum value must have a matching `AudioAssetInfo` entry with:

- a stable Resources path
- category: `Music`, `Sfx`, or `Ui`
- loop flag
- release-required flag
- usage description

EditMode tests validate that every `AudioClipId` is represented, that Resources
paths remain stable, and that every release-required entry has an authored audio
file plus Unity `.meta` file.

## Replacement Workflow

1. Create, license, or internally generate final audio.
2. Add the clip under the matching folder in `Assets/Resources/audio/`.
3. Name the file to match the `AudioAssetInfo.ResourcePath` leaf name.
4. Configure Unity import settings:
   - Music loops: compressed in memory or streaming, depending on clip length.
   - Short SFX/UI clips: decompress on load for low latency.
   - Normalize source files before import; avoid runtime volume fixes where possible.
5. Confirm the clip loops cleanly when `AudioAssetInfo.Loop` is true.
6. Add attribution to `docs/Credits.md` before release if any third-party or contractor asset is used.
7. Run EditMode/PlayMode tests and a Unity runtime audio pass.

## Fallback Behavior

If an authored clip is missing, `AudioAssetService.GetClip` uses the supplied procedural fallback factory. This is intentional for development builds and prevents silent gameplay when the production asset library is incomplete.

Release candidates should treat missing authored clips as a content-readiness
issue unless procedural audio is explicitly approved as the final shipped
direction.

## Authored Candidate Pack

Current candidate pack:

- Location: `Assets/Resources/audio/`
- Format: WAV, mono, 44.1 kHz, 16-bit PCM source files
- Count: 49 clips
- Provenance: project-owned procedural synthesis generated for this repository
- Third-party audio: none

Music clips are short loop candidates intended for Unity import/compression and
listening validation. SFX/UI clips are one-shot candidates intended to replace
runtime silence and provide stable authored assets for build validation.

Before beta/release, run a listening and mix pass for:

- loop clicks and musical repetition
- volume consistency across SFX/UI/music
- mobile/PC speaker translation
- Unity import settings by platform
- final creative approval against the garden audio direction

## Validation Checklist

- `dotnet build sudoku_rouge_like.slnx --verbosity minimal`
- `dotnet test GameTests.EditMode.csproj --verbosity minimal`
- Unity EditMode tests through `tools/run-unity-tests.ps1 -TestPlatform EditMode`
- Unity PlayMode tests through `tools/run-unity-tests.ps1 -TestPlatform PlayMode`
- Manual runtime pass:
  - main menu loop starts and stops correctly
  - puzzle/path/shop/rest contexts switch clips correctly
  - floor music changes without silence
  - boss layer starts and stops
  - all UI feedback sounds play
  - settings sliders affect master, music, SFX, and UI volume in real time

## Open Content Dependency

The repository now has authored candidate clips, but final release readiness
still depends on creative listening approval, mix balancing, Unity import
verification, and platform compression review.
