param(
    [string]$Model = "deepseek-coder:6.7b",
    [string]$InputGlob = "docs/*.md",
    [string]$OutputDir = "Tickets/ai-reviews",
    [switch]$AutoPullModel
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

if (-not (Test-Path -Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
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

$systemGuidance = @"
You are a senior QA and requirements engineer.
Your goals:
1) Review requirement clarity and correctness.
2) Detect blind spots and ambiguities.
3) Generate test scenarios and concrete test cases.

Output format requirements:
- Use Markdown.
- Be specific and actionable.
- Include sections in this exact order:
  1. Requirement Summary
  2. Ambiguities / Unclear Meanings
  3. Blind Spots / Missing Requirements
  4. Edge and Boundary Cases
  5. Security and Abuse Cases
  6. Assumptions to Validate
  7. Test Scenario Matrix
  8. Detailed Test Cases

Rules for test cases:
- Each test case must have: ID, Title, Priority, Preconditions, Steps, Expected Result.
- Include positive, negative, edge, and failure-path coverage.
- Add at least one non-functional test case when relevant (performance, reliability, accessibility, etc.).
"@

foreach ($file in $inputFiles) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $outFile = Join-Path $OutputDir ("{0}_review.md" -f $baseName)

    Write-Host "Reviewing $($file.FullName) -> $outFile"

    $requirementText = Get-Content -Path $file.FullName -Raw

    $prompt = @"
$systemGuidance

Repository context:
- Project: Run of the Nine (Unity Sudoku roguelike)
- Requirement source file: $($file.FullName)

Requirement document content:
---
$requirementText
---
"@

    if ($script:OllamaExecutable -eq "ollama") {
        $analysis = $prompt | & ollama run $Model
    }
    else {
        $analysis = $prompt | & $script:OllamaExecutable run $Model
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Model run failed for '$($file.FullName)'."
    }

    $header = @"
# AI Requirement Review

- Source: $($file.FullName)
- Model: $Model
- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

"@

    ($header + $analysis) | Set-Content -Path $outFile -Encoding utf8
}

Write-Host "Done. Reviews written to '$OutputDir'."
