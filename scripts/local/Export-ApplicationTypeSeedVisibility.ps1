# Regenerates Visa2026.DataImporter/seed/application-type-visibility.json from Module ApplicationType seed.
# Run from repo root after changing ApplicationTypeConfigurationSeed.Data.cs.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$cs = Get-Content (Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate/ApplicationTypeConfigurationSeed.Data.cs') -Raw
$types = [ordered]@{}
foreach ($m in [regex]::Matches($cs, 'Name = "(App_[^"]+)"')) {
  $name = $m.Groups[1].Value
  $start = $m.Index
  $end = $cs.IndexOf('new ApplicationTypeConfigurationRow', $start + 1)
  if ($end -lt 0) { $end = $cs.Length }
  $block = $cs.Substring($start, $end - $start)
  $showLine = [regex]::Match($block, '(ShowProjectContract = [^\r\n]+)').Groups[1].Value
  $flags = [ordered]@{}
  foreach ($kv in [regex]::Matches($showLine, '(Show\w+) = (true|false)')) {
    $flags[$kv.Groups[1].Value] = ($kv.Groups[2].Value -eq 'true')
  }
  $types[$name] = $flags
}
$outPath = Join-Path $repoRoot 'Visa2026.DataImporter/seed/application-type-visibility.json'
$obj = @{ version = 1; generatedFrom = 'ApplicationTypeConfigurationSeed.Data.cs'; applicationTypes = $types }
[System.IO.File]::WriteAllText($outPath, ($obj | ConvertTo-Json -Depth 6))
Write-Host "Wrote $($types.Count) application types to $outPath"
