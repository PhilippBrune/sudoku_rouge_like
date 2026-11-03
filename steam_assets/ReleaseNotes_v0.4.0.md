# Release notes: v0.4.0 (10th iteration)

Release build made 2026-11-03 from `feature/game-ui-refactor` (tag `v0.4.0`, local). Owner's request (2026-11-03): "a release build + patch notes", and the Discord server fixed for the Daily Walk once the upload is done. 0.4.0 is the first public build since 0.3.0 (live since 2026-10-27); it carries the local test builds 0.3.1 to 0.3.3. Player-facing text comes from `CHANGELOG.md` 0.3.1 to 0.4.0. Not uploaded and not live.

## Build

- `Builds/RunOfTheNine_0.4.0/` and `Builds/RunOfTheNine_0.4.0_win64.zip` (54.5 MB, 157 files; made with Python `zipfile`; the `Builds/` folder is not in git). Same layout and names as the live depot (`sudoku_rouge_like.exe`, `sudoku_rouge_like_Data` with `Plugins/x86_64/steam_api64.dll`, `Licenses/`, no `steam_appid.txt`); company and product stay `Kratossos` / `sudoku_rouge_like`, so saves and Steam Cloud keep working.
- Built from a fresh `Library` on the test mirror: "Build Finished, Result: Success", no "missing script" line in the build log.
- Tests on this code: EditMode 832 (803 passed, 0 failed, 29 skipped review diagnostics), PlayMode 143 (111 passed, 0 failed, 32 skipped screenshot diagnostics); the Discord and Steam tool tests pass (21 and 5).
- Smoke test: started from its own folder, at the main menu with "Version 0.4.0" after 30 s, no exception in the player log; the save folder and PlayerPrefs were restored from the backup afterwards and are byte for byte as before.
- SteamPipe build description: **v0.4.0 (10th iteration)**.

## Going live (owner)

1. **Achievements first:** Steamworks → Run of the Nine (App 4858470) → Publish. The page shows 39 achievements, every language fully localized; the ten story achievements (created 2026-11-03) are saved but not published, and until they are, they cannot be earned.
2. **Upload:** SteamPipe → Builds → *Upload Depots via HTTP*: the zip, depot 4858471, description "v0.4.0 (10th iteration)", then commit the upload. It is not live after this step.
3. **Set live:** SteamPipe → Builds → set the build live on `default`. If App Data shows "unpublished changes" afterwards, check the diff before publishing it.
4. **Steam Input (GAP-108):** while you are in Steamworks, check Steam Input → the default configuration. It should be the "Gamepad" template with no keyboard keys and no mouse on the right stick; a template with keys made the D-pad place digits in the 0.3.1 playtest.
5. **Announcement:** Events & Announcements → Create → *Patch notes / Small update*, title and text below; publish it after the build is live.
6. **Discord**, after the build is live:
   - `python tools/discord/walk_posts.py walk-live`: from the next day the Daily Walk post describes the walk (two floors with a boss each, the walker, ink stamps, the day's leaderboard) instead of only the goals. Its goals already come from this build's code, so before the upload they could name a goal the live 0.3.0 does not have ("Read an event on the path").
   - `python tools/discord/discord_admin.py replace how-to-play tools/discord/texts/how_to_play.md`: the Daily Walk, the Incense Walk and the keepers' tales in #how-to-play.
   - `python tools/discord/discord_admin.py plan`, then `apply`: the channel topic of #rotn-seeds-and-walks.
   - The short version below goes to `#release-drafts` first, then to the announcements channel after your OK.

## Steam announcement

Title: **Update v0.4.0: the story of the Nine**

Summary: Eight keepers, five chapters each, told in scenes on the garden's paintings. Ten story achievements, XP painted in with a brush, and a controller that answers A and B on every screen.

```
[h2]The story of the Nine[/h2]
[list]
[*][b]Eight keepers stayed when the ninth left the garden.[/b] Each class is one of them, and each has a tale of five chapters. The first chapter is open; the others open when you defeat a warden, reach Floor 3, win a run and win at Harmony III with that keeper. The ninth keeper's chapter waits until every keeper has won.
[*][b]Every chapter is a scene.[/b] The keeper and the warden face each other on the floor's painting and talk, one line at a time, and a voice tells the rest. A new chapter plays once when your next run with that keeper starts, and the Codex's Tales page plays any chapter again.
[*][b]Ten story achievements:[/b] a new page, the whole tale of each keeper, and the ninth keeper.
[*][b]A goodbye when the game closes:[/b] the garden at dusk and what waits tomorrow.
[/list]

[h2]New in the garden[/h2]
[list]
[*][b]XP is painted in.[/b] At the end of a run a brush paints the XP into the bar, and every new level gets a red seal. Any key or button skips it.
[*][b]Event tiles are back[/b] on both routes of the Garden Run.
[*][b]The Custom Puzzle ends on a result[/b] with your time, mistakes, pencil marks, rules and grade, and irregular regions work on every Custom Puzzle size.
[*][b]A Spirit Trial starts with one item:[/b] Normal on Apprentice and Adept, Rare on Master and Grandmaster.
[*]The mouse pointer is a Japanese brush, and Enter on an empty cell clears its pencil marks.
[/list]

[h2]Changed[/h2]
[list]
[*]The legend is now the rule slip beside the board: each rule with its colour, its name and what it asks. Tap a rule to see only its lines.
[*]The Daily Walk's floors are two tiles shorter. A saved walk waits until midnight, and the path and the main menu say so.
[*]Custom Puzzle greys out the regions a board size does not have.
[*]Options: the page marks show LB/RB or Q/E, and the Accessibility preview shows every switch.
[*]The Daily Walk widget opens the Daily Walk in Game Modes, and Back from the Monthly Walk returns to where you opened it.
[/list]

[h2]Controllers[/h2]
[list]
[*]A confirms and B goes back on every screen.
[*]The left stick moves through the menus like the D-pad, and Start opens and closes the Options.
[*]On Select Class, A on a class picks it and jumps to Begin.
[*]The mouse pointer hides while you play with a controller, and the button hints no longer flicker.
[/list]

[h2]Fixed[/h2]
[list]
[*]Apply and Cancel on the Language page had no text on ultrawide screens.
[*]Enter, digits and Q no longer reach the board while a run's Options are open.
[*]A run's Options no longer show an empty Language page.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.4.0 is out!
• The story of the Nine: eight keepers, five chapters each, told in scenes
• Ten story achievements and XP painted in with a brush
• Event tiles are back, the Custom Puzzle ends on a result, Spirit Trials start with an item
• A and B work on every screen with a controller, and the left stick moves through the menus
Full notes on Steam. Bugs go to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```

## Notes for the owner

- The Unity Editor that is open on the project may still hold version 0.3.3 in memory; if it saves the project settings, check that `ProjectSettings.asset` keeps `bundleVersion: 0.4.0`.
- Open: GAP-108 (Steam Input, step 4), GAP-073 (sounds never played), and the playtest checks in `docs/review/OPEN_POINTS.md` §1.
