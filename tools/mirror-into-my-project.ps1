param(
    [string]$Root = (Resolve-Path ".").Path,
    [string]$NestedRoot = "Assets/Scripts/My project"
)

$dest = Join-Path $Root $NestedRoot
if (-not (Test-Path $dest)) {
    Write-Error "Nested root not found: $dest"
    exit 1
}

function Copy-Tree {
    param(
        [string]$Source,
        [string]$Target
    )

    if (-not (Test-Path $Source)) {
        return
    }

    New-Item -ItemType Directory -Path $Target -Force | Out-Null
    Copy-Item -Path (Join-Path $Source "*") -Destination $Target -Recurse -Force
}

function Copy-ScriptsExcludingNestedProject {
    param(
        [string]$Source,
        [string]$Target
    )

    if (-not (Test-Path $Source)) {
        return
    }

    New-Item -ItemType Directory -Path $Target -Force | Out-Null

    Get-ChildItem -Path $Source -Force | Where-Object { $_.Name -ne "My project" } | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $Target -Recurse -Force
    }
}

# Unity content
Copy-ScriptsExcludingNestedProject -Source (Join-Path $Root "Assets/Scripts") -Target (Join-Path $dest "Assets/Scripts")
Copy-Tree -Source (Join-Path $Root "Assets/Resources") -Target (Join-Path $dest "Assets/Resources")

# Supporting docs/tooling
Copy-Tree -Source (Join-Path $Root "docs") -Target (Join-Path $dest "docs")
Copy-Tree -Source (Join-Path $Root "tools") -Target (Join-Path $dest "tools")
Copy-Tree -Source (Join-Path $Root ".github") -Target (Join-Path $dest ".github")
Copy-Tree -Source (Join-Path $Root ".githooks") -Target (Join-Path $dest ".githooks")

# Root files
$rootFiles = @("README.md", ".gitignore")
foreach ($file in $rootFiles) {
    $src = Join-Path $Root $file
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination (Join-Path $dest $file) -Force
    }
}

Write-Host "Mirror complete -> $dest"
