using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using System.Globalization;

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

        // PATTERN MATCHING UNIVERSAL: Funciona sea DateTime o DateTime?
        DateTime fightDate = fight.Date is DateTime fd ? fd : DateTime.Today.AddDays(30);
        LblFightDateDisplay.Text = fightDate.ToString("dd 'de' MMMM, yyyy");
        LblCountdown.Text = fight.DaysLeftText;

        LblCurrentWeight.Text = $"{profile.WeightKg:F1} kg";
        LblTargetWeight.Text = $"{fight.TargetWeightKg:F1} kg";
        LblCurrentWeight.TextColor = (profile.WeightKg > fight.TargetWeightKg) ? Color.FromArgb("#EF4444") : Colors.White;

        DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        var weeklySessions = history.Where(s => s.Date >= startOfWeek).ToList();

        int currentRounds = weeklySessions.Sum(s => s.RoundsCompleted);
        int goalRounds = 50;
        double progressPercentage = Math.Min((double)currentRounds / goalRounds, 1.0);

        LblDailyCals.Text = $"~{weeklySessions.Sum(s => s.CaloriesBurned):F0} kcal";

        if (ProgressGraphicsView.Drawable is CircularProgressDrawable drawable)
        {
            drawable.Progress = progressPercentage;
            drawable.ProgressColor = progressPercentage >= 1.0 ? Color.FromArgb("#EAB308") : Color.FromArgb("#10B981");
            ProgressGraphicsView.Invalidate();
        }

        RecentSessions.Clear();
        var sortedHistory = history.OrderByDescending(s => s.Date).Take(10).ToList();
        foreach (var s in sortedHistory) RecentSessions.Add(s);
    }

    private void OnToggleEditFight(object sender, EventArgs e)
    {
        FormEditFight.IsVisible = !FormEditFight.IsVisible;
        if (FormEditFight.IsVisible)
        {
            var fight = StorageService.LoadFightEvent();
            DpFightDate.Date = fight.Date is DateTime fd ? fd : DateTime.Today.AddDays(30);
            EntryTargetWeight.Text = fight.TargetWeightKg.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private void OnSaveFight(object sender, EventArgs e)
    {
        if (double.TryParse(EntryTargetWeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double targetW) && targetW > 0)
        {
            DateTime safeDate = DpFightDate.Date is DateTime dt ? dt : DateTime.Today;
            var fight = new FightEvent { Date = safeDate, TargetWeightKg = targetW };
            StorageService.SaveFightEvent(fight);

            LoadDashboardData();
            FormEditFight.IsVisible = false;
        }
    }
}