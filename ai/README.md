# Local AI Requirement Review Workflow (Windows)

This repo now includes a local, Windows-friendly AI flow for:
- reviewing requirement documents,
- finding blind spots and unclear wording,
- generating test scenarios and detailed test cases.

## Stack

- Local runtime: `Ollama`
- Model (default): `deepseek-coder:6.7b`
- Automation:
	- `tools/review-requirements-with-ollama.ps1` (direct Ollama call)
	- `tools/review-requirements-with-aider.ps1` (Aider + Ollama model)
- Output location: `Tickets/ai-reviews`

## 1) Install Ollama

- Download: https://ollama.com/download
- Verify:

```powershell
ollama --version
```

## 2) Pull a model

```powershell
ollama pull deepseek-coder:6.7b
```

Optional alternatives for stronger requirement reasoning:

```powershell
ollama pull deepseek-r1:8b
ollama pull qwen2.5-coder:7b
```

## 3) Run requirement review

From repo root:

```powershell
./tools/review-requirements-with-ollama.ps1
```

By default it scans `docs/*.md` and writes per-file reviews into `Tickets/ai-reviews`.

On Windows, the script auto-detects Ollama at `%LOCALAPPDATA%\Programs\Ollama\ollama.exe` if `ollama` is not on `PATH`.

If you prefer your Aider workflow:

```powershell
.\.venv\Scripts\python.exe -m pip install aider-chat
./tools/review-requirements-with-aider.ps1
```

## 4) Common variants

Review only requirement files in a dedicated folder:

```powershell
./tools/review-requirements-with-ollama.ps1 -InputGlob "requirements/*.md"
```

Switch model:

```powershell
./tools/review-requirements-with-ollama.ps1 -Model "deepseek-r1:8b"
```

Auto-pull missing model:

```powershell
./tools/review-requirements-with-ollama.ps1 -Model "qwen2.5-coder:7b" -AutoPullModel
```

Rewrite all spec markdown files (safe output folder, no in-place overwrite):

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
./tools/rewrite-specs-with-ollama.ps1 -InputGlob "docs/*.md" -AutoPullModel -SkipExisting
```

Rewritten files are created in `Tickets/ai-rewrites` as `*_rewritten.md`.

## Output sections

Each generated review includes:
1. Requirement Summary
2. Ambiguities / Unclear Meanings
3. Blind Spots / Missing Requirements
4. Edge and Boundary Cases
5. Security and Abuse Cases
6. Assumptions to Validate
7. Test Scenario Matrix
8. Detailed Test Cases

## Notes

- For large docs, split into smaller domain files for better output quality.
- Keep requirement files specific and testable.
- Re-run after requirement updates and keep generated outputs under `Tickets/ai-reviews` for traceability.
