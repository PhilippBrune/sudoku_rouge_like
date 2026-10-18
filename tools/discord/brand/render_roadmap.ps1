# Renders tools/discord/brand/roadmap.html to roadmap.png (1920x1080) with headless Chrome.
# The content follows docs/ROADMAP_BACKLOG.md; update both together.
$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$url = 'file:///' + (Join-Path $here 'roadmap.html').Replace([char]92, [char]47)
& $chrome --headless=new --disable-gpu --hide-scrollbars --allow-file-access-from-files --force-device-scale-factor=1 `
    --window-size=1920,1080 --virtual-time-budget=4000 "--screenshot=$(Join-Path $here 'roadmap.png')" $url 2>$null | Out-Null
Write-Output 'roadmap.png'
