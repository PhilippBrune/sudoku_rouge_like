# Trailer 1.1

Steam store trailer for release 1.1.0, made on 2026-10-13. It replaces `Trailer.mp4` (1.0, June 2026) on the store page; the old file is kept for reference.

## Files

| File | Content |
| --- | --- |
| `Trailer_1.1.mp4` | The trailer: 70 s, 1920×1080, 30 fps, H.264 High (yuv420p, fast start), AAC stereo 48 kHz 320 kbps, −14 LUFS integrated, −1.4 dBFS peak. |
| `Trailer_1.1_thumbnail.jpg` | 1920×1080 frame from the end card, for the Steamworks thumbnail field. |

## Content

All footage is captured from the 1.1 Windows build at 1920×1080. It covers one seeded Garden Run on a staged profile (all classes and modes unlocked) and three Custom Puzzle boards. Solving is sped up 2.4–5.6× in places. There is no mouse cursor or debug overlay, and no footage is mocked up.

| Time | Shot | Caption |
| --- | --- | --- |
| 0:00 | Logo card over the blurred garden | — |
| 0:03 | Floor 1 path map | Choose your path through the garden |
| 0:07 | 6×6 puzzle solved | Solve to advance. A wrong digit costs HP. |
| 0:14 | Reward screen | Earn gold, XP and items |
| 0:16 | Shop | Spend it in the shop |
| 0:20 | Cursed tile | Accept a curse for a bigger reward |
| 0:23 | Cursed puzzle with Renban lines and the legend | Modifiers bend the rules |
| 0:29 | Relic node | Collect relics that change your run |
| 0:32 | Boss gate, modifier pick | At the boss gate, you choose the rules |
| 0:37 | Boss puzzle (XV Pairs), "Boss Defeated!" | — |
| 0:45 | Moss Garden, Floor 2 | Five gardens. One run. |
| 0:49 | Killer/Whispers/Kropki, Thermo/Arrows/Even-Odd, Renban/XV/Fortress boards | 30 Sudoku modifiers |
| 0:53 | 7×7 board solved | Boards from 5×5 to 9×9 |
| 0:58 | Class select | 8 classes, each with its own passive |
| 1:02 | Game Modes | Endless Zen · Spirit Trials · Daily and Monthly Walks |
| 1:04 | Logo card over the library hero art | ON STEAM |

The captions are in English only. The end-card line "ON STEAM" is a single constant (`END_CTA`) in the edit script and can be changed, for example to "Wishlist now".

## Provenance

- **Video:** gameplay captures of the game; logo and end-card art from `steam_assets/library/` (project-owned).
- **Music:** an original score synthesized for this trailer (numpy synthesis: Hirajoshi scale, plucks, pads, taiko); no samples or third-party music.
- **Sound effects:** the game's own `Assets/Resources/audio/sfx/` files (project-owned).
- **Font:** Roboto (Apache License 2.0), as bundled with the game (`docs/legal/third-party/Roboto-NOTICE.md`).

## Upload

In Steamworks, go to Store Page Admin → Trailers. Upload `Trailer_1.1.mp4`, set the thumbnail, and mark it as the first trailer. Retire or unlist `Trailer.mp4`, which shows the 1.0 boss gate, path map and digit styles.
