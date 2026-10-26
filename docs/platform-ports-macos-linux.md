# Plan: Run of the Nine on macOS and Linux / SteamOS

**Version:** 1.3 | **Date:** 2026-10-26 | **Status:** review (decision 3 taken: Steam Input; the Steam Deck runs the Windows build; Legion Go checked, X-input and D-input built; no build settings changed yet)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.3 | 2026-10-26 | review | X-input and D-input modes built (INPUT-PAD-002): the Legion Go's D-input mode and PlayStation pads without Steam Input press the right buttons. |
| 1.2 | 2026-10-25 | review | Owner question: does the controller work on a Lenovo Legion? Section "Lenovo Legion Go" added. The check found GAP-070 (in a run the D-pad and LT/RT did nothing on any Xbox pad, the Deck and the Legion Go included), fixed the same day. |
| 1.1 | 2026-10-22 | review | Owner decision 3: **Steam Input** (a default layout in Steamworks), not the Input System rewrite, and the Steam Deck as the next target. The Deck needs no build of its own: it runs the Windows build through Proton (section "Steam Deck"). Phase 1 shrinks to Steamworks settings and the Deck checks; the button work moves to the native Linux and macOS builds. |
| 1.0 | 2026-10-20 | new | Owner request after the 0.2.0 playtest: publish on macOS (with 64-bit Intel binaries) and on Linux + SteamOS. Code audit, build and Steamworks steps, risks, order. Roadmap item RM-107 (extends RM-102 Steam Deck Verified and RM-205). |

## Goal

The Steam store page lists Windows, **macOS** (with the "64-bit (Intel) binaries included" box ticked, and Apple Silicon too) and **Linux + SteamOS**, with one shared save and Steam Cloud on all three.

## What the code needs (audit, 2026-10-20)

| Area | Finding | Work |
|---|---|---|
| Engine | Unity 6000.3.10f1, Mono backend, Input System 1.11 with the legacy Input Manager both active (`activeInputHandler: 2`). | Install *Mac Build Support (Mono)* and *Linux Build Support (Mono)* in Unity Hub. Mono builds for both are made on Windows; IL2CPP for macOS would need a Mac. |
| Steamworks | Steamworks.NET 2025.163.0 ships `steam_api.bundle` (macOS) and `libsteam_api.so` (Linux x64) next to the Windows DLLs. | Check the plugin import settings: the bundle enabled for macOS (Intel + Apple Silicon), the .so for Linux x86_64 only, the Windows DLLs off for both. |
| Window frame | `WindowFrameService` uses user32 and is guarded by `#if UNITY_STANDALONE_WIN`. "Frameless Window" (DISPLAY-MODE-004) has no effect elsewhere. | Hide the Frameless option on macOS/Linux (or map it to borderless). |
| Rumble | `RumbleService` calls XInput (`xinput1_4.dll`); on macOS/Linux the call fails once and rumble switches itself off. | Move rumble to the Input System's `Gamepad.SetMotorSpeeds`, or to Steam Input; until then no rumble off Windows. |
| Controller buttons | About 90 references to legacy joystick buttons (`KeyCode.JoystickButtonN`) in InRunController (44), InputRemapService (18), MainMenuController (13), MainMenuBlueprintBuilder (13) and the Custom Puzzle book (2). The legacy button numbers differ on macOS and Linux (an Xbox pad's A, B, Start and the shoulders are other numbers there), so menus would react to the wrong buttons. | Biggest code task: read buttons through the Input System's `Gamepad` (same names on every OS), or enable Steam Input with a default layout so every pad looks like an Xbox pad. Needed for Steam Deck in any case. |
| Saves | `Application.persistentDataPath` = `LocalLow/Kratossos/sudoku_rouge_like` on Windows, `~/Library/Application Support/Kratossos/sudoku_rouge_like` on macOS, `~/.config/unity3d/Kratossos/sudoku_rouge_like` on Linux. Code is path-independent. | Steam Auto-Cloud root overrides so one cloud save follows the player between systems (below). PlayerPrefs (options cache) are per machine and need no sync. |
| Fonts, audio, art | TTF fonts, MP3/WAV audio and PNG art load the same everywhere. | Check text size at 1280×800 for the Deck (RM-102). |
| Python/PowerShell tools | Editor-side only, not in the build. | None. |

## Build and package

- **macOS:** Build Settings → macOS, Architecture **Intel 64-bit + Apple Silicon** (Universal). This ticks both store boxes. Output `sudoku_rouge_like.app`.
  Signing and notarization: an **Apple Developer account (99 USD a year)**, a Developer ID certificate, then `codesign` with the hardened runtime and `notarytool`. This can run on a Mac, or on Windows with the open-source `rcodesign`. Steam starts unsigned apps, but macOS warns or refuses on newer versions, so notarization is recommended.
- **Linux:** Build Settings → Linux, x86_64. Output `sudoku_rouge_like.x86_64` plus `_Data`. It runs in Steam's Linux runtime (Sniper container); select it under *Installation → Linux Runtime*.
- **Steam Deck:** no build of its own. The Windows build runs through Proton (section "Steam Deck" below); a native Linux build is optional. For "Verified", what matters is the Deck review checklist (controller, text size, no launcher, on-screen keyboard for the seed field), not the binary.

## Steam Deck (decided 2026-10-22)

**No extra build.** Without a Linux depot, Steam runs the Windows depot (4858471) on the Deck through Proton, and Valve's
compatibility review tests that. With Steam Input the Deck's controls reach the game as an Xbox (XInput) pad, so the
buttons, the D-pad and the sticks work as they do with an Xbox pad on Windows. The legacy button numbers in the code
(`KeyCode.JoystickButtonN`) are Windows numbers; Proton keeps them. A native Linux build would not: Unity sets those
numbers per OS, so a Linux or macOS build needs a button table per OS first (phase 2 and 3).

What the Deck still needs (owner steps in Steamworks, then a check on a Deck):

| Step | Where | Notes |
|---|---|---|
| Default controller layout | Steamworks → Application → Steam Input | "Gamepad" template for the Deck. Opting PlayStation, Switch and generic pads into Steam Input makes them Xbox pads on Windows too, which the legacy button numbers expect. |
| Text size | The game at 1280×800 | Valve's floor is 9 px (12 px recommended). The UI scales from 1920×1080 by about 0.70 at 1280×800, so text below size 13 falls under 9 px: a few labels at 10–12 (curse panel, shield badge, licence texts) and best-fit minimums of 10–12. Part of RM-102. |
| On-screen keyboard | Seed field (start run, Custom Puzzle) | For "Verified" the keyboard opens by itself when a text field is chosen with the controller (`SteamUtils.ShowFloatingGamepadTextInput`); until then Steam + X opens it. Part of RM-102. |
| Button names | Controller hints | The Deck's shoulders read L1/R1 where the game says LB/RB. Part of RM-102. |
| Compatibility review | Steamworks → Steam Deck Compatibility | Ask for it once the steps above are done; a password beta branch can be tested on a Deck before. |

## Lenovo Legion Go (checked 2026-10-25)

The Legion Go (and Legion Go 2) is a Windows handheld; the Legion Go S also ships with SteamOS. A Legion laptop is an ordinary Windows PC: any pad works as below.

| Case | What the game sees | Works? |
|---|---|---|
| Legion Go, Windows, both controllers attached | An Xbox (XInput) pad: Lenovo's guide says the console is in X-input mode with both controllers attached or both detached | Yes, like an Xbox pad on Windows |
| Legion Go, Windows, one controller detached | "Dual D-input" mode (Lenovo's guide): a generic pad with other button numbers | Yes since INPUT-PAD-002: Options → Controls → Controller "Automatic" picks D-input for a pad it does not know; "D-input" sets it by hand |
| Legion Go started from Steam with Steam Input on | An Xbox pad (Steam Input supports the Legion Go's controllers) | Yes |
| Legion Go S with SteamOS | The Windows build through Proton, like the Steam Deck | Yes, as on the Deck |
| Touch screen | Unity turns taps into mouse clicks | Yes: every screen can be played with the mouse |

**Found and fixed on the way (GAP-070):** in a run the game read the D-pad as buttons 12–15 and the triggers as axes the Input Manager does not define. On an Xbox pad under Windows (so on the Legion Go, the Deck and every pad Steam Input turns into one) the D-pad did nothing on the board, the path and the bag, and LT/RT never worked; only the stick moved the cursor. Both are read through the Input System now (INPUT-PAD-001). The menus were already fine (GAP-062).

**Screen:** the Legion Go has 2560×1600, the Legion Go 2 and Go S 1920×1200, all 16:10. The UI scales from 1920×1080 with match 0.5, so 16:10 gets a little more height; no layout was checked at 16:10 yet. Text size is less of a risk than on the Deck (8 to 8.8 inch screens at higher resolution).

**To check on the device:** the D-pad on the board after the fix, that the button names the game shows (A, B, X, Y, LB/RB) match the labels on the controllers, and the 16:10 layouts.

## Steamworks (owner steps; I prepare and stop before every Publish)

1. **Depots:** add a macOS depot and a Linux depot to App 4858470 (Windows stays depot 4858471), set the operating system on each.
2. **Launch options:** macOS `sudoku_rouge_like.app`, Linux `sudoku_rouge_like.x86_64`, each limited to its OS.
3. **Auto-Cloud root overrides** (the Windows root `WinAppDataLocalLow` + `Kratossos/sudoku_rouge_like/`):
   macOS → `MacAppSupport`, path `Kratossos/sudoku_rouge_like/`; Linux → `LinuxXdgConfigHome`, path `unity3d/Kratossos/sudoku_rouge_like/`; pattern `save_profile_*.json` as today.
4. **Store page:** tick macOS (64-bit Intel and Apple Silicon) and Linux + SteamOS; add system requirements per OS.
5. **Uploads:** one build with all three depots; test on a password beta branch first.

## Order and effort

| Phase | Work | Effort | Needs |
|---|---|---|---|
| 1 | Steam Deck: Steam Input default layout, text size, on-screen keyboard, button names (RM-102); Frameless option hidden off Windows | S | A Deck for the check |
| 2 | Native Linux build: a legacy button table per OS, rumble through the Input System, Linux depot, cloud override | S–M | A Linux PC or a Deck |
| 3 | macOS Universal build, signing and notarization, macOS depot | S–M | Apple Developer account; a Mac for a first test is strongly advised |
| 4 | Store page OS boxes, requirements, beta branch, release | S | Owner's publish OK |

Linux and the Deck come first: no fees, and the controller work they need is the same work macOS needs.

## Risks

- **Testing without the hardware:** a build that was never started on a Mac or a Deck should not be published. Borrowing one, or a cloud Mac (for example MacinCloud), is enough for a smoke test.
- **Controller mapping:** a native Linux or macOS build without the button table presses the wrong buttons. This is the main source of bad reviews for ports. The Deck through Proton is not affected.
- **Cloud saves across systems:** test that a save made on Windows loads on Linux and macOS (same JSON, same file names) before ticking the boxes.

## Decisions for the owner

1. Linux/Deck first, then macOS (recommended), or both at once?
2. Apple Developer account for notarization: yes or no?
3. ~~Steam Input default layout, or the Input System rewrite, for the controller work?~~ **Decided 2026-10-22: Steam Input**, with the Steam Deck next; the Deck runs the Windows build.
