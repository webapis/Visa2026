# Revert-ClaudeProxy.ps1
# Run this script to remove Psiphon proxy configurations

Write-Host "Reverting Proxy Settings..." -ForegroundColor Cyan

# 1. Clear Current Session Variables
$env:HTTP_PROXY = $null
$env:HTTPS_PROXY = $null
$env:NODE_TLS_REJECT_UNAUTHORIZED = $null
Write-Host "Current session variables cleared." -ForegroundColor Green

# 2. Revert VS Code Settings
$vscodeSettingsPath = "$env:APPDATA\Code\User\settings.json"

if (Test-Path $vscodeSettingsPath) {
    try {
        $settings = Get-Content $vscodeSettingsPath | ConvertFrom-Json
        
        # Remove the proxy properties if they exist
        $propertiesToRemove = @("http.proxy", "http.proxyStrictSSL")
        foreach ($prop in $propertiesToRemove) {
            if ($settings.PSObject.Properties[$prop]) {
                $settings.PSObject.Properties.Remove($prop)
            }
        }
        
        $settings | ConvertTo-Json -Depth 100 | Set-Content $vscodeSettingsPath
        Write-Host "VS Code settings.json cleaned." -ForegroundColor Green
    } catch {
        Write-Warning "Failed to update VS Code settings. Ensure the file is not open in another program."
    }
}

# 3. Delete Permanent User Environment Variables
Write-Host "Removing permanent User environment variables..." -ForegroundColor Cyan
[Environment]::SetEnvironmentVariable("HTTP_PROXY", $null, "User")
[Environment]::SetEnvironmentVariable("HTTPS_PROXY", $null, "User")
[Environment]::SetEnvironmentVariable("NODE_TLS_REJECT_UNAUTHORIZED", $null, "User")

Write-Host "`nRevert complete! Proxy settings have been removed." -ForegroundColor Magenta
Write-Host "Please restart VS Code and any open terminals for changes to fully take effect." -ForegroundColor Yellow
