#Requires -Version 5.1
<#
.SYNOPSIS
  Build appsettings.Production.json from slot env file (PostgreSQL only).

.PARAMETER PublishPath
  IIS site folder.

.PARAMETER EnvFile
  Source env file (must set EFCORE_PROVIDER=Postgres and PG_*).
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "",

    [string]$PublishPath = "",
    [string]$EnvFile = "",

    # Obsolete: Visa2026 is PostgreSQL-only. Kept so older callers do not fail on unknown params.
    [string]$SqlServer = "",
    [int]$SqlPort = 0
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

if (-not [string]::IsNullOrWhiteSpace($SqlServer) -or $SqlPort -gt 0) {
    Write-Warning "SqlServer/SqlPort parameters are ignored - Visa2026 uses PostgreSQL only."
}

$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile -PublishPath $PublishPath -EnvFile $EnvFile
$PublishPath = $ctx.PublishPath
$EnvFile = $ctx.EnvFile
$dataProtectionKeysPath = $ctx.DataProtectionKeysPath
$defaultDbName = $ctx.DbName

function Read-DotEnvMap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
    Read-Visa2026DotEnvMap -Path $Path
}

$envMap = Read-DotEnvMap $EnvFile
$devexpressKey = $envMap["DEVEXPRESS_LICENSEKEY"]
$dbName = if ($envMap.ContainsKey("DB_NAME") -and -not [string]::IsNullOrWhiteSpace($envMap["DB_NAME"])) { $envMap["DB_NAME"] } else { $defaultDbName }
$efCoreProvider = if ($envMap.ContainsKey("EFCORE_PROVIDER")) { $envMap["EFCORE_PROVIDER"].Trim() } else { "" }

if ([string]::IsNullOrWhiteSpace($devexpressKey)) { throw "DEVEXPRESS_LICENSEKEY missing in $EnvFile" }

$isPostgres = $efCoreProvider -match '^(?i)Postgres(ql)?$'
if (-not $isPostgres) {
    throw "Visa2026 requires EFCORE_PROVIDER=Postgres in $EnvFile (SQL Server Express is not supported)."
}

$pgHost = if ($envMap.ContainsKey("PG_HOST") -and -not [string]::IsNullOrWhiteSpace($envMap["PG_HOST"])) { $envMap["PG_HOST"].Trim() } else { "localhost" }
$pgPort = if ($envMap.ContainsKey("PG_PORT") -and -not [string]::IsNullOrWhiteSpace($envMap["PG_PORT"])) { $envMap["PG_PORT"].Trim() } else { "5432" }
$pgUser = if ($envMap.ContainsKey("PG_USER") -and -not [string]::IsNullOrWhiteSpace($envMap["PG_USER"])) { $envMap["PG_USER"].Trim() } else { "postgres" }
$pgPassword = if ($envMap.ContainsKey("PG_PASSWORD") -and -not [string]::IsNullOrWhiteSpace($envMap["PG_PASSWORD"])) {
    $envMap["PG_PASSWORD"]
} else {
    throw "PG_PASSWORD missing in $EnvFile"
}
if ([string]::IsNullOrWhiteSpace($dbName)) { throw "DB_NAME missing in $EnvFile" }

# Persist Security Info=True required for XAF + Npgsql (DX doc 404290).
$connectionString = "Host=$pgHost;Port=$pgPort;Database=$dbName;Username=$pgUser;Password=$pgPassword;Persist Security Info=True;EFCoreProvider=Postgres"
Write-Host "Provider: PostgreSQL ($pgHost`:$pgPort / $dbName)" -ForegroundColor Cyan

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
    ApplicationRuntimeLog = @{
        Enabled = $true
        ReportUiErrors = $true
        PersistWarnings = $false
        MinLevel = "Error"
        QueueCapacity = 1000
        RetentionDays = 90
        RetentionCleanupIntervalHours = 24
        RetentionBatchSize = 500
        RealtimeNotifyEnabled = $true
        RealtimeNotifyMinLevel = "Error"
        CursorBridgeEnabled = $false
        CursorBridgeLocalDevOnly = $true
        CursorBridgeMinLevel = "Error"
    }
    Sentry = @{
        Enabled = $false
        Dsn = ""
        BridgeRuntimeLog = $true
        BridgeWarnings = $false
        TracesSampleRate = 0.0
        SendDefaultPii = $false
    }
    DeploymentEnvironment = @{
        Slot = $ctx.Profile
        ShowOnLoginPage = $true
    }
    ImportHistory = @{
        RootPath = switch ($ctx.Profile) {
            'Demo' { 'C:\visa2026-sync-demo\history' }
            'Staging' { 'C:\visa2026-sync-staging\history' }
            default { 'C:\visa2026-sync\history' }
        }
    }
    MaglumatCsvExport = @{
        ApiKey = if ($envMap.ContainsKey("MAGLUMAT_CSV_API_KEY")) { $envMap["MAGLUMAT_CSV_API_KEY"] } else { "" }
    }
}

$templateStagingEnabled = Resolve-Visa2026TemplateEditStagingEnabled -EnvFile $EnvFile -DefaultEnabled:$true
$httpsEnabled = Resolve-Visa2026HttpsEnabled -EnvFile $EnvFile
$httpsPort = Resolve-Visa2026HttpsPort -EnvFile $EnvFile

if ($httpsEnabled) {
    $config["Https"] = @{
        Port = $httpsPort
    }
    $config["FileUpload"] = @{ MaxRequestBodyBytes = 52428800 }
}
else {
    $config["FileUpload"] = @{ MaxRequestBodyBytes = 10485760 }
}

if ($templateStagingEnabled) {
    $config["TemplateEditStaging"] = @{
        Enabled = $true
        LocalFolderSubfolderName = "Visa2026\TemplateEdit"
        FileNamePattern = "{safeName}{extension}"
        AutoExtractValidateOnImport = $true
        MaxFileSizeBytes = 52428800
    }
}
else {
    $config["TemplateEditStaging"] = @{
        Enabled = $false
        LocalFolderSubfolderName = "Visa2026\TemplateEdit"
        FileNamePattern = "{safeName}{extension}"
        AutoExtractValidateOnImport = $true
        MaxFileSizeBytes = 52428800
    }
}

$outPath = Join-Path $PublishPath "appsettings.Production.json"
$config | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outPath -Encoding UTF8

New-Item -ItemType Directory -Force -Path $dataProtectionKeysPath | Out-Null

$poolEnv = @{
    ASPNETCORE_ENVIRONMENT = "Production"
    DEVEXPRESS_LICENSEKEY = $devexpressKey
    ASPNETCORE_DATA_PROTECTION_KEYS = $dataProtectionKeysPath
}

Write-Host "Wrote $outPath" -ForegroundColor Green
Write-Host "Slot: $($ctx.Profile)  Database: $dbName on ${pgHost}:$pgPort (PostgreSQL)"
if ($templateStagingEnabled) {
    Write-Host "Template staging: enabled (local sandbox)" -ForegroundColor Green
    if (-not $httpsEnabled) {
        Write-Host "  WARNING: Local sandbox requires HTTPS (set HTTPS_ENABLED=true and run Enable-Visa2026IisHttps.ps1)." -ForegroundColor Yellow
    }
}
else {
    Write-Host "Template staging: disabled (set TEMPLATE_EDIT_STAGING_ENABLED=true in $EnvFile)" -ForegroundColor Yellow
}

Write-Host "Set app pool environment variables:" -ForegroundColor Yellow
$poolEnv.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key)=***" }

$poolEnv | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PublishPath "iis-apppool-env.json") -Encoding UTF8