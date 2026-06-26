<#
.SYNOPSIS
    Game Dev AI Pipeline shortcut.

.DESCRIPTION
    Passes your prompt to the LangGraph agent pipeline.
    With no arguments, starts the interactive REPL.

.EXAMPLE
    .\agent.ps1 "create a fire mage enemy with pixel art icon"

.EXAMPLE
    .\agent.ps1 --tasks code test git "frost archer enemy"

.EXAMPLE
    .\agent.ps1                  # interactive REPL mode

.EXAMPLE
    .\agent.ps1 --tasks icon_batch "generate all missing icons"

.EXAMPLE
    .\agent.ps1 --tasks review ticket "review all docs and create tickets"
#>

$repoRoot = Split-Path $PSScriptRoot -Parent
$python = "$repoRoot\.venv\Scripts\python.exe"
$script = "$repoRoot\ai\main.py"

if (-not (Test-Path $python)) {
    Write-Error "Python venv not found at $python. Run: python -m venv .venv"
    exit 1
}

# Load ai/.env if present (so environment variables are available to the script)
$envFile = "$repoRoot\ai\.env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match "^\s*([^#][^=]+)=(.*)$") {
            $name  = $Matches[1].Trim()
            $value = $Matches[2].Trim()
            [System.Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

if ($args.Count -eq 0) {
    # Interactive REPL
    Push-Location $repoRoot
    & $python -m ai.main --interactive
    Pop-Location
} else {
    Push-Location $repoRoot
    & $python -m ai.main @args
    Pop-Location
}
