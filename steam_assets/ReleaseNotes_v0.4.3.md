# Release notes: v0.4.3

Release build of 2026-11-09 from `feature/game-ui-refactor` (tag `v0.4.3`, local). It holds everything since 0.4.2: the
picks from the 0.4.1 and 0.4.2 playtests, the owner's 0.4.3 playtest (GAP-133 to GAP-142, RM-230 to RM-235) and the last
picks of 2026-11-09. 0.4.2 is uploaded with its own notes (owner, 2026-11-07). Player-facing text comes from `CHANGELOG.md`.
Uploaded on request (owner, 2026-11-09): **Steam build 26534828** (depot 4858471, manifest 7748882595683954094, note "v0.4.3"), live on `default` and the announcement published by the owner (2026-11-09).

## Build

See the build section of `docs/release/release-readiness-status.md` (0.4.3). SteamPipe build description: **v0.4.3**.

## Going live (owner)

1. **Achievement texts:** the English `floor_5` and the eight `challenge_*_2` are saved in Steamworks with all five
   languages. They need Publish (Steamworks → Publish). Their icons still read "40 Min" while the text says 25 minutes.
2. **Set live:** SteamPipe → Builds → set build 26534828 live on `default`.
3. **Announcement:** Steam Events → the draft "Update v0.4.3" → check the title and publish it once the build is live.
   The Steam news mirror posts it to #rotn-news by itself.
4. **Discord:** the short text below goes to #release-drafts first. I change the "current build" line of
   `tools/discord/texts/known_issues.md` to v0.4.3 when you say the build is live; then
   `python tools/discord/discord_admin.py replace known-issues tools/discord/texts/known_issues.md`.

## Steam announcement: 0.4.3

Title: **Update v0.4.3: three floors, more rules on the board and new items**

Summary: Three denser floors, rules that show up more often, six items and relics that do something new, a timed event, and F8 sends me your feedback from the game.

```
[h2]Three floors[/h2]
[list]
[*][b]The Garden Run is three floors:[/b] the Bamboo Courtyard, the Koi Terrace and the Shrine Summit. Each floor is shorter and denser, with bigger boards (6×6 to 7×7, 7×7 to 9×9, then 9×9), 40 % more gold and 50 % more XP per puzzle.
[*]A shop waits right after each boss. In the middle of Floors 2 and 3 stands the warden of the garden you skip: an Elite puzzle, and after it you choose a relic.
[*]The story's third chapter opens on Floor 2, Summit in Sight asks for the last floor, and the Swift Path challenge is a win in under 25 minutes. A run you saved before keeps its five floors.
[/list]

[h2]Rules show up more often[/h2]
[list]
[*]A rule used to bring two or three lines to a board. A rule alone now brings twice as many, and the more rules share a board, the fewer each one gets.
[*]Lines can run as wide as the board, and a no-repeat line inside one box is gone. It said nothing the box did not.
[*]Irregular regions turn with the seed. Fulfilled X-Sum clues are struck through, and Prime Cells names only the digits your board has.
[/list]

[h2]Items and relics[/h2]
[list]
[*][b]Chime of Notes[/b] (was Wind Chime): writes every possible digit as free notes on the whole board.
[*][b]River Bell[/b] (was Koi Dragon Scale): mistakes keep your combo in this puzzle, and the end pays +2 gold per 5 combo.
[*][b]Stone Oath[/b] (was Temple Stamp): costs 1 HP, and this puzzle pays 50 % more gold.
[*][b]Moth Light[/b] (was Garden Lantern): lifts the fog of a box you choose, for good.
[*][b]Steady Brush[/b] (was Steady Hand): pencil costs 40 % less. [b]Void Petal:[/b] the first mistake of each floor costs 3 pencil instead of HP.
[*][b]Unbroken Thread[/b], a new relic: a mistake halves your combo instead of ending it. The Cracked Teacup now keeps your combo too.
[*]The Golden Kintsugi Jar turns your next 3 mistakes in a puzzle into the right digit. The Garden Rake and the Zen Sand Sifter work on your notes, and the Solver completes boxes as it says.
[/list]

[h2]Classes[/h2]
[list]
[*][b]Lantern Seer:[/b] a light floats over an empty cell in every puzzle. The right digit there catches it: +1 HP, +8 gold or +2 pencil, at random, and the light moves on.
[*][b]Shrine Archivist:[/b] the first pencil mark in a cell is free (two at Level 20), and a cell narrowed to two notes has a 30 % chance to write itself.
[*][b]Stone Gardener:[/b] every item has 2 uses, 3 from Level 20.
[*][b]Garden Monk, Number Freak, Reed Duelist, Quiet Cartographer:[/b] new passives that reward a clean or a pencil-free puzzle. The class items work now, and the Reed Duelist can be unlocked.
[/list]

[h2]New to do[/h2]
[list]
[*][b]The Hourglass Master:[/b] a new event. Solve a timed puzzle for an item, or a bigger one on a tighter clock for a relic. Out of time, you are back on the path with nothing lost.
[*][b]Your last runs[/b] wait on a slip in the main menu: see the last board and walk the seed again.
[*][b]Custom Puzzle:[/b] up to seven rules, and the pressure rules have a tab in the rule book.
[*]Name your profiles. Renaming keeps everything in them.
[/list]

[h2]Easier to follow[/h2]
[list]
[*][b]A card for each new rule:[/b] the first time a puzzle brings a rule you have not met, it is explained before the clock starts.
[*]The chosen cell wears a red frame, the chosen path tile a red brush ring, and a full bag compares the two items side by side.
[*]View Modifiers is a slip like the legend. Relics sit in rows that never overlap. Events show what happened and every number it changed.
[*][b]Keyboard:[/b] arrows on rest and event screens, Tab in and out of the bag, R to your relics. The pointer hides while you use the keys.
[*][b]The Daily Walk has its own save[/b], continues from the main menu, and its first shop has first-floor prices. The Monthly Walk board shows your time.
[/list]

[h2]Send feedback from the game[/h2]
[list]
[*]Press F8, or Options › Run › Send feedback. Write what happened and tick what goes with it: the screenshot, the run's seed and board, the log, your save. Nothing goes without a tick, and it comes straight to me.
[*]After a crash the game asks whether to send the log. Nothing goes without a yes.
[/list]

[h2]Fixed[/h2]
[list]
[*]The puzzle timer no longer starts over after Save & Quit.
[*]A bag of four or more slots was invisible for the Stone Gardener, and the rule card lay under the bag.
[*]A Windows lock screen could end the game on a Legion Go. The game now runs on Direct3D 11 and saves when it loses focus.
[*]After a rest the path showed the old HP, a solved board could keep some fog, and at a large Text Size words broke in the middle.
[*]A boss with Full Kropki could start without some of its chosen lines, circles or thermos.
[/list]

Thank you, Paul and everyone who played and wrote! Found a bug? Press F8 in the game, or tell us in the Steam discussions or on our Discord.
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.4.3 is out!
• The Garden Run is three denser floors: bigger boards, more gold and XP, a shop after every boss
• Rules show up more often in a puzzle, and lines can run as wide as the board
• Six items and relics do something new, and the Unbroken Thread halves your combo instead of ending it
• The Hourglass Master: a timed puzzle for an item or a relic
• F8 in the game sends me your feedback with the screenshot and the seed, if you tick them
A run you saved before keeps its five floors. Thank you for the notes! Bugs to #rotn-bugs, ideas to #rotn-feedback.
```
