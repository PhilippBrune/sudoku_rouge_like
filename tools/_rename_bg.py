import os, pathlib

d = str(pathlib.Path(__file__).resolve().parent.parent / 'Assets' / 'Resources' / 'background')
pairs = [
    ('bg_Shrine_Garden', 'bg_shrine_garden'),
    ('ui_class_select', 'bg_class_select'),
    ('ui_game_modes', 'bg_game_modes'),
    ('ui_items_menu', 'bg_items_menu'),
    ('ui_main_menu', 'bg_main_menu'),
    ('ui_meta_progression', 'bg_meta_progression'),
    ('ui_options', 'bg_options'),
    ('ui_tutorial_progress', 'bg_tutorial_progress'),
    ('ui_tutorial_setup', 'bg_tutorial_setup'),
]
for old, new in pairs:
    for ext in ['.png', '.png.meta']:
        src = os.path.join(d, old + ext)
        dst = os.path.join(d, new + ext)
        if os.path.exists(src):
            os.rename(src, dst)
            print(f'OK: {old+ext} -> {new+ext}')
        else:
            print(f'SKIP: {old+ext}')
print('Done')
