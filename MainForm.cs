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
    private readonly Panel _creditPanel = new();
    private readonly Button _themeButton = new();
    private bool _lightTheme;
    private bool _hebrew = true;
    private List<StartupEntry> _entries = [];
    private Color _background = Color.FromArgb(17, 24, 39);
    private Color _surface = Color.FromArgb(31, 41, 55);
    private Color _blue = Color.FromArgb(37, 99, 235);
    private Color _textColor = Color.White;
    private Color _mutedColor = Color.FromArgb(156, 163, 175);

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
        _creditPanel.Size = new Size(176, 56); _creditPanel.BackColor = _surface; _creditPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _creditPanel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, _creditPanel.ClientRectangle, Color.FromArgb(75, 85, 99), ButtonBorderStyle.Solid);
        _creditImage.Size = new Size(46, 46); _creditImage.SizeMode = PictureBoxSizeMode.Zoom; _creditImage.Location = new Point(6, 5);
        using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("WindowsStartupGuard.Assets.hershaleh.jpg"))
            if (stream != null) { using var sourceImage = new Bitmap(stream); _creditImage.Image = new Bitmap(sourceImage); }
        var creditCaption = new Label { Text = "נוצר על ידי", AutoSize = true, ForeColor = _mutedColor, Font = new Font("Segoe UI", 8), Location = new Point(62, 8) };
        _creditLink.AutoSize = true; _creditLink.LinkColor = Color.FromArgb(96, 165, 250); _creditLink.ActiveLinkColor = Color.FromArgb(59, 130, 246); _creditLink.Text = "הערשלה"; _creditLink.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold); _creditLink.Location = new Point(62, 25);
        _creditLink.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo("https://mitmachim.top/user/%D7%94%D7%A2%D7%A8%D7%A9%D7%9C%D7%94") { UseShellExecute = true });
        _creditPanel.Controls.AddRange([_creditImage, creditCaption, _creditLink]);
        header.Controls.AddRange([title, _subtitle, language, _guideButton, _creditPanel]);
        header.Resize += (_, _) => { language.Left = header.ClientSize.Width - language.Width; _guideButton.Left = language.Left - _guideButton.Width - 8; _creditPanel.Left = _guideButton.Left - _creditPanel.Width - 12; _creditPanel.Top = 0; };
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
        ConfigureButton(_themeButton, "☼ מצב בהיר", false, (_, _) => { _lightTheme = !_lightTheme; ApplyTheme(); });
        _themeButton.Width = 112;
        var restoreSelectedButton = MakeButton("Restore selected", false, (_, _) => RestoreSelected());
        restoreSelectedButton.Name = "RestoreSelectedButton";
        toolbar.Controls.AddRange([_refreshButton, _disableButton, _stopButton, restoreSelectedButton, _restoreButton, _exportButton, _themeButton]);
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
        _themeButton.Text = _lightTheme ? (_hebrew ? "☾ מצב כהה" : "☾ Dark mode") : (_hebrew ? "☼ מצב בהיר" : "☼ Light mode");
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
        ApplyTheme();
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
            ? "מדריך פשוט — מה לעשות בכל מצב?\r\n\r\nהמטרה של התוכנה\r\nלמנוע מתוכנות מיותרות להיפתח לבד בכל הפעלה של Windows. כך אפשר לצמצם עומס בזמן הכניסה למחשב. התוכנה לא מסירה תוכנות מהמחשב — היא רק משנה אם הן יופעלו אוטומטית.\r\n\r\nאיזו בעיה יש לך?\r\n• המחשב עולה לאט או נפתחות הרבה תוכנות? סמן בטבלה תוכנות שאתה מזהה ואינך צריך מיד עם הפעלת המחשב, ואז לחץ על ״נטרול נבחרים״. בפעם הבאה הן לא אמורות להיפתח אוטומטית.\r\n• תוכנה שהפסקת להפעיל אוטומטית חסרה לך? סמן את השורה שמסומנת ״מושבת״ ולחץ על ״שחזור נבחר״. אפשר גם להחזיר את כל הגיבויים באמצעות ״שחזור גיבויים״.\r\n• תוכנה פתוחה לא מגיבה? בחר את הפריט שלה ולחץ על ״סגירת תהליך״. זו בקשה לסגירה רגילה בלבד; שמור עבודה לפני כן. הכפתור אינו מונע מהתוכנה לעלות שוב באתחול הבא.\r\n• אתה רוצה לשמור את הרשימה או לשלוח אותה לתמיכה? לחץ על ״ייצוא CSV״ ושמור את הקובץ.\r\n• הרשימה לא עדכנית? לחץ על ״רענון״ כדי לסרוק שוב.\r\n\r\nמה עושה כל כפתור?\r\n• רענון — בודק מחדש אילו תוכנות מוגדרות להפעלה אוטומטית.\r\n• נטרול נבחרים — מוציא את השורות המסומנות מרשימת ההפעלה האוטומטית ושומר גיבוי כדי שאפשר יהיה להחזיר אותן. התוכנה עצמה נשארת מותקנת.\r\n• סגירת תהליך — מבקש מתוכנה שכבר פועלת להיסגר בצורה רגילה. זה לא מסיר אותה מהאתחול.\r\n• שחזור נבחר — מחזיר להפעלה אוטומטית רק את הגיבויים שסימנת.\r\n• שחזור גיבויים — מחזיר את כל הגיבויים האפשריים. אם היעד כבר תפוס, הוא לא יידרס.\r\n• ייצוא CSV — יוצר קובץ שאפשר לפתוח בתוכנת גיליונות או לצרף לפנייה לתמיכה.\r\n• מצב בהיר/כהה — מחליף את צבעי התצוגה בלבד; אינו משנה את הגדרות Windows.\r\n\r\nאיך משתמשים ברשימה?\r\nחפש לפי שם תוכנה, מפרסם או פקודה. המסנן מאפשר להציג פריטים של המשתמש הנוכחי או של כלל המשתמשים. השורה ״מופעל״ מציינת שהתוכנה מוגדרת לעלות אוטומטית; ״מושבת״ מציינת שיש לה גיבוי והיא אינה אמורה לעלות ממקור זה.\r\n\r\nאיך להימנע מתקלה?\r\nנטרל רק תוכנות שאתה מזהה ויודע שאינך צריך בהפעלה. אל תיגע ברכיבי אבטחה, גיבוי, מנהלי התקנים או רכיבי Windows שאינך מכיר. אם אינך בטוח — אל תשנה. פריטים של כלל המשתמשים עשויים לדרוש הפעלה כמנהל.\r\n\r\nמה התוכנה עדיין לא בודקת?\r\nהגרסה הנוכחית בודקת את רשומות Run/RunOnce ואת תיקיות האתחול בלבד. היא אינה מציגה שירותי Windows או משימות מתוזמנות. נתון הזיכרון הוא אומדן של תהליכים בעלי שם תואם, ולא מדידה מדויקת של השימוש של התוכנה."
            : "QUICK GUIDE — WHAT SHOULD I DO?\r\n\r\nWhat this app does\r\nIt helps stop unnecessary apps from opening automatically whenever Windows starts. This can reduce the number of apps competing during sign-in. It does not uninstall apps; it only changes whether they launch automatically.\r\n\r\nChoose the problem you want to solve\r\n• Too many apps open when you turn on the PC? Select apps you recognize and do not need immediately, then click “Disable selected”. They should no longer launch automatically.\r\n• An app you disabled is missing from startup? Select its “Disabled” row and click “Restore selected”. Or use “Restore backups” to bring back all restorable items.\r\n• An already-open app is not responding? Select its item and click “Stop process”. This only requests a normal close. Save your work first. It does not prevent the app from launching next time.\r\n• Need to save or share the inventory? Click “Export CSV” and save the file.\r\n• The list may be out of date? Click “Refresh” to scan again.\r\n\r\nWhat each button does\r\n• Refresh — checks again which apps are configured to start automatically.\r\n• Disable selected — removes selected entries from automatic startup and keeps a backup so you can undo the change. The app remains installed.\r\n• Stop process — asks an already-running app to close normally. It does not remove the app from startup.\r\n• Restore selected — returns only the selected backups to automatic startup.\r\n• Restore backups — attempts to return all backups. Existing targets are not overwritten.\r\n• Export CSV — creates a file you can open in a spreadsheet or attach to a support request.\r\n• Light/Dark mode — changes appearance only; it does not change Windows settings.\r\n\r\nUsing the list\r\nSearch by app name, publisher, or command. The filter shows entries for the current user or all users. “Enabled” means an app is configured to start automatically. “Disabled” means a backup exists and it should no longer launch from that location.\r\n\r\nAvoiding problems\r\nDisable only apps you recognize and know you do not need at startup. Do not change security, backup, driver, or Windows components you do not understand. If unsure, leave the item alone. All-users entries may require administrator rights.\r\n\r\nCurrent limitations\r\nThis version checks Run/RunOnce registry entries and Startup folders only. It does not list Windows services or scheduled tasks. RAM is an estimate based on matching process names, not an exact measurement for the app itself.";
        var close = MakeButton(_hebrew ? "סגירה" : "Close", true, (_, _) => guide.Close());
        close.Dock = DockStyle.Bottom; close.Height = 42;
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = _background };
        panel.Controls.Add(content); panel.Controls.Add(close); guide.Controls.Add(panel); guide.AcceptButton = close;
        guide.ShowDialog(this);
    }

    private void ApplyTheme()
    {
        _background = _lightTheme ? Color.FromArgb(243, 246, 250) : Color.FromArgb(17, 24, 39);
        _surface = _lightTheme ? Color.White : Color.FromArgb(31, 41, 55);
        _blue = _lightTheme ? Color.FromArgb(37, 99, 235) : Color.FromArgb(37, 99, 235);
        _textColor = _lightTheme ? Color.FromArgb(31, 41, 55) : Color.White;
        _mutedColor = _lightTheme ? Color.FromArgb(100, 116, 139) : Color.FromArgb(156, 163, 175);
        BackColor = _background; ForeColor = _textColor;
        foreach (Control control in GetAllControls(this))
        {
            if (control == _grid || control == _creditImage || control == _creditLink) continue;
            if (control == _disableButton || control == _guideButton) { control.BackColor = _blue; control.ForeColor = Color.White; }
            else if (control is Button) { control.BackColor = _surface; control.ForeColor = _textColor; }
            else if (control is TextBoxBase || control is ComboBox) { control.BackColor = _surface; control.ForeColor = _textColor; }
            else if (control is Label) control.ForeColor = control == _subtitle || _cardDetails.Contains(control) || _cardHeadings.Contains(control) ? _mutedColor : _textColor;
            else if (control is Panel panel && panel == _creditPanel) panel.BackColor = _surface;
            else if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel) control.BackColor = _background;
        }
        _grid.BackgroundColor = _surface;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = _lightTheme ? Color.FromArgb(226, 232, 240) : Color.FromArgb(55, 65, 81);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = _textColor;
        _grid.DefaultCellStyle.BackColor = _surface; _grid.DefaultCellStyle.ForeColor = _textColor;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 160); _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = _lightTheme ? Color.FromArgb(241, 245, 249) : Color.FromArgb(36, 47, 63);
        _status.Owner?.Invalidate();
        _themeButton.Text = _lightTheme ? (_hebrew ? "☾ מצב כהה" : "☾ Dark mode") : (_hebrew ? "☼ מצב בהיר" : "☼ Light mode");
        Invalidate(true);
    }

    private static IEnumerable<Control> GetAllControls(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in GetAllControls(child)) yield return nested;
        }
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
