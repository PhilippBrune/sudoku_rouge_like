# Mirrors the repo into %TEMP%\sr_testcopy so Unity batchmode tests can run while the Editor has this project open.
#   tools/sync-test-mirror.ps1                 # sync sources (fast)
#   tools/sync-test-mirror.ps1 -WithLibrary    # also copy Library/ (first run, or when Windows cleaned the PackageCache:
#                                              #  compile errors like "UnityEngine.UI does not exist" mean this)
# Then: & "$env:TEMP\sr_testcopy\tools\run-unity-tests.ps1" -TestPlatform EditMode -ProjectPath "$env:TEMP\sr_testcopy"
param([switch]$WithLibrary)
$src = Resolve-Path (Join-Path $PSScriptRoot '..')
$dst = Join-Path $env:TEMP 'sr_testcopy'
$exclude = @('.git', 'build', 'build.app', 'TestResults', 'Temp', 'Logs', 'obj', 'bin', 'foocus_api', '.claude', '.vs', '.vscode',
             'ProfilerCaptures', 'ai', 'graphify-out')
if (-not $WithLibrary) { $exclude += 'Library' }
$xd = @(); foreach ($d in $exclude) { $xd += (Join-Path $src $d) }
& robocopy $src $dst /MIR /R:0 /W:0 /NFL /NDL /NP /NJH /XF '*.lscache' /XD @xd | Select-Object -Last 3
if ($LASTEXITCODE -ge 8) { Write-Error "robocopy failed with exit code $LASTEXITCODE"; exit $LASTEXITCODE }
Write-Output "Mirror ready: $dst"
exit 0
