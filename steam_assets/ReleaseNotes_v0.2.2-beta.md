# Release notes: v0.2.2 beta (8th iteration)

Playtest build made 2026-10-25 from `feature/game-ui-refactor` (tag `v0.2.2-beta`, local). Owner's request (2026-10-22): put it on the password beta branch, ask testers for two or three runs per sitting, and do the Steamworks content survey in the same visit. Player-facing text comes from `CHANGELOG.md` 0.2.2 beta. Not uploaded and not live.

## Build

- `Builds/RunOfTheNine_0.2.2-beta/` and `Builds/RunOfTheNine_0.2.2-beta_win64.zip` (54.3 MB, 157 files, made with Python `zipfile`; the `Builds/` folder is not in git). Same layout and names as the live depot (`sudoku_rouge_like.exe`, `sudoku_rouge_like_Data`, `Licenses/`, no `steam_appid.txt`), so saves and Steam Cloud keep working.
- Tests on this code: EditMode 712 (684 passed, 0 failed, 28 skipped review diagnostics), PlayMode 110 (90 passed, 0 failed, 20 skipped screenshot diagnostics).
- Smoke test: the main menu shows "Version 0.2.2". The first of two starts ended with a D3D12 error on the first frame after Steam started (`swapchain present failed`, 887a0001); the second start and a start of 0.2.1 right before ran normally. Details: `docs/review/E2E_TEST_REPORT.md` §13. Ask testers to report a start that closes by itself.
- SteamPipe build description: **v0.2.2 beta (8th iteration)**.

## On the beta branch (owner)

1. **Upload:** Steamworks → Run of the Nine (App 4858470) → SteamPipe → Builds → *Upload Depots via HTTP*: the zip, depot 4858471, description "v0.2.2 beta (8th iteration)", then commit the upload. It is not live after this step.
2. **Branch:** SteamPipe → Builds → *Manage Branches*: the password branch `beta` (create it with a password if it does not exist). Set the new build live on `beta` only. `default` keeps the current build.
3. **App Data:** if Steamworks shows "unpublished changes" afterwards, check the diff before publishing it.
4. **Content survey** (same visit, `docs/legal/legal-check-2026-10.md`): the answers below.
5. **Testers:** the tester note below, with the branch password, by DM or in `#playtest-info`. `python tools/discord/discord_admin.py give-role Playtester <user-id>` gives the Discord role.

## Content survey (Steamworks → App Admin → Content Survey)

| Question | Answer for Run of the Nine |
|---|---|
| AI-generated content, pre-generated | Yes. The background paintings, icons, item and relic art and class portraits were made with an image model (Fooocus with SDXL) and then chosen and edited by the developer. Texts (rules, item texts, translations) were written with AI assistance and reviewed by the developer. |
| AI-generated content, generated live while playing | No. The game generates its Sudoku puzzles with its own algorithm; no AI model runs in the game. |
| Music and sound | Made by the developer with code (synthesis); no AI music in the game. |
| Simulated gambling | Yes: the Koi Gambler class and the "Double or Quits" item risk in-game gold or 1 HP on chance. No real money, nothing to buy, nothing to cash out. |
| Violence, nudity, sexual content, mature themes | None. |

Suggested text for the store page's AI disclosure: "The game's artwork was created with the help of AI image generation and edited by the developer. Some texts were written with AI assistance. No AI is used while you play."

## Tester note (DM or #playtest-info, with the branch password)

```
Thank you for testing Run of the Nine 0.2.2!

Getting the beta
1. Library → Run of the Nine → Properties → Game Versions & Betas.
2. Enter the code "<branch password>" and choose "beta".
3. The main menu shows "Version 0.2.2" in the bottom left corner.

How to play this round
Please play two or three runs in one sitting. The game measures how long a run takes on your PC (nothing is sent anywhere), and this round decides whether we add a shorter run mode.

What to look at
- Walk again: after a run ends, does it make you start the next one? Is anything missing on the end screen?
- "This run": does it say how close you came and what the run moved toward?
- Harmony perks, if you play at Harmony IV or higher: Moonshade Offering, Scholar's Burden and Empty Canvas should now do what their text says.
- Controller: move through the Codex, Game Modes and Options with the D-pad. Does it go where you expect?

After the sitting
Send me your save file by DM: %USERPROFILE%\AppData\LocalLow\Kratossos\sudoku_rouge_like\save_profile_0.json (or _1, _2 for other profiles). It holds your progress and run lengths, nothing personal.

How to report
One post per bug in #rotn-bugs: what you did, what happened, what you expected, a screenshot if you can. Ideas go to #rotn-feedback. If the game closes by itself at start, please say so and send the newest folder from %TEMP%\Kratossos\sudoku_rouge_like\Crashes.

Please do not share the branch password. Thank you!
```

## Discord (short version, for #release-drafts first)

```
Beta testers: Run of the Nine 0.2.2 is on the beta branch.
• Walk again: the next run is one button away
• The end screen says how close you came and what the run moved
• Three Harmony perks fixed, D-pad menus that go where you expect
This round: please play 2 or 3 runs in one sitting and send me your save afterwards. Bugs to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```

## Notes for the owner

- The Unity Editor open on the project still holds version 0.2.1 in memory; if it saves the project settings, check that `ProjectSettings.asset` keeps `bundleVersion: 0.2.2`.
- After the testers' saves come in: `python tools/playtest/run_length_report.py <save files>` prints the run-length table and the verdict on RM-113.
