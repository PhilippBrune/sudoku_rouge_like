# Release notes — v0.1.5 (5th iteration)

Steam build **25938214** ("v0.1.5 (5th iteration)", depot 4858471), uploaded 2026-10-14, not live yet. Source: `CHANGELOG.md` 0.1.5 (player-facing part).

Where to post: Steamworks → Run of the Nine → Community → **Post / Manage Events & Announcements** → Create New Event or Announcement → **Patch Notes** (or Regular Update). Paste the fields below; the body is Steam BBCode. Publish the announcement when the build is live on `default`.

## Title

Update v0.1.5 — Resume, finishable runs and a fairer garden

## Subtitle

The fifth Early Access build

## Summary (one line, shown in the library and the event list)

Resume mid-puzzle, continue after every boss, faster puzzle generation, Harmony that does what it says, boss debuffs, new window modes and many fixes.

## Body (BBCode)

```
Thank you for playing Run of the Nine in Early Access. This update fixes the blockers many of you ran into, finishes several systems that were only half done, and makes the rules match what the game tells you.

[h2]Highlights[/h2]
[list]
[*][b]Resume works.[/b] Save & Quit in the middle of a puzzle, come back later and keep playing: board, modifiers, pressure and counters are restored.
[*][b]Runs can be finished.[/b] After a non-final boss you can claim your reward and continue to the next floor.
[*][b]Faster, reliable puzzles.[/b] Boards generate in well under a second where they used to take 20–30 seconds or fail.
[*][b]Harmony does what it says.[/b] Every Harmony level now changes HP cost, gold, boss choices, board sizes and rewards as described.
[*][b]Boss debuffs.[/b] From Floor 3, each Garden Run boss carries one debuff, shown on the boss gate.
[/list]

[h2]New[/h2]
[list]
[*]Daily Walk and Monthly Walk on the main menu, with your streak and the time left in the month.
[*]Enter a seed when starting a run. The in-run menu shows the run seed and has View Modifiers and Abandon Run.
[*]Starting a new run while one is saved now warns you and offers Abandon & Begin.
[*]Modifier legend on the board: tap an entry to highlight its overlay for 3 seconds.
[*]Buy pencil during a puzzle (+3 pencil for 20 gold, 20 more each time; not for Garden Monk).
[*]Reroll item rewards with a reroll token or gold (20 gold, 20 more each time).
[*]Prestige a Level 40 class from the Meta Progression screen.
[*]The path map shows every tile from the start, your position, and one bridge between the lanes per floor.
[*]The class screen shows base stats with your level bonus, for example "HP 10 (+2)".
[*]First-run setup offers all five languages.
[*]New display modes: Windowed and Frameless Window, next to Fullscreen and Borderless Fullscreen. Windows open centred and sized to fit your screen.
[/list]

[h2]Changed[/h2]
[list]
[*][b]Wrong digits:[/b] a digit that does not match the solution is now a mistake even when it does not visibly clash. It is rejected and costs HP. Custom 7★ puzzles keep the rule-based check.
[*][b]Undo removed.[/b] Mistakes count, as in an HP-based run.
[*][b]Anti-Bishop is back:[/b] no digit may repeat on a diagonal within two cells, from 6×6 boards.
[*]Boss boards follow the floor's board-size range, and the map preview always matches the puzzle you get.
[*]Early floors stay readable: puzzles on Floors 1–2 are at most one star above the floor's default.
[*]The Harmony XP multiplier applies to every run end, including defeats; the old flat bonus is gone.
[*]XP above Level 40 is no longer stored.
[*]Rewards that roll "Nothing" give a little gold (5 × floor).
[*]"Collect 50,000 gold" counts all gold you collect, not the gold you hold at the end of a run.
[*]The daily streak resets after a missed day.
[*]"Allow irregular puzzles" is on by default.
[*]Digits are easier to tell apart: givens are bold ivory, your digits are blue.
[*]Pressure mechanics can appear from Floor 2.
[*]The game window is titled "Run of the Nine".
[/list]

[h2]Fixed[/h2]
[list]
[*]A fresh install shows profile selection, first-run setup and the Sudoku Basics prompt.
[*]On a new PC or after a reinstall, the game opens the profile you played last instead of an empty one.
[*]Codex discoveries and option changes made during a run are no longer lost at the next autosave.
[*]Save & Quit can no longer re-roll boss choices, rewards or relic choices.
[*]Accepted boss relics are always added.
[*]Mistakes are saved immediately; quitting after a mistake no longer undoes it.
[*]The XP breakdown and run statistics keep the tiles played before a resume.
[*]Pressure countdowns and highlights no longer vanish after tapping a cell.
[*]Item Block (boss debuff) really blocks items; before, the item worked and stayed in your bag.
[*]The shop no longer charges for an item that does not fit in your bag.
[*]The Reed Pledge bonus is credited to the right tile.
[*]Number Freak gets its Level 3 "+1 Pencil".
[*]Boss modifiers you clear count towards completion.
[*]German text displays correctly everywhere, and more of the interface is translated.
[*]Monthly Walk generates for every month, and a failed start no longer traps you.
[*]Option dropdowns show all their entries.
[*]Endless Zen and Spirit Trials only use modifiers that work.
[/list]

[h2]Your saves[/h2]
Your profiles and runs carry over, and Steam Cloud keeps syncing them. A run saved before this update keeps its old path layout until you reach the next floor. Completion and codex entries that earlier builds failed to record fill in as you play.

[h2]Known issues[/h2]
[list]
[*]French, Spanish and Brazilian Portuguese: item, relic, boss, class and story texts are still in English.
[*]The in-game Options panel keeps its older look.
[*]Music tracks are short loops.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions. Thank you for helping shape the garden!
```

## Checklist before publishing (owner)

1. Optional: set build 25938214 live on a beta branch and check it in the Steam client (resume, Save & Quit, a boss, achievements/stats at a run end, Steam Cloud).
2. Steamworks → SteamPipe → Builds: set build 25938214 live on `default` (Preview Change, then confirm).
3. App Data Admin → Publish: the depot upload left "unpublished changes"; review the diff and publish if it only concerns the new build.
4. Store Page Admin → Publish: the trailer rename ("Gameplay Trailer") is saved but not published; decide on the old 1.0 trailer.
5. Post the announcement above.
