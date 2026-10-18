# Quick compile check without Unity (about 20 s): copies the Unity-generated csproj files, adds .cs files Unity has
# not imported yet, points the test projects at the copy, runs `dotnet build` and prints the errors.
# Usage: python tools/quick_compile.py [Game|GameTests.EditMode|GameTests.PlayMode]   (exit code = dotnet's)
import os, re, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDERS = {'Game': r'Assets\Scripts', 'GameTests.EditMode': r'Assets\Scripts\Tests\EditMode',
           'GameTests.PlayMode': r'Assets\Scripts\Tests\PlayMode'}


def write_check(name):
    text = open(os.path.join(ROOT, name + '.csproj'), encoding='utf-8-sig').read()
    listed = set(m.lower() for m in re.findall(r'<Compile Include="([^"]+)"', text))
    skip = ('\\editor\\', '\\tests\\') if name == 'Game' else ()
    extra = []
    for dirpath, _, files in os.walk(os.path.join(ROOT, FOLDERS[name])):
        for f in files:
            rel = os.path.relpath(os.path.join(dirpath, f), ROOT)
            if f.endswith('.cs') and not any(s in '\\' + rel.lower() for s in skip) and rel.lower() not in listed:
                extra.append(rel)
    anchor = text.index('<Compile Include=')
    text = text[:anchor] + ''.join(f'<Compile Include="{e}" />\n    ' for e in extra) + text[anchor:]
    text = text.replace('Include="Game.csproj"', 'Include="Game.check.csproj"')
    path = os.path.join(ROOT, name + '.check.csproj')
    open(path, 'w', encoding='utf-8').write(text)
    return path, extra


def main(name):
    if name not in FOLDERS:
        sys.exit('usage: quick_compile.py [' + '|'.join(FOLDERS) + ']')
    made = []
    try:
        made.append(write_check('Game'))
        if name != 'Game':
            made.append(write_check(name))
        out = subprocess.run(['dotnet', 'build', made[-1][0], '-nologo', '-v', 'q',
                              '-p:OutputPath=' + os.path.join(os.environ.get('TEMP', ROOT), 'sr_check_out') + os.sep],
                             capture_output=True, text=True, encoding='utf-8', errors='replace', cwd=ROOT, timeout=600)
        errors = sorted(set(line.strip() for line in out.stdout.splitlines() if ' error ' in line))
        print(name, 'added:', ', '.join(e for _, extra in made for e in extra) or '-')
        if errors:
            print('\n'.join(errors[:40]))
        elif out.returncode != 0:
            print('dotnet build failed without compiler errors:\n' + '\n'.join((out.stdout + out.stderr).splitlines()[-20:]))
        else:
            print('compiled without errors')
        return out.returncode
    finally:
        for path, _ in made:
            try:
                os.remove(path)
            except FileNotFoundError:
                pass


if __name__ == '__main__':
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else 'Game'))
