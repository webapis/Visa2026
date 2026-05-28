# Regenerates Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationCatalog.json from
# Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationSeed.Data.cs (the auto-generated C# seed file).
#
# Goal: single source of truth for ApplicationType "Show*" flags and metadata without relying on LOOKUPS.md parsing.
#
param(
    [string]$SeedCsPath = (Join-Path $PSScriptRoot '..\..\Visa2026.Module\DatabaseUpdate\ApplicationTypeConfigurationSeed.Data.cs'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\..\Visa2026.Module\DatabaseUpdate\ApplicationTypeConfigurationCatalog.json')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SeedCsPath)) {
    throw "Seed C# file not found: $SeedCsPath"
}

$cs = Get-Content -LiteralPath $SeedCsPath -Raw -Encoding UTF8

function Unescape-CSharpString([string]$s) {
    if ($null -eq $s) { return "" }
    # Minimal unescape for our generated file format.
    return ($s -replace '\\\\', '\' -replace '\"', '"')
}

$rows = New-Object System.Collections.Generic.List[object]

# Match each row block.
$rowRegex = [regex]::new(
    'new\s+ApplicationTypeConfigurationRow\s*\{\s*(?<body>[\s\S]*?)\s*\}\s*,',
    [System.Text.RegularExpressions.RegexOptions]::Multiline
)

foreach ($m in $rowRegex.Matches($cs)) {
    $body = $m.Groups['body'].Value

    $name = [regex]::Match($body, 'Name\s*=\s*"(?<v>[^"]+)"').Groups['v'].Value
    if ([string]::IsNullOrWhiteSpace($name)) { continue }

    $nameTm = Unescape-CSharpString ([regex]::Match($body, 'NameTm\s*=\s*"(?<v>[^"]*)"').Groups['v'].Value)
    $code = Unescape-CSharpString ([regex]::Match($body, 'Code\s*=\s*"(?<v>[^"]*)"').Groups['v'].Value)

    $pdf = [int]([regex]::Match($body, 'PdfForm_Code\s*=\s*(?<v>\d+)').Groups['v'].Value)
    $dur = [int]([regex]::Match($body, 'DurationInDays\s*=\s*(?<v>\d+)').Groups['v'].Value)

    $ls = [regex]::Match($body, 'LifecycleStage\s*=\s*ApplicationLifecycleStage\.(?<v>\w+)').Groups['v'].Value
    $cat = [regex]::Match($body, 'Category\s*=\s*ApplicationTypeCategory\.(?<v>\w+)').Groups['v'].Value

    # Extract Show* assignments from the inline list.
    $flags = [ordered]@{}
    foreach ($kv in [regex]::Matches($body, '(Show\w+)\s*=\s*(true|false)')) {
        $flags[$kv.Groups[1].Value] = ($kv.Groups[2].Value -eq 'true')
    }

    $rows.Add([ordered]@{
        Name = $name
        NameTm = $nameTm
        Code = $code
        PdfForm_Code = $pdf
        LifecycleStage = $ls
        Category = $cat
        DurationInDays = $dur
        Flags = $flags
    })
}

$obj = [ordered]@{
    version = 1
    generatedFrom = 'ApplicationTypeConfigurationSeed.Data.cs'
    rows = $rows
}

[System.IO.File]::WriteAllText($OutputPath, ($obj | ConvertTo-Json -Depth 8), (New-Object System.Text.UTF8Encoding $false))
Write-Host "Wrote $($rows.Count) rows to $OutputPath"

