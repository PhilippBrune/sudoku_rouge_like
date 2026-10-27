# Release notes: v0.3.0 (9th iteration)

Release build made 2026-10-27 from `feature/game-ui-refactor` (tag `v0.3.0`, local). Owner's request (2026-10-27): test the Options in full and, if they pass, make a current version with release notes; no beta. Player-facing text comes from `CHANGELOG.md` 0.3.0, and the announcement also covers the 0.2.2 beta, which never left the beta build. Not uploaded and not live.

## Build

- `Builds/RunOfTheNine_0.3.0/` and `Builds/RunOfTheNine_0.3.0_win64.zip` (54.4 MB, 157 files, made with Python `zipfile`; the `Builds/` folder is not in git). Same layout and names as the live depot (`sudoku_rouge_like.exe`, `sudoku_rouge_like_Data` with `Plugins/x86_64/steam_api64.dll`, `Licenses/`, no `steam_appid.txt`); company and product stay `Kratossos` / `sudoku_rouge_like`, so saves and Steam Cloud keep working.
- Built from a fresh `Library` on the test mirror: "Build Finished, Result: Success", no "missing script" line in the build log.
- Tests on this code: EditMode 771 (743 passed, 0 failed, 28 skipped review diagnostics), PlayMode 117 (94 passed, 0 failed, 23 skipped screenshot diagnostics).
- Smoke test: started windowed from its own folder, at the main menu with "Version 0.3.0" after 30 s, no exception in the player log; the save folder and PlayerPrefs were restored from the backup afterwards (`docs/review/E2E_TEST_REPORT.md` §14).
- SteamPipe build description: **v0.3.0 (9th iteration)**.

## Options test (before the build)

Every control of Options → Audio was checked in the running game (`InRunAudioPlayModeTests.EveryVolumeRow_ChangesWhatIsHeard_InTheMenuAndInARun`): Master and Music set the menu music, Master, Music and SFX set the run's music and effects at once, Mute all silences both, Mute when unfocused pauses the sound in the background, and Menu Music changes the menu's track. Display, Gameplay and Accessibility are covered by the existing tests (`GameplayOptionsPlayModeTests`, `OptionsPersistenceTests`, `GeneratedUiPlayModeTraversalTests`, `DisplayModeTests`, `ReduceMotionTests`). Found and fixed:

- **GAP-071:** a setting changed in the main menu's Options came back undone after a change in a run's Options (and the other way round); a profile deleted and chosen again kept the old profile's options.
- **GAP-072:** UI Volume set nothing, since the game plays no interface sounds. The row is removed (owner's choice).
- **GAP-073 (open):** 25 of the 35 sound effects are never played (shop reroll, level up, boss gate and others). The SFX volume works for the ten that play.

## Going live (owner)

1. **Achievements first:** Steamworks → Run of the Nine (App 4858470) → Stats & Achievements → Publish. The 29 achievements (21 new) and their German, French, Spanish and Brazilian Portuguese texts are saved but not published; until they are, the new ones cannot be earned.
2. **Upload:** SteamPipe → Builds → *Upload Depots via HTTP*: the zip, depot 4858471, description "v0.3.0 (9th iteration)", then commit the upload. It is not live after this step.
3. **Set live:** SteamPipe → Builds → set the build live on `default`. If App Data shows "unpublished changes" afterwards, check the diff before publishing it.
4. **Content survey:** the answers in `steam_assets/ReleaseNotes_v0.2.2-beta.md` (Content survey) still hold; the new modes add nothing to them.
5. **Announcement:** Events & Announcements → Create → *Patch notes / Small update*, title and text below; publish it after the build is live.
6. **Discord:** the short version below goes to `#release-drafts` first, then to the announcements channel after your OK.

The daily and weekly Steam leaderboards (`DailyWalk_<date>`, `IncenseWalk_<week>`) are created by the game the first time they are used; nothing to set up in Steamworks.

## Steam announcement

Title: **Update v0.3.0: the Daily Walk and the Incense Walk**

Summary: Two short ways to play, a relic for every class, keystones after the first boss, ink stamps that buy looks, and 21 new achievements.

```
[h2]Two short ways to play[/h2]
[list]
[*][b]The Daily Walk is a run of its own.[/b] Two floors, each with its boss, on the day's seed. Everyone walks as the same walker (10 HP, 10 Pencil, 2 bag slots), so no class has to be unlocked first. The day's three goals and the finished walk pay class XP, and you choose the class that gets it. A streak adds up to 25 %, and each day has its own leaderboard.
[*][b]Ink stamps buy looks.[/b] Meet all three goals of a day for a stamp, keep your streak for more, and spend them in the stamp shop on board ink, seal frames and path seals.
[*][b]The Incense Walk.[/b] A time attack: an incense stick burns down from 8:00, every solved puzzle adds time, and a mistake costs 10 seconds instead of HP. The puzzles grow as you go, pencil marks are free, and the week's seed has its own leaderboard.
[*][b]Walk again.[/b] The end of a Garden Run, Endless Zen, Spirit Trials or the Monthly Walk starts the next run with one button, and a lost run tells you how close it came and what it moved.
[/list]

[h2]Relics, rewards and achievements[/h2]
[list]
[*][b]A signature relic for every class.[/b] Eight new relics, one per class, show up in relic offerings for their class.
[*][b]Keystones.[/b] After the first boss of a Garden Run, pick one of three keystones that bend a rule of the puzzle.
[*][b]The seed of the week rewards pace:[/b] a quick win scores up to +30 %.
[*][b]21 new achievements[/b] for the first hours, every class, Endless Zen, Spirit Trials, the Monthly Walk, the Daily Walk streak and the Codex.
[/list]

[h2]Controllers[/h2]
[list]
[*]In a run the D-pad and LT/RT now work on Xbox pads and on every pad Steam Input turns into one (Steam Deck, Legion Go).
[*]New controller mode in Options → Controls: Automatic, X-input or D-input, for PlayStation pads without Steam Input and older PC pads.
[*]The D-pad on the main menu's screens moves along each screen's rows and columns.
[/list]

[h2]Fixed[/h2]
[list]
[*]Scholar's Burden, Moonshade Offering and Empty Canvas now give what they promise.
[*]A setting changed in the main menu's Options is no longer undone by a change in a run's Options.
[*]Options → Audio no longer shows UI Volume, which changed nothing.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.3.0 is out!
• The Daily Walk: two floors a day, class XP, a streak and a daily leaderboard
• The Incense Walk: a time attack against a burning incense stick
• A relic for every class, keystones after the first boss, ink stamps that buy looks
• 21 new achievements, and the D-pad works in a run on Xbox pads and the Steam Deck
Full notes on Steam. Bugs go to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```

## Notes for the owner

- The Unity Editor that is open on the project may still hold version 0.2.2 in memory; if it saves the project settings, check that `ProjectSettings.asset` keeps `bundleVersion: 0.3.0`.
- Prices in the stamp shop and the class time forms of the Incense Walk are first values for playtesting.
- Open: GAP-073 (sounds never played) and the midnight edge of the Incense Walk's week board (`docs/IncenseWalkMode.md`).
