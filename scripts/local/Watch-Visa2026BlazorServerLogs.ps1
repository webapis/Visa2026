#Requires -Version 5.1
<#
.SYNOPSIS
  Stream runtime stdout logs from an already-running Visa2026.Blazor.Server instance.

.DESCRIPTION
  Does NOT start the app. Connects to a running host by HTTP hostname/port and tails the
  process console output via the supported log sinks for this repo:

  - Docker (local):  docker logs -f on the container publishing the port
  - Docker (remote): ssh + docker logs -f
  - IIS (local):     tail latest logs\stdout_*.log under the matching slot publish path
  - IIS (remote):    ssh + tail stdout log on the Windows Server

  Visual Studio F5 and other bare dotnet processes keep stdout in their own console; this
  script cannot attach to them. Use Docker, IIS stdout logging, or dotnet run in a console.

.PARAMETER HostName
  App base host (and optional port), e.g. localhost:8081, 127.0.0.1:5001, 10.100.128.25:8080.
  Port is required unless -HttpPort is supplied.

.PARAMETER Mode
  Auto (default), Docker, IisStdout, IisSsh.

.PARAMETER SshHost
  SSH config host for remote IIS/Docker (default visa2026-onprem when the HTTP host is remote).

.PARAMETER ComposeProject
  Optional Docker Compose project name hint when port matching fails (e.g. visa2026-dev).

.PARAMETER Tail
  Number of existing log lines to show before following (default 100).

.PARAMETER OutFile
  Optional path under agent-local/ to cache streamed lines (appended in real time).

.PARAMETER SkipHttpCheck
  Do not probe /LoginPage before tailing.

.EXAMPLE
  .\scripts\local\Watch-Visa2026BlazorServerLogs.ps1 -HostName localhost:8081

.EXAMPLE
  .\scripts\local\Watch-Visa2026BlazorServerLogs.ps1 -HostName 10.100.128.25:8080 -Mode IisSsh

.EXAMPLE
  .\scripts\local\Watch-Visa2026BlazorServerLogs.ps1 -HostName localhost:5001 -OutFile agent-local/blazor-watch.log

.NOTES
  IIS stdout must be enabled: scripts/windows-iis/Enable-Visa2026StdoutLog.ps1
  Logging reference: docs/BLAZOR_SERVER_LOGGING.md
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HostName,

    [ValidateSet("Auto", "Docker", "IisStdout", "IisSsh")]
    [string]$Mode = "Auto",

    [string]$SshHost = "visa2026-onprem",

    [string]$ComposeProject = "",

    [int]$HttpPort = 0,

    [int]$Tail = 100,

    [string]$OutFile = "",

    [switch]$SkipHttpCheck
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$IisSlotsScript = Join-Path $RepoRoot "scripts\windows-iis\Visa2026-IisSlots.ps1"

function Parse-Visa2026HostName {
    param([string]$Value, [int]$PortOverride)

    $raw = $Value.Trim()
    if ($raw -match '^\[([^\]]+)\]:(\d+)$') {
        return [PSCustomObject]@{
            Host = $Matches[1]
            Port = [int]$Matches[2]
        }
    }

    if ($raw -match '^([^:/\\]+):(\d+)$') {
        return [PSCustomObject]@{
            Host = $Matches[1]
            Port = [int]$Matches[2]
        }
    }

    if ($PortOverride -gt 0) {
        return [PSCustomObject]@{
            Host = $raw
            Port = $PortOverride
        }
    }

    throw "HostName must include a port (e.g. localhost:8081) or pass -HttpPort."
}

function Test-Visa2026LocalHost {
    param([string]$TargetHost)

    $h = $TargetHost.ToLowerInvariant()
    return $h -in @("localhost", "127.0.0.1", "::1", "0.0.0.0")
}

function Get-Visa2026LoginProbeUri {
    param([string]$TargetHost, [int]$Port)

    $hostLiteral = if ($TargetHost -match ':') { "[$TargetHost]" } else { $TargetHost }
    if ($Port -eq 443) {
        return "https://${hostLiteral}/LoginPage"
    }

    return "http://${hostLiteral}:$Port/LoginPage"
}

function Test-Visa2026HttpEndpoint {
    param([string]$Uri)

    try {
        $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 15 -MaximumRedirection 5
        Write-Host "HTTP probe OK: $Uri -> $($response.StatusCode)" -ForegroundColor DarkGreen
        return $true
    }
    catch {
        Write-Warning "HTTP probe failed for $Uri : $($_.Exception.Message)"
        return $false
    }
}

function New-Visa2026LogSink {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path $RepoRoot $Path
    }

    $directory = Split-Path -Parent $fullPath
    if ($directory) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $header = "=== $(Get-Date -Format o) Watch-Visa2026BlazorServerLogs HostName=$HostName Mode=$Mode ==="
    Add-Content -LiteralPath $fullPath -Value $header -Encoding UTF8

    return [PSCustomObject]@{
        Path = $fullPath
        Writer = New-Object System.IO.StreamWriter($fullPath, $true, ([System.Text.UTF8Encoding]::new($false)))
    }
}

function Write-Visa2026StreamLine {
    param(
        [string]$Line,
        $Sink
    )

    if ($null -ne $Line) {
        Write-Host $Line
        if ($Sink) {
            $Sink.Writer.WriteLine($Line)
            $Sink.Writer.Flush()
        }
    }
}

function Test-Visa2026DockerAvailable {
    try {
        & docker info *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Find-LocalDockerContainerByPort {
    param(
        [int]$Port,
        [string]$ProjectHint
    )

    if (-not (Test-Visa2026DockerAvailable)) {
        return $null
    }

    $names = @(& docker ps --format "{{.Names}}|{{.Ports}}" 2>$null)
    foreach ($line in $names) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split '\|', 2
        $name = $parts[0]
        $ports = if ($parts.Count -gt 1) { $parts[1] } else { "" }

        if ($ports -match "(?:0\.0\.0\.0|127\.0\.0\.1|\[::\]|::):$Port->") {
            return $name
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ProjectHint)) {
        $pattern = "$ProjectHint-app-"
        $match = $names | Where-Object { $_ -like "$pattern*" } | Select-Object -First 1
        if ($match) {
            return ($match -split '\|', 2)[0]
        }
    }

    return $null
}

function Get-IisSlotProfileByHttpPort {
    param([int]$Port)

    if (-not (Test-Path -LiteralPath $IisSlotsScript)) {
        return $null
    }

    . $IisSlotsScript
    foreach ($profile in (Get-Visa2026IisSlotProfiles) + @("Legacy")) {
        $slot = Get-Visa2026IisSlotDefinition -Profile $profile
        if ($slot.HttpPort -eq $Port) {
            return $profile
        }
    }

    return $null
}

function Get-LatestIisStdoutLogPath {
    param([string]$PublishPath)

    $logDir = Join-Path $PublishPath "logs"
    if (-not (Test-Path -LiteralPath $logDir)) {
        return $null
    }

    $latest = Get-ChildItem -LiteralPath $logDir -Filter "stdout_*" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latest) {
        return $latest.FullName
    }

    return $null
}

function Wait-ForIisStdoutLog {
    param(
        [string]$PublishPath,
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $path = Get-LatestIisStdoutLogPath -PublishPath $PublishPath
        if ($path) {
            return $path
        }

        Write-Host "Waiting for IIS stdout log under $(Join-Path $PublishPath 'logs') ..." -ForegroundColor DarkYellow
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "No stdout_*.log found under $(Join-Path $PublishPath 'logs'). Run Enable-Visa2026StdoutLog.ps1 on the server and recycle the app pool."
}

function Start-Visa2026DockerLogFollow {
    param(
        [string]$ContainerName,
        [bool]$Remote,
        [string]$RemoteSshHost,
        [int]$InitialTail,
        $Sink
    )

    Write-Host "Following Docker logs: $ContainerName$(if ($Remote) { " via ssh $RemoteSshHost" } else { '' })" -ForegroundColor Cyan

    if ($Remote) {
        $remoteCommand = "docker logs -f --tail $InitialTail $ContainerName"
        & ssh -tt $RemoteSshHost $remoteCommand 2>&1 | ForEach-Object {
            Write-Visa2026StreamLine -Line "$_" -Sink $Sink
        }
        return
    }

    & docker logs -f --tail $InitialTail $ContainerName 2>&1 | ForEach-Object {
        Write-Visa2026StreamLine -Line "$_" -Sink $Sink
    }
}

function Start-Visa2026IisStdoutFollow {
    param(
        [string]$PublishPath,
        [int]$InitialTail,
        $Sink,
        [bool]$Remote,
        [string]$RemoteSshHost
    )

    if ($Remote) {
        $remotePublishWin = $PublishPath
        Write-Host "Following IIS stdout via ssh $RemoteSshHost : $remotePublishWin\logs" -ForegroundColor Cyan

        $psCommand = @"
`$logDir = '$remotePublishWin\logs';
if (-not (Test-Path -LiteralPath `$logDir)) { throw "Missing `$logDir. Enable stdout logging first." }
`$deadline = (Get-Date).AddMinutes(2);
do {
  `$f = Get-ChildItem -LiteralPath `$logDir -Filter 'stdout_*' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1;
  if (`$f) { break }
  Start-Sleep -Seconds 2
} while ((Get-Date) -lt `$deadline);
if (-not `$f) { throw 'No stdout log file yet.' }
Write-Output "=== Tailing `$(`$f.FullName) ===";
Get-Content -LiteralPath `$f.FullName -Tail $InitialTail -Wait
"@

        $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($psCommand))
        $remoteInvocation = "powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded"
        & ssh -tt $RemoteSshHost $remoteInvocation 2>&1 | ForEach-Object {
            Write-Visa2026StreamLine -Line "$_" -Sink $Sink
        }
        return
    }
    else {
        $stdoutPath = Wait-ForIisStdoutLog -PublishPath $PublishPath
        Write-Host "Following IIS stdout: $stdoutPath" -ForegroundColor Cyan
        $currentPath = $stdoutPath
        $lastLength = (Get-Item -LiteralPath $currentPath).Length

        if ($InitialTail -gt 0) {
            Get-Content -LiteralPath $currentPath -Tail $InitialTail | ForEach-Object {
                Write-Visa2026StreamLine -Line $_ -Sink $Sink
            }
        }

        while ($true) {
            $latestPath = Get-LatestIisStdoutLogPath -PublishPath $PublishPath
            if ($latestPath -and $latestPath -ne $currentPath) {
                Write-Visa2026StreamLine -Line "=== switched to newer stdout log: $latestPath ===" -Sink $Sink
                $currentPath = $latestPath
                $lastLength = 0
            }

            $info = Get-Item -LiteralPath $currentPath
            if ($info.Length -gt $lastLength) {
                $stream = [System.IO.File]::Open($currentPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                try {
                    $stream.Seek($lastLength, [System.IO.SeekOrigin]::Begin) | Out-Null
                    $reader = New-Object System.IO.StreamReader($stream)
                    while (-not $reader.EndOfStream) {
                        $line = $reader.ReadLine()
                        if ($null -ne $line) {
                            Write-Visa2026StreamLine -Line $line -Sink $Sink
                        }
                    }
                    $lastLength = $stream.Position
                }
                finally {
                    $stream.Dispose()
                }
            }

            Start-Sleep -Milliseconds 500
        }
    }

}

function Resolve-Visa2026LogTransport {
    param(
        [string]$TargetHost,
        [int]$Port,
        [string]$RequestedMode,
        [string]$ProjectHint,
        [bool]$IsLocal
    )

    if ($RequestedMode -eq "Docker") {
        return [PSCustomObject]@{ Transport = "Docker" }
    }
    if ($RequestedMode -eq "IisStdout") {
        return [PSCustomObject]@{ Transport = "IisStdout" }
    }
    if ($RequestedMode -eq "IisSsh") {
        return [PSCustomObject]@{ Transport = "IisStdout"; Remote = $true }
    }

    if (Test-Visa2026DockerAvailable) {
        $container = Find-LocalDockerContainerByPort -Port $Port -ProjectHint $ProjectHint
        if ($container) {
            return [PSCustomObject]@{ Transport = "Docker"; Container = $container; Remote = $false }
        }
    }

    $slotProfile = Get-IisSlotProfileByHttpPort -Port $Port
    if ($slotProfile) {
        . $IisSlotsScript
        $ctx = Resolve-Visa2026IisSlotContext -Profile $slotProfile
        if ($IsLocal -and (Test-Path -LiteralPath $ctx.PublishPath)) {
            return [PSCustomObject]@{ Transport = "IisStdout"; PublishPath = $ctx.PublishPath; Remote = $false; Slot = $slotProfile }
        }

        return [PSCustomObject]@{ Transport = "IisStdout"; PublishPath = $ctx.PublishPath; Remote = $true; Slot = $slotProfile }
    }

    if (-not $IsLocal) {
        if (Get-Command ssh -ErrorAction SilentlyContinue) {
            return [PSCustomObject]@{ Transport = "IisStdout"; Remote = $true }
        }
        throw "Could not auto-detect log transport for ${TargetHost}:$Port. Pass -Mode Docker or -Mode IisSsh."
    }

    if ($Port -in 5000, 5001, 5050, 5051, 5052, 44318) {
        throw @"
Could not attach to console logs for ${TargetHost}:$Port.

Visual Studio F5 and standalone dotnet run keep stdout in their own process console.
This script only follows Docker container logs or IIS stdout files.

Options:
  1. Run the app in Docker and pass -HostName localhost:<published-port> (e.g. localhost:8081)
  2. Run under IIS with stdout enabled (scripts/windows-iis/Enable-Visa2026StdoutLog.ps1)
  3. Start the app yourself in a console window and watch that window directly
"@
    }

    throw "Could not auto-detect log transport for ${TargetHost}:$Port. Use -Mode Docker or -Mode IisSsh, or enable IIS stdout logging."
}

$endpoint = Parse-Visa2026HostName -Value $HostName -PortOverride $HttpPort
$isLocalHost = Test-Visa2026LocalHost -TargetHost $endpoint.Host
$probeUri = Get-Visa2026LoginProbeUri -TargetHost $endpoint.Host -Port $endpoint.Port

if (-not $SkipHttpCheck) {
    $null = Test-Visa2026HttpEndpoint -Uri $probeUri
}

$transport = Resolve-Visa2026LogTransport -TargetHost $endpoint.Host -Port $endpoint.Port -RequestedMode $Mode -ProjectHint $ComposeProject -IsLocal $isLocalHost
$sink = New-Visa2026LogSink -Path $OutFile

if ($sink) {
    Write-Host "Caching log stream to $($sink.Path)" -ForegroundColor DarkGray
}

try {
    switch ($transport.Transport) {
        "Docker" {
            $container = $transport.Container
            if (-not $container) {
                if ($transport.Remote) {
                    $remoteList = ssh $SshHost "docker ps --format '{{.Names}}|{{.Ports}}'"
                    foreach ($line in @($remoteList)) {
                        if ($line -match "(?:0\.0\.0\.0|127\.0\.0\.1|\[::\]|::):$($endpoint.Port)->") {
                            $container = ($line -split '\|', 2)[0]
                            break
                        }
                    }
                    if (-not $container -and $ComposeProject) {
                        $match = @($remoteList) | Where-Object { $_ -like "$ComposeProject-app-*" } | Select-Object -First 1
                        if ($match) { $container = ($match -split '\|', 2)[0] }
                    }
                }
                else {
                    $container = Find-LocalDockerContainerByPort -Port $endpoint.Port -ProjectHint $ComposeProject
                }
            }

            if (-not $container) {
                throw "No Docker container found publishing port $($endpoint.Port). Is the compose app running?"
            }

            Start-Visa2026DockerLogFollow -ContainerName $container -Remote:([bool]$transport.Remote) -RemoteSshHost $SshHost -InitialTail $Tail -Sink $sink
        }
        "IisStdout" {
            $publishPath = $transport.PublishPath
            if (-not $publishPath) {
                $slotProfile = Get-IisSlotProfileByHttpPort -Port $endpoint.Port
                if (-not $slotProfile) {
                    throw "Port $($endpoint.Port) does not match a known IIS slot (80/8080/8081)."
                }
                . $IisSlotsScript
                $publishPath = (Resolve-Visa2026IisSlotContext -Profile $slotProfile).PublishPath
            }

            $remote = [bool]$transport.Remote
            if ($Mode -eq "IisSsh") { $remote = $true }
            if (-not $remote -and -not $isLocalHost) { $remote = $true }

            Start-Visa2026IisStdoutFollow -PublishPath $publishPath -InitialTail $Tail -Sink $sink -Remote $remote -RemoteSshHost $SshHost
        }
        default {
            throw "Unsupported transport: $($transport.Transport)"
        }
    }
}
finally {
    if ($sink) {
        $sink.Writer.WriteLine("=== $(Get-Date -Format o) watch stopped ===")
        $sink.Writer.Flush()
        $sink.Writer.Dispose()
        Write-Host "Cached log: $($sink.Path)" -ForegroundColor DarkGray
    }
}
