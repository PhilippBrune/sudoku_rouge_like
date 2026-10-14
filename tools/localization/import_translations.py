"""Imports translated texts into a catalog after validating them.

    python tools/localization/import_translations.py fr [es pt] [--check]

Reads tools/localization/out/missing_<lang>.json (from export_missing.py) and
tools/localization/translations/<lang>.txt, one line per export row: "NNN|text" (NNN = row index).
Checks: every row translated exactly once, nothing empty, the same placeholders ({0}, {1:0.0}, ...) and rich-text
tags (<b>, <color=...>) as the source. Appends the new keys to Assets/Resources/Localization/<lang>.json in the
catalog's own format (4-space indent, '"key":  "value"'). --check validates without writing.
"""
import json, re, sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
CATALOGS = HERE.parents[1] / 'Assets' / 'Resources' / 'Localization'
BOM = b'\xef\xbb\xbf'
TOKEN = re.compile(r'\{\d+(?::[^}]*)?\}|</?[a-z]+(?:=[^>]*)?>')


def tokens(text):
    return Counter(TOKEN.findall(text or ''))


def read_translations(lang, count):
    rows = {}
    for n, line in enumerate((HERE / 'translations' / f'{lang}.txt').read_text(encoding='utf-8').splitlines(), 1):
        if not line.strip():
            continue
        idx, sep, text = line.partition('|')
        if not sep or not idx.isdigit():
            sys.exit(f'{lang}.txt:{n}: expected "NNN|text"')
        i = int(idx)
        if i in rows:
            sys.exit(f'{lang}.txt:{n}: row {i} twice')
        rows[i] = text
    missing = [i for i in range(count) if i not in rows]
    extra = [i for i in rows if i >= count]
    if missing or extra:
        sys.exit(f'{lang}: missing rows {missing[:10]}, unknown rows {extra[:10]}')
    return rows


def write_catalog(lang, additions):
    path = CATALOGS / f'{lang}.json'
    raw = path.read_bytes()
    bom = raw.startswith(BOM)
    text = raw.decode('utf-8-sig')
    body = text.rstrip()
    if not body.endswith('}'):
        sys.exit(f'{path}: unexpected format')
    lines = [f'    {json.dumps(k, ensure_ascii=False)}:  {json.dumps(v, ensure_ascii=False)}' for k, v in additions]
    new = body[:-1].rstrip() + ',\n' + ',\n'.join(lines) + '\n}\n'
    json.loads(new)  # must stay valid JSON
    path.write_bytes((BOM if bom else b'') + new.encode('utf-8'))


def main(args):
    check = '--check' in args
    langs = [a for a in args if not a.startswith('--')] or ['fr', 'es', 'pt']
    for lang in langs:
        export = json.loads((HERE / 'out' / f'missing_{lang}.json').read_text(encoding='utf-8'))
        rows = read_translations(lang, len(export))
        catalog = json.loads((CATALOGS / f'{lang}.json').read_text(encoding='utf-8-sig'))
        problems, additions = [], []
        for i, row in enumerate(export):
            text = rows[i]
            source = row['en'] if row['en'] is not None else row['de']
            if not text.strip():
                problems.append(f'{i:03} {row["key"]}: empty')
            if tokens(text) != tokens(source):
                problems.append(f'{i:03} {row["key"]}: placeholders {dict(tokens(text))} vs source {dict(tokens(source))}')
            if row['key'] in catalog:
                problems.append(f'{i:03} {row["key"]}: already in {lang}.json')
            additions.append((row['key'], text))
        if problems:
            print(f'{lang}: {len(problems)} problem(s)')
            for p in problems:
                print('  ' + p)
            sys.exit(1)
        if check:
            print(f'{lang}: {len(additions)} rows OK (check only)')
            continue
        write_catalog(lang, additions)
        print(f'{lang}: imported {len(additions)} keys into {lang}.json')


if __name__ == '__main__':
    main(sys.argv[1:])
