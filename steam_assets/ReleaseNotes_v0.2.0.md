# Release notes — v0.2.0 (6th iteration)

Release build made 2026-10-20 from `feature/game-ui-refactor` (commit `af3d9dd` and the release commit after it, tag `v0.2.0`). Player-facing text comes from `CHANGELOG.md` 0.2.0. It replaces the 0.2.0 beta notes (`ReleaseNotes_v0.2.0-beta.md`), which covered a smaller build for testers.

## Build

- `Builds/RunOfTheNine_0.2.0/` and `Builds/RunOfTheNine_0.2.0_win64.zip` (the `Builds/` folder is not in git). Same layout and names as the live depot: `sudoku_rouge_like.exe`, `sudoku_rouge_like_Data` (with `Plugins/x86_64/steam_api64.dll`), `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `MonoBleedingEdge`, `D3D12`; company and product stay `Kratossos` / `sudoku_rouge_like`, so saves and Steam Cloud keep working; no `steam_appid.txt` inside.
- Tests on the release commit: EditMode 597 (569 passed, 0 failed, 28 skipped review diagnostics), PlayMode 55 (44 passed, 0 failed, 11 skipped screenshot diagnostics).
- SteamPipe build description: **v0.2.0 (6th iteration)**.

## Going live (owner)

1. **Upload:** Steamworks → Run of the Nine → SteamPipe → Builds → *Upload Depots via HTTP*: the zip, depot 4858471, description "v0.2.0 (6th iteration)", then commit the upload. It is not live after this step.
2. **Optional beta first:** set the new build live on the password branch `beta` and let testers check it.
3. **Set live:** SteamPipe → Builds → set the build live on `default`. If App Data shows "unpublished changes" afterwards, check the diff before publishing it.
4. **Announcement:** Steamworks → Events & Announcements → Create → *Patch notes / Small update*, title and text below, then publish it after the build is live.
5. **Discord:** post the short version below in `#release-drafts` first, then in the announcements channel.

## Steam announcement

Title: **Update v0.2.0 — The garden in ink and paper**

Summary: Every screen redrawn in ink and paper, 41 new puzzle rules (71 in all), and a Custom Puzzle rule book with every one of them.

```
[h2]Highlights[/h2]
[list]
[*][b]The whole run in ink and paper.[/b] The garden path is a hand scroll; the puzzle hangs as a scroll in front of the floor's painting, with HP and Pencil as paper lanterns and the number pad as wooden tiles. The boss gate, rewards, relic shrine, market stall, rest, events, cursed tiles and the end of a run follow the same style.
[*][b]41 new puzzle rules, 71 in all.[/b] New lines (Zipper, Nabner, Entropic, K-Whispers, 10-Line and more), new dots and pairs, cages and arrows (Hidden Killer, Product Cage, Pill Arrow…), cell rules, whole-board rules (X-Sudoku, Hyper Sudoku, Magic Square…) and clues outside the grid (Skyscrapers, Sandwich, X-Sums, Little Killer, Quadruples).
[*][b]Custom Puzzle as a rule book.[/b] One tab per family, every rule as a tile, up to five rules at once; a rule that does not fit tells you why.
[*][b]Lines in the colours puzzlers know.[/b] German Whispers green, Dutch Whispers orange, Renban pink, Palindrome grey, thermometers grey with a bulb.
[*][b]Seed of the week.[/b] One Garden Run seed per week for everyone.
[/list]

[h2]Fixed[/h2]
[list]
[*]A chosen modifier could be missing from the board while its rule was listed. Every chosen modifier now gets its own marks.
[*]Lines and arrows could cross themselves in an X. They no longer do.
[*]Picking Entropy at the boss gate could give a board where correct digits showed as conflicts.
[*]Cleared cursed puzzles now add their +10% to the run score, and the score's breakdown adds up.
[*]Region borders could show the previous puzzle's regions.
[*]Boss puzzles play their stage's boss music; Q after a boss no longer ends the run.
[*]A killer cage is one outline around all its cells; with a full bag you no longer lose your reward.
[/list]

[h2]Changed[/h2]
[list]
[*]Prestige also gives +1 starting gold per tier for that class (up to +9).
[*]Rest, rewards and relics: choose first, then take. Events grey out choices you cannot pay for and say why.
[*]The in-game Options match the main menu and work fully with a controller.
[*]French, Spanish and Brazilian Portuguese cover items, relics, modifiers, classes and story texts.
[*]Seeds from earlier versions play differently, because the pool of rules grew. Saved runs keep their boards.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.2.0 is out!
• The whole run redrawn in ink and paper — path, puzzle, boss gate, rewards, shop, rest, events and the end of a run
• 41 new puzzle rules, 71 in all, from Zipper lines to Skyscrapers and the Magic Square
• Custom Puzzle as a rule book with every rule
• Fixes: lines that crossed themselves, the Entropy boss board, the cursed-puzzle score bonus
Full notes on Steam. Bugs go to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```

## Notes for the owner

- Seeds from 0.1.5 and the 0.2.0 beta replay differently in 0.2.0 (the pool grew); say so when players ask.
- Run scores differ slightly from 0.1.5 for the same run (bonuses added as the rules say). The leaderboard ranks XP and is unaffected.
- The build that is live on `default` today was not changed by this upload.
