# Store & Platform Asset Checklist — 1.1.0

Updated 2026-10-07. Store art lives in the Steamworks portal, not in this repository. Each item is reviewed for 1.1; anything the 1.1 UI changed visibly needs new captures. Gate: PRG-STORE-ASSETS in `docs/release/platform-release-manifest.md`.

| ID | Deliverable | Where | 1.1 action | State |
|----|-------------|-------|-----------|-------|
| ASSET-001 | Steam capsule images (header, small, main, vertical) | Steamworks portal | Review; no change required by 1.1 | OWNER_ACTION |
| ASSET-002 | Library art: library hero, library logo and library capsule | Steamworks portal | Review | OWNER_ACTION |
| ASSET-003 | Store app icon and community icon | Steamworks portal | Review; the executable itself has no custom icon | OWNER_ACTION |
| ASSET-004 | Screenshots | Steamworks portal | Replace shots showing the old boss gate, digit styles, toggles or path map (1.1 changed them) | RELEASE_BLOCKER |
| ASSET-005 | Trailer | Steamworks portal | Optional update; current trailer predates 1.1 | OWNER_ACTION |
| ASSET-006 | In-game controller glyph set (Steam Deck / Xbox face labels) | In-game (`ControllerGlyphService`) | Verify glyphs in a controller session | OWNER_ACTION |
| ASSET-007 | Store description and patch notes | Steamworks portal | Publish `CHANGELOG.md` 1.1.0 as the update announcement | OWNER_ACTION |

The repository deliberately contains no `store_capsule.png`, `store_header.png`, `library_hero.png`, `app_icon.png` or `controller_glyphs.png`; `StorePlatformAssetReadinessTests` checks that, so this checklist stays the single record.
