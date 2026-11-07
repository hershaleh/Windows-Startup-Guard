using System.Diagnostics;
using System.Security.Principal;

namespace WindowsStartupGuard;

public sealed class MainForm : Form
{
    private readonly StartupManager _manager = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _filter = new();
    private readonly ToolStripStatusLabel _status = new();
    private readonly ToolStripStatusLabel _privilegeStatus = new();
    private readonly Label _count = new();
    private readonly Label _ram = new();
    private readonly Label _backups = new();
    private readonly Label _subtitle = new();
    private readonly Label[] _cardHeadings = [new(), new(), new()];
    private readonly Label[] _cardDetails = [new(), new(), new()];
    private readonly Button _refreshButton = new();
    private readonly Button _disableButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _restoreButton = new();
    private readonly Button _exportButton = new();
    private readonly Button _guideButton = new();
    private readonly PictureBox _creditImage = new();
    private readonly LinkLabel _creditLink = new();
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
        _subtitle.Text = "Take control of what starts with Windows"; _subtitle.ForeColor = Color.FromArgb(156, 163, 175); _subtitle.AutoSize = true; _subtitle.Location = new Point(3, 49);
        var language = MakeButton("עברית / EN", false, (_, _) => { _hebrew = !_hebrew; ApplyLanguage(); });
        language.Size = new Size(112, 36); language.Anchor = AnchorStyles.Top | AnchorStyles.Right; language.Location = new Point(900, 10);
        ConfigureButton(_guideButton, "מדריך", true, (_, _) => ShowGuide());
        _guideButton.Size = new Size(92, 36); _guideButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _creditImage.Size = new Size(44, 44); _creditImage.SizeMode = PictureBoxSizeMode.Zoom; _creditImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("WindowsStartupGuard.Assets.hershaleh.jpg"))
            if (stream != null) _creditImage.Image = Image.FromStream(stream);
        _creditLink.AutoSize = true; _creditLink.LinkColor = Color.FromArgb(147, 197, 253); _creditLink.Text = "הערשלה";
        _creditLink.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo("https://mitmachim.top/user/%D7%94%D7%A2%D7%A8%D7%A9%D7%9C%D7%94") { UseShellExecute = true });
        header.Controls.AddRange([title, _subtitle, language, _guideButton, _creditImage, _creditLink]);
        header.Resize += (_, _) => { language.Left = header.ClientSize.Width - language.Width; _guideButton.Left = language.Left - _guideButton.Width - 8; _creditImage.Left = _guideButton.Left - _creditImage.Width - 18; _creditImage.Top = 7; _creditLink.Left = _creditImage.Left - _creditLink.Width - 7; _creditLink.Top = 22; };
        header.PerformLayout();
        root.Controls.Add(header, 0, 0);

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = _background };
        for (var i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        cards.Controls.Add(MakeCard("STARTUP ITEMS", _count, "Run, RunOnce and Startup folders", 0), 0, 0);
        cards.Controls.Add(MakeCard("MATCHED RAM", _ram, "Working set of matching processes", 1), 1, 0);
        cards.Controls.Add(MakeCard("SAFE BACKUPS", _backups, "Items that can be restored", 2), 2, 0);
        root.Controls.Add(cards, 0, 1);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = _background, Padding = new Padding(0, 8, 0, 0) };
        _search.Width = 210; _search.Height = 34; _search.PlaceholderText = "Search apps or commands"; _search.BackColor = _surface; _search.ForeColor = Color.White; _search.BorderStyle = BorderStyle.FixedSingle; _search.TextChanged += (_, _) => RenderRows();
        _filter.DropDownStyle = ComboBoxStyle.DropDownList; _filter.Width = 125; _filter.BackColor = _surface; _filter.ForeColor = Color.White; _filter.SelectedIndexChanged += (_, _) => RenderRows();
        toolbar.Controls.Add(_search); toolbar.Controls.Add(_filter);
        ConfigureButton(_refreshButton, "↻  Refresh", false, (_, _) => RefreshEntries());
        ConfigureButton(_disableButton, "⏸  Disable selected", true, (_, _) => DisableSelected());
        ConfigureButton(_stopButton, "■  Stop process", false, (_, _) => StopSelected());
        ConfigureButton(_restoreButton, "↶  Restore backups", false, (_, _) => RestoreBackups());
        ConfigureButton(_exportButton, "⇩ Export CSV", false, (_, _) => ExportCsv());
        var restoreSelectedButton = MakeButton("Restore selected", false, (_, _) => RestoreSelected());
        restoreSelectedButton.Name = "RestoreSelectedButton";
        toolbar.Controls.AddRange([_refreshButton, _disableButton, _stopButton, restoreSelectedButton, _restoreButton, _exportButton]);
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
        _grid.Columns.Add("Restore", "Restore");
        _grid.Columns[0].FillWeight = 95; _grid.Columns[1].FillWeight = 105; _grid.Columns[7].FillWeight = 150; _grid.Columns[8].FillWeight = 170; _grid.Columns[9].FillWeight = 65;
        _grid.Columns[9].Visible = false;
        root.Controls.Add(_grid, 0, 3);

        var status = new StatusStrip { BackColor = _background, ForeColor = Color.Silver, Dock = DockStyle.Bottom };
        status.Items.Add(_status); status.Items.Add(new ToolStripStatusLabel { Spring = true });
        status.Items.Add(_privilegeStatus); Controls.Add(status);
    }

    private Panel MakeCard(string heading, Label value, string detail, int index)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = _surface, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(14) };
        var h = new Label { Text = heading, ForeColor = Color.FromArgb(156, 163, 175), Font = new Font("Segoe UI Semibold", 8, FontStyle.Bold), Location = new Point(14, 10), AutoSize = true };
        _cardHeadings[index] = h;
        value.Text = "—"; value.ForeColor = Color.White; value.Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold); value.Location = new Point(14, 30); value.AutoSize = true;
        var d = new Label { Text = detail, ForeColor = Color.FromArgb(156, 163, 175), Font = new Font("Segoe UI", 8), Location = new Point(92, 47), AutoSize = true };
        _cardDetails[index] = d;
        card.Controls.AddRange([h, value, d]); return card;
    }

    private Button MakeButton(string text, bool primary, EventHandler click)
    {
        var button = new Button();
        ConfigureButton(button, text, primary, click);
        return button;
    }

    private void ConfigureButton(Button button, string text, bool primary, EventHandler click)
    {
        button.Text = text; button.Size = new Size(primary ? 164 : 136, 36); button.Margin = new Padding(7, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat; button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderSize = primary ? 0 : 1; button.FlatAppearance.BorderColor = Color.FromArgb(75, 85, 99);
        button.BackColor = primary ? _blue : _surface; button.ForeColor = Color.White; button.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
        button.Click += click;
    }

    private void ApplyLanguage()
    {
        RightToLeft = _hebrew ? RightToLeft.Yes : RightToLeft.No; RightToLeftLayout = _hebrew;
        Text = _hebrew ? "מגן אתחול Windows" : "Windows Startup Guard";
        _subtitle.Text = _hebrew ? "שליטה בתוכנות המופעלות עם Windows" : "Take control of what starts with Windows";
        _search.PlaceholderText = _hebrew ? "חיפוש תוכנה או פקודה..." : "Search apps or commands";
        var selected = _filter.SelectedIndex; _filter.Items.Clear(); _filter.Items.AddRange(_hebrew ? ["הכול", "המשתמש הנוכחי", "כל המשתמשים"] : ["All items", "Current user", "All users"]);
        _filter.SelectedIndex = selected < 0 ? 0 : selected;
        _cardHeadings[0].Text = _hebrew ? "פריטי אתחול" : "STARTUP ITEMS";
        _cardHeadings[1].Text = _hebrew ? "זיכרון משוער" : "MATCHED RAM";
        _cardHeadings[2].Text = _hebrew ? "גיבויים בטוחים" : "SAFE BACKUPS";
        _cardDetails[0].Text = _hebrew ? "רישום ותיקיות אתחול" : "Run, RunOnce and Startup folders";
        _cardDetails[1].Text = _hebrew ? "זיכרון תהליכים תואמים" : "Working set of matching processes";
        _cardDetails[2].Text = _hebrew ? "פריטים שניתן לשחזר" : "Items that can be restored";
        _grid.Columns[9].HeaderText = _hebrew ? "שחזור" : "Restore";
        _refreshButton.Text = _hebrew ? "↻  רענון" : "↻  Refresh";
        _disableButton.Text = _hebrew ? "⏸  נטרול נבחרים" : "⏸  Disable selected";
        _stopButton.Text = _hebrew ? "■  סגירת תהליך" : "■  Stop process";
        _restoreButton.Text = _hebrew ? "↶  שחזור גיבויים" : "↶  Restore backups";
        _exportButton.Text = _hebrew ? "⇩ ייצוא CSV" : "⇩ Export CSV";
        _guideButton.Text = _hebrew ? "מדריך" : "Guide";
        _creditLink.Text = _hebrew ? "הערשלה" : "Hershaleh";
        var restoreSelectedButton = Controls.Find("RestoreSelectedButton", true).OfType<Button>().FirstOrDefault();
        if (restoreSelectedButton != null) restoreSelectedButton.Text = _hebrew ? "שחזור נבחר" : "Restore selected";
        string[] headers = _hebrew
            ? ["תוכנה", "מפרסם", "מצב", "זיכרון (MB)", "מקור", "היקף", "גרסה", "נתיב קובץ", "פקודת אתחול"]
            : ["Application", "Publisher", "Status", "RAM (MB)", "Source", "Scope", "Version", "Executable path", "Startup command"];
        for (var i = 0; i < headers.Length; i++) _grid.Columns[i].HeaderText = headers[i];
        _status.Text = _hebrew ? "מוכן" : "Ready";
        var isAdministrator = false;
        try { isAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator); } catch { }
        _privilegeStatus.Text = isAdministrator
            ? (_hebrew ? "הרשאות מנהל" : "Administrator")
            : (_hebrew ? "משתמש רגיל — שינוי פריטי כל המשתמשים עשוי לדרוש מנהל" : "Standard user — changing all-user items may require admin");
    }

    private void ShowGuide()
    {
        var guide = new Form { Text = _hebrew ? "מדריך — מגן אתחול Windows" : "Guide — Windows Startup Guard",
            StartPosition = FormStartPosition.CenterParent, Size = new Size(780, 680), MinimumSize = new Size(620, 500),
            BackColor = _background, ForeColor = Color.White, Font = new Font("Segoe UI", 10),
            RightToLeft = _hebrew ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = _hebrew, ShowInTaskbar = false };
        var content = new TextBox { Multiline = true, ReadOnly = true, WordWrap = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = _surface, ForeColor = Color.FromArgb(229, 231, 235),
            Font = new Font("Segoe UI", 10.5f), Padding = new Padding(16), RightToLeft = _hebrew ? RightToLeft.Yes : RightToLeft.No };
        content.Text = _hebrew
            ? "מדריך שימוש\r\n\r\nמה מופיע בטבלה?\r\nכל שורה היא פריט שעשוי לפעול בעת הכניסה ל-Windows. הסריקה כוללת את מפתחות Run ו-RunOnce ברישום ואת תיקיות האתחול של המשתמש ושל כלל המשתמשים.\r\n\r\nהעמודות\r\n• תוכנה — שם הפריט.\r\n• מפרסם וגרסה — מידע מפרטי הקובץ; אין בכך אימות חתימה דיגיטלית או הוכחה שהמפרסם אמין.\r\n• מצב — מופעל: הפריט נמצא במקור האתחול. מושבת: הפריט הוסר ממקור האתחול ונשמר גיבוי שניתן לנסות לשחזר.\r\n• זיכרון — אומדן זיכרון של תהליכים בעלי שם תואם, לא מדידה מדויקת של הפריט עצמו.\r\n• מקור — רישום מערכת או תיקיית אתחול.\r\n• היקף — המשתמש הנוכחי או כלל המשתמשים.\r\n• נתיב ופקודה — המקום והפקודה שמהם הפריט מופעל. היזהר מפקודות שאינך מזהה.\r\n\r\nהכפתורים\r\n• רענון — סריקה מחדש.\r\n• נטרול נבחרים — מסיר את הפריטים המסומנים ממקור האתחול ושומר גיבוי. הפעולה דורשת אישור ואינה מסירה את התוכנה מהמחשב.\r\n• סגירת תהליך — מבקש סגירה רגילה של התהליך המתאים לפריט יחיד. שמור עבודה פתוחה תחילה; אין סגירה כפויה.\r\n• שחזור נבחר — משחזר רק גיבויים שסומנו בטבלה.\r\n• שחזור גיבויים — מנסה לשחזר את כל הגיבויים. יעדים קיימים אינם נדרסים.\r\n• ייצוא CSV — שומר את השורות המוצגות בקובץ לגיליון אלקטרוני.\r\n\r\nחיפוש וסינון\r\nהקלד שם, מפרסם או פקודה בתיבת החיפוש. הרשימה הנפתחת מסננת לפי היקף המשתמשים.\r\n\r\nהרשאות ובטיחות\r\nשינוי פריטי אתחול של כלל המשתמשים עשוי לדרוש הרשאות מנהל. אל תנטרל פריט שאינך מבין: רכיבי אבטחה, גיבוי, מנהלי התקנים ורכיבי מערכת עלולים להיפגע. מומלץ לייצא רשימה לפני שינויים.\r\n\r\nהיקף הסריקה\r\nהגרסה הנוכחית סורקת Run/RunOnce ותיקיות אתחול בלבד. היא אינה סורקת שירותי Windows או משימות מתוזמנות."
            : "USER GUIDE\r\n\r\nWhat is listed?\r\nEach row is an item that may launch when you sign in to Windows. The scan covers Run and RunOnce registry keys and the current-user and all-users Startup folders.\r\n\r\nColumns\r\n• Application — item name.\r\n• Publisher and version — file metadata only; this does not verify a digital signature or prove trustworthiness.\r\n• Status — Enabled means present in startup. Disabled means removed from startup and backed up for possible restoration.\r\n• RAM — approximate memory used by processes with a matching name, not an exact measurement for the startup item.\r\n• Source — registry or Startup folder.\r\n• Scope — current user or all users.\r\n• Path and command — where and how the item launches. Be careful with commands you do not recognize.\r\n\r\nToolbar buttons\r\n• Refresh — scan again.\r\n• Disable selected — removes selected items from startup and keeps a backup. Confirmation is required; this does not uninstall the app.\r\n• Stop process — requests a normal close for the process matching one selected item. Save work first; no forced termination is used.\r\n• Restore selected — restores only selected backups.\r\n• Restore backups — attempts to restore all backups. Existing targets are never overwritten.\r\n• Export CSV — saves the visible rows to a spreadsheet file.\r\n\r\nSearch and filters\r\nSearch by name, publisher, or command. The dropdown filters by user scope.\r\n\r\nPermissions and safety\r\nChanging all-users entries may require administrator rights. Do not disable items you do not understand: security, backup, driver, and system components may be affected. Export the inventory before making changes.\r\n\r\nScan coverage\r\nThis version scans Run/RunOnce and Startup folders only. It does not scan Windows services or scheduled tasks.";
        var close = MakeButton(_hebrew ? "סגירה" : "Close", true, (_, _) => guide.Close());
        close.Dock = DockStyle.Bottom; close.Height = 42;
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = _background };
        panel.Controls.Add(content); panel.Controls.Add(close); guide.Controls.Add(panel); guide.AcceptButton = close;
        guide.ShowDialog(this);
    }

    private void RefreshEntries()
    {
        try { _entries = _manager.GetEntries(); RenderRows(); UpdateCards(); _status.Text = (_hebrew ? "נמצאו פריטים: " : "Items found: ") + _entries.Count; }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RenderRows()
    {
        _grid.Rows.Clear();
        var query = _search.Text.Trim();
        var filter = _filter.SelectedIndex;
        var disabled = _manager.GetDisabledBackups();

        foreach (var item in _entries.Where(x =>
            (query.Length == 0 || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             x.Command.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (filter == 0 || (filter == 1 && x.Scope == StartupScope.CurrentUser) ||
             (filter == 2 && x.Scope == StartupScope.AllUsers))))
        {
            var rowIndex = _grid.Rows.Add(item.Name, item.Publisher, _hebrew ? "מופעל" : "Enabled", item.MemoryMb,
                item.Source == StartupSource.Registry ? (_hebrew ? "רישום מערכת" : "Registry") : (_hebrew ? "תיקיית אתחול" : "Startup folder"),
                item.Scope == StartupScope.AllUsers ? (_hebrew ? "כל המשתמשים" : "All users") : (_hebrew ? "המשתמש הנוכחי" : "Current user"),
                item.FileVersion, item.ExecutablePath ?? "—", item.Command);
            _grid.Rows[rowIndex].Tag = item;
        }

        foreach (var item in disabled.Where(x =>
            (query.Length == 0 || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             x.Command.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (filter == 0 || (filter == 1 && x.Scope == StartupScope.CurrentUser) ||
             (filter == 2 && x.Scope == StartupScope.AllUsers))))
        {
            var index = _grid.Rows.Add(item.Name, item.Publisher, _hebrew ? "מושבת" : "Disabled", item.MemoryMb,
                item.Source == StartupSource.Registry ? (_hebrew ? "רישום מערכת" : "Registry") : (_hebrew ? "תיקיית אתחול" : "Startup folder"),
                item.Scope == StartupScope.AllUsers ? (_hebrew ? "כל המשתמשים" : "All users") : (_hebrew ? "המשתמש הנוכחי" : "Current user"),
                item.FileVersion, item.ExecutablePath ?? "—", item.Command, item.BackupId);
            _grid.Rows[index].Tag = item;
            _grid.Rows[index].DefaultCellStyle.ForeColor = Color.FromArgb(156, 163, 175);
        }
    }

    private void UpdateCards()
    {
        _count.Text = _entries.Count.ToString();
        var ramTotal = _entries
            .GroupBy(x => x.ExecutablePath ?? x.Command, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First().MemoryMb)
            .Sum();
        _ram.Text = ramTotal.ToString() + " MB";
        _backups.Text = _manager.DisabledBackupCount().ToString();
    }

    private List<StartupEntry> Selected() => _grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Tag).OfType<StartupEntry>().ToList();

    private void DisableSelected()
    {
        var selected = Selected().Where(x => x.IsEnabled).ToList();
        if (selected.Count == 0) { MessageBox.Show(_hebrew ? "בחר פריטים בטבלה." : "Select items in the table."); return; }
        if (MessageBox.Show(_hebrew ? "לנטרל את הפריטים שנבחרו? יישמר גיבוי לשחזור." : "Disable selected startup items? A backup will be kept for restore.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var errors = new List<string>();
        foreach (var item in selected) try { _manager.Disable(item); } catch (Exception ex) { errors.Add(item.Name + ": " + ex.Message); }
        RefreshEntries(); if (errors.Count > 0) MessageBox.Show(string.Join(Environment.NewLine, errors), _hebrew ? "חלק מהפריטים לא נוטרלו" : "Some items could not be disabled");
    }

    private void StopSelected()
    {
        var selected = Selected().Where(x => x.IsEnabled).ToList(); if (selected.Count != 1) { MessageBox.Show(_hebrew ? "בחר פריט אחד בלבד." : "Select exactly one item."); return; }
        if (MessageBox.Show(_hebrew ? "לנסות לסגור את התהליך המתאים? שמור עבודה פתוחה תחילה." : "Try to stop the matching process? Save open work first.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { _manager.StopMatchingProcesses(selected[0]); _status.Text = _hebrew ? "נשלחה בקשת סגירה" : "Stop request sent"; } catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreSelected()
    {
        var selected = _grid.SelectedRows.Cast<DataGridViewRow>().Select(row => row.Tag).ToList();
        var backupIds = selected.Select(item => item?.GetType().GetProperty("BackupId")?.GetValue(item)?.ToString()).Where(id => !string.IsNullOrWhiteSpace(id)).Cast<string>().Distinct().ToList();
        if (backupIds.Count == 0)
        {
            MessageBox.Show(_hebrew ? "בחר פריט מושבת אחד או יותר." : "Select one or more disabled items.");
            return;
        }
        if (MessageBox.Show(_hebrew ? $"לשחזר {backupIds.Count} פריטים שנבחרו?" : $"Restore {backupIds.Count} selected items?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            var results = _manager.RestoreSelected(backupIds);
            RefreshEntries();
            MessageBox.Show(string.Join(Environment.NewLine, results.Take(30)), _hebrew ? "תוצאות שחזור" : "Restore results");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreBackups()
    {
        if (MessageBox.Show(_hebrew ? "לשחזר את כל הפריטים שגובו?" : "Restore all backed-up startup items?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            var results = _manager.RestoreAll();
            RefreshEntries();
            var restored = results.Count(x => x.StartsWith("Restored ", StringComparison.Ordinal));
            var skipped = results.Count(x => x.StartsWith("Restore skipped", StringComparison.Ordinal) || x.StartsWith("File restore skipped", StringComparison.Ordinal) || x.StartsWith("Registry restore skipped", StringComparison.Ordinal));
            var errors = results.Count(x => x.Contains(" restore error:", StringComparison.OrdinalIgnoreCase) || x.StartsWith("Could not ", StringComparison.Ordinal));
            _status.Text = _hebrew
                ? $"שחזור הסתיים — הצליחו: {restored}, דולגו: {skipped}, שגיאות: {errors}"
                : $"Restore finished — restored: {restored}, skipped: {skipped}, errors: {errors}";
            var summary = _hebrew
                ? $"סיכום: {restored} שוחזרו, {skipped} דולגו, {errors} שגיאות."
                : $"Summary: {restored} restored, {skipped} skipped, {errors} errors.";
            MessageBox.Show(results.Count == 0 ? (_hebrew ? "לא נמצאו גיבויים." : "No backups found.") : summary + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, results.Take(30)),
                _hebrew ? "תוצאות שחזור" : "Restore results");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ExportCsv()
    {
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "startup-inventory.csv", Title = _hebrew ? "ייצוא רשימת אתחול" : "Export startup inventory" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var headers = new[] { "Name", "Publisher", "Status", "MemoryMB", "Scope", "Source", "FileVersion", "ExecutablePath", "Command" };
            var lines = new List<string> { string.Join(",", headers.Select(CsvEscape)) };
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var values = new string[9];
                for (var i = 0; i < values.Length; i++)
                    values[i] = Convert.ToString(row.Cells[i].Value) ?? "";
                lines.Add(string.Join(",", values.Select(CsvEscape)));
            }
            File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new System.Text.UTF8Encoding(true));
            _status.Text = _hebrew ? "הייצוא הושלם" : "CSV export completed";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static string CsvEscape(string value)
    {
        value ??= "";
        var firstMeaningful = value.TrimStart().FirstOrDefault();
        if (firstMeaningful is '=' or '+' or '-' or '@')
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private void ShowError(Exception ex) => MessageBox.Show(ex.Message, _hebrew ? "שגיאה" : "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
