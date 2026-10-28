# Playtest 0.3.0: analysis and fix plan

**Version:** 1.3 | **Date:** 2026-10-28 | **Status:** review (all items built or answered; K2, GAP-074 and the event tiles of the Garden Run wait for the owner, §6)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.3 | 2026-10-28 | review | The owner's answers of 2026-10-28 built: D2 (GAP-097), RM-208, RM-209, RM-211, RM-216 (way B, "Read an event"), RM-217 (main menu B and three screens); the review fixes; K1 checked read-only; the K2 list now covers the new screens. |
| 1.2 | 2026-10-28 | review | Status of every item (§6), a ten-step check list for a real pad (K2), the questions waiting for the owner. GAP-096 found and fixed on the way. |
| 1.1 | 2026-10-27 | review | Architecture review of the plan (§5) and the owner's answers (§4): GAP-074, GAP-077, GAP-080 and GAP-089 rewritten; change sets regrouped (§3). |
| 1.0 | 2026-10-27 | review | The owner's playtest of 0.3.0 (desktop 3440×1440 with an Xbox One controller, and a Lenovo Legion Go 2): 45 findings analysed against the code, sorted into defects (GAP-074..095), change requests (RM-206..217), checks, mockups and concepts. |

## 1. Sources

- The owner's list of 2026-10-27 (German, in the session), with three reference screenshots in `Builds/playtest/RunOfTheNine_0.3.0/`: `20261027_Sudoku_zu_Groß.png`, `20261027_Klasse_nicht_im_Kreis.png`, `20261027_Timer_pencil_overlap.png`.
- The owner's `Player.log` and `Player-prev.log` (Steam install, `D:/Steam/steamapps/common/Run of the Nine`), the Windows Application event log and `%LOCALAPPDATA%\CrashDumps`.
- A read-only code analysis of every item (three parallel passes: controller and input, puzzle screen and layout, rules, menus and Custom Puzzle). Line numbers below are from commit c029e47.
- A reproduction attempt: the 0.3.0 player started with the owner's profile (exclusive fullscreen 1920×1200), minimized and restored three times by script. It survived.

## 2. Findings

Kind: **GAP** a defect (fixed under its `GAP-NNN`), **RM** a change request (`docs/ROADMAP_BACKLOG.md`), **UI** needs a mockup the owner picks first (CLAUDE.md), **D** needs an owner decision (§4), **C** a concept, **M** a mockup, **K** a check.

### 2.1 Stability and display

| ID | Finding | Kind | Cause (short) | Fix |
|---|---|---|---|---|
| GAP-074 | The game ends by itself after minimizing or locking the PC | GAP, D | Not reproduced. The logs of both sessions end in a clean shutdown (Physics cleanup, Input System shutdown), and the event log has no crash since 2026-10-25 (that one was the D3D12 present error of the beta smoke test). Strong factor: the desktop profile runs **exclusive fullscreen at 1920×1200 on a 3440×1440 monitor**. The display settings live in the save, and Steam Cloud carries them from the Legion Go to the desktop. Every focus loss in exclusive fullscreen at a foreign resolution switches the monitor mode (`dxgi: SetFullscreenState failed (887a0022)` in the log). The only game code that quits is the main menu's Quit button, without a confirmation. | (1) Display settings per PC, not in the cloud save (PlayerPrefs; the profile keeps them only as the first-start default). (2) Quit asks first. (3) A log line for every quit request and every focus/pause change, so the next report shows the cause. (4) Ask the owner what "ends" looks like (§4). |
| GAP-075 | A profile switch re-applies the display mode (a window may jump to the centre) | GAP | `OptionsController.LoadShared` calls `ApplyDisplay` on every load (since GAP-071 also on every profile choice). | Apply the display only when mode or size differ from what is on screen. |
| GAP-076 | Borderless Fullscreen looks different and blurry/pixelated below the native resolution | GAP | `ApplyDisplay` (`OptionsController.cs:532-549`) passes the saved resolution to `Screen.SetResolution(w, h, FullScreenWindow)`: the window stays desktop-sized, the back buffer shrinks, and the Windows compositor stretches it. Exclusive mode instead changes the monitor mode. | Borderless always at the display's native size; the resolution row says so in Borderless. |
| GAP-089 | The board is too big at 3440×1440: it covers the title and the combo line | GAP | `BoardViewController.BuildGrid` (`:507-536`) makes the board a fixed 620 canvas px, while the title, status line and combo are fractions of the panel. With `matchWidthOrHeight 0.5` the canvas is only 932 px high at 21:9. Breaks above an aspect of about 1.85 (21:9, 32:9); 16:9, 16:10 and 1280×720 pass. | Cap the board by the free band of the panel (height and width) and rebuild when it changes; screenshot check at 3440×1440, 2560×1080, 5120×1440, 1920×1200, 1280×720, 1024×768. |
| GAP-090 | The class emblem sits off-centre in its ring | GAP | `HudViewController.EnsureClassBadge` (`:692-701`): the ring is a 76/84 px disc centred at x 0.18, the icon a fraction rect centred at x 0.20 and sized by the badge height. | Build the icon as a 76 px spot at the ring's centre. |

### 2.2 Controller and input

The common cause of most pad items: several buttons are read only through the legacy joystick numbers, which an Xbox pad on Windows does not report reliably (the same class as GAP-070, which fixed the D-pad and LT/RT).

| ID | Finding | Kind | Cause (short) | Fix |
|---|---|---|---|---|
| GAP-077 | LB/RB do not turn the Options pages on an Xbox One pad | GAP | `OptionsTabSet.cs:135-137` reads `JoystickButton4/5` through the legacy Input Manager only. | `PadButtons.Pressed(button)`: the Input System's `Gamepad.current` first, the legacy read as fallback; used for LB/RB/Start/Select/L3/R3 everywhere (Options, Harmony hotkeys, bag, Custom Puzzle book, puzzle). Virtual-pad PlayMode test. |
| GAP-078 | The pad gets stuck in the seed field on Select Class | GAP | `MainMenuBlueprintBuilder.ClassSelect.cs:114-135`: a legacy `InputField` with Automatic navigation; once selected it activates and eats Move/Submit/Cancel. | `Navigation.None` on the seed field (mouse and typing still work; the week's-seed button stays reachable). |
| GAP-079 | On the garden path the node tooltip appears at the bottom left with the pad | GAP | `NodeHoverHandler.OnSelect` (`:57-61`) passes `Vector2.zero` as the screen point. | Pass the node's own screen position (after the scroll settles). |
| GAP-080 | On the garden path the pad's Start, B and LT/RT do nothing | GAP | Found in the analysis: `HandleGamepadController` runs only inside `HandleKeyboard`, which runs only while the puzzle panel is active (`InRunController.cs:628-630, 1788, 1991`); the path handler `HandleGamepadPathFocus` is unreachable. | Dispatch the pad on the path too. |
| GAP-081 | The right stick no longer moves the number pad cursor | GAP | `HandleRightStickNumpad` (`InRunController.cs:2189-2216`) reads only the legacy axes in a swallowing `try/catch`; the cursor is not clamped to the board size and its ring shows only after the first move. | Read `Gamepad.current.rightStick` first; clamp; show the ring in puzzle focus. |
| RM-206 | Puzzle pad layout: LB → bag (items), LT → relics, RB → Solve/Pencil, Start → Options, Select → Save & Quit (tap), L3 → highlight the digit | RM | Today: LB toggles bag focus, LT/RT only switch bag slots, RB and X are pencil, Start opens Options, Select needs a 1.5 s hold, R3 highlights; relics have no pad focus. | Remap in `HandleGamepadPuzzleFocus`; a relic focus next to the bag focus; tests with a virtual pad. |
| RM-207 | Key hints follow the device in use (pad or keyboard) | RM | No "last input device" state exists; about 15 hint texts are fixed. | `InputDeviceTracker` fed by `InputRemapService.UpdateCursorVisibility`; hint texts with a pad and a keyboard variant (five catalogs). |
| GAP-082 | Highlighting the digit by key: Enter works only by accident, the hint text is wrong | GAP | Enter reaches `OnCellClicked` only through the EventSystem's Submit on the selected cell button; H and R3 are the real keys; the hint says "Double click", but one click toggles. | Enter as an explicit key (with H), L3 on the pad; hint line fixed in five catalogs. |
| GAP-083 | Keyboard and D-pad can walk onto the puzzle's Options and Save & Quit buttons | GAP | `InRunUiBlueprintBuilder.cs:276-278`: the two buttons stay in the Automatic navigation search. | `Navigation.None` on both (they keep their hotkeys and the mouse). |
| D1 / GAP-095 | Input lock: after a mouse click a digit waits, after an arrow move it does not | GAP, D | `_cellSelectedTime` is stamped only in `OnCellClicked` (`:2668`), never in `MoveSelection`. | Owner decides: lock on both, on neither, or keyboard only (§4). |
| GAP-094 | Moving with the keyboard colours cells purple; a mouse click does not | GAP | `MoveSelection` keeps a stale `_highlightValue`, so the conflict preview (`BoardViewController.cs:314-325, 379`) shows where that old digit may not go. | Moving behaves like a click (highlight = the digit in the new cell); the preview colour gets a legend line ("this digit cannot go here"). |

### 2.3 Menus and structure

| ID | Finding | Kind | Cause (short) | Fix |
|---|---|---|---|---|
| D2 / GAP-097 | Fixed: an A press on the title opened Select Class ("New Run"); nothing is chosen under the title now | caf2b79 |
| GAP-084 | Back on Select Class goes to the main menu after Game Modes → Incense Walk | GAP | `BtnClassBack` is wired to `BackToMainMenu` (`ClassSelect.cs:212`), which clears the screen history; Escape/B already go back one screen. | `BackFromPanel` on Select Class and on every other Back button of the menu flow. |
| RM-213 | Game Modes without the Garden Run curtain | RM, D | Start Game already starts it. The Garden curtain's slip is the only Harmony selector on Game Modes (UI-STYLE-025 says Harmony is on both screens). | Remove the curtain; Harmony stays on Select Class (owner to confirm, §4). |

### 2.4 Incense Walk

| ID | Finding | Kind | Cause (short) | Fix |
|---|---|---|---|---|
| GAP-085 | "+3 Pencil (20g)" overlaps the time readout | GAP | `RefreshBuyPencilButton` (`InRunController.Economy.cs:28-43`) shows the button again every frame; it does not know the walk. In the walk pencil marks are free (there is no pencil to buy). | No buy button in the walk (UI and `RunDirector.CanBuyPencil`). |
| D3 | "The pencil UI is missing" | D | The walk has no pencil count: marks are free. | A short "Pencil marks: free" line on the walk's slip (recommended), or nothing. |

### 2.5 Custom Puzzle

| ID | Finding | Kind | Cause (short) | Fix |
|---|---|---|---|---|
| GAP-086 | A chosen rule can be missing from the puzzle (9×9, 7 stars, Sum Dots: no dots) | GAP | `RunDirector.GetMandatoryModifiers` (`LevelGeneration.cs:1062-1070`) protects only the Garden Run's boss-gate picks; for a Custom Puzzle the retry never runs and `LastGenerationMissingModifiers` is read by nothing. Why the sum dots fail on that board is still to reproduce. | Custom Puzzle rules are mandatory; a test over many seeds for each catalog rule on 9×9 at 7 stars; a status line if a rule still cannot be placed. |
| GAP-087 | A class passive shows with "Free" resources (no class chosen) | GAP | `TutorialModeService.CreateTutorialRunState` always sets the simulated class (default Number Freak), so the HUD shows its passive and the passive also acts. | Free mode uses the neutral walker (as the Daily Walk does): no passive shown or applied. |
| GAP-088 | Two timers in a Custom Puzzle | GAP | The run timer is hidden only in Endless Zen (`HudViewController.cs:368`). | Hide it in every single-puzzle mode (Custom Puzzle, Sudoku Basics). |
| RM-209 | Built: help, seed and share code (UI-STYLE-037); one saved Custom Puzzle per profile with Continue on the screen and the main menu (CUSTOM-SAVE-001) | 5c8eb96, 712f186, 536710f |

### 2.6 Main game

| ID | Finding | Kind | Cause (short) | Fix |
|---|---|---|---|---|
| GAP-091 | Clicking next to "Legend" turns the whole grid gold, and it stays | GAP | The rule slip under the Legend pill (`HudViewController.cs:554-571`) calls `HighlightGlobalConstraint` for Nonconsecutive/Antiknight, which marks every pair, i.e. every cell; `ClearGlobalConstraintHighlight` has no caller. | A 3 s pulse like the legend's isolation, cleared on a second tap and on a new puzzle. |
| GAP-092 | A digit that is already complete on the board can be placed and costs HP | GAP | Only the number pad tile is disabled (`NumpadViewController.cs:44-80`); keys and the pad's A go straight to `EnterNumber`/`PlaceNumber`, which do not count. | `EnterNumber` refuses a complete digit with a status line, no mistake. |
| GAP-093 | Opposite Sums: "mirror cell" is not explained | GAP | `Boss.Description.OppositeCellsSum` uses "mirror cell", which is also the name of another rule (Mirror Cells). | New text in five catalogs: the two cells opposite each other through the centre. |
| RM-208 | Built: gold beside the Pencil, the detail slip for the passive, items and relics (UI-STYLE-036) | ac8c458, 689e718 |
| RM-210 | Option rows can be dragged with the mouse | RM | `BuildPaperValueRow` has only the arrow buttons. | A drag handler on the bar; the value saves on release. |
| RM-211 | Built: 100 gold, once per floor (UI-STYLE-038, SHOP-RULE-001) | 17c0540, 689e718 |
| RM-212 | Reward screen: the Golden Koi's extra gold as its own line | RM | The Koi gold (`RunDirector.cs:1646-1651`) is added into one gold total. | A transient `LastKoiGold` and a tally row when above 0. |
| RM-214 | Clue numbers (Opposite Sums and others) are too small on the Legion Go | RM | Text Size scales only the digits and pencil marks; overlay labels use fixed 7–10 px (`DrawOverlayText`). | Overlay labels and their badges scale with Text Size, with a floor. |

### 2.7 Checks, mockups and concepts

| ID | Item | Plan |
|---|---|---|
| K1 | Checked read-only (2026-10-28): Steam lists all 29 achievements, their API names match the game's 29; four already earned by testers. No public leaderboard yet (the day's and week's boards are made at the first score from a Steam build with them; that needs a check in the Steam client) | |
| K2 | A real controller | Virtual-pad PlayMode tests for every mapping; the owner checks the real Xbox One pad with a ten-press list after the fixes. |
| RM-217 | Built: main menu B (UI-STYLE-032), the Monthly Walk, Select Profile, Spirit Trials (033 to 035) | 536710f, 33cd967 |
| RM-215 | A title-screen sound | A code-made 8-bit sting (no paid service), played by `SplashScreenController` at Master × SFX; an optional clip id. |
| RM-216 | Built: way B, the Daily Walk on one path over Floors 2 and 4 with an event tile per floor; "Read an event on the path" replaces the Risk route goal (DAILY-WALK-009) | 23d9d7c |
| C (RM-209) | Custom Puzzle save & quit without touching the Garden Run save | Concept: an additive `ActiveCustomRunState`/`ActiveCustomPuzzle` pair like the Monthly Walk's. |

## 3. Plan (change sets, after the review in §5)

1. **Display and quits:** GAP-074 (per-PC display, applied once per start and by the setters; quit logging; Quit asks twice), GAP-075, GAP-076.
2. **Controller and input, one set:** GAP-077 (`PadButtons.Pressed/Held`), GAP-078, GAP-079, GAP-080 (Start, B, LT/RT on the path only), GAP-081, GAP-082, GAP-083, GAP-094, GAP-095 (lock everywhere), RM-206.
3. **Puzzle screen:** GAP-089 (`Expand` first), GAP-090, GAP-091, GAP-092, GAP-093, GAP-085 with the "pencil marks are free" line, GAP-088, the board click sound (RM-208 part).
4. **Menus and Custom Puzzle:** GAP-084, RM-213, GAP-086, GAP-087.
5. **Open owner question:** D2.
6. **Mockups** (owner picks): RM-217, RM-208, RM-209 help/seed, RM-211 button, RM-212 row.
7. **Features after the picks:** RM-207, RM-208, RM-209, RM-210, RM-211, RM-212, RM-214, RM-215.
8. **Concepts:** RM-216, the Custom Puzzle save slot.

Each fix: a failing test first where the code allows it, the fix, the five catalogs for new text, before/after screenshots for visible changes, the reviewers, a commit with the GAP or RM ID.

## 4. Owner decisions (answered 2026-10-27 unless marked open)

1. **Input lock (D1, GAP-095):** "Überall": the lock applies after a mouse click and after every keyboard, D-pad or stick move.
2. **Incense Walk pencil (D3):** "Notizen gratis": the walk's slip says that pencil marks are free; the buy button goes.
3. **Game Modes without the Garden Run (RM-213):** "Ja, nur Select Class": the curtain and its Harmony strip go; Harmony is chosen on Select Class.
4. **Quits (GAP-074):** "Fenster ist einfach weg": no error message, Steam shows the game as stopped. This matches the logs' clean shutdown (no crash).
5. **"New Game" at start (D2):** open. Which screen, on which device?

## 5. Review of this analysis

### 5.1 Architecture review (2026-10-27)

| # | Problem in plan 1.0 | Change |
|---|---|---|
| 1 | GAP-080: running the old path handler would make one A press act twice (the EventSystem's Submit plus the dead handler: two nodes, two shop purchases). | On the path only the missing reads are added (Start, B, LT/RT); A and movement stay with the EventSystem; the overlays are not routed. |
| 2 | GAP-074: "the profile as first-start default" would copy the Legion Go's exclusive 1920×1200 to the desktop again; the built-in default is also exclusive 1920×1080 on any screen. | Without a per-PC value the game keeps what Unity opened (borderless at the native size); Reset Display gives Borderless at the native size. |
| 3 | GAP-074/075: `ApplyDisplay` runs inside every options load (profile choice, first-run setup, every new controller). | The display is applied once per process at startup and by the four display setters; that alone ends GAP-075. Per-PC values in PlayerPrefs through `TestSaves.PrefKey`; the profile fields are still written (DISPLAY-MODE-005 stays). |
| 4 | GAP-077: OR-ing the Input System and the legacy read can fire twice a frame apart, and `Map` contradicts a forced D-input mode on an Xbox pad. | With a `Gamepad` present only the Input System is read, by the Xbox button; legacy plus `Map` only for other joysticks. `Held()` for the B and Select holds. InputAndProfilesSpec changes (INPUT-PAD-002 applies to non-gamepad joysticks). The root cause on the owner's pad stays unproven: a one-line log records which reader saw the first LB/RB/Start/Select press. |
| 5 | GAP-089: capping the board by the blueprint band shrinks the approved 16:9 board and fails at 32:9 against the 48 px cell floor. | First the one-line alternative `screenMatchMode = Expand` on the in-run canvas (21:9 keeps 16:9 proportions); screenshots at all six sizes decide. |
| 6 | Conflicts: GAP-094 must not call `OnCellClicked` (lock, sound); an explicit Enter key would toggle twice with the Submit click; GAP-077/080/081/082, RM-206 and GAP-095 all rewrite the pad handler. | GAP-094 sets only the highlight value; the board cells leave the EventSystem's navigation, so Enter and A act once through the game's own handler; the controller items are one change set. |
| 7 | Rules: the IDs were not registered; new UI (quit confirmation, legend line, slip line, hint variants) needs a mockup or the existing look; spec bumps for GAP-092, GAP-077, GAP-074, GAP-088; quit logging must not block Alt+F4 or a Windows shutdown; `SaveDurabilityReadinessTests` checks the focus handler's source. | IDs registered in GAME_GAPS and ROADMAP_BACKLOG; the quit confirmation reuses the path quit modal's two-press pattern; spec bumps per change set; `wantsToQuit` only logs. |

### 5.2 Own review

- **Shared causes first.** GAP-077 (legacy pad reads) explains C1 and probably the right stick; one reader fixes several items. GAP-089 explains why item and relic texts are "behind the puzzle" at 21:9; RM-208's slip still needs the mockup.
- **Not over-claiming.** GAP-074 is not reproduced; the plan removes the strongest factor (exclusive mode at a foreign resolution, carried by the cloud) and adds evidence logging instead of guessing at a single cause.
- **Save compatibility.** Only additive fields (RM-211's reroll counter, the custom save slot). Display settings move to PlayerPrefs but are still read from the profile when no per-PC value exists, so nothing is lost.
- **Spec impact.** RM-213 touches UI-STYLE-025/027 (`docs/design/layout-proposals.md`); GAP-076 changes DISPLAY-MODE rules; RM-206 changes InputAndProfilesSpec's controller table; each edit bumps its spec.
- **Out of reach for a test here:** a real Xbox pad, the Steam client with published achievements, locking the PC.

## 6. Status (2026-10-28)

| Item | Status | Commit |
|---|---|---|
| GAP-074 | In part: the display belongs to the PC, Quit asks twice, every quit request and focus change is logged. Not reproduced; waiting for the Player.log of the next quit. | d8a8dcc |
| GAP-075, GAP-076 | Fixed | d8a8dcc |
| GAP-084, GAP-085, GAP-088 to GAP-093, D3 | Fixed (D3: "pencil marks are free" on the Incense slip) | 164c36a |
| GAP-077 to GAP-083, GAP-094, GAP-095, RM-206 | Fixed with a virtual pad; the real pad is K2 | dfcfc4c |
| GAP-086 | In part: not reproduced over three region kinds; a missing rule is now named on the status line | 714b122 |
| GAP-087, RM-213 | Fixed / built | 714b122 |
| GAP-096 (new) | Fixed: Spirit Trials showed "???" for the stars | 1f34382 |
| RM-207 | Built (ten hints; a pad connected at start shows the pad's buttons) | d0188e3 |
| RM-208 | Built: gold beside the Pencil, the detail slip for the passive, items and relics (UI-STYLE-036) | ac8c458, 689e718 |
| RM-209 | Built: help, seed and share code (UI-STYLE-037); one saved Custom Puzzle per profile with Continue on the screen and the main menu (CUSTOM-SAVE-001) | 5c8eb96, 712f186, 536710f |
| RM-210, RM-214 | Built; after the review a drag saves once on release, and the Quadruples and Little Killer clues grow too | 1ece097, d0188e3 |
| RM-211 | Built: 100 gold, once per floor (UI-STYLE-038, SHOP-RULE-001) | 17c0540, 689e718 |
| RM-212 | Built | 4bdbdb3 |
| RM-215 | Built | ce4ef47 |
| RM-216 | Built: way B, the Daily Walk on one path over Floors 2 and 4 with an event tile per floor; "Read an event on the path" replaces the Risk route goal (DAILY-WALK-009) | 23d9d7c |
| RM-217 | Built: main menu B (UI-STYLE-032), the Monthly Walk, Select Profile, Spirit Trials (033 to 035) | 536710f, 33cd967 |
| D2 / GAP-097 | Fixed: an A press on the title opened Select Class ("New Run"); nothing is chosen under the title now | caf2b79 |
| K1 | Checked read-only (2026-10-28): Steam lists all 29 achievements, their API names match the game's 29; four already earned by testers. No public leaderboard yet (the day's and week's boards are made at the first score from a Steam build with them; that needs a check in the Steam client) | |
| K2 | The check list below | |

### K2: a real pad, ten checks

On the desktop with the Xbox One pad and on the Legion Go. If a button does nothing, send the `Player.log`
(`%USERPROFILE%\AppData\LocalLow\Kratossos\sudoku_rouge_like\Player.log`): each button's first press is logged as a
`[Pad]` line that says whether the Input System and the old input saw it.

1. Main menu: the D-pad moves, A opens, B goes back from Options.
2. Options: LB and RB turn the pages, one page per press; left and right change a value.
3. Select Class: down reaches the seed field, and the D-pad leaves it again.
4. Garden path: the D-pad chooses a tile and its text appears beside the tile; Start opens Options and B closes them; LT and RT scroll.
5. Puzzle: the D-pad moves the cell; the right stick moves the number pad cursor and A places the digit; RB switches Solve and Pencil.
6. L3 highlights the cell's digit; L3 again clears it.
7. LB opens the bag (with an item in it), A uses, B goes back; LT opens the relics.
8. Select saves and quits; Start opens Options.
9. With an input lock set, a digit after a D-pad move waits like one after a click.
10. The hint under the number pad shows the pad's buttons after a pad press and the keys after a key.
11. The title screen: press A while it shows; the main menu must follow, not Select Class (GAP-097).
12. Main menu: the D-pad walks the tablets (and the run card); right on Custom Puzzle reaches Continue when a puzzle is
    saved; Spirit Trials: left and right choose a stele, A goes on; Select Profile: Delete asks first.
13. Puzzle: LB into the bag shows the item on the slip at the foot of the left column; LT shows a relic there.

### Waiting for the owner

1. K2 on a build with these fixes (0.3.0 does not have them): a new build needs the owner's word on its version.
2. GAP-074: the `Player.log` of the next quit the game did not ask for.
3. Event tiles: the Garden Run's routes have rolled no event tiles since March 2026 (commit 0f538b8 took them out of
   `RunGraphService.RollNodeType`); only the Daily Walk's path has one now. Bring them back to the Garden Run, or keep
   it so? (A saved floor is rebuilt from its seed, so the change would come with a new graph version to keep saved floors.)
