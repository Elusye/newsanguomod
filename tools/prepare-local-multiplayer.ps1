param(
    [Parameter(Mandatory = $true)]
    [string]$GameExe
)
$ErrorActionPreference = 'Stop'

# Offline startup needs the complete Workshop package, including every compat runtime.
$resolvedGameExe = (Resolve-Path -LiteralPath $GameExe).ProviderPath
$gameDirectory = [IO.Path]::GetDirectoryName($resolvedGameExe)
$steamApps = Split-Path -Parent (Split-Path -Parent $gameDirectory)
$source = Join-Path $steamApps 'workshop\content\2868840\3747602295'
$destination = Join-Path $gameDirectory 'mods\STS2-RitsuLib'
$manifestPath = Join-Path $source 'ritsulib-variants.manifest'
if (!(Test-Path -LiteralPath $manifestPath)) {
    throw "RitsuLib Workshop files not found: $source. Subscribe to item 3747602295 and let Steam finish downloading."
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if (!($manifest.variants | Where-Object { $_.compatTarget -eq '0.111.0' })) {
    throw 'The Workshop RitsuLib package does not include STS2 0.111.0 support. Update it in Steam first.'
}
$files = @(Get-ChildItem -LiteralPath $source -File -Recurse)
if ($files.Count -eq 0) { throw 'The RitsuLib Workshop package is empty.' }
$needsUpdate = $false
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\')
    $target = Join-Path $destination $relative
    if (!(Test-Path -LiteralPath $target) -or
        (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
        $needsUpdate = $true
        break
    }
}
if (!$needsUpdate) {
    Write-Host 'Local RitsuLib already matches the Workshop package.'
    exit 0
}
if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) {
    throw 'Close all STS2 instances before updating local RitsuLib, then run the test script again.'
}
$modsDirectory = Join-Path $gameDirectory 'mods'
New-Item -ItemType Directory -Force -Path $modsDirectory | Out-Null
$staging = Join-Path $gameDirectory ('.newsanguo-ritsulib-staging-' + [Guid]::NewGuid().ToString('N'))
Copy-Item -LiteralPath $source -Destination $staging -Recurse
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\')
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $staging $relative)).Hash) {
        throw "RitsuLib copy verification failed: $relative"
    }
}
# Keep backups outside mods so the game cannot discover duplicate manifests.
$expected = [IO.Path]::GetFullPath((Join-Path $gameDirectory 'mods\STS2-RitsuLib'))
if ([IO.Path]::GetFullPath($destination) -ne $expected) { throw 'Unexpected RitsuLib destination.' }
if (Test-Path -LiteralPath $destination) {
    $backupRoot = Join-Path $env:LOCALAPPDATA 'newsanguo\local-multiplayer-backups'
    New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
    $backup = Join-Path $backupRoot ('STS2-RitsuLib-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
    Move-Item -LiteralPath $destination -Destination $backup
    Write-Host "Previous local RitsuLib backed up to: $backup"
}
Move-Item -LiteralPath $staging -Destination $destination
$version = (Get-Content -LiteralPath (Join-Path $destination 'mod_manifest.json') -Raw | ConvertFrom-Json).version
Write-Host "Local RitsuLib updated to $version with STS2 0.111.0 support."
