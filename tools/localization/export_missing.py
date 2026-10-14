"""Exports the localization keys a language is missing compared with German, with the English source text.

    python tools/localization/export_missing.py [fr es pt]

Writes tools/localization/out/missing_<lang>.json: [{key, en, de, category, placeholders}].

English source, in order of preference:
  1. tools/localization/english_sources.json, dumped at runtime by the [Explicit] EditMode test
     TranslationSourceExportDiagnostics (covers keys built at runtime, such as Item.Description.{type});
  2. literal pairs in the C# source: "Key", "English text";
  3. raw-string keys, whose key is the English text itself.
Keys without English get "en": null and are translated from the German reference.
"""
import json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOGS = ROOT / 'Assets' / 'Resources' / 'Localization'
OUT = Path(__file__).resolve().parent / 'out'
RUNTIME_SOURCES = Path(__file__).resolve().parent / 'english_sources.json'
PLACEHOLDER = re.compile(r'\{\d+(?::[^}]*)?\}|<[^>]+>')
DOTTED_KEY = re.compile(r'^[A-Z][A-Za-z0-9]*(\.[A-Za-z0-9]+)+$')
PAIR = re.compile(r'"((?:[^"\\]|\\.)+)"\s*,\s*\$?@?"((?:[^"\\]|\\.)*)"')


def load(lang):
    return json.loads((CATALOGS / f'{lang}.json').read_text(encoding='utf-8-sig'))


def unescape(s):
    return bytes(s, 'utf-8').decode('unicode_escape').encode('latin-1').decode('utf-8') if '\\' in s else s


def static_sources(keys):
    found = {}
    for path in (ROOT / 'Assets' / 'Scripts').rglob('*.cs'):
        rel = path.as_posix()
        if '/Tests/' in rel or path.name == 'LocalizationService.cs':
            continue
        text = path.read_text(encoding='utf-8-sig')
        for m in PAIR.finditer(text):
            k = m.group(1)
            if k in keys and k not in found:
                try:
                    found[k] = unescape(m.group(2))
                except UnicodeError:
                    found[k] = m.group(2)
    return found


def english_for(keys):
    runtime = json.loads(RUNTIME_SOURCES.read_text(encoding='utf-8')) if RUNTIME_SOURCES.exists() else {}
    static = static_sources(set(keys))
    out = {}
    for k in keys:
        if k in runtime:
            out[k] = runtime[k]
        elif k in static:
            out[k] = static[k]
        elif not DOTTED_KEY.match(k):
            out[k] = k
        else:
            out[k] = None
    return out


def main(langs):
    de = load('de')
    OUT.mkdir(exist_ok=True)
    for lang in langs:
        cat = load(lang)
        missing = [k for k in de if k not in cat]
        en = english_for(missing)
        rows = [{'key': k, 'en': en[k], 'de': de[k],
                 'category': k.split('.')[0] if DOTTED_KEY.match(k) else 'UI',
                 'placeholders': sorted(set(PLACEHOLDER.findall(de[k])))} for k in missing]
        (OUT / f'missing_{lang}.json').write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding='utf-8')
        no_en = sum(1 for r in rows if r['en'] is None)
        print(f'{lang}: {len(rows)} missing, {len(rows) - no_en} with English source, {no_en} German-only -> {OUT / f"missing_{lang}.json"}')


if __name__ == '__main__':
    main(sys.argv[1:] or ['fr', 'es', 'pt'])
