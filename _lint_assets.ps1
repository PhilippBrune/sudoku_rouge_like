<#
.SYNOPSIS
    Asset lint tool for Run of the Nine.

.DESCRIPTION
    Checks for:
      1. PNG files under Assets/Resources whose filenames contain uppercase letters
         (violates the snake_case naming convention).
      2. PNG filenames that don't start with the expected prefix for their folder:
           background/ -> bg_
           class|items|relic|modifier|cursed|legendary|economy|meta|node|ui -> icon_
      3. PNG files missing a paired .meta file.
      4. Resources.Load<> string literals in C# files that reference paths with no
         matching PNG under Assets/Resources.

.NOTES
    Run from the project root:   .\_lint_assets.ps1
    Use -Verbose for extra detail.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$root      = $PSScriptRoot
$resRoot   = Join-Path $root 'Assets\Resources'
$scriptsDir= Join-Path $root 'Assets\Scripts'

$issues = [System.Collections.Generic.List[string]]::new()

function Add-Issue([string]$msg) {
    $issues.Add($msg)
    Write-Warning $msg
}

# ── 1. Uppercase PNG filenames ────────────────────────────────────────────────
Write-Verbose "Checking for uppercase PNG filenames..."
Get-ChildItem -Path $resRoot -Recurse -Filter '*.png' | ForEach-Object {
    $name = $_.Name
    if ($name -cmatch '[A-Z]') {
        Add-Issue "UPPERCASE: $($_.FullName.Replace($resRoot + '\', ''))"
    }
}

# ── 2. Prefix convention ──────────────────────────────────────────────────────
Write-Verbose "Checking filename prefix conventions..."
Get-ChildItem -Path $resRoot -Recurse -Filter '*.png' | ForEach-Object {
    $rel    = $_.FullName.Replace($resRoot + '\', '')
    $folder = ($rel -split '\\')[0]
    $name   = $_.Name
    switch ($folder) {
        'background' {
            if (-not $name.StartsWith('bg_')) {
                Add-Issue "BAD PREFIX (expected bg_): $rel"
            }
        }
        { $_ -in 'class','items','relic','modifier','cursed','legendary','economy','meta','node','ui' } {
            if (-not $name.StartsWith('icon_')) {
                Add-Issue "BAD PREFIX (expected icon_): $rel"
            }
        }
    }
}

# ── 3. Missing .meta files ────────────────────────────────────────────────────
Write-Verbose "Checking for PNG files missing .meta..."
Get-ChildItem -Path $resRoot -Recurse -Filter '*.png' | ForEach-Object {
    $meta = $_.FullName + '.meta'
    if (-not (Test-Path $meta)) {
        Add-Issue "MISSING META: $($_.FullName.Replace($resRoot + '\', ''))"
    }
}

# ── 4. Dangling Resources.Load paths ─────────────────────────────────────────
Write-Verbose "Scanning C# files for Resources.Load string literals..."
$loadPattern = 'Resources\.Load(?:<[^>]+>)?\s*\(\s*"([^"]+)"'
$danglingCount = 0

Get-ChildItem -Path $scriptsDir -Recurse -Filter '*.cs' | ForEach-Object {
    $csFile = $_.FullName
    $lines  = Get-Content $csFile
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $matches2 = [regex]::Matches($line, $loadPattern)
        foreach ($m in $matches2) {
            $resPath = $m.Groups[1].Value
            # Skip non-asset paths (scenes, etc.)
            if ($resPath -match '^(LegacyRuntime|Arial|Resources)') { continue }

            # Try .png extension first, then without
            $candidates = @(
                (Join-Path $resRoot "$resPath.png"),
                (Join-Path $resRoot $resPath)
            )
            $found = $candidates | Where-Object { Test-Path $_ }
            if (-not $found) {
                $rel = $csFile.Replace($root + '\', '')
                Add-Issue "DANGLING LOAD: `"$resPath`" in $rel`:$($i+1)"
                $danglingCount++
            }
        }
    }
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
if ($issues.Count -eq 0) {
    Write-Host "[LINT] All checks passed. No issues found." -ForegroundColor Green
} else {
    Write-Host "[LINT] $($issues.Count) issue(s) found:" -ForegroundColor Yellow
    $issues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    exit 1
}
