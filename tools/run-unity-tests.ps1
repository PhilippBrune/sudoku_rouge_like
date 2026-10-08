param(
    [ValidateSet("EditMode", "PlayMode", "All")]
    [string]$TestPlatform = "EditMode",

    [string]$UnityExe = $env:UNITY_EXE,

    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,

    [string]$ResultsDir = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "TestResults"),

    # Optional NUnit full-name filter (regex or semicolon-separated names), passed to Unity as -testFilter.
    # [Explicit] fixtures (e.g. the review diagnostics) only run when selected here.
    [string]$TestFilter = "",

    # Optional semicolon-separated category list, passed to Unity as -testCategory.
    [string]$TestCategory = ""
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

function Assert-ProjectNotOpenInEditor {
    param([string]$Project)

    # Batchmode cannot open a project that an interactive Editor already holds (Temp/UnityLockfile).
    $normalized = [System.IO.Path]::GetFullPath($Project).TrimEnd('\').ToLowerInvariant()
    $editors = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine.ToLowerInvariant().Contains($normalized) -and -not $_.CommandLine.ToLowerInvariant().Contains("-batchmode") })
    if ($editors.Count -gt 0) {
        throw "This project is open in the Unity Editor (PID $($editors[0].ProcessId)). Close the Editor and rerun; batchmode cannot run tests while the project is locked."
    }
}

function Write-TestSummary {
    param([string]$ResultPath, [string]$Platform)

    [xml]$xml = Get-Content -LiteralPath $ResultPath -Raw
    $run = $xml.'test-run'
    $total = [int]$run.total
    $passed = [int]$run.passed
    $failed = [int]$run.failed
    $skipped = [int]$run.skipped
    Write-Host "Unity $Platform tests: total=$total passed=$passed failed=$failed skipped=$skipped"

    foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
        $message = ""
        $failureNode = $case.SelectSingleNode("failure/message")
        if ($null -ne $failureNode) {
            $message = ($failureNode.InnerText -replace '\s+', ' ').Trim()
            if ($message.Length -gt 300) { $message = $message.Substring(0, 300) + "..." }
        }
        Write-Host "  FAIL $($case.fullname)"
        if ($message) { Write-Host "       $message" }
    }

    return [pscustomobject]@{ Total = $total; Failed = $failed }
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

    # A stale XML from an earlier run must never be mistaken for this run's result.
    if (Test-Path -LiteralPath $resultPath) {
        Remove-Item -LiteralPath $resultPath -Force
    }

    # Note: -quit must NOT be passed together with -runTests. With -quit Unity exits
    # before the test runner starts, returns 0 and writes no XML (GAP-010).
    $unityArgs = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $Project,
        "-runTests",
        "-testPlatform", $Platform,
        "-testResults", $resultPath,
        "-logFile", $logPath
    )
    if ($TestFilter) { $unityArgs += @("-testFilter", $TestFilter) }
    if ($TestCategory) { $unityArgs += @("-testCategory", $TestCategory) }

    Write-Host "Running Unity $Platform tests..."
    Write-Host "Unity: $Editor"
    Write-Host "Project: $Project"
    Write-Host "Results: $resultPath"

    $argumentLine = ($unityArgs | ForEach-Object {
        if ($_ -match '[\s;]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    }) -join " "

    # Wait for Unity itself, not its process tree: Start-Process -Wait also waits for descendants,
    # and Unity can leave a long-lived adb server running, which would block this script forever.
    $process = Start-Process -FilePath $Editor -ArgumentList $argumentLine -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    $exitCode = $process.ExitCode

    if ($exitCode -eq 198) {
        throw "Unity exited with code 198, commonly caused by missing Editor licensing in batchmode. Activate Unity for this machine and rerun. See $logPath"
    }

    if (!(Test-Path -LiteralPath $resultPath)) {
        throw "Unity exited with code $exitCode without creating $resultPath, so no tests ran. See $logPath"
    }

    $summary = Write-TestSummary -ResultPath $resultPath -Platform $Platform

    if ($summary.Total -eq 0) {
        throw "Unity $Platform run produced a results file with 0 tests. Check the filter/category and $logPath"
    }

    # Unity returns 2 when tests failed and other non-zero codes for run errors.
    if ($summary.Failed -gt 0 -or $exitCode -ne 0) {
        return $false
    }

    Write-Host "Unity $Platform tests completed."
    return $true
}

$unity = Resolve-UnityExe -Candidate $UnityExe
Assert-ProjectNotOpenInEditor -Project $ProjectPath
$platforms = if ($TestPlatform -eq "All") { @("EditMode", "PlayMode") } else { @($TestPlatform) }

$allPassed = $true
foreach ($platform in $platforms) {
    $ok = Invoke-UnityTests -Editor $unity -Platform $platform -Project $ProjectPath -OutDir $ResultsDir
    if (-not $ok) { $allPassed = $false }
}

if (-not $allPassed) {
    Write-Error "Unity tests failed. See the summary above and the logs in $ResultsDir."
}
