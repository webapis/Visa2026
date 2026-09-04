# UTF-8 no BOM mechanical rename: Application (case BO) -> ApplicationProfileInstance
$ErrorActionPreference = 'Stop'
$root = 'c:\Users\webap\Documents\GitHub\Visa2026'
$utf8 = New-Object System.Text.UTF8Encoding $false

$scopes = @(
  'Visa2026.Module.Tests'
)

$include = @('*.cs','*.razor','*.cshtml','*.sql','*.yaml','*.yml','*.xafml','*.json','*.ps1','*.md','*.css','*.js','*.html')

$protectPairs = @(
  @('XafApplication', '<<<XafApplication>>>'),
  @('ApplicationProfile', '<<<ApplicationProfile>>>'),
  @('ApplicationType', '<<<ApplicationType>>>'),
  @('ApplicationState', '<<<ApplicationState>>>'),
  @('ApplicationLocation', '<<<ApplicationLocation>>>'),
  @('ApplicationUser', '<<<ApplicationUser>>>'),
  @('ApplicationRuntime', '<<<ApplicationRuntime>>>'),
  @('ApplicationNumbering', '<<<ApplicationNumbering>>>'),
  @('ApplicationMigration', '<<<ApplicationMigration>>>'),
  @('ApplicationReason', '<<<ApplicationReason>>>'),
  @('ApplicationStatus', '<<<ApplicationStatus>>>'),
  @('ApplicationItem', '<<<ApplicationItem>>>')
)

# Case-sensitive pairs (Id vs ID matter)
$renamePairs = @(
  @('ApplicationApprovalLegSnapshot', 'ApplicationProfileInstanceApprovalLegSnapshot'),
  @('ApplicationPersonResolvedLink', 'ApplicationProfileInstancePersonResolvedLink'),
  @('ApplicationPersonLinkKind', 'ApplicationProfileInstancePersonLinkKind'),
  @('ApplicationPeople', 'ApplicationProfileInstancePeople'),
  @('ApplicationPerson', 'ApplicationProfileInstancePerson'),
  @('ApplicationProgress', 'ApplicationProfileInstanceProgress'),
  @('IssuingApplicationID', 'IssuingApplicationProfileInstanceID'),
  @('IssuingApplications', 'IssuingApplicationProfileInstances'),
  @('IssuingApplication', 'IssuingApplicationProfileInstance'),
  @('AvailableApplications', 'AvailableApplicationProfileInstances'),
  @('CancelApplications', 'CancelApplicationProfileInstances'),
  @('"Applications"', '"ApplicationProfileInstances"'),
  @('DbSet<Application>', 'DbSet<ApplicationProfileInstance>'),
  @('IList<Application>', 'IList<ApplicationProfileInstance>'),
  @('ICollection<Application>', 'ICollection<ApplicationProfileInstance>'),
  @('ObservableCollection<Application>', 'ObservableCollection<ApplicationProfileInstance>'),
  @('CreateObject<Application>', 'CreateObject<ApplicationProfileInstance>'),
  @('GetObjectByKey<Application>', 'GetObjectByKey<ApplicationProfileInstance>'),
  @('FindObject<Application>', 'FindObject<ApplicationProfileInstance>'),
  @('typeof(Application)', 'typeof(ApplicationProfileInstance)'),
  @('nameof(Application)', 'nameof(ApplicationProfileInstance)'),
  @('Application?', 'ApplicationProfileInstance?'),
  @('Application[]', 'ApplicationProfileInstance[]'),
  @('ApplicationOid', 'ApplicationProfileInstanceOid'),
  @('ApplicationID', 'ApplicationProfileInstanceID'),
  @('ApplicationId', 'ApplicationProfileInstanceId')
)

function Protect-Text([string]$text) {
  foreach ($p in $protectPairs) { $text = $text.Replace($p[0], $p[1]) }
  return $text
}

function Unprotect-Text([string]$text) {
  foreach ($p in $protectPairs) { $text = $text.Replace($p[1], $p[0]) }
  return $text
}

function Rename-Core([string]$text) {
  $text = $text.Replace('Application Profile', '<<<Application_Profile_phrase>>>')
  foreach ($p in $renamePairs) { $text = $text.Replace($p[0], $p[1]) }
  $text = [regex]::Replace($text, '\bpartial class Application\b', 'partial class ApplicationProfileInstance')
  $text = [regex]::Replace($text, '\bclass Application\b', 'class ApplicationProfileInstance')
  $text = [regex]::Replace($text, '<Application>', '<ApplicationProfileInstance>')
  $text = [regex]::Replace($text, '\(Application\)', '(ApplicationProfileInstance)')
  $text = [regex]::Replace($text, '(?<![\.\w])Application(?=[\s\>\),;\]]|$)', 'ApplicationProfileInstance')
  $text = $text.Replace('virtual ApplicationProfileInstance Application {', 'virtual ApplicationProfileInstance ApplicationProfileInstance {')
  $text = $text.Replace('virtual ApplicationProfileInstance Application;', 'virtual ApplicationProfileInstance ApplicationProfileInstance;')
  $text = $text.Replace('public ApplicationProfileInstance Application {', 'public ApplicationProfileInstance ApplicationProfileInstance {')
  $text = $text.Replace('IList<ApplicationProfileInstance> Applications', 'IList<ApplicationProfileInstance> Instances')
  $text = $text.Replace('WithMany(p => p.Applications)', 'WithMany(p => p.Instances)')
  $text = $text.Replace('[XafDisplayName("Application")]', '[XafDisplayName("Application Profile instance")]')
  $text = $text.Replace('--entity Application', '--entity ApplicationProfileInstance')
  $text = $text.Replace('<<<Application_Profile_phrase>>>', 'Application Profile')
  return $text
}

$filesChanged = 0
foreach ($scope in $scopes) {
  $dir = Join-Path $root $scope
  if (-not (Test-Path $dir)) { continue }
  Get-ChildItem $dir -Recurse -File -Include $include | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|\.git|\.vs|node_modules)\\' -and
    $_.Name -ne 'Rename-ApplicationToProfileInstance.ps1'
  } | ForEach-Object {
    $path = $_.FullName
    $original = [System.IO.File]::ReadAllText($path)
    $text = Protect-Text $original
    $text = Rename-Core $text
    $text = Unprotect-Text $text
    if ($text -ne $original) {
      [System.IO.File]::WriteAllText($path, $text, $utf8)
      $script:filesChanged++
      Write-Output ("updated: " + $path.Substring($root.Length + 1))
    }
  }
}

$fileRenames = @{}
foreach ($k in $fileRenames.Keys) {
  $from = Join-Path $root $k
  $to = Join-Path $root $fileRenames[$k]
  if ((Test-Path $from) -and -not (Test-Path $to)) {
    Move-Item -LiteralPath $from -Destination $to
    Write-Output ("renamed file: " + $k)
  }
}

Write-Output "DONE filesChanged=$filesChanged"