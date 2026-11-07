using System.Diagnostics;

namespace WindowsStartupGuard;

public sealed class MainForm : Form
{
    private readonly StartupManager _manager = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _filter = new();
    private readonly ToolStripStatusLabel _status = new();
    private readonly Label _count = new();
    private readonly Label _ram = new();
    private readonly Label _backups = new();
    private bool _hebrew = true;
    private List<StartupEntry> _entries = [];
    private readonly Color _background = Color.FromArgb(17, 24, 39);
    private readonly Color _surface = Color.FromArgb(31, 41, 55);
    private readonly Color _blue = Color.FromArgb(37, 99, 235);

    public MainForm()
    {
        Text = "Windows Startup Guard"; MinimumSize = new Size(1000, 650); Size = new Size(1220, 780);
        StartPosition = FormStartPosition.CenterScreen; BackColor = _background; ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f); BuildUi(); ApplyLanguage(); RefreshEntries();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(22), BackColor = _background };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = _background };
        var title = new Label { Text = "Startup Guard", Font = new Font("Segoe UI Semibold", 25, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(0, 0) };
        var subtitle = new Label { Text = "Take control of what starts with Windows", ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Location = new Point(3, 49) };
        var language = MakeButton("עברית / EN", false, (_, _) => { _hebrew = !_hebrew; ApplyLanguage(); });
        language.Size = new Size(112, 36); language.Anchor = AnchorStyles.Top | AnchorStyles.Right; language.Location = new Point(900, 10);
        header.Controls.AddRange([title, subtitle, language]); header.Resize += (_, _) => language.Left = header.ClientSize.Width - language.Width;
        root.Controls.Add(header, 0, 0);

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = _background };
        for (var i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        cards.Controls.Add(MakeCard("STARTUP ITEMS", _count, "Run, RunOnce and Startup folders"), 0, 0);
        cards.Controls.Add(MakeCard("MATCHED RAM", _ram, "Working set of matching processes"), 1, 0);
        cards.Controls.Add(MakeCard("SAFE BACKUPS", _backups, "Items that can be restored"), 2, 0);
        root.Controls.Add(cards, 0, 1);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = _background, Padding = new Padding(0, 8, 0, 0) };
        _search.Width = 210; _search.Height = 34; _search.PlaceholderText = "Search apps or commands"; _search.BackColor = _surface; _search.ForeColor = Color.White; _search.BorderStyle = BorderStyle.FixedSingle; _search.TextChanged += (_, _) => RenderRows();
        _filter.DropDownStyle = ComboBoxStyle.DropDownList; _filter.Width = 125; _filter.BackColor = _surface; _filter.ForeColor = Color.White; _filter.SelectedIndexChanged += (_, _) => RenderRows();
        toolbar.Controls.Add(_search); toolbar.Controls.Add(_filter);
        toolbar.Controls.Add(MakeButton("↻  Refresh", false, (_, _) => RefreshEntries()));
        toolbar.Controls.Add(MakeButton("⏸  Disable selected", true, (_, _) => DisableSelected()));
        toolbar.Controls.Add(MakeButton("■  Stop process", false, (_, _) => StopSelected()));
        toolbar.Controls.Add(MakeButton("↶  Restore backups", false, (_, _) => RestoreBackups()));
        toolbar.Controls.Add(MakeButton("⇩ Export CSV", false, (_, _) => ExportCsv()));
        root.Controls.Add(toolbar, 0, 2);

        _grid.Dock = DockStyle.Fill; _grid.BackgroundColor = _surface; _grid.BorderStyle = BorderStyle.None; _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 65, 81); _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
        _grid.DefaultCellStyle.BackColor = _surface; _grid.DefaultCellStyle.ForeColor = Color.FromArgb(229, 231, 235);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 160); _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(36, 47, 63); _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.ReadOnly = true; _grid.MultiSelect = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Columns.Add("Name", "Application"); _grid.Columns.Add("Publisher", "Publisher"); _grid.Columns.Add("Status", "Status");
        _grid.Columns.Add("Memory", "RAM (MB)"); _grid.Columns.Add("Source", "Source"); _grid.Columns.Add("Scope", "Scope");
        _grid.Columns.Add("Version", "Version"); _grid.Columns.Add("Executable", "Executable path"); _grid.Columns.Add("Command", "Startup command");
        _grid.Columns[0].FillWeight = 95; _grid.Columns[1].FillWeight = 105; _grid.Columns[7].FillWeight = 150; _grid.Columns[8].FillWeight = 170;
        root.Controls.Add(_grid, 0, 3);

        var status = new StatusStrip { BackColor = _background, ForeColor = Color.Silver, Dock = DockStyle.Bottom };
        status.Items.Add(_status); Controls.Add(status);
    }

    private Panel MakeCard(string heading, Label value, string detail)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = _surface, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(14) };
        var h = new Label { Text = heading, ForeColor = Color.FromArgb(156, 163, 175), Font = new Font("Segoe UI Semibold", 8, FontStyle.Bold), Location = new Point(14, 10), AutoSize = true };
        value.Text = "—"; value.ForeColor = Color.White; value.Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold); value.Location = new Point(14, 30); value.AutoSize = true;
        var d = new Label { Text = detail, ForeColor = Color.FromArgb(156, 163, 175), Font = new Font("Segoe UI", 8), Location = new Point(92, 47), AutoSize = true };
        card.Controls.AddRange([h, value, d]); return card;
    }

    private Button MakeButton(string text, bool primary, EventHandler click)
    {
        var button = new Button { Text = text, Size = new Size(primary ? 164 : 136, 36), Margin = new Padding(7, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = primary ? 0 : 1; button.FlatAppearance.BorderColor = Color.FromArgb(75, 85, 99);
        button.BackColor = primary ? _blue : _surface; button.ForeColor = Color.White; button.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold); button.Click += click;
        return button;
    }

    private void ApplyLanguage()
    {
        RightToLeft = _hebrew ? RightToLeft.Yes : RightToLeft.No; RightToLeftLayout = _hebrew;
        _search.PlaceholderText = _hebrew ? "חיפוש תוכנה או פקודה..." : "Search apps or commands";
        var selected = _filter.SelectedIndex; _filter.Items.Clear(); _filter.Items.AddRange(_hebrew ? ["הכול", "המשתמש הנוכחי", "כל המשתמשים"] : ["All items", "Current user", "All users"]);
        _filter.SelectedIndex = selected < 0 ? 0 : selected; _status.Text = _hebrew ? "מוכן" : "Ready";
    }

    private void RefreshEntries()
    {
        try { _entries = _manager.GetEntries(); RenderRows(); UpdateCards(); _status.Text = (_hebrew ? "נמצאו פריטים: " : "Items found: ") + _entries.Count; }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RenderRows()
    {
        _grid.Rows.Clear(); var query = _search.Text.Trim(); var filter = _filter.SelectedIndex;
        foreach (var item in _entries.Where(x => (query.Length == 0 || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Command.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (filter == 0 || (filter == 1 && x.Scope == StartupScope.CurrentUser) || (filter == 2 && x.Scope == StartupScope.AllUsers)))
        {
            var rowIndex = _grid.Rows.Add(item.Name, item.Publisher, _hebrew ? "מופעל" : "Enabled", item.MemoryMb,
                item.Source == StartupSource.Registry ? "Registry" : "Startup folder", item.Scope == StartupScope.AllUsers ? "All users" : "Current user",
                item.FileVersion, item.ExecutablePath ?? "—", item.Command);
            _grid.Rows[rowIndex].Tag = item;
        }
    }

    private void UpdateCards()
    {
        _count.Text = _entries.Count.ToString(); _ram.Text = _entries.Sum(x => x.MemoryMb).ToString() + " MB";
        _backups.Text = _manager.DisabledBackupCount().ToString();
    }

    private List<StartupEntry> Selected() => _grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Tag).OfType<StartupEntry>().ToList();

    private void DisableSelected()
    {
        var selected = Selected();
        if (selected.Count == 0) { MessageBox.Show(_hebrew ? "בחר פריטים בטבלה." : "Select items in the table."); return; }
        if (MessageBox.Show(_hebrew ? "לנטרל את הפריטים שנבחרו? יישמר גיבוי לשחזור." : "Disable selected startup items? A backup will be kept for restore.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var errors = new List<string>();
        foreach (var item in selected) try { _manager.Disable(item); } catch (Exception ex) { errors.Add(item.Name + ": " + ex.Message); }
        RefreshEntries(); if (errors.Count > 0) MessageBox.Show(string.Join(Environment.NewLine, errors), _hebrew ? "חלק מהפריטים לא נוטרלו" : "Some items could not be disabled");
    }

    private void StopSelected()
    {
        var selected = Selected(); if (selected.Count != 1) { MessageBox.Show(_hebrew ? "בחר פריט אחד בלבד." : "Select exactly one item."); return; }
        if (MessageBox.Show(_hebrew ? "לנסות לסגור את התהליך המתאים? שמור עבודה פתוחה תחילה." : "Try to stop the matching process? Save open work first.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { _manager.StopMatchingProcesses(selected[0]); _status.Text = _hebrew ? "נשלחה בקשת סגירה" : "Stop request sent"; } catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreBackups()
    {
        if (MessageBox.Show(_hebrew ? "לשחזר את כל הפריטים שגובו?" : "Restore all backed-up startup items?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var results = _manager.RestoreAll(); RefreshEntries();
        MessageBox.Show(results.Count == 0 ? (_hebrew ? "לא נמצאו גיבויים." : "No backups found.") : string.Join(Environment.NewLine, results.Take(20)), _hebrew ? "תוצאות שחזור" : "Restore results");
    }

    private void ExportCsv()
    {
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "startup-inventory.csv", Title = _hebrew ? "ייצוא רשימת אתחול" : "Export startup inventory" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var visible = _grid.Rows.Cast<DataGridViewRow>().Select(r => r.Tag).OfType<StartupEntry>().ToList();
            var lines = new List<string> { "Name,Publisher,Scope,Source,MemoryMB,FileVersion,ExecutablePath,Command" };
            lines.AddRange(visible.Select(x => string.Join(",", new[] { x.Name, x.Publisher, x.Scope.ToString(), x.Source.ToString(), x.MemoryMb.ToString(), x.FileVersion, x.ExecutablePath ?? "", x.Command }.Select(CsvEscape))));
            File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new System.Text.UTF8Encoding(true));
            _status.Text = _hebrew ? "הייצוא הושלם" : "CSV export completed";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static string CsvEscape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private void ShowError(Exception ex) => MessageBox.Show(ex.Message, _hebrew ? "שגיאה" : "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
