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
            DbName                 = "visa2026_prod"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys-Prod"
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
            DbName                 = "visa2026_staging"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys-Staging"
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
            DbName                 = "visa2026_demo"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys-Demo"
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
            DbName                 = "visa2026_prod"
            DataProtectionKeysPath = "C:\ProgramData\Visa2026\DataProtection-Keys"
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
        "DEVEXPRESS_LICENSEKEY=CHANGE_ME_LICENSE_KEY"
        "EFCORE_PROVIDER=Postgres"
        "PG_HOST=localhost"
        "PG_PORT=5432"
        "PG_USER=postgres"
        "PG_PASSWORD=CHANGE_ME_PG_PASSWORD"
        "DB_NAME=$($slot.DbName)"
        "# One-shot: FORCE_XAF_DB_UPDATE=true (then remove from app pool) - see docs/ENVIRONMENTS.md"
        "# FORCE_XAF_DB_UPDATE=true"
        "# Resminamalar local sandbox template editing (HTTPS required):"
        "# TEMPLATE_EDIT_STAGING_ENABLED=true"
        $(if ($Profile -eq "Staging") {
            @(
                "HTTPS_ENABLED=true"
                "HTTPS_PORT=$(Resolve-Visa2026DefaultHttpsPortForProfile -Profile $Profile)"
            )
        }
        elseif ($Profile -eq "Demo") {
            @(
                "# Demo playground - HTTP only (:$($slot.HttpPort))"
                "HTTPS_ENABLED=false"
            )
        }
        else {
            @(
                "# HTTPS_ENABLED=true"
                "# HTTPS_PORT=$(Resolve-Visa2026DefaultHttpsPortForProfile -Profile $Profile)"
            )
        })
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
        [int]$DefaultPort = 0
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

    if ($DefaultPort -gt 0) {
        return $DefaultPort
    }

    return 443
}

function Resolve-Visa2026DefaultHttpsPortForProfile {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo", "Legacy")]
        [string]$Profile
    )

    switch ($Profile) {
        "Staging" { return 8080 }
        "Demo" { return 8081 }
        default { return 443 }
    }
}

function Get-Visa2026SlotSmokeLoginPageUrl {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo", "Legacy")]
        [string]$Profile,

        [string]$EnvFile = "",
        [string]$HostName = "127.0.0.1"
    )

    $slot = Get-Visa2026IisSlotDefinition -Profile $Profile
    if (-not $EnvFile) {
        $EnvFile = $slot.EnvFile
    }

    if (-not (Resolve-Visa2026HttpsEnabled -EnvFile $EnvFile)) {
        return $slot.LoginPageUrl
    }

    $httpsPort = Resolve-Visa2026HttpsPort -EnvFile $EnvFile -DefaultPort (Resolve-Visa2026DefaultHttpsPortForProfile -Profile $Profile)
    if ($httpsPort -eq 443) {
        return "https://${HostName}/LoginPage"
    }

    return "https://${HostName}:$httpsPort/LoginPage"
}
