param(
    [ValidateSet("EditMode", "PlayMode", "All")]
    [string]$TestPlatform = "EditMode",

    [string]$UnityExe = $env:UNITY_EXE,

    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,

    [string]$ResultsDir = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "TestResults")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-UnityExe {
    param([string]$Candidate)

    if ($Candidate -and (Test-Path -LiteralPath $Candidate)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $default = "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe"
    if (Test-Path -LiteralPath $default) {
        return $default
    }

    $hubRoot = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path -LiteralPath $hubRoot) {
        $found = Get-ChildItem -LiteralPath $hubRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object {
                $exe = Join-Path $_.FullName "Editor\Unity.exe"
                if (Test-Path -LiteralPath $exe) { $exe }
            } |
            Select-Object -First 1

        if ($found) {
            return $found
        }
    }

    throw "Unity.exe was not found. Pass -UnityExe or set UNITY_EXE."
}

function Invoke-UnityTests {
    param(
        [string]$Editor,
        [string]$Platform,
        [string]$Project,
        [string]$OutDir
    )

    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

    $resultPath = Join-Path $OutDir "$Platform-results.xml"
    $logPath = Join-Path $OutDir "$Platform-unity.log"

    $unityArgs = @(
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath", $Project,
        "-runTests",
        "-testPlatform", $Platform,
        "-testResults", $resultPath,
        "-logFile", $logPath
    )

    Write-Host "Running Unity $Platform tests..."
    Write-Host "Unity: $Editor"
    Write-Host "Project: $Project"
    Write-Host "Results: $resultPath"

    $argumentLine = ($unityArgs | ForEach-Object {
        if ($_ -match '\s') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    }) -join " "

    $process = Start-Process -FilePath $Editor -ArgumentList $argumentLine -Wait -PassThru -WindowStyle Hidden
    $exitCode = $process.ExitCode

    if ($exitCode -eq 198) {
        Write-Error "Unity exited with code 198, commonly caused by missing Editor licensing in batchmode. Activate Unity for this machine and rerun. See $logPath"
    }

    if ($exitCode -ne 0) {
        Write-Error "Unity $Platform tests failed with exit code $exitCode. See $logPath"
    }

    if (!(Test-Path -LiteralPath $resultPath)) {
        Write-Error "Unity completed without creating $resultPath. See $logPath"
    }

    Write-Host "Unity $Platform tests completed."
}

$unity = Resolve-UnityExe -Candidate $UnityExe
$platforms = if ($TestPlatform -eq "All") { @("EditMode", "PlayMode") } else { @($TestPlatform) }

foreach ($platform in $platforms) {
    Invoke-UnityTests -Editor $unity -Platform $platform -Project $ProjectPath -OutDir $ResultsDir
}
