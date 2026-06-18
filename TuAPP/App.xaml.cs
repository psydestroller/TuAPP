namespace TuAPP;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // Forma moderna y sin advertencias para arrancar MAUI
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}