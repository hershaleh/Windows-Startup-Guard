using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace WindowsStartupGuard;

public partial class MainWindow : System.Windows.Window
{
    private readonly StartupManager _manager = new();
    private readonly ObservableCollection<StartupRow> _rows = [];
    private List<StartupEntry> _enabled = [];
    private List<StartupManager.DisabledStartupBackup> _disabled = [];
    private bool _dark;
    private bool _loading;
    private bool _hebrew = true;

    public MainWindow()
    {
        InitializeComponent();
        StartupGrid.ItemsSource = _rows;
    }

    private async void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        await Task.Delay(40);
        var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(520))
        { EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } };
        Root.BeginAnimation(OpacityProperty, fade);
        RefreshInventory();
    }

    private void RefreshInventory()
    {
        try
        {
            _loading = true;
            _enabled = _manager.GetEntries();
            _disabled = _manager.GetDisabledBackups();
            _rows.Clear();
            foreach (var item in _enabled)
                _rows.Add(new StartupRow(item.Name, item.Publisher, "מופעל", item.Source == StartupSource.Registry ? "רישום מערכת" : "תיקיית אתחול",
                    item.Scope == StartupScope.AllUsers ? "כל המשתמשים" : "המשתמש הנוכחי", item.FileVersion,
                    item.ExecutablePath ?? item.Command, item, null));
            foreach (var item in _disabled)
                _rows.Add(new StartupRow(item.Name, item.Publisher, "מושבת", item.Source == StartupSource.Registry ? "רישום מערכת" : "תיקיית אתחול",
                    item.Scope == StartupScope.AllUsers ? "כל המשתמשים" : "המשתמש הנוכחי", item.FileVersion,
                    item.ExecutablePath ?? item.Command, null, item));
            ItemsCount.Text = _rows.Count.ToString();
            BackupCount.Text = _disabled.Count.ToString();
            ApplyFilter();
            StatusText.Text = $"נסרקו {_enabled.Count} פריטים פעילים ונמצאו {_disabled.Count} גיבויים";
        }
        catch (Exception ex) { ErrorReporter.LogException(ex, "Refresh inventory"); ShowError(ex); }
        finally { _loading = false; }
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text.Trim();
        var currentOnly = ScopeFilter.SelectedIndex == 1;
        StartupGrid.ItemsSource = _rows.Where(x =>
            (!currentOnly || x.Scope == "המשתמש הנוכחי") &&
            (query.Length == 0 || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Path.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private List<StartupRow> SelectedRows() => StartupGrid.SelectedItems.Cast<StartupRow>().ToList();
    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();
    private void ScopeFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!_loading && IsLoaded) ApplyFilter(); }
    private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e) => RefreshInventory();

    private void Disable_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var selected = SelectedRows().Where(x => x.EnabledItem != null).ToList();
        if (selected.Count == 0) { Info("בחר פריטים פעילים לנטרול."); return; }
        if (System.Windows.MessageBox.Show($"לנטרל {selected.Count} פריטים? יישמר גיבוי לשחזור.", Title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
        foreach (var row in selected)
            try { _manager.Disable(row.EnabledItem!); }
            catch (Exception ex) { ErrorReporter.LogException(ex, "Disable " + row.Name); ShowError(ex); }
        RefreshInventory();
    }

    private void Stop_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var selected = SelectedRows().Where(x => x.EnabledItem != null).ToList();
        if (selected.Count != 1) { Info("בחר תוכנה פעילה אחת בלבד."); return; }
        if (System.Windows.MessageBox.Show("לנסות לסגור את התהליך? שמור עבודה פתוחה תחילה.", Title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
        try { _manager.StopMatchingProcesses(selected[0].EnabledItem!); StatusText.Text = "נשלחה בקשת סגירה"; }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreSelected_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var ids = SelectedRows().Where(x => x.BackupItem != null).Select(x => x.BackupItem!.BackupId).Distinct().ToList();
        if (ids.Count == 0) { Info("בחר פריטים מושבתים לשחזור."); return; }
        if (System.Windows.MessageBox.Show($"לשחזר {ids.Count} פריטים?", Title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
        try { var results = _manager.RestoreSelected(ids); RefreshInventory(); Info(string.Join(Environment.NewLine, results.Take(25))); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreAll_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_manager.DisabledBackupCount() == 0) { Info("לא נמצאו גיבויים לשחזור."); return; }
        if (System.Windows.MessageBox.Show("לשחזר את כל הגיבויים?", Title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
        try { var results = _manager.RestoreAll(); RefreshInventory(); Info(string.Join(Environment.NewLine, results.Take(30))); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Export_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "startup-inventory.csv" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var lines = new List<string> { "Name,Publisher,Status,Source,Scope,Version,Path" };
            foreach (var row in _rows) lines.Add(string.Join(",", new[] { row.Name,row.Publisher,row.Status,row.Source,row.Scope,row.Version,row.Path }.Select(CsvEscape)));
            File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
            StatusText.Text = "הייצוא הושלם";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static string CsvEscape(string value)
    {
        value ??= "";
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@') value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private void Theme_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _dark = !_dark;
        SetBrush("AppBg", _dark ? "#101827" : "#F3F6FC");
        SetBrush("PanelBg", _dark ? "#1B293D" : "#FFFFFF");
        SetBrush("TextMain", _dark ? "#F1F5FC" : "#18243B");
        SetBrush("TextMuted", _dark ? "#A6B4C9" : "#77849A");
        SetBrush("Line", _dark ? "#34435A" : "#E6EBF3");
        SetBrush("SoftBlue", _dark ? "#253B60" : "#EAF0FF");
        ThemeButton.Content = _dark ? "☼  מצב בהיר" : "☾  מצב כהה";
        StartupGrid.AlternatingRowBackground = Brush(_dark ? "#202F45" : "#F8FAFD");
    }

    private void SetBrush(string key, string color) => Resources[key] = Brush(color);
    private static System.Windows.Media.Brush Brush(string color) => new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

    private void Guide_Click(object sender, System.Windows.RoutedEventArgs e) =>
        Info("Windows Startup Guard מציגה תוכנות המוגדרות לעלות עם Windows. בחר פריט ולחץ על ״נטרול נבחרים״ כדי להסירו מהאתחול ולשמור גיבוי. לשחזור, בחר פריט מושבת ולחץ ״שחזור נבחרים״. סגירת תהליך מבקשת סגירה רגילה בלבד ואינה משנה את הגדרת האתחול. רענון סורק מחדש; ייצוא CSV שומר את הרשימה. הגרסה הנוכחית סורקת Run/RunOnce ותיקיות אתחול, ולא שירותים או משימות מתוזמנות.");

    private void Report_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(ErrorReporter.CreateIssueUrl(_hebrew)) { UseShellExecute = true }); StatusText.Text = "נפתח טופס דיווח ב-GitHub"; }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Info(string message) => System.Windows.MessageBox.Show(message, Title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    private void ShowError(Exception ex) { ErrorReporter.LogException(ex, "WPF UI"); System.Windows.MessageBox.Show(ex.Message, Title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }

    private sealed record StartupRow(string Name, string Publisher, string Status, string Source, string Scope, string Version, string Path,
        StartupEntry? EnabledItem, StartupManager.DisabledStartupBackup? BackupItem);
}