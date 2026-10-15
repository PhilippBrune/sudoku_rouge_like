"""Composes and renders the 21 music loops of docs/audio-8bit-prompts.md (M1..M21) with 2A03 voices.

Each track is a list of 8-bar sections. A section's melody comes from a seeded theme in pentatonic scale-index
space, so a boss track can play its stage's melody in the darker scale (same contour, AUDIO-MUSIC-002) and the
credits can reprise the menu theme. Channels: p1 lead, p2 echo/arpeggio/counter-line, triangle bass, kick
(triangle sweeps) and noise drums. Loops are exact bar lengths; tails wrap to the start, so the seam is seamless.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from functools import lru_cache

import numpy as np

from chip_synth import SR, LoopMix, decay_env, envelope, master, midi_hz, noise, pulse, triangle

SCALES = {'yo': (0, 2, 5, 7, 9), 'in': (0, 1, 5, 7, 8), 'maj': (0, 2, 4, 7, 9), 'min': (0, 3, 5, 7, 10)}
GAINS = {'p1': 0.50, 'p2': 0.40, 'tri': 0.55, 'kick': 0.55, 'noise': 0.30}
PANS = {'p1': -0.2, 'p2': 0.25, 'tri': 0.0, 'kick': 0.0, 'noise': 0.1}

# One-bar rhythms on a 16-step grid: (start, length).
RHYTHMS = {
    'sparse': [[(0, 12)], [(0, 6), (8, 8)], [(4, 12)], [(0, 16)], [(0, 4), (8, 8)]],
    'calm': [[(0, 8), (8, 8)], [(0, 6), (6, 2), (8, 8)], [(0, 4), (4, 4), (8, 8)], [(0, 12), (12, 4)],
             [(0, 4), (4, 4), (8, 4), (12, 4)]],
    'normal': [[(0, 4), (4, 2), (6, 2), (8, 4), (12, 4)], [(0, 2), (2, 2), (4, 4), (8, 2), (10, 2), (12, 4)],
               [(0, 6), (6, 2), (8, 4), (12, 2), (14, 2)], [(0, 4), (4, 4), (8, 2), (10, 2), (12, 4)],
               [(0, 3), (3, 3), (6, 2), (8, 8)]],
    'busy': [[(i * 2, 2) for i in range(8)], [(0, 2), (2, 1), (3, 1), (4, 2), (6, 2), (8, 2), (10, 1), (11, 1), (12, 4)],
             [(0, 1), (1, 1), (2, 2), (4, 2), (6, 2), (8, 4), (12, 2), (14, 2)], [(0, 2), (2, 2), (4, 1), (5, 1), (6, 2),
                                                                                (8, 2), (10, 2), (12, 2), (14, 2)]],
    'cadence': [[(0, 4), (4, 12)], [(0, 8), (8, 8)], [(0, 16)], [(0, 2), (2, 2), (4, 12)]],
}
RHYTHMS['climb'] = RHYTHMS['busy']
STEPS = {'default': ([-2, -1, 0, 1, 2, 3, -3], [0.18, 0.28, 0.04, 0.28, 0.14, 0.04, 0.04]),
         'climb': ([-2, -1, 0, 1, 2, 3, -3], [0.08, 0.14, 0.04, 0.42, 0.24, 0.06, 0.02])}

PROG = {
    'calm': (0, 2, 3, 0, 4, 2, 3, 0), 'bright': (0, 3, 2, 3, 0, 3, 4, 0), 'b': (2, 2, 4, 3, 2, 4, 1, 3),
    'dark': (0, 0, 4, 3, 0, 2, 4, 0), 'dark_b': (4, 4, 2, 3, 4, 2, 1, 3), 'slow': (0, 0, 2, 2, 3, 3, 0, 0),
}


@dataclass(frozen=True)
class Note:
    step: int    # 16th steps from the section start
    length: int
    idx: int     # scale index: octave * 5 + degree, relative to the track's root


@dataclass(frozen=True)
class Theme:
    seed: int
    density: str
    prog_a: tuple
    prog_b: tuple
    lo: int = 3
    hi: int = 11


@dataclass(frozen=True)
class Section:
    kind: str = 'A'           # 'A' = the theme's main melody, 'B' = the track's own contrast melody
    p2: str = 'none'          # second pulse: echo, arp16, arp8, slow, sparkle, counter, harmony, stabs, call
    bass: str = 'half'        # half, eighths, walk, drone, ostinato, bounce, march, quarters
    drums: str = 'none'       # see DRUMS
    fill: bool = False
    scale: str | None = None  # override (e.g. the summit's turn to major)
    shift: int = 0            # transposition in scale steps
    extras: tuple = ()        # bell, coins, hiss, deep


@dataclass(frozen=True)
class Track:
    id: str
    file: str
    bpm: int
    root: int                 # MIDI note of the tonic; scale index 0
    scale: str
    theme: str
    sections: tuple
    duty: float = 0.25
    legato: float = 0.9
    b: tuple = (0, 'normal')  # seed and density of the B melody
    lead: tuple = ()          # options: vibrato, slide, grace, octave_jump, soft
    calm_tail: bool = False


THEMES = {
    'menu': Theme(101, 'calm', PROG['calm'], PROG['b']),
    'bamboo': Theme(202, 'normal', PROG['bright'], PROG['b']),
    'moss': Theme(303, 'calm', PROG['dark'], PROG['dark_b'], lo=4, hi=11),
    'koi': Theme(404, 'normal', PROG['bright'], PROG['b'], lo=5, hi=13),
    'lantern': Theme(505, 'calm', PROG['dark'], PROG['dark_b']),
    'summit': Theme(606, 'normal', PROG['bright'], PROG['b']),
    'walk': Theme(707, 'normal', PROG['bright'], PROG['b']),
    'shop': Theme(808, 'normal', PROG['bright'], PROG['b'], lo=5, hi=13),
    'rest': Theme(909, 'calm', PROG['slow'], PROG['slow']),
    'zen': Theme(1010, 'sparse', PROG['slow'], PROG['slow']),
    'trials': Theme(1111, 'climb', PROG['dark'], PROG['dark_b']),
    'tutorial': Theme(1212, 'calm', PROG['calm'], PROG['b']),
    'defeat': Theme(1313, 'calm', PROG['dark'], PROG['calm'], lo=2, hi=9),
}

S = Section
TRACKS = (
    Track('M1', 'menu_loop', 80, 62, 'yo', 'menu', (
        S('A', 'none', 'half', 'soft'), S('A', 'sparkle', 'half', 'soft'), S('B', 'slow', 'half', 'soft'),
        S('A', 'sparkle', 'half', 'soft')), b=(111, 'calm')),
    Track('M2', 'floor_00_bamboo_courtyard', 92, 62, 'yo', 'bamboo', (
        S('A', 'echo', 'bounce', 'clicks'), S('A', 'echo', 'bounce', 'clicks'), S('B', 'arp8', 'bounce', 'clicks'),
        S('A', 'echo', 'bounce', 'clicks', fill=True), S('A', 'arp8', 'bounce', 'clicks')),
        legato=0.6, b=(212, 'normal')),
    Track('M3', 'boss_layer_00', 112, 62, 'in', 'bamboo', (
        S('A', 'arp16', 'eighths', 'drive'), S('B', 'arp16', 'eighths', 'drive', fill=True),
        S('A', 'harmony', 'eighths', 'drive'), S('B', 'stabs', 'ostinato', 'drive', fill=True)),
        duty=0.5, legato=0.8, b=(2021, 'busy')),
    Track('M4', 'floor_01_moss_garden', 84, 57, 'in', 'moss', (
        S('A', 'echo', 'drone', extras=('hiss',)), S('A', 'echo', 'drone', extras=('hiss', 'deep')),
        S('B', 'slow', 'drone', extras=('hiss',)), S('A', 'echo', 'half', extras=('hiss', 'deep')),
        S('A', 'echo', 'drone', extras=('hiss',))), duty=0.125, legato=0.95, b=(313, 'calm')),
    Track('M5', 'boss_layer_01', 104, 57, 'in', 'moss', (
        S('A', 'harmony', 'ostinato', 'backbeat'), S('A', 'harmony', 'ostinato', 'backbeat', fill=True),
        S('B', 'stabs', 'ostinato', 'drive', fill=True), S('A', 'harmony', 'ostinato', 'backbeat', fill=True)),
        b=(3031, 'busy')),
    Track('M6', 'floor_02_koi_terrace', 100, 55, 'maj', 'koi', (
        S('A', 'arp16', 'bounce', 'offbeat'), S('A', 'arp16', 'bounce', 'offbeat'), S('B', 'arp8', 'walk', 'offbeat'),
        S('A', 'arp16', 'bounce', 'offbeat'), S('B', 'counter', 'walk', 'offbeat')),
        b=(414, 'normal'), lead=('slide',)),
    Track('M7', 'boss_layer_02', 120, 55, 'min', 'koi', (
        S('A', 'call', 'bounce', 'drive'), S('A', 'arp16', 'bounce', 'drive', fill=True),
        S('B', 'call', 'eighths', 'drive'), S('B', 'arp16', 'eighths', 'drive', fill=True),
        S('A', 'call', 'bounce', 'drive', fill=True)), b=(4041, 'busy')),
    Track('M8', 'floor_03_stone_lantern_walk', 88, 64, 'in', 'lantern', (
        S('A', 'slow', 'drone', 'taiko_sparse'), S('A', 'slow', 'drone', 'taiko_sparse'),
        S('B', 'counter', 'half', 'taiko_sparse'), S('A', 'slow', 'drone', 'taiko_sparse'),
        S('A', 'arp8', 'drone', 'taiko_sparse')), b=(515, 'calm'), lead=('vibrato',)),
    Track('M9', 'boss_layer_03', 110, 64, 'in', 'lantern', (
        S('A', 'stabs', 'march', 'taiko'), S('A', 'stabs', 'march', 'taiko_dense'),
        S('B', 'arp16', 'march', 'taiko_dense'), S('B', 'arp16', 'eighths', 'drive', fill=True)),
        b=(5051, 'busy')),
    Track('M10', 'floor_04_shrine_summit', 108, 60, 'min', 'summit', (
        S('A', 'counter', 'half', 'backbeat'), S('B', 'arp8', 'walk', 'backbeat', fill=True),
        S('A', 'counter', 'bounce', 'backbeat'), S('A', 'counter', 'half', 'backbeat', scale='maj'),
        S('B', 'arp16', 'walk', 'backbeat', fill=True, scale='maj'),
        S('A', 'counter', 'bounce', 'backbeat', scale='maj', extras=('bell',))), b=(616, 'normal')),
    Track('M11', 'boss_layer_04', 128, 60, 'min', 'summit', (
        S('A', 'arp16', 'eighths', 'drive'), S('A', 'harmony', 'eighths', 'drive', fill=True),
        S('B', 'stabs', 'ostinato', 'drive'), S('B', 'arp16', 'ostinato', 'drive', fill=True),
        S('A', 'arp16', 'eighths', 'drive'), S('A', 'counter', 'eighths', 'drive')),
        duty=0.5, b=(6061, 'busy'), lead=('octave_jump',), calm_tail=True),
    Track('M12', 'walk_loop', 96, 65, 'yo', 'walk', (
        S('A', 'none', 'walk', 'shuffle', extras=('bell',)), S('A', 'echo', 'walk', 'shuffle', extras=('bell',)),
        S('B', 'slow', 'walk', 'shuffle'), S('A', 'echo', 'walk', 'shuffle', extras=('bell',)),
        S('B', 'arp8', 'walk', 'shuffle')), b=(717, 'normal')),
    Track('M13', 'credits_loop', 76, 62, 'yo', 'menu', (
        S('A', 'slow', 'drone'), S('A', 'sparkle', 'half'), S('B', 'slow', 'half', 'soft'),
        S('A', 'sparkle', 'half', 'soft'), S('A', 'counter', 'half', 'soft')),
        duty=0.125, b=(111, 'calm'), lead=('vibrato',)),
    Track('M14', 'run_victory', 100, 60, 'maj', 'summit', (
        S('A', 'arp16', 'bounce', 'backbeat'), S('B', 'arp16', 'bounce', 'backbeat', fill=True, extras=('bell',)),
        S('A', 'counter', 'bounce', 'backbeat', fill=True)), b=(1414, 'normal')),
    Track('M15', 'run_defeat', 72, 57, 'in', 'defeat', (
        S('A', 'none', 'half'), S('A', 'slow', 'half', scale='yo')), duty=0.125, b=(1515, 'calm'), lead=('soft',)),
    Track('M16', 'shop_loop', 104, 55, 'yo', 'shop', (
        S('A', 'none', 'bounce', 'shuffle', extras=('coins',)), S('B', 'arp8', 'bounce', 'shuffle', extras=('coins',)),
        S('A', 'echo', 'bounce', 'shuffle', extras=('coins',))), legato=0.7, b=(818, 'normal'), lead=('grace',)),
    Track('M17', 'rest_loop', 70, 62, 'yo', 'rest', (
        S('A', 'none', 'drone'), S('A', 'sparkle', 'drone')), duty=0.125, b=(919, 'calm'), lead=('soft',)),
    Track('M18', 'endless_zen_loop', 66, 62, 'yo', 'zen', (
        S('A', 'none', 'drone'), S('A', 'slow', 'drone'), S('B', 'none', 'drone'), S('A', 'sparkle', 'drone')),
        duty=0.125, b=(1818, 'sparse'), lead=('soft',)),
    Track('M19', 'spirit_trials_loop', 132, 64, 'in', 'trials', (
        S('A', 'stabs', 'eighths', 'clock'), S('A', 'arp16', 'eighths', 'clock', fill=True),
        S('A', 'stabs', 'eighths', 'clock', shift=1), S('A', 'arp16', 'eighths', 'clock', fill=True, shift=1),
        S('B', 'arp16', 'ostinato', 'clock', shift=2), S('B', 'stabs', 'ostinato', 'clock', fill=True, shift=2)),
        legato=0.55, b=(1919, 'climb')),
    Track('M20', 'tutorial_loop', 88, 60, 'maj', 'tutorial', (
        S('A', 'none', 'quarters', 'soft'), S('A', 'slow', 'quarters', 'soft'), S('B', 'slow', 'half', 'soft'),
        S('A', 'none', 'quarters', 'soft')), b=(2020, 'calm')),
    Track('M21', 'puzzle_loop', 92, 62, 'yo', 'bamboo', (
        S('A', 'arp8', 'half', 'clicks'), S('B', 'echo', 'bounce', 'clicks'), S('A', 'echo', 'bounce', 'clicks')),
        legato=0.6, b=(2121, 'normal')),
)


# --- melody ------------------------------------------------------------------------------------------------------------

def _chord_tones(c: int, lo: int, hi: int) -> list[int]:
    return sorted({c + d + 5 * k for k in range(-4, 5) for d in (0, 2, 4)} & set(range(lo, hi + 1)))


def _reflect(v: int, lo: int, hi: int) -> int:
    while v < lo or v > hi:
        v = 2 * lo - v if v < lo else 2 * hi - v
    return v


def _phrase(rng, bars, chords, density, lo, hi, start, cadence=None):
    notes, cur = [], start
    moves, weights = STEPS['climb' if density == 'climb' else 'default']
    for b in range(bars):
        last = cadence is not None and b == bars - 1
        pool = RHYTHMS['cadence' if last else density]
        rhythm = pool[rng.integers(len(pool))]
        tones = _chord_tones(chords[b], lo, hi)
        for k, (s, length) in enumerate(rhythm):
            if k == 0 or s % 8 == 0:
                cur = min(tones, key=lambda t: (abs(t - cur), rng.random()))
            else:
                cur = _reflect(cur + int(rng.choice(moves, p=weights)), lo, hi)
            notes.append(Note(b * 16 + s, length, cur))
    if cadence is not None and notes:
        end = notes[-1]
        target = min((i for i in range(lo, hi + 1) if i % 5 == cadence % 5), key=lambda i: abs(i - end.idx))
        notes[-1] = Note(end.step, end.length, target)
    return notes


def _shift(notes, steps):
    return [Note(n.step + steps, n.length, n.idx) for n in notes]


@lru_cache(maxsize=None)
def melody(seed: int, density: str, prog: tuple, lo: int, hi: int) -> tuple:
    """8 bars: motif, motif varied, contrast (higher), motif closing on the tonic chord."""
    rng = np.random.default_rng(seed)
    mid = (lo + hi) // 2
    motif = _phrase(rng, 2, prog[0:2], density, lo, hi, mid)
    first_bar = [n for n in motif if n.step < 16]
    answer = _shift(_phrase(rng, 1, prog[3:4], density, lo, hi, motif[-1].idx, cadence=prog[3]), 48)
    contrast = _shift(_phrase(rng, 2, prog[4:6], density, min(lo + 2, hi - 3), hi, hi - 2), 64)
    close = _shift(_phrase(rng, 1, prog[7:8], density, lo, hi, first_bar[-1].idx, cadence=prog[7]), 112)
    return tuple(motif + _shift(first_bar, 32) + answer + contrast + _shift(first_bar, 96) + close)


# --- instruments -------------------------------------------------------------------------------------------------------

def _midi(track: Track, scale: str, idx: int) -> int:
    s = SCALES[scale]
    return track.root + 12 * (idx // 5) + s[idx % 5]


def _bass_midi(track: Track, scale: str, idx: int) -> int:
    m = _midi(track, scale, idx)
    while m > 50:
        m -= 12
    while m < 33:
        m += 12
    return m


def _pulse_note(freq_hz, seconds, duty, vel, sustain=0.6, vibrato=False, slide_from=None):
    n_gate = max(int(seconds * SR), 1)
    env = envelope(n_gate, 0.003, 0.1, sustain, 0.03)
    t = np.arange(len(env)) / SR
    freq = np.full(len(env), float(freq_hz))
    if slide_from is not None:
        glide = np.clip(t / 0.05, 0.0, 1.0)
        freq = slide_from * (freq_hz / slide_from) ** glide
    if vibrato and seconds > 0.3:
        depth = np.clip((t - 0.18) / 0.1, 0.0, 1.0) * 15.0
        freq = freq * 2.0 ** (depth / 1200.0 * np.sin(2 * np.pi * 5.5 * t))
    return pulse(freq, len(env), duty) * env * vel


def _tri_note(freq_hz, seconds):
    n_gate = max(int(seconds * SR), 1)
    env = envelope(n_gate, 0.002, 1.0, 1.0, 0.012, steps=False)
    return triangle(np.full(len(env), float(freq_hz)), len(env)) * env


def _drum(name: str):
    """(channel, samples) for one drum hit."""
    if name == 'kick':
        n = int(0.09 * SR)
        f = 140 * (48 / 140) ** (np.arange(n) / n)
        return 'kick', triangle(f, n) * decay_env(n, 0.05, steps=False)
    if name == 'taiko':
        n = int(0.24 * SR)
        f = 100 * (38 / 100) ** (np.arange(n) / n)
        return 'kick', triangle(f, n) * decay_env(n, 0.12, steps=False)
    if name == 'snare':
        n = int(0.14 * SR)
        return 'noise', noise(9000, n) * decay_env(n, 0.05)
    if name == 'hat':
        n = int(0.04 * SR)
        return 'noise', noise(30000, n) * decay_env(n, 0.012) * 0.7
    if name == 'click':
        n = int(0.02 * SR)
        return 'noise', (noise(6000, n, short=True) * 0.6 + pulse(1800.0, n, 0.125) * 0.4) * decay_env(n, 0.006)
    if name == 'thud':
        n = int(0.16 * SR)
        return 'noise', noise(1200, n) * decay_env(n, 0.06)
    raise KeyError(name)


DRUMS = {
    'none': {},
    'soft': {'hat': ([4, 12], 0.35)},
    'clicks': {'click': ([0, 6, 10], 0.55), 'hat': ([4, 12], 0.25)},
    'backbeat': {'kick': ([0, 8], 0.9), 'snare': ([4, 12], 0.7), 'hat': ([2, 6, 10, 14], 0.3)},
    'drive': {'kick': ([0, 3, 8, 11], 0.9), 'snare': ([4, 12], 0.75), 'hat': (list(range(0, 16, 2)), 0.28)},
    'shuffle': {'kick': ([0], 0.7), 'snare': ([8], 0.4), 'hat': ([0, 3, 4, 7, 8, 11, 12, 15], 0.25)},
    'offbeat': {'kick': ([0], 0.6), 'hat': ([2, 6, 10, 14], 0.4)},
    'clock': {'kick': ([0, 8], 0.8), 'hat': (list(range(16)), 0.22), 'snare': ([12], 0.4)},
    'taiko_sparse': {'taiko': ([0], 0.8), 'thud': ([10], 0.25)},
    'taiko': {'taiko': ([0, 3, 8], 0.85), 'hat': ([12], 0.3)},
    'taiko_dense': {'taiko': ([0, 3, 6, 8, 11, 14], 0.85), 'snare': ([12], 0.5), 'hat': ([2, 10], 0.3)},
}


# --- rendering ---------------------------------------------------------------------------------------------------------

def loop_seconds(track: Track) -> float:
    return len(track.sections) * 8 * 4 * 60.0 / track.bpm


def section_notes(track: Track, sec: Section) -> list:
    theme = THEMES[track.theme]
    if sec.kind == 'A':
        notes = melody(theme.seed, theme.density, theme.prog_a, theme.lo, theme.hi)
    else:
        notes = melody(track.b[0], track.b[1], theme.prog_b, theme.lo, theme.hi)
    return [Note(n.step, n.length, n.idx + sec.shift) for n in notes]


def render(track: Track, log_notes: list | None = None) -> np.ndarray:
    """Stereo float loop (n, 2), mastered to about -16 LUFS with peaks at most -1 dBFS."""
    step = 60.0 / track.bpm / 4.0
    mix = LoopMix(loop_seconds(track))
    theme = THEMES[track.theme]
    total_bars = len(track.sections) * 8
    sustain = 0.45 if 'soft' in track.lead else 0.6
    lead_vel = 0.75 if 'soft' in track.lead else 1.0

    def note_out(ch, t0, seconds, midi, x):
        mix.add(ch, t0, x)
        if log_notes is not None:
            log_notes.append((ch, t0, seconds, midi))

    for si, sec in enumerate(track.sections):
        base_bar = si * 8
        scale = sec.scale or track.scale
        prog = theme.prog_a if sec.kind == 'A' else theme.prog_b
        chords = [c + sec.shift for c in prog]
        notes = section_notes(track, sec)

        def quiet(bar):
            return track.calm_tail and base_bar + bar == total_bars - 1

        # lead (p1); 'call' answers every second bar on p2
        prev = None
        for i, n in enumerate(notes):
            bar = n.step // 16
            if quiet(bar):
                continue
            idx = n.idx
            if 'octave_jump' in track.lead and n.step % 16 == 8 and idx + 5 <= theme.hi + 5:
                idx += 5
            midi = _midi(track, scale, idx)
            t0 = (base_bar * 16 + n.step) * step
            seconds = n.length * step * track.legato
            ch, duty, vel = 'p1', track.duty, lead_vel
            if sec.p2 == 'call' and bar % 2 == 1:
                ch, duty, vel = 'p2', 0.5, 0.85
            if 'grace' in track.lead and n.length >= 4 and n.step % 4 == 0:
                g = 0.03
                note_out(ch, t0, g, midi - 2, _pulse_note(midi_hz(_midi(track, scale, idx - 1)), g, duty, vel * 0.8))
                t0, seconds = t0 + g, max(seconds - g, 0.02)
            slide = midi_hz(prev) if ('slide' in track.lead and prev is not None) else None
            x = _pulse_note(midi_hz(midi), seconds, duty, vel, sustain, 'vibrato' in track.lead, slide)
            note_out(ch, t0, seconds, midi, x)
            prev = midi
            if sec.p2 == 'echo':
                note_out('p2', t0 + 3 * step, seconds, midi, _pulse_note(midi_hz(midi), seconds, duty, 0.38, sustain))
            if sec.p2 == 'harmony':
                h = _midi(track, scale, idx - 1)
                note_out('p2', t0, seconds, h, _pulse_note(midi_hz(h), seconds, 0.5, 0.45, sustain))

        for bar in range(8):
            if quiet(bar):
                root = _bass_midi(track, scale, chords[bar])
                note_out('tri', (base_bar + bar) * 16 * step, 16 * step, root, _tri_note(midi_hz(root), 16 * step))
                continue
            t_bar = (base_bar + bar) * 16 * step
            c = chords[bar]
            _render_p2(track, sec, scale, c, t_bar, step, note_out, theme)
            _render_bass(track, sec, scale, c, t_bar, step, note_out)
            _render_drums(sec, bar, t_bar, step, mix)
            _render_extras(track, sec, scale, bar, base_bar, t_bar, step, note_out, mix)
    return master(mix, GAINS, PANS)


def _render_p2(track, sec, scale, c, t_bar, step, note_out, theme):
    def put(steps, length, idx_list, duty, vel, legato):
        for k, s in enumerate(steps):
            m = _midi(track, scale, idx_list[k % len(idx_list)])
            secs = length * step * legato
            note_out('p2', t_bar + s * step, secs, m, _pulse_note(midi_hz(m), secs, duty, vel))

    base = min(_chord_tones(c, theme.lo, theme.hi + 3) or [c + 5])
    if sec.p2 == 'arp16':
        put(range(16), 1, [base, base + 2, base + 4, base + 5], 0.125, 0.32, 0.7)
    elif sec.p2 == 'arp8':
        put(range(0, 16, 2), 2, [base, base + 2, base + 4, base + 2], 0.125, 0.32, 0.7)
    elif sec.p2 == 'slow':
        put(range(0, 16, 4), 4, [base, base + 2, base + 4, base + 2], 0.125, 0.3, 0.9)
    elif sec.p2 == 'sparkle':
        hi = base + 5
        put([4, 6, 12, 14], 2, [hi, hi + 2, hi + 4, hi + 2], 0.125, 0.25, 0.5)
    elif sec.p2 == 'counter':
        low = base - 3
        put([0, 8], 8, [low, low + 2], 0.5, 0.42, 0.92)
    elif sec.p2 == 'stabs':
        put([2, 6, 10, 14], 1, [base, base + 2], 0.5, 0.4, 0.45)


def _render_bass(track, sec, scale, c, t_bar, step, note_out):
    def put(pattern):
        for s, length, offset in pattern:
            m = _bass_midi(track, scale, c) + offset if offset in (0, 12) else _bass_midi(track, scale, c + offset)
            secs = length * step * 0.92
            note_out('tri', t_bar + s * step, secs, m, _tri_note(midi_hz(m), secs))

    style = sec.bass
    if style == 'half':
        put([(0, 8, 0), (8, 8, 3)])
    elif style == 'eighths':
        put([(s, 2, 0) for s in range(0, 12, 2)] + [(12, 2, 12), (14, 2, 3)])
    elif style == 'walk':
        put([(0, 4, 0), (4, 4, 1), (8, 4, 2), (12, 4, 3)])
    elif style == 'drone':
        put([(0, 16, 0)])
    elif style == 'ostinato':
        put([(s * 2, 2, o) for s, o in enumerate([0, 3, 12, 3, 1, 3, 12, 3])])
    elif style == 'bounce':
        put([(0, 2, 0), (4, 2, 12), (6, 2, 0), (8, 2, 3), (12, 2, 12), (14, 2, 3)])
    elif style == 'march':
        put([(0, 3, 0), (4, 3, 3), (8, 3, 0), (12, 3, 3)])
    elif style == 'quarters':
        put([(s, 3, 0) for s in (0, 4, 8, 12)])


def _render_drums(sec, bar, t_bar, step, mix):
    pattern = DRUMS[sec.drums]
    for name, (steps, vel) in pattern.items():
        ch, x = _drum(name)
        for s in steps:
            mix.add(ch, t_bar + s * step, x, vel)
    if sec.fill and bar == 7 and sec.drums != 'none':
        ch, x = _drum('snare')
        for k, s in enumerate((12, 13, 14, 15)):
            mix.add(ch, t_bar + s * step, x, 0.35 + 0.1 * k)


def _render_extras(track, sec, scale, bar, base_bar, t_bar, step, note_out, mix):
    if 'bell' in sec.extras and bar == 7:
        for k, m in enumerate((track.root + 36, track.root + 43)):
            note_out('p2', t_bar + (8 + 2 * k) * step, 2 * step, m, _pulse_note(midi_hz(m), 2 * step, 0.125, 0.45))
    if 'coins' in sec.extras and bar in (3, 7):
        for k, m in enumerate((95, 100)):
            note_out('p2', t_bar + (13 + k) * step, step, m, _pulse_note(midi_hz(m), step * 0.9, 0.5, 0.3))
    if 'hiss' in sec.extras:
        n = int(16 * step * SR)
        start = int(round(t_bar * SR))
        t = (start + np.arange(n)) / SR
        mix.add('noise', t_bar, noise(16000, n, offset=start) * (0.05 + 0.02 * np.sin(2 * np.pi * t / 7.0)))
    if 'deep' in sec.extras and bar % 4 == 0:
        m = _bass_midi(track, scale, 0) - 12
        mix.add('kick', t_bar + 8 * step, _tri_note(midi_hz(m), 6 * step), 0.7)


def tracks_by_id() -> dict:
    return {t.id: t for t in TRACKS}
