# Setup-ClaudeProxy.ps1
# Run this script to configure VS Code and Terminal for Psiphon VPN

$psiphonPort = Read-Host "Enter your Psiphon HTTP Proxy Port (e.g., 8080 or 52143)"

if (-not $psiphonPort) {
    Write-Error "Port is required. Check Psiphon > Settings > Local Proxy Ports."
    return
}

$proxyUrl = "http://127.0.0.1:$psiphonPort"

# Validate Psiphon is actually listening on the specified port
Write-Host "Verifying Psiphon connection on port $psiphonPort..." -ForegroundColor Gray
if (-not (Test-NetConnection -ComputerName 127.0.0.1 -Port $psiphonPort -InformationLevel Quiet)) {
    Write-Error "Psiphon does not seem to be listening on port $psiphonPort. Please check Psiphon > Settings > Local Proxy Ports."
    return
}

# 1. Configure Current Terminal Session
Write-Host "Configuring current session environment variables..." -ForegroundColor Cyan
$env:HTTP_PROXY = $proxyUrl
$env:HTTPS_PROXY = $proxyUrl
$env:NODE_TLS_REJECT_UNAUTHORIZED = "0"

# 2. Configure VS Code Settings (User level)
$vscodeSettingsPath = "$env:APPDATA\Code\User\settings.json"

if (Test-Path $vscodeSettingsPath) {
    Write-Host "Updating VS Code settings.json..." -ForegroundColor Cyan
    
    # Backup existing settings before modification
    Copy-Item $vscodeSettingsPath "$vscodeSettingsPath.bak" -Force
    
    $settings = Get-Content $vscodeSettingsPath | ConvertFrom-Json
    
    # Add or update proxy settings
    $settings | Add-Member -MemberType NoteProperty -Name "http.proxy" -Value $proxyUrl -Force
    $settings | Add-Member -MemberType NoteProperty -Name "http.proxyStrictSSL" -Value $false -Force
    
    $settings | ConvertTo-Json -Depth 100 | Set-Content $vscodeSettingsPath
    Write-Host "VS Code settings updated successfully." -ForegroundColor Green
} else {
    Write-Warning "VS Code settings.json not found at $vscodeSettingsPath. Please ensure VS Code is installed."
}

# 3. Optional: Set Permanent User Environment Variables
$response = Read-Host "Do you want to set these variables permanently for your Windows User? This allows Claude to work in new terminals automatically. (Y/N)"
if ($response -eq 'Y' -or $response -eq 'y') {
    [Environment]::SetEnvironmentVariable("HTTP_PROXY", $proxyUrl, "User")
    [Environment]::SetEnvironmentVariable("HTTPS_PROXY", $proxyUrl, "User")
    
    # Note: Setting this to 0 disables SSL certificate validation for Node.js. 
    # Necessary for some VPNs, but reduces security against MITM attacks.
    [Environment]::SetEnvironmentVariable("NODE_TLS_REJECT_UNAUTHORIZED", "0", "User")
    Write-Host "User environment variables set permanently. Security: SSL Verification Disabled for Node.js." -ForegroundColor Green
    Write-Host "NOTE: You may need to restart VS Code and any open terminals for changes to take effect." -ForegroundColor Yellow
}

Write-Host "`nConfiguration complete!" -ForegroundColor Magenta
Write-Host "You can now run 'claude' in this terminal."
