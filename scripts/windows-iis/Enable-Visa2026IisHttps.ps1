#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Add HTTPS binding to a Visa2026 IIS slot (on-prem Windows Server).

.DESCRIPTION
  Creates or reuses a TLS certificate and binds https:// to the Visa2026 IIS site.
  Required for Resminamalar LocalFolder template editing (browser File System Access API).

.PARAMETER Profile
  Production, Staging, Demo, or Legacy (default Production).

.PARAMETER HttpsPort
  HTTPS port (default 443).

.PARAMETER DnsName
  Certificate DNS name (default: server host name).

.PARAMETER IpAddress
  Optional IP SAN for officers who browse by IP (e.g. 10.100.128.25).

.PARAMETER CertificateThumbprint
  Use an existing certificate from LocalMachine\My instead of creating self-signed.

.PARAMETER RedirectHttpToHttps
  Add IIS URL Rewrite rule to redirect HTTP to HTTPS for this site.

.EXAMPLE
  .\Enable-Visa2026IisHttps.ps1 -Profile Production -IpAddress 10.100.128.25

.NOTES
  Officers must trust the certificate (enterprise CA or import self-signed once).
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy")]
    [string]$Profile = "Production",

    [int]$HttpsPort = 443,
    [string]$DnsName = "",
    [string]$IpAddress = "",
    [string]$CertificateThumbprint = "",
    [switch]$RedirectHttpToHttps
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

Import-Module WebAdministration -ErrorAction Stop

$slot = Get-Visa2026IisSlotDefinition -Profile $Profile
$siteName = $slot.SiteName
$envFile = $slot.EnvFile

if ([string]::IsNullOrWhiteSpace($DnsName)) {
    $DnsName = [System.Net.Dns]::GetHostEntry('localhost').HostName
    if ($DnsName -match '\.') {
        $DnsName = $DnsName.Split('.')[0]
    }
    if ([string]::IsNullOrWhiteSpace($DnsName)) {
        $DnsName = $env:COMPUTERNAME
    }
}

if ([string]::IsNullOrWhiteSpace($IpAddress) -and (Test-Path -LiteralPath $envFile)) {
    $envMap = Read-Visa2026DotEnvMap -Path $envFile
    if ($envMap.ContainsKey('TEMPLATE_EDIT_UNC_HOST') -and $envMap['TEMPLATE_EDIT_UNC_HOST']) {
        $candidate = $envMap['TEMPLATE_EDIT_UNC_HOST'].Trim()
        if ($candidate -match '^\d{1,3}(\.\d{1,3}){3}$') {
            $IpAddress = $candidate
        }
    }
}

Write-Host ""
Write-Host "Visa2026 HTTPS binding - $($slot.Profile)" -ForegroundColor Cyan
Write-Host "  Site       : $siteName"
Write-Host "  HTTPS port : $HttpsPort"
Write-Host "  DNS name   : $DnsName"
if ($IpAddress) { Write-Host "  IP SAN     : $IpAddress" }

if (-not (Test-Path "IIS:\Sites\$siteName")) {
    throw "IIS site not found: $siteName"
}

$cert = $null
if ($CertificateThumbprint) {
    $cert = Get-ChildItem "Cert:\LocalMachine\My\$CertificateThumbprint" -ErrorAction SilentlyContinue
    if (-not $cert) {
        throw "Certificate not found in LocalMachine\My: $CertificateThumbprint"
    }
    Write-Host "  Using certificate thumbprint: $($cert.Thumbprint)" -ForegroundColor DarkGray
}
else {
    $sanParts = @("DNS=$DnsName", "DNS=localhost")
    if ($IpAddress) {
        $sanParts += "IPAddress=$IpAddress"
    }

    $san = ($sanParts -join "&")
    if ($PSCmdlet.ShouldProcess($DnsName, "Create self-signed certificate ($san)")) {
        $cert = New-SelfSignedCertificate `
            -DnsName $DnsName, "localhost" `
            -CertStoreLocation "Cert:\LocalMachine\My" `
            -FriendlyName "Visa2026-$($slot.Profile)-HTTPS" `
            -KeyExportPolicy Exportable `
            -NotAfter (Get-Date).AddYears(5) `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1", "2.5.29.17={text}$san")

        Write-Host "  Created self-signed certificate: $($cert.Thumbprint)" -ForegroundColor Green
        Write-Host "  Import this cert to officer PCs (Trusted Root) or deploy via enterprise CA." -ForegroundColor Yellow
    }
}

if ($PSCmdlet.ShouldProcess($siteName, "Bind HTTPS on port $HttpsPort")) {
    $binding = Get-WebBinding -Name $siteName -Protocol "https" -ErrorAction SilentlyContinue |
        Where-Object { $_.bindingInformation -like "*:${HttpsPort}:*" }

    if (-not $binding) {
        New-WebBinding -Name $siteName -Protocol "https" -Port $HttpsPort -IPAddress "*" | Out-Null
        $binding = Get-WebBinding -Name $siteName -Protocol "https" |
            Where-Object { $_.bindingInformation -like "*:${HttpsPort}:*" } |
            Select-Object -First 1
    }

    if (-not $binding) {
        throw "Failed to create HTTPS binding for $siteName on port $HttpsPort"
    }

    $binding.AddSslCertificate($cert.Thumbprint, "my")
    Write-Host "  HTTPS binding ready: https://localhost:$HttpsPort/LoginPage" -ForegroundColor Green
    if ($IpAddress) {
        Write-Host "  Officer URL (after trusting cert): https://${IpAddress}:$HttpsPort/LoginPage" -ForegroundColor Green
    }
}

if ($RedirectHttpToHttps) {
    if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
        Write-Warning "URL Rewrite module not detected; skip HTTP redirect rule."
    }
    else {
        $rewriteRoot = "system.webServer/rewrite/rules"
        $ruleName = "Visa2026-$($slot.Profile)-HttpToHttps"
        if ($PSCmdlet.ShouldProcess($siteName, "Add HTTP to HTTPS redirect rule")) {
            try {
                Add-WebConfigurationProperty -PSPath "IIS:\Sites\$siteName" -Filter $rewriteRoot -Name "." -Value @{
                    name = $ruleName
                    match = @{ url = "(.*)" }
                    conditions = @{
                        logicalGrouping = "MatchAny"
                        add = @(@{ input = "{HTTPS}"; pattern = "off" })
                    }
                    action = @{
                        type = "Redirect"
                        url = "https://{HTTP_HOST}/{R:1}"
                        redirectType = "Permanent"
                    }
                } -ErrorAction Stop
                Write-Host "  Added HTTP -> HTTPS redirect rule." -ForegroundColor Green
            }
            catch {
                Write-Warning "Could not add URL Rewrite rule (module missing or rule exists): $($_.Exception.Message)"
            }
        }
    }
}

return [PSCustomObject]@{
    Profile     = $slot.Profile
    SiteName    = $siteName
    HttpsPort   = $HttpsPort
    DnsName     = $DnsName
    IpAddress   = $IpAddress
    Thumbprint  = $cert.Thumbprint
}
