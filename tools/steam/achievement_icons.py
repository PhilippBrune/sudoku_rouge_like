"""Makes the Steam achievement icons of docs/Achievements.md from the game's own art: a square crop of a painting, the
game icon in the middle, a short gold label, a gold frame (achieved) and the same in grey (unachieved), 256x256 JPG.
The Harmony icons (harmony_*) are older and hand-made; this tool leaves them alone.

Usage: python tools/steam/achievement_icons.py [--only <api name> ...]
Writes steam_assets/achievements/achieved_<api name>.jpg and unachieved_<api name>.jpg.
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'Resources')
OUT = os.path.join(ROOT, 'steam_assets', 'achievements')
SIZE = 256
GOLD = (222, 184, 102)
FONT = os.path.join(os.environ.get('WINDIR', r'C:\Windows'), 'Fonts', 'timesbd.ttf')  # lining digits

# api name: (painting, icon, label)
ICONS = {
    'first_puzzle': ('background/bg_garden_grove', 'node/icon_puzzle_tile', 'First Stone'),
    'first_boss': ('background/bg_boss_gate', 'boss/icon_shrine_oni', 'The Gate'),
    'floor_5': ('background/bg_demon_summit', 'node/icon_preboss_gate', 'Summit'),
    'first_win': ('background/bg_boss_cleared', 'ui/icon_golden_bloom', 'Victory'),
    'perfect_run': ('background/bg_puzzle_board', 'ui/icon_scroll_stamp', 'Clean Ink'),
    'no_items_win': ('background/bg_rest', 'items/icon_offering_bowl', 'Empty Hands'),
    'one_hp_win': ('background/bg_defeat', 'items/icon_dim_lantern', 'By a Thread'),
    'cursed_three': ('background/bg_curse', 'cursed/icon_broken_mask', 'The Curse'),
    'nine_by_nine': ('background/bg_puzzle_board', 'ui/icon_framed_garden', '9 \u00d7 9'),
    'five_stars': ('background/bg_koi_terrace', 'meta/icon_golden_sakura', 'Five Stars'),
    'class_second': ('background/bg_class_select', 'class/icon_garden_monk', 'Two Walkers'),
    'class_all': ('background/bg_class_select', 'meta/icon_full_sakura', 'Eight'),
    'class_level_10': ('background/bg_meta_progression', 'meta/icon_bud', 'Level 10'),
    'class_level_40': ('background/bg_meta_progression', 'meta/icon_halo_bloom', 'Level 40'),
    'win_every_class': ('background/bg_main_menu', 'meta/icon_dual_mask', 'Eight Ways'),
    'endless_25': ('background/bg_menu_zen_results', 'meta/icon_infinity_stone', 'Depth 25'),
    'trials_grandmaster': ('background/bg_menu_trials_results', 'ui/icon_trials_seal', 'Grandmaster'),
    'monthly_walk': ('background/bg_monthly_walk', 'ui/icon_monthly_lantern', 'The Moon'),
    'daily_streak_7': ('background/bg_daily_walk', 'meta/icon_petal_hourglass', '7 Dawns'),
    'codex_half': ('background/bg_items_menu', 'ui/icon_items_codex', 'Half'),
    'codex_all': ('background/bg_items_menu', 'items/icon_golden_kintsugi_jar', 'Complete'),
}


def load(rel):
    path = os.path.join(ART, rel + '.png')
    if not os.path.isfile(path):
        sys.exit(f'missing art: {path}')
    return Image.open(path).convert('RGBA')


def square(painting):
    side = min(painting.size)
    left = (painting.width - side) // 2
    top = (painting.height - side) // 2
    return painting.crop((left, top, left + side, top + side)).resize((SIZE, SIZE), Image.LANCZOS)


def make(painting_rel, icon_rel, label):
    bg = square(load(painting_rel)).convert('RGB')  # Brightness on RGBA would dim the alpha too
    bg = ImageEnhance.Brightness(bg.filter(ImageFilter.GaussianBlur(1.5))).enhance(0.62).convert('RGBA')
    icon = load(icon_rel)
    icon.thumbnail((150, 150), Image.LANCZOS)
    x, y = (SIZE - icon.width) // 2, 30 + (150 - icon.height) // 2
    halo = Image.new('RGBA', bg.size, (0, 0, 0, 0))
    halo.paste(Image.new('RGBA', icon.size, (255, 230, 170, 140)), (x, y), icon)
    bg = Image.alpha_composite(bg, halo.filter(ImageFilter.GaussianBlur(9)))
    bg.alpha_composite(icon, (x, y))

    # A dark band under the label keeps the gold readable on bright paintings.
    band = Image.new('L', (1, SIZE), 0)
    for row in range(170, SIZE):
        band.putpixel((0, row), min(200, (row - 170) * 6))
    shade = Image.new('RGBA', bg.size, (0, 0, 0, 255))
    shade.putalpha(band.resize(bg.size))
    bg = Image.alpha_composite(bg, shade)

    draw = ImageDraw.Draw(bg)
    font = ImageFont.truetype(FONT, 27)
    while draw.textlength(label, font=font) > SIZE - 24 and font.size > 14:
        font = ImageFont.truetype(FONT, font.size - 1)
    width = draw.textlength(label, font=font)
    tx, ty = (SIZE - width) / 2, 204
    draw.text((tx + 1, ty + 2), label, font=font, fill=(0, 0, 0, 200))
    draw.text((tx, ty), label, font=font, fill=GOLD + (255,))
    draw.rectangle((2, 2, SIZE - 3, SIZE - 3), outline=GOLD + (255,), width=3)
    achieved = bg.convert('RGB')

    grey = ImageEnhance.Brightness(ImageOps.grayscale(achieved).convert('RGB')).enhance(0.8)
    ImageDraw.Draw(grey).rectangle((2, 2, SIZE - 3, SIZE - 3), outline=(150, 150, 150), width=3)
    return achieved, grey


def main(argv):
    parser = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    parser.add_argument('--only', nargs='*', help='API names to make (default: all)')
    args = parser.parse_args(argv)
    names = args.only or list(ICONS)
    unknown = [n for n in names if n not in ICONS]
    if unknown:
        sys.exit('unknown API names: ' + ', '.join(unknown))
    os.makedirs(OUT, exist_ok=True)
    for name in names:
        achieved, grey = make(*ICONS[name])
        achieved.save(os.path.join(OUT, f'achieved_{name}.jpg'), quality=92)
        grey.save(os.path.join(OUT, f'unachieved_{name}.jpg'), quality=92)
    print(f'{len(names)} icon pairs in {OUT}')


if __name__ == '__main__':
    main(sys.argv[1:])
