"""Builds the Steamworks entry sheet for the achievements of docs/Achievements.md: one local HTML page with each new
achievement's two icons, API name and texts in five languages (copy buttons), and one overview image of all icons.

Usage: python tools/steam/achievement_sheet.py
Writes steam_assets/achievements/steamworks_entry.html and steam_assets/achievements/new_achievements_overview.jpg.
"""
import html
import os
import re

from PIL import Image

import achievement_icons as icons

SPEC = os.path.join(icons.ROOT, 'docs', 'Achievements.md')
LANGS = ['English', 'Deutsch', 'Français', 'Español', 'Português (Brasil)']


def read_spec():
    """English name and description from the section 2 tables, the other languages from section 5."""
    text = open(SPEC, encoding='utf-8').read()
    english = {}
    for row in re.finditer(r'^\| `(\w+)` \| ([^|]+) \| ([^|]+) \|', text, re.M):
        english.setdefault(row.group(1), (row.group(2).strip(), row.group(3).strip()))
    others = {}
    section5 = text[text.index('## 5.'):]
    for row in re.finditer(r'^\| `(\w+)` \| (.+) \|$', section5, re.M):
        cells = [c.strip() for c in row.group(2).split(' | ')]
        others[row.group(1)] = [name_and_description(cell) for cell in cells]
    return english, others


def name_and_description(cell):
    name, description = (part.strip() for part in cell.split(':', 1))
    return name, description[:1].upper() + description[1:]


def page(english, others):
    rows = []
    for number, name in enumerate(icons.ICONS, 1):
        texts = [english[name]] + others[name]
        cells = ''.join(
            f'<td><b>{lang}</b><br>{copy(t[0])}<br>{copy(t[1])}</td>' for lang, t in zip(LANGS, texts))
        rows.append(
            f'<tr><td class="n">{number}</td>'
            f'<td><img src="achieved_{name}.jpg"><img src="unachieved_{name}.jpg"></td>'
            f'<td>{copy(name)}<br><small>achieved_{name}.jpg<br>unachieved_{name}.jpg</small></td>{cells}</tr>')
    return TEMPLATE.replace('{rows}', '\n'.join(rows)).replace('{count}', str(len(icons.ICONS)))


def copy(value):
    escaped = html.escape(value)
    return f'<button class="c" onclick="navigator.clipboard.writeText(this.dataset.v)" data-v="{escaped}">{escaped}</button>'


def overview():
    names = list(icons.ICONS)
    cols = 7
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * 132, rows * 132), (24, 22, 20))
    for i, name in enumerate(names):
        tile = Image.open(os.path.join(icons.OUT, f'achieved_{name}.jpg')).resize((128, 128), Image.LANCZOS)
        sheet.paste(tile, ((i % cols) * 132 + 2, (i // cols) * 132 + 2))
    return sheet


TEMPLATE = """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Run of the Nine: achievements for Steamworks</title>
<style>
body { font-family: Segoe UI, sans-serif; background: #f4efe4; color: #2b2520; margin: 16px; }
table { border-collapse: collapse; width: 100%; font-size: 13px; }
td { border-bottom: 1px solid #d6cbb5; padding: 6px; vertical-align: top; }
td.n { color: #8a7d68; }
img { width: 64px; height: 64px; margin-right: 4px; }
button.c { border: 1px solid #c8b89a; background: #fffaf0; padding: 2px 6px; margin: 2px 0; cursor: copy; text-align: left; font: inherit; }
button.c:active { background: #e8d8b0; }
</style></head><body>
<h1>Achievements for Steamworks ({count} new)</h1>
<p>Steamworks, App 4858470: Stats &amp; Achievements, Achievements, New Achievement. For each row: the API name, the
English name and description, Set By: Client, Hidden: no, the two icons from this folder. Then each language under the
achievement's localization. A click on a field copies it. Source: docs/Achievements.md (made by tools/steam/achievement_sheet.py).</p>
<table>
{rows}
</table></body></html>
"""


def main():
    english, others = read_spec()
    missing = [n for n in icons.ICONS if n not in english or n not in others]
    if missing:
        raise SystemExit('not in docs/Achievements.md: ' + ', '.join(missing))
    with open(os.path.join(icons.OUT, 'steamworks_entry.html'), 'w', encoding='utf-8') as f:
        f.write(page(english, others))
    overview().save(os.path.join(icons.OUT, 'new_achievements_overview.jpg'), quality=88)
    print(f'{len(icons.ICONS)} achievements in steamworks_entry.html and new_achievements_overview.jpg')


if __name__ == '__main__':
    main()
