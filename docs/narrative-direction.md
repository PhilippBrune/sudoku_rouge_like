# Narrative Direction

**Version:** 1.1 | **Date:** 2026-10-27 | **Status:** new (the story proposal at the end is not built)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-27 | new | Owner request (2026-10-27): the story concept (v1.0, 2026-10-25; its file is removed) is merged as the section "Proposal: the story of the Nine". A proposal for the owner's pick (RM-125): nothing built, no requirement IDs yet. The text above has no version before 1.1 and is unchanged. |

## Current Direction

Run of the Nine is a Sudoku roguelike journey through five stylized Japanese garden spaces. The run is framed as a passage through living gardens, spirit gates, relic offerings, and boss thresholds rather than an everyday city-park walk.

This direction aligns with:

- `docs/GameDesignSpec.md`
- `docs/AudioVisualDirection.md`
- `docs/PathSystem_GardenOverview.md`
- `Assets/Scripts/Run/ParkNarrativeService.cs`
- `Assets/Scripts/Core/LocalizationService.cs`

## Floor Identity

1. Bamboo Courtyard: raked sand, bamboo gates, patient opening puzzles.
2. Moss Garden: koi pond, moss stones, quiet water rhythm.
3. Koi Terrace: bridges, black water, reflected patterns.
4. Stone Lantern Walk: lantern glow, autumn leaves, stronger pressure.
5. Shrine Summit: torii, mist, final offering, summit spirit.

## Narrative Rules

- Keep text short enough for generated UI panels.
- Prefer concrete garden imagery over exposition.
- Treat bosses as threshold spirits or garden guardians.
- Treat relics as offerings, charms, tokens, or found shrine objects.
- Keep class, relic, curse, and boss copy compatible with English and German localization.
- Avoid modern city-park details unless a future mode intentionally uses them as a variant.

## Runtime Narrative Hooks

- Floor and node flavor is served by `ParkNarrativeService.GetFloorEntryFlavor()` and `GetNodeFlavor()`.
- Cursed nodes use the same floor-aware flavor hook as puzzle, shop, rest, relic, event, and boss nodes, so curse risk is presented as a garden story beat before the mechanical confirmation panel appears.
- The first run intro card includes `ParkNarrativeService.GetClassIntroFlavor()` so each selected class enters the garden with a short identity beat.
- The victory/defeat screen uses the class-aware `ParkNarrativeService.GetRunEndFlavor(victory, classId)` overload, appending a class-specific ending line after the shared run conclusion.
- Narrative keys for floor, node, class intro, and class endings are exposed through `ParkNarrativeService.GetLocalizationKeys()` and covered by release localization tests.

## Localization Contract

Narrative runtime keys remain under `Narrative.*` to preserve existing UI calls and tests. English fallback text lives in `ParkNarrativeService`; translator-facing English and German text lives in `Assets/Resources/Localization/*.json` and is loaded by `LocalizationService`.

Before release, narrative text still needs runtime layout validation in German and at 150% accessibility font scale.

## Proposal: the story of the Nine (not built)

The owner asked (2026-10-25): "Does the game tell a story? If not, create a concept for how the classes could be part of
the story and how to add it to the game." The proposal is for the owner's pick (roadmap RM-125). Nothing is built.
The chapters must follow the Narrative Rules above: short text, garden imagery, bosses as guardians.

### What the game tells today

Only mood lines: 8 class lines at the start of a run (`Narrative.ClassIntro.*`), 16 class lines at the end of a run, a
win and a loss each (`Narrative.ClassRunEnd.*`), 5 floor lines (`Narrative.FloorEntry.*`), 10 lines on cursed tiles
(`Narrative.Node.Cursed.*`) and closing lines on the mode results. The bosses have no names in the game. The lines set a
calm tone, but they do not add up: nobody knows why the walker is in the garden, who the bosses are, or what a win means.

### The premise

- The garden was laid out by **nine keepers**, one for each digit. Each held one number in order, and the paths ran true.
- **The ninth keeper left.** Nobody knows why. Without the ninth the patterns slipped: paths became puzzles, and five
  **wardens** grew out of the disorder, one on each floor, each holding one kind of rule (lines, cages, dots, regions,
  the summit's mixed rules).
- **The eight classes are the eight keepers who stayed.** Each climbs the garden for its own reason and learns a part of
  the truth.
- **The ending:** after a win with every class, the last chapter opens. The ninth keeper is the one who walks all eight
  paths: the player. The title says so: *Run of the Nine*.

The tone stays quiet, with short sentences and no fight scenes. The wardens are not evil; they are rules that forgot what
they were for.

### The classes in the story

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

### How it is told (no cutscenes)

All on screens that exist, a line or two at a time:

| Moment | Where | What |
|---|---|---|
| A chapter opens | The floor entry (UI-STYLE-021): two seconds of the painting with the floor's banner | One line of the class's current chapter under the banner (skippable as today). |
| The warden | The boss gate (UI-STYLE-004) | The warden's name and one line it says, different per class. |
| The warden falls | The offering table (UI-STYLE-005) | One line of what the class learned. |
| The run ends | The run-end scroll | The existing class lines become chapter-aware ("Chapter 3 of 5"). |
| Reading again | The Codex: a fifth bookmark, **Tales** | Every fragment found, per class, in order, with the missing ones as "???" like other Codex entries. |

**Chapters:** five per class, opened by milestones of that class: the first run, the first boss defeated, Floor 3 reached,
the first win, a win at Harmony III. That is about 40 chapters of two or three sentences, five wardens with a name and
eight lines each, and the last chapter.

### What it touches

- About 120 short lines in five languages (French "vous", Spanish "tú", Portuguese "você").
- Five warden names, to decide (the owner's pick; the mockups' working titles can be a start).
- A small `StoryService`: which chapter a class is on and which fragments are found, saved in the profile (old saves
  start at chapter 1).
- The floor entry, the boss gate, the offering table and the run-end scroll: one line each.
- The Codex: the Tales bookmark (the book exists, UI-STYLE-018).
- Achievements: "first chapter", "all chapters of a class", "the ninth keeper".

Effort: M for the code (a few days), plus the writing. Requirement IDs follow when the owner picks.

### Owner's questions (open)

1. The premise of the nine keepers and the player as the ninth: yes, or another direction?
2. The five wardens: names from the owner, or proposals first?
3. Chapters opened by class milestones (as proposed), or by Harmony levels for all classes together?
4. Is the Codex's Tales page wanted, or only the lines in the run?
5. Who writes: drafts for the owner to edit, or the owner's own text?
