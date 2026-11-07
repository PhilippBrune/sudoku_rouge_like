# Release notes: v0.4.3

Release build of 2026-11-07 from `feature/game-ui-refactor` (tag `v0.4.3`, local). Owner's request (2026-11-05): "Bau das
noch in 0.4.3 mit ein und kombiniere die releasenots von 0.4.2 & 0.4.3". It holds the owner's picks from the 0.4.1
playtest on a Legion Go (path A, bag B, T1, F3, M2, P-NF, P-RD, P-QC) and the fixes GAP-119, GAP-120 and GAP-121. 0.4.2
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

Title: **Update v0.4.3: three floors**

Summary: The Garden Run is three denser floors now, with the skipped gardens' wardens in the middle and a shop after
every boss. Four classes have new passives, and the path and the full bag are easier to read on a handheld.

```
[h2]Three floors[/h2]
[list]
[*][b]The Garden Run is three floors:[/b] the Bamboo Courtyard, the Koi Terrace and the Shrine Summit. Each floor is shorter and denser, with bigger boards (6×6 to 7×7, 7×7 to 9×9, then 9×9), 40 % more gold and 50 % more XP per puzzle.
[*]A shop waits right after each boss.
[*]In the middle of Floors 2 and 3 stands the warden of the garden you skip, the Moss Garden's and the Stone Lantern Walk's: an Elite puzzle, and after it you choose a relic.
[*]The story's third chapter opens on Floor 2, Summit in Sight asks for the last floor, and the Swift Path challenge is a win in under 25 minutes.
[*]A run you saved before this update keeps its five floors.
[/list]

[h2]Classes[/h2]
[list]
[*][b]Garden Monk:[/b] a puzzle without mistakes heals 2 HP (3 at Level 15). The digits no longer heal one by one.
[*][b]Number Freak:[/b] a puzzle without mistakes gives a reroll token and 10 gold.
[*][b]Reed Duelist:[/b] a puzzle without pencil marks pays half its gold again and heals 1 HP (2 at Level 15).
[*][b]Quiet Cartographer:[/b] sees every tile's size and stars from the start of a floor; after a perfect puzzle the next one is half a star easier (a whole star at Level 15).
[/list]

[h2]Easier to read on a handheld[/h2]
[list]
[*]The path tile you chose with the pad or the keys wears a red brush ring and your class's marker.
[*]A full bag shows the new item and the one you would give up side by side, with your bag as a row below.
[/list]

[h2]Items[/h2]
[list]
[*]The Lantern of Clarity and the Garden Lantern are no longer rewards, because without fog they did nothing. The market sells them when the floor or the boss brings fog, or a fog curse is active.
[/list]

[h2]Fixed[/h2]
[list]
[*]The Number Freak's token, the Reed Duelist's HP and the Quiet Cartographer's preview were on the class screen but never happened.
[*]Countdown, Haunted Cell, Crumbling Region and Pressure Wave showed a code name and no rule on the rule slip. Thanks to the player who sent a photo of it!
[*]A boss with Full Kropki could start without some of its chosen lines, circles or thermos while the slip listed them.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Steam announcement: 0.4.2 and 0.4.3 together

Only if 0.4.2 has no announcement of its own yet. Title: **Update v0.4.3: three floors, trades and friends on the boards**

Summary: The Garden Run is three denser floors now. Seven relics, four items and seven events, many of them trades;
leaderboards with your friends; a sound for every run moment; Text Size on every screen; new passives for four classes.

```
[h2]Three floors[/h2]
[list]
[*][b]The Garden Run is three floors:[/b] the Bamboo Courtyard, the Koi Terrace and the Shrine Summit. Each floor is shorter and denser, with bigger boards (6×6 to 7×7, 7×7 to 9×9, then 9×9), 40 % more gold and 50 % more XP per puzzle.
[*]A shop waits right after each boss. In the middle of Floors 2 and 3 stands the warden of the garden you skip: an Elite puzzle, and after it you choose a relic.
[*]The story's third chapter opens on Floor 2, Summit in Sight asks for the last floor, and the Swift Path challenge is a win in under 25 minutes. A run you saved before keeps its five floors.
[/list]

[h2]New in the garden[/h2]
[list]
[*][b]Seven new relics.[/b] The Cracked Bell takes 2 Max HP now and gives 2 back for every boss you defeat. Twin Koi gives every item you get one more charge. The Gold Leaf Brush turns a combo of 10 into a rarer next reward. The Lantern of Daring pays +50 % gold on the Risk route and 20 % less on the Calm one.
[*][b]Three pacts:[/b] the Oni Pact (every mistake costs 1 HP more, every puzzle pays +50 % gold and shows one more reward), Smouldering Incense (each puzzle costs 1 HP to start and begins with one box solved) and the Dry Inkstone (5 less pencil, but every combo of 5 heals 1 HP).
[*][b]Four new items.[/b] The Seed Pouch grows into a Rare item after two puzzles. Blood Ink costs 2 HP and writes 3 digits, the Ink Pact costs 6 pencil marks and completes a row, column or box, and the Red Thread costs 1 Max HP for a random relic.
[*][b]Seven new events:[/b] the Koi Pond Wager, the Fallen Lantern, the Wandering Monk, the Bone Carver, the Ink Spring, the Blood Lantern (it only shows up while you carry a curse) and the Ghost Merchant. Most of them trade HP, Max HP or pencil marks for gold, relics, items or a lifted curse.
[*]The Lantern of Clarity and the Garden Lantern are no longer rewards; the market sells them where there is fog.
[/list]

[h2]Classes[/h2]
[list]
[*][b]Garden Monk:[/b] a puzzle without mistakes heals 2 HP (3 at Level 15). The digits no longer heal one by one.
[*][b]Number Freak:[/b] a puzzle without mistakes gives a reroll token and 10 gold.
[*][b]Reed Duelist:[/b] a puzzle without pencil marks pays half its gold again and heals 1 HP (2 at Level 15).
[*][b]Quiet Cartographer:[/b] sees every tile's size and stars from the start of a floor; after a perfect puzzle the next one is half a star easier (a whole star at Level 15).
[/list]

[h2]Leaderboards[/h2]
[list]
[*][b]All or Friends:[/b] every board sits on a slip with two bookmarks. Y on the controller or F on the keyboard shows where you stand among your friends.
[*]A Garden Run on the seed of the week and a finished Monthly Walk now have boards of their own, shown at the end.
[/list]

[h2]Sound[/h2]
[list]
[*]Using an item, taking a reward or a relic, healing, a combo of 5 and 10 and its break, low HP, the last pencil mark, gold, lifting fog, the boss gate, a new floor, a defeated boss, a new level and an achievement each have their own sound now, and choosing a cell ticks softly.
[*]The sounds for a wrong digit and for losing HP are higher, so laptop and handheld speakers play them.
[/list]

[h2]Accessibility and handhelds[/h2]
[list]
[*][b]Text Size works on every screen[/b], not only on the board: menus and run screens grow as far as their boxes have room.
[*][b]Dyslexia-Friendly Font:[/b] a new switch under Accessibility sets every text in OpenDyslexic.
[*]The path tile you chose with the pad or the keys wears a red brush ring and your class's marker.
[*]A full bag shows the new item and the one you would give up side by side, with your bag as a row below.
[/list]

[h2]Fixed[/h2]
[list]
[*]With a controller, the D-pad and the stick move through the Options you open from a puzzle, and through all relic offerings after a boss.
[*]An item that asks you to pick cells is no longer lost when you cancel it, open the Options or Save & Quit first; it goes back into your bag.
[*]Text Size can be changed during a run too, and it enlarges the rule slip beside the board.
[*]The Number Freak's token, the Reed Duelist's HP and the Quiet Cartographer's preview were on the class screen but never happened.
[*]Countdown, Haunted Cell, Crumbling Region and Pressure Wave showed a code name and no rule on the rule slip.
[*]A boss with Full Kropki could start without some of its chosen lines, circles or thermos while the slip listed them.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.4.3 is out!
• The Garden Run is three denser floors: bigger boards, more gold and XP, a shop after every boss, and the skipped gardens' wardens in the middle
• New passives for the Garden Monk, the Number Freak, the Reed Duelist and the Quiet Cartographer
• On a handheld you see which path tile you chose, and a full bag compares the two items side by side
• Fog lanterns are sold where there is fog, not rolled as rewards
• Fixed: three class passives that never worked, the "HauntedCell" rule slip, and bosses missing some of their chosen rules
A run you saved before keeps its five floors. Bugs go to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```
