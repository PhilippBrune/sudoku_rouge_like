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
- [x] Company/product names stay `Kratossos` / `sudoku_rouge_like`, the names of the released 1.0 build, so saves stay in the 1.0 folder that Steam Auto-Cloud syncs (PRG-PLAYER-METADATA, SAVE-FOLDER-001). The window is titled "Run of the Nine" (DISPLAY-MODE-007). Pre-release saves are copied once (SAVE-MIGRATE-001).

## Steamworks portal and Steam client (owner)

Portal state checked on 2026-10-14 (App Data Admin → Publish: "No uncommitted app data").

- [x] Installation → General Installation: the launch option runs `sudoku_rouge_like.exe` (Windows, 64-bit), which is the name of the 1.1 build too (PRG-PLAYER-METADATA).
- [ ] Platform achievements are not release-verified. The eight achievements exist and their API names match the game; check unlocks in a Steam client build (PRG-ACHIEVEMENTS).
- [ ] Stats and the "Harmony_RunScore" leaderboard exist and are published; check that a run end updates them in a Steam client build (PRG-STATS-LEADERBOARD).
- [x] Steam Auto-Cloud is configured for the save files (PRG-CLOUD-SAVE). In-game status: Cloud save is not configured; local saves only.
- [ ] Store/platform art package is missing from the repository by design; review the portal assets for 1.1 (PRG-STORE-ASSETS, `docs/release/store-platform-asset-checklist.md`). The 1.1 trailer is uploaded and saved as "Gameplay Trailer"; publish the store page, decide whether the 1.0 trailer stays visible, and replace outdated screenshots.
- [ ] Decide how to present French, Spanish and Portuguese (PRG-LOCALIZATION).
- [ ] Listening checklist (PRG-AUDIO).
- [ ] Decide the achievement names: the live names differ from HarmonyDifficultySystem §9 (SPEC-CONF-040).
- [ ] Upload the build to a beta branch, test it in the Steam client, then set it live.

## Steamworks settings for App 4858470 (Run of the Nine), as configured

Live values read from the Steamworks partner site (`partner.steamgames.com/apps/landing/4858470`) on 2026-10-14. The 1.1 build needs no change to them.

**Installation → General Installation**
- Install folder "Run of the Nine". Launch option 0: executable `sudoku_rouge_like.exe`, operating system Windows, 64-bit.

**SteamPipe → Builds**
- Branch `default` runs build 24153478 (10 Jul 2026, "4th iteration fixes"), depot 4858471 "Run of the Nine Content": `sudoku_rouge_like.exe`, `sudoku_rouge_like_Data\app.info` = Kratossos / sudoku_rouge_like (the save folder of 1.0).

**Stats & Achievements → Stats** (type INT, set by the client)
| API name |
| --- |
| `stat_puzzles_solved` |
| `stat_bosses_defeated` |
| `stat_runs_completed` |

**Stats & Achievements → Achievements** (API names match `SteamAchievementService`; icons are set, achieved and unachieved)
| API name | Display name | Description | Hidden |
| --- | --- | --- | --- |
| `harmony_1_win` | First Awakening | Complete a Garden Run on Harmony Level 1 or higher. | no |
| `harmony_3_win` | Moss and Gravel | Complete a Garden Run on Harmony Level 3 or higher. | no |
| `harmony_5_win` | Water Reflection | Complete a Garden Run on Harmony Level 5 or higher. | no |
| `harmony_7_win` | Lantern Glow | Complete a Garden Run on Harmony Level 7 or higher. | no |
| `harmony_9_win` | Torii Gateway | Complete a Garden Run on Harmony Level 9 or higher. | no |
| `harmony_10_win` | Summit Ascent | Complete a Garden Run on Harmony Level 10. | no |
| `harmony_10_perfect` | Tranquil Perfection | Complete a Garden Run on Harmony Level 10 with zero mistakes. | yes |
| `harmony_all_classes` | Master of the Garden | Complete a Garden Run on Harmony Level 5 or higher with all 8 classes. | no |

**Stats & Achievements → Leaderboards**
- `Harmony_RunScore`: sort method Descending, display type Numeric, writes by the game (score = XP of a won run, best kept).

**Steam Cloud** (PRG-CLOUD-SAVE): Auto-Cloud root `WinAppDataLocalLow`, subdirectory `Kratossos/sudoku_rouge_like/`, pattern `save_profile_*.json`, OS Windows; quota 100 MB and 100 files.

**Store Page Admin → Trailers / Screenshots**: the 1.1 trailer (`steam_assets/Trailer_1.1.mp4`, thumbnail with the end card) is uploaded as "Gameplay Trailer", category Gameplay, before the 1.0 "Trailer". Store changes stay in store beta mode until published (Store Page Admin → Publish). Replace screenshots that show the 1.0 boss gate, digit styles, toggles or path map (`docs/release/store-platform-asset-checklist.md`).

Do not rename the Player Settings company or product, or the executable: the launch option, the Auto-Cloud path and every player's save folder depend on them (SAVE-FOLDER-001).

## Upload to Steam (SteamPipe), when the gates above are closed

Nothing has been uploaded. Steps for the owner:

1. Build Windows x64 from the committed tree with the Editor closed: `Unity.exe -batchmode -quit -projectPath <repo> -buildTarget Win64 -buildWindows64Player <out>\sudoku_rouge_like.exe -logFile <log>`.
2. Check the output folder: `sudoku_rouge_like.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `sudoku_rouge_like_Data/` (with `Plugins/x86_64/steam_api64.dll`), `MonoBleedingEdge/`, `D3D12/`. Do not ship `steam_appid.txt` or any `*_DoNotShip` folder.
3. Copy the folder into the SteamPipe content root (`sdk/tools/ContentBuilder/content/`), set the build description (for example "1.1.0") in `app_build_4858470.vdf` (depot 4858471), and point `SetLive` at a beta branch, not `default`.
4. Run `steamcmd +login <builder account> +run_app_build <path to app_build_4858470.vdf> +quit`.
5. In a Steam client, opt into the beta branch and repeat the smoke test of `E2E_TEST_REPORT.md` §10 (resume, Save & Quit, achievements, stats, leaderboard, Steam Cloud).
6. Set the build live on `default` in the Steamworks build page, and post `CHANGELOG.md` 1.1.0 (player-facing part only) as the update announcement.

## Notes

- The executable has no custom Windows icon (`m_BuildTargetIcons: []`); Steam uses the library icon from the portal.
- Saves from 1.0 load in 1.1 (`docs/SaveLoadArchitecture.md` §Compatibility) from the same folder; saves of the pre-release builds (`LocalLow\DefaultCompany\RunOfTheNine`) are copied once (SAVE-MIGRATE-001).
- To run a local build outside Steam, put a copy of `steam_appid.txt` next to the executable; otherwise `RestartAppIfNecessary` hands the launch to Steam.
