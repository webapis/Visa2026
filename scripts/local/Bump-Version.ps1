#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
<#
.SYNOPSIS
  Bump Visa2026 project versions (AssemblyVersion / FileVersion) consistently.

.DESCRIPTION
  Today, CI and local docker builds derive APP_VERSION from:
    Visa2026.Module/Visa2026.Module.csproj <AssemblyVersion>

  This script updates:
  - Visa2026.Module/Visa2026.Module.csproj: <AssemblyVersion>
  - Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj: <AssemblyVersion>, <FileVersion>

  It supports bumping major/minor/patch/build of a 4-part version: A.B.C.D

.PARAMETER Part
  Which part to bump: major | minor | patch | build
  Default: build

.PARAMETER SetVersion
  Set an explicit version (A.B.C.D) instead of bumping.

.EXAMPLE
  ./scripts/local/Bump-Version.ps1
  Bumps build number (A.B.C.D -> A.B.C.(D+1))

.EXAMPLE
  ./scripts/local/Bump-Version.ps1 -Part patch
  Bumps patch (A.B.C.D -> A.B.(C+1).0)

.EXAMPLE
  ./scripts/local/Bump-Version.ps1 -SetVersion 1.2.3.4
#>
param(
    [ValidateSet('major', 'minor', 'patch', 'build')]
    [string]$Part = 'build',

    [string]$SetVersion
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$ModuleCsproj = Join-Path $RepoRoot "Visa2026.Module\Visa2026.Module.csproj"
$ServerCsproj = Join-Path $RepoRoot "Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj"

function Read-Xml([string]$path) {
    if (-not (Test-Path $path)) { throw "File not found: $path" }
    return [xml](Get-Content -Raw -LiteralPath $path)
}

function Save-Xml([xml]$xml, [string]$path) {
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.NewLineChars = "`r`n"
    $settings.NewLineHandling = "Replace"
    $settings.OmitXmlDeclaration = $true

    $sw = New-Object System.IO.StringWriter
    $xw = [System.Xml.XmlWriter]::Create($sw, $settings)
    $xml.Save($xw)
    $xw.Flush()
    $xw.Close()

    # Ensure trailing newline for git friendliness
    $content = $sw.ToString().TrimEnd() + "`r`n"
    Set-Content -LiteralPath $path -Value $content -Encoding UTF8
}

function Parse-Version4 {
    param([Parameter(Mandatory = $true)][string]$v)
    if ([string]::IsNullOrWhiteSpace($v)) { throw "Version is empty." }
    $parts = $v.Split('.')
    if ($parts.Length -ne 4) { throw "Expected 4-part version A.B.C.D, got: '$v'" }
    try {
        return @([int]$parts[0], [int]$parts[1], [int]$parts[2], [int]$parts[3])
    } catch {
        throw "Version must be numeric A.B.C.D, got: '$v'"
    }
}

function Format-Version4 {
    param([Parameter(Mandatory = $true)][int[]]$p)
    return "{0}.{1}.{2}.{3}" -f $p[0], $p[1], $p[2], $p[3]
}

function Bump-Version4 {
    param(
        [Parameter(Mandatory = $true)][int[]]$p,
        [Parameter(Mandatory = $true)]
        [ValidateSet('major', 'minor', 'patch', 'build')]
        [string]$part
    )
    switch ($part) {
        'major' { return @($p[0] + 1, 0, 0, 0) }
        'minor' { return @($p[0], $p[1] + 1, 0, 0) }
        'patch' { return @($p[0], $p[1], $p[2] + 1, 0) }
        'build' { return @($p[0], $p[1], $p[2], $p[3] + 1) }
        default { throw "Unknown part: $part" }
    }
}

function Get-PropertyNode([xml]$xml, [string]$name) {
    # Prefer the first PropertyGroup that already contains the property,
    # otherwise fall back to the first PropertyGroup.
    foreach ($pg in @($xml.Project.PropertyGroup)) {
        if ($pg.$name) { return $pg }
    }
    return $xml.Project.PropertyGroup | Select-Object -First 1
}

function Get-PropertyValue([xml]$xml, [string]$name) {
    foreach ($pg in @($xml.Project.PropertyGroup)) {
        $v = $pg.$name
        if ($v) { return [string]$v }
    }
    return $null
}

function Set-PropertyValue([xml]$xml, [string]$name, [string]$value) {
    $pg = Get-PropertyNode -xml $xml -name $name
    if (-not $pg) { throw "No <PropertyGroup> found in project file." }
    if ($pg.$name) {
        $pg.$name = $value
        return
    }

    # Create element if missing
    $node = $xml.CreateElement($name)
    $node.InnerText = $value
    [void]$pg.AppendChild($node)
}

$moduleXml = Read-Xml $ModuleCsproj
$serverXml = Read-Xml $ServerCsproj

$current = Get-PropertyValue -xml $moduleXml -name "AssemblyVersion"
if (-not $current) { throw "Could not read <AssemblyVersion> from $ModuleCsproj" }

$next = $null
if (-not [string]::IsNullOrWhiteSpace($SetVersion)) {
    $next = $SetVersion
    [void](Parse-Version4 $next) # validate
} else {
    $p = Parse-Version4 $current
    if ($env:VERSION_BUMP_DEBUG -eq '1') {
        Write-Host ("DEBUG Part='{0}' Parsed=({1})" -f $Part, ($p -join ',')) -ForegroundColor Magenta
    }
    $p2 = $null
    switch ($Part) {
        'major' {
            $maj = ([int]$p[0]) + 1
            $p2 = @($maj, 0, 0, 0)
        }
        'minor' {
            $min = ([int]$p[1]) + 1
            $p2 = @([int]$p[0], $min, 0, 0)
        }
        'patch' {
            $pat = ([int]$p[2]) + 1
            $p2 = @([int]$p[0], [int]$p[1], $pat, 0)
        }
        'build' {
            $bld = ([int]$p[3]) + 1
            $p2 = @([int]$p[0], [int]$p[1], [int]$p[2], $bld)
        }
        default { throw "Unknown Part: $Part" }
    }
    if ($env:VERSION_BUMP_DEBUG -eq '1') {
        Write-Host ("DEBUG Bumped=({0})" -f ($p2 -join ',')) -ForegroundColor Magenta
    }
    $next = Format-Version4 $p2
}

if ($next -eq $current) {
    Write-Host "No change (current already '$current')." -ForegroundColor Yellow
    exit 0
}

Write-Host "Version: $current -> $next" -ForegroundColor Cyan

if (-not $PSCmdlet.ShouldProcess("Project files", "Set version to $next (from $current)")) {
    exit 0
}

Set-PropertyValue -xml $moduleXml -name "AssemblyVersion" -value $next
Set-PropertyValue -xml $serverXml -name "AssemblyVersion" -value $next
Set-PropertyValue -xml $serverXml -name "FileVersion" -value $next

Save-Xml -xml $moduleXml -path $ModuleCsproj
Save-Xml -xml $serverXml -path $ServerCsproj

Write-Host "Updated project files." -ForegroundColor Green
