#Requires -Version 5.1
<#
.SYNOPSIS
  Officer-PC opener for Visa2026 Edit template.

.DESCRIPTION
  Started by the visa2026edit: protocol (registered by Set-Visa2026TemplateEditOfficeTrust.ps1).
  Writes the template under %LOCALAPPDATA%\Visa2026\TemplateEdit and opens Word or Excel.
  Sync reads that folder and posts changed files back to the Visa2026 server.

  The production server is a different machine. This script runs on the PC where Edit was clicked.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$LaunchUrl
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Write-RibbonHost {
    param(
        [string]$Folder,
        [string]$ServerBase,
        [string]$RibbonToken
    )

    if ([string]::IsNullOrWhiteSpace($Folder) -or [string]::IsNullOrWhiteSpace($ServerBase) -or [string]::IsNullOrWhiteSpace($RibbonToken)) {
        return
    }

    $path = Join-Path $Folder "ribbon-host.json"
    @{
        serverBase = $ServerBase.TrimEnd("/")
        ribbonToken = $RibbonToken
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $path -Encoding UTF8
}

function Write-EditLog {
    param([string]$Message)
    try {
        $dir = Join-Path $env:LOCALAPPDATA "Visa2026\TemplateEdit"
        if (-not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
        }

        $line = (Get-Date).ToString("s") + " " + $Message
        Add-Content -LiteralPath (Join-Path $dir "edit-host.log") -Value $line -Encoding UTF8
    }
    catch {
    }
}

function Get-QueryMap {
    param([string]$Url)
    $map = @{}
    $q = $Url.IndexOf("?")
    if ($q -lt 0) {
        return $map
    }

    foreach ($pair in $Url.Substring($q + 1).Split("&")) {
        $kv = $pair.Split("=", 2)
        if ($kv.Length -eq 2) {
            $map[$kv[0]] = [Uri]::UnescapeDataString($kv[1])
        }
    }

    return $map
}

function Get-Sha256Hex {
    param([byte[]]$Bytes)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($Bytes)
    }
    finally {
        $sha.Dispose()
    }

    return ([BitConverter]::ToString($hash)).Replace("-", "")
}

function Get-TemplateFolder {
    param([string]$Relative)
    if ([string]::IsNullOrWhiteSpace($Relative)) {
        $Relative = "Visa2026\TemplateEdit"
    }

    $Relative = $Relative.Replace("/", "\")
    if ($Relative -match "\.\." -or $Relative -match "^[A-Za-z]:") {
        throw "Template folder is not allowed."
    }

    $folder = Join-Path $env:LOCALAPPDATA $Relative
    if (-not (Test-Path -LiteralPath $folder)) {
        New-Item -ItemType Directory -Force -Path $folder | Out-Null
    }

    return $folder
}

function Send-Complete {
    param(
        [string]$Base,
        [string]$Token,
        [hashtable]$Body
    )

    $uri = "$Base/api/user-report-templates/staging/desktop/$Token/complete"
    $json = $Body | ConvertTo-Json -Depth 6 -Compress
    Invoke-RestMethod -Method Post -Uri $uri -Body $json -ContentType "application/json; charset=utf-8" | Out-Null
}

function Open-TemplateFile {
    param([string]$Path)
    Start-Process -FilePath $Path | Out-Null
}

try {
    if ([string]::IsNullOrWhiteSpace($LaunchUrl)) {
        throw "Launch URL is missing."
    }

    $LaunchUrl = $LaunchUrl.Trim().Trim('"')
    $mode = "open"
    if ($LaunchUrl -match "visa2026edit:(?://)?(open|sync)") {
        $mode = $Matches[1]
    }

    $query = Get-QueryMap $LaunchUrl
    $token = [string]$query["token"]
    $base = ([string]$query["base"]).TrimEnd("/")
    if ([string]::IsNullOrWhiteSpace($token) -or [string]::IsNullOrWhiteSpace($base)) {
        throw "Launch URL is incomplete."
    }

    if ($base -notmatch "^https://" -and $base -notmatch "^http://(localhost|127\.0\.0\.1)(:[0-9]+)?$") {
        throw "Server address is not allowed."
    }

    $folder = Get-TemplateFolder ([string]$query["folder"])
    if ($mode -eq "sync") {
        $uploads = New-Object System.Collections.Generic.List[object]
        Add-Type -AssemblyName System.Net.Http
        $metaFiles = @(Get-ChildItem -LiteralPath $folder -Filter "*.meta.json" -File -ErrorAction SilentlyContinue)
        foreach ($metaFile in $metaFiles) {
            $meta = Get-Content -LiteralPath $metaFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            $templateId = [string]$meta.templateId
            $fileName = [string]$meta.documentFileName
            if ([string]::IsNullOrWhiteSpace($templateId) -or [string]::IsNullOrWhiteSpace($fileName)) {
                continue
            }

            $docPath = Join-Path $folder $fileName
            if (-not (Test-Path -LiteralPath $docPath)) {
                $uploads.Add(@{
                    templateId = $templateId
                    status = "SkippedNotFound"
                    fileName = $fileName
                    displayName = [string]$meta.templateName
                })
                continue
            }

            $bytes = [System.IO.File]::ReadAllBytes($docPath)
            $hash = Get-Sha256Hex $bytes
            $uploadUri = "$base/api/user-report-templates/staging/desktop/$token/upload?templateId=$templateId"
            $client = New-Object System.Net.Http.HttpClient
            try {
                $content = New-Object System.Net.Http.MultipartFormDataContent
                $fileContent = New-Object System.Net.Http.ByteArrayContent (,$bytes)
                $content.Add($fileContent, "file", $fileName)
                $response = $client.PostAsync($uploadUri, $content).Result
                $text = $response.Content.ReadAsStringAsync().Result
                $parsed = $text | ConvertFrom-Json
                $status = [string]$parsed.status
                if ($status -eq "Imported" -or $status -eq "SkippedUnchanged") {
                    $meta.sourceContentHashSha256 = $hash
                    $meta.lastImportedContentHashSha256 = $hash
                    ($meta | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath $metaFile.FullName -Encoding UTF8
                }

                $uploads.Add(@{
                    templateId = $templateId
                    status = $(if ($status) { $status } else { "Failed" })
                    errorMessage = [string]$parsed.errorMessage
                    fileName = $fileName
                    displayName = [string]$meta.templateName
                })
            }
            finally {
                $client.Dispose()
            }
        }

        $imported = @($uploads | Where-Object { $_.status -eq "Imported" }).Count
        $unchanged = @($uploads | Where-Object { $_.status -eq "SkippedUnchanged" }).Count
        $missing = @($uploads | Where-Object { $_.status -eq "SkippedNotFound" }).Count
        $failed = @($uploads | Where-Object { $_.status -eq "Failed" }).Count
        Send-Complete -Base $base -Token $token -Body @{
            importedCount = $imported
            skippedUnchangedCount = $unchanged
            skippedNotFoundCount = $missing
            failedCount = $failed
            cancelled = $false
            uploads = $uploads
        }
        return
    }

    $payload = Invoke-RestMethod -Method Get -Uri "$base/api/user-report-templates/staging/desktop/$token"
    $fileName = [string]$payload.documentFileName
    $docPath = Join-Path $folder $fileName
    $sourceHash = [string]$payload.sourceContentHashSha256
    $code = "exported"
    $opened = $false
    if (Test-Path -LiteralPath $docPath) {
        $existingHash = Get-Sha256Hex ([System.IO.File]::ReadAllBytes($docPath))
        if (-not [string]::IsNullOrWhiteSpace($sourceHash) -and $existingHash -ne $sourceHash) {
            $code = "needsSync"
        }
    }
    else {
        $bytes = [Convert]::FromBase64String([string]$payload.fileBase64)
        [System.IO.File]::WriteAllBytes($docPath, $bytes)
        $metaPath = $docPath + ".meta.json"
        @{
            templateId = [string]$payload.templateId
            templateName = [string]$payload.displayName
            documentFileName = $fileName
            outputFormat = [string]$payload.outputFormat
            sourceContentHashSha256 = $sourceHash
            exportedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
            serverBase = $base.TrimEnd("/")
            ribbonToken = [string]$payload.ribbonToken
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metaPath -Encoding UTF8
    }

    Write-RibbonHost $folder $base ([string]$payload.ribbonToken)
    Open-TemplateFile $docPath
    $opened = $true
    Send-Complete -Base $base -Token $token -Body @{
        code = $code
        templateId = [string]$payload.templateId
        displayName = [string]$payload.displayName
        fileName = $fileName
        folderName = $folder
        copyPath = $docPath
        opened = $opened
    }
}
catch {
    Write-EditLog $_.Exception.Message
    if ($base -and $token) {
        try {
            Send-Complete -Base $base -Token $token -Body @{
                code = "exportFailed"
                error = $_.Exception.Message
            }
        }
        catch {
            Write-EditLog $_.Exception.Message
        }
    }
}
