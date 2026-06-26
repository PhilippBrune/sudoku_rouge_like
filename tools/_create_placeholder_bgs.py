import struct, zlib, os, pathlib

_root = pathlib.Path(__file__).resolve().parent.parent

def make_png(path, r, g, b, w=8, h=8):
    def chunk(tag, data):
        c = zlib.crc32(tag + data) & 0xffffffff
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', c)
    raw = b''.join(b'\x00' + bytes([r, g, b] * w) for _ in range(h))
    idat = zlib.compress(raw)
    png  = b'\x89PNG\r\n\x1a\n'
    png += chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0))
    png += chunk(b'IDAT', idat)
    png += chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(png)

bg_base = str(_root / 'Assets' / 'Resources' / 'background')
bg_files = [
    ('bg_daily_walk.png',     40, 55, 35),
    ('bg_monthly_walk.png',   30, 45, 60),
    ('bg_profile_select.png', 35, 35, 50),
]
for name, r, g, b in bg_files:
    make_png(os.path.join(bg_base, name), r, g, b)
    print('Created', name)

ui_base = str(_root / 'Assets' / 'Resources' / 'ui')
make_png(os.path.join(ui_base, 'icon_start_game.png'), 180, 60, 20)
print('Created icon_start_game.png')

print('Done')
