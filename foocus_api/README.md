# Foocus Icon Generator

This folder contains a script that:
- reads `docs/run_of_the_nine_icon_prompt_guide.md`
- extracts icon/frame filenames and only uses text placed after a `Prompt` line
- validates naming convention (`icon_...png` or `frame_...png`)
- calls a Fooocus-compatible API
- saves generated PNGs into `docs/icons_foocus`

The script forces Fooocus style selection to exactly `Sai Pixel Art`.
It also writes detailed JSONL logs to `foocus_api/generation_log.jsonl` by default.

## Files

- `generate_icons.py`: main generator script
- `requirements.txt`: Python dependency list

## Install

```powershell
pip install -r foocus_api/requirements.txt
```

## Verify parsing first

```powershell
python foocus_api/generate_icons.py --list-only
```

## Generate all icons

```powershell
python foocus_api/generate_icons.py --api-url http://127.0.0.1:7865 --overwrite
```

Optional custom log path:

```powershell
python foocus_api/generate_icons.py --api-url http://127.0.0.1:7865 --overwrite --log-file foocus_api/my_run_log.jsonl
```

If your API runs on a different address, replace `--api-url` with your server URL.

## Notes

- By default, generated files are picked from `%TEMP%\\fooocus` (Fooocus temp output path).
- The script tries several common Fooocus API endpoints automatically.
- Output is written directly to `docs/icons_foocus`.
- If files already exist, they are skipped unless you use `--overwrite`.
