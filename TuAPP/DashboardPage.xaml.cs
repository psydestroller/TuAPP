using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace TuAPP;

// Se agregó el ID para poder identificar cuál borrar o editar
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

    public bool IsBoxing => RoundsCompleted > 0;
    public bool IsSprint => SprintsCompleted > 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
}

public partial class DashboardPage : ContentPage
{
    public ObservableCollection<SessionDisplayModel> RecentSessions { get; set; } = new();

    // Variable para saber si estamos editando un registro viejo
    private Guid? _editingSessionId = null;

    public DashboardPage()
    {
        InitializeComponent();
        ListGymSessions.ItemsSource = RecentSessions;
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
        LblCurrentWeight.TextColor = (profile.WeightKg > fight.TargetWeightKg) ? Color.FromArgb("#EF4444") : Colors.White;

        RecentSessions.Clear();

        var sortedHistory = history.OrderByDescending(s => s.Date).ToList();

        foreach (var s in sortedHistory)
        {
            RecentSessions.Add(new SessionDisplayModel
            {
                Id = s.Id, // Pasamos el ID real
                Date = s.Date,
                Type = TranslateWorkoutType(s.Type),
                RoundsCompleted = s.RoundsCompleted,
                SprintsCompleted = s.SprintsCompleted,
                Intensity = string.IsNullOrEmpty(s.Intensity) ? "Media" : s.Intensity,
                Feeling = string.IsNullOrEmpty(s.Feeling) ? "Normal" : s.Feeling,
                Notes = s.Notes
            });
        }

        LblEmptyHistory.IsVisible = RecentSessions.Count == 0;
    }

    private string TranslateWorkoutType(WorkoutType type)
    {
        return type switch
        {
            WorkoutType.ClassicBoxing => "Boxeo Clásico",
            WorkoutType.HeavyBag => "Costal",
            WorkoutType.Sparring => "Sparring",
            WorkoutType.Shadow => "Sombra",
            WorkoutType.Sprints => "Sprints / Cardio",
            _ => "Entrenamiento Libre"
        };
    }

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
        if (double.TryParse(EntryTargetWeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double targetW) && targetW > 0)
        {
            // AQUÍ ESTÁ EL REPARO: Agregamos el "?? DateTime.Today" de nuevo
            DateTime safeDate = DpFightDate.Date ?? DateTime.Today;

            var fight = new FightEvent { Date = safeDate, TargetWeightKg = targetW };
            StorageService.SaveFightEvent(fight);

            LoadDashboardData();
            FormEditFight.IsVisible = false;
        }
    }

    // =======================================================
    // LOGICA DE AGREGAR, EDITAR Y ELIMINAR BITÁCORA
    // =======================================================

    private void OnToggleAddJournal(object sender, EventArgs e)
    {
        _editingSessionId = null; // Como es uno nuevo, limpiamos el ID
        LblJournalTitle.Text = "¿Cómo te fue hoy?";
        BtnSaveJournal.Text = "Guardar en Bitácora";

        FormAddJournal.IsVisible = !FormAddJournal.IsVisible;
        if (FormAddJournal.IsVisible)
        {
            PckIntensity.SelectedIndex = 1;
            PckFeeling.SelectedIndex = 1;
            EdtNotes.Text = string.Empty;
        }
    }

    private void OnEditSessionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SessionDisplayModel model)
        {
            // Guardamos el ID que estamos editando
            _editingSessionId = model.Id;

            // Llenamos el formulario con sus datos actuales
            PckIntensity.SelectedItem = model.Intensity;
            PckFeeling.SelectedItem = model.Feeling;
            EdtNotes.Text = model.Notes;

            // Cambiamos los textos
            LblJournalTitle.Text = "Editar Entrenamiento";
            BtnSaveJournal.Text = "Guardar Cambios";

            FormAddJournal.IsVisible = true;
        }
    }

    private async void OnDeleteSessionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SessionDisplayModel model)
        {
            bool answer = await DisplayAlert("Eliminar", "¿Seguro que deseas borrar este registro de tu campamento?", "Sí, borrar", "Cancelar");
            if (answer)
            {
                var history = StorageService.LoadWorkoutHistory() ?? new List<WorkoutSession>();
                var itemToRemove = history.FirstOrDefault(x => x.Id == model.Id);

                if (itemToRemove != null)
                {
                    history.Remove(itemToRemove);
                    StorageService.SaveWorkoutHistory(history); // Guarda la lista sin ese elemento
                    LoadDashboardData(); // Recarga la pantalla
                }
            }
        }
    }

    private void OnSaveJournal(object sender, EventArgs e)
    {
        var history = StorageService.LoadWorkoutHistory() ?? new List<WorkoutSession>();

        if (_editingSessionId.HasValue)
        {
            // MODIFICAR EXISTENTE
            var existingSession = history.FirstOrDefault(x => x.Id == _editingSessionId.Value);
            if (existingSession != null)
            {
                existingSession.Intensity = PckIntensity.SelectedItem?.ToString() ?? "Media";
                existingSession.Feeling = PckFeeling.SelectedItem?.ToString() ?? "Normal";
                existingSession.Notes = EdtNotes.Text ?? string.Empty;
            }
            StorageService.SaveWorkoutHistory(history); // Sobrescribimos la base de datos
            DisplayAlert("Actualizado", "El registro ha sido modificado.", "OK");
        }
        else
        {
            // CREAR UNO NUEVO
            var manualSession = new WorkoutSession
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                Type = WorkoutType.Other,
                Intensity = PckIntensity.SelectedItem?.ToString() ?? "Media",
                Feeling = PckFeeling.SelectedItem?.ToString() ?? "Normal",
                Notes = EdtNotes.Text ?? string.Empty,
                TotalSeconds = 0,
                CaloriesBurned = 0,
                RoundsCompleted = 0,
                TotalRounds = 0
            };

            history.Add(manualSession);
            StorageService.SaveWorkoutHistory(history);
            DisplayAlert("Bitácora Guardada", "Tu registro del día ha sido añadido.", "OK");
        }

        // Limpiamos todo al terminar
        LoadDashboardData();
        FormAddJournal.IsVisible = false;
        _editingSessionId = null;
    }
}