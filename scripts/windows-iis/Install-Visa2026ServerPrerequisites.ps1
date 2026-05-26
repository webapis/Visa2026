#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install IIS, WebSockets, and .NET 8 Hosting Bundle on Windows Server for Visa2026.
#>
param(
    [switch]$SkipHostingBundle
)

$ErrorActionPreference = "Stop"

Write-Host "==> IIS features" -ForegroundColor Cyan
$features = @(
    "Web-Server",
    "Web-WebSockets",
    "Web-Common-Http",
    "Web-Default-Doc",
    "Web-Dir-Browsing",
    "Web-Http-Errors",
    "Web-Static-Content",
    "Web-Health",
    "Web-Http-Logging",
    "Web-Request-Monitor",
    "Web-Performance",
    "Web-Stat-Compression",
    "Web-Security",
    "Web-Filtering",
    "Web-App-Dev",
    "Web-Net-Ext45",
    "Web-ISAPI-Ext",
    "Web-ISAPI-Filter",
    "Web-Mgmt-Tools",
    "Web-Mgmt-Console"
)
$result = Install-WindowsFeature -Name $features -IncludeManagementTools
$failed = $result | Where-Object { $_.InstallState -eq "InstallFailed" }
if ($failed) {
    $failed | Format-Table Name, InstallState
    throw "One or more Windows features failed to install."
}

if (-not $SkipHostingBundle) {
    $bundleDir = "C:\visa2026-deploy\dotnet"
    New-Item -ItemType Directory -Force -Path $bundleDir | Out-Null
    $bundleExe = Join-Path $bundleDir "dotnet-hosting-8.0-win.exe"

    if (-not (Test-Path -LiteralPath $bundleExe)) {
        Write-Host "==> Download .NET 8 Hosting Bundle" -ForegroundColor Cyan
        $index = Invoke-RestMethod -Uri "https://dotnetcli.azureedge.net/dotnet/release-metadata/8.0/releases.json"
        $ver = $index."latest-runtime"
        $url = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/$ver/dotnet-hosting-$ver-win.exe"
        Write-Host "    $url"
        Invoke-WebRequest -Uri $url -OutFile $bundleExe -UseBasicParsing
    }

    Write-Host "==> Install Hosting Bundle (quiet)" -ForegroundColor Cyan
    $p = Start-Process -FilePath $bundleExe -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru
    if ($p.ExitCode -notin 0, 1638, 3010) {
        throw "Hosting bundle installer exit code: $($p.ExitCode)"
    }
    Write-Host "==> iisreset" -ForegroundColor Cyan
    iisreset | Out-Null
}

$dotnet = Get-ChildItem "C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App" -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending | Select-Object -First 1
if (-not $dotnet) {
    throw "Microsoft.AspNetCore.App 8.x not found after Hosting Bundle install."
}
Write-Host "ASP.NET Core runtime: $($dotnet.Name)" -ForegroundColor Green
Write-Host "Prerequisites OK." -ForegroundColor Green
