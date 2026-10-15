"""Builds the Discord server's emoji images and soundboard sounds from the game's own assets.

    python tools/discord/build_assets.py

- emojis: 128×128 PNGs from Assets/Resources icons (classes, bosses, items, relics) -> tools/discord/emoji/
- sounds: OGG Vorbis from the code-made 8-bit effects (tools/audio/make_8bit_audio.py) -> tools/discord/sounds/
  (Discord soundboard: at most 512 KB and 5.2 s per sound)

layout.json lists what discord_admin.py uploads; this script only prepares the files.
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import soundfile as sf
from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
RES = REPO / 'Assets' / 'Resources'
SYNTH = REPO / 'steam_assets' / 'audio_8bit' / 'synth'

EMOJIS = {
    # classes
    'number_freak': 'class/icon_number_freak.png', 'garden_monk': 'class/icon_garden_monk.png',
    'shrine_archivist': 'class/icon_shrine_archivist.png', 'koi_gambler': 'class/icon_koi_gambler.png',
    'stone_gardener': 'class/icon_stone_gardener.png', 'lantern_seer': 'class/icon_lantern_seer.png',
    'reed_duelist': 'class/icon_reed_duelist.png', 'quiet_cartographer': 'class/icon_quiet_cartographer.png',
    # bosses
    'grove_spirit': 'boss/icon_grove_spirit.png', 'koi_warden': 'boss/icon_koi_warden.png',
    'shrine_oni': 'boss/icon_shrine_oni.png', 'stone_patriarch': 'boss/icon_stone_patriarch.png',
    'summit_demon': 'boss/icon_demon_mask.png',
    # items and relics
    'solver': 'items/icon_solver.png', 'bamboo_scroll': 'items/icon_bamboo_scroll.png',
    'golden_koi': 'items/icon_golden_koi.png', 'garden_lantern': 'items/icon_garden_lantern.png',
    'wind_chime': 'items/icon_wind_chime.png', 'sakura_seal': 'relic/icon_sakura_seal.png',
    'paper_crane': 'relic/icon_paper_crane.png', 'cracked_teacup': 'relic/icon_cracked_teacup.png',
    'spirit_lantern': 'relic/icon_spirit_lantern.png', 'phoenix_feather': 'relic/icon_phoenix_feather.png',
    'puzzle_tile': 'node/icon_puzzle_tile.png',
}

SOUNDS = ['puzzle_solved', 'level_up', 'gold_gain', 'victory', 'wrong_placement', 'achievement', 'boss_gate_open',
          'run_victory_fanfare']


def build_emojis() -> None:
    out = HERE / 'emoji'
    out.mkdir(exist_ok=True)
    for name, rel in EMOJIS.items():
        img = Image.open(RES / rel).convert('RGBA')
        bbox = img.getbbox()  # trim transparent margins so the icon fills the emoji
        if bbox:
            img = img.crop(bbox)
        side = max(img.size)
        square = Image.new('RGBA', (side, side), (0, 0, 0, 0))
        square.paste(img, ((side - img.width) // 2, (side - img.height) // 2))
        square.resize((128, 128), Image.LANCZOS).save(out / f'{name}.png', optimize=True)
    print(f'{len(EMOJIS)} emojis -> {out.relative_to(REPO)}')


def build_sounds() -> None:
    if not all((SYNTH / 'sfx' / f'{s}.wav').exists() for s in SOUNDS):
        subprocess.run([sys.executable, str(REPO / 'tools' / 'audio' / 'make_8bit_audio.py'), 'sfx'], check=True)
    out = HERE / 'sounds'
    out.mkdir(exist_ok=True)
    for name in SOUNDS:
        data, sr = sf.read(SYNTH / 'sfx' / f'{name}.wav')
        if len(data) / sr > 5.2:
            raise SystemExit(f'{name} is longer than 5.2 s')
        path = out / f'{name}.ogg'
        sf.write(path, data, sr, format='OGG', subtype='VORBIS')
        if path.stat().st_size > 512 * 1024:
            raise SystemExit(f'{name}.ogg is larger than 512 KB')
    print(f'{len(SOUNDS)} sounds -> {out.relative_to(REPO)}')


if __name__ == '__main__':
    build_emojis()
    build_sounds()
