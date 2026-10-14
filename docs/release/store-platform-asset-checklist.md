# Store & Platform Asset Checklist — 0.1.5

Updated 2026-10-13. Store art sources are kept in `steam_assets/` (outside `Assets/`, so nothing ships in the build); the Steamworks portal holds the published versions. Each item is reviewed for 1.1; anything the 1.1 UI changed visibly needs new captures. Gate: PRG-STORE-ASSETS in `docs/release/platform-release-manifest.md`.

| ID | Deliverable | Where | 1.1 action | State |
|----|-------------|-------|-----------|-------|
| ASSET-001 | Steam capsule images (header, small, main, vertical) | Steamworks portal | Review; no change required by 1.1 | OWNER_ACTION |
| ASSET-002 | Library art: library hero, library logo and library capsule | Steamworks portal | Review | OWNER_ACTION |
| ASSET-003 | Store app icon and community icon | Steamworks portal | Review; the executable itself has no custom icon | OWNER_ACTION |
| ASSET-004 | Screenshots | Steamworks portal | Replace shots showing the old boss gate, digit styles, toggles or path map (1.1 changed them) | RELEASE_BLOCKER |
| ASSET-005 | Trailer | `steam_assets/Trailer_1.1.mp4` → Steamworks portal | New 1.1 trailer made 2026-10-13 (70 s, 1080p30, −14 LUFS; shot list and provenance in `steam_assets/Trailer_1.1.md`); upload it, set `Trailer_1.1_thumbnail.jpg`, retire the 1.0 trailer | OWNER_ACTION (upload) |
| ASSET-006 | In-game controller glyph set (Steam Deck / Xbox face labels) | In-game (`ControllerGlyphService`) | Verify glyphs in a controller session | OWNER_ACTION |
| ASSET-007 | Store description and patch notes | Steamworks portal | Publish the 0.1.5 release notes (`steam_assets/ReleaseNotes_v0.1.5.md`) as the update announcement | OWNER_ACTION |

`Assets/` deliberately contains no `store_capsule.png`, `store_header.png`, `library_hero.png`, `app_icon.png` or `controller_glyphs.png`, so no store art ships in the build; `StorePlatformAssetReadinessTests` checks that.
