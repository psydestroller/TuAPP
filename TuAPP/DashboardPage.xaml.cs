using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using Plugin.AdMob;
using Plugin.AdMob.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TuAPP;

public class SessionDisplayModel
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public int RoundsCompleted { get; set; }
    public int SprintsCompleted { get; set; }
    public string Intensity { get; set; } = string.Empty;
    public string Feeling { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string FocusArea { get; set; } = string.Empty;
    public string DurationDisplay { get; set; } = string.Empty;
    public int TotalSeconds { get; set; }
    public double PostWeight { get; set; }

    public bool IsBoxing => RoundsCompleted > 0;
    public bool IsSprint => SprintsCompleted > 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool HasPostWeight => PostWeight > 0;
}

public partial class DashboardPage : ContentPage
{
    public ObservableCollection<SessionDisplayModel> RecentSessions { get; set; } = new();

    private Guid? _editingSessionId = null;
    private WorkoutSession? _recentlyDeletedSession = null;

    private IInterstitialAdService? _interstitialAdService;

    public DashboardPage()
    {
        InitializeComponent();
        ListGymSessions.ItemsSource = RecentSessions;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler?.MauiContext != null && _interstitialAdService == null)
        {
            _interstitialAdService = Handler.MauiContext.Services.GetService<IInterstitialAdService>();
            _interstitialAdService?.PrepareAd("ca-app-pub-5227773888709145/5642690948");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        try { LoadDashboardData(); }
        catch (Exception) { LblFightDateDisplay.Text = "Error leyendo datos. Reinstala la app."; }
    }

    private void LoadDashboardData()
    {
        var profile = StorageService.LoadProfile() ?? new AthleteProfile();
        var fight = StorageService.LoadFightEvent() ?? new FightEvent();
        var history = StorageService.LoadWorkoutHistory() ?? new List<WorkoutSession>();

        DateTime fightDate = fight.Date;
        if (fightDate.Year < 2000) fightDate = DateTime.Today.AddDays(30);

        LblFightDateDisplay.Text = fightDate.ToString("dd 'de' MMMM, yyyy");
        LblCountdown.Text = fight.DaysLeftText;

        LblCurrentWeight.Text = $"{profile.WeightKg:F1} kg";
        LblTargetWeight.Text = $"{fight.TargetWeightKg:F1} kg";
        LblCurrentWeight.TextColor = (profile.WeightKg > fight.TargetWeightKg)
            ? Color.FromArgb("#EF4444")
            : Colors.White;

        RecentSessions.Clear();

        foreach (var s in history.OrderByDescending(s => s.Date))
        {
            RecentSessions.Add(new SessionDisplayModel
            {
                Id = s.Id,
                Date = s.Date,
                Type = TranslateWorkoutType(s.Type),
                RoundsCompleted = s.RoundsCompleted,
                SprintsCompleted = s.SprintsCompleted,
                Intensity = string.IsNullOrEmpty(s.Intensity) ? "Media" : s.Intensity,
                Feeling = string.IsNullOrEmpty(s.Feeling) ? "Normal" : s.Feeling,
                Notes = s.Notes,
                FocusArea = string.IsNullOrEmpty(s.FocusArea) ? "General" : s.FocusArea,
                TotalSeconds = s.TotalSeconds,
                DurationDisplay = s.TotalSeconds > 0 ? $"{s.TotalSeconds / 60} min" : "--",
                PostWeight = s.PostWeight
            });
        }

        LblEmptyHistory.IsVisible = RecentSessions.Count == 0;
    }

    private string TranslateWorkoutType(WorkoutType type) => type switch
    {
        WorkoutType.ClassicBoxing => "Boxeo Clásico",
        WorkoutType.HeavyBag => "Costal",
        WorkoutType.Sparring => "Sparring",
        WorkoutType.Shadow => "Sombra",
        WorkoutType.Sprints => "Sprints / Cardio",
        _ => "Entrenamiento Libre"
    };

    private void OnToggleEditFight(object sender, EventArgs e)
    {
        FormEditFight.IsVisible = !FormEditFight.IsVisible;
        if (FormEditFight.IsVisible)
        {
            var fight = StorageService.LoadFightEvent() ?? new FightEvent();
            DpFightDate.Date = fight.Date;
            EntryTargetWeight.Text = fight.TargetWeightKg.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private void OnSaveFight(object sender, EventArgs e)
    {
        if (double.TryParse(EntryTargetWeight.Text?.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double targetW) && targetW > 0)
        {
            DateTime safeDate = DpFightDate.Date ?? DateTime.Today;
            StorageService.SaveFightEvent(new FightEvent { Date = safeDate, TargetWeightKg = targetW });
            LoadDashboardData();
            FormEditFight.IsVisible = false;
        }
    }

    private string GetSelectedFocusAreas()
    {
        var list = new List<string>();
        if (ChkFocusSparring.IsChecked) list.Add("Sparring");
        if (ChkFocusCostal.IsChecked) list.Add("Costal");
        if (ChkFocusManoplas.IsChecked) list.Add("Manoplas");
        if (ChkFocusGobernadora.IsChecked) list.Add("Gobernadora");
        if (ChkFocusPerilla.IsChecked) list.Add("Perilla");
        if (ChkFocusPeraLoca.IsChecked) list.Add("Pera loca");
        if (ChkFocusTecnica.IsChecked) list.Add("Técnica/Sombra");
        if (ChkFocusFisico.IsChecked) list.Add("Físico/Cardio");
        if (ChkFocusOtro.IsChecked && !string.IsNullOrWhiteSpace(EntryOtherFocus.Text))
            list.Add(EntryOtherFocus.Text.Trim());

        return list.Count == 0 ? "General" : string.Join(", ", list);
    }

    private void SetSelectedFocusAreas(string focusStr)
    {
        focusStr ??= "";
        var focuses = focusStr.Split(',').Select(f => f.Trim()).ToList();

        ChkFocusSparring.IsChecked = focuses.Contains("Sparring");
        ChkFocusCostal.IsChecked = focuses.Contains("Costal");
        ChkFocusManoplas.IsChecked = focuses.Contains("Manoplas");
        ChkFocusGobernadora.IsChecked = focuses.Contains("Gobernadora");
        ChkFocusPerilla.IsChecked = focuses.Contains("Perilla");
        ChkFocusPeraLoca.IsChecked = focuses.Contains("Pera loca");
        ChkFocusTecnica.IsChecked = focuses.Contains("Técnica/Sombra");
        ChkFocusFisico.IsChecked = focuses.Contains("Físico/Cardio");

        var predefined = new HashSet<string>
        {
            "Sparring","Costal","Manoplas","Gobernadora","Perilla",
            "Pera loca","Técnica/Sombra","Físico/Cardio","General"
        };
        var custom = focuses.Where(f => !predefined.Contains(f) && !string.IsNullOrEmpty(f)).ToList();

        ChkFocusOtro.IsChecked = custom.Any();
        EntryOtherFocus.Text = custom.Any() ? string.Join(", ", custom) : string.Empty;
    }

    private void OnToggleAddJournal(object sender, EventArgs e)
    {
        _editingSessionId = null;
        LblJournalTitle.Text = "Registrar Nuevo Entrenamiento";
        BtnSaveJournal.Text = "Guardar en Bitácora";

        FormAddJournal.IsVisible = !FormAddJournal.IsVisible;
        if (FormAddJournal.IsVisible)
        {
            DpSessionDate.Date = DateTime.Today; // <-- NUEVO: Por defecto poner la fecha de hoy
            PckIntensity.SelectedIndex = 1;
            ChkFocusSparring.IsChecked = false;
            ChkFocusCostal.IsChecked = false;
            ChkFocusManoplas.IsChecked = false;
            ChkFocusGobernadora.IsChecked = false;
            ChkFocusPerilla.IsChecked = false;
            ChkFocusPeraLoca.IsChecked = false;
            ChkFocusTecnica.IsChecked = false;
            ChkFocusFisico.IsChecked = false;
            ChkFocusOtro.IsChecked = false;
            EntryOtherFocus.Text = string.Empty;
            EntryDuration.Text = string.Empty;
            EntryPostWeight.Text = string.Empty;
            EdtNotes.Text = string.Empty;
        }
    }

    private void OnEditSessionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SessionDisplayModel model)
        {
            _editingSessionId = model.Id;
            DpSessionDate.Date = model.Date; // <-- NUEVO: Cargar la fecha exacta que seleccionó al guardarlo

            PckIntensity.SelectedItem = model.Intensity;
            EntryDuration.Text = model.TotalSeconds > 0
                ? (model.TotalSeconds / 60).ToString() : "";
            SetSelectedFocusAreas(model.FocusArea);
            EntryPostWeight.Text = model.PostWeight > 0
                ? model.PostWeight.ToString("0.##", CultureInfo.InvariantCulture) : "";
            EdtNotes.Text = model.Notes;
            LblJournalTitle.Text = "Editar Entrenamiento";
            BtnSaveJournal.Text = "Guardar Cambios";
            FormAddJournal.IsVisible = true;
        }
    }

    private void OnSaveJournal(object sender, EventArgs e)
    {
        var history = StorageService.LoadWorkoutHistory() ?? new List<WorkoutSession>();

        double.TryParse(EntryPostWeight.Text?.Replace(",", "."),
            NumberStyles.Any, CultureInfo.InvariantCulture, out double postW);
        int.TryParse(EntryDuration.Text, out int durationMins);
        int totalSecs = durationMins * 60;

        // NUEVO: Extraemos la fecha de forma 100% segura
        DateTime safeSessionDate = DpSessionDate.Date ?? DateTime.Today;

        if (_editingSessionId.HasValue)
        {
            var existing = history.FirstOrDefault(x => x.Id == _editingSessionId.Value);
            if (existing != null)
            {
                existing.Date = safeSessionDate; // <-- Corrección aplicada
                existing.Intensity = PckIntensity.SelectedItem?.ToString() ?? "Media";
                existing.TotalSeconds = totalSecs;
                existing.FocusArea = GetSelectedFocusAreas();
                existing.PostWeight = postW;
                existing.Notes = EdtNotes.Text ?? string.Empty;
            }
            StorageService.SaveWorkoutHistory(history);
        }
        else
        {
            history.Add(new WorkoutSession
            {
                Id = Guid.NewGuid(),
                Date = safeSessionDate, // <-- Corrección aplicada
                Type = WorkoutType.Other,
                Intensity = PckIntensity.SelectedItem?.ToString() ?? "Media",
                TotalSeconds = totalSecs,
                FocusArea = GetSelectedFocusAreas(),
                PostWeight = postW,
                Notes = EdtNotes.Text ?? string.Empty,
                CaloriesBurned = 0,
                RoundsCompleted = 0,
                TotalRounds = 0
            });
            StorageService.SaveWorkoutHistory(history);
        }

        LoadDashboardData();
        FormAddJournal.IsVisible = false;
        _editingSessionId = null;

        if (_interstitialAdService != null)
        {
            _interstitialAdService.ShowAd();
            _interstitialAdService.PrepareAd("ca-app-pub-3940256099942544/1033173712");
        }
    }

    private async void OnDeleteSessionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SessionDisplayModel model)
        {
            var history = StorageService.LoadWorkoutHistory() ?? new List<WorkoutSession>();
            var itemToRemove = history.FirstOrDefault(x => x.Id == model.Id);

            if (itemToRemove != null)
            {
                _recentlyDeletedSession = itemToRemove;
                history.Remove(itemToRemove);
                StorageService.SaveWorkoutHistory(history);
                LoadDashboardData();

                UndoPanel.IsVisible = true;
                await Task.Delay(5000);

                if (_recentlyDeletedSession?.Id == itemToRemove.Id)
                {
                    UndoPanel.IsVisible = false;
                    _recentlyDeletedSession = null;
                }
            }
        }
    }

    private void OnUndoDeleteClicked(object sender, EventArgs e)
    {
        if (_recentlyDeletedSession != null)
        {
            var history = StorageService.LoadWorkoutHistory() ?? new List<WorkoutSession>();
            history.Add(_recentlyDeletedSession);
            StorageService.SaveWorkoutHistory(history);
            _recentlyDeletedSession = null;
            UndoPanel.IsVisible = false;
            LoadDashboardData();
        }
    }
}