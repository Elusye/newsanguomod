param(
    [string]$GodotExe = "",
    [string]$Sts2Dir = "",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Read-LocalProps {
    param([string]$Root)

    $propsPath = Join-Path $Root "local.props"
    if (-not (Test-Path -LiteralPath $propsPath)) {
        return $null
    }

    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($propsPath)
    $group = $xml.SelectSingleNode("/Project/PropertyGroup")
    if ($null -eq $group) {
        throw "local.props does not contain /Project/PropertyGroup."
    }

    return [pscustomobject]@{
        Sts2Dir = [string]$group.Sts2Dir
        GodotExe = [string]$group.GodotExe
    }
}

function Resolve-ExistingPath {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description not found: $Path"
    }
    return (Resolve-Path -LiteralPath $Path).Path
}

$localProps = Read-LocalProps $ProjectRoot
if ([string]::IsNullOrWhiteSpace($Sts2Dir) -and $null -ne $localProps) {
    $Sts2Dir = $localProps.Sts2Dir
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    if ([string]::IsNullOrWhiteSpace($Sts2Dir)) {
        throw "Sts2Dir is not configured. Copy local.props.example to local.props, or pass -Sts2Dir / -OutputPath."
    }
    $Sts2Dir = Resolve-ExistingPath $Sts2Dir "Sts2Dir"
    $OutputPath = Join-Path $Sts2Dir "mods\newsanguo\newsanguo.pck"
}

function Resolve-GodotExe {
    param(
        [string]$ExplicitPath,
        [string]$ConfiguredPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return Resolve-ExistingPath $ExplicitPath "Godot executable"
    }

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        return Resolve-ExistingPath $ConfiguredPath "Godot executable from local.props"
    }

    foreach ($name in @(
        "Godot_v4.5.1-stable_mono_win64_console.exe",
        "Godot_v4.5.1-stable_mono_win64.exe"
    )) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command) {
            return $command.Source
        }
    }

    $candidates = @(
        "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe",
        "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Godot 4.5.1 Mono was not found. Set GodotExe in local.props, or pass -GodotExe."
}

$configuredGodotExe = ""
if ($null -ne $localProps) {
    $configuredGodotExe = $localProps.GodotExe
}
$GodotExe = Resolve-GodotExe -ExplicitPath $GodotExe -ConfiguredPath $configuredGodotExe
$tempOutput = Join-Path ([System.IO.Path]::GetTempPath()) ("newsanguo-{0}.pck" -f [guid]::NewGuid().ToString("N"))

try {
    Write-Host "Exporting newsanguo PCK..." -ForegroundColor Cyan
    & $GodotExe --headless --path $ProjectRoot --export-pack "Windows Desktop" $tempOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Godot export failed with exit code $LASTEXITCODE"
    }
    if (-not (Test-Path -LiteralPath $tempOutput)) {
        throw "Godot exited successfully but did not create a PCK."
    }

    Write-Host "Removing project-local Godot caches..." -ForegroundColor Cyan
    & python (Join-Path $ProjectRoot "tools\sanitize_pck.py") $tempOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to sanitize exported PCK"
    }

    $destinationDir = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($destinationDir)) {
        New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null
    }
    Move-Item -LiteralPath $tempOutput -Destination $OutputPath -Force
    Write-Host "Wrote $OutputPath" -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $tempOutput) {
        Remove-Item -LiteralPath $tempOutput -Force
    }
}
