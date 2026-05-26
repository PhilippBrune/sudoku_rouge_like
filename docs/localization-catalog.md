# Localization Catalog Workflow

## Runtime Source Order

`LocalizationService` loads JSON catalogs from:

```text
Assets/Resources/Localization/en.json
Assets/Resources/Localization/de.json
```

External catalog entries take priority. The existing C# dictionaries remain as
a fallback while localization is migrated in batches, so missing or malformed
catalog files do not blank release-facing UI.

## Current Catalog Scope

The initial catalogs were generated from the current in-code dictionaries:

- `en.json`: English entries that already existed in `LocalizationService.En`.
- `de.json`: German entries that already existed in `LocalizationService.De`.

Many English strings still use call-site fallbacks such as
`LocalizationService.T(key, englishFallback)`. Those should be migrated into
`en.json` in later batches when the copy is stable.

## Editing Rules

1. Add or update the English entry in `en.json`.
2. Add or update the German entry in `de.json`.
3. Keep the key name stable once it is used by code or save-facing UI.
4. Run the localization tests after catalog edits.
5. Keep code fallbacks until the catalog has complete parity for that subsystem.

## Validation

Run the Unity EditMode tests through the normal test entrypoint:

```powershell
.\tools\run-unity-tests.ps1 -TestPlatform EditMode
```

Fast compile-level checks:

```powershell
dotnet build sudoku_rouge_like.slnx --verbosity minimal
dotnet test GameTests.EditMode.csproj --verbosity minimal
```

The current EditMode localization tests validate that the catalog files exist,
parse as JSON dictionaries, and contain release-critical credit/gameplay keys.
`ReleaseFacingGermanLocalizationReport_IsComplete` is the consolidated release
gate: it aggregates credits, first-run setup, menu flow, options/accessibility,
Sudoku basics, generated menu keys, gameplay content services, boss modifiers,
relics, items, and narrative keys, then fails with a sorted missing-key report
if German coverage is incomplete.

`GeneratedUiLocalizationCoverageTests` also scans generated menu/UI construction
calls in `MainMenuBlueprintBuilder`, `InRunUiBlueprintBuilder`, and
`MainMenuController`. New direct raw
visible strings passed to `BuildText`, `BuildButton`, `BuildToggle`,
`CreateText`, or `SetStatus` fail the test unless they are:

- non-localizable UI chrome such as empty text or arrow glyphs
- an approved literal brand title
- listed in the test's known-debt allowlist for a later localization batch

When a localization batch replaces one of those literals with `T(...)` or
`LocalizationService.Format(...)`, remove the corresponding known-debt entry
from the test in the same change.

`LocalizationCatalogQualityTests` provides translator-facing catalog QA:

- duplicate keys are rejected in both `en.json` and `de.json`;
- German strings must preserve the same numeric `string.Format` placeholders as
  their English source strings;
- count-dependent player-facing copy should use `LocalizationService.Plural`
  or `LocalizationService.FormatPlural` with explicit singular and plural keys
  instead of inline `item(s)` / `cell(s)` wording;
- this workflow document must continue to describe duplicate keys and
  placeholder parity so translation batches have clear review rules.

## Migration Plan

1. Keep the external catalog loader enabled with code fallback.
2. Move stable subsystem copy from call-site fallbacks into `en.json`.
3. Keep German parity in `de.json` for every moved key.
4. Add subsystem key-list tests before removing corresponding code fallback
   entries.
5. After all release-facing text is catalog-backed, reduce the C# dictionaries
   to emergency fallback entries only.
6. Use `GeneratedUiLocalizationCoverageTests` as the generated-UI backlog:
   localize allowlisted raw literals in small panel-focused batches and delete
   each allowlist entry as it is resolved.
7. When adding release-facing text, either add it to an existing subsystem key
   provider such as `ItemService.GetLocalizationKeys()` or add it to the
   consolidated release-facing key report in
   `ModifierDiscoveryPersistenceTests`.
