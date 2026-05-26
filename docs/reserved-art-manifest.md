# Reserved Art Manifest

## Purpose
This manifest classifies `Assets/_ReservedArt/` as non-runtime reserve/source art. These files are intentionally outside `Assets/Resources`, are not loaded through `Resources`, and should not be counted as missing runtime content.

## Current Files
| Path | Category | Runtime status | Provenance | Intended use | Release action |
| --- | --- | --- | --- | --- | --- |
| `Assets/_ReservedArt/legendary/icon_enlightenment_tree.png` | Legendary reserve icon | Non-runtime | Generated/curated visual asset directed by Philipp Brune | Candidate future legendary relic or replacement art | Keep outside `Resources` until assigned to a shipped relic and reviewed. |
| `Assets/_ReservedArt/legendary/icon_spirit_dragon_coin.png` | Legendary reserve icon | Non-runtime | Generated/curated visual asset directed by Philipp Brune | Candidate future legendary relic or replacement art | Keep outside `Resources` until assigned to a shipped relic and reviewed. |

Unity `.meta` files beside reserved art are retained for stable import identity if the files are later moved into a runtime folder.

## Rules
- Do not reference `_ReservedArt` from runtime code.
- Do not move reserved art into `Assets/Resources` unless the owning gameplay content, localization keys, and legal/provenance status are updated in the same change.
- If a reserved image is promoted to runtime content, remove it from this manifest or change its status to "promoted" with the new runtime path.
- If a reserved image is rejected, delete it only after verifying no production document still references it.

## Validation
`ReservedArtManifestTests` verifies that every non-meta file under `Assets/_ReservedArt` is listed here and that the legal provenance register links this manifest.
