# Call any PostHog CLI agent tool with a JSON payload kept in a file.
# (Windows PowerShell 5.1 mangles inline quotes/backslashes when passing args to native exes,
#  and non-ASCII inline args are risky, so the payload is read from disk and escaped here.)
#
# Usage:
#   & .\tools\posthog-call.ps1 -Tool insight-create -JsonFile analysis\posthog\insights\pick_rate.json
#   & .\tools\posthog-call.ps1 -Tool execute-sql -JsonFile <file> -Raw
param(
    [Parameter(Mandatory = $true)][string]$Tool,
    [Parameter(Mandatory = $true)][string]$JsonFile,
    [switch]$Raw,
    [switch]$Confirm
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$credSource = Join-Path $repoRoot '.posthog-home\credentials.json'
$env:POSTHOG_HOME = Join-Path $env:TEMP 'newsanguo-posthog'
New-Item -ItemType Directory -Force -Path $env:POSTHOG_HOME | Out-Null
if (Test-Path -LiteralPath $credSource) {
    Copy-Item -LiteralPath $credSource -Destination (Join-Path $env:POSTHOG_HOME 'credentials.json') -Force
}

if (-not (Test-Path -LiteralPath $JsonFile)) { throw "JSON file not found: $JsonFile" }
# .NET read: Get-Content output carries PSObject note-properties that break ConvertTo-Json,
# and raw newlines must not reach the native command line, so collapse them to spaces.
$json = [System.IO.File]::ReadAllText($JsonFile, [System.Text.Encoding]::UTF8)
$json = ($json -replace "`r`n", ' ') -replace "`n", ' '

# C-runtime escaping for the native argument: \ -> \\ first, then " -> \"
$escaped = ($json -replace '\\', '\\') -replace '"', '\"'

$args = @('api', 'call')
if ($Raw) { $args += '--json' }
if ($Confirm) { $args += '--confirm' }
$args += @($Tool, $escaped)
& posthog-cli @args
