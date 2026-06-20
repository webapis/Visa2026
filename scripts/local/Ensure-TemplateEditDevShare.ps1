#Requires -Version 5.1
<#
.SYNOPSIS
  Verifies the Resminamalar template-edit UNC share is reachable.

.DESCRIPTION
  Template staging uses UNC only (\\server\share). Configure TemplateEditStaging:StagingRootUnc
  in appsettings. Do not use local drive paths or project TemplateEdit folders.

.EXAMPLE
  .\scripts\local\Ensure-TemplateEditDevShare.ps1

.EXAMPLE
  .\scripts\local\Ensure-TemplateEditDevShare.ps1 -UncPath '\\127.0.0.1\Visa2026TemplateEdit'
#>
[CmdletBinding()]
param(
    [string]$UncPath = '\\127.0.0.1\Visa2026TemplateEdit'
)

$ErrorActionPreference = 'Stop'

if (-not $UncPath.TrimStart().StartsWith('\\')) {
    Write-Error 'UncPath must be a UNC path (\\server\share).'
}

Write-Host "Checking UNC share: $UncPath"

if (Test-Path -LiteralPath $UncPath) {
    Write-Host 'OK - share is reachable from this account.'
}
else {
    Write-Warning "Cannot access $UncPath. Create the Windows share and grant Modify to the app account and officers."
}

$backslash = [char]92
$jsonUnc = -join ($UncPath.ToCharArray() | ForEach-Object {
    if ($_ -eq $backslash) { "$backslash$backslash" } else { $_ }
})

Write-Host ''
Write-Host 'appsettings.Development.json example:'
Write-Host '  "TemplateEditStaging": {'
Write-Host '    "Enabled": true,'
Write-Host ('    "StagingRootUnc": "' + $jsonUnc + '"')
Write-Host '  }'
Write-Host ''
Write-Host 'Restart the Blazor host after changing config.'
