#Requires -Version 5.1
<#
.SYNOPSIS
  Deployment slot manifest for Visa2026 IIS on one Windows Server (prod / staging / demo).

.NOTES
  Dot-source from other scripts/windows-iis/*.ps1:
    . (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")
#>

function Get-Visa2026IisSlotDefinition {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo", "Legacy")]
        [string]$Profile
    )

    $definitions = @{
        Production = @{
            Profile                = "Production"
            SiteName               = "Visa2026-Prod"
            AppPoolName            = "Visa2026-Prod"
            HttpPort               = 80
            PublishPath            = "C:\inetpub\visa2026-prod"
            EnvFile                = "C:\visa2026\env\prod.env"
            DbName                 = "Visa2026DbProd"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys-Prod"
            TemplateEditLocalPath  = "C:\ProgramData\Visa2026\TemplateEdit\prod"
            TemplateEditShareName  = "Visa2026TemplateEdit-Prod"
            BackupSubfolder        = "prod"
            LoginPageUrl           = "http://localhost/LoginPage"
        }
        Staging    = @{
            Profile                = "Staging"
            SiteName               = "Visa2026-Staging"
            AppPoolName            = "Visa2026-Staging"
            HttpPort               = 8080
            PublishPath            = "C:\inetpub\visa2026-staging"
            EnvFile                = "C:\visa2026\env\staging.env"
            DbName                 = "Visa2026DbStaging"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys-Staging"
            TemplateEditLocalPath  = "C:\ProgramData\Visa2026\TemplateEdit\staging"
            TemplateEditShareName  = "Visa2026TemplateEdit-Staging"
            BackupSubfolder        = "staging"
            LoginPageUrl           = "http://localhost:8080/LoginPage"
        }
        Demo       = @{
            Profile                = "Demo"
            SiteName               = "Visa2026-Demo"
            AppPoolName            = "Visa2026-Demo"
            HttpPort               = 8081
            PublishPath            = "C:\inetpub\visa2026-demo"
            EnvFile                = "C:\visa2026\env\demo.env"
            DbName                 = "Visa2026DbDemo"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys-Demo"
            TemplateEditLocalPath  = "C:\ProgramData\Visa2026\TemplateEdit\demo"
            TemplateEditShareName  = "Visa2026TemplateEdit-Demo"
            BackupSubfolder        = "demo"
            LoginPageUrl           = "http://localhost:8081/LoginPage"
        }
        Legacy     = @{
            Profile                = "Legacy"
            SiteName               = "Visa2026"
            AppPoolName            = "Visa2026"
            HttpPort               = 80
            PublishPath            = "C:\inetpub\visa2026"
            EnvFile                = "C:\visa2026\.env.prod"
            DbName                 = "Visa2026DbProd"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys"
            TemplateEditLocalPath  = "C:\ProgramData\Visa2026\TemplateEdit\legacy"
            TemplateEditShareName  = "Visa2026TemplateEdit"
            BackupSubfolder        = "legacy"
            LoginPageUrl           = "http://localhost/LoginPage"
        }
    }

    [PSCustomObject]$definitions[$Profile]
}

function Get-Visa2026IisSlotProfiles {
    @("Production", "Staging", "Demo")
}

function Resolve-Visa2026IisSlotContext {
    [CmdletBinding()]
    param(
        [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
        [string]$Profile = "",

        [string]$PublishPath = "",
        [string]$EnvFile = "",
        [string]$SiteName = "",
        [string]$AppPoolName = "",
        [int]$HttpPort = 0,
        [string]$DataProtectionKeysPath = ""
    )

    if ([string]::IsNullOrWhiteSpace($Profile)) {
        if (-not [string]::IsNullOrWhiteSpace($PublishPath)) {
            foreach ($name in (Get-Visa2026IisSlotProfiles) + @("Legacy")) {
                $slot = Get-Visa2026IisSlotDefinition -Profile $name
                if ($PublishPath -ieq $slot.PublishPath) {
                    $Profile = $name
                    break
                }
            }
        }
        if ([string]::IsNullOrWhiteSpace($Profile)) {
            $Profile = "Production"
        }
    }

    $slot = Get-Visa2026IisSlotDefinition -Profile $Profile

    [PSCustomObject]@{
        Profile                = $slot.Profile
        SiteName               = if ($SiteName) { $SiteName } else { $slot.SiteName }
        AppPoolName            = if ($AppPoolName) { $AppPoolName } else { $slot.AppPoolName }
        HttpPort               = if ($HttpPort -gt 0) { $HttpPort } else { $slot.HttpPort }
        PublishPath            = if ($PublishPath) { $PublishPath } else { $slot.PublishPath }
        EnvFile                = if ($EnvFile) { $EnvFile } else { $slot.EnvFile }
        DbName                 = $slot.DbName
        DataProtectionKeysPath = if ($DataProtectionKeysPath) { $DataProtectionKeysPath } else { $slot.DataProtectionKeysPath }
        BackupSubfolder        = $slot.BackupSubfolder
        LoginPageUrl           = $slot.LoginPageUrl
        BackupRoot             = "C:\visa2026\backups"
        EnvRoot                = "C:\visa2026\env"
        DeployScriptsPath      = "C:\visa2026-deploy\iis"
    }
}

function Initialize-Visa2026IisServerFolders {
    [CmdletBinding()]
    param(
        [switch]$IncludeLegacy
    )

    $paths = @(
        "C:\visa2026-deploy\iis",
        "C:\visa2026\backups",
        (Resolve-Visa2026IisSlotContext -Profile Production).EnvRoot,
        (Resolve-Visa2026IisSlotContext -Profile Production).BackupRoot
    )

    foreach ($name in Get-Visa2026IisSlotProfiles) {
        $ctx = Resolve-Visa2026IisSlotContext -Profile $name
        $paths += $ctx.PublishPath
        $paths += $ctx.DataProtectionKeysPath
        $paths += $ctx.TemplateEditLocalPath
        $paths += (Join-Path $ctx.BackupRoot $ctx.BackupSubfolder)
    }

    if ($IncludeLegacy) {
        $legacy = Resolve-Visa2026IisSlotContext -Profile Legacy
        $paths += $legacy.PublishPath
        $paths += $legacy.DataProtectionKeysPath
    }

    foreach ($path in ($paths | Select-Object -Unique)) {
        New-Item -ItemType Directory -Force -Path $path | Out-Null
    }
}

function Get-Visa2026IisSlotEnvTemplate {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo")]
        [string]$Profile
    )

    $slot = Get-Visa2026IisSlotDefinition -Profile $Profile
    @(
        "# Copy to $($slot.EnvFile) on the server. Never commit real secrets."
        "SA_PASSWORD=CHANGE_ME_STRONG_PASSWORD"
        "DEVEXPRESS_LICENSEKEY=CHANGE_ME_LICENSE_KEY"
        "DB_NAME=$($slot.DbName)"
        "# One-shot: FORCE_XAF_DB_UPDATE=true (then remove from app pool) - see docs/ENVIRONMENTS.md"
        "# FORCE_XAF_DB_UPDATE=true"
        "# Resminamalar template staging (Ensure-Visa2026TemplateEditShare.ps1):"
        "# TEMPLATE_EDIT_STAGING_ENABLED=true"
        "# TEMPLATE_EDIT_UNC_HOST=10.100.128.25"
        "# TEMPLATE_EDIT_OFFICERS_PRINCIPAL=DOMAIN\VisaOfficers"
    ) -join "`r`n"
}

function Ensure-Visa2026IisSlotEnvFile {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo")]
        [string]$Profile,

        [string]$SourceEnvFile = ""
    )

    $ctx = Resolve-Visa2026IisSlotContext -Profile $Profile
    if (Test-Path -LiteralPath $ctx.EnvFile) {
        return $ctx.EnvFile
    }

    Initialize-Visa2026IisServerFolders | Out-Null

    if ($SourceEnvFile -and (Test-Path -LiteralPath $SourceEnvFile)) {
        $lines = Get-Content -LiteralPath $SourceEnvFile
        $out = foreach ($line in $lines) {
            if ($line -match '^\s*DB_NAME\s*=') { "DB_NAME=$($ctx.DbName)" } else { $line }
        }
        $out | Set-Content -LiteralPath $ctx.EnvFile -Encoding UTF8
        Write-Host "Created $($ctx.EnvFile) from $SourceEnvFile (DB_NAME=$($ctx.DbName))." -ForegroundColor Yellow
        return $ctx.EnvFile
    }

    Get-Visa2026IisSlotEnvTemplate -Profile $Profile | Set-Content -LiteralPath $ctx.EnvFile -Encoding UTF8
    Write-Warning "Created template $($ctx.EnvFile) - set SA_PASSWORD and DEVEXPRESS_LICENSEKEY before configure/deploy."
    return $ctx.EnvFile
}

function Grant-Visa2026IisAppPoolDataProtectionAcl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppPoolName,

        [Parameter(Mandatory = $true)]
        [string]$DataProtectionKeysPath
    )

    New-Item -ItemType Directory -Force -Path $DataProtectionKeysPath | Out-Null
    $grantee = "IIS AppPool\$AppPoolName`:(OI)(CI)M"
    icacls $DataProtectionKeysPath /grant $grantee 2>$null | Out-Null
}

function Read-Visa2026DotEnvMap {
    param([string]$Path)

    $map = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $map
    }

    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq '') { return }
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

function Resolve-Visa2026TemplateEditUncHost {
    param(
        [string]$EnvFile = "",
        [string]$UncHost = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($UncHost)) {
        return $UncHost.Trim().TrimEnd('\')
    }

    if ($EnvFile -and (Test-Path -LiteralPath $EnvFile)) {
        $envMap = Read-Visa2026DotEnvMap -Path $EnvFile
        if ($envMap.ContainsKey('TEMPLATE_EDIT_UNC_HOST') -and $envMap['TEMPLATE_EDIT_UNC_HOST']) {
            return $envMap['TEMPLATE_EDIT_UNC_HOST'].Trim().TrimEnd('\')
        }
    }

    $dns = [System.Net.Dns]::GetHostEntry('localhost').HostName
    if ($dns -and $dns -notmatch '^\d') {
        return $dns.Split('.')[0]
    }

    return $env:COMPUTERNAME
}

function Get-Visa2026TemplateEditStagingUnc {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo", "Legacy")]
        [string]$Profile,

        [string]$UncHost = "",
        [string]$EnvFile = ""
    )

    $slot = Get-Visa2026IisSlotDefinition -Profile $Profile
    if (-not $EnvFile) { $EnvFile = $slot.EnvFile }
    $hostName = Resolve-Visa2026TemplateEditUncHost -EnvFile $EnvFile -UncHost $UncHost
    return "\\$hostName\$($slot.TemplateEditShareName)"
}

function Resolve-Visa2026TemplateEditStagingEnabled {
    param(
        [string]$EnvFile = "",
        [switch]$DefaultEnabled
    )

    if (-not $EnvFile -or -not (Test-Path -LiteralPath $EnvFile)) {
        return [bool]$DefaultEnabled
    }

    $envMap = Read-Visa2026DotEnvMap -Path $EnvFile
    if (-not $envMap.ContainsKey('TEMPLATE_EDIT_STAGING_ENABLED')) {
        return [bool]$DefaultEnabled
    }

    $raw = $envMap['TEMPLATE_EDIT_STAGING_ENABLED'].Trim().ToLowerInvariant()
    return $raw -in @('1', 'true', 'yes', 'on')
}

function Resolve-Visa2026TemplateEditStagingMode {
    param(
        [string]$EnvFile = ""
    )

    if ($EnvFile -and (Test-Path -LiteralPath $EnvFile)) {
        $envMap = Read-Visa2026DotEnvMap -Path $EnvFile
        if ($envMap.ContainsKey('TEMPLATE_EDIT_STAGING_MODE') -and $envMap['TEMPLATE_EDIT_STAGING_MODE']) {
            $raw = $envMap['TEMPLATE_EDIT_STAGING_MODE'].Trim()
            if ($raw -eq 'LocalFolder') { return 'LocalFolder' }
            if ($raw -eq 'Share') { return 'Share' }
        }
    }

    return 'Share'
}

function Resolve-Visa2026HttpsEnabled {
    param(
        [string]$EnvFile = ""
    )

    if (-not $EnvFile -or -not (Test-Path -LiteralPath $EnvFile)) {
        return $false
    }

    $envMap = Read-Visa2026DotEnvMap -Path $EnvFile
    if (-not $envMap.ContainsKey('HTTPS_ENABLED')) {
        return $false
    }

    $raw = $envMap['HTTPS_ENABLED'].Trim().ToLowerInvariant()
    return $raw -in @('1', 'true', 'yes', 'on')
}

function Resolve-Visa2026HttpsPort {
    param(
        [string]$EnvFile = "",
        [int]$DefaultPort = 443
    )

    if ($EnvFile -and (Test-Path -LiteralPath $EnvFile)) {
        $envMap = Read-Visa2026DotEnvMap -Path $EnvFile
        if ($envMap.ContainsKey('HTTPS_PORT') -and $envMap['HTTPS_PORT']) {
            $parsed = 0
            if ([int]::TryParse($envMap['HTTPS_PORT'].Trim(), [ref]$parsed) -and $parsed -gt 0) {
                return $parsed
            }
        }
    }

    return $DefaultPort
}
