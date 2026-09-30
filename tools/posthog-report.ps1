# Run a list of PostHog SQL files and save each result as TSV under analysis\posthog\out\.
#
# Usage:
#   & .\tools\posthog-report.ps1 -SqlFiles analysis\posthog\13_card_pick_rates.sql, analysis\posthog\14_overview.sql
param(
    [Parameter(Mandatory = $true)][string[]]$SqlFiles
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $repoRoot 'analysis\posthog\out'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$runner = Join-Path $PSScriptRoot 'posthog-sql.ps1'

foreach ($file in $SqlFiles) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($file)
    $target = Join-Path $outDir ($name + '.tsv')
    Write-Host "=== $name -> $target"
    & $runner -SqlFile $file 2>&1 | Tee-Object -FilePath $target
}
