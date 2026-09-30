# Run PostHog HogQL with the SQL kept in a file (avoids Windows PowerShell 5.1 native-arg quoting issues).
#
# Usage:
#   & .\tools\posthog-sql.ps1 -SqlFile analysis\posthog\01_events.sql
#   & .\tools\posthog-sql.ps1 -SqlFile <file> -Context "..." -Raw
#
# Credentials live in the repo (<repo>\.posthog-home, gitignored), but the confined shell cannot
# always write inside the workspace, and posthog-cli needs to install its bundle. So run with
# POSTHOG_HOME in a writable temp dir and copy the credentials over on every run.
param(
    [Parameter(Mandatory = $true)][string]$SqlFile,
    [string]$Context = "governed catalog consulted: no match - ad hoc mod telemetry analysis",
    [string]$Tool = "execute-sql",
    [switch]$Raw
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$credSource = Join-Path $repoRoot '.posthog-home\credentials.json'
$env:POSTHOG_HOME = Join-Path $env:TEMP 'newsanguo-posthog'
New-Item -ItemType Directory -Force -Path $env:POSTHOG_HOME | Out-Null
if (Test-Path -LiteralPath $credSource) {
    Copy-Item -LiteralPath $credSource -Destination (Join-Path $env:POSTHOG_HOME 'credentials.json') -Force
}

if (-not (Test-Path -LiteralPath $SqlFile)) { throw "SQL file not found: $SqlFile" }

# Read through .NET: PowerShell's Get-Content output can carry PSObject note-properties here,
# which ConvertTo-Json then serializes as {"value":...} instead of a plain JSON string.
# Also flatten to one line and drop "--" comments: HogQL does not need newlines, and a payload
# without newlines is immune to command-line newline handling. (Comment lines are dropped so a
# swallowed "comment-plus-rest-of-query" cannot happen.)
$lines = [System.IO.File]::ReadAllLines($SqlFile, [System.Text.Encoding]::UTF8) |
    Where-Object { $t = $_.Trim(); $t -ne '' -and -not $t.StartsWith('--') }
$sql = ($lines -join ' ')
if ($sql.Trim() -eq '') { throw "SQL file has no statements: $SqlFile" }

# Build the JSON by hand (no ConvertTo-Json: PS 5.1 escapes < > ' as \u00xx, adding backslashes
# that complicate the native-argument round trip).
function ConvertTo-JsonLiteral([string]$value) {
    $v = $value -replace '\\', '\\\\' -replace '"', '\"' -replace "`t", ' '
    return '"' + $v + '"'
}

$json = '{"query":' + (ConvertTo-JsonLiteral $sql) + ',"context":' + (ConvertTo-JsonLiteral $Context) + '}'

# PowerShell 5.1 hands arguments to native exes without escaping embedded quotes/backslashes,
# so apply the standard C-runtime escaping ourselves: \ -> \\ first, then " -> \"
$escaped = ($json -replace '\\', '\\') -replace '"', '\"'

if ($Raw) {
    posthog-cli api call --json $Tool $escaped
} else {
    posthog-cli api call $Tool $escaped
}
