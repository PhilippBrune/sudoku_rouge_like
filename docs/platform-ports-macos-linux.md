# Plan: Run of the Nine on macOS and Linux / SteamOS

**Version:** 1.0 | **Date:** 2026-10-20 | **Status:** new (plan for the owner; no build settings changed yet)

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
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
- **Steam Deck:** the Linux build runs natively; the Windows build also runs through Proton today. For "Verified", what matters is the Deck review checklist (controller, text size, no launcher, on-screen keyboard for the seed field), not the binary.

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
| 1 | Controller input through the Input System (or Steam Input), rumble, Frameless option | M (1–2 weeks) | Tests in PlayMode; helps the Deck too |
| 2 | Linux build, Steam Deck test, Linux depot, cloud override | S | A Steam Deck or a Linux PC |
| 3 | macOS Universal build, signing and notarization, macOS depot | S–M | Apple Developer account; a Mac for a first test is strongly advised |
| 4 | Store page OS boxes, requirements, beta branch, release | S | Owner's publish OK |

Linux and the Deck come first: no fees, and the controller work they need is the same work macOS needs.

## Risks

- **Testing without the hardware:** a build that was never started on a Mac or a Deck should not be published. Borrowing one, or a cloud Mac (for example MacinCloud), is enough for a smoke test.
- **Controller mapping:** if phase 1 is skipped, pads on macOS and Linux press the wrong buttons. This is the main source of bad reviews for ports.
- **Cloud saves across systems:** test that a save made on Windows loads on Linux and macOS (same JSON, same file names) before ticking the boxes.

## Decisions for the owner

1. Linux/Deck first, then macOS (recommended), or both at once?
2. Apple Developer account for notarization: yes or no?
3. Steam Input default layout, or the Input System rewrite, for the controller work?
