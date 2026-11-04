"""Two garden-style proposals per sound effect the game does not play yet (GAP-073, round 2, owner 2026-11-03),
and for the two it plays that small speakers barely sound.

The first round offered one simple tone per event and most were dropped; the owner still wants feedback by sound, so each
event gets proposal A and B here, made from soft "garden" voices (koto, bell, singing bowl, wood block, water drop, wind,
taiko, gong). The code-made 8-bit set (steam_assets/audio_8bit/synth) is proposal C. Everything is deterministic.

    python tools/audio/garden_sfx.py            # WAVs and the listening page in steam_assets/audio_proposals/round2
"""
from __future__ import annotations

import base64
import html
import io
import os
import sys
import wave

import numpy as np

SR = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'steam_assets', 'audio_proposals', 'round2')
CHIP = os.path.join(ROOT, 'steam_assets', 'audio_8bit', 'synth')

# The yo scale on D (D E G A B), the garden's pentatonic.
YO = [62, 64, 67, 69, 71, 74, 76, 79, 81, 83, 86]


def hz(midi: float) -> float:
    return 440.0 * 2 ** ((midi - 69) / 12)


def t(seconds: float) -> np.ndarray:
    return np.arange(int(round(seconds * SR))) / SR


def fade(x: np.ndarray, attack: float = 0.004, release: float = 0.02) -> np.ndarray:
    """Short ramps at both ends so nothing clicks."""
    n = len(x)
    a, r = min(n, int(attack * SR)), min(n, int(release * SR))
    env = np.ones(n)
    if a: env[:a] = np.linspace(0, 1, a)
    if r: env[-r:] *= np.linspace(1, 0, r)
    return x * env


def noise(seconds: float, seed: int) -> np.ndarray:
    return np.random.default_rng(seed).uniform(-1, 1, len(t(seconds)))


def lowpass(x: np.ndarray, cutoff) -> np.ndarray:
    """One-pole lowpass; cutoff (Hz) may be an array the length of x."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a[i]) * v + a[i] * acc
        y[i] = acc
    return y


# ── voices ────────────────────────────────────────────────────────────────────────────────────────────────────────────

def koto(midi: float, seconds: float = 0.7, bright: float = 1.0) -> np.ndarray:
    """A plucked string: harmonics that fade faster the higher they are, a slight pitch drop at the attack."""
    x = t(seconds)
    phase = 2 * np.pi * np.cumsum(hz(midi) * (1 + 0.006 * np.exp(-x * 40))) / SR
    out = np.zeros_like(x)
    for n in range(1, 9):
        out += (bright ** (n - 1)) / n * np.exp(-x * (3 + n * 2.2)) * np.sin(n * phase)
    return fade(out, 0.002)


def bell(midi: float, seconds: float = 1.2) -> np.ndarray:
    """An inharmonic temple bell (Risset's partials)."""
    x = t(seconds)
    f = hz(midi)
    out = np.zeros_like(x)
    for ratio, amp, decay in [(0.56, 1, 2.0), (0.92, 0.67, 2.4), (1.19, 1, 3.0), (1.71, 1.8, 4.0), (2.0, 2.7, 5.0),
                              (2.74, 1.67, 6.5), (3.0, 1.46, 7.5), (3.76, 1.33, 9.0), (4.07, 1.33, 10.0)]:
        out += amp * np.exp(-x * decay) * np.sin(2 * np.pi * f * ratio * x)
    return fade(out, 0.001)


def bowl(midi: float, seconds: float = 2.0) -> np.ndarray:
    """A singing bowl (rin): few partials, a slow beat between two near tones."""
    x = t(seconds)
    f = hz(midi)
    out = np.zeros_like(x)
    for ratio, amp, decay in [(1.0, 1.0, 1.1), (2.71, 0.5, 1.8), (5.15, 0.25, 3.0)]:
        out += amp * np.exp(-x * decay) * (np.sin(2 * np.pi * f * ratio * x) + 0.7 * np.sin(2 * np.pi * (f * ratio + 1.8) * x))
    return fade(out, 0.003)


def wood(midi: float, seconds: float = 0.09, seed: int = 1) -> np.ndarray:
    """A wood block: a damped tone with a short knock of noise."""
    x = t(seconds)
    tone = np.exp(-x * 60) * np.sin(2 * np.pi * hz(midi) * x)
    knock = lowpass(noise(seconds, seed), 3000) * np.exp(-x * 140)
    return fade(tone + 0.6 * knock, 0.0005, 0.01)


def drop(midi_from: float, midi_to: float, seconds: float = 0.12) -> np.ndarray:
    """A water drop: a quick rising sine."""
    x = t(seconds)
    f = hz(midi_from) * (hz(midi_to) / hz(midi_from)) ** (x / seconds)
    phase = 2 * np.pi * np.cumsum(f) / SR
    return fade(np.exp(-x * 30) * np.sin(phase), 0.001)


def wind(seconds: float, low: float, high: float, seed: int = 2) -> np.ndarray:
    """Air rising (low→high) or falling (high→low): noise through a moving lowpass, swelling in and out."""
    x = t(seconds)
    cutoff = low * (high / low) ** (x / seconds)
    swell = np.sin(np.pi * x / seconds) ** 1.5
    return fade(lowpass(noise(seconds, seed), cutoff) * swell * 2.5, 0.01, 0.05)


def taiko(seconds: float = 0.5, seed: int = 3) -> np.ndarray:
    """A drum: a falling low tone and a soft skin noise."""
    x = t(seconds)
    f = 95 * np.exp(-x * 3) + 50
    body = np.exp(-x * 7) * np.sin(2 * np.pi * np.cumsum(f) / SR)
    skin = lowpass(noise(seconds, seed), 900) * np.exp(-x * 25)
    return fade(body + 0.5 * skin, 0.001)


def gong(midi: float, seconds: float = 2.4, swell: float = 0.15) -> np.ndarray:
    """A gong: low inharmonic partials that bloom after the strike."""
    x = t(seconds)
    f = hz(midi)
    out = np.zeros_like(x)
    for ratio, amp, decay in [(1.0, 1.0, 1.0), (1.48, 0.6, 1.4), (2.03, 0.5, 1.8), (2.66, 0.35, 2.4), (3.31, 0.25, 3.0)]:
        out += amp * np.exp(-x * decay) * np.sin(2 * np.pi * f * ratio * x)
    bloom = 1 - np.exp(-x / max(swell, 1e-3))
    return fade(out * bloom, 0.002)


def mix(*parts) -> np.ndarray:
    """Parts as (start seconds, samples, gain), laid on one track."""
    end = max(int(round(s * SR)) + len(p) for s, p, _ in parts)
    out = np.zeros(end)
    for start, part, gain in parts:
        i = int(round(start * SR))
        out[i:i + len(part)] += part * gain
    return out


def run_of(voice, notes, step: float, **kw) -> np.ndarray:
    return mix(*[(i * step, voice(n, **kw), 1.0) for i, n in enumerate(notes)])


# ── the events ────────────────────────────────────────────────────────────────────────────────────────────────────────
# event: (what happens in the game, A, B, peak dBFS). Frequent sounds are quieter.

def _events():
    return {
        'sfx/item_use': ('An item from the bag is used',
                         lambda: run_of(koto, [67, 74], 0.07, seconds=0.5),
                         lambda: mix((0, wind(0.25, 600, 4000, 11), 0.5), (0.12, bell(86, 0.6), 0.35)), -9),
        'sfx/reward_claim': ('A reward is taken after a puzzle',
                             lambda: mix((0, bell(79, 1.0), 0.5), (0.09, bell(83, 1.0), 0.5)),
                             lambda: run_of(koto, [67, 71, 74], 0.06, seconds=0.6), -8),
        'sfx/cell_select': ('A cell on the board is chosen (very frequent)',
                            lambda: wood(88, 0.05, 4),
                            lambda: drop(84, 91, 0.06), -22),
        'sfx/rest_heal': ('Healing at the rest',
                          lambda: mix((0, bowl(69, 2.0), 0.6), (0.35, bowl(76, 1.6), 0.4)),
                          lambda: mix((0, wind(0.9, 300, 1500, 12), 0.6), (0.25, run_of(koto, [62, 67, 71], 0.12, seconds=0.9), 0.7)), -9),
        'sfx/relic_pickup': ('A relic is taken',
                             lambda: mix((0, bell(81, 1.4), 0.5), (0.0, bell(81.1, 1.4), 0.4)),
                             lambda: mix((0, gong(57, 1.6, 0.05), 0.6), (0.18, bell(88, 0.9), 0.35)), -8),
        'sfx/combo_05': ('Five right digits in a row',
                         lambda: run_of(bell, [79, 83], 0.08, seconds=0.6),
                         lambda: run_of(koto, [74, 76, 79], 0.05, seconds=0.45), -12),
        'sfx/combo_10': ('Ten right digits in a row',
                         lambda: run_of(bell, [76, 79, 83, 86], 0.07, seconds=0.7),
                         lambda: mix((0, run_of(koto, [74, 76, 79, 81, 83], 0.045, seconds=0.45), 1.0), (0.25, bell(86, 0.8), 0.4)), -11),
        'sfx/combo_break': ('A mistake ends a run of five or more',
                            lambda: mix((0, koto(64, 0.4, bright=0.5), 1.0), (0.06, koto(63, 0.4, bright=0.5), 0.8)),
                            lambda: run_of(wood, [72, 67], 0.09, seconds=0.12), -13),
        'sfx/hp_critical': ('HP fall to a quarter or less',
                            lambda: mix((0, taiko(0.4, 5), 0.15), (0, wood(69, 0.12, 5), 1.0), (0.22, taiko(0.4, 6), 0.1), (0.22, wood(67, 0.12, 6), 0.8)),
                            lambda: mix((0, bowl(64, 1.6), 0.7), (0.02, bowl(65, 1.6), 0.5)), -9),
        'sfx/pencil_depleted': ('The last pencil mark is used',
                                lambda: wind(0.35, 3000, 500, 13),
                                lambda: run_of(wood, [74, 74], 0.12, seconds=0.1), -15),
        'sfx/gold_gain': ('Gold after a puzzle',
                          lambda: run_of(bell, [91, 95], 0.06, seconds=0.45),
                          lambda: mix(*[(i * 0.04, bell(88 + 3 * i, 0.4), 0.4) for i in range(3)]), -14),
        'sfx/fog_reveal': ('Fog lifts from cells',
                           lambda: wind(0.6, 400, 6000, 14),
                           lambda: mix(*[(i * 0.07, bell(YO[6 + i % 4], 0.7), 0.3) for i in range(4)]), -14),
        'sfx/modifier_activate': ("The boss's rules appear at the gate",
                                  lambda: mix((0, gong(67, 1.6, 0.05), 0.6), (0.15, bell(79, 1.2), 0.4), (0.2, wind(0.6, 800, 5000, 15), 0.6)),
                                  lambda: mix((0, taiko(0.5, 7), 0.3), (0, wood(74, 0.1, 10), 0.8), (0.15, wood(79, 0.1, 8), 0.7), (0.3, wood(86, 0.1, 9), 0.7)), -9),
        'sfx/boss_gate_open': ('The boss gate opens',
                               lambda: mix((0, gong(60, 2.6, 0.25), 1.0), (0.3, bell(81, 1.8), 0.5)),
                               lambda: mix(*[(i * 0.11, wood(67 + 3 * i, 0.12, 20 + i), 0.5 + i * 0.12) for i in range(4)], (0.45, gong(64, 2.0, 0.1), 0.7), (0.5, bell(86, 1.2), 0.45)), -7),
        'sfx/floor_transition': ("A new floor begins (the floor's name)",
                                 lambda: mix((0, wind(1.4, 500, 4000, 16), 0.7), (0.3, bowl(69, 1.4), 0.4)),
                                 lambda: mix(*[(i * 0.09, bell(YO[3 + i], 1.0), 0.28) for i in range(5)]), -10),
        'sfx/level_up': ('A class gains a level (the red seal at the run end)',
                         lambda: run_of(bell, [74, 79, 83, 86], 0.09, seconds=0.9),
                         lambda: mix((0, run_of(koto, [62, 67, 71, 74], 0.03, seconds=0.8), 1.0), (0.2, bell(86, 1.0), 0.45)), -8),
        'sfx/achievement': ('An achievement unlocks',
                            lambda: bowl(74, 2.2),
                            lambda: mix((0, bowl(74, 2.0), 0.7), (0.35, bowl(81, 1.8), 0.5)), -8),
        'sfx/xp_bar_fill': ('While the brush paints the XP',
                            lambda: wind(1.1, 300, 3500, 17) * 0.8,
                            lambda: mix(*[(i * 0.12, bell(YO[i], 0.5), 0.25) for i in range(8)]), -16),
        'sfx/victory': ('A boss is defeated',
                        lambda: mix((0, taiko(0.6, 18), 1.0), (0.05, bell(74, 1.3), 0.4), (0.05, bell(78, 1.3), 0.3), (0.05, bell(81, 1.3), 0.3)),
                        lambda: mix((0, run_of(koto, [62, 67, 69, 74, 79], 0.06, seconds=0.9), 1.0), (0.35, gong(50, 1.6, 0.05), 0.5)), -6),
        # Played already, but under 300 Hz only: small speakers (laptops, handhelds) barely sound them.
        'sfx/wrong_placement': ('A wrong digit (plays already, too low for small speakers)',
                                lambda: mix((0, koto(63, 0.35, bright=0.4), 1.0), (0.0, koto(64, 0.35, bright=0.4), 0.8)),
                                lambda: run_of(wood, [71, 66], 0.07, seconds=0.1), -10),
        'sfx/hp_loss': ('HP lost (plays already, too low for small speakers)',
                        lambda: mix((0, taiko(0.35, 30), 0.12), (0, wood(66, 0.14, 31), 1.0)),
                        lambda: mix((0, bowl(66, 0.8), 0.8), (0.0, bowl(67, 0.8), 0.6)), -9),
        'ui/item_reward': ('An item appears on the reward screen',
                           lambda: bell(86, 0.8),
                           lambda: run_of(koto, [79, 86], 0.05, seconds=0.45), -12),
        'ui/relic_reveal': ('A relic appears at the shrine',
                            lambda: mix((0, wind(0.5, 800, 7000, 19), 0.5), (0.25, bell(83, 1.0), 0.45)),
                            lambda: mix((0, gong(47, 1.4, 0.05), 0.5), (0.15, bell(88, 0.8), 0.35)), -10),
    }


EVENTS = _events()


def normalize(x: np.ndarray, peak_db: float) -> np.ndarray:
    peak = np.max(np.abs(x))
    return x if peak == 0 else x / peak * 10 ** (peak_db / 20)


def wav_bytes(x: np.ndarray) -> bytes:
    buf = io.BytesIO()
    with wave.open(buf, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())
    return buf.getvalue()


def render(event: str) -> tuple:
    """The event's proposals A and B as WAV bytes."""
    _, a, b, peak = EVENTS[event]
    return wav_bytes(normalize(a(), peak)), wav_bytes(normalize(b(), peak))


def low_only(data: bytes) -> bool:
    """True when less than 60 % of a 16-bit WAV's energy lies above 300 Hz (laptop and handheld speakers barely play
    what is under it). Other sample widths are not judged."""
    with wave.open(io.BytesIO(data)) as w:
        if w.getsampwidth() != 2:
            return False
        x = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float)
        if w.getnchannels() == 2:
            x = x.reshape(-1, 2).mean(axis=1)
        rate = w.getframerate()
    spec = np.abs(np.fft.rfft(x)) ** 2
    return spec.sum() > 0 and spec[np.fft.rfftfreq(len(x), 1 / rate) >= 300].sum() / spec.sum() < 0.6


def page(sounds: dict) -> str:
    rows = []
    for event, (what, a, b, c) in sounds.items():
        name = event.split('/')[1]
        cells = []
        c_label = 'C 8-bit (tief: auf kleinen Lautsprechern leise)' if c and low_only(c) else 'C 8-bit'
        for label, data in (('A', a), ('B', b), (c_label, c)):
            src = 'data:audio/wav;base64,' + base64.b64encode(data).decode() if data else ''
            cells.append(f'<div class="opt"><b>{label}</b>' + (f'<audio controls preload="metadata" src="{src}"></audio>' if src else '<i>none</i>') + '</div>')
        choice = ''.join(f'<label><input type="radio" name="{name}" value="{v}"><span>{v}</span></label>' for v in ('A', 'B', 'C', 'none'))
        rows.append(f'<section><h3>{html.escape(name.replace("_", " "))}</h3><p>{html.escape(what)}</p>'
                    f'<div class="opts">{"".join(cells)}</div><div class="choice">{choice}</div></section>')
    return TEMPLATE.replace('{rows}', '\n'.join(rows)).replace('{names}', repr([e.split('/')[1] for e in sounds]))


TEMPLATE = """<!doctype html><html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Sound-Vorschläge Runde 2</title><style>
:root{--bg:#f4efe4;--paper:#fbf7ee;--ink:#2b2620;--soft:#6b6155;--line:#d9cfbd;--red:#9e2a2b;--moss:#4f6b3a}
@media (prefers-color-scheme:dark){:root{--bg:#1d1a16;--paper:#26221d;--ink:#eee6d6;--soft:#b3a894;--line:#3d362d;--red:#d9655f;--moss:#93b06f}}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.45 system-ui,"Segoe UI",sans-serif}main{max-width:1000px;margin:0 auto;padding:24px 16px 120px}
h1{font-size:26px;margin:0 0 6px}p.lead{color:var(--soft)}section{background:var(--paper);border:1px solid var(--line);border-radius:10px;padding:12px 14px;margin:10px 0}
h3{margin:0;font-size:18px}section p{margin:2px 0 8px;color:var(--soft);font-size:14px}.opts{display:grid;grid-template-columns:repeat(3,1fr);gap:10px}
.opt b{display:block;font-size:13px;color:var(--soft)}audio{width:100%;height:32px}.choice{display:flex;gap:6px;margin-top:8px}
.choice label{border:1px solid var(--line);border-radius:999px;padding:3px 12px;cursor:pointer;font-size:14px}.choice input{display:none}
.choice label:has(input:checked){background:var(--moss);color:#fff;border-color:var(--moss)}.choice label:has(input[value=none]:checked){background:var(--red);border-color:var(--red)}
.bar{position:fixed;left:0;right:0;bottom:0;background:var(--bg);border-top:1px solid var(--line);padding:10px 16px;display:flex;gap:12px;align-items:center}
button{background:var(--red);color:#fff;border:0;border-radius:999px;padding:9px 18px;font-size:15px;cursor:pointer}textarea{flex:1;height:44px;background:var(--paper);color:var(--ink);border:1px solid var(--line);border-radius:8px;font:12px ui-monospace,Consolas,monospace}
@media (max-width:760px){.opts{grid-template-columns:1fr}}</style></head><body><main>
<h1>Sound-Vorschläge, Runde 2</h1><p class="lead">GAP-073: Für jedes Ereignis, das du weglassen wolltest, gibt es jetzt drei Vorschläge.
A und B sind neue Garten-Klänge (Koto, Glocke, Klangschale, Holz, Wasser, Wind, Taiko, Gong), C ist das 8-bit-Set. Wähl pro Ereignis einen
oder "none", dann "Auswahl kopieren" und den Text in den Chat. Die Klänge stecken in dieser Datei, sie spielt ohne weitere Dateien.</p>
{rows}
</main><div class="bar"><button id="copy">Auswahl kopieren</button><textarea id="out" readonly></textarea></div>
<script>const NAMES={names};document.getElementById('copy').onclick=async()=>{const lines=NAMES.map(n=>{const v=document.querySelector(`input[name="${n}"]:checked`);return `${n}: ${v?v.value:'-'}`});
const text='GAP-073 Runde 2\\n'+lines.join('\\n');document.getElementById('out').value=text;try{await navigator.clipboard.writeText(text)}catch(e){document.getElementById('out').select()}};</script></body></html>"""


def main(argv) -> int:
    os.makedirs(OUT, exist_ok=True)
    sounds = {}
    for event in EVENTS:
        a, b = render(event)
        name = event.split('/')[1]
        for tag, data in (('a', a), ('b', b)):
            with open(os.path.join(OUT, f'{name}_{tag}.wav'), 'wb') as f:
                f.write(data)
        chip = os.path.join(CHIP, event + '.wav')
        with open(chip, 'rb') if os.path.exists(chip) else io.BytesIO() as f:
            sounds[event] = (EVENTS[event][0], a, b, f.read())
    path = os.path.join(OUT, 'sfx_round2.html')
    with open(path, 'w', encoding='utf-8') as f:
        f.write(page(sounds))
    print(path)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
