$csv = "$(Split-Path $PSScriptRoot -Parent)\Assets\Resources\GeneratedIcons\RunOfTheNine_IconMap.csv"

# Items that are on disk but have zero C# code references — reserved for future use
$reserved = @(
    'icon_jade_amulet.png',
    'icon_moss_stone.png',
    'icon_pebble.png',
    'icon_rice_bowl.png',
    'icon_sacred_bell.png',
    'icon_sun_medallion.png',
    'icon_tea_cup.png',
    'icon_water_basin.png',
    'icon_wind_bell.png',
    'icon_compass_of_order.png',
    'icon_enlightenment_tree.png',
    'icon_spirit_dragon_coin.png'
)

# ui_tutorial_progress has no C# reference — flag as unreferenced
$unreferenced = @('ui_tutorial_progress.png')

# 3 new background rows to add (stopgap copies of bg_reward already on disk)
$newBgRows = @(
    "background,bg_rest.png,Bg Rest",
    "background,bg_relic.png,Bg Relic",
    "background,bg_swap.png,Bg Swap"
)

$lines = Get-Content $csv
# New header: index,label,category,file,status
$newHeader = "index,label,category,file,status"

$output = [System.Collections.Generic.List[string]]::new()

foreach ($row in $lines[1..($lines.Length-1)]) {
    $parts = $row -split ','
    if ($parts.Count -lt 4) { continue }
    $file = $parts[3]

    # Drop segmented_blue_line
    if ($file -eq 'icon_segmented_blue_line.png') { continue }
    # Drop old status column if present (idempotent)
    $label    = $parts[1]
    $category = $parts[2]

    $status = if ($reserved -contains $file) { 'reserved' }
              elseif ($unreferenced -contains $file) { 'unreferenced' }
              else { 'active' }

    $output.Add("$label,$category,$file,$status")
}

# Append the 3 new backgrounds
foreach ($r in $newBgRows) {
    $p = $r -split ','
    $output.Add("$($p[2]),$($p[0]),$($p[1]),active")
}

# Re-number and write
$numbered = [System.Collections.Generic.List[string]]::new()
$numbered.Add($newHeader)
$i = 1
foreach ($r in $output) {
    $numbered.Add("$i,$r")
    $i++
}

Set-Content -Path $csv -Value $numbered -Encoding UTF8
Write-Host "CSV rebuilt: $($numbered.Count - 1) data rows"
Write-Host "  active:       $(($numbered | Select-Object -Skip 1 | Where-Object { $_ -match ',active$' }).Count)"
Write-Host "  reserved:     $(($numbered | Select-Object -Skip 1 | Where-Object { $_ -match ',reserved$' }).Count)"
Write-Host "  unreferenced: $(($numbered | Select-Object -Skip 1 | Where-Object { $_ -match ',unreferenced$' }).Count)"
