#Requires -Version 5.1
<#
.SYNOPSIS
  Build appsettings.Production.json from C:\visa2026\.env.prod (on server).

.PARAMETER PublishPath
  IIS site folder (default C:\inetpub\visa2026).

.PARAMETER EnvFile
  Source env file (default C:\visa2026\.env.prod).

.PARAMETER SqlServer
  SQL Server host or host\instance (default localhost\SQLEXPRESS for native Express).

.PARAMETER SqlPort
  Optional port when not using a named instance (default: omit; use instance name).
#>
param(
    [string]$PublishPath = "C:\inetpub\visa2026",
    [string]$EnvFile = "C:\visa2026\.env.prod",
    [string]$SqlServer = "localhost\SQLEXPRESS",
    [int]$SqlPort = 0
)

$ErrorActionPreference = "Stop"

function Read-DotEnvMap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq "") { return }
        if ($line -match '^\s*([^#=]+)=(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            if ($v.Length -ge 2 -and $v.StartsWith('"') -and $v.EndsWith('"')) {
                $v = $v.Substring(1, $v.Length - 2)
            }
            $map[$k] = $v
        }
    }
    $map
}

$envMap = Read-DotEnvMap $EnvFile
$saPassword = $envMap["SA_PASSWORD"]
$devexpressKey = $envMap["DEVEXPRESS_LICENSEKEY"]
$dbName = if ($envMap.ContainsKey("DB_NAME")) { $envMap["DB_NAME"] } else { "Visa2026DbProd" }

if ([string]::IsNullOrWhiteSpace($saPassword)) { throw "SA_PASSWORD missing in $EnvFile" }
if ([string]::IsNullOrWhiteSpace($devexpressKey)) { throw "DEVEXPRESS_LICENSEKEY missing in $EnvFile" }

if ($SqlPort -gt 0) {
    $serverPart = if ($SqlServer -match '\\') { "$SqlServer,$SqlPort" } else { "$SqlServer,$SqlPort" }
}
else {
    $serverPart = $SqlServer
}
$connectionString = "Server=$serverPart;Database=$dbName;User Id=sa;Password=$saPassword;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False"

$jwtSecret = [guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N")

$config = @{
    ConnectionStrings = @{ DefaultConnection = $connectionString }
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.AspNetCore" = "Warning"
            DevExpress = "Information"
        }
    }
    AllowedHosts = "*"
    FileUpload = @{ MaxRequestBodyBytes = 10485760 }
    PdfSettings = @{ TemplatePath = "Resources/Visa_Application_TM_QR_08.pdf" }
    TempFileCleanupSettings = @{
        Enabled = $true
        CleanupIntervalHours = 24
        FileRetentionDays = 2
    }
    Authentication = @{
        Jwt = @{
            Issuer = "Visa2026"
            Audience = "Visa2026"
            IssuerSigningKey = $jwtSecret
        }
    }
    DevExpress = @{
        ExpressApp = @{
            Languages = "en-US;tr-TR;tk-TM;ru-RU"
            ShowLanguageSwitcher = $true
        }
    }
}

$outPath = Join-Path $PublishPath "appsettings.Production.json"
$config | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outPath -Encoding UTF8

# App pool environment (DevExpress + data protection; connection string in json)
$poolEnv = @{
    ASPNETCORE_ENVIRONMENT = "Production"
    DEVEXPRESS_LICENSEKEY = $devexpressKey
    ASPNETCORE_DATA_PROTECTION_KEYS = "C:\ProgramData\Visa2026\DataProtection-Keys"
}

Write-Host "Wrote $outPath" -ForegroundColor Green
Write-Host "Database: $dbName on $serverPart (sa)"
Write-Host "Set app pool environment variables:" -ForegroundColor Yellow
$poolEnv.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key)=***" }

# Export for caller / manual appcmd
$poolEnv | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PublishPath "iis-apppool-env.json") -Encoding UTF8
