namespace WindowsStartupGuard;
public partial class SplashWindow : System.Windows.Window
{
    public SplashWindow() => InitializeComponent();
    private async void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        await Task.Delay(2100);
        Close();
    }
}