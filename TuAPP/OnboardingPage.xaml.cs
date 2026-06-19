using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        double.TryParse(EntryWeight.Text, out double weight);
        int.TryParse(EntryAge.Text, out int age);

        var profile = new AthleteProfile
        {
            Name = string.IsNullOrWhiteSpace(EntryName.Text) ? "Atleta" : EntryName.Text,
            WeightKg = weight > 0 ? weight : 66.0,
            Age = age > 0 ? age : 18
        };

        StorageService.SaveProfile(profile);

        // CORRECCIÓN 1: La llave coincide exactamente con App.xaml.cs
        Preferences.Set("IsFirstLaunch", false);

        // CORRECCIÓN 2: Navegación moderna y segura para .NET MAUI
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}