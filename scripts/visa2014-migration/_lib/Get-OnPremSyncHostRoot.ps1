# Default sync-host folder per IIS slot on 10.100.128.25, or local PG import root.

function Get-DefaultOnPremSyncHostRoot {
    param(
        [ValidateSet('Production', 'Staging', 'Demo', 'Local')]
        [string]$Profile = 'Production'
    )

    switch ($Profile) {
        'Production' { return 'C:\visa2026-sync' }
        'Staging' { return 'C:\visa2026-sync-staging' }
        'Demo' { return 'C:\visa2026-sync-demo' }
        'Local' {
            # scripts/visa2014-migration/_lib → repo root → artifacts/local-pg-import
            $repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
            return (Join-Path $repo 'artifacts\local-pg-import')
        }
        default { return 'C:\visa2026-sync' }
    }
}

function Get-OnPremSyncHostAppSettingsPath {
    param(
        [ValidateSet('Production', 'Staging', 'Demo', 'Local')]
        [string]$Profile = 'Production'
    )

    switch ($Profile) {
        'Production' { return 'C:\inetpub\visa2026-prod\appsettings.Production.json' }
        'Staging' { return 'C:\inetpub\visa2026-staging\appsettings.Production.json' }
        'Demo' { return 'C:\inetpub\visa2026-demo\appsettings.Production.json' }
        'Local' { return '' }
        default { return 'C:\inetpub\visa2026-prod\appsettings.Production.json' }
    }
}

function Get-OnPremSyncHostTargetConnectionEnv {
    param(
        [ValidateSet('Production', 'Staging', 'Demo', 'Local')]
        [string]$Profile = 'Production'
    )

    switch ($Profile) {
        'Production' { return 'VISA2026_PROD_SQL_CONNECTION' }
        'Staging' { return 'VISA2026_STAGING_SQL_CONNECTION' }
        'Demo' { return 'VISA2026_DEMO_SQL_CONNECTION' }
        'Local' { return '' }
        default { return 'VISA2026_PROD_SQL_CONNECTION' }
    }
}