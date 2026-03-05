param(
    [string]$Root = (Resolve-Path ".").Path,
    [string]$ProjectName = "My project_clean",
    [string]$DriveLetter = "U",
    [switch]$PurgeCache
)

$projectRoot = Join-Path $Root ("Assets/Scripts/" + $ProjectName)
$drive = ($DriveLetter.TrimEnd(':') + ":")

if (-not (Test-Path $projectRoot)) {
    Write-Error "Project root not found: $projectRoot"
    exit 1
}

# Recreate mapping cleanly.
subst $drive /d | Out-Null
subst $drive $projectRoot

$mappedRoot = $drive + "\\"
if (-not (Test-Path (Join-Path $mappedRoot "Packages\manifest.json"))) {
    Write-Error "SUBST mapping failed for $drive -> $projectRoot"
    exit 2
}

# Optional cache cleanup before opening.
if ($PurgeCache) {
    foreach ($folder in @("Library", "Temp", "Logs")) {
        $path = Join-Path $mappedRoot $folder
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$longPath = Join-Path $projectRoot "Library\PackageCache\com.unity.2d.tooling@249995156768\Editor\Insider\SpriteAtlas\SpriteAtlasIssueReport\SpriteAtlasTextureSpaceUsedIssue\SpriteAtlasTextureSpaceUsedIssueSettings.uxml"
$shortPath = Join-Path $mappedRoot "Library\PackageCache\com.unity.2d.tooling@249995156768\Editor\Insider\SpriteAtlas\SpriteAtlasIssueReport\SpriteAtlasTextureSpaceUsedIssue\SpriteAtlasTextureSpaceUsedIssueSettings.uxml"

Write-Host "Mapped project: $mappedRoot"
Write-Host "Original path length: $($longPath.Length)"
Write-Host "Mapped path length:   $($shortPath.Length)"
Write-Host "Open Unity project using this path: $mappedRoot"
Write-Host "Do not open the long nested path directly in Unity Hub."
