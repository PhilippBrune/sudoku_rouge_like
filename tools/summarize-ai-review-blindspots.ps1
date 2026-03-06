param(
    [string]$InputDir = "Tickets/ai-reviews",
    [string]$OutputFile = "Tickets/ai-reviews/Consolidated_BlindSpots_Summary_2026-03-06.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$files = Get-ChildItem -Path $InputDir -Filter "*_review.md" -File
if (-not $files -or $files.Count -eq 0) {
    throw "No review files found in '$InputDir'."
}

$categoryPatterns = [ordered]@{
    "Unclear or ambiguous wording" = "ambigu|unclear|vague|not clear|interpret"
    "Missing edge and boundary cases" = "edge case|boundary|upper limit|lower limit|out of range|min|max"
    "Missing error handling / failure behavior" = "error handling|failure path|fail gracefully|exception|timeout|retry"
    "Missing security and abuse-case requirements" = "security|abuse|unauthori|authentication|authorization|rate limit|injection|privacy"
    "Missing non-functional requirements" = "performance|latency|reliability|availability|scalability|accessibility|non-functional"
    "Missing acceptance criteria / measurable outcomes" = "acceptance criteria|measurable|success criteria|definition of done|pass/fail"
    "Missing data validation and input constraints" = "validation|invalid input|input constraint|format|sanit|null|empty"
    "Unclear state transitions / lifecycle rules" = "state transition|lifecycle|precondition|postcondition|rollback"
    "Missing observability / audit expectations" = "logging|audit|telemetry|monitor|trace"
    "Missing dependency / integration assumptions" = "dependency|integration|external system|third-party|compatibility|version"
}

$categoryStats = foreach ($entry in $categoryPatterns.GetEnumerator()) {
    $totalMentions = 0
    $fileHits = @()

    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw
        $count = [regex]::Matches($content, $entry.Value, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
        if ($count -gt 0) {
            $totalMentions += $count
            $fileHits += [PSCustomObject]@{
                File = $file.Name
                Mentions = $count
            }
        }
    }

    [PSCustomObject]@{
        Category = $entry.Key
        Mentions = $totalMentions
        HitCount = ($fileHits | Measure-Object).Count
        TopFiles = ($fileHits | Sort-Object Mentions -Descending | Select-Object -First 5)
    }
}

$ranked = $categoryStats | Sort-Object Mentions -Descending

$topFilesByVolume = $files |
    ForEach-Object {
        $content = Get-Content -Path $_.FullName -Raw
        $count = [regex]::Matches($content, "blind spot|missing|ambigu|unclear|edge case|security|assumption", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
        [PSCustomObject]@{
            File = $_.Name
            SignalCount = $count
        }
    } |
    Sort-Object SignalCount -Descending |
    Select-Object -First 10

$lines = @()
$lines += "# Consolidated Blind Spot Summary"
$lines += ""
$lines += "- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$lines += ("- Source folder: {0}" -f $InputDir)
$lines += "- Review files analyzed: $($files.Count)"
$lines += ""
$lines += "## Ranked Blind Spot Categories"
$lines += ""
$rank = 1
foreach ($row in $ranked) {
    if ($row.Mentions -le 0) {
        continue
    }

    $lines += "### $rank. $($row.Category)"
    $lines += "- Mention score: $($row.Mentions)"
    $lines += "- Files impacted: $($row.HitCount)"
    if ($row.TopFiles) {
        $lines += "- Top evidence files:"
        foreach ($f in $row.TopFiles) {
            $lines += "  - $($f.File) ($($f.Mentions))"
        }
    }
    $lines += ""
    $rank++
}

$lines += "## Highest-Risk Documents (By Blind-Spot Signal Density)"
$lines += ""
foreach ($f in $topFilesByVolume) {
    $lines += "- $($f.File): $($f.SignalCount)"
}
$lines += ""
$lines += "## Recommended Next Actions"
$lines += ""
$lines += "1. Add acceptance criteria and pass/fail conditions to documents with highest signal counts."
$lines += "2. Add explicit edge/boundary and failure-path sections to all requirements."
$lines += "3. Add security/abuse requirements where user input, economy changes, or progression state is involved."
$lines += "4. Add non-functional targets (latency, reliability, accessibility) for UI-heavy and generation-heavy systems."
$lines += "5. Re-run AI review after updates and compare trend over time."

$lines | Set-Content -Path $OutputFile -Encoding utf8
Write-Host "Consolidated report written to '$OutputFile'."
