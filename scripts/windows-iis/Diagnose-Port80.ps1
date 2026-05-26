#Requires -Version 5.1
$conn = Get-NetTCPConnection -LocalPort 80 -State Listen -ErrorAction SilentlyContinue
foreach ($c in $conn) {
    $proc = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
    $svc = Get-CimInstance Win32_Service -Filter "ProcessId=$($c.OwningProcess)" -ErrorAction SilentlyContinue
    Write-Host "Port 80 listener PID=$($c.OwningProcess) Process=$($proc.ProcessName) Service=$($svc.Name)"
}
Write-Host "--- IIS sites ---"
& "$env:windir\System32\inetsrv\appcmd" list site
Write-Host "--- Docker ---"
$docker = Get-Command docker -ErrorAction SilentlyContinue
if ($docker) {
    docker ps --format "{{.Names}} {{.Ports}}" 2>&1
}
