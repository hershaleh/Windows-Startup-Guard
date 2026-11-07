using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WindowsStartupGuard;

internal static class AppDialog
{
    private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(71, 120, 245));

    public static bool Confirm(Window owner, string title, string message, bool dark)
    {
        var window = CreateWindow(owner, title, dark, 520, 270);
        var root = CreateRoot(dark);
        root.Children.Add(Paragraph(message, dark, 16, true));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 24, 0, 0) };
        var yes = MakeButton("אישור", true);
        var no = MakeButton("ביטול", false);
        yes.Click += (_, _) => { window.DialogResult = true; window.Close(); };
        no.Click += (_, _) => { window.DialogResult = false; window.Close(); };
        buttons.Children.Add(yes); buttons.Children.Add(no);
        root.Children.Add(buttons);
        window.Content = root;
        return window.ShowDialog() == true;
    }

    public static void Info(Window owner, string title, string message, bool dark)
    {
        var window = CreateWindow(owner, title, dark, 660, 430);
        var root = CreateRoot(dark);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 300 };
        scroll.Content = Paragraph(message, dark, 14, false);
        root.Children.Add(scroll);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 18, 0, 0) };
        var close = MakeButton("סגירה", true);
        close.Click += (_, _) => window.Close();
        buttons.Children.Add(close);
        root.Children.Add(buttons);
        window.Content = root;
        window.ShowDialog();
    }

    public static void Guide(Window owner, bool dark, bool hebrew)
    {
        var title = hebrew ? "מדריך שימוש — Windows Startup Guard" : "User guide — Windows Startup Guard";
        var window = CreateWindow(owner, title, dark, 820, 720);
        window.MinWidth = 680;
        window.MinHeight = 560;
        var root = CreateRoot(dark);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var body = new StackPanel { Margin = new Thickness(2, 0, 10, 0) };
        AddSection(body, hebrew ? "ברוכים הבאים" : "Welcome",
            hebrew ? "Windows Startup Guard עוזרת להבין אילו תוכנות מוגדרות לעלות יחד עם Windows. אפשר לנטרל פריטים שאינם נחוצים, לשמור אותם בגיבוי ולהחזיר אותם בכל עת." :
            "Windows Startup Guard helps you review applications configured to launch with Windows. Disable unneeded entries, keep a backup, and restore them at any time.", dark);
        AddSection(body, hebrew ? "איך מתחילים?" : "Getting started",
            hebrew ? "1. עיין ברשימת התוכנות ובדוק את שם התוכנה, המפרסם, המקור ונתיב ההפעלה.\n2. השתמש בחיפוש ובמסנן כדי למצוא פריט מסוים.\n3. סמן פריט אחד או יותר ולחץ על ״נטרול נבחרים״.\n4. אשר את הפעולה. לפני הסרת פריט נשמר גיבוי מקומי." :
            "1. Review the application, publisher, source, and launch path.\n2. Use search and filters to find an entry.\n3. Select one or more entries and choose Disable selected.\n4. Confirm. A local backup is created before an entry is removed.", dark);
        AddSection(body, hebrew ? "שחזור וביטול שינויים" : "Restore and undo",
            hebrew ? "פריטים מושבתים נשארים ברשימה עם מצב ״מושבת״. בחר אותם ולחץ על ״שחזור נבחרים״, או השתמש ב״שחזור הכול״ כדי להחזיר את כל הגיבויים. אם יעד השחזור כבר קיים, התוכנה לא תדרוס אותו; במקרה הצורך קובץ יקבל שם חלופי." :
            "Disabled entries remain visible with a Disabled status. Select them and choose Restore selected, or use Restore all. Existing registry values are not overwritten; restored files receive a safe alternate name when needed.", dark);
        AddSection(body, hebrew ? "מה עושה ״סגירת תהליך״?" : "What does Stop process do?",
            hebrew ? "הפעולה מבקשת מהתוכנה להיסגר בדרך הרגילה בלבד. היא אינה מכריחה סגירה ואינה משנה את הגדרת האתחול. שמור עבודה פתוחה לפני השימוש." :
            "It asks the matching application to close normally. It never force-kills a process and does not change its startup setting. Save open work first.", dark);
        AddSection(body, hebrew ? "מה נכלל בסריקה?" : "What is scanned?",
            hebrew ? "הסריקה כוללת את מפתחות Run ו־RunOnce ברישום Windows ואת תיקיות האתחול של המשתמש ושל כלל המשתמשים. היא אינה סורקת או משנה שירותים, מנהלי התקנים או משימות מתוזמנות." :
            "The inventory includes Windows Run and RunOnce registry entries and the current-user and shared Startup folders. Services, drivers, and scheduled tasks are not scanned or modified.", dark);
        AddSection(body, hebrew ? "הרשאות, גיבויים ופרטיות" : "Permissions, backups, and privacy",
            hebrew ? "שינוי פריטים המשותפים לכל המשתמשים עשוי לדרוש הפעלה בהרשאות מנהל. אין העלאה אוטומטית של הרשאות. הגיבויים נשמרים במחשב, בתיקיית LocalAppData וברישום המשתמש. ייצוא CSV נשמר רק במיקום שתבחר." :
            "Changing all-user entries may require administrator permissions. The app does not elevate automatically. Backups stay on this PC under LocalAppData and the current user's registry hive. CSV export is saved only where you choose.", dark);
        AddSection(body, hebrew ? "חשוב לדעת" : "Important",
            hebrew ? "מידע המפרסם והגרסה נקרא ממטא־נתונים של קובץ ההפעלה ואינו אימות חתימה דיגיטלית. הערכת הזיכרון היא משוערת. אם אינך בטוח מהו פריט מסוים, אל תנטרל אותו לפני בדיקה." :
            "Publisher and version details come from executable metadata, not digital-signature verification. RAM figures are estimates. If you are unsure what an entry does, research it before disabling it.", dark);
        scroll.Content = body;
        root.Children.Add(scroll);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 18, 0, 0) };
        var close = MakeButton(hebrew ? "הבנתי" : "Close", true);
        close.Click += (_, _) => window.Close();
        buttons.Children.Add(close);
        root.Children.Add(buttons);
        window.Content = root;
        window.ShowDialog();
    }

    private static Window CreateWindow(Window owner, string title, bool dark, double width, double height)
    {
        var window = new Window
        {
            Title = title, Width = width, Height = height, MinWidth = 420, MinHeight = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = owner,
            Background = BrushColor(dark ? "#111C30" : "#F3F6FC"),
            Foreground = BrushColor(dark ? "#F1F5FC" : "#18243B"),
            FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Segoe UI"),
            ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false, Opacity = 0
        };
        window.Loaded += (_, _) =>
        {
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(190))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        };
        return window;
    }

    private static StackPanel CreateRoot(bool dark)
    {
        var root = new StackPanel { Margin = new Thickness(28), Background = BrushColor(dark ? "#111C30" : "#F3F6FC") };
        return root;
    }

    private static TextBlock Paragraph(string value, bool dark, double size, bool strong) => new()
    {
        Text = value, FontSize = size, FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = BrushColor(dark ? "#EAF0FC" : "#34435B"),
        TextWrapping = TextWrapping.Wrap, LineHeight = size * 1.75, Margin = new Thickness(0, 8, 0, 4)
    };

    private static void AddSection(Panel parent, string heading, string body, bool dark)
    {
        var card = new Border
        {
            Background = BrushColor(dark ? "#1B2A42" : "#FFFFFF"),
            BorderBrush = BrushColor(dark ? "#344762" : "#E3EAF4"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14),
            Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 13)
        };
        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = heading, FontSize = 17, FontWeight = FontWeights.Bold,
            Foreground = Accent, Margin = new Thickness(0, 0, 0, 9)
        });
        content.Children.Add(Paragraph(body, dark, 13, false));
        card.Child = content;
        parent.Children.Add(card);
    }

    private static Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Content = text, MinWidth = 104, Height = 40, Padding = new Thickness(20, 8, 20, 8),
            Margin = new Thickness(0, 0, 10, 0), BorderThickness = new Thickness(0),
            Background = primary ? Accent : BrushColor("#34445E"),
            Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.MouseEnter += (_, _) => button.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0.78, TimeSpan.FromMilliseconds(110)));
        button.MouseLeave += (_, _) => button.BeginAnimation(OpacityProperty,
            new DoubleAnimation(1, TimeSpan.FromMilliseconds(140)));
        return button;
    }

    private static Brush BrushColor(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
}
