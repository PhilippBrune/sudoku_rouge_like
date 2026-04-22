import os, shutil

base = r'C:\Users\Philipp\Documents\sudoku_rouge_like\Assets\Resources'
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
