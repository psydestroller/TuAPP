using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace TuAPP;

public class GymSession
{
    public string Type { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int Rounds { get; set; }
    public double Calories { get; set; }
    public string RoundsText => $"{Rounds} Rounds";
    public string CaloriesText => $"~{Calories:F0} kcal";
}

public partial class DashboardPage : ContentPage
{
    public ObservableCollection<GymSession> TodaySessions { get; set; } = new();
    private GymSession? _lastSavedSession;

    public DashboardPage()
    {
        InitializeComponent();
        ListGymSessions.ItemsSource = TodaySessions;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        // Cargar metas
        long dateTicks = Preferences.Get("TargetFightDateTicks", new DateTime(2026, 7, 26).Ticks);
        DateTime targetDate = new DateTime(dateTicks);
        double targetWeight = Preferences.Get("TargetWeightLimit", 67.0);

        DpFightDate.Date = targetDate;
        EntryTargetWeight.Text = targetWeight.ToString("F1");

        // Cuenta regresiva
        TimeSpan countdown = targetDate.Date - DateTime.Now.Date;
        LblFightDateDisplay.Text = targetDate.ToString("dd 'de' MMMM, yyyy");

        if (countdown.Days > 0)
            LblCountdown.Text = $"{countdown.Days} Días";
        else if (countdown.Days == 0)
            LblCountdown.Text = "¡ES HOY!";
        else
            LblCountdown.Text = "Completado";

        // Evaluar pesos
        string currentWeightStr = Preferences.Get("AthleteWeight", "66.0");
        LblCurrentWeight.Text = $"{currentWeightStr} kg";
        LblTargetWeight.Text = $"{targetWeight:F1} kg";

        if (double.TryParse(currentWeightStr, out double currentW))
        {
            if (currentW > targetWeight)
                LblCurrentWeight.TextColor = Color.FromArgb("#EF4444");
            else
                LblCurrentWeight.TextColor = Colors.White;
        }

        UpdateDailyStats();
    }

    private void OnEditGoalsClicked(object? sender, EventArgs e)
    {
        FormEditGoals.IsVisible = !FormEditGoals.IsVisible;
    }

    private void OnSaveGoalsClicked(object? sender, EventArgs e)
    {
        // SOLUCIÓN AL ERROR CS1061: Convertimos forzosamente para extraer los Ticks sin fallas
        DateTime safeDate = Convert.ToDateTime(DpFightDate.Date);
        Preferences.Set("TargetFightDateTicks", safeDate.Ticks);

        if (double.TryParse(EntryTargetWeight.Text, out double w))
            Preferences.Set("TargetWeightLimit", w);

        FormEditGoals.IsVisible = false;
        LoadDashboardData();
    }

    private void OnSaveGymSession(object? sender, EventArgs e)
    {
        if (int.TryParse(EntryGymRounds.Text, out int rounds) && PckGymType.SelectedIndex != -1)
        {
            string type = PckGymType.SelectedItem?.ToString() ?? "Entrenamiento";
            double weight = Preferences.Get("TargetWeightLimit", 67.0);
            double.TryParse(Preferences.Get("AthleteWeight", "67.0"), out weight);

            double calsPerRound = 0;
            switch (type)
            {
                case "Sparring": calsPerRound = (14.0 * 3.5 * weight) / 200 * 3; break;
                case "Manoplas": calsPerRound = (11.0 * 3.5 * weight) / 200 * 3; break;
                case "Costal": calsPerRound = (9.0 * 3.5 * weight) / 200 * 3; break;
                case "Sombra": calsPerRound = (7.0 * 3.5 * weight) / 200 * 3; break;
            }

            double totalCals = calsPerRound * rounds;

            _lastSavedSession = new GymSession
            {
                Type = type,
                Rounds = rounds,
                Calories = totalCals,
                Notes = string.IsNullOrWhiteSpace(EntryGymNotes.Text) ? "Sin notas" : EntryGymNotes.Text
            };

            TodaySessions.Insert(0, _lastSavedSession);

            EntryGymRounds.Text = "";
            EntryGymNotes.Text = "";
            PckGymType.SelectedIndex = -1;

            UpdateDailyStats();
        }
    }

    private void UpdateDailyStats()
    {
        int totalRounds = 0;
        double totalCalories = 0;

        foreach (var session in TodaySessions)
        {
            totalRounds += session.Rounds;
            totalCalories += session.Calories;
        }

        LblDailyRounds.Text = totalRounds.ToString();
        LblDailyCals.Text = $"~{totalCalories:F0} kcal";
    }

    private async void OnShareSession(object? sender, EventArgs e)
    {
        if (_lastSavedSession == null && TodaySessions.Count == 0)
        {
            await DisplayAlert("Aviso", "Primero guarda una sesión para compartirla.", "OK");
            return;
        }

        var sessionToShare = _lastSavedSession ?? TodaySessions[0];

        string shareText = $"🥊 ¡Sesión completada en mi camino al ring!\n\n" +
                           $"🔥 Tipo: {sessionToShare.Type}\n" +
                           $"⏱️ Rounds: {sessionToShare.Rounds}\n" +
                           $"⚡ Calorías quemadas: {sessionToShare.Calories:F0} kcal\n" +
                           $"📝 {sessionToShare.Notes}\n\n" +
                           $"{LblCountdown.Text} para la próxima pelea.";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Resumen de Entrenamiento",
            Text = shareText
        });
    }
}