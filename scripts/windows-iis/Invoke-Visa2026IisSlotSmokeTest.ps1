#Requires -Version 5.1
<#
.SYNOPSIS
  HTTPS (or HTTP) LoginPage smoke test for one IIS slot.
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy")]
    [string]$Profile = "Production",

    [string]$HostName = "127.0.0.1"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$url = Get-Visa2026SlotSmokeLoginPageUrl -Profile $Profile -HostName $HostName

if ($url -like "https://*") {
    $curl = Join-Path $env:WINDIR "System32\curl.exe"
    if (Test-Path -LiteralPath $curl) {
        $code = & $curl -k -s -o NUL -w "%{http_code}" $url
        Write-Host $code
        if ($code -eq "200") { exit 0 }
        exit 1
    }
}

try {
    if ($url -like "https://*") {
        add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class Visa2026TrustAllCerts {
    public static bool Validate(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors errors) { return true; }
}
"@
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { [Visa2026TrustAllCerts]::Validate }
    }

    $code = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 180).StatusCode
    Write-Host $code
    exit 0
}
catch {
    Write-Host $_.Exception.Message
    exit 1
}
