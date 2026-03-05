param(
    [string]$Root = (Resolve-Path ".").Path,
    [ValidateSet("nested-to-top","top-to-nested")]
    [string]$Direction = "nested-to-top",
    [switch]$IncludeMeta
)

$nestedRoot = Join-Path $Root "Assets/Scripts/My project/Assets/Scripts"
$topRoot = Join-Path $Root "Assets/Scripts"

if (-not (Test-Path $nestedRoot) -or -not (Test-Path $topRoot)) {
    Write-Error "Required script roots not found. Expected '$topRoot' and '$nestedRoot'."
    exit 1
}

$sourceRoot = if ($Direction -eq "nested-to-top") { $nestedRoot } else { $topRoot }
$destRoot = if ($Direction -eq "nested-to-top") { $topRoot } else { $nestedRoot }

$patterns = @("*.cs")
if ($IncludeMeta) {
    $patterns += "*.cs.meta"
}

$copied = 0
$updated = 0
$skipped = 0

foreach ($pattern in $patterns) {
    Get-ChildItem -Path $sourceRoot -Recurse -File -Filter $pattern | ForEach-Object {
        $rel = $_.FullName.Substring($sourceRoot.Length)
        if ($rel.StartsWith("\\") -or $rel.StartsWith("/")) {
            $rel = $rel.Substring(1)
        }
        $dest = Join-Path $destRoot $rel

        if (-not (Test-Path $dest)) {
            New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null
            Copy-Item -Path $_.FullName -Destination $dest -Force
            $copied++
            return
        }

        $srcHash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash
        $dstHash = (Get-FileHash -Path $dest -Algorithm SHA256).Hash
        if ($srcHash -ne $dstHash) {
            Copy-Item -Path $_.FullName -Destination $dest -Force
            $updated++
        }
        else {
            $skipped++
        }
    }
}

Write-Host "Sync complete ($Direction)"
Write-Host "- New files copied: $copied"
Write-Host "- Existing files updated: $updated"
Write-Host "- Unchanged files skipped: $skipped"
