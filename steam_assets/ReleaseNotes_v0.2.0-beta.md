# Release notes — v0.2.0 beta (6th iteration)

Beta build for testers, made 2026-10-19 from `feature/game-ui-refactor` (see `CHANGELOG.md` 0.2.0 beta). Not uploaded and not live. Source of the player-facing text: the changelog.

## Getting it to the testers (owner)

The build is `Builds/RunOfTheNine_0.2.0-beta/` (zip: `Builds/RunOfTheNine_0.2.0-beta_win64.zip`, 51.7 MB; the `Builds/` folder is not in git), the same layout as the live depot: `sudoku_rouge_like.exe`, `sudoku_rouge_like_Data` (with `Plugins/x86_64/steam_api64.dll`), `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `MonoBleedingEdge`, `D3D12`. Company and product stay `Kratossos` / `sudoku_rouge_like`, so saves and Steam Cloud keep working; no `steam_appid.txt` inside.

1. **Upload:** Steamworks → Run of the Nine → SteamPipe → Builds → *Upload Depots via HTTP*: the zip, depot 4858471, description "v0.2.0 beta (6th iteration)". Commit the upload (it is not live yet).
2. **A beta branch:** SteamPipe → Builds → *Manage Branches* → New branch, for example `beta`, with a password (testers type it under Properties → Betas). Set the new build live on `beta` only; `default` keeps the current version.
3. **Testers:** send the keys (see `docs/community/beta-test-keys.md`) with the branch password and the tester note below. `give-role Playtester <user-id>` gives them the Playtester role on Discord.
4. **App Data:** if the upload leaves "unpublished changes", check the diff in App Data Admin before publishing it.

Nothing here touches the store page or players on `default`.

## Tester note (DM or #playtest-info)

```
Thank you for testing Run of the Nine!

How to get the beta
1. Steam → Games → Activate a Product on Steam → enter your key.
2. Library → Run of the Nine → Properties → Betas → enter the code "<branch password>" → choose "beta".
3. Steam downloads the beta (under Properties → Betas it shows "beta" as the active branch).

What to look at this time
- The new look: garden path, puzzle scroll, boss gate, rewards, relic shrine, market stall.
- The 13 new modifiers: does each rule make sense from its text and its marks on the board?
- Line colours: can you tell every line apart, also on a 9×9 with several kinds?
- Anything that blocks a run: crashes, a puzzle you cannot finish, a screen you cannot leave.

How to report
One post per bug in #rotn-bugs: what you did, what happened, what you expected, your board size and floor, and a screenshot if you can. Ideas go to #rotn-feedback.

Please do not share your key or the branch password. Thank you!
```

## Steam announcement (later, when the update goes to everyone)

Title: **Update v0.2.0 — The garden in ink and paper**

Summary: A new look for the whole run, 13 new puzzle rules, and lines in the colours puzzlers know.

```
[h2]Highlights[/h2]
[list]
[*][b]A new look for the whole run.[/b] The garden path is an ink hand scroll; the puzzle hangs as a scroll in front of the floor's painting, with HP and Pencil as paper lanterns and the number pad as wooden tiles. The boss gate, rewards, relic shrine and market stall follow the same style.
[*][b]13 new modifiers:[/b] Nonconsecutive Line, High-Low Line, Nabner Line, Entropic Line, Modular Line, Zipper Line, Fast Thermo, Ambiguous Thermo, Full White Kropki, Full Black Kropki, Difference ≥ 3, Multiples of 3 and Mirror Cells.
[*][b]Lines in the colours puzzlers know:[/b] German Whispers green, Dutch Whispers orange, Renban pink, Palindrome grey, thermometers grey with a bulb.
[*][b]Seed of the week:[/b] one Garden Run seed per week for everyone.
[/list]

[h2]Fixed[/h2]
[list]
[*]A chosen modifier could be missing from the board while its rule was listed. Every chosen modifier now gets its own marks.
[*]Region borders could show the previous puzzle's regions.
[*]Boss puzzles play their stage's boss music.
[*]Q on the screen after a boss no longer ends the run.
[*]A killer cage is one outline around all its cells.
[*]With a full bag, keeping your bag returns you to the rewards instead of losing them.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```
