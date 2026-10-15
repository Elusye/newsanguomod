<#
.SYNOPSIS
同步共有牌蜀汉实体的本地化键。

.DESCRIPTION
从 Scripts/Cards/ShuHanSharedCardVariants.cs 自动读取
“蜀汉实体类 : 原卡类”的继承关系，以原卡 title/description 为唯一文案源，
同步 newsanguo/localization/{zhs,eng}/cards.json 中对应的 *_SHU_HAN 键。

默认模式会更新或补齐派生键；-Check 只检查，不写文件，存在差异时返回退出码 1。
#>
param(
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$variantSourcePath = Join-Path $repoRoot 'Scripts\Cards\ShuHanSharedCardVariants.cs'
$localizationPaths = @(
    (Join-Path $repoRoot 'newsanguo\localization\zhs\cards.json'),
    (Join-Path $repoRoot 'newsanguo\localization\eng\cards.json')
)

function ConvertTo-UpperSnakeCase {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    # 与 RitsuLib NormalizePublicStem 的驼峰/连续大写拆词顺序一致。
    $value = [regex]::Replace($Name, '([A-Z]+)([A-Z][a-z])', '$1_$2')
    $value = [regex]::Replace($value, '([a-z0-9])([A-Z])', '$1_$2')
    return $value.ToUpperInvariant()
}

function Find-JsonPropertyLine {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Lines,

        [Parameter(Mandatory = $true)]
        [string]$Key
    )

    $pattern = '^(?<indent>\s*)"' + [regex]::Escape($Key) + '"\s*:\s*(?<rest>.+?)\s*$'
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        $match = [regex]::Match($Lines[$index], $pattern)
        if (-not $match.Success) {
            continue
        }

        $rest = $match.Groups['rest'].Value.TrimEnd()
        $hasComma = $rest.EndsWith(',', [StringComparison]::Ordinal)
        if ($hasComma) {
            $rest = $rest.Substring(0, $rest.Length - 1).TrimEnd()
        }

        return [pscustomobject]@{
            Index = $index
            Indent = $match.Groups['indent'].Value
            Value = $rest
            HasComma = $hasComma
        }
    }

    return $null
}

function Get-SharedCardMappings {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "找不到共有牌实体源码：$Path"
    }

    $source = [IO.File]::ReadAllText($Path)
    $matches = [regex]::Matches(
        $source,
        'public\s+sealed\s+class\s+(?<variant>[A-Za-z0-9_]+ShuHan)\s*:\s*(?<base>[A-Za-z0-9_]+)')

    if ($matches.Count -eq 0) {
        throw "未在 $Path 中找到任何 '*ShuHan : 原卡类' 映射。"
    }

    $seenVariants = @{}
    $mappings = foreach ($match in $matches) {
        $variantType = $match.Groups['variant'].Value
        $baseType = $match.Groups['base'].Value
        if ($seenVariants.ContainsKey($variantType)) {
            throw "发现重复的蜀汉实体类：$variantType"
        }
        $seenVariants[$variantType] = $true

        [pscustomobject]@{
            BaseType = $baseType
            VariantType = $variantType
            BaseStem = ConvertTo-UpperSnakeCase $baseType
            VariantStem = ConvertTo-UpperSnakeCase $variantType
        }
    }

    return @($mappings)
}

function Get-JsonKeyCount {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Json,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    try {
        $parsed = $Json | ConvertFrom-Json
    }
    catch {
        throw ("JSON 校验失败：$Path" + [Environment]::NewLine + $_.Exception.Message)
    }

    return @($parsed.PSObject.Properties).Count
}

function Get-SynchronizedLocalization {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object[]]$Mappings
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "找不到本地化文件：$Path"
    }

    $originalText = [IO.File]::ReadAllText($Path)
    $trimmedText = $originalText.TrimEnd([char]13, [char]10)
    $lines = @([regex]::Split($trimmedText, '\r?\n'))
    $pendingProperties = [System.Collections.Generic.List[object]]::new()

    foreach ($mapping in $Mappings) {
        foreach ($suffix in @('title', 'description')) {
            $sourceKey = "NEWSANGUO_CARD_$($mapping.BaseStem).$suffix"
            $targetKey = "NEWSANGUO_CARD_$($mapping.VariantStem).$suffix"

            $sourceInfo = Find-JsonPropertyLine -Lines $lines -Key $sourceKey
            if ($null -eq $sourceInfo) {
                throw "缺少源本地化键：$sourceKey（$Path）"
            }

            $targetInfo = Find-JsonPropertyLine -Lines $lines -Key $targetKey
            if ($null -ne $targetInfo) {
                $comma = if ($targetInfo.HasComma) { ',' } else { '' }
                $lines[$targetInfo.Index] =
                    $targetInfo.Indent + '"' + $targetKey + '": ' + $sourceInfo.Value + $comma
                continue
            }

            $pendingProperties.Add([pscustomobject]@{
                Key = $targetKey
                Value = $sourceInfo.Value
                Indent = $sourceInfo.Indent
            })
        }
    }

    if ($pendingProperties.Count -gt 0) {
        $closingBraceIndex = -1
        for ($index = $lines.Count - 1; $index -ge 0; $index--) {
            if ($lines[$index].Trim() -eq '}') {
                $closingBraceIndex = $index
                break
            }
        }
        if ($closingBraceIndex -lt 0) {
            throw "找不到 JSON 根对象的结束大括号：$Path"
        }

        $previousPropertyIndex = $closingBraceIndex - 1
        while ($previousPropertyIndex -ge 0 -and
               [string]::IsNullOrWhiteSpace($lines[$previousPropertyIndex])) {
            $previousPropertyIndex--
        }
        if ($previousPropertyIndex -lt 0) {
            throw "JSON 根对象中没有可用的属性：$Path"
        }
        if (-not $lines[$previousPropertyIndex].TrimEnd().EndsWith(',', [StringComparison]::Ordinal)) {
            $lines[$previousPropertyIndex] = $lines[$previousPropertyIndex].TrimEnd() + ','
        }

        $newLines = [System.Collections.Generic.List[string]]::new()
        for ($index = 0; $index -lt $pendingProperties.Count; $index++) {
            $property = $pendingProperties[$index]
            $comma = if ($index -lt $pendingProperties.Count - 1) { ',' } else { '' }
            $newLines.Add(
                $property.Indent + '"' + $property.Key + '": ' + $property.Value + $comma)
        }

        $beforeClosingBrace = @($lines[0..($closingBraceIndex - 1)])
        $fromClosingBrace = @($lines[$closingBraceIndex..($lines.Count - 1)])
        $lines = @($beforeClosingBrace + $newLines.ToArray() + $fromClosingBrace)
    }

    $lineFeed = [string][char]10
    $newText = ($lines -join $lineFeed) + $lineFeed
    $keyCount = Get-JsonKeyCount -Json $newText -Path $Path
    return [pscustomobject]@{
        Path = $Path
        OriginalText = $originalText
        NewText = $newText
        KeyCount = $keyCount
        AddedCount = $pendingProperties.Count
    }
}

$mappings = Get-SharedCardMappings -Path $variantSourcePath
$results = foreach ($path in $localizationPaths) {
    Get-SynchronizedLocalization -Path $path -Mappings $mappings
}

$keyCounts = @($results | ForEach-Object { $_.KeyCount } | Sort-Object -Unique)
if ($keyCounts.Count -ne 1) {
    $details = $results | ForEach-Object { "$($_.Path)=$($_.KeyCount)" }
    throw "中英文本地化键数不一致：$($details -join ', ')"
}

$outOfSync = @($results | Where-Object { $_.OriginalText -cne $_.NewText })
if ($Check) {
    if ($outOfSync.Count -gt 0) {
        foreach ($result in $outOfSync) {
            Write-Error "共有牌本地化未同步：$($result.Path)"
        }
        exit 1
    }

    Write-Host "共有牌本地化检查通过：$($mappings.Count) 张共有牌，$($keyCounts[0]) 个键/语言。"
    exit 0
}

$utf8NoBom = New-Object Text.UTF8Encoding($false)
foreach ($result in $outOfSync) {
    [IO.File]::WriteAllText($result.Path, $result.NewText, $utf8NoBom)
    Write-Host "已同步：$($result.Path)（新增 $($result.AddedCount) 个键）"
}

if ($outOfSync.Count -eq 0) {
    Write-Host "无需修改：共有牌本地化已经同步。"
}
Write-Host "同步完成：$($mappings.Count) 张共有牌，$($keyCounts[0]) 个键/语言。"
