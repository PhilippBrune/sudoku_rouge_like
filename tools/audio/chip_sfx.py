"""The game's 35 sound effects, made from 2A03 voices only (docs/audio-8bit-prompts.md, section 5).

Each effect is a small function returning mono samples; EFFECTS maps the game's file path (as in
AudioAssetService, e.g. "sfx/gold_gain") to (function, peak dBFS). Frequent sounds are quieter.
"""
from __future__ import annotations

import numpy as np

from chip_synth import SR, click_free, decay_env, envelope, midi_hz, noise, pulse, triangle


def _n(seconds: float) -> int:
    return max(int(round(seconds * SR)), 1)


class Shot:
    """A one-shot that grows as parts are added at time offsets."""

    def __init__(self):
        self.buf = np.zeros(1)

    def add(self, start: float, x: np.ndarray, gain: float = 1.0) -> 'Shot':
        pos = _n(start) if start > 0 else 0
        end = pos + len(x)
        if end > len(self.buf):
            self.buf = np.concatenate([self.buf, np.zeros(end - len(self.buf))])
        self.buf[pos:end] += x * gain
        return self

    def done(self) -> np.ndarray:
        return self.buf


def tone(f0, dur, f1=None, wave='pulse', duty=0.5, tau=None, adsr=None, vib=None):
    """One note: optional exponential glide f0 -> f1, decay (tau) or ADSR envelope, vibrato (rate Hz, cents)."""
    n = _n(dur)
    t = np.arange(n) / SR
    freq = np.full(n, float(f0)) if f1 is None else f0 * (f1 / f0) ** (t / dur)
    if vib:
        rate, cents = vib
        freq = freq * 2.0 ** (cents / 1200.0 * np.sin(2 * np.pi * rate * t))
    x = pulse(freq, n, duty) if wave == 'pulse' else triangle(freq, n)
    if tau is not None:
        env = decay_env(n, tau)
    else:
        a, d, s, r = adsr or (0.003, 0.08, 0.7, 0.03)
        env = envelope(max(n - _n(r), 1), a, d, s, r)[:n]
        env = np.pad(env, (0, n - len(env)))
    return click_free(x * env)


def notes(midis, step, wave='pulse', duty=0.25, gate=0.9, tau=None, last=None):
    """A run of notes `step` seconds apart; `last` gives the final note its own length."""
    shot = Shot()
    for i, m in enumerate(midis):
        length = (last if (last and i == len(midis) - 1) else step) * gate
        shot.add(i * step, tone(midi_hz(m), length, wave=wave, duty=duty, tau=tau))
    return shot.done()


def chord(midis, dur, rate=50.0, duty=0.25, tau=0.5):
    """The NES chord trick: one channel cycling through the chord tones very fast."""
    n = _n(dur)
    idx = (np.arange(n) / SR * rate).astype(int) % len(midis)
    freq = midi_hz(np.array(midis))[idx]
    return click_free(pulse(freq, n, duty) * decay_env(n, tau))


def nz(dur, rate0, rate1=None, short=False, tau=None, swell=False):
    n = _n(dur)
    t = np.arange(n) / SR
    rate = np.full(n, float(rate0)) if rate1 is None else rate0 * (rate1 / rate0) ** (t / dur)
    if swell:  # starts audible: the delivery rules allow no silence before a sound
        env = 0.12 + 0.88 * np.sin(np.pi * t / dur) ** 1.5
        env = np.round(env * 15) / 15
    else:
        env = decay_env(n, tau or dur / 3)
    return click_free(noise(rate, n, short) * env)


# --- the effects (names = file names the game loads) ------------------------------------------------------------------

def cell_select():
    return tone(2400, 0.035, duty=0.125, tau=0.012)


def correct_placement():
    return Shot().add(0, tone(midi_hz(88), 0.07, duty=0.25, tau=0.05)).add(
        0.065, tone(midi_hz(95), 0.13, duty=0.25, tau=0.07)).done()


def wrong_placement():
    return Shot().add(0, tone(midi_hz(57), 0.13, f1=midi_hz(56.5), duty=0.5, tau=0.09)).add(
        0.12, tone(midi_hz(52), 0.18, f1=midi_hz(51), duty=0.5, tau=0.1)).done()


def pencil_toggle():
    return Shot().add(0, nz(0.03, 18000, short=True, tau=0.008)).add(0, tone(3200, 0.02, duty=0.125, tau=0.006), 0.4).done()


def pencil_depleted():
    return tone(600, 0.28, f1=140, wave='tri', tau=0.12)


def puzzle_solved():
    run = notes([74, 76, 79, 81, 83, 86], 0.06, duty=0.25, tau=0.08)
    return Shot().add(0, run).add(0.36, chord([86, 90, 93], 0.8, rate=40, duty=0.25, tau=0.35)).add(
        0.36, tone(midi_hz(50), 0.7, wave='tri', tau=0.4), 0.8).done()


def combo_05():
    return Shot().add(0, notes([79, 83, 86], 0.05, duty=0.25, tau=0.05, last=0.12)).add(
        0.15, tone(midi_hz(98), 0.08, duty=0.125, tau=0.04), 0.5).done()


def combo_10():
    return Shot().add(0, notes([79, 81, 83, 86, 88], 0.04, duty=0.25, tau=0.04)).add(
        0.2, tone(midi_hz(100), 0.3, duty=0.25, tau=0.12, vib=(9, 25))).done()


def combo_break():
    return tone(800, 0.3, f1=190, duty=0.5, tau=0.14)


def hp_loss():
    return Shot().add(0, tone(170, 0.16, f1=55, wave='tri', tau=0.08)).add(0, nz(0.09, 6000, 1500, tau=0.03), 0.8).done()


def hp_critical():
    beep = lambda m: tone(midi_hz(m), 0.11, duty=0.5, tau=0.07)  # noqa: E731
    return Shot().add(0, beep(57)).add(0.13, beep(53)).add(0.32, beep(57), 0.7).done()


def gold_gain():
    return Shot().add(0, tone(midi_hz(83), 0.06, duty=0.5, tau=0.05)).add(0.055, tone(midi_hz(88), 0.2, duty=0.5, tau=0.09)).done()


def item_use():
    return Shot().add(0, tone(400, 0.26, f1=1600, duty=0.25, tau=0.2, vib=(14, 40))).add(
        0.18, notes([96, 100, 103], 0.04, duty=0.125, tau=0.04), 0.5).done()


def relic_pickup():
    arp = Shot()
    for i, m in enumerate([81, 85, 88, 93, 85, 88, 93, 97, 88, 93, 97, 100]):
        arp.add(i * 0.04, tone(midi_hz(m), 0.045, duty=0.25, tau=0.05), 0.7 + 0.025 * i)
    return arp.add(0.5, tone(midi_hz(93), 0.35, wave='tri', tau=0.2)).done()


def reward_claim():
    return Shot().add(0, chord([72, 76, 79], 0.14, rate=45, duty=0.5, tau=0.1)).add(
        0.14, notes([79, 81, 84, 88], 0.06, duty=0.25, tau=0.06, last=0.18)).done()


def shop_purchase():
    coin = lambda: Shot().add(0, tone(midi_hz(83), 0.05, duty=0.5, tau=0.04)).add(  # noqa: E731
        0.045, tone(midi_hz(88), 0.12, duty=0.5, tau=0.06)).done()
    return Shot().add(0, coin()).add(0.13, coin(), 0.85).done()


def shop_reroll():
    n = _n(0.38)
    t = np.arange(n) / SR
    freq = 880 * 2 ** (3 / 12 * np.sin(2 * np.pi * (10 + 14 * t) * t))
    return click_free(pulse(freq, n, 0.25) * decay_env(n, 0.2))


def rest_heal():
    shot = Shot().add(0, tone(300, 0.7, f1=900, wave='tri', adsr=(0.05, 0.3, 0.6, 0.15)))
    for i, m in enumerate([86, 90, 93, 98]):
        shot.add(0.18 + i * 0.12, tone(midi_hz(m), 0.08, duty=0.125, tau=0.05), 0.45)
    return shot.done()


def path_advance():
    return Shot().add(0, nz(0.018, 9000, tau=0.006)).add(0.03, tone(midi_hz(76), 0.06, duty=0.25, tau=0.04), 0.7).done()


def fog_reveal():
    return nz(0.32, 2000, 20000, swell=True)


def modifier_activate():
    n = _n(0.5)
    t = np.arange(n) / SR
    swell = (0.12 + 0.88 * np.minimum(t / 0.25, 1.0)) * np.exp(-np.maximum(t - 0.25, 0) / 0.15)
    swell = np.round(swell * 15) / 15
    low = pulse(midi_hz(45), n, 0.5) + pulse(midi_hz(46), n, 0.25) * 0.8
    return click_free(low * swell)


def boss_gate_open():
    return Shot().add(0, nz(0.7, 400, 900, swell=True)).add(
        0.55, notes([57, 60, 64, 69], 0.1, duty=0.5, tau=0.12, last=0.35)).add(
        0.55, tone(midi_hz(33), 0.7, wave='tri', tau=0.4), 0.8).done()


def floor_transition():
    return Shot().add(0, nz(0.85, 1000, 6000, swell=True), 0.8).add(
        0.75, notes([76, 81, 86], 0.15, duty=0.125, tau=0.15, last=0.45)).done()


def level_up():
    return Shot().add(0, notes([72, 76, 79, 84, 88, 91], 0.04, duty=0.25, tau=0.05)).add(
        0.24, chord([84, 88, 91, 96], 0.7, rate=50, duty=0.25, tau=0.35)).add(
        0.24, tone(midi_hz(48), 0.6, wave='tri', tau=0.35), 0.7).done()


def achievement():
    return Shot().add(0, chord([77, 81, 84], 0.24, rate=45, tau=0.2)).add(
        0.24, chord([84, 88, 91], 0.6, rate=45, tau=0.3)).add(
        0.3, notes([96, 100, 103, 108], 0.05, duty=0.125, tau=0.04), 0.5).done()


def xp_bar_fill():
    shot, t, i = Shot(), 0.0, 0
    while t < 1.45:
        gap = 0.065 - 0.045 * (t / 1.45)
        shot.add(t, tone(500 * 3 ** (t / 1.45), 0.025, duty=0.25, tau=0.012))
        t += gap
        i += 1
    return shot.done()


def victory():
    return Shot().add(0, notes([72, 76, 79, 84], 0.11, duty=0.25, tau=0.12, last=0.6)).add(
        0, notes([48, 48, 55, 48], 0.11, wave='tri', tau=0.12, last=0.6), 0.8).done()


def run_victory_fanfare():
    shot = Shot()
    melody = [(67, 0.2), (72, 0.2), (76, 0.2), (79, 0.4), (76, 0.2), (79, 0.2), (84, 1.0)]
    t = 0.0
    for m, d in melody:
        shot.add(t, tone(midi_hz(m), d * 0.92, duty=0.25, adsr=(0.004, 0.1, 0.7, 0.03), vib=(5.5, 12) if d > 0.5 else None))
        shot.add(t, tone(midi_hz(m - 12), d * 0.8, duty=0.5, tau=0.15), 0.45)
        t += d
    bass = [(48, 0.4), (43, 0.4), (48, 0.4), (55, 0.4), (48, 0.8)]
    tb = 0.0
    for m, d in bass:
        shot.add(tb, tone(midi_hz(m), d * 0.9, wave='tri', adsr=(0.002, 0.2, 1.0, 0.02)), 0.9)
        tb += d
    for i in range(16):  # rising noise roll into the final chord
        shot.add(1.5 + i * 0.05, nz(0.04, 7000, tau=0.02), 0.3 + 0.04 * i)
    shot.add(t, chord([72, 76, 79, 84], 1.6, rate=40, duty=0.25, tau=0.7))
    shot.add(t, tone(midi_hz(36), 1.4, wave='tri', tau=0.7), 0.9)
    return shot.done()


def game_over():
    return Shot().add(0, notes([69, 67, 64, 62, 60], 0.22, duty=0.25, tau=0.2, last=0.4)).add(
        1.15, tone(midi_hz(45), 0.6, wave='tri', tau=0.3), 0.9).done()


def button_hover():
    return tone(3000, 0.025, duty=0.125, tau=0.008)


def button_click():
    return Shot().add(0, tone(1000, 0.04, duty=0.5, tau=0.015)).add(0, nz(0.012, 15000, tau=0.004), 0.5).done()


def menu_open():
    return Shot().add(0, tone(midi_hz(84), 0.06, duty=0.25, tau=0.04)).add(0.06, tone(midi_hz(91), 0.09, duty=0.25, tau=0.05)).done()


def menu_close():
    return Shot().add(0, tone(midi_hz(91), 0.06, duty=0.25, tau=0.04)).add(0.06, tone(midi_hz(84), 0.09, duty=0.25, tau=0.05)).done()


def item_reward():
    shot = Shot().add(0, notes([79, 83, 86], 0.09, duty=0.25, tau=0.08, last=0.2))
    for i in range(3):
        shot.add(0.3 + i * 0.07, tone(midi_hz(100), 0.04, duty=0.125, tau=0.02), 0.5)
    return shot.done()


def relic_reveal():
    shot = Shot()
    scale = [81, 82, 86, 88, 89]  # A in: A Bb D E F
    for i in range(14):
        shot.add(i * 0.045, tone(midi_hz(scale[(i * 2) % 5] + (12 if i > 6 else 0)), 0.05, duty=0.125, tau=0.05),
                 0.4 + 0.3 * np.sin(np.pi * i / 14))
    return shot.add(0.6, tone(midi_hz(81), 0.4, wave='tri', tau=0.25)).done()


# file path (as in AudioAssetService) -> (generator, peak dBFS)
EFFECTS = {
    'sfx/cell_select': (cell_select, -14.0),
    'sfx/correct_placement': (correct_placement, -6.0),
    'sfx/wrong_placement': (wrong_placement, -4.0),
    'sfx/pencil_toggle': (pencil_toggle, -10.0),
    'sfx/pencil_depleted': (pencil_depleted, -5.0),
    'sfx/puzzle_solved': (puzzle_solved, -3.0),
    'sfx/combo_05': (combo_05, -6.0),
    'sfx/combo_10': (combo_10, -5.0),
    'sfx/combo_break': (combo_break, -6.0),
    'sfx/hp_loss': (hp_loss, -3.0),
    'sfx/hp_critical': (hp_critical, -4.0),
    'sfx/gold_gain': (gold_gain, -6.0),
    'sfx/item_use': (item_use, -4.0),
    'sfx/relic_pickup': (relic_pickup, -3.0),
    'sfx/reward_claim': (reward_claim, -4.0),
    'sfx/shop_purchase': (shop_purchase, -5.0),
    'sfx/shop_reroll': (shop_reroll, -6.0),
    'sfx/rest_heal': (rest_heal, -4.0),
    'sfx/path_advance': (path_advance, -8.0),
    'sfx/fog_reveal': (fog_reveal, -7.0),
    'sfx/modifier_activate': (modifier_activate, -4.0),
    'sfx/boss_gate_open': (boss_gate_open, -3.0),
    'sfx/floor_transition': (floor_transition, -4.0),
    'sfx/level_up': (level_up, -3.0),
    'sfx/achievement': (achievement, -3.0),
    'sfx/xp_bar_fill': (xp_bar_fill, -8.0),
    'sfx/victory': (victory, -3.0),
    'sfx/run_victory_fanfare': (run_victory_fanfare, -3.0),
    'sfx/game_over': (game_over, -3.0),
    'ui/button_hover': (button_hover, -18.0),
    'ui/button_click': (button_click, -10.0),
    'ui/menu_open': (menu_open, -9.0),
    'ui/menu_close': (menu_close, -9.0),
    'ui/item_reward': (item_reward, -5.0),
    'ui/relic_reveal': (relic_reveal, -4.0),
}
