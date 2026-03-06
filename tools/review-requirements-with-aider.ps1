param(
    [string]$Model = "ollama/deepseek-coder:6.7b",
    [string]$InputGlob = "docs/*.md",
    [string]$OutputDir = "Tickets/ai-reviews"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-CommandAvailable {
    param([Parameter(Mandatory = $true)][string]$CommandName)
    $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

if (-not (Test-CommandAvailable -CommandName "aider")) {
    throw "Aider is not installed or not on PATH. Install with: .\\.venv\\Scripts\\python.exe -m pip install aider-chat"
}

if (-not (Test-Path -Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$inputFiles = Get-ChildItem -Path $InputGlob -File
if (-not $inputFiles -or $inputFiles.Count -eq 0) {
    throw "No files matched '$InputGlob'."
}

foreach ($file in $inputFiles) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $outFile = Join-Path $OutputDir ("{0}_aider_review.md" -f $baseName)

    Write-Host "Reviewing with Aider: $($file.FullName) -> $outFile"

    $message = @"
Review this requirement file and write a complete Markdown report to '$outFile'.

Tasks:
1. Requirement summary.
2. Ambiguities / unclear meanings.
3. Blind spots / missing requirements.
4. Edge and boundary cases.
5. Security and abuse cases.
6. Assumptions to validate.
7. Test scenario matrix.
8. Detailed test cases.

Test case format:
- ID
- Title
- Priority
- Preconditions
- Steps
- Expected Result

Use concise, actionable language.
"@

    # One-shot mode: read file and produce/update output without interactive chat.
    & aider --model $Model --yes --message $message $file.FullName $outFile

    if ($LASTEXITCODE -ne 0) {
        throw "Aider failed for '$($file.FullName)'."
    }
}

Write-Host "Done. Aider reviews written to '$OutputDir'."
