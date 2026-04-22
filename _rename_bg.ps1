$d = "C:\Users\Philipp\Documents\sudoku_rouge_like\Assets\Resources\background"
$pairs = @(
    @("bg_Shrine_Garden", "bg_shrine_garden"),
    @("ui_class_select",  "bg_class_select"),
    @("ui_game_modes",    "bg_game_modes"),
    @("ui_items_menu",    "bg_items_menu"),
    @("ui_main_menu",     "bg_main_menu"),
    @("ui_meta_progression", "bg_meta_progression"),
    @("ui_options",       "bg_options"),
    @("ui_tutorial_progress", "bg_tutorial_progress"),
    @("ui_tutorial_setup", "bg_tutorial_setup")
)
foreach ($p in $pairs) {
    $old = $p[0]; $new = $p[1]
    foreach ($ext in @(".png", ".png.meta")) {
        $src = Join-Path $d ($old + $ext)
        $dst = Join-Path $d ($new + $ext)
        if (Test-Path $src) {
            Rename-Item $src $dst
            Write-Host "OK: $old$ext -> $new$ext"
        } else {
            Write-Host "SKIP: $src not found"
        }
    }
}
