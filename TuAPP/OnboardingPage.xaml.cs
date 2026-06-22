using System.Globalization;
using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    private async void OnComenzarClicked(object sender, EventArgs e)
    {
        double.TryParse(EntryWeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double w);
        double.TryParse(EntryHeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double h);
        int.TryParse(EntryAge.Text, out int age);

        var profile = new AthleteProfile
        {
            Name = string.IsNullOrWhiteSpace(EntryName.Text) ? "Atleta" : EntryName.Text,
            WeightKg = w > 0 ? w : 66.0,
            HeightCm = h > 0 ? h : 170.0,
            Age = age > 0 ? age : 18
        };
        StorageService.SaveProfile(profile);

        var fight = new FightEvent { Date = DateTime.Today.AddDays(30), TargetWeightKg = w > 0 ? w : 66.0 };
        StorageService.SaveFightEvent(fight);

        if (TpTrainingTime != null)
        {
            TimeSpan safeTime = TpTrainingTime.Time is TimeSpan ts ? ts : new TimeSpan(17, 0, 0);
            await NotificationService.RequestAndScheduleAsync(safeTime);
        }

        Preferences.Set("IsFirstLaunch", false);
        Application.Current!.MainPage = new AppShell();
    }
}