param(
    [string]$Root = (Resolve-Path ".").Path,
    [switch]$IncludeMeta,
    [switch]$ExpectRetired,
    [switch]$ExpectNestedRoot
)

$nestedRoot = Join-Path $Root "Assets/Scripts/My project/Assets/Scripts"
$topRoot = Join-Path $Root "Assets/Scripts"
$nestedProjectVersion = Join-Path $Root "Assets/Scripts/My project/ProjectSettings/ProjectVersion.txt"

$hasTop = Test-Path $topRoot
$hasNested = Test-Path $nestedRoot
$hasNestedProject = Test-Path $nestedProjectVersion

if ($ExpectRetired -and $ExpectNestedRoot) {
    Write-Error "Choose only one expectation mode: -ExpectRetired or -ExpectNestedRoot."
    exit 1
}

if (-not $hasTop) {
    Write-Error "Top-level script root not found: '$topRoot'."
    exit 1
}

if ($ExpectNestedRoot) {
    if (-not $hasNestedProject) {
        Write-Error "Nested Unity project root is missing expected project settings: '$nestedProjectVersion'."
        exit 2
    }

    if (-not $hasNested) {
        Write-Error "Nested script root not found but nested root mode is required: '$nestedRoot'."
        exit 2
    }

    Write-Host "Nested script root is present as expected for active Unity project mode."
    exit 0
}

if ($ExpectRetired) {
    if ($hasNestedProject) {
        Write-Host "Nested Unity project root detected; skipping retired check in this workspace layout."
        exit 0
    }

    if ($hasNested) {
        Write-Error "Nested script tree is still present but retired mode is required: '$nestedRoot'."
        exit 2
    }

    Write-Host "Nested script tree is retired as expected."
    exit 0
}

if (-not $hasNested) {
    Write-Error "Nested script root not found in drift-compare mode: '$nestedRoot'."
    exit 1
}

$extensions = @("*.cs")
if ($IncludeMeta) {
    $extensions += "*.cs.meta"
}

function Get-HashMap([string]$basePath, [string[]]$patterns) {
    $map = @{}
    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $basePath -Recurse -File -Filter $pattern | ForEach-Object {
            $rel = $_.FullName.Substring($basePath.Length)
            if ($rel.StartsWith("\\") -or $rel.StartsWith("/")) {
                $rel = $rel.Substring(1)
            }
            $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash
            $map[$rel] = $hash
        }
    }
    return $map
}

$top = Get-HashMap $topRoot $extensions
$nested = Get-HashMap $nestedRoot $extensions

$missingInTop = @()
$missingInNested = @()
$different = @()

foreach ($rel in $nested.Keys) {
    if (-not $top.ContainsKey($rel)) {
        $missingInTop += $rel
        continue
    }

    if ($top[$rel] -ne $nested[$rel]) {
        $different += $rel
    }
}

foreach ($rel in $top.Keys) {
    if (-not $nested.ContainsKey($rel)) {
        $missingInNested += $rel
    }
}

Write-Host "Script tree drift report"
Write-Host "- Missing in top-level: $($missingInTop.Count)"
Write-Host "- Missing in nested: $($missingInNested.Count)"
Write-Host "- Content differs: $($different.Count)"

if ($missingInTop.Count -gt 0) {
    Write-Host "`nMissing in top-level:"
    $missingInTop | Sort-Object | ForEach-Object { Write-Host "  $_" }
}

if ($missingInNested.Count -gt 0) {
    Write-Host "`nMissing in nested:"
    $missingInNested | Sort-Object | ForEach-Object { Write-Host "  $_" }
}

if ($different.Count -gt 0) {
    Write-Host "`nDifferent content:"
    $different | Sort-Object | ForEach-Object { Write-Host "  $_" }
}

if ($missingInTop.Count -eq 0 -and $missingInNested.Count -eq 0 -and $different.Count -eq 0) {
    Write-Host "`nNo drift detected."
    exit 0
}

exit 2
