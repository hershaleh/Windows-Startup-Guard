using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace WindowsStartupGuard;

public partial class MainWindow : Window
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
        LoadCreatorAvatar();
        ApplyLanguage();
    }

    private void LoadCreatorAvatar()
    {
        try
        {
            using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("WindowsStartupGuard.Assets.hershaleh.jpg");
            if (stream is null) return;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            CreditAvatar.Fill = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
        }
        catch (Exception ex) { ErrorReporter.LogException(ex, "Load creator avatar"); }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(35);
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(480))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Root.BeginAnimation(OpacityProperty, fade);
        AnimateButtons(Root);
        AnimateCard(CardItems, 80);
        AnimateCard(CardBackups, 180);
        AnimateCard(CardProtection, 280);
        StartupGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(650)) { BeginTime = TimeSpan.FromMilliseconds(180) });
        RefreshInventory();
    }

    private static void AnimateButtons(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is System.Windows.Controls.Button button)
            {
                var scale = new ScaleTransform(1, 1);
                button.RenderTransformOrigin = new Point(.5, .5);
                button.RenderTransform = scale;
                button.MouseEnter += (_, _) =>
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.035, TimeSpan.FromMilliseconds(120)));
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.035, TimeSpan.FromMilliseconds(120)));
                    button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.88, TimeSpan.FromMilliseconds(120)));
                };
                button.MouseLeave += (_, _) =>
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));
                    button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));
                };
            }
            AnimateButtons(child);
        }
    }

    private static void AnimateCard(FrameworkElement card, int delay)
    {
        var opacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(520))
        { BeginTime = TimeSpan.FromMilliseconds(delay), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        card.BeginAnimation(OpacityProperty, opacity);
        if (card.RenderTransform is TranslateTransform translate)
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(520))
            { BeginTime = TimeSpan.FromMilliseconds(delay), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private string T(string hebrew, string english) => _hebrew ? hebrew : english;

    private void RefreshInventory()
    {
        try
        {
            _loading = true;
            _enabled = _manager.GetEntries();
            _disabled = _manager.GetDisabledBackups();
            _rows.Clear();
            foreach (var item in _enabled)
                _rows.Add(new StartupRow(item.Name, item.Publisher, T("מופעל", "Enabled"),
                    item.Source == StartupSource.Registry ? T("רישום מערכת", "Registry") : T("תיקיית אתחול", "Startup folder"),
                    item.Scope == StartupScope.AllUsers ? T("כל המשתמשים", "All users") : T("המשתמש הנוכחי", "Current user"),
                    item.FileVersion, item.ExecutablePath ?? item.Command, item, null));
            foreach (var item in _disabled)
                _rows.Add(new StartupRow(item.Name, item.Publisher, T("מושבת", "Disabled"),
                    item.Source == StartupSource.Registry ? T("רישום מערכת", "Registry") : T("תיקיית אתחול", "Startup folder"),
                    item.Scope == StartupScope.AllUsers ? T("כל המשתמשים", "All users") : T("המשתמש הנוכחי", "Current user"),
                    item.FileVersion, item.ExecutablePath ?? item.Command, null, item));
            ItemsCount.Text = _rows.Count.ToString();
            BackupCount.Text = _disabled.Count.ToString();
            ApplyFilter();
            StatusText.Text = T($"נסרקו {_enabled.Count} פריטים פעילים ונמצאו {_disabled.Count} גיבויים",
                $"Scanned {_enabled.Count} active entries and found {_disabled.Count} backups");
        }
        catch (Exception ex) { ErrorReporter.LogException(ex, "Refresh inventory"); ShowError(ex); }
        finally { _loading = false; }
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text.Trim();
        var currentOnly = ScopeFilter.SelectedIndex == 1;
        StartupGrid.ItemsSource = _rows.Where(x =>
            (!currentOnly || x.Scope == T("המשתמש הנוכחי", "Current user")) &&
            (query.Length == 0 || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Path.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private List<StartupRow> SelectedRows() => StartupGrid.SelectedItems.Cast<StartupRow>().ToList();
    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();
    private void ScopeFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!_loading && IsLoaded) ApplyFilter(); }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshInventory();

    private void Disable_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedRows().Where(x => x.EnabledItem != null).ToList();
        if (selected.Count == 0) { Info(T("בחר פריטים פעילים לנטרול.", "Select active entries to disable.")); return; }
        if (!AppDialog.Confirm(this, T("אישור נטרול", "Confirm disable"),
            T($"לנטרל {selected.Count} פריטים? יישמר גיבוי לשחזור.", $"Disable {selected.Count} entries? A backup will be saved for restoration."), _dark)) return;
        foreach (var row in selected)
            try { _manager.Disable(row.EnabledItem!); }
            catch (Exception ex) { ErrorReporter.LogException(ex, "Disable " + row.Name); ShowError(ex); }
        RefreshInventory();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedRows().Where(x => x.EnabledItem != null).ToList();
        if (selected.Count != 1) { Info(T("בחר תוכנה פעילה אחת בלבד.", "Select exactly one active application.")); return; }
        if (!AppDialog.Confirm(this, T("סגירת תהליך", "Close process"),
            T("לנסות לסגור את התהליך? שמור עבודה פתוחה תחילה.", "Ask the application to close? Save open work first."), _dark)) return;
        try { _manager.StopMatchingProcesses(selected[0].EnabledItem!); StatusText.Text = T("נשלחה בקשת סגירה", "A graceful close request was sent"); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreSelected_Click(object sender, RoutedEventArgs e)
    {
        var ids = SelectedRows().Where(x => x.BackupItem != null).Select(x => x.BackupItem!.BackupId).Distinct().ToList();
        if (ids.Count == 0) { Info(T("בחר פריטים מושבתים לשחזור.", "Select disabled entries to restore.")); return; }
        if (!AppDialog.Confirm(this, T("אישור שחזור", "Confirm restore"),
            T($"לשחזר {ids.Count} פריטים?", $"Restore {ids.Count} entries?"), _dark)) return;
        try { var results = _manager.RestoreSelected(ids); RefreshInventory(); Info(string.Join(Environment.NewLine, results.Take(25))); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreAll_Click(object sender, RoutedEventArgs e)
    {
        if (_manager.DisabledBackupCount() == 0) { Info(T("לא נמצאו גיבויים לשחזור.", "No backups were found to restore.")); return; }
        if (!AppDialog.Confirm(this, T("שחזור כל הגיבויים", "Restore all backups"),
            T("לשחזר את כל הגיבויים? פריטים קיימים לא יידרסו.", "Restore all backups? Existing entries will not be overwritten."), _dark)) return;
        try { var results = _manager.RestoreAll(); RefreshInventory(); Info(string.Join(Environment.NewLine, results.Take(30))); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "startup-inventory.csv" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var lines = new List<string> { "Name,Publisher,Status,Source,Scope,Version,Path" };
            foreach (var row in _rows) lines.Add(string.Join(",", new[] { row.Name,row.Publisher,row.Status,row.Source,row.Scope,row.Version,row.Path }.Select(CsvEscape)));
            File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
            StatusText.Text = T("הייצוא הושלם", "Export completed");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static string CsvEscape(string value)
    {
        value ??= "";
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@') value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;
        var palette = _dark
            ? new Dictionary<string, string> {
                ["AppBg"]="#101827", ["PanelBg"]="#1B293D", ["TextMain"]="#F1F5FC", ["TextMuted"]="#A6B4C9",
                ["Line"]="#34435A", ["SoftBlue"]="#253B60", ["ControlBg"]="#202F45", ["GridHeaderBg"]="#24334A",
                ["GridAltBg"]="#202F45", ["GridSelectionBg"]="#344D75" }
            : new Dictionary<string, string> {
                ["AppBg"]="#F3F6FC", ["PanelBg"]="#FFFFFF", ["TextMain"]="#18243B", ["TextMuted"]="#77849A",
                ["Line"]="#E6EBF3", ["SoftBlue"]="#EAF0FF", ["ControlBg"]="#FFFFFF", ["GridHeaderBg"]="#F5F7FB",
                ["GridAltBg"]="#F8FAFD", ["GridSelectionBg"]="#DCE8FF" };
        foreach (var pair in palette) Application.Current.Resources[pair.Key] = Brush(pair.Value);
        ThemeButton.Content = T(_dark ? "☼  מצב בהיר" : "☾  מצב כהה", _dark ? "☼  Light mode" : "☾  Dark mode");
    }

    private void Language_Click(object sender, RoutedEventArgs e)
    {
        _hebrew = !_hebrew;
        ApplyLanguage();
        RefreshInventory();
    }

    private void ApplyLanguage()
    {
        FlowDirection = _hebrew ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Greeting.Text = T("שלום וברוכים הבאים", "Welcome");
        Subtitle.Text = T("ניהול פשוט ובטוח של התוכנות שעולות עם Windows", "Simple, safe management of apps that start with Windows");
        SectionHeading.Text = T("תוכנות אתחול", "Startup applications");
        SectionSubtitle.Text = T("בדוק מה מופעל, נטרל פריטים מיותרים ושחזר בכל עת", "Review active entries, disable what you do not need, and restore at any time");
        ItemsHeading.Text = T("פריטי אתחול", "Startup items");
        LanguageButton.Content = T("עברית / EN", "English / עב");
        ThemeButton.Content = T(_dark ? "☼  מצב בהיר" : "☾  מצב כהה", _dark ? "☼  Light mode" : "☾  Dark mode");
        GuideButton.Content = T("מדריך", "Guide");
        ReportButton.Content = T("דיווח תקלה", "Report a problem");
        RefreshButton.Content = T("רענון", "Refresh");
        DisableButton.Content = T("נטרול נבחרים", "Disable selected");
        StopButton.Content = T("סגירת תהליך", "Close process");
        RestoreSelectedButton.Content = T("שחזור נבחרים", "Restore selected");
        RestoreAllButton.Content = T("שחזור הכול", "Restore all");
        ExportButton.Content = T("ייצוא CSV", "Export CSV");
        SearchBox.ToolTip = T("חיפוש לפי שם, מפרסם או נתיב", "Search by name, publisher, or path");
        var selected = ScopeFilter.SelectedIndex;
        ScopeFilter.Items.Clear();
        ScopeFilter.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = T("כל המשתמשים", "All users") });
        ScopeFilter.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = T("המשתמש הנוכחי", "Current user") });
        ScopeFilter.SelectedIndex = selected < 0 ? 0 : selected;
        var headers = _hebrew
            ? new[] { "שם התוכנה", "מפרסם", "מצב", "מקור", "היקף", "גרסה", "נתיב הפעלה" }
            : new[] { "Application", "Publisher", "Status", "Source", "Scope", "Version", "Launch path" };
        for (var i = 0; i < headers.Length; i++) ((System.Windows.Controls.DataGridTextColumn)StartupGrid.Columns[i]).Header = headers[i];
        CreditAvatar.ToolTip = T("הערשלה — דף המשתמש באתר מתמחים טופ", "Hershaleh — Mitmachim Top profile");
    }

    private void Guide_Click(object sender, RoutedEventArgs e) => AppDialog.Guide(this, _dark, _hebrew);

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        ContentScroll.ScrollToTop();
        DashboardNav.Background = Brush("#263B62");
        StartupNav.Background = Brushes.Transparent;
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        ContentScroll.ScrollToVerticalOffset(0);
        StartupGrid.BringIntoView();
        StartupNav.Background = Brush("#263B62");
        DashboardNav.Background = Brushes.Transparent;
    }

    private void CreditLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { ShowError(ex); }
        e.Handled = true;
    }

    private void Report_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(ErrorReporter.CreateIssueUrl(_hebrew)) { UseShellExecute = true }); StatusText.Text = T("נפתח טופס דיווח ב-GitHub", "GitHub report form opened"); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Info(string message) => AppDialog.Info(this, T("הודעה", "Information"), message, _dark);
    private void ShowError(Exception ex)
    {
        ErrorReporter.LogException(ex, "WPF UI");
        AppDialog.Info(this, T("אירעה שגיאה", "An error occurred"), ex.Message, _dark);
    }

    private static SolidColorBrush Brush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));

    private sealed record StartupRow(string Name, string Publisher, string Status, string Source, string Scope, string Version, string Path,
        StartupEntry? EnabledItem, StartupManager.DisabledStartupBackup? BackupItem);
}