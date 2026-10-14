# Platform Readiness Checklist — 1.1.0

Status: **release candidate (owner actions open)** · Updated 2026-10-14

Gate states and evidence: `docs/release/platform-release-manifest.md`.

## Engineering (repository)

- [x] Steamworks.NET and the Steam API redistributables are bundled and credited (PRG-STEAM-SDK).
- [x] Unity Analytics packages are not present. The build treats privacy/analytics as a closed gate with an analytics-free baseline (PRG-PRIVACY).
- [x] EditMode and PlayMode suites pass in Unity batchmode (`tools/run-unity-tests.ps1`); results in `docs/release/release-readiness-status.md`.
- [x] Version set to 1.1.0 (`ProjectSettings/ProjectSettings.asset`).
- [x] Windows x64 player build and smoke test recorded (PRG-PLATFORM-BUILDS): `docs/review/E2E_TEST_REPORT.md` §10.
- [x] Real App ID 4858470 in `SteamManager`; `steam_appid.txt` (4858470) stays in the repository root for the Editor and local builds and is not in the build output (PRG-STEAM-APPID).
- [x] Company/product names match Steamworks: `Kratossos` / `Run of the Nine` (PRG-PLAYER-METADATA). Earlier save folders are copied once (SAVE-MIGRATE-001).

## Steamworks portal and Steam client (owner)

- [ ] Installation → General Installation: the launch option's executable is `RunOfTheNine.exe` (PRG-PLAYER-METADATA).
- [ ] Platform achievements are not release-verified. Check unlocks in a Steam client build (PRG-ACHIEVEMENTS).
- [ ] Stats and the "Harmony_RunScore" leaderboard exist in the portal (PRG-STATS-LEADERBOARD).
- [ ] Cloud save is not configured; local saves only. Configure Auto-Cloud for `save_profile_*.json` or leave cloud save off the store page (PRG-CLOUD-SAVE).
- [ ] Store/platform art package is missing from the repository by design; review the portal assets for 1.1 (PRG-STORE-ASSETS, `docs/release/store-platform-asset-checklist.md`).
- [ ] Decide how to present French, Spanish and Portuguese (PRG-LOCALIZATION).
- [ ] Listening checklist (PRG-AUDIO).
- [ ] Upload the build to a beta branch, test it in the Steam client, then set it live.

## Steamworks settings for App 4858470 (Run of the Nine)

Values the game expects; enter them in the Steamworks partner site (`partner.steamgames.com/apps/landing/4858470`).

**Installation → General Installation**
- Launch option 1: executable `RunOfTheNine.exe`, operating system Windows, 64-bit.

**Stats & Achievements → Stats** (type INT, set by game, increment only)
| API name | Display name |
| --- | --- |
| `stat_puzzles_solved` | Puzzles solved |
| `stat_bosses_defeated` | Bosses defeated |
| `stat_runs_completed` | Runs completed |

**Stats & Achievements → Achievements** (API names must match `SteamAchievementService`; icons in `steam_assets/achievements/`, achieved and unachieved 256×256 JPG)
| API name | Display name | Description | Hidden |
| --- | --- | --- | --- |
| `harmony_1_win` | First Harmony | Win a Garden Run at Harmony 1 or higher. | no |
| `harmony_3_win` | Deep Roots | Win a Garden Run at Harmony 3 or higher. | no |
| `harmony_5_win` | Halfway to Void | Win a Garden Run at Harmony 5 or higher. | no |
| `harmony_7_win` | Beyond the Garden | Win a Garden Run at Harmony 7 or higher. | no |
| `harmony_9_win` | Borderless Garden | Win a Garden Run at Harmony 9 or higher. | no |
| `harmony_10_win` | Void Garden | Win a Garden Run at Harmony 10. | no |
| `harmony_10_perfect` | Void Perfection | Win at Harmony 10 without a mistake. | yes |
| `harmony_all_classes` | Master of Harmony | Win at Harmony 5 or higher with all 8 classes. | yes |

**Stats & Achievements → Leaderboards**
- `Harmony_RunScore`: sort method Descending, display type Numeric, writes by the game (score = XP of a won run, best kept).

**Steam Cloud** (optional, PRG-CLOUD-SAVE): Auto-Cloud root `WinAppDataLocalLow`, subdirectory `Kratossos/Run of the Nine`, pattern `save_profile_*.json`, OS Windows; quota at least 5 MB and 10 files.

**Store Page Admin → Trailers / Screenshots**: upload `steam_assets/Trailer_1.1.mp4` with `Trailer_1.1_thumbnail.jpg`, retire the 1.0 trailer, and replace screenshots that show the 1.0 boss gate, digit styles, toggles or path map (`docs/release/store-platform-asset-checklist.md`).

After editing stats, achievements or leaderboards, publish the changes (Publish tab), or the game cannot see them.

## Upload to Steam (SteamPipe), when the gates above are closed

Nothing has been uploaded. Steps for the owner:

1. Build Windows x64 from the committed tree with the Editor closed: `Unity.exe -batchmode -quit -projectPath <repo> -buildTarget Win64 -buildWindows64Player <out>\RunOfTheNine.exe -logFile <log>`.
2. Check the output folder: `RunOfTheNine.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `RunOfTheNine_Data/` (with `Plugins/x86_64/steam_api64.dll`), `MonoBleedingEdge/`, `D3D12/`. Do not ship `steam_appid.txt` or any `*_DoNotShip` folder.
3. Copy the folder into the SteamPipe content root (`sdk/tools/ContentBuilder/content/`), set the build description (for example "1.1.0") in `app_build_4858470.vdf`, and point `SetLive` at a beta branch, not `default`.
4. Run `steamcmd +login <builder account> +run_app_build <path to app_build_4858470.vdf> +quit`.
5. In a Steam client, opt into the beta branch and repeat the smoke test of `E2E_TEST_REPORT.md` §10 (resume, Save & Quit, achievements, stats, leaderboard, cloud if configured).
6. Set the build live on `default` in the Steamworks build page, and post `CHANGELOG.md` 1.1.0 (player-facing part only) as the update announcement.

## Notes

- The executable has no custom Windows icon (`m_BuildTargetIcons: []`); Steam uses the library icon from the portal.
- Saves from 1.0 load in 1.1 (`docs/SaveLoadArchitecture.md` §Compatibility); saves from the Steam review build (`LocalLow\DefaultCompany\RunOfTheNine`) and 1.1 development builds are copied once (SAVE-MIGRATE-001).
- To run a local build outside Steam, put a copy of `steam_appid.txt` next to the executable; otherwise `RestartAppIfNecessary` hands the launch to Steam.
