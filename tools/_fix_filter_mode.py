"""
Phase 5: FilterMode.Point audit
Finds all icon .meta files under Assets/Resources where filterMode is 1 (Bilinear) or 2 (Trilinear)
and corrects them to filterMode: 0 (Point/Nearest neighbour) for pixel-art crispness.
"""
import os
import re
import pathlib

root = str(pathlib.Path(__file__).resolve().parent.parent / 'Assets' / 'Resources')

changed = []
skipped = []

for dirpath, dirnames, filenames in os.walk(root):
    for fname in filenames:
        if not fname.endswith('.meta'):
            continue
        fpath = os.path.join(dirpath, fname)
        with open(fpath, 'r', encoding='utf-8', errors='replace') as f:
            text = f.read()
        # Only process texture meta files
        if 'TextureImporter' not in text:
            continue
        # Match filterMode value
        m = re.search(r'(    filterMode: )(\d)', text)
        if m and m.group(2) != '0':
            new_text = text[:m.start()] + m.group(1) + '0' + text[m.end():]
            with open(fpath, 'w', encoding='utf-8') as f:
                f.write(new_text)
            changed.append(os.path.relpath(fpath, root))
        else:
            skipped.append(fname)

print(f"Fixed filterMode -> 0 in {len(changed)} files:")
for p in changed:
    print(' ', p)
print(f"Already correct or non-texture: {len(skipped)} files")
