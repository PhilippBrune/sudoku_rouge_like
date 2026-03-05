param(
    [string]$Root = (Resolve-Path ".").Path,
    [switch]$PurgeUnityCache,
    [switch]$HardResetScripts
)

$projectRoot = Join-Path $Root "Assets/Scripts/My project"
$loopFolder = Join-Path $projectRoot "Assets/Scripts/My project"
$nestedAssetsRoot = Join-Path $projectRoot "Assets"
$nestedScriptsRoot = Join-Path $projectRoot "Assets/Scripts"
$canonicalScriptsRoot = Join-Path $Root "Assets/Scripts"

if (-not (Test-Path $projectRoot)) {
    Write-Error "Project root not found: $projectRoot"
    exit 1
}

Write-Host "Project root: $projectRoot"

# Ensure Unity/Hub is not holding file locks.
Get-Process -Name Unity, UnityHub -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400

function Remove-LoopFolderUsingSubst {
    param(
        [string]$AssetsRoot,
        [string]$TargetLoopFolder
    )

    $drive = "Z:"
    cmd /c "subst $drive /d" | Out-Null
    cmd /c "subst $drive `"$AssetsRoot`""
    try {
        # Deletes Assets/Scripts/My project through a short path.
        cmd /c "rd /s /q `"$drive\Scripts\My project`""
    }
    finally {
        cmd /c "subst $drive /d" | Out-Null
    }

    return (-not (Test-Path $TargetLoopFolder))
}

function Rebuild-NestedScripts {
    param(
        [string]$AssetsRoot,
        [string]$NestedScripts,
        [string]$CanonicalScripts
    )

    Write-Host "Hard reset enabled: rebuilding nested Assets/Scripts..."
    $drive = "Y:"
    cmd /c "subst $drive /d" | Out-Null
    cmd /c "subst $drive `"$AssetsRoot`""
    try {
        cmd /c "rd /s /q `"$drive\Scripts`""
    }
    finally {
        cmd /c "subst $drive /d" | Out-Null
    }

    New-Item -ItemType Directory -Path $NestedScripts -Force | Out-Null

    # Copy canonical scripts but skip the nested Unity project folder to avoid recursion.
    Get-ChildItem -Path $CanonicalScripts -Force | Where-Object { $_.Name -ne "My project" } | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $NestedScripts -Recurse -Force
    }
}

if (Test-Path $loopFolder) {
    Write-Host "Removing recursive loop folder: $loopFolder"

    $removed = Remove-LoopFolderUsingSubst -AssetsRoot $nestedAssetsRoot -TargetLoopFolder $loopFolder
    if (-not $removed) {
        Write-Host "Primary removal failed. Trying ownership/ACL fix + retry..."
        cmd /c "takeown /f `"$loopFolder`" /r /a" | Out-Null
        cmd /c "icacls `"$loopFolder`" /grant %USERNAME%:F /t /c" | Out-Null
        $removed = Remove-LoopFolderUsingSubst -AssetsRoot $nestedAssetsRoot -TargetLoopFolder $loopFolder
    }

    if ((-not $removed) -and $HardResetScripts) {
        Rebuild-NestedScripts -AssetsRoot $nestedAssetsRoot -NestedScripts $nestedScriptsRoot -CanonicalScripts $canonicalScriptsRoot
    }

    if (Test-Path $loopFolder) {
        Write-Error "Recursive loop folder still exists: $loopFolder. Re-run with -HardResetScripts."
        exit 2
    }
}
else {
    Write-Host "Recursive loop folder not found."
}

if ($PurgeUnityCache) {
    Write-Host "Purging Unity cache folders (Library, Temp, Logs)..."
    $cacheFolders = @("Library", "Temp", "Logs")
    foreach ($name in $cacheFolders) {
        $path = Join-Path $projectRoot $name
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "Recovery script finished."
Write-Host "Now reopen Unity with: $projectRoot"
