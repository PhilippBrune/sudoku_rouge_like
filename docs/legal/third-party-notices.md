# Third-Party Notices

## Bundled Development Tools

### Google Mobile Dependency Resolver For Unity

- Repository path: `Assets/MobileDependencyResolver/Editor/`
- Version evidence: `Assets/MobileDependencyResolver/Editor/mobile-dependency-resolver_version-1.2.185_manifest.txt`
- License: Apache License 2.0
- License text: `Assets/MobileDependencyResolver/Editor/LICENSE`
- Release note: include this notice and license text in release legal materials if the bundled editor tooling remains in the distributed project.

## Engine And Packages

Unity packages are credited in `docs/Credits.md`. Verify final package list against `Packages/manifest.json` before release.

## Platform SDKs

### Steamworks.NET

- Repository path: `Assets/com.rlabrecque.steamworks.net/` (Steamworks.NET 2025.163.0)
- Author: Riley Labrecque
- License: MIT License
- License text: `Assets/com.rlabrecque.steamworks.net/LICENSE.md`, copy in `docs/legal/third-party/Steamworks.NET-LICENSE.md`

### Steamworks API redistributables (Valve Corporation)

- Files: `steam_api.dll`, `steam_api64.dll`, `libsteam_api.so`, `steam_api.bundle` under `Assets/com.rlabrecque.steamworks.net/Plugins/`
- Distributed under the Steamworks SDK Access Agreement (Valve Corporation).
- Steam and the Steam logo are trademarks and/or registered trademarks of Valve Corporation.

Updated 2026-10-07 (SPEC-PROC-007): the earlier statement that no Steamworks SDK binaries are bundled stopped being true when Steamworks.NET was committed (0826fec). See `docs/steam-platform-integration.md` for the integration path.

## Assets

Runtime asset provenance is tracked in `docs/legal/asset-provenance-register.md`.

### Roboto Regular

- Repository path: `Assets/Resources/Fonts/MainFont.ttf`
- Upstream project: https://github.com/googlefonts/roboto-2
- License: Apache License 2.0
- Notice: `docs/legal/third-party/Roboto-NOTICE.md`
- License text: `docs/legal/third-party/APACHE-2.0.txt`
- Release note: validate the exact shipped font binary against the intended upstream/vendor package before final commercial release.
