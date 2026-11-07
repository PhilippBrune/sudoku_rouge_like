# Release notes: v0.4.3

Release build of 2026-11-07 from `feature/game-ui-refactor` (tag `v0.4.3`, local). Owner's request (2026-11-05): "Bau das
noch in 0.4.3 mit ein und kombiniere die releasenots von 0.4.2 & 0.4.3". It holds the owner's picks from the 0.4.1
playtest on a Legion Go (path A, bag B, T1, F3, M2, P-NF, P-RD, P-QC) and the fixes GAP-119, GAP-120 and GAP-121, and
(owner, 2026-11-07, "bau das auch noch in 0.4.3 ein") the picks after 0.4.2 (LS1, SA1, U1, the item ticks, FB1, FB4,
FB5, K3, D1) with the fixes of the owner's Daily Walk playtest (GAP-122 to GAP-132). 0.4.2
is uploaded with its own notes (owner, 2026-11-07), so this file has two announcement texts: one for 0.4.3 alone, and
one for 0.4.2 and 0.4.3 together, in case 0.4.2 was not announced yet. Player-facing text comes from `CHANGELOG.md`.
Not uploaded and not live.

## Build

See the build section of `docs/release/release-readiness-status.md` (0.4.3). SteamPipe build description: **v0.4.3**.

## Going live (owner)

1. **Achievements first** (Steamworks → Stats & Achievements): the English texts of `floor_5` and the eight
   `challenge_*_2` changed (the run is three floors now; the Swift Path is under 25 minutes). English, as in
   `docs/Achievements.md` section 2:
   - `floor_5`: **Summit in Sight**: "Reach the last floor of a Garden Run."
   - `challenge_<class>_2`: **The Swift Path (<class>)**: "Win a Garden Run in under 25 minutes."

   Then upload the other four languages from `steam_assets/achievements/localization/*.vdf` and publish the achievement
   change (stop before Publish, as always).
2. **Upload:** SteamPipe → Builds → *Upload Depots via HTTP*: `Builds/RunOfTheNine_0.4.3_win64.zip`, depot 4858471,
   description "v0.4.3", then commit the upload. It is not live after this step.
3. **Set live:** SteamPipe → Builds → set the build live on `default`.
4. **Announcement:** Events & Announcements → Create → *Patch notes / Small update*, one of the texts below; publish it
   after the build is live. The Steam news mirror posts it to #rotn-news by itself.
5. **Discord:** I change the "current build" line of `tools/discord/texts/known_issues.md` to v0.4.3 when you say the
   build is live; then `python tools/discord/discord_admin.py replace known-issues tools/discord/texts/known_issues.md`.

## Steam announcement: 0.4.3 alone

Title: **Update v0.4.3: three floors, a light for the Lantern Seer and feedback from the game**

Summary: The Garden Run is three denser floors now. The Lantern Seer and the Shrine Archivist play new, the class items
work, a new rule gets its own card, the Daily Walk keeps its own save, and F8 sends me your feedback from the game.

```
[h2]Three floors[/h2]
[list]
[*][b]The Garden Run is three floors:[/b] the Bamboo Courtyard, the Koi Terrace and the Shrine Summit. Each floor is shorter and denser, with bigger boards (6×6 to 7×7, 7×7 to 9×9, then 9×9), 40 % more gold and 50 % more XP per puzzle.
[*]A shop waits right after each boss. In the middle of Floors 2 and 3 stands the warden of the garden you skip: an Elite puzzle, and after it you choose a relic.
[*]The story's third chapter opens on Floor 2, Summit in Sight asks for the last floor, and the Swift Path challenge is a win in under 25 minutes. A run you saved before keeps its five floors.
[/list]

[h2]Classes[/h2]
[list]
[*][b]Lantern Seer:[/b] a light floats over an empty cell in every puzzle. The right digit there catches it: +1 HP, +8 gold or +2 pencil, at random, and the light moves on. Every third light writes a digit too, and it lifts the fog around it.
[*][b]Shrine Archivist:[/b] the first pencil mark in a cell is free (two at Level 20). Narrow a cell to two notes with the right one among them, and it has a 30 % chance to write itself.
[*][b]Garden Monk:[/b] a puzzle without mistakes heals 2 HP (3 at Level 15). [b]Number Freak:[/b] a puzzle without mistakes gives a reroll token and 10 gold.
[*][b]Reed Duelist:[/b] a puzzle without pencil marks pays half its gold again and heals 1 HP. [b]Quiet Cartographer:[/b] sees every tile from the start of a floor; after a perfect puzzle the next one is half a star easier.
[*]The class items work now. They did nothing when used before, and the Reed Duelist could not be unlocked.
[/list]

[h2]Items[/h2]
[list]
[*]The Golden Kintsugi Jar turns your next 3 mistakes in a puzzle into the right digit. Double or Quits doubles the puzzle's gold or costs 1 HP when it ends.
[*]The Koi Dragon Scale completes two units, the Garden Rake removes every note the board already rules out, the Zen Sand Sifter writes the two possible digits into up to 4 cells, and the Solver completes boxes as it says.
[*]The Pattern Scroll and the fog lanterns are offered only where they have something to do.
[/list]

[h2]Easier to follow[/h2]
[list]
[*][b]A card for each new rule:[/b] the first time a puzzle brings a rule you have not met, it is explained before the clock starts.
[*]The chosen cell wears a red frame, the chosen path tile a red brush ring, and a full bag compares the two items side by side.
[*]An event shows what happened and every number it changed. The shop stays open after you buy something.
[*][b]The Daily Walk has its own save[/b]: starting it no longer ends your saved Garden Run.
[/list]

[h2]Send feedback from the game[/h2]
[list]
[*]Press F8, or Options › Run › Send feedback. Write what happened and tick what goes with it: the screenshot, the run's seed and board, the log, your save. Nothing goes without a tick.
[/list]

[h2]Fixed[/h2]
[list]
[*]After a rest the path showed the old HP; a solved board could keep some fog; at a large Text Size words broke in the middle.
[*]Countdown, Haunted Cell, Crumbling Region and Pressure Wave showed a code name and no rule on the rule slip.
[*]A boss with Full Kropki could start without some of its chosen lines, circles or thermos.
[/list]

Thank you, Paul and everyone who played and wrote! Found a bug? Press F8 in the game, or tell us in the Steam discussions or on our Discord.
```

## Steam announcement: 0.4.2 and 0.4.3 together

Only if 0.4.2 has no announcement of its own yet. Title: **Update v0.4.3: three floors, trades and feedback from the game**

Summary: The Garden Run is three denser floors now. Seven relics, four items and seven events, many of them trades;
leaderboards with your friends; a sound for every run moment; new passives; and F8 sends your feedback from the game.

```
[h2]Three floors[/h2]
[list]
[*][b]The Garden Run is three floors:[/b] the Bamboo Courtyard, the Koi Terrace and the Shrine Summit, shorter and denser, with bigger boards, 40 % more gold and 50 % more XP per puzzle.
[*]A shop waits right after each boss; in the middle of Floors 2 and 3 stands the warden of the garden you skip, and after it you choose a relic. A run you saved before keeps its five floors.
[/list]

[h2]New in the garden[/h2]
[list]
[*][b]Seven new relics[/b], among them three pacts that cost you to make you stronger: the Oni Pact, Smouldering Incense and the Dry Inkstone.
[*][b]Four new items:[/b] the Seed Pouch, Blood Ink, the Ink Pact and the Red Thread. [b]Seven new events[/b], most of them trades of HP, Max HP or pencil for gold, relics, items or a lifted curse.
[*]Reworked items: the Golden Kintsugi Jar turns your next 3 mistakes into the right digit, the Garden Rake and the Zen Sand Sifter work on your notes, the Solver completes boxes, and the class items finally work.
[/list]

[h2]Classes[/h2]
[list]
[*][b]Lantern Seer:[/b] catch the light over an empty cell with the right digit for +1 HP, +8 gold or +2 pencil. [b]Shrine Archivist:[/b] a cell narrowed to two notes may write itself.
[*][b]Garden Monk, Number Freak, Reed Duelist, Quiet Cartographer:[/b] new passives that reward a clean or a pencil-free puzzle.
[/list]

[h2]Leaderboards, sound and accessibility[/h2]
[list]
[*]Every board sits on a slip with All and Friends (Y or F); the seed of the week and the Monthly Walk have boards of their own.
[*]Every run moment has a sound, and the wrong-digit sound is audible on handhelds.
[*]Text Size works on every screen without breaking words, and a Dyslexia-Friendly Font is under Accessibility.
[/list]

[h2]Easier to follow[/h2]
[list]
[*]A card explains each rule the first time you meet it. The chosen cell and path tile are framed in red.
[*]Events show what happened, the shop stays open after a purchase, and the Daily Walk keeps its own save.
[/list]

[h2]Send feedback from the game[/h2]
[list]
[*]Press F8, or Options › Run › Send feedback, and tick what goes with your note: the screenshot, the run, the log, your save.
[/list]

[h2]Fixed[/h2]
[list]
[*]The controller in a puzzle's Options and in the relic choice after a boss; items no longer vanish on Save & Quit.
[*]The rule slip names every pressure rule; bosses with Full Kropki keep all their chosen rules; a solved board shows no fog; the path shows your HP after a rest.
[/list]

Thank you, Paul and everyone who played and wrote! Found a bug? Press F8 in the game, or tell us in the Steam discussions or on our Discord.
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.4.3 is out!
• The Garden Run is three denser floors: bigger boards, more gold and XP, a shop after every boss, and the skipped gardens' wardens in the middle
• The Lantern Seer follows a light through every puzzle, the Shrine Archivist's notes can write themselves, and the class items finally work
• A card explains each new rule before the puzzle starts, and events show what they did
• The Daily Walk keeps its own save, so your Garden Run waits
• F8 in the game sends me your feedback with the screenshot and the seed, if you tick them
A run you saved before keeps its five floors. Thank you for the notes! Bugs to #rotn-bugs, ideas to #rotn-feedback.
```
