# Font Asset Pipeline

## Current Status

The game now bundles a baseline production UI font at `Assets/Resources/Fonts/MainFont.ttf`.
UI text resolves through `FontAssetService`, which first attempts to load this production font
and then falls back to Unity built-in fonts if the asset is unavailable.

Current bundled font:

- Font: Roboto Regular
- Runtime file: `Assets/Resources/Fonts/MainFont.ttf`
- License: Apache License 2.0
- Notice: `docs/legal/third-party/Roboto-NOTICE.md`
- Bundled license text: `docs/legal/third-party/APACHE-2.0.txt`

Production font path:

```text
Assets/Resources/Fonts/MainFont.ttf
Assets/Resources/Fonts/MainFont.otf
```

Runtime `Resources.Load` path:

```text
Fonts/MainFont
```

## Runtime Contract

`FontAssetService.GetFont()` is the canonical font entrypoint for generated uGUI text.

Fallback order:

1. `Resources.Load<Font>("Fonts/MainFont")`
2. `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`
3. `Resources.GetBuiltinResource<Font>("Arial.ttf")`

This keeps builds functional with a fallback while giving the production font a single import location.

## Production Font Requirements

Before beta or release, the production font should remain:

- licensed for commercial game distribution
- credited in `docs/Credits.md`
- verified for English and German glyph coverage, including U+00E4, U+00F6, U+00FC, U+00C4, U+00D6, U+00DC, U+00DF, and common punctuation
- tested at normal font scale and 150% accessibility font scale
- reviewed in dense screens such as options, class select, item/relic panels, credits, tutorial, and end screens

## Import Checklist

1. Add or replace the licensed font file as `Assets/Resources/Fonts/MainFont.ttf` or `Assets/Resources/Fonts/MainFont.otf`.
2. Keep the matching `.meta` file committed for stable Unity asset identity.
3. Verify `FontAssetService.TryLoadProductionFont(out _)` returns true in EditMode or runtime.
4. Run static checks:
   - `dotnet build sudoku_rouge_like.slnx --verbosity minimal`
   - `dotnet test GameTests.EditMode.csproj --verbosity minimal`
5. Run Unity runtime checks for German and accessibility font scale.
6. Update credits with font name, author/source, license, and license URL or bundled license file.

## Remaining Release Risk

The font file is now present and covered by static tests for file presence and required German glyphs.
Release readiness still depends on Unity import validation, dense-screen layout checks, accessibility font
scale checks, and final confirmation that the bundled binary matches the intended upstream/vendor package.
