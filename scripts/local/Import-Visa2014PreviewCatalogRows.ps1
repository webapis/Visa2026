#Requires -Version 5.1
<#
.SYNOPSIS
  Reads import rows from a VISA2014 minimal preview .xlsx (Visa2014MinimalXlsxWriter format).
#>
Set-StrictMode -Version Latest

function Get-Visa2014PreviewCatalogNames {
    param(
        [Parameter(Mandatory)]
        [string]$PreviewPath,

        [string]$ValueColumn = 'B',

        [string]$ImportActionColumn = 'A'
    )

    return Get-Visa2014PreviewCatalogValues -PreviewPath $PreviewPath -ValueColumn $ValueColumn -ImportActionColumn $ImportActionColumn
}

function Get-Visa2014PreviewCatalogValues {
    param(
        [Parameter(Mandatory)]
        [string]$PreviewPath,

        [string]$ValueColumn = 'B',

        [string]$ImportActionColumn = 'A'
    )

    if (-not (Test-Path -LiteralPath $PreviewPath)) {
        throw "Preview file not found: $PreviewPath"
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("visa2014-preview-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    try {
        [IO.Compression.ZipFile]::ExtractToDirectory($PreviewPath, $tempDir)
        $sheetPath = Join-Path $tempDir 'xl\worksheets\sheet1.xml'
        if (-not (Test-Path -LiteralPath $sheetPath)) {
            throw "Missing sheet1.xml in preview workbook: $PreviewPath"
        }

        [xml]$doc = Get-Content -LiteralPath $sheetPath -Encoding UTF8
        $ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
        $ns.AddNamespace('m', 'http://schemas.openxmlformats.org/spreadsheetml/2006/main')

        $names = New-Object System.Collections.Generic.List[string]
        foreach ($row in $doc.SelectNodes('//m:sheetData/m:row', $ns)) {
            $cells = @{}
            foreach ($cell in $row.SelectNodes('m:c', $ns)) {
                $col = ($cell.r -replace '\d+', '')
                $text = $null
                if ($cell.t -eq 'inlineStr') {
                    $text = $cell.is.t
                }
                elseif ($cell.InnerText) {
                    $text = $cell.InnerText
                }
                if ($null -ne $text) {
                    $cells[$col] = [string]$text
                }
            }

            if ($cells.ContainsKey($ImportActionColumn) -and $cells[$ImportActionColumn] -eq 'import' -and $cells.ContainsKey($ValueColumn)) {
                $name = $cells[$ValueColumn].Trim()
                if ($name) {
                    [void]$names.Add($name)
                }
            }
        }

        return ,$names.ToArray()
    }
    finally {
        Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Write-Visa2014NameCatalogJson {
    param(
        [Parameter(Mandatory)]
        [string[]]$Names,

        [Parameter(Mandatory)]
        [string]$OutputPath,

        [string]$SeedPath
    )

    $byKey = [ordered]@{}
    foreach ($name in $Names) {
        $key = ($name.ToLowerInvariant() -replace '\s+', ' ').Trim()
        if (-not $byKey.Contains($key)) {
            $byKey[$key] = $name
        }
    }

    if ($SeedPath -and (Test-Path -LiteralPath $SeedPath)) {
        $seed = Get-Content -LiteralPath $SeedPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($row in $seed.rows) {
            if (-not $row.Name) { continue }
            $key = ($row.Name.ToLowerInvariant() -replace '\s+', ' ').Trim()
            if (-not $byKey.Contains($key)) {
                $byKey[$key] = [string]$row.Name
            }
        }
    }

    $rows = @($byKey.Values | Sort-Object)
    $payload = [ordered]@{
        rows = @($rows | ForEach-Object { [ordered]@{ Name = $_ } })
    }

    $json = $payload | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText($OutputPath, $json, [System.Text.UTF8Encoding]::new($false))
    return $rows.Count
}

function Write-Visa2014SiteCatalogJsonFromPreview {
    param(
        [Parameter(Mandatory)]
        [string]$PreviewPath,

        [Parameter(Mandatory)]
        [string]$OutputPath,

        [ValidateSet('FullAddress', 'Name')]
        [string]$ScalarProperty = 'FullAddress',

        [string]$ValueColumn = 'B',
        [string]$RegionColumn = 'D',
        [string]$CityColumn = 'E'
    )

    if (-not (Test-Path -LiteralPath $PreviewPath)) {
        throw "Preview file not found: $PreviewPath"
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("visa2014-preview-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    try {
        [IO.Compression.ZipFile]::ExtractToDirectory($PreviewPath, $tempDir)
        $sheetPath = Join-Path $tempDir 'xl\worksheets\sheet1.xml'
        [xml]$doc = Get-Content -LiteralPath $sheetPath -Encoding UTF8
        $ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
        $ns.AddNamespace('m', 'http://schemas.openxmlformats.org/spreadsheetml/2006/main')

        $byKey = [ordered]@{}
        foreach ($row in $doc.SelectNodes('//m:sheetData/m:row', $ns)) {
            $cells = @{}
            foreach ($cell in $row.SelectNodes('m:c', $ns)) {
                $col = ($cell.r -replace '\d+', '')
                $text = $null
                if ($cell.t -eq 'inlineStr') { $text = $cell.is.t }
                elseif ($cell.InnerText) { $text = $cell.InnerText }
                if ($null -ne $text) { $cells[$col] = [string]$text }
            }
            if (-not $cells.ContainsKey('A') -or $cells['A'] -ne 'import') { continue }
            if (-not $cells.ContainsKey($ValueColumn)) { continue }

            $scalar = $cells[$ValueColumn].Trim()
            $region = if ($cells.ContainsKey($RegionColumn)) { $cells[$RegionColumn].Trim() } else { '' }
            $city = if ($cells.ContainsKey($CityColumn)) { $cells[$CityColumn].Trim() } else { '' }
            if (-not $scalar -or -not $region -or -not $city) { continue }

            $key = (($region + '|' + $city + '|' + $scalar).ToLowerInvariant() -replace '\s+', ' ').Trim()
            if (-not $byKey.Contains($key)) {
                $rowObj = [ordered]@{ Region = $region; City = $city }
                $rowObj[$ScalarProperty] = $scalar
                $byKey[$key] = $rowObj
            }
        }

        $rows = @($byKey.Values | Sort-Object { $_.Region }, { $_.City }, { $_.$ScalarProperty })
        $payload = [ordered]@{ rows = $rows }
        $json = $payload | ConvertTo-Json -Depth 4
        [System.IO.File]::WriteAllText($OutputPath, $json, [System.Text.UTF8Encoding]::new($false))
        return $rows.Count
    }
    finally {
        Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Write-Visa2014FullAddressCatalogJson {
    param(
        [Parameter(Mandatory)]
        [string[]]$FullAddresses,

        [Parameter(Mandatory)]
        [string]$OutputPath,

        [string]$SeedPath
    )

    $byKey = [ordered]@{}
    foreach ($line in $FullAddresses) {
        $key = ($line.ToLowerInvariant() -replace '\s+', ' ').Trim()
        if (-not $byKey.Contains($key)) {
            $byKey[$key] = $line
        }
    }

    if ($SeedPath -and (Test-Path -LiteralPath $SeedPath)) {
        $seed = Get-Content -LiteralPath $SeedPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($row in $seed.rows) {
            if (-not $row.FullAddress) { continue }
            $key = ($row.FullAddress.ToLowerInvariant() -replace '\s+', ' ').Trim()
            if (-not $byKey.Contains($key)) {
                $byKey[$key] = [string]$row.FullAddress
            }
        }
    }

    $rows = @($byKey.Values | Sort-Object)
    $payload = [ordered]@{
        rows = @($rows | ForEach-Object { [ordered]@{ FullAddress = $_ } })
    }

    $json = $payload | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText($OutputPath, $json, [System.Text.UTF8Encoding]::new($false))
    return $rows.Count
}
