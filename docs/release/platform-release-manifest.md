# Platform Release Manifest — 1.1.0

**Release:** 1.1.0 (second Steam release) · **Updated:** 2026-10-07 · **Owner:** Philipp Brune

This manifest lists every release gate with its state, exit criteria and the repository evidence. States:

- `READY` — done and backed by repository evidence.
- `OWNER_ACTION` — the code side is done; a step in the Steamworks portal, the Steam client or a decision remains with the owner.
- `RELEASE_BLOCKER` — must be resolved before upload.
- `MILESTONE_DEFERRED` — intentionally out of scope for this release.

Public release state: blocked until every `RELEASE_BLOCKER` and `OWNER_ACTION` gate below is closed.
Store submission state: pending owner actions (portal configuration, store art review).
Steam runtime validation state: pending (manual check in a Steam client build).

| ID | Decision / evidence statement | State | Exit Criteria | Owner Area | Repository Evidence |
|----|-------------------------------|-------|---------------|------------|---------------------|
| PRG-STEAM-SDK | Steamworks.NET 2025.163.0 and the Steam API redistributables are bundled; `STEAMWORKS_NET` is defined for Standalone. | READY | SDK files present, notices in the credits (SPEC-PROC-007). | Engineering | `Assets/com.rlabrecque.steamworks.net/`, `ProjectSettings/ProjectSettings.asset` (`scriptingDefineSymbols`), `docs/legal/third-party-notices.md` |
| PRG-STEAM-APPID | `SteamManager` calls `RestartAppIfNecessary(new AppId_t(4858470))` (Run of the Nine, set 2026-10-14). `steam_appid.txt` in the repository root holds 4858470 for the Editor and local test builds; Unity does not copy it into the build, so it is not in the depot. A local build started outside Steam needs a copy next to the executable, or Steam restarts it. | READY | Real App ID set in `SteamManager`, `steam_appid.txt` not in the depot. | Engineering | `Assets/Scripts/Steamworks.NET/SteamManager.cs`, `steam_appid.txt` |
| PRG-ACHIEVEMENTS | Platform achievements are not release-verified. The bridge forwards local unlocks when Steam runs; `PlatformServiceRegistry` keeps reporting the blocker until a Steam client check is recorded. | OWNER_ACTION | Achievement IDs match the Steamworks backend; unlocks seen in a Steam client build. | Owner + QA | `SteamAchievementService`, `PlatformAchievementBridge`, `docs/steam-platform-integration.md` |
| PRG-STATS-LEADERBOARD | Three stats (`SteamStatsService`) and the "Harmony_RunScore" leaderboard (`SteamLeaderboardService`) must exist in the Steamworks portal. | OWNER_ACTION | Stats and leaderboard created; a run end updates them in a Steam client build. | Owner | `Assets/Scripts/Meta/SteamStatsService.cs`, `SteamLeaderboardService.cs` |
| PRG-CLOUD-SAVE | Save system is local-only. Cloud save is not configured; local saves only. Steam Auto-Cloud needs only portal settings: root `WinAppDataLocalLow`, subdirectory `Kratossos/Run of the Nine`, pattern `save_profile_*.json` (`docs/SaveLoadArchitecture.md` §Local-Only Scope). | OWNER_ACTION | Auto-Cloud paths set in the portal, or cloud save left out of the store page. | Owner | `docs/SaveLoadArchitecture.md`, `LocalOnlyCloudSaveProvider` |
| PRG-PRIVACY | No platform telemetry package is configured and no Unity Analytics package is present. | READY | `Packages/manifest.json` without analytics packages; privacy notice in the credits. | Engineering | `Packages/manifest.json`, `docs/legal/privacy-analytics-notice.md` |
| PRG-PLAYER-METADATA | Company `Kratossos`, product `Run of the Nine`, version 1.1.0: the developer/publisher and store name in Steamworks (App 4858470), set 2026-10-14. The window title is "Run of the Nine"; saves live in `%USERPROFILE%\AppData\LocalLow\Kratossos\Run of the Nine`, and `LegacySaveMigration` copies profiles from the earlier folders once (SAVE-MIGRATE-001). Build the executable as `RunOfTheNine.exe`. No default Windows executable icon is set (`m_BuildTargetIcons: []`). | OWNER_ACTION | Steamworks → Installation → General Installation: the launch option's executable is `RunOfTheNine.exe`. | Owner | `ProjectSettings/ProjectSettings.asset`, `Assets/Scripts/Save/LegacySaveMigration.cs` |
| PRG-STORE-ASSETS | Store capsule, header, library art, and platform icon package are managed in the Steamworks portal, not in this repository. | OWNER_ACTION | Every item in `docs/release/store-platform-asset-checklist.md` reviewed for 1.1 (new screenshots for the 1.1 UI). | Owner | `docs/release/store-platform-asset-checklist.md` |
| PRG-PLATFORM-BUILDS | Windows x64 player build and smoke test for 1.1.0: built from the 1.1 tree, smoke tests B01–B19 passed; rebuilt 2026-10-14 as `RunOfTheNine.exe` with the Steamworks names and App ID, checks B20–B22 passed (title, save migration, no `steam_appid.txt` in the output). | READY | Build created and the smoke test recorded in `docs/review/E2E_TEST_REPORT.md` §10. | Engineering | `docs/review/E2E_TEST_REPORT.md` |
| PRG-LOCALIZATION | English and German are complete; French, Spanish and Brazilian Portuguese lack 431 gameplay-content keys each (English fallback). | OWNER_ACTION | Translate the missing keys, or label the three languages as partial on the store page. | Owner | `docs/development.md` §Localization Catalogs |
| PRG-AUDIO | All release clips load (`AudioClipPlayModeTests`); every music loop is 6 s of mono audio. | OWNER_ACTION | Manual listening checklist passed; longer loops are optional. | Owner | `docs/AudioVisualDirection.md` |

Previous internal-validation gates (1.0 era) "Steamworks SDK binaries are not bundled", "Unity player settings still use development defaults" and "No target platform build validation is recorded" are closed or replaced by the gates above.
