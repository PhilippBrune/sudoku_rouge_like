$base = "C:\Users\Philipp\Documents\sudoku_rouge_like\Assets\Resources"
Copy-Item "$base\node\icon_campfire_stones.png" "$base\node\icon_engraved_stone.png" -Force
Copy-Item "$base\cursed\icon_fog_stone.png" "$base\node\icon_moss_trap.png" -Force
Write-Host "Done. node/ files:"
Get-ChildItem "$base\node\" -Name
