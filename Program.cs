namespace WindowsStartupGuard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => ErrorReporter.LogException(e.Exception, "UI thread");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                ErrorReporter.LogException(exception, "Unhandled application exception");
        };
        using (var splash = new SplashScreen())
            splash.ShowDialog();
        Application.Run(new MainForm());
    }
}
