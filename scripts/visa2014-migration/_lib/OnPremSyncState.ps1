# Shared on-prem sync state queries (.15 VISA2015 vs .25 Visa2026DbProd).
# Dot-source from Compare-OnPremSyncState.ps1 and Watch-OnPremSyncState.ps1.

function Get-OnPremScalarRowDefinitions {
    @(
        @{ BO = 'Person'; L = 'SELECT COUNT(*) FROM dbo.Person WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM People'; Note = '' }
        @{ BO = 'Passport'; L = 'SELECT COUNT(*) FROM dbo.Passport pp INNER JOIN dbo.Person p ON pp.Person = p.Oid AND p.GCRecord IS NULL WHERE pp.GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM Passports WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'Visa'; L = 'SELECT COUNT(*) FROM dbo.Visa WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM Visas WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'Education'; L = 'SELECT COUNT(*) FROM dbo.Education WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM Educations WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'EmployeePositionHistory'; L = 'SELECT COUNT(*) FROM dbo.WorkHistoryOfEmployee WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM EmployeePositionHistories WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'EmployeeSalary'; L = 'SELECT COUNT(*) FROM dbo.Employee e INNER JOIN dbo.Person p ON p.Oid = e.Oid AND p.GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM EmployeeSalaries WHERE GCRecord IS NULL OR GCRecord = 0'; Note = 'legacy = Employee scope' }
        @{ BO = 'AddressOfResidence'; L = 'SELECT COUNT(*) FROM dbo.AddressOfResidence WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM AddressesOfResidence WHERE GCRecord IS NULL OR GCRecord = 0'; Note = 'prod may exceed legacy (PIA inference)' }
        @{ BO = 'MedicalRecord'; L = 'SELECT COUNT(*) FROM dbo.IPersonn_SpidKepilnama WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM MedicalRecords WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'Application'; L = 'SELECT COUNT(*) FROM dbo.Application WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM Applications WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0)'; Note = 'manual-entry only' }
        @{ BO = 'WorkPermit'; L = 'SELECT COUNT(*) FROM dbo.WorkPermitLetter WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM WorkPermits WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'WorkPermitItem'; L = 'SELECT COUNT(*) FROM dbo.WorkPermit WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM WorkPermitItems WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'Invitation'; L = 'SELECT COUNT(*) FROM dbo.ApplicationResult WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM Invitations WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'InvitationItem'; L = 'SELECT COUNT(*) FROM dbo.PersonInInvitation WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM InvitationItems WHERE GCRecord IS NULL OR GCRecord = 0'; Note = '' }
        @{ BO = 'ApplicationItem'; L = 'SELECT COUNT(*) FROM dbo.PersonInApplication WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM ApplicationItems ai INNER JOIN Applications a ON ai.ApplicationID = a.ID WHERE a.IsManualEntry = 1 AND (a.GCRecord IS NULL OR a.GCRecord = 0) AND (ai.GCRecord IS NULL OR ai.GCRecord = 0)'; Note = 'manual-entry items' }
        @{ BO = 'ApplicationProgress'; L = 'SELECT COUNT(*) FROM dbo.Application WHERE GCRecord IS NULL'; T = 'SELECT COUNT(*) FROM ApplicationProgresses ap INNER JOIN Applications a ON ap.ApplicationID = a.ID WHERE a.IsManualEntry = 1 AND (a.GCRecord IS NULL OR a.GCRecord = 0) AND (ap.GCRecord IS NULL OR ap.GCRecord = 0)'; Note = 'synthetic multi-step per app' }
    )
}

function Get-OnPremFileRowDefinitions {
    @(
        @{ BO = 'Person.Photo'; L = 'SELECT COUNT(*) FROM dbo.Person WHERE GCRecord IS NULL AND Photo IS NOT NULL AND DATALENGTH(Photo) > 0'; T = 'SELECT COUNT(*) FROM People WHERE Photo IS NOT NULL AND DATALENGTH(Photo) > 0'; Map = $null }
        @{ BO = 'PassportDocument'; L = $null; T = 'SELECT COUNT(*) FROM PassportDocuments WHERE GCRecord IS NULL OR GCRecord = 0'; Map = 'PassportCopy' }
        @{ BO = 'EducationDocument'; L = $null; T = 'SELECT COUNT(*) FROM EducationDocument WHERE GCRecord IS NULL OR GCRecord = 0'; Map = 'EducationDocument' }
        @{ BO = 'VisaDocument'; L = $null; T = 'SELECT COUNT(*) FROM VisaDocument WHERE GCRecord IS NULL OR GCRecord = 0'; Map = $null }
        @{ BO = 'WorkPermitDocument'; L = $null; T = 'SELECT COUNT(*) FROM WorkPermitDocuments WHERE GCRecord IS NULL OR GCRecord = 0'; Map = 'WorkPermitDocument' }
        @{ BO = 'InvitationDocument'; L = $null; T = 'SELECT COUNT(*) FROM InvitationDocuments WHERE GCRecord IS NULL OR GCRecord = 0'; Map = 'InvitationDocument' }
        @{ BO = 'FamilyProofDocument'; L = $null; T = 'SELECT COUNT(*) FROM PersonFamilyRelationDocuments WHERE GCRecord IS NULL OR GCRecord = 0'; Map = 'FamilyProofDocument' }
        @{ BO = 'MedicalRecordDocument'; L = 'SELECT COUNT(*) FROM dbo.Copy c WHERE c.GCRecord IS NULL AND c.IPersonn_SpidKepilnama IS NOT NULL'; T = 'SELECT COUNT(*) FROM MedicalRecordDocuments WHERE GCRecord IS NULL OR GCRecord = 0'; Map = 'MedicalRecordDocument' }
        @{ BO = 'FileData (all)'; L = $null; T = 'SELECT COUNT(*) FROM FileData WHERE GCRecord IS NULL OR GCRecord = 0'; Map = $null }
    )
}

function Set-OnPremProdConnectionFromSsh {
    param(
        [string]$SshHost = 'visa2026-onprem',
        [string]$RemoteAppSettings = 'C:\inetpub\visa2026-prod\appsettings.Production.json'
    )

    if (-not [string]::IsNullOrWhiteSpace($env:VISA2026_PROD_SQL_CONNECTION)) {
        return $env:VISA2026_PROD_SQL_CONNECTION
    }

    Write-Host "INF Loading prod connection from ssh ${SshHost}:$RemoteAppSettings ..." -ForegroundColor DarkGray
    $jsonText = & ssh $SshHost "type $RemoteAppSettings" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SSH failed ($SshHost): $jsonText"
    }

    $cfg = $jsonText | ConvertFrom-Json
    $conn = $cfg.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($conn)) {
        throw "DefaultConnection missing in $RemoteAppSettings on $SshHost"
    }

    $conn = $conn -replace '(?i)Server=localhost\\SQLEXPRESS', 'Server=10.100.128.25\SQLEXPRESS'
    $env:VISA2026_PROD_SQL_CONNECTION = $conn
    return $conn
}

function Test-OnPremSqlConnections {
    param($Config)

    Write-Host 'INF Preflight: legacy + prod SQL ...' -ForegroundColor DarkGray
    Invoke-OnPremSqlCount -Config $Config -Query 'SELECT 1' -Side Legacy | Out-Null
    Invoke-OnPremSqlCount -Config $Config -Query 'SELECT 1' -Side Target | Out-Null
    Write-Host 'INF SQL preflight OK' -ForegroundColor Green
}

function Resolve-OnPremSyncStateConfig {
    param(
        [string]$LegacyServer = '10.100.128.15',
        [string]$LegacyDatabase = 'VISA2015',
        [string]$LegacyUser = 'ReadOnlyUser',
        [string]$LegacyPassword = '',
        [string]$TargetConnection = '',
        [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
        [string]$TargetDatabase = 'Visa2026DbProd',
        [string]$TargetUser = 'sa',
        [string]$TargetPassword = '',
        [string]$LegacySource = 'calik-energi-onprem-prod',
        [string]$RepoRoot = ''
    )

    if ([string]::IsNullOrWhiteSpace($LegacyPassword)) {
        $LegacyPassword = [Environment]::GetEnvironmentVariable('SQL_SERVER_10.100.128.15', 'User')
        if ([string]::IsNullOrWhiteSpace($LegacyPassword)) {
            $LegacyPassword = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD', 'User')
        }
    }
    if ([string]::IsNullOrWhiteSpace($LegacyPassword)) {
        throw 'Set SQL_SERVER_10.100.128.15 or VISA2014_SQL_PASSWORD (user env) for legacy ReadOnlyUser.'
    }

    $useTargetConnection = -not [string]::IsNullOrWhiteSpace($TargetConnection)
    if (-not $useTargetConnection) {
        $TargetConnection = [Environment]::GetEnvironmentVariable('VISA2026_PROD_SQL_CONNECTION', 'Process')
        if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
            $TargetConnection = [Environment]::GetEnvironmentVariable('VISA2026_PROD_SQL_CONNECTION', 'User')
        }
    }
    if ($useTargetConnection -or -not [string]::IsNullOrWhiteSpace($TargetConnection)) {
        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $TargetConnection
        if ($builder.ContainsKey('Data Source') -and $builder.'Data Source') { $TargetServer = $builder.'Data Source' }
        if ($builder.ContainsKey('Server') -and $builder.Server) { $TargetServer = $builder.Server }
        if ($builder.ContainsKey('Initial Catalog') -and $builder.'Initial Catalog') { $TargetDatabase = $builder.'Initial Catalog' }
        if ($builder.ContainsKey('Database') -and $builder.Database) { $TargetDatabase = $builder.Database }
        if ($builder.ContainsKey('User ID') -and $builder.'User ID') { $TargetUser = $builder.'User ID' }
        if ($builder.ContainsKey('Password') -and $builder.Password) { $TargetPassword = $builder.Password }
    }
    if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
        throw @"
Prod SQL credentials required. Set user env VISA2026_PROD_SQL_CONNECTION (full connection string), pass -TargetConnection, use -LoadProdConnectionFromSsh on Watch-OnPremSyncState.ps1, or pass -TargetPassword.

Example (load from prod IIS config — same as OnPrem-Sync.ps1):
  .\scripts\visa2014-migration\Watch-OnPremSyncState.ps1 -LoadProdConnectionFromSsh -IntervalSeconds 30 -ClearScreen

Do not use placeholder text (YOUR_PASSWORD or <real-sa-password>) in the connection string.
"@
    }

    $placeholderPatterns = @('YOUR_PASSWORD', '<real-sa-password>', '<real-sa-password>', 'Password=...;')
    foreach ($pat in $placeholderPatterns) {
        if ($TargetPassword -like "*$pat*" -or ($TargetConnection -and $TargetConnection -like "*$pat*")) {
            throw "Prod SQL password looks like a placeholder ($pat). Use -LoadProdConnectionFromSsh or paste the real password from prod appsettings.Production.json."
        }
    }

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        . (Join-Path $PSScriptRoot 'Get-RepoRoot.ps1')
        $RepoRoot = Get-Visa2026RepoRoot
    }

    $mapRoot = Join-Path $RepoRoot "Visa2026.DataImporter/legacy/visa2014/id-maps/$LegacySource"
    $syncStatePath = Join-Path $RepoRoot "Visa2026.DataImporter/legacy/visa2014/sync-state/$LegacySource.json"

    [pscustomobject]@{
        LegacyServer    = $LegacyServer
        LegacyDatabase  = $LegacyDatabase
        LegacyUser      = $LegacyUser
        LegacyPassword  = $LegacyPassword
        TargetServer    = $TargetServer
        TargetDatabase  = $TargetDatabase
        TargetUser      = $TargetUser
        TargetPassword  = $TargetPassword
        LegacySource    = $LegacySource
        MapRoot         = $mapRoot
        SyncStatePath   = $syncStatePath
    }
}

function Invoke-OnPremSqlCount {
    param(
        $Config,
        [string]$Query,
        [ValidateSet('Legacy', 'Target')]
        [string]$Side
    )

  if ($Side -eq 'Legacy') {
        $server = $Config.LegacyServer
        $user = $Config.LegacyUser
        $password = $Config.LegacyPassword
        $database = $Config.LegacyDatabase
        $hint = 'Check SQL_SERVER_10.100.128.15 (ReadOnlyUser on 10.100.128.15).'
    }
    else {
        $server = $Config.TargetServer
        $user = $Config.TargetUser
        $password = $Config.TargetPassword
        $database = $Config.TargetDatabase
        $hint = 'Use the real sa password from prod appsettings.Production.json (not the YOUR_PASSWORD placeholder). Same string as OnPrem-Sync.ps1.'
    }

    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $raw = & sqlcmd -S $server -U $user -P $password -d $database -C `
            -Q "SET NOCOUNT ON; $Query" -W -h-1 2>&1
        $exit = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    $text = @($raw | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() }
        else { $_.ToString() }
    })
    $lines = @($text | Where-Object { $_ -match '^\s*\d+\s*$' })
    if ($exit -ne 0) {
        $detail = ($text | Where-Object { $_ -match 'Error|failed' } | Select-Object -First 1)
        if (-not $detail) { $detail = "sqlcmd exit $exit" }
        throw "${Side} SQL count failed ($server / $database): $detail. $hint"
    }
    if ($lines.Count -eq 0) { return $null }
    [int]($lines[0].ToString().Trim())
}

function Invoke-OnPremLegacyCount {
    param($Config, [string]$Query)
    Invoke-OnPremSqlCount -Config $Config -Query $Query -Side Legacy
}

function Invoke-OnPremTargetCount {
    param($Config, [string]$Query)
    Invoke-OnPremSqlCount -Config $Config -Query $Query -Side Target
}

function Get-OnPremIdMapCount {
    param($Config, [string]$Entity)
    $p = Join-Path $Config.MapRoot "$Entity.json"
    if (-not (Test-Path -LiteralPath $p)) { return $null }
    $pattern = '"[0-9a-fA-F-]{36}"\s*:'
    return ([regex]::Matches((Get-Content -LiteralPath $p -Raw), $pattern)).Count
}

function Get-OnPremScalarSyncState {
    param([string]$Bo, [int]$Legacy, [int]$Migrated, [int]$NotCompleted)
    if ($Bo -eq 'ApplicationProgress') { return 'Synthetic (multi-step)' }
    if ($Bo -eq 'AddressOfResidence' -and $Migrated -gt $Legacy) { return 'Complete (PIA inferred)' }
    if ($Bo -eq 'WorkPermit' -and $Migrated -ge $Legacy) { return 'Complete' }
    if ($NotCompleted -eq 0) { return 'Complete' }
    if ($NotCompleted -le 100) { return 'Near complete' }
    return 'Partial'
}

function Get-OnPremFileSyncState {
    param([int]$LegacyScope, [int]$Migrated, [int]$FileIdMap)
    if ($null -eq $LegacyScope -or $LegacyScope -eq 0) {
        if ($Migrated -gt 0) { return 'Bootstrap only' }
        return 'N/A'
    }
    if ($Migrated -eq 0 -and $FileIdMap -eq 0) { return 'Not started' }
    if ($Migrated -ge $LegacyScope -and $FileIdMap -ge $LegacyScope) { return 'Bootstrap complete' }
    if ($Migrated -gt 0 -and $FileIdMap -gt 0) { return "Partial ($FileIdMap mapped)" }
    if ($Migrated -gt 0) { return 'Prod rows; no file id-map' }
    return 'Not started'
}

function Get-OnPremSyncWatermark {
    param($Config)
    if (-not (Test-Path -LiteralPath $Config.SyncStatePath)) { return $null }
    try {
        $syncJson = Get-Content -LiteralPath $Config.SyncStatePath -Raw | ConvertFrom-Json
        return $syncJson.LastSuccessfulRunUtc
    }
    catch { return $null }
}

function Get-OnPremScalarSyncSnapshot {
    param($Config)

    foreach ($row in Get-OnPremScalarRowDefinitions) {
        $legacy = Invoke-OnPremLegacyCount -Config $Config -Query $row.L
        $migrated = Invoke-OnPremTargetCount -Config $Config -Query $row.T
        $notCompleted = if ($null -ne $legacy -and $null -ne $migrated) { [Math]::Max(0, $legacy - $migrated) } else { $null }
        $idMap = Get-OnPremIdMapCount -Config $Config -Entity $row.BO
        $state = if ($null -ne $legacy -and $null -ne $migrated) {
            Get-OnPremScalarSyncState -Bo $row.BO -Legacy $legacy -Migrated $migrated -NotCompleted $notCompleted
        } else { 'Unknown' }

        [pscustomobject]@{
            Kind         = 'Scalar'
            BO           = $row.BO
            Legacy       = $legacy
            Migrated     = $migrated
            NotCompleted = $notCompleted
            IdMap        = $idMap
            SyncState    = $state
            Note         = $row.Note
        }
    }
}

function Get-OnPremFileSyncSnapshot {
    param($Config)

    foreach ($row in Get-OnPremFileRowDefinitions) {
        $legacyScope = if ($row.L) { Invoke-OnPremLegacyCount -Config $Config -Query $row.L } else { $null }
        $migrated = Invoke-OnPremTargetCount -Config $Config -Query $row.T
        $fileIdMap = if ($row.Map) { Get-OnPremIdMapCount -Config $Config -Entity $row.Map } else { $null }
        if ($null -eq $legacyScope -and $fileIdMap -gt 0) { $legacyScope = $fileIdMap }
        $notCompleted = if ($null -ne $legacyScope -and $null -ne $migrated) { [Math]::Max(0, $legacyScope - $migrated) } else { $null }
        $mapCount = if ($null -eq $fileIdMap) { 0 } else { $fileIdMap }
        $state = Get-OnPremFileSyncState -LegacyScope $legacyScope -Migrated $migrated -FileIdMap $mapCount

        [pscustomobject]@{
            Kind         = 'FileData'
            BO           = $row.BO
            Legacy       = $legacyScope
            Migrated     = $migrated
            NotCompleted = $notCompleted
            IdMap        = $fileIdMap
            SyncState    = $state
            Note         = ''
        }
    }
}

function Get-OnPremSyncStateSnapshot {
    param(
        $Config,
        [switch]$IncludeFileData
    )

    $rows = @(Get-OnPremScalarSyncSnapshot -Config $Config)
    if ($IncludeFileData) {
        $rows += @(Get-OnPremFileSyncSnapshot -Config $Config)
    }
    return $rows
}
