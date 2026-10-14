# 8-bit Soundtrack — Prompts and Delivery Sheet

**Version:** 1.1 | **Date:** 2026-10-14 | **Status:** new

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-14 | new | Prompts rewritten to sound more 8-bit: every prompt names only the NES chip voices (pulse, triangle, noise) and no real instruments; short enough for Mureka. Generation through the Mureka API with `tools/audio/mureka_generate.py` (section 7). Sound effects: Mureka makes songs, not sub-second effects; section 5 says how they are made instead. |
| 1.0 | 2026-10-14 | new | Owner decision: replace all music and sound effects with an 8-bit (chiptune) set made with an AI tool. Track list, prompts, delivery format (AUDIO-MUSIC-001/002 in `docs/AudioVisualDirection.md`). |

Every sound in the game gets an 8-bit replacement: 21 music tracks and 35 sound effects. Each entry below has the file name the game loads, length, loop rule, tempo, key and a prompt to paste into an AI music or sound tool. Until a file arrives, the current sound keeps playing; nothing breaks if only part of the set is delivered.

## 1. How to use this sheet

1. Paste the **style block** (2.) in front of every music prompt, so all tracks sound like one soundtrack. The Mureka tool (7.) does this for you.
2. Generate 2–3 takes per track and keep the best. Instrumental only (in the Mureka app: **Instrumental**, not Song).
3. Export **WAV, 44.1 kHz, 16-bit**: stereo for music, mono for sound effects.
4. Name the file exactly as in the tables (for example `floor_00_bamboo_courtyard.wav`) and put it in `steam_assets/audio_8bit/` (music and SFX in one folder is fine). I move the files into `Assets/Resources/audio/`, cut the loop points, convert music to OGG, run the tests and the build.
5. Listen in the game (agents cannot hear): loop seam, level against the sound effects, boss switch.

AI tools rarely end a track exactly on a loop point. Generate a little longer than the target length; the loop is cut on a bar line using the BPM in the table (one 4/4 bar = 240 / BPM seconds).

## 2. Style block (put in front of every music prompt)

> 8-bit chiptune, NES Famicom 2A03 sound chip, retro video game music. Only four chip voices: two square pulse-wave channels (12.5 %, 25 %, 50 % duty), one triangle-wave bass, one white-noise channel for drums; chords as fast arpeggios. Dry and lo-fi, no reverb, no real instruments, no synth pads, no vocals. Japanese pentatonic melody, loopable.

Why it is worded like this: AI music models drift to modern production when a prompt mentions real instruments or places ("flute", "taiko", "bell", "rain"). The prompts below describe only what the four chip channels play; the garden theme comes through in the scales, tempo and mood words.

## 3. Delivery rules (AUDIO-MUSIC-002)

| | Music | Sound effects |
|---|---|---|
| Format | WAV 44.1 kHz 16-bit stereo | WAV 44.1 kHz 16-bit mono |
| Loudness | about −16 LUFS integrated, peaks ≤ −1 dBTP | peaks ≤ −3 dBFS, quieter than the music bed for repeated sounds |
| Loop | seamless: ends where it begins, cut on a bar line, no fade-out | one-shot, no silence before the sound, short tail |
| Boss tracks | same key and instrumentation as their stage, faster and darker (they replace the stage track during the boss puzzle, AUDIO-MUSIC-003). For the same melody, generate the stage track first and use the chosen take as the **reference track** for its boss track in the Mureka app; separate prompts cannot share a melody. | — |

## 4. Music

### 4.1 Track list

| # | File | Plays when | Length | Loop | BPM | Key / scale | Status in the game |
|---|---|---|---|---|---|---|---|
| M1 | `menu_loop` | Main menu | 90 s | yes | 80 | D yo (D E G A B) | replaces current |
| M2 | `floor_00_bamboo_courtyard` | Stage 1: path and puzzles | 90–120 s | yes | 92 | D yo | replaces current |
| M3 | `boss_layer_00` | Stage 1 boss puzzle | 60–90 s | yes | 112 | D in (D Eb G A Bb) | replaces current (now actually played, GAP-045) |
| M4 | `floor_01_moss_garden` | Stage 2 | 90–120 s | yes | 84 | A in (A Bb D E F) | replaces current |
| M5 | `boss_layer_01` | Stage 2 boss | 60–90 s | yes | 104 | A in | replaces current |
| M6 | `floor_02_koi_terrace` | Stage 3 | 90–120 s | yes | 100 | G major pentatonic | replaces current |
| M7 | `boss_layer_02` | Stage 3 boss | 60–90 s | yes | 120 | G minor pentatonic | replaces current |
| M8 | `floor_03_stone_lantern_walk` | Stage 4 | 90–120 s | yes | 88 | E in (E F A B C) | replaces current |
| M9 | `boss_layer_03` | Stage 4 boss | 60–90 s | yes | 110 | E in | replaces current |
| M10 | `floor_04_shrine_summit` | Stage 5 | 120 s | yes | 108 | C minor pentatonic → C major at the loop's midpoint | replaces current |
| M11 | `boss_layer_04` | Stage 5 boss (final boss) | 90 s | yes | 128 | C minor pentatonic | replaces current |
| M12 | `walk_loop` | Daily Walk and Monthly Walk puzzles | 90 s | yes | 96 | F yo | new slot |
| M13 | `credits_loop` | Credits screen | 120 s | yes | 76 | D yo, reprise of the menu melody | new slot |
| M14 | `run_victory` | End screen after a full-run victory (after the fanfare) | 60 s | yes | 100 | C major pentatonic | new slot |
| M15 | `run_defeat` | End screen after a defeat (after the stinger) | 45 s | yes | 72 | A in | new slot (suggested) |
| M16 | `shop_loop` | Shop node | 60 s | yes | 104 | G yo | replaces current (suggested) |
| M17 | `rest_loop` | Rest node | 60 s | yes | 70 | D yo | replaces current (suggested) |
| M18 | `endless_zen_loop` | Endless Zen | 120 s | yes | 66 | D yo, sparse | new slot (suggested) |
| M19 | `spirit_trials_loop` | Spirit Trials (timed) | 90 s | yes | 132 | E in | new slot (suggested) |
| M20 | `tutorial_loop` | Custom puzzles and Sudoku Basics | 90 s | yes | 88 | C major pentatonic | new slot (suggested) |
| M21 | `puzzle_loop` | Fallback before the stage is known | 60 s | yes | 92 | D yo | replaces current (may be a cut of M2) |

Your list (stages, bosses, walks, menu, credits, victory) is M1–M14. M15–M21 are what else the game plays music for today or could: shop, rest, the three other modes, the defeat screen and the fallback loop. The menu's "music style" option keeps working with the new menu, stage 1 and rest tracks.

### 4.2 Prompts

Each prompt follows the style block. The tool in section 7 reads these lines, so keep the format `**M<n> `<file>`** — <prompt>`.

**M1 `menu_loop`** — Title screen theme, 80 BPM, D yo pentatonic (D E G A B). A 25 % square lead plays a memorable 8-bar melody, a 12.5 % pulse plays slow sparkling high arpeggios, triangle bass on root and fifth, soft noise hi-hat on beats 2 and 4. Calm, curious, welcoming.

**M2 `floor_00_bamboo_courtyard`** — Level 1 theme, 92 BPM, D yo pentatonic. Bright 25 % square melody, staccato echo notes on the second pulse channel, bouncing eighth-note triangle bass, short clicky noise percussion. Fresh morning energy, light, gently motivating.

**M3 `boss_layer_00`** — Level 1 boss theme, 112 BPM, D in scale (D Eb G A Bb). Driving eighth-note triangle bass, 50 % square lead with fast arpeggios, snappy noise kick and snare, tension rising in the second half. Urgent and focused, not chaotic.

**M4 `floor_01_moss_garden`** — Level 2 theme, 84 BPM, A in scale (A Bb D E F). Soft 12.5 % pulse lead, a quieter delayed copy of it on the second pulse channel as an echo, slow triangle bass, a faint noise-channel hiss and a rare deep triangle note. Still, contemplative, damp.

**M5 `boss_layer_01`** — Level 2 boss theme, 104 BPM, A in scale. Heavy triangle bass ostinato, the two pulse channels clashing in minor seconds, rolling noise-channel drums building like a storm. Ominous but steady.

**M6 `floor_02_koi_terrace`** — Level 3 theme, 100 BPM, G major pentatonic. Rippling 16th-note pulse arpeggios, a smooth 25 % square melody with pitch slides, round triangle bass, light noise splashes on the off-beats. Playful, flowing, sunny.

**M7 `boss_layer_02`** — Level 3 boss theme, 120 BPM, G minor pentatonic. Fast minor pulse arpeggios, punchy triangle bass, syncopated noise drums, call and response between the two square channels. Energetic, slippery, tense.

**M8 `floor_03_stone_lantern_walk`** — Level 4 theme, 88 BPM, E in scale (E F A B C). Warm 25 % square melody with vibrato, sustained triangle bass, slow pulse arpeggios, sparse deep noise hits like distant drums. Twilight, quiet resolve, a little mysterious.

**M9 `boss_layer_03`** — Level 4 boss theme, 110 BPM, E in scale. Marching triangle bass, sharp 50 % square stabs, a noise drum pattern that grows denser every 8 bars, flickering fast arpeggios. Determined, heated.

**M10 `floor_04_shrine_summit`** — Level 5 theme, 108 BPM, C minor pentatonic turning to C major halfway. All four chip channels: heroic 25 % square lead, counter-melody on the second pulse, strong triangle bass, steady noise drums, a high triangle accent at the loop point. Majestic, hopeful, the final climb.

**M11 `boss_layer_04`** — Final boss theme, 128 BPM, C minor pentatonic. Fast driving triangle bass, dramatic square lead with octave jumps, intense noise drum fills, one calm bar before the loop. Epic, decisive, the hardest fight.

**M12 `walk_loop`** — Daily challenge theme, 96 BPM, F yo pentatonic. Walking-pace triangle bass, cheerful 25 % square melody, light noise shuffle, a short high pulse blip motif every 8 bars. Relaxed, friendly, a small daily ritual.

**M13 `credits_loop`** — Ending credits theme, 76 BPM, D yo pentatonic. Tender 12.5 % pulse lead, warm sustained triangle, gentle arpeggios; no drums in the first half, soft noise drums in the second. Grateful, nostalgic, peaceful.

**M14 `run_victory`** — Victory screen theme, 100 BPM, C major pentatonic. Bright square melody, bouncing triangle bass, festive noise drums, sparkling high arpeggios. Earned, joyful, calm.

**M15 `run_defeat`** — Game over screen theme, 72 BPM, A in scale. Slow 12.5 % pulse melody, soft triangle bass, no drums, a hopeful turn in the last bars that invites another try. Gentle melancholy, not punishing.

**M16 `shop_loop`** — Shop theme, 104 BPM, G yo pentatonic. Bouncy triangle bass, cheeky 25 % square melody with grace notes, coin-like high pulse blips, light noise shuffle. Playful, inviting.

**M17 `rest_loop`** — Rest area theme, 70 BPM, D yo pentatonic. Very soft pulse melody, long triangle notes, no drums, an occasional sparkling high pulse arpeggio. Restful, slow breathing.

**M18 `endless_zen_loop`** — Endless mode focus music, 66 BPM, D yo pentatonic. Sparse pulse notes with long rests, a soft triangle drone, no drums, harmony changing slowly. Deep focus, unobtrusive.

**M19 `spirit_trials_loop`** — Time attack theme, 132 BPM, E in scale. Ticking noise hi-hat like a clock, urgent triangle bass, a staccato square motif that climbs, tension lifting every 16 bars. Fast, focused, competitive.

**M20 `tutorial_loop`** — Tutorial theme, 88 BPM, C major pentatonic. Simple clear square melody, steady triangle bass, soft noise beat. Encouraging, patient, light.

**M21 `puzzle_loop`** — Short level theme, 92 BPM, D yo pentatonic. 25 % square melody, staccato second pulse, bouncing triangle bass, clicky noise percussion. Light and steady; may also be a cut of M2.

## 5. Sound effects (all 35 replaced)

**Mureka cannot make these.** It generates songs of a minute or more, not 0.1-second effects. Two ways that sound properly 8-bit:

- **Chip synthesis (recommended):** I generate all 35 in code from square, triangle and noise waves, like the NES did (deterministic, no cost), and you listen and ask for changes; or
- **sfxr / jsfxr** (free, https://sfxr.me): presets like "Pickup/Coin", "Hit/Hurt", "Powerup", tweak, export WAV.

If you use another AI sound tool, put this prefix in front of each prompt:

> 8-bit NES sound effect made only by a square, triangle or noise channel. One short sound, no music, no reverb, mono.

| File | Plays when | Length | Prompt |
|---|---|---|---|
| `cell_select` | A grid cell is selected | 0.05–0.1 s | tiny soft square-wave tick, high pitch, very quiet |
| `correct_placement` | A correct digit is placed | 0.2 s | bright two-note rising square blip (perfect fifth), satisfying |
| `wrong_placement` | A wrong digit is placed | 0.3 s | low buzzy square "nope", two falling notes, short |
| `pencil_toggle` | Pencil mode on/off | 0.1 s | light noise-scratch click, like a pencil tap |
| `pencil_depleted` | No pencil marks left | 0.3 s | short descending triangle bloop, empty feeling |
| `puzzle_solved` | A puzzle is complete | 1.0–1.5 s | cheerful ascending pentatonic arpeggio on square wave ending on a held bright note |
| `combo_05` | 5-placement combo | 0.4 s | quick three-note rising arpeggio, sparkly |
| `combo_10` | 10-placement combo | 0.6 s | faster five-note rising arpeggio with a final octave jump |
| `combo_break` | Combo is lost | 0.3 s | short descending pitch-bend square, deflating |
| `hp_loss` | HP lost | 0.3 s | low triangle thump with a short noise burst, damage hit |
| `hp_critical` | HP is critically low | 0.5 s | two-tone low square warning beep, heartbeat-like |
| `gold_gain` | Gold gained | 0.2 s | classic coin pickup: two quick high square notes |
| `item_use` | An item is used | 0.4 s | magical square-wave sweep upward with a sparkle |
| `relic_pickup` | A relic is collected | 0.8 s | shimmering arpeggio with a bell-like triangle note at the end |
| `reward_claim` | A reward is claimed | 0.5 s | bright chord stab followed by a short rising run |
| `shop_purchase` | Something is bought | 0.4 s | cash-register-style double coin blip |
| `shop_reroll` | Shop reroll | 0.4 s | quick spinning pitch wobble, square wave |
| `rest_heal` | Healing at a rest node | 0.8 s | soft rising triangle glissando with gentle sparkle |
| `path_advance` | Moving to the next node | 0.2 s | short footstep-like noise tick followed by a soft blip |
| `fog_reveal` | Fog lifts from a cell | 0.3 s | airy noise whoosh with a rising filter |
| `modifier_activate` | A boss modifier appears | 0.5 s | ominous low square swell with a dissonant interval |
| `boss_gate_open` | The boss gate opens | 1.0–1.5 s | heavy gate: low noise rumble, then a rising minor arpeggio |
| `floor_transition` | Next stage begins | 1.5 s | wind-like noise sweep, then an ascending three-note chime |
| `level_up` | A class levels up | 1.0 s | classic level-up fanfare: quick rising arpeggio plus held major chord |
| `achievement` | An achievement unlocks | 1.0 s | bright two-chord jingle with sparkling arpeggio |
| `xp_bar_fill` | XP bar fills after a run | 1.5 s | rising pitch sweep of quick square ticks, accelerating |
| `victory` | Short victory confirmation | 1.5 s | short triumphant four-note jingle in major |
| `run_victory_fanfare` | Full-run victory (before `run_victory`) | 4 s | full chiptune fanfare: rising melody, drum roll on noise, final major chord |
| `game_over` | Run lost (before `run_defeat`) | 1.5–2 s | slow descending minor melody, soft final low note |
| `button_hover` | UI hover | 0.03–0.05 s | extremely short quiet square tick |
| `button_click` | UI click | 0.08 s | crisp square click, mid pitch |
| `menu_open` | A menu panel opens | 0.2 s | short rising two-note blip |
| `menu_close` | A menu panel closes | 0.2 s | short falling two-note blip |
| `item_reward` | An item reward is revealed | 0.6 s | small treasure jingle, three rising notes and a sparkle |
| `relic_reveal` | A relic is revealed | 0.8 s | mysterious shimmering arpeggio, ends on a bell-like note |

Optional new jingles (not used by the game yet; only if you want them): boss defeated, floor cleared, prestige, daily streak.

## 7. Generating with Mureka (API)

The Mureka desktop app has no interface another program can use, but Mureka offers an API with the same models: `tools/audio/mureka_generate.py` sends the prompts above (style block + track prompt) and downloads every take. Facts used: base URL `https://api.mureka.ai`, `POST /v1/instrumental/generate` with `model` "auto" and `prompt`, then `GET /v1/instrumental/query/{task_id}` until the status is `succeeded`; the takes are the `choices` (sources: platform.mureka.ai docs, Mureka's own MCP server `mureka-mcp` 0.0.13).

**You (once):**

1. Sign in at https://platform.mureka.ai, add credit (the API is billed there; check whether your app subscription includes API credit), open **API Keys** and create a key.
2. Store it on your PC, not in chat. In PowerShell (it asks for the key and does not show it):

   ```powershell
   $k = Read-Host "Mureka API key" -AsSecureString
   New-Item -ItemType Directory -Force "$env:USERPROFILE\.mureka" | Out-Null
   "MUREKA_API_KEY=$([Net.NetworkCredential]::new('', $k).Password)" | Set-Content -Encoding ascii "$env:USERPROFILE\.mureka\.env"
   ```

3. Tell me "Mureka key is in".

**Me (or you):**

```powershell
python tools/audio/mureka_generate.py list                 # tracks, prompt lengths, takes already downloaded
python tools/audio/mureka_generate.py show M3               # the full prompt, to paste into the app as well
python tools/audio/mureka_generate.py credit                # account balance
python tools/audio/mureka_generate.py generate M1 M2        # dry run: what would be sent
python tools/audio/mureka_generate.py generate M1 M2 --yes  # send, wait, download
python tools/audio/mureka_generate.py generate --all --yes  # every track without takes yet
python tools/audio/mureka_generate.py resume                # finish tasks an interrupted run left open
```

Takes land in `steam_assets/audio_8bit/mureka/<file>/` (`<file>_take1.mp3`, …, plus the task JSON); the folder is not committed. Every request costs credit, so `generate` only sends with `--yes`, skips tracks that already have takes (`--again` to regenerate) and never prints the key. You pick the takes; I then do section 6.

## 6. After delivery (Claude)

1. Copy each file over the old one with the same name (`Assets/Resources/audio/music|sfx|ui/`), keep the `.meta` files; new slots get new `.meta` files.
2. Music: cut loops on bar lines, convert to OGG Vorbis (quality about 6), Unity import Load Type "Streaming", Compression "Vorbis" (AUDIO-MUSIC-004).
3. Mark delivered optional slots as release-required in `AudioAssetService`, so a missing file fails the tests.
4. Tests (EditMode, PlayMode), build, short in-game check of the boss switch and the end screens.
5. You: listening check (loop seams, levels, boss switch, menu style option, volume sliders, mute). Closes GAP-030 and PRG-AUDIO.
6. You: check that the Steamworks content survey (AI-generated content disclosure) covers the music and sound effects.
