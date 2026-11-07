using System.Drawing.Drawing2D;

namespace WindowsStartupGuard;

internal sealed class SplashScreen : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1150 };

    public SplashScreen()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 300);
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(15, 45, 90);
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        _timer.Tick += (_, _) => { _timer.Stop(); Close(); };
        Shown += (_, _) => _timer.Start();
        FormClosed += (_, _) => _timer.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(9, 65, 145), Color.FromArgb(28, 150, 245), 35f);
        e.Graphics.FillRectangle(brush, ClientRectangle);
        using var glow = new SolidBrush(Color.FromArgb(35, 255, 255, 255));
        e.Graphics.FillEllipse(glow, 355, -90, 260, 260);
        using var border = new Pen(Color.FromArgb(95, 190, 235), 2);
        e.Graphics.DrawRoundedRectangle(border, new Rectangle(1, 1, Width - 3, Height - 3), 22);
        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (icon != null) e.Graphics.DrawIcon(icon, new Rectangle(205, 35, 110, 110));
        using var titleFont = new Font("Segoe UI Semibold", 22, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 10.5f);
        using var white = new SolidBrush(Color.White);
        using var muted = new SolidBrush(Color.FromArgb(220, 239, 255));
        var title = "Windows Startup Guard";
        var titleSize = e.Graphics.MeasureString(title, titleFont);
        e.Graphics.DrawString(title, titleFont, white, (Width - titleSize.Width) / 2, 165);
        var subtitle = "שליטה בטוחה בתוכנות האתחול";
        var subtitleSize = e.Graphics.MeasureString(subtitle, subtitleFont);
        e.Graphics.DrawString(subtitle, subtitleFont, muted, (Width - subtitleSize.Width) / 2, 207);
        using var track = new SolidBrush(Color.FromArgb(70, 255, 255, 255));
        using var fill = new SolidBrush(Color.White);
        e.Graphics.FillRoundedRectangle(track, new Rectangle(190, 255, 140, 4), 2);
        e.Graphics.FillRoundedRectangle(fill, new Rectangle(190, 255, 75, 4), 2);
    }
}

internal static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = Rounded(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = Rounded(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
