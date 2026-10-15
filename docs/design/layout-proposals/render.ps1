# Renders every src/*.html mockup to <name>.png at 1920x1080 with headless Chrome (docs/design/layout-proposals.md).
param([string]$Only = '*')
$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Get-ChildItem (Join-Path $here 'src') -Filter "$Only.html" | ForEach-Object {
    $out = Join-Path $here ($_.BaseName + '.png')
    $url = 'file:///' + $_.FullName.Replace([char]92, [char]47)
    & $chrome --headless=new --disable-gpu --hide-scrollbars --allow-file-access-from-files --force-device-scale-factor=1 `
        --window-size=1920,1080 --virtual-time-budget=4000 "--screenshot=$out" $url 2>$null | Out-Null
    Write-Output "$($_.BaseName).png"
}
