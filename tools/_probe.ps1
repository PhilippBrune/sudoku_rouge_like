$root = "$(Split-Path $PSScriptRoot -Parent)\Assets\Resources"
$needed = @("icon_torii_lock","icon_stone_gear","icon_flow_ribbon","icon_engraved_stone","icon_moss_trap","icon_iron_latch","icon_compass_of_order","icon_golden_koi")
foreach ($n in $needed) {
    $found = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object { $_.BaseName -ieq $n } | Select-Object -ExpandProperty FullName
    if ($found) { Write-Host "FOUND: $n => $found" } else { Write-Host "MISSING: $n" }
}
Write-Host ""; Write-Host "--- All Resources subfolders ---"
Get-ChildItem -LiteralPath $root -Directory | ForEach-Object { Write-Host "$($_.Name)/ : $((Get-ChildItem $_.FullName -File).Count) files" }
