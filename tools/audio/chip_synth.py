"""NES-style (Ricoh 2A03) voices, a looping mixer and loudness tools for the 8-bit soundtrack.

The 2A03 has two pulse channels (duty 12.5/25/50/75 %, 16 volume steps), a triangle channel (32-step waveform,
no volume control) and a noise channel (15-bit LFSR, long or short "metallic" mode). The game's music and sound
effects are rendered from exactly these voices (docs/audio-8bit-prompts.md, AUDIO-MUSIC-001/002).
"""
from __future__ import annotations

import numpy as np
from scipy import signal

SR = 44100


def midi_hz(midi):
    return 440.0 * 2.0 ** ((np.asarray(midi, dtype=float) - 69.0) / 12.0)


def _phase(freq, n: int):
    inc = np.broadcast_to(np.asarray(freq, dtype=float), (n,)) / SR
    phase = np.concatenate(([0.0], np.cumsum(inc)[:-1]))
    return np.mod(phase, 1.0), inc


def _blep(t, dt):
    """PolyBLEP residual: removes most of the aliasing of a hard step without softening the pulse's edge."""
    out = np.zeros_like(t)
    a = t < dt
    x = t[a] / dt[a]
    out[a] = x + x - x * x - 1.0
    b = t > 1.0 - dt
    x = (t[b] - 1.0) / dt[b]
    out[b] = x * x + x + x + 1.0
    return out


def pulse(freq, n: int, duty: float = 0.5):
    t, dt = _phase(freq, n)
    y = np.where(t < duty, 1.0, -1.0)
    return y + _blep(t, dt) - _blep(np.mod(t - duty, 1.0), dt)


_TRI = np.concatenate([np.arange(15, -1, -1), np.arange(0, 16)]) / 7.5 - 1.0


def triangle(freq, n: int):
    t, _ = _phase(freq, n)
    return _TRI[(t * 32).astype(int) % 32]


def _lfsr(short: bool) -> np.ndarray:
    reg, tap, out = 1, 6 if short else 1, []
    for _ in range(93 if short else 32767):
        bit = (reg ^ (reg >> tap)) & 1
        reg = (reg >> 1) | (bit << 14)
        out.append(reg & 1)
    return np.array(out, dtype=float) * 2.0 - 1.0


_NOISE = {False: _lfsr(False), True: _lfsr(True)}


def noise(rate, n: int, short: bool = False, offset: int = 0):
    """LFSR noise clocked at `rate` Hz (scalar or per-sample array; higher = brighter)."""
    seq = _NOISE[short]
    steps = np.cumsum(np.broadcast_to(np.asarray(rate, dtype=float), (n,)) / SR)
    return seq[(steps.astype(np.int64) + offset) % len(seq)]


def envelope(n_gate: int, attack=0.004, decay=0.12, sustain=0.65, release=0.04, steps=True):
    """Gate-length ADSR plus release tail; `steps` quantizes to the 2A03's 16 volume levels."""
    n_gate = max(int(n_gate), 1)
    n_rel = max(int(release * SR), 1)
    t = np.arange(n_gate) / SR
    a = max(attack, 1e-4)
    held = np.where(t < a, t / a, sustain + (1.0 - sustain) * np.exp(-(t - a) / max(decay, 1e-4)))
    tail = held[-1] * np.linspace(1.0, 0.0, n_rel)
    env = np.concatenate([held, tail])
    if steps:
        env = np.round(env * 15.0) / 15.0
    return env


def decay_env(n: int, tau: float, attack: float = 0.002, steps=True):
    t = np.arange(n) / SR
    env = np.minimum(t / max(attack, 1e-4), 1.0) * np.exp(-t / tau)
    return np.round(env * 15.0) / 15.0 if steps else env


def click_free(x, ms: float = 2.0):
    """Short fades at both ends so a gated note never clicks."""
    k = min(int(ms * SR / 1000), len(x) // 2)
    if k > 0:
        ramp = np.linspace(0.0, 1.0, k)
        x[:k] *= ramp
        x[-k:] *= ramp[::-1]
    return x


# --- mixing ------------------------------------------------------------------------------------------------------------

class LoopMix:
    """Per-channel mono buffers of one loop; anything that runs past the end wraps to the start (seamless loop)."""

    def __init__(self, seconds: float, channels=('p1', 'p2', 'tri', 'kick', 'noise')):
        self.n = int(round(seconds * SR))
        self.ch = {c: np.zeros(self.n) for c in channels}

    def add(self, channel: str, start: float, samples: np.ndarray, gain: float = 1.0):
        buf = self.ch[channel]
        pos = int(round(start * SR)) % self.n
        data = samples * gain
        while len(data):
            take = min(len(data), self.n - pos)
            buf[pos:pos + take] += data[:take]
            data = data[take:]
            pos = 0


def _loop_filter(b, a, x):
    """Filters a loop as if it had been playing already (no start-up transient at the seam)."""
    return signal.lfilter(b, a, np.concatenate([x, x]), axis=0)[len(x):]


def nes_output(x, loop: bool = True):
    """The console's output stage: high-pass ~37 Hz (DC removal) and low-pass ~14 kHz."""
    run = _loop_filter if loop else (lambda b, a, v: signal.lfilter(b, a, v, axis=0))
    b, a = signal.butter(1, 37.0, 'highpass', fs=SR)
    x = run(b, a, x)
    b, a = signal.butter(1, 14000.0, 'lowpass', fs=SR)
    return run(b, a, x)


def master(mix: LoopMix, gains: dict, pans: dict, target_lufs=-16.0, peak_dbfs=-1.0):
    """Stereo master of a loop: gains, constant-power pan, NES output filter, loudness and peak normalisation."""
    out = np.zeros((mix.n, 2))
    for name, buf in mix.ch.items():
        pan = pans.get(name, 0.0)
        angle = (pan + 1.0) * np.pi / 4.0
        out[:, 0] += buf * gains.get(name, 1.0) * np.cos(angle)
        out[:, 1] += buf * gains.get(name, 1.0) * np.sin(angle)
    out = nes_output(out)
    loud = lufs(out)
    if np.isfinite(loud):
        out *= 10.0 ** ((target_lufs - loud) / 20.0)
    peak = np.max(np.abs(out))
    limit = 10.0 ** (peak_dbfs / 20.0)
    if peak > limit:
        out *= limit / peak
    return out


def sfx_master(x, peak_dbfs=-3.0):
    x = nes_output(np.asarray(x, dtype=float), loop=False)
    peak = np.max(np.abs(x))
    if peak > 0:
        x = x * (10.0 ** (peak_dbfs / 20.0) / peak)
    return click_free(x, 1.0)


# --- loudness (ITU-R BS.1770 / EBU R128 integrated) ------------------------------------------------------------------

def _biquad_shelf(fs, f0=1681.974450955533, gain_db=3.999843853973347, q=0.7071752369554196):
    a_ = 10 ** (gain_db / 40.0)
    w0 = 2 * np.pi * f0 / fs
    alpha = np.sin(w0) / (2 * q)
    cos = np.cos(w0)
    b = [a_ * ((a_ + 1) + (a_ - 1) * cos + 2 * np.sqrt(a_) * alpha),
         -2 * a_ * ((a_ - 1) + (a_ + 1) * cos),
         a_ * ((a_ + 1) + (a_ - 1) * cos - 2 * np.sqrt(a_) * alpha)]
    a = [(a_ + 1) - (a_ - 1) * cos + 2 * np.sqrt(a_) * alpha,
         2 * ((a_ - 1) - (a_ + 1) * cos),
         (a_ + 1) - (a_ - 1) * cos - 2 * np.sqrt(a_) * alpha]
    return np.array(b) / a[0], np.array(a) / a[0]


def _biquad_highpass(fs, f0=38.13547087602444, q=0.5003270373238773):
    w0 = 2 * np.pi * f0 / fs
    alpha = np.sin(w0) / (2 * q)
    cos = np.cos(w0)
    b = [(1 + cos) / 2, -(1 + cos), (1 + cos) / 2]
    a = [1 + alpha, -2 * cos, 1 - alpha]
    return np.array(b) / a[0], np.array(a) / a[0]


def lufs(x) -> float:
    """Integrated loudness in LUFS (mono or stereo), with the absolute and relative gates."""
    x = np.atleast_2d(np.asarray(x, dtype=float).T).T if np.ndim(x) == 1 else np.asarray(x, dtype=float)
    b1, a1 = _biquad_shelf(SR)
    b2, a2 = _biquad_highpass(SR)
    y = signal.lfilter(b2, a2, signal.lfilter(b1, a1, x, axis=0), axis=0)
    block, step = int(0.4 * SR), int(0.1 * SR)
    if len(y) < block:
        z = np.mean(y ** 2, axis=0)[None, :]
    else:
        starts = range(0, len(y) - block + 1, step)
        z = np.array([np.mean(y[s:s + block] ** 2, axis=0) for s in starts])
    power = z.sum(axis=1)
    with np.errstate(divide='ignore'):
        loud = -0.691 + 10 * np.log10(power)
    gated = power[loud > -70.0]
    if not len(gated):
        return float('-inf')
    relative = -0.691 + 10 * np.log10(gated.mean()) - 10.0
    gated = power[(loud > -70.0) & (loud > relative)]
    return float(-0.691 + 10 * np.log10(gated.mean()))


def peak_db(x) -> float:
    peak = float(np.max(np.abs(x)))
    return 20 * np.log10(peak) if peak > 0 else float('-inf')
