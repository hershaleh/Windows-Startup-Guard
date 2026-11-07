namespace WindowsStartupGuard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        var splash = new SplashWindow();
        splash.ShowDialog();
        app.Run();
    }
}