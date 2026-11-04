# Release notes: v0.4.2

Release build made 2026-11-04 from `feature/game-ui-refactor` (tag `v0.4.2`, local). Owner's request (2026-11-04):
"update das zu einem neuen build v0.4.2". It follows 0.4.1, which the owner built, uploaded and announced the same day
(that build still shows "Version 0.4.0" in the menu: `bundleVersion` had not been raised). Player-facing text comes from
`CHANGELOG.md` 0.4.2. Not uploaded and not live.

## Build

See the build section of `docs/release/release-readiness-status.md` (0.4.2) for the build, the tests and the smoke test.
SteamPipe build description: **v0.4.2**.

## Going live (owner)

1. **Upload:** SteamPipe → Builds → *Upload Depots via HTTP*: `Builds/RunOfTheNine_0.4.2_win64.zip`, depot 4858471,
   description "v0.4.2", then commit the upload. It is not live after this step.
2. **Set live:** SteamPipe → Builds → set the build live on `default`.
3. **Announcement:** Events & Announcements → Create → *Patch notes / Small update*, title and text below; publish it
   after the build is live. The Steam news mirror then posts it to #rotn-news by itself (within 30 minutes).
4. **Discord**, after the build is live: `python tools/discord/discord_admin.py replace known-issues
   tools/discord/texts/known_issues.md` once the "current build" line says v0.4.2 (I change it when you say the build is live).

## Steam announcement

Title: **Update v0.4.2: relics, trades and friends on the boards**

Summary: Seven relics, four items and seven events, many of them trades: pay HP, Max HP or pencil marks for something
strong. Every leaderboard now shows your friends too, every run moment has a sound, and Text Size works on every screen.

```
[h2]New in the garden[/h2]
[list]
[*][b]Seven new relics.[/b] The Cracked Bell takes 2 Max HP now and gives 2 back for every boss you defeat. Twin Koi gives every item you get one more charge. The Gold Leaf Brush turns a combo of 10 into a rarer next reward. The Lantern of Daring pays +50 % gold on the Risk route and 20 % less on the Calm one.
[*][b]Three pacts:[/b] the Oni Pact (every mistake costs 1 HP more, every puzzle pays +50 % gold and shows one more reward), Smouldering Incense (each puzzle costs 1 HP to start and begins with one box solved) and the Dry Inkstone (5 less pencil, but every combo of 5 heals 1 HP).
[*][b]Four new items.[/b] The Seed Pouch grows into a Rare item after two puzzles. Blood Ink costs 2 HP and writes 3 digits, the Ink Pact costs 6 pencil marks and completes a row, column or box, and the Red Thread costs 1 Max HP for a random relic.
[*][b]Seven new events:[/b] the Koi Pond Wager, the Fallen Lantern, the Wandering Monk, the Bone Carver, the Ink Spring, the Blood Lantern (it only shows up while you carry a curse) and the Ghost Merchant. Most of them trade HP, Max HP or pencil marks for gold, relics, items or a lifted curse.
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

[h2]Accessibility[/h2]
[list]
[*][b]Text Size works on every screen[/b], not only on the board: menus and run screens grow as far as their boxes have room.
[*][b]Dyslexia-Friendly Font:[/b] a new switch under Accessibility sets every text in OpenDyslexic.
[/list]

[h2]Fixed[/h2]
[list]
[*]With a controller, the D-pad and the stick move through the Options you open from a puzzle (found on a Legion Go).
[*]The relic offerings after a boss can be chosen with the D-pad, not only the middle one.
[*]An item that asks you to pick cells (Koi Reflection, Temple Incense, Wind Chime, Pattern Scroll) is no longer lost when you cancel it, open the Options or Save & Quit first; it goes back into your bag.
[*]Text Size can be changed during a run too, and it enlarges the legend beside the board.
[/list]

Found a bug or have feedback? Tell us in the Steam Community discussions or on our Discord. Thank you for helping shape the garden!
```

## Discord (short version, for #release-drafts first)

```
Run of the Nine v0.4.2 is out!
• Seven relics, four items and seven events, many of them trades: pay HP, Max HP or pencil for something strong
• Every leaderboard shows All or Friends (Y or F), with new boards for the seed of the week and the Monthly Walk
• A sound for every run moment, and the wrong-digit sound is audible on handhelds now
• Text Size on every screen and a Dyslexia-Friendly Font
• Fixed: the controller in a puzzle's Options and in the relic choice after a boss; items no longer vanish on Save & Quit
Bugs go to #rotn-bugs, ideas to #rotn-feedback. Thank you!
```
