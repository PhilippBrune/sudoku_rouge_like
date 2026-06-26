$repoRoot = Split-Path $PSScriptRoot -Parent
$gi     = "$repoRoot\Assets\Resources\GeneratedIcons"
$items  = "$repoRoot\Assets\Resources\items"
$node   = "$repoRoot\Assets\Resources\node"
$backup = "$repoRoot\Assets\Resources\backup_icons"

# ── Create backup_icons folder ──────────────────────────────────────────────
New-Item -Type Directory -Path $backup -Force | Out-Null
Write-Host "[OK] backup_icons/ folder ready"

# ── Copy compass_of_order to items/ (try both casings) ──────────────────────
$compassSrc = Get-ChildItem $gi | Where-Object { $_.Name -ilike "icon_compass_of_order*" -and $_.Extension -eq ".png" } | Select-Object -First 1
if ($compassSrc) {
    Copy-Item $compassSrc.FullName "$items\icon_compass_of_order.png" -Force
    Write-Host "[OK] Copied $($compassSrc.Name) -> items/icon_compass_of_order.png"
} else {
    Write-Host "[SKIP] compass_of_order not found in GeneratedIcons/ (may already be in items/)"
}
Write-Host "      items/ has it: $(Test-Path "$items\icon_compass_of_order.png")"

# ── Move 8 backup icons to backup_icons/ ────────────────────────────────────
$backupNames = @("Geometric_Seal","Flame_Stone","Flowing_Wind","Fractured_Lantern","Scroll_Stamp","Ink_Save","Language_Scroll","Temple_Bell")
foreach ($name in $backupNames) {
    $src = Get-ChildItem $gi | Where-Object { $_.Name -ilike "icon_$name.png" } | Select-Object -First 1
    if ($src) {
        Move-Item $src.FullName "$backup\$($src.Name)" -Force
        # move meta too
        $meta = "$($src.FullName).meta"
        if (Test-Path $meta) { Move-Item $meta "$backup\$($src.Name).meta" -Force }
        Write-Host "[OK] Moved $($src.Name) -> backup_icons/"
    } else {
        Write-Host "[SKIP] icon_$name.png not found"
    }
}

# ── Delete 19 Extra_Filler icons (141-159) ──────────────────────────────────
$deleted = 0
Get-ChildItem $gi | Where-Object { $_.Name -match "icon_Extra_Filler_1[4-5]\d" } | ForEach-Object {
    Remove-Item $_.FullName -Force
    $meta = "$($_.FullName).meta"
    if (Test-Path $meta) { Remove-Item $meta -Force }
    $deleted++
}
Write-Host "[OK] Deleted $deleted Extra_Filler files (including metas)"

# ── Delete retired icon duplicates from GeneratedIcons/ ─────────────────────
$retired = @("Green_Whisper","Orange_Whisper","Infinite_Lotus")
foreach ($name in $retired) {
    $src = Get-ChildItem $gi | Where-Object { $_.Name -ilike "icon_$name.png" } | Select-Object -First 1
    if ($src) {
        Remove-Item $src.FullName -Force
        $meta = "$($src.FullName).meta"
        if (Test-Path $meta) { Remove-Item $meta -Force }
        Write-Host "[OK] Deleted $($src.Name) from GeneratedIcons/"
    } else {
        Write-Host "[SKIP] icon_$name.png not found in GeneratedIcons/"
    }
}

# ── Delete Triple_Chest from node/ ──────────────────────────────────────────
$tc = Get-ChildItem $node | Where-Object { $_.Name -ilike "icon_triple_chest.png" } | Select-Object -First 1
if ($tc) {
    Remove-Item $tc.FullName -Force
    $meta = "$($tc.FullName).meta"
    if (Test-Path $meta) { Remove-Item $meta -Force }
    Write-Host "[OK] Deleted $($tc.Name) from node/"
} else {
    Write-Host "[SKIP] icon_triple_chest.png not in node/"
}

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== Final verification ==="
Write-Host "items/icon_compass_of_order.png : $(Test-Path "$items\icon_compass_of_order.png")"
Write-Host "backup_icons/ count            : $((Get-ChildItem $backup -Filter '*.png').Count) png files"
Write-Host "node/ remaining                : $((Get-ChildItem $node -Filter '*.png').Count) png files"
Write-Host "GeneratedIcons/ remaining      : $((Get-ChildItem $gi -Filter '*.png').Count) png files"
