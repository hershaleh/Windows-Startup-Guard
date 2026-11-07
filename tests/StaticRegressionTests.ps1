$ErrorActionPreference = 'Stop'
$source = Get-Content (Join-Path $PSScriptRoot '..\StartupManager.cs') -Raw
$form = Get-Content (Join-Path $PSScriptRoot '..\MainForm.cs') -Raw
$project = Get-Content (Join-Path $PSScriptRoot '..\WindowsStartupGuard.csproj') -Raw
$workflow = Get-Content (Join-Path $PSScriptRoot '..\.github\workflows\build.yml') -Raw
$passed = 0
$failed = [System.Collections.Generic.List[string]]::new()
function Test-Contract([string]$name, [string]$text, [string]$expected) {
    if ($text.Contains($expected)) { $script:passed++ } else { $script:failed.Add($name) }
}
function Test-NotContains([string]$name, [string]$text, [string]$unexpected) {
    if (-not $text.Contains($unexpected)) { $script:passed++ } else { $script:failed.Add($name) }
}
Test-Contract 'HKCU Run discovery' $source 'Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"'
Test-Contract 'HKCU RunOnce discovery' $source 'Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"'
Test-Contract 'HKLM Run discovery' $source 'Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"'
Test-Contract 'HKLM RunOnce discovery' $source 'Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"'
Test-Contract '32-bit Run discovery' $source 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run'
Test-Contract '32-bit RunOnce discovery' $source 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce'
Test-Contract 'Current-user Startup folder' $source 'Environment.SpecialFolder.Startup'
Test-Contract 'Shared Startup folder' $source 'Environment.SpecialFolder.CommonStartup'
Test-Contract 'Inventory sorting' $source 'OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)'
Test-Contract 'Registry ID prefix validation' $source 'parts[0].Equals("reg", StringComparison.Ordinal)'
Test-Contract 'Registry ID shape validation' $source 'parts.Length != 4'
Test-Contract 'Registry hive allowlist' $source 'Registry.CurrentUser.Name'
Test-Contract 'Registry target allowlist' $source 'IsAllowedRegistryTarget(parts[1], parts[2])'
Test-Contract 'Registry backup collision protection' $source 'A backup for this startup item already exists.'
Test-Contract 'Registry backup before deletion' $source 'backup.SetValue(backupName'
Test-Contract 'Registry deletion rollback' $source 'backup.DeleteValue(backupName, false)'
Test-Contract 'Registry value existence checked before restore' $source 'target.GetValue(record.Name'
Test-Contract 'Registry backup identity validation' $source 'expectedBackupName'
Test-Contract 'Registry type validation' $source 'Enum.IsDefined(kind)'
Test-Contract 'Binary backup preservation' $source 'Convert.ToBase64String(bytes)'
Test-Contract 'Multi-string backup preservation' $source 'JsonSerializer.Serialize(strings)'
Test-Contract 'Unsupported registry types rejected' $source 'This registry value type is not supported'
Test-Contract 'No HKCU WOW6432Node restore' $source 'path.StartsWith(@"Software\WOW6432Node"'
Test-Contract 'Startup file path allowlist on disable' $source 'Only files inside a Windows Startup folder can be disabled.'
Test-Contract 'Startup file path canonicalization' $source 'file = Path.GetFullPath(file);'
Test-Contract 'File backup metadata written first' $source 'File.WriteAllText(metadata'
Test-Contract 'File move failure cleans metadata' $source 'File.Delete(metadata)'
Test-Contract 'File restore path allowlist' $source 'IsAllowedStartupFolderPath(record.OriginalPath)'
Test-Contract 'Selected metadata path restricted' $source 'IsBackupMetadataPath'
Test-Contract 'Restore collision avoids overwrite' $source '.restored-'
Test-Contract 'File backups use unique identifiers' $source 'Guid.NewGuid().ToString("N")'
Test-Contract 'Restore only allows canonical registry targets' $source 'IsAllowedRegistryTarget(parsed.Hive, parsed.Path)'
Test-Contract 'Registry restore preserves original value kind' $source 'target.SetValue(record.Name, value, kind)'
Test-Contract 'Metadata must be JSON' $source 'path.EndsWith(".json"'
Test-Contract 'Metadata confined to backup directory' $source 'Path.GetDirectoryName(fullPath)'
Test-Contract 'Process matching by executable name' $source 'Process.GetProcessesByName(name)'
Test-Contract 'Exact executable path comparison' $source 'StringComparison.OrdinalIgnoreCase'
Test-Contract 'Graceful process close only' $source 'process.CloseMainWindow()'
Test-Contract 'Graceful close timeout' $source 'process.WaitForExit(3000)'
Test-NotContains 'No forced process termination' $source 'process.Kill('
Test-Contract 'Disable confirmation' $form 'Disable selected startup items?'
Test-Contract 'Restore confirmation' $form 'Restore all backed-up startup items?'
Test-Contract 'Selected restore action' $form 'RestoreSelected(backupIds)'
Test-Contract 'Disabled items filter' $form 'disabled.Where(x =>'
Test-Contract 'CSV quoting' $form 'value.Replace("\"", "\"\"")'
Test-Contract 'CSV formula mitigation' $form "firstMeaningful is '=' or '+' or '-' or '@'"
Test-Contract 'UTF-8 BOM export' $form 'new System.Text.UTF8Encoding(true)'
Test-Contract 'Admin status visibility' $form 'WindowsBuiltInRole.Administrator'
Test-Contract 'Disable action ignores disabled rows' $form 'Selected().Where(x => x.IsEnabled)'
Test-Contract 'Nullable reference checks' $project '<Nullable>enable</Nullable>'
Test-Contract '.NET 10 Windows target' $project '<TargetFramework>net10.0-windows</TargetFramework>'
Test-Contract 'WinForms enabled' $project '<UseWindowsForms>true</UseWindowsForms>'
Test-Contract 'Windows runner' $workflow 'runs-on: windows-latest'
Test-Contract 'Release build step' $workflow 'dotnet build WindowsStartupGuard.csproj --configuration Release'
Test-Contract 'Self-contained publish' $workflow '--self-contained true'
Test-Contract 'Single-file publish' $workflow '-p:PublishSingleFile=true'
Test-Contract 'In-app guide action' $form 'ShowGuide()'
Test-Contract 'Guide explains safe disable and restore' $form 'נטרול נבחרים'
Test-Contract 'Creator credit link' $form 'https://mitmachim.top/user/%D7%94%D7%A2%D7%A8%D7%A9%D7%9C%D7%94'
Test-Contract 'Embedded creator image' $project 'Assets\hershaleh.jpg'
Test-Contract 'Guide explains user problems and outcomes' $form 'איזו בעיה יש לך?'
Test-Contract 'Guide explains undoing a disabled app' $form 'Restore selected'
Test-Contract 'Guide explains app remains installed' $form 'התוכנה עצמה נשארת מותקנת'
Test-Contract 'Light and dark theme toggle' $form 'ApplyTheme()'
Test-Contract 'Light theme palette' $form 'Color.FromArgb(243, 246, 250)'
Test-Contract 'Credit displayed in a styled panel' $form 'Name = "SummaryCard"'
Test-Contract 'Creator image embedded' $project 'Assets\hershaleh.jpg'
Write-Host "Regression checks passed: $passed"
if ($failed.Count -gt 0) { Write-Host "Regression checks failed ($($failed.Count)):"; $failed | ForEach-Object { Write-Host " - $_" }; exit 1 }
if ($passed -lt 45) { throw "Expected at least 45 regression checks; ran $passed." }
