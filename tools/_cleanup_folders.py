import os, shutil, pathlib

base = str(pathlib.Path(__file__).resolve().parent.parent / 'Assets' / 'Resources')
for d in ['Icons', 'backup_icons', 'review']:
    p = os.path.join(base, d)
    if os.path.isdir(p):
        contents = [f for f in os.listdir(p) if not f.endswith('.meta')]
        if not contents:
            shutil.rmtree(p)
            mp = p + '.meta'
            if os.path.exists(mp):
                os.remove(mp)
            print(f'Deleted: {d}')
        else:
            print(f'Skipped {d}: non-empty {contents}')
    else:
        print(f'Not found: {d}')
