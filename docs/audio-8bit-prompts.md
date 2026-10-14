# 8-bit Soundtrack — Prompts and Delivery Sheet

**Version:** 1.0 | **Date:** 2026-10-14 | **Status:** new

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-14 | new | Owner decision: replace all music and sound effects with an 8-bit (chiptune) set made with an AI tool. Track list, prompts, delivery format (AUDIO-MUSIC-001/002 in `docs/AudioVisualDirection.md`). |

Every sound in the game gets an 8-bit replacement: 22 music tracks and 35 sound effects. Each entry below has the file name the game loads, length, loop rule, tempo, key and a prompt to paste into an AI music or sound tool. Until a file arrives, the current sound keeps playing; nothing breaks if only part of the set is delivered.

## 1. How to use this sheet

1. Paste the **style block** (2.) in front of every music prompt, so all tracks sound like one soundtrack.
2. Generate 2–3 takes per track and keep the best. Instrumental only.
3. Export **WAV, 44.1 kHz, 16-bit**: stereo for music, mono for sound effects.
4. Name the file exactly as in the tables (for example `floor_00_bamboo_courtyard.wav`) and put it in `steam_assets/audio_8bit/` (music and SFX in one folder is fine). I move the files into `Assets/Resources/audio/`, cut the loop points, convert music to OGG, run the tests and the build.
5. Listen in the game (agents cannot hear): loop seam, level against the sound effects, boss switch.

AI tools rarely end a track exactly on a loop point. Generate a little longer than the target length; the loop is cut on a bar line using the BPM in the table (one 4/4 bar = 240 / BPM seconds).

## 2. Style block (put in front of every music prompt)

> 8-bit chiptune, NES/Famicom sound chip: two pulse-wave channels, one triangle bass, one noise channel for percussion, no reverb tail, no vocals, instrumental only. Japanese garden mood, pentatonic melodies (yo and in scales), calm and focused like a puzzle game, clean mix, seamless loop.

## 3. Delivery rules (AUDIO-MUSIC-002)

| | Music | Sound effects |
|---|---|---|
| Format | WAV 44.1 kHz 16-bit stereo | WAV 44.1 kHz 16-bit mono |
| Loudness | about −16 LUFS integrated, peaks ≤ −1 dBTP | peaks ≤ −3 dBFS, quieter than the music bed for repeated sounds |
| Loop | seamless: ends where it begins, cut on a bar line, no fade-out | one-shot, no silence before the sound, short tail |
| Boss tracks | same key and motif as their stage, faster and darker (they replace the stage track during the boss puzzle, AUDIO-MUSIC-003) | — |

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

Each prompt follows the style block.

**M1 `menu_loop`** — Main menu, 80 BPM, D yo scale, 90 s seamless loop. A calm title theme for a zen sudoku roguelike: soft 25 % pulse lead with a memorable 8-bar melody, slow triangle bass on root and fifth, gentle 12.5 % pulse arpeggio like wind chimes, very light noise hi-hat on beats 2 and 4. Welcoming, curious, unhurried.

**M2 `floor_00_bamboo_courtyard`** — Stage 1 "Bamboo Courtyard", 92 BPM, D yo scale, 90–120 s seamless loop. Morning in a bamboo garden: bright 25 % pulse melody, plucky staccato pulse counter-line imitating a bamboo flute, bouncing triangle bass, noise percussion like soft wooden clacks. Fresh, light, gently motivating for solving puzzles.

**M3 `boss_layer_00`** — Stage 1 boss, 112 BPM, D in scale, 60–90 s seamless loop. Boss version of the bamboo courtyard theme: same motif in a darker mode, driving eighth-note triangle bass, 50 % pulse lead with fast arpeggios, snappy noise drums, rising tension in the B section. Focused, urgent, not chaotic.

**M4 `floor_01_moss_garden`** — Stage 2 "Moss Garden", 84 BPM, A in scale, 90–120 s seamless loop. Quiet rain in a moss garden: soft 12.5 % pulse lead with echo-like repeated notes, slow triangle drone bass, noise channel as light rain hiss with an occasional low "temple bell" hit. Contemplative, damp, still.

**M5 `boss_layer_01`** — Stage 2 boss, 104 BPM, A in scale, 60–90 s seamless loop. Boss version of the moss garden theme: heavier triangle bass ostinato, tense pulse harmonies in minor seconds, rolling noise drums like a storm building. Ominous but steady.

**M6 `floor_02_koi_terrace`** — Stage 3 "Koi Terrace", 100 BPM, G major pentatonic, 90–120 s seamless loop. Sunlit water and koi: flowing 16th-note pulse arpeggios like ripples, a smooth 25 % pulse melody that glides, round triangle bass, light noise "splash" accents. Playful, flowing, bright.

**M7 `boss_layer_02`** — Stage 3 boss, 120 BPM, G minor pentatonic, 60–90 s seamless loop. Boss version of the koi terrace theme: same arpeggio idea turned minor and faster, punchy triangle bass, syncopated noise drums, call-and-response between the two pulse channels. Energetic, slippery, tense.

**M8 `floor_03_stone_lantern_walk`** — Stage 4 "Stone Lantern Walk", 88 BPM, E in scale, 90–120 s seamless loop. Twilight path lit by stone lanterns: warm 25 % pulse melody with slight vibrato, sustained triangle bass, slow pulse chords, sparse low noise hits like a distant taiko. Warm, quiet resolve, slightly mysterious.

**M9 `boss_layer_03`** — Stage 4 boss, 110 BPM, E in scale, 60–90 s seamless loop. Boss version of the lantern walk theme: marching triangle bass, sharp 50 % pulse stabs, noise taiko pattern growing louder each 8 bars, flickering arpeggios like fire. Determined, heated.

**M10 `floor_04_shrine_summit`** — Stage 5 "Shrine Summit", 108 BPM, C minor pentatonic resolving to C major halfway, 120 s seamless loop. Final climb to a mountain shrine in mist: full four-channel arrangement, heroic pulse melody, counter-melody in the second pulse, strong triangle bass, steady noise drums, a bell-like accent at the loop point. Majestic, hopeful, building.

**M11 `boss_layer_04`** — Final boss, 128 BPM, C minor pentatonic, 90 s seamless loop. Final boss theme based on the shrine summit motif: fast driving bass, dramatic pulse lead with octave jumps, intense noise drum fills, a short calm bar before the loop for breath. Epic, decisive, the hardest puzzle of the run.

**M12 `walk_loop`** — Daily Walk and Monthly Walk, 96 BPM, F yo scale, 90 s seamless loop. A daily stroll through the garden: friendly walking-pace triangle bass, cheerful 25 % pulse melody, light noise shuffle, a small "bell" motif every 8 bars. Relaxed routine, a little ritual each day.

**M13 `credits_loop`** — Credits, 76 BPM, D yo scale, 120 s seamless loop. Reprise of the main menu melody, slower and more tender: 12.5 % pulse lead, warm sustained triangle, gentle arpeggios, no drums for the first half, soft drums in the second half. Grateful, nostalgic, peaceful.

**M14 `run_victory`** — Full-run victory end screen, 100 BPM, C major pentatonic, 60 s seamless loop (plays after the short victory fanfare). Triumphant but calm celebration: bright pulse melody quoting the shrine summit theme in major, bouncing triangle bass, festive noise drums, sparkling high arpeggios. Earned, joyful, serene.

**M15 `run_defeat`** — Defeat end screen, 72 BPM, A in scale, 45 s seamless loop (plays after the game-over stinger). Gentle melancholy, not punishing: slow 12.5 % pulse melody, soft triangle bass, no drums, a hopeful turn in the last bars that invites another try.

**M16 `shop_loop`** — Shop, 104 BPM, G yo scale, 60 s seamless loop. A spirit merchant's stall: bouncy triangle bass, cheeky 25 % pulse melody with grace notes, coin-like high pulse blips, light noise shuffle. Playful, inviting.

**M17 `rest_loop`** — Rest, 70 BPM, D yo scale, 60 s seamless loop. A meditation stone in a quiet courtyard: very soft pulse melody, long triangle notes, no drums, occasional chime arpeggio. Restful, breathing.

**M18 `endless_zen_loop`** — Endless Zen, 66 BPM, D yo scale, 120 s seamless loop. Minimal focus music: sparse pulse notes with long rests, soft triangle drone, no drums, slowly changing harmony. Deep focus, flow state, unobtrusive.

**M19 `spirit_trials_loop`** — Spirit Trials (timed), 132 BPM, E in scale, 90 s seamless loop. Timed challenge: ticking noise percussion like a clock, urgent triangle bass, staccato pulse motif that climbs, tension that lifts every 16 bars. Fast, focused, competitive.

**M20 `tutorial_loop`** — Custom puzzles and Sudoku Basics, 88 BPM, C major pentatonic, 90 s seamless loop. Friendly learning music: simple clear pulse melody, steady triangle bass, soft noise beat. Encouraging, patient, light.

**M21 `puzzle_loop`** — Fallback puzzle loop, 92 BPM, D yo scale, 60 s seamless loop. A shorter variation of the Stage 1 theme (same motif and instrumentation).

## 5. Sound effects (all 35 replaced)

Prompt prefix for every sound effect:

> 8-bit retro game sound effect, NES-style square/triangle/noise synthesis, clean, short, no reverb, mono.

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

## 6. After delivery (Claude)

1. Copy each file over the old one with the same name (`Assets/Resources/audio/music|sfx|ui/`), keep the `.meta` files; new slots get new `.meta` files.
2. Music: cut loops on bar lines, convert to OGG Vorbis (quality about 6), Unity import Load Type "Streaming", Compression "Vorbis" (AUDIO-MUSIC-004).
3. Mark delivered optional slots as release-required in `AudioAssetService`, so a missing file fails the tests.
4. Tests (EditMode, PlayMode), build, short in-game check of the boss switch and the end screens.
5. You: listening check (loop seams, levels, boss switch, menu style option, volume sliders, mute). Closes GAP-030 and PRG-AUDIO.
6. You: check that the Steamworks content survey (AI-generated content disclosure) covers the music and sound effects.
