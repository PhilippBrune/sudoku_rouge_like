# The story of the Nine

**Version:** 1.0 | **Date:** 2026-10-25 | **Status:** new (concept for the owner's pick; nothing built)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-25 | new | Owner question: "Does the game tell a story? If not, create a concept for how the classes could be part of the story and how to add it to the game." Suggested roadmap item RM-125. |

## 1. What the game tells today

There is no story, only mood lines:

- 8 class lines at the start of a run (`Narrative.ClassIntro.*`, "You set one palm on warm stone...").
- 16 class lines at the end of a run, one for a win and one for a loss (`Narrative.ClassRunEnd.*`).
- 5 floor lines (`Narrative.FloorEntry.*`) and 10 lines on cursed tiles (`Narrative.Node.Cursed.*`).
- Closing lines on the results of Endless Zen, Spirit Trials and the Monthly Walk.
- The bosses have no names in the game; the mockups use working titles ("the Shrine Oni").

The lines set a calm, garden-like tone, but they do not add up: nobody knows why the walker is in the garden, who the bosses are, or what a win means.

## 2. The premise (proposal)

The title gives it: **the Nine.**

- The garden was laid out by **nine keepers**, one for each digit. Each keeper held one number in order, and the garden's paths ran true.
- **The ninth keeper left.** Nobody knows why. Without the ninth, the patterns slipped: paths became puzzles, and five **wardens** grew out of the disorder, one on each floor, each holding one kind of rule (lines, cages, dots, regions, the summit's mixed rules).
- **The eight classes are the eight keepers who stayed.** Each climbs the garden for their own reason and learns a part of the truth.
- **The ending:** after a win with every class, the last chapter opens. The ninth keeper is the one who walks all eight paths: the player. *Run of the Nine.*

The tone stays as it is: quiet, short sentences, no fight scenes. The wardens are not evil; they are rules that forgot what they were for.

## 3. The classes in the story

| Class | Number | Why they climb | What they learn |
|---|---|---|---|
| Number Freak | 1 | Counts everything; the missing ninth is a hole in every count. | The ninth left on purpose. |
| Garden Monk | 2 | Keeps the garden's breath; feels it falter. | The wardens are in pain, not angry. |
| Shrine Archivist | 3 | Keeps the records; the ninth's page is torn out. | The torn page is the first rule ever written. |
| Koi Gambler | 4 | Bet that the ninth will return; wants to win the bet. | The ninth bet too, and lost. |
| Stone Gardener | 5 | Repairs paths; the summit path breaks faster than any can be fixed. | Some paths must be broken to be walked. |
| Lantern Seer | 6 | Sees hidden rules; sees a ninth shadow behind every warden. | The shadow is the ninth's own. |
| Reed Duelist | 7 | Swore a vow to the ninth and wants it answered. | A vow can be kept by someone else. |
| Quiet Cartographer | 8 | Maps the garden; one blank space stays on every map. | The blank space is where the walker stands. |

## 4. How it is told (no cutscenes)

All on screens that exist, a line or two at a time:

| Moment | Where | What |
|---|---|---|
| A chapter opens | The floor entry (UI-STYLE-021): two seconds of the painting with the floor's banner | One line of the class's current chapter under the banner (skippable as today). |
| The warden | The boss gate (UI-STYLE-004) | The warden's name and one line it says, different per class. |
| The warden falls | The offering table (UI-STYLE-005) | One line of what the class learned. |
| The run ends | The run-end scroll | The existing class lines become chapter-aware ("Chapter 3 of 5"). |
| Reading again | The Codex: a fifth bookmark **Tales** | Every fragment found, per class, in order, with the missing ones as "???" like other Codex entries. |

**Chapters:** five per class, opened by milestones of that class: the first run, the first boss defeated, Floor 3 reached, the first win, a win at Harmony III. That is 40 chapters of two or three sentences, plus 5 wardens with a name and 8 lines each, plus the last chapter.

## 5. What it touches

- Text: about 120 short lines, in five languages (the catalogs; French "vous", Spanish "tú", Portuguese "você").
- Warden names: five names to decide (the owner's pick; the mockups' working titles can be a start).
- A small `StoryService`: which chapter a class is on, which fragments are found (saved in the profile, old saves start at chapter 1).
- The floor entry, the boss gate, the offering table, the run-end scroll: one line each.
- The Codex: the Tales bookmark (the book exists, UI-STYLE-018).
- Achievements: "first chapter", "all chapters of a class", "the ninth keeper".

Effort: M for the code (a few days), plus the writing. Requirement IDs follow in GameDesignSpec when the owner picks.

## 6. Questions for the owner

1. The premise of the nine keepers and the player as the ninth: yes, or another direction?
2. The five wardens: names from the owner, or proposals first?
3. Chapters opened by class milestones (as proposed), or by Harmony levels for all classes together?
4. Is the Codex's Tales page wanted, or only the lines in the run?
5. Who writes: drafts from me for the owner to edit, or the owner's own text?
