using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class WorkoutSummaryPage : ContentPage
{
    private WorkoutSession _session;
    private AthleteProfile _profile;

    public WorkoutSummaryPage(WorkoutSession session)
    {
        InitializeComponent();
        _session = session;
        _profile = StorageService.LoadProfile();

        LblType.Text = $"{_session.TypeIcon} {_session.TypeLabel}";
        LblRounds.Text = _session.RoundsCompleted.ToString();
        LblTime.Text = _session.DurationLabel;
        LblCals.Text = _session.CaloriesBurned.ToString("F0");
        LblAthlete.Text = $"{_profile.Name} · {_profile.BoxingCategory}";
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        string text = $"🥊 ¡Entrenamiento destrozado!\n\n" +
                      $"🔥 Tipo: {_session.TypeLabel}\n" +
                      $"⏱️ Rounds: {_session.RoundsCompleted}\n" +
                      $"⚡ Calorías: {_session.CaloriesBurned:F0} kcal\n" +
                      $"👤 Atleta: {_profile.Name} ({_profile.BoxingCategory})\n\n" +
                      $"Preparación para la victoria. 🏆";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Resumen de Entrenamiento",
            Text = text
        });
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}