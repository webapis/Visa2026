#Requires -Version 5.1
<#
.SYNOPSIS
  Query ApplicationRuntimeLog on the IIS server and emit inbox JSON documents (stdout).

.PARAMETER Profile
  Production, Staging, or Demo.

.PARAMETER SinceMinutes
  Look back window (default 60).

.PARAMETER Limit
  Max rows (default 50).

.NOTES
  Run on the Windows Server (localhost SQL). Consumed by Pull-Visa2026RuntimeErrorsRemote.ps1.
#>
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Production",

    [int]$SinceMinutes = 60,
    [int]$Limit = 50,

    [ValidateSet("Error", "Warning", "Critical")]
    [string]$MinLevel = "Error"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

if ($SinceMinutes -le 0) { $SinceMinutes = 60 }
if ($Limit -le 0) { $Limit = 50 }

$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile
$appSettingsPath = Join-Path $ctx.PublishPath "appsettings.Production.json"
if (-not (Test-Path -LiteralPath $appSettingsPath)) {
    throw "Missing $appSettingsPath"
}

$cfg = Get-Content -LiteralPath $appSettingsPath -Raw | ConvertFrom-Json
$connectionString = $cfg.ConnectionStrings.DefaultConnection
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "DefaultConnection missing in $appSettingsPath"
}

$minSeverity = switch ($MinLevel) {
    "Warning" { 0 }
    "Error" { 1 }
    "Critical" { 2 }
    default { 1 }
}

$sinceUtc = (Get-Date).ToUniversalTime().AddMinutes(-1 * $SinceMinutes)

Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

try {
    $command = $connection.CreateCommand()
    $command.CommandText = @"
SELECT TOP (@Limit)
    ID,
    OccurredAtUtc,
    Severity,
    ResolutionStatus,
    ErrorCode,
    Category,
    Message,
    ExceptionType,
    StackTrace,
    UserName,
    CorrelationId,
    RequestPath,
    MachineName,
    DeploymentEnvironment,
    ApplicationVersion,
    RelatedBatchId,
    SentryEventId
FROM ApplicationRuntimeLogs
WHERE GCRecord = 0
  AND OccurredAtUtc >= @SinceUtc
  AND Severity >= @MinSeverity
  AND ResolutionStatus IN (0, 1, 2)
ORDER BY OccurredAtUtc DESC
"@
    [void]$command.Parameters.AddWithValue("@Limit", $Limit)
    [void]$command.Parameters.AddWithValue("@SinceUtc", $sinceUtc)
    [void]$command.Parameters.AddWithValue("@MinSeverity", $minSeverity)

    $reader = $command.ExecuteReader()
    $documents = @()
    $writtenAtUtc = (Get-Date).ToUniversalTime().ToString("o")

    while ($reader.Read()) {
        $relatedBatchId = $null
        if (-not $reader.IsDBNull($reader.GetOrdinal("RelatedBatchId"))) {
            $relatedBatchId = $reader.GetGuid($reader.GetOrdinal("RelatedBatchId")).ToString("D")
        }

        $documents += [ordered]@{
            id = $reader.GetGuid($reader.GetOrdinal("ID")).ToString("D")
            occurredAtUtc = $reader.GetDateTime($reader.GetOrdinal("OccurredAtUtc")).ToUniversalTime().ToString("o")
            severity = [int]$reader.GetValue($reader.GetOrdinal("Severity"))
            resolutionStatus = [int]$reader.GetValue($reader.GetOrdinal("ResolutionStatus"))
            errorCode = if ($reader.IsDBNull($reader.GetOrdinal("ErrorCode"))) { $null } else { $reader.GetString($reader.GetOrdinal("ErrorCode")) }
            category = if ($reader.IsDBNull($reader.GetOrdinal("Category"))) { $null } else { $reader.GetString($reader.GetOrdinal("Category")) }
            message = if ($reader.IsDBNull($reader.GetOrdinal("Message"))) { $null } else { $reader.GetString($reader.GetOrdinal("Message")) }
            exceptionType = if ($reader.IsDBNull($reader.GetOrdinal("ExceptionType"))) { $null } else { $reader.GetString($reader.GetOrdinal("ExceptionType")) }
            stackTrace = if ($reader.IsDBNull($reader.GetOrdinal("StackTrace"))) { $null } else { $reader.GetString($reader.GetOrdinal("StackTrace")) }
            userName = if ($reader.IsDBNull($reader.GetOrdinal("UserName"))) { $null } else { $reader.GetString($reader.GetOrdinal("UserName")) }
            correlationId = if ($reader.IsDBNull($reader.GetOrdinal("CorrelationId"))) { $null } else { $reader.GetString($reader.GetOrdinal("CorrelationId")) }
            requestPath = if ($reader.IsDBNull($reader.GetOrdinal("RequestPath"))) { $null } else { $reader.GetString($reader.GetOrdinal("RequestPath")) }
            machineName = if ($reader.IsDBNull($reader.GetOrdinal("MachineName"))) { $null } else { $reader.GetString($reader.GetOrdinal("MachineName")) }
            deploymentEnvironment = [int]$reader.GetValue($reader.GetOrdinal("DeploymentEnvironment"))
            applicationVersion = if ($reader.IsDBNull($reader.GetOrdinal("ApplicationVersion"))) { $null } else { $reader.GetString($reader.GetOrdinal("ApplicationVersion")) }
            relatedBatchId = $relatedBatchId
            sentryEventId = if ($reader.IsDBNull($reader.GetOrdinal("SentryEventId"))) { $null } else { $reader.GetString($reader.GetOrdinal("SentryEventId")) }
            writtenAtUtc = $writtenAtUtc
            sourceSlot = $ctx.Profile
            sourceDatabase = $ctx.DbName
        }
    }

    $reader.Close()

    [pscustomobject]@{
        profile = $ctx.Profile
        database = $ctx.DbName
        sinceUtc = $sinceUtc.ToString("o")
        queriedCount = $documents.Count
        documents = $documents
    } | ConvertTo-Json -Depth 8 -Compress
}
finally {
    $connection.Close()
}
