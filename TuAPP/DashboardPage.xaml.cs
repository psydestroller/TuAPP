using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;

namespace TuAPP;

public partial class DashboardPage : ContentPage
{
    public ObservableCollection<WorkoutSession> RecentSessions { get; set; } = new();

    public DashboardPage()
    {
        InitializeComponent();
        ListGymSessions.ItemsSource = RecentSessions;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        var profile = StorageService.LoadProfile();
        var fight = StorageService.LoadFightEvent();
        var history = StorageService.LoadWorkoutHistory();

        LblFightDateDisplay.Text = fight.Date.ToString("dd 'de' MMMM, yyyy");
        LblCountdown.Text = fight.DaysLeftText;

        LblCurrentWeight.Text = $"{profile.WeightKg:F1} kg";
        LblTargetWeight.Text = $"{fight.TargetWeightKg:F1} kg";
        LblCurrentWeight.TextColor = (profile.WeightKg > fight.TargetWeightKg) ? Color.FromArgb("#EF4444") : Colors.White;

        DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        var weeklySessions = history.Where(s => s.Date >= startOfWeek).ToList();

        LblDailyRounds.Text = weeklySessions.Sum(s => s.RoundsCompleted).ToString();
        LblDailyCals.Text = $"~{weeklySessions.Sum(s => s.CaloriesBurned):F0} kcal";

        RecentSessions.Clear();
        var sortedHistory = history.OrderByDescending(s => s.Date).Take(10).ToList();
        foreach (var s in sortedHistory)
        {
            RecentSessions.Add(s);
        }
    }

    private async void OnScheduleReminderClicked(object sender, EventArgs e)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }

        if (status == PermissionStatus.Granted)
        {
            NotificationService.ScheduleDailyReminder(TpReminder.Time.Value);
            await DisplayAlertAsync("Campamento Activo", $"Te recordaremos entrenar todos los días a las {TpReminder.Time.Value:hh\\:mm}. ¡No bajes la guardia!", "OK");
        }
        else
        {
            await DisplayAlertAsync("Permiso Denegado", "Necesitas habilitar las notificaciones para usar los recordatorios.", "Entendido");
        }
    }
}