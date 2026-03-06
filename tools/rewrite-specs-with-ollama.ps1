param(
    [string]$Model = "deepseek-coder:6.7b",
    [string]$InputGlob = "docs/*.md",
    [string]$OutputDir = "Tickets/ai-rewrites",
    [switch]$AutoPullModel,
    [switch]$OverwriteInPlace,
    [switch]$SkipExisting,
    [int]$MaxChunkChars = 12000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-CommandAvailable {
    param([Parameter(Mandatory = $true)][string]$CommandName)
    $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Invoke-Ollama {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    if ($script:OllamaExecutable -eq "ollama") {
        & ollama @Arguments
    }
    else {
        & $script:OllamaExecutable @Arguments
    }
}

function Split-IntoChunks {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][int]$ChunkSize
    )

    if ($Text.Length -le $ChunkSize) {
        return @($Text)
    }

    $parts = @()
    $paragraphs = $Text -split "`r?`n`r?`n"
    $current = ""

    foreach ($p in $paragraphs) {
        $candidate = if ([string]::IsNullOrEmpty($current)) { $p } else { $current + "`r`n`r`n" + $p }
        if ($candidate.Length -gt $ChunkSize -and -not [string]::IsNullOrEmpty($current)) {
            $parts += $current
            $current = $p
        }
        else {
            $current = $candidate
        }
    }

    if (-not [string]::IsNullOrEmpty($current)) {
        $parts += $current
    }

    return $parts
}

$defaultOllamaExe = Join-Path $env:LOCALAPPDATA "Programs\Ollama\ollama.exe"
if (Test-CommandAvailable -CommandName "ollama") {
    $script:OllamaExecutable = "ollama"
}
elseif (Test-Path -Path $defaultOllamaExe) {
    $script:OllamaExecutable = $defaultOllamaExe
}
else {
    throw "Ollama is not available. Install from https://ollama.com/download or ensure '$defaultOllamaExe' exists."
}

$modelPresent = (Invoke-Ollama -Arguments @("list")) -match ("^" + [regex]::Escape($Model) + "\s")
if (-not $modelPresent) {
    if ($AutoPullModel) {
        Write-Host "Model '$Model' not found locally. Pulling now..."
        Invoke-Ollama -Arguments @("pull", $Model)
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pull model '$Model'."
        }
    }
    else {
        throw "Model '$Model' not found. Run: ollama pull $Model or pass -AutoPullModel"
    }
}

$inputFiles = @(Get-ChildItem -Path $InputGlob -File)
if (-not $inputFiles -or $inputFiles.Count -eq 0) {
    throw "No files matched '$InputGlob'."
}

if (-not $OverwriteInPlace -and -not (Test-Path -Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$rewriteGuidance = @"
You are a senior technical writer and game requirements editor.

Rewrite the given specification in clear, implementation-ready English while preserving intent.

Hard requirements:
1) Preserve all original functional meaning and constraints.
2) Do not invent new game mechanics or remove existing mechanics.
3) Resolve ambiguous wording where possible.
4) Keep and improve markdown structure with clear headings and bullet points.
5) Add a short "Assumptions/Clarifications" section only when the source is unclear.
6) Keep it concise but complete.
7) Preserve file-specific naming references and terms used by the project.

Output rules:
- Output markdown only.
- Do not include analysis commentary.
- Do not include code fences around the whole document.
"@

foreach ($file in $inputFiles) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    if ($OverwriteInPlace) {
        $outFile = $file.FullName
    }
    else {
        $outFile = Join-Path $OutputDir ("{0}_rewritten.md" -f $baseName)
        if ($SkipExisting -and (Test-Path -Path $outFile)) {
            Write-Host "Skipping existing rewritten file: $outFile"
            continue
        }
    }

    Write-Host "Rewriting $($file.FullName) -> $outFile"
    $sourceText = Get-Content -Path $file.FullName -Raw
    $chunks = Split-IntoChunks -Text $sourceText -ChunkSize $MaxChunkChars
    $chunkOutputs = @()
    $chunkIndex = 1

    foreach ($chunk in $chunks) {
        Write-Host "  - Chunk $chunkIndex/$($chunks.Count)"
        $prompt = @"
$rewriteGuidance

Project context:
- Project: Run of the Nine (Unity Sudoku roguelike)
- Source file: $($file.FullName)
- Chunk: $chunkIndex of $($chunks.Count)

Source markdown chunk:
---
$chunk
---
"@

        if ($script:OllamaExecutable -eq "ollama") {
            $rewrittenChunk = $prompt | & ollama run $Model
        }
        else {
            $rewrittenChunk = $prompt | & $script:OllamaExecutable run $Model
        }

        if ($LASTEXITCODE -ne 0) {
            throw "Model run failed for '$($file.FullName)' chunk $chunkIndex."
        }

        if ([string]::IsNullOrWhiteSpace($rewrittenChunk)) {
            throw "Model returned empty output for '$($file.FullName)' chunk $chunkIndex."
        }

        $chunkOutputs += $rewrittenChunk.Trim()
        $chunkIndex++
    }

    ($chunkOutputs -join "`r`n`r`n") | Set-Content -Path $outFile -Encoding utf8
}

if ($OverwriteInPlace) {
    Write-Host "Done. Rewrote $($inputFiles.Count) files in place."
}
else {
    Write-Host "Done. Rewritten files are in '$OutputDir'."
}
