$base = "C:\Users\Philipp\Documents\sudoku_rouge_like\Assets\Resources"

function New-UnityMeta {
    param(
        [string]$TemplateMeta,
        [string]$OutputPath,
        [string]$SpriteName
    )
    $guid       = [System.Guid]::NewGuid().ToString("N")
    $spriteGuid = [System.Guid]::NewGuid().ToString("N")
    # Unity stores internalID as a signed int64 — generate via Get-Random (PS5.1 compatible)
    $intId = [int64](Get-Random -Minimum 1000000000000000 -Maximum ([long]::MaxValue))

    $content = Get-Content $TemplateMeta -Raw

    # Replace file-level GUID (line 2)
    $content = $content -replace '(?m)^guid: [0-9a-f]{32}', "guid: $guid"

    # Replace the internalIDToNameTable entry name and its internalID
    $oldName = [regex]::Match($content, 'second: (\S+)').Groups[1].Value
    $oldId   = [regex]::Match($content, '213: (\d+)').Groups[1].Value

    $content = $content -replace [regex]::Escape("second: $oldName"), "second: ${SpriteName}_0"
    $content = $content -replace [regex]::Escape("213: $oldId"),   "213: $intId"

    # Replace name in spriteSheet.sprites section (appears twice: name: and nameFileIdTable key)
    $content = $content -replace [regex]::Escape("name: ${oldName}"), "name: ${SpriteName}_0"
    $content = $content -replace [regex]::Escape("${oldName}: $oldId"), "${SpriteName}_0: $intId"

    # Replace spriteID (the per-sprite GUID, lowercase no dashes)
    $content = $content -replace '(?m)(spriteID: )([0-9a-f]{32})', "`${1}$spriteGuid"

    # Replace internalID in the sprites array (the one after spriteID, not the nameFileIdTable one)
    $content = $content -replace [regex]::Escape("internalID: $oldId"), "internalID: $intId"

    Set-Content -Path $OutputPath -Value $content -NoNewline -Encoding UTF8
}

# ── P0: bg stopgap copies ──────────────────────────────────────────────────────
$bgSrc = "$base\background\bg_reward.png"
$bgMeta = "$base\background\bg_reward.png.meta"
foreach ($name in @("bg_rest", "bg_relic", "bg_swap")) {
    $dst = "$base\background\$name.png"
    if (-not (Test-Path $dst)) {
        Copy-Item $bgSrc $dst -Force
        Write-Host "Copied -> $name.png"
    } else {
        Write-Host "Already exists: $name.png"
    }
    New-UnityMeta -TemplateMeta $bgMeta -OutputPath "$base\background\$name.png.meta" -SpriteName $name
    Write-Host "  .meta written for $name"
}

# ── P0: node .meta files ───────────────────────────────────────────────────────
$nodeMeta = "$base\node\icon_campfire_stones.png.meta"
New-UnityMeta -TemplateMeta $nodeMeta -OutputPath "$base\node\icon_engraved_stone.png.meta" -SpriteName "icon_engraved_stone"
Write-Host "  .meta written for icon_engraved_stone"
New-UnityMeta -TemplateMeta $nodeMeta -OutputPath "$base\node\icon_moss_trap.png.meta" -SpriteName "icon_moss_trap"
Write-Host "  .meta written for icon_moss_trap"

# ── P2: Delete orphaned .meta files in background/ ────────────────────────────
$orphans = @(
    "$base\background\AI_prompts.txt.meta",
    "$base\background\background_generation_prompts.txt.meta"
)
foreach ($f in $orphans) {
    if (Test-Path $f) { Remove-Item $f -Force; Write-Host "Deleted orphan: $(Split-Path $f -Leaf)" }
    else { Write-Host "Not found (already removed?): $(Split-Path $f -Leaf)" }
}

# ── P2: Delete segmented_blue_line ────────────────────────────────────────────
$sbl = "$base\modifier\icon_segmented_blue_line.png"
$sblMeta = "$base\modifier\icon_segmented_blue_line.png.meta"
if (Test-Path $sbl) { Remove-Item $sbl -Force; Write-Host "Deleted: icon_segmented_blue_line.png" }
if (Test-Path $sblMeta) { Remove-Item $sblMeta -Force; Write-Host "Deleted: icon_segmented_blue_line.png.meta" }

Write-Host ""
Write-Host "Done. node/ now:"
Get-ChildItem "$base\node\" -Filter "*.png" | Select-Object -ExpandProperty Name
Write-Host ""
Write-Host "background/ .png count: $((Get-ChildItem "$base\background\" -Filter "*.png").Count)"
Write-Host "modifier/ .png count:   $((Get-ChildItem "$base\modifier\" -Filter "*.png").Count)"
