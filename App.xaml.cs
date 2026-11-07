namespace WindowsStartupGuard;
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorReporter.LogException(args.Exception, "WPF dispatcher");
            System.Windows.MessageBox.Show(args.Exception.Message, "Windows Startup Guard",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            args.Handled = true;
        };
        base.OnStartup(e);
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        var splash = new SplashWindow();
        splash.ShowDialog();
        var main = new MainWindow();
        MainWindow = main;
        main.Show();
        ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
    }
}