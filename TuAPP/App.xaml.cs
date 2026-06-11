namespace TuAPP;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Verificamos si es la primera vez
        bool isFirstTime = Preferences.Get("is_first_time", true);
        MainPage = isFirstTime ? new OnboardingPage() : new AppShell();
    }
}