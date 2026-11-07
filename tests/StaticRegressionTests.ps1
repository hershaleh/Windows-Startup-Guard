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
Test-Contract 'Guide explains user problems and outcomes' $form 'המחשב עולה לאט או נפתחות הרבה תוכנות'
Test-Contract 'Guide explains undoing a disabled app' $form 'Restore selected'
Test-Contract 'Guide explains app remains installed' $form 'הן נשארות מותקנות ואפשר לפתוח אותן ידנית.'
Test-Contract 'Light and dark theme toggle' $form 'ApplyTheme()'
Test-Contract 'Light theme palette' $form 'Color.FromArgb(243, 246, 250)'
Test-Contract 'Credit displayed in a styled panel' $form 'Name = "SummaryCard"'
Test-Contract 'Creator image embedded' $project 'Assets\hershaleh.jpg'

Test-Contract 'Formatted guide uses RichTextBox' $form 'new RichTextBox'
Test-Contract 'Guide headings have distinct typography' $form 'SelectionFont = new Font("Segoe UI Semibold", 13, FontStyle.Bold)'
Test-Contract 'Guide text uses theme-aware foreground' $form 'ForeColor = _textColor'
Test-Contract 'Header actions share aligned flow layout' $form 'FlowDirection = FlowDirection.RightToLeft'
Test-Contract 'Header action buttons have equal widths' $form '_guideButton.Size = new Size(112, 36)'


Test-Contract 'Buttons use rounded regions' $form 'ApplyRoundedRegion(button, 12)'
Test-Contract 'Buttons have hover feedback' $form 'FlatAppearance.MouseOverBackColor'
Test-Contract 'Modern drawing smoothing' $form 'using System.Drawing.Drawing2D;'


Test-Contract 'Report problem button' $form 'ReportProblem()'
Test-Contract 'GitHub issue report link' (Get-Content (Join-Path $PSScriptRoot '..\ErrorReporter.cs') -Raw) 'github.com/hershaleh/Windows-Startup-Guard/issues/new'
Test-Contract 'Error log stores exception details' (Get-Content (Join-Path $PSScriptRoot '..\ErrorReporter.cs') -Raw) 'exception.ToString()'
Test-Contract 'Report log redacts secrets' (Get-Content (Join-Path $PSScriptRoot '..\ErrorReporter.cs') -Raw) '[redacted]'
Test-Contract 'Report requires user confirmation on GitHub' (Get-Content (Join-Path $PSScriptRoot '..\ErrorReporter.cs') -Raw) 'Nothing is sent until you click Submit'
Test-Contract 'Branded splash screen' (Get-Content (Join-Path $PSScriptRoot '..\SplashScreen.cs') -Raw) 'Windows Startup Guard'
Test-Contract 'Animated WPF splash shown before main window' (Get-Content (Join-Path $PSScriptRoot '..\App.xaml.cs') -Raw) 'splash.ShowDialog()'
Test-Contract 'Executable icon configured' $project '<ApplicationIcon>Assets\WindowsStartupGuard.ico</ApplicationIcon>'
Test-Contract 'Icon generated before build' $project 'GenerateApplicationIcon'
Test-Contract 'Icon generator script exists' (Get-Content (Join-Path $PSScriptRoot '..\tools\GenerateIcon.ps1') -Raw) 'WindowsStartupGuard.ico'
Test-Contract 'Light theme enabled by default' $form 'private bool _lightTheme = true;'
Test-Contract 'Dashboard cards use rounded corners' $form 'ApplyRoundedRegion(card, 16)'


$wpfXaml = Get-Content (Join-Path $PSScriptRoot '..\MainWindow.xaml') -Raw
$wpfCode = Get-Content (Join-Path $PSScriptRoot '..\MainWindow.xaml.cs') -Raw
$dialogCode = Get-Content (Join-Path $PSScriptRoot '..\AppDialog.cs') -Raw
$splashXaml = Get-Content (Join-Path $PSScriptRoot '..\SplashWindow.xaml') -Raw
$appResources = Get-Content (Join-Path $PSScriptRoot '..\App.xaml') -Raw
Test-Contract 'WPF header has separate action column' $wpfXaml '<ColumnDefinition Width="Auto"/>'
Test-Contract 'WPF status and actions do not overlap' $wpfXaml 'Grid.Column="1" Orientation="Horizontal"'
Test-Contract 'WPF startup loading bar grows right-to-left' $splashXaml 'HorizontalAlignment="Right" Background="#75A2FF"'
Test-Contract 'WPF guide uses custom designed dialog' $wpfCode 'AppDialog.Guide(this, _dark, _hebrew)'
Test-Contract 'Guide has detailed restore instructions' $dialogCode 'שחזור וביטול שינויים'
Test-Contract 'Guide explains scan coverage' $dialogCode 'מה נכלל בסריקה?'
Test-Contract 'Confirmations use modern themed dialogs' $wpfCode 'AppDialog.Confirm(this'
Test-NotContains 'No legacy WPF message boxes' $wpfCode 'System.Windows.MessageBox.Show'
Test-Contract 'Dark theme includes control backgrounds' $wpfCode '["ControlBg"]="#202F45"'
Test-Contract 'Dark theme includes grid header colors' $wpfCode '["GridHeaderBg"]="#24334A"'
Test-Contract 'Data grid rows use dynamic theme resources' $appResources 'DynamicResource GridAltBg'
Test-Contract 'Creator avatar loaded from embedded resource' $wpfCode 'WindowsStartupGuard.Assets.hershaleh.jpg'
Test-Contract 'Creator profile is clickable' $wpfXaml 'https://mitmachim.top/user/%D7%94%D7%A2%D7%A8%D7%A9%D7%9C%D7%94'
Test-Contract 'Creator avatar is shown in sidebar' $wpfXaml 'x:Name="CreditAvatar"'
Test-Contract 'Language toggle retained in WPF' $wpfXaml 'Click="Language_Click"'
Test-Contract 'Language toggle updates RTL and LTR' $wpfCode 'FlowDirection = _hebrew ? FlowDirection.RightToLeft : FlowDirection.LeftToRight'
Test-Contract 'Dashboard navigation is wired' $wpfXaml 'Click="Dashboard_Click"'
Test-Contract 'Startup navigation is wired' $wpfXaml 'Click="Startup_Click"'
Test-Contract 'Dashboard cards animate into view' $wpfCode 'AnimateCard(CardProtection, 280)'
Test-Contract 'Window entrance animation retained' $wpfCode 'Root.BeginAnimation(OpacityProperty, fade)'
Test-Contract 'Button hover animation in dialogs' $dialogCode 'DoubleAnimation(0.78'

Write-Host "Regression checks passed: $passed"
if ($failed.Count -gt 0) { Write-Host "Regression checks failed ($($failed.Count)):"; $failed | ForEach-Object { Write-Host " - $_" }; exit 1 }
if ($passed -lt 45) { throw "Expected at least 45 regression checks; ran $passed." }
