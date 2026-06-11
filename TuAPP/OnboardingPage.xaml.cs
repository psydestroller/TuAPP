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
        Preferences.Set("is_first_time", false);

        // Cambia la pantalla a la aplicación principal (AppShell)
        Application.Current.MainPage = new AppShell();
    }
}