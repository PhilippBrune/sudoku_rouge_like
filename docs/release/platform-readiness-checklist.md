# Platform Readiness Checklist — 1.1.0

Status: **release candidate (owner actions open)** · Updated 2026-10-07

Gate states and evidence: `docs/release/platform-release-manifest.md`.

## Engineering (repository)

- [x] Steamworks.NET and the Steam API redistributables are bundled and credited (PRG-STEAM-SDK).
- [x] Unity Analytics packages are not present. The build treats privacy/analytics as a closed gate with an analytics-free baseline (PRG-PRIVACY).
- [x] EditMode and PlayMode suites pass in Unity batchmode (`tools/run-unity-tests.ps1`); results in `docs/release/release-readiness-status.md`.
- [x] Version set to 1.1.0 (`ProjectSettings/ProjectSettings.asset`).
- [x] Windows x64 player build and smoke test recorded (PRG-PLATFORM-BUILDS): `docs/review/E2E_TEST_REPORT.md` §10, B01–B15. Rebuild from the committed tree after the PRG-PLAYER-METADATA decision.
- [ ] **Confirm company/product names** (PRG-PLAYER-METADATA): the working tree uses `Kratossos` / `sudoku_rouge_like`, the last commit `DefaultCompany` / `RunOfTheNine`. Use the names the 1.0 Steam build shipped with, or players' saves (and the Steam launch option for the exe) will not be found.

## Steamworks portal and Steam client (owner)

- [ ] Real App ID in `SteamManager`; `steam_appid.txt` (480, development only) not in the depot (PRG-STEAM-APPID).
- [ ] Platform achievements are not release-verified. Check unlocks in a Steam client build (PRG-ACHIEVEMENTS).
- [ ] Stats and the "Harmony_RunScore" leaderboard exist in the portal (PRG-STATS-LEADERBOARD).
- [ ] Cloud save is not configured; local saves only. Configure Auto-Cloud for `save_profile_*.json` or leave cloud save off the store page (PRG-CLOUD-SAVE).
- [ ] Store/platform art package is missing from the repository by design; review the portal assets for 1.1 (PRG-STORE-ASSETS, `docs/release/store-platform-asset-checklist.md`).
- [ ] Decide how to present French, Spanish and Portuguese (PRG-LOCALIZATION).
- [ ] Listening checklist (PRG-AUDIO).
- [ ] Upload the build to a beta branch, test it in the Steam client, then set it live.

## Upload to Steam (SteamPipe), when the gates above are closed

Nothing has been uploaded. Steps for the owner:

1. Commit the 1.1 change set (grouping: `docs/repository-working-tree-triage.md`) and build Windows x64 from that commit with the Editor closed: `Unity.exe -batchmode -quit -projectPath <repo> -buildTarget Win64 -buildWindows64Player <out>\<product>.exe -logFile <log>`.
2. Check the output folder: `<product>.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `<product>_Data/` (with `Plugins/x86_64/steam_api64.dll`), `MonoBleedingEdge/`, `D3D12/`. Do not ship `steam_appid.txt` or any `*_DoNotShip` folder.
3. Copy the folder into the SteamPipe content root (`sdk/tools/ContentBuilder/content/`), set the build description (for example "1.1.0") in the `app_build_<APPID>.vdf`, and point `SetLive` at a beta branch, not `default`.
4. Run `steamcmd +login <builder account> +run_app_build <path>pp_build_<APPID>.vdf +quit`.
5. In a Steam client, opt into the beta branch and repeat the smoke test of `E2E_TEST_REPORT.md` §10 with a real 1.0 save (resume, Save & Quit, achievements, stats, leaderboard, cloud if configured).
6. Set the build live on `default` in the Steamworks build page, and post `CHANGELOG.md` 1.1.0 (player-facing part only) as the update announcement.

## Notes

- The executable has no custom Windows icon (`m_BuildTargetIcons: []`); Steam uses the library icon from the portal.
- Saves from 1.0 load in 1.1 (`docs/SaveLoadArchitecture.md` §Compatibility).
