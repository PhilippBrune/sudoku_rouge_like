# Renders every src/*.html mockup to <name>.png at 1920x1080 with headless Chrome (docs/design/layout-proposals.md).
# A file src/<name>.variants.txt with lines "suffix|query" also renders <name>_<suffix>.png from <name>.html?<query>.
param([string]$Only = '*')
$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Render([string]$url, [string]$out) {
    & $chrome --headless=new --disable-gpu --hide-scrollbars --allow-file-access-from-files --force-device-scale-factor=1 `
        --window-size=1920,1080 --virtual-time-budget=4000 "--screenshot=$out" $url 2>$null | Out-Null
    Write-Output (Split-Path -Leaf $out)
}

Get-ChildItem (Join-Path $here 'src') -Filter "$Only.html" | ForEach-Object {
    $url = 'file:///' + $_.FullName.Replace([char]92, [char]47)
    Render $url (Join-Path $here ($_.BaseName + '.png'))
    $variants = Join-Path $_.DirectoryName ($_.BaseName + '.variants.txt')
    if (Test-Path $variants) {
        foreach ($line in Get-Content $variants) {
            if (-not $line.Trim()) { continue }
            $suffix, $query = $line.Split('|', 2)
            Render ($url + '?' + $query) (Join-Path $here ($_.BaseName + '_' + $suffix + '.png'))
        }
    }
}
