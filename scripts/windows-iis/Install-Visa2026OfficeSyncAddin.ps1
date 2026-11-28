#Requires -Version 5.1
<#
.SYNOPSIS
  Install the Visa2026 Sync button into Word and Excel for this Windows user.

.DESCRIPTION
  The button is an Office application add-in, not a control stored inside each template.
  It appears on the Home tab for every open document: templates already on this PC and
  templates added later. Office does not allow a third-party button on the Save control.
#>
[CmdletBinding()]
param(
    [switch]$Compile
)

$ErrorActionPreference = "Stop"
$destDir = Join-Path $env:LOCALAPPDATA "Visa2026\TemplateEditHost"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
$dll = Join-Path $destDir "Visa2026.OfficeSync.dll"
$shipped = Join-Path $PSScriptRoot "office-sync\Visa2026.OfficeSync.dll"

if (-not $Compile -and (Test-Path -LiteralPath $shipped)) {
    Copy-Item -LiteralPath $shipped -Destination $dll -Force
}
else {
    $source = Join-Path $PSScriptRoot "office-sync\Connect.cs"
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Visa2026.OfficeSync.dll was not found next to this script."
    }

    $refRoot = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
    if (-not (Test-Path -LiteralPath $refRoot)) {
        $refRoot = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2"
    }
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (-not (Test-Path -LiteralPath $csc)) {
        throw "The .NET Framework compiler was not found."
    }

    $extensibility = "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\PublicAssemblies\extensibility.dll"
    if (-not (Test-Path -LiteralPath $extensibility)) {
        throw "extensibility.dll was not found. Visual Studio is required only when compiling the add-in."
    }

    & $csc /nologo /t:library "/out:$dll" /codepage:65001 `
        "/r:$refRoot\System.Net.Http.dll" `
        "/link:$extensibility" `
        $source
    if ($LASTEXITCODE -ne 0) {
        throw "Office Sync add-in compile failed."
    }

    $shippedDir = Split-Path -Parent $shipped
    if (Test-Path -LiteralPath $shippedDir) {
        Copy-Item -LiteralPath $dll -Destination $shipped -Force
    }
}

$guid = "{A7C3E1B2-4D58-4F0A-9C21-6B8E5D4A1F30}"
$codeBase = "file:///" + ($dll -replace "\\", "/")
$classRoot = "HKCU:\Software\Classes"
$clsid = Join-Path $classRoot "CLSID\$guid"
$inproc = Join-Path $clsid "InprocServer32"
$prog = Join-Path $classRoot "Visa2026.OfficeSync.Connect"

New-Item -Path $prog -Force | Out-Null
Set-Item -LiteralPath $prog -Value "Visa2026 Office Sync"
$progClsid = Join-Path $prog "CLSID"
New-Item -Path $progClsid -Force | Out-Null
Set-Item -LiteralPath $progClsid -Value $guid
$clsidProg = Join-Path $clsid "ProgId"
New-Item -Path $clsidProg -Force | Out-Null
Set-Item -LiteralPath $clsidProg -Value "Visa2026.OfficeSync.Connect"

function Set-InprocValues {
    param([string]$Path)
    New-Item -Path $Path -Force | Out-Null
    Set-Item -LiteralPath $Path -Value "mscoree.dll"
    New-ItemProperty -LiteralPath $Path -Name "Assembly" -Value "Visa2026.OfficeSync, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $Path -Name "Class" -Value "Visa2026.OfficeSync.Connect" -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $Path -Name "CodeBase" -Value $codeBase -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $Path -Name "RuntimeVersion" -Value "v4.0.30319" -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $Path -Name "ThreadingModel" -Value "Both" -PropertyType String -Force | Out-Null
}

Set-InprocValues $inproc
Set-InprocValues (Join-Path $inproc "1.0.0.0")
$category = Join-Path $clsid "Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}"
New-Item -Path $category -Force | Out-Null

foreach ($app in @("Word", "Excel")) {
    $addin = "HKCU:\Software\Microsoft\Office\$app\Addins\Visa2026.OfficeSync.Connect"
    New-Item -Path $addin -Force | Out-Null
    New-ItemProperty -LiteralPath $addin -Name "FriendlyName" -Value "Visa2026 Sync" -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $addin -Name "Description" -Value "Sync button on the Home tab for every Visa2026 template." -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $addin -Name "LoadBehavior" -Value 3 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -LiteralPath $addin -Name "CommandLineSafe" -Value 0 -PropertyType DWord -Force | Out-Null
}

Write-Host "Visa2026 Sync is installed for Word and Excel."
Write-Host "Close Word and Excel, then open a template. Home tab, left side: Sync."
Write-Host $dll
