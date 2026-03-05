param(
    [string]$Root = (Resolve-Path ".").Path,
    [string]$ProjectName = "My project_clean"
)

$projectRoot = Join-Path $Root ("Assets/Scripts/" + $ProjectName)
$nestedScriptsRoot = Join-Path $projectRoot "Assets/Scripts"
$retiredScriptsRoot = Join-Path $Root "retired/nested-script-tree/Scripts"

if (-not (Test-Path $projectRoot)) {
    Write-Error "Project root not found: $projectRoot"
    exit 1
}

if (-not (Test-Path $nestedScriptsRoot)) {
    Write-Error "Nested scripts root not found: $nestedScriptsRoot"
    exit 1
}

if (-not (Test-Path $retiredScriptsRoot)) {
    Write-Error "Retired scripts root not found: $retiredScriptsRoot"
    exit 1
}

# Stop Unity/Hub to release locks.
Get-Process -Name Unity, UnityHub -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

Write-Host "Repairing script GUID metadata using retired script tree..."
$copiedMeta = 0
$missingMeta = 0

Get-ChildItem -Path $nestedScriptsRoot -Recurse -Filter "*.cs" -File | ForEach-Object {
    $rel = $_.FullName.Substring($nestedScriptsRoot.Length)
    $rel = $rel -replace '^[\\/]+', ''
    $retiredMeta = Join-Path $retiredScriptsRoot ($rel + ".meta")
    $targetMeta = $_.FullName + ".meta"

    if (Test-Path $retiredMeta) {
        Copy-Item -Path $retiredMeta -Destination $targetMeta -Force
        $copiedMeta++
    }
    else {
        $missingMeta++
    }
}

Write-Host "Copied script metas: $copiedMeta"
Write-Host "No retired meta match: $missingMeta"

Write-Host "Resetting Unity cache (Library/Temp/Logs)..."
foreach ($folder in @("Library", "Temp", "Logs")) {
    $path = Join-Path $projectRoot $folder
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Repair complete."
Write-Host "Open Unity with: $projectRoot"
Write-Host "Then run: Assets > Reimport All (once), and save modified scenes/prefabs."
