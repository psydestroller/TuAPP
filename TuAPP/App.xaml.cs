namespace TuAPP;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Verificamos si es la primera vez que se abre la app
        bool isFirstLaunch = Preferences.Get("IsFirstLaunch", true);

        if (isFirstLaunch)
        {
            // Si es la primera vez, mostramos tu pantalla de bienvenida
            return new Window(new OnboardingPage());
        }
        else
        {
            // Si ya está configurada, vamos directo a tu menú inferior de campamento
            return new Window(new AppShell());
        }
    }
}