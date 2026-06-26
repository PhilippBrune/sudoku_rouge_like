$file = "$(Split-Path $PSScriptRoot -Parent)\Assets\Scripts\UI\MainMenuBlueprintBuilder.cs"
$content = [System.IO.File]::ReadAllText($file)

# icon paths -> correct subfolders
$content = $content.Replace('"GeneratedIcons/icon_torii_lock"',       '"ui/icon_torii_lock"')
$content = $content.Replace('"GeneratedIcons/icon_bud"',              '"meta/icon_bud"')
$content = $content.Replace('"GeneratedIcons/icon_infinite_lotus"',   '"legendary/icon_eternal_lotus"')
$content = $content.Replace('"GeneratedIcons/icon_temple_seal"',      '"items/icon_temple_seal"')
$content = $content.Replace('"GeneratedIcons/icon_lantern_of_clarity"','"items/icon_lantern_of_clarity"')

# ui_ backgrounds -> background subfolder
$content = $content.Replace('"GeneratedIcons/ui_main_menu"',          '"background/ui_main_menu"')
$content = $content.Replace('"GeneratedIcons/ui_class_select"',       '"background/ui_class_select"')
$content = $content.Replace('"GeneratedIcons/ui_tutorial_setup"',     '"background/ui_tutorial_setup"')
$content = $content.Replace('"GeneratedIcons/ui_options"',            '"background/ui_options"')
$content = $content.Replace('"GeneratedIcons/ui_meta_progression"',   '"background/ui_meta_progression"')
$content = $content.Replace('"GeneratedIcons/ui_game_modes"',         '"background/ui_game_modes"')
$content = $content.Replace('"GeneratedIcons/ui_items_menu"',         '"background/ui_items_menu"')

[System.IO.File]::WriteAllText($file, $content, [System.Text.Encoding]::UTF8)

# Verify no GeneratedIcons/icon_ or GeneratedIcons/ui_ remain
$remaining = ([regex]::Matches($content, '"GeneratedIcons/')).Count
Write-Host "Replacements done. Remaining GeneratedIcons/ refs: $remaining"
