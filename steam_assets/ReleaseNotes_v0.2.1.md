# Release notes — v0.2.1 (7th iteration)

Release build made 2026-10-21 from `feature/game-ui-refactor` (tag `v0.2.1`). Player-facing text comes from `CHANGELOG.md` 0.2.1. Not uploaded and not live.

## Build

- `Builds/RunOfTheNine_0.2.1/` and `Builds/RunOfTheNine_0.2.1_win64.zip` (the `Builds/` folder is not in git; zip made with Python `zipfile`). Same layout and names as the live depot: `sudoku_rouge_like.exe`, `sudoku_rouge_like_Data` (with `Plugins/x86_64/steam_api64.dll`), `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `MonoBleedingEdge`, `D3D12`; company and product stay `Kratossos` / `sudoku_rouge_like`, so saves and Steam Cloud keep working; no `steam_appid.txt` inside.
- New in the package: `Licenses/` with the licence texts of the Roboto font (Apache 2.0 and its NOTICE) and Steamworks.NET (MIT), which those licences require to travel with the game (GAP-068, `docs/legal/legal-check-2026-10.md`).
- Tests on the release code: EditMode 689 (661 passed, 0 failed, 28 skipped review diagnostics), PlayMode 105 (85 passed, 0 failed, 20 skipped screenshot diagnostics).
- SteamPipe build description: **v0.2.1 (7th iteration)**.

## Going live (owner)

1. **Upload:** Steamworks → Run of the Nine → SteamPipe → Builds → *Upload Depots via HTTP*: the zip, depot 4858471, description "v0.2.1 (7th iteration)", then commit the upload. It is not live after this step.
2. **Optional beta first:** set the new build live on the password branch `beta` and let testers check it.
3. **Set live:** SteamPipe → Builds → set the build live on `default`. If App Data shows "unpublished changes" afterwards, check the diff before publishing it.
4. **Content survey (legal check):** while in Steamworks, check that the AI-generated content disclosure covers the art and texts and that simulated gambling (Koi Gambler, Double or Quits) is declared.
5. **Announcement:** Steamworks → Events & Announcements → Create → *Patch notes / Small update*, title and text below, then publish it after the build is live.
6. **Discord:** post the short version below in `#release-drafts` first, then in the announcements channel.

## Steam announcement

Title: **Update v0.2.1 — Every menu in ink and paper**

Summary: Select Class, Game Modes and every mode's results in the new look, a grade for every puzzle, gold that matters, six counter items, and options that do what they say.

```
[h2]Highlights[/h2]
[list]
[*][b]Choose your class at a glance.[/b] Every class is a seal with its level and prestige stars, or with what unlocks it. The chosen class hangs on a scroll with what the run starts with, and Harmony, its perk, irregular puzzles and the seed sit on one strip below. Begin the run starts it: no second window.
[*][b]Game Modes as five curtains.[/b] Garden Run, Endless Zen, Spirit Trials, Daily Walk and Monthly Walk side by side, each with your progress on it; a locked mode tells you what opens it.
[*][b]Results on a scroll.[/b] Endless Zen, Spirit Trials and the Monthly Walk now end like a Garden Run: your record large, your best beside it, and a stamp when you beat it.
[*][b]Options, Codex and classes redrawn.[/b] Options as a hand scroll with bookmarks, the Codex as a collector's book with a new Curses page, and Meta Progression as a garden of classes.
[*][b]A grade for every puzzle.[/b] Perfect, Clean, Rough or Messy, shown as stamps at the end of a run, instead of a flat penalty per mistake.
[*][b]Gold that matters.[/b] Items cost more, gold at the boss gate can strike a trial or calm the boss, and gold you keep adds to your score. Six new counter items answer the floor's rules.
[/list]

[h2]Fixed[/h2]
[list]
[*]Options do what they say: Highlight Errors, Show Digits Left, Jump to Next Empty Cell, Double-Tap to Confirm, Screen Shake, Master Volume, Board Brightness and Contrast, Text Size on the board, colour-blind line colours.
[*]Reduce Motion now also stops the pulsing path nodes, the boss node and the digit pops.
[*]Numbers follow the game's language, not your PC's region.
[*]Lines no longer cover the digits from the second puzzle on; mistakes count when they happen; the reward shows the puzzle's real time; number keys work on every scene over the path.
[/list]

[h2]Changed[/h2]
[list]
[*]Class Level 30 starts every run with the class's item, Level 40 gives the class its frame.
[*]Input Lock can be turned off or set from 0.10 to 1.00 s (Options → Gameplay).
[*]From Floor 4 a mistake costs at least 2 HP, and the rest heals a quarter of your HP.
[*]The main menu shows the version, and "Items & Relic Codex" is now simply "Codex".
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.2.1 is out!
• Select Class, Game Modes and every mode's results in ink and paper
• A grade for every puzzle, gold that matters, six counter items
• Options, Codex and the garden of classes redrawn
• Options that do what they say, Reduce Motion that really stops motion
Full notes on Steam. Bugs go to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```

## Notes for the owner

- The Unity Editor that is open on the project still holds version 0.2.0 in memory; if it saves the project settings, check that `ProjectSettings.asset` keeps `bundleVersion: 0.2.1`.
- Open owner decisions from this iteration: `docs/review/OPEN_POINTS.md` §3 (GAP-064 rules, the one-more-run picks, the legal-check actions).
