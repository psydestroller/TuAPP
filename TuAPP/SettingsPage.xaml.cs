using Plugin.Maui.Audio;
using TuAPP.Services;

namespace TuAPP;

public partial class SettingsPage : ContentPage
{
    private bool _isInitializing = true;
    private IAudioPlayer? _previewPlayer;

    // Variables para el menú flotante
    private string _currentCategory = "";
    private string _tempSoundSelection = "";

    // Diccionario limpio: SOLO TUS 6 ALARMAS Y LA CAMPANA
    private readonly Dictionary<string, string> _soundMap = new()
    {
        { "Campana clásica", "bell.mp3" },
        { "Alarma 1", "alarma_1.mp3" },
        { "Alarma 2", "alarma_2.mp3" },
        { "Alarma 3", "alarma_3.mp3" },
        { "Alarma 4", "alarma_4.mp3" },
        { "Alarma 5", "alarma_5.mp3" },
        { "Alarma 6", "alarma_6.mp3" }
    };

    public SettingsPage()
    {
        InitializeComponent();
        LoadPickers();
        LoadPreferences();
        LoadSoundSettings();
        _isInitializing = false;
    }

    private void LoadPickers()
    {
        var rounds = Enumerable.Range(1, 50).Select(i => i.ToString()).ToList();
        var times = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();

        PckRounds.ItemsSource = rounds;
        PckWorkMin.ItemsSource = times; PckWorkSec.ItemsSource = times;
        PckRestMin.ItemsSource = times; PckRestSec.ItemsSource = times;
        PckPrepMin.ItemsSource = times; PckPrepSec.ItemsSource = times;
    }

    private void LoadPreferences()
    {
        PckRounds.SelectedIndex = Preferences.Get("BoxingRounds", 12) - 1;
        int w = Preferences.Get("BoxingWork", 180); PckWorkMin.SelectedIndex = w / 60; PckWorkSec.SelectedIndex = w % 60;
        int r = Preferences.Get("BoxingRest", 30); PckRestMin.SelectedIndex = r / 60; PckRestSec.SelectedIndex = r % 60;
        int p = Preferences.Get("BoxingPrep", 10); PckPrepMin.SelectedIndex = p / 60; PckPrepSec.SelectedIndex = p % 60;

        SwSound.IsToggled = Preferences.Get("UseSound", true);
        SwVib.IsToggled = Preferences.Get("UseVibration", true);
        SwScreen.IsToggled = Preferences.Get("KeepScreenOn", true);

        string savedReminder = Preferences.Get("SavedReminderTime", "17:00:00");
        if (TimeSpan.TryParse(savedReminder, out TimeSpan ts)) TpReminder.Time = ts;
        else TpReminder.Time = new TimeSpan(17, 0, 0);
    }

    // Valores por defecto arreglados
    private void LoadSoundSettings()
    {
        // PREDETERMINADOS DE BOXEO
        BtnSoundPrep.Text = GetNameByFilename(Preferences.Get("SoundPrep", "alarma_3.mp3"));
        BtnSoundRoundStart.Text = GetNameByFilename(Preferences.Get("SoundRoundStart", "bell.mp3"));
        BtnSound10Sec.Text = GetNameByFilename(Preferences.Get("Sound10Sec", "alarma_6.mp3"));
        BtnSoundRoundEnd.Text = GetNameByFilename(Preferences.Get("SoundRoundEnd", "bell.mp3"));

        // PREDETERMINADOS DE SPRINTS
        BtnSoundSprintPrep.Text = GetNameByFilename(Preferences.Get("SoundSprintPrep", "alarma_3.mp3"));
        BtnSoundSprintWork.Text = GetNameByFilename(Preferences.Get("SoundSprintWork", "alarma_1.mp3"));
        BtnSoundSprintRest.Text = GetNameByFilename(Preferences.Get("SoundSprintRest", "alarma_4.mp3"));
        BtnSoundSprintEnd.Text = GetNameByFilename(Preferences.Get("SoundSprintEnd", "bell.mp3"));

        // Llenamos el menú flotante con las opciones
        CvSonidos.ItemsSource = _soundMap.Keys.ToList();
    }

    private string GetNameByFilename(string filename)
    {
        return _soundMap.FirstOrDefault(x => x.Value == filename).Key ?? "Campana clásica";
    }

    // =========================================================
    // LÓGICA DEL MENÚ FLOTANTE
    // =========================================================

    private void OnOpenMenuClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            _currentCategory = btn.CommandParameter.ToString() ?? "";

            LblMenuTitle.Text = _currentCategory switch
            {
                "Prep" => "Boxeo: Preparación",
                "Start" => "Boxeo: Inicio de Round",
                "10Sec" => "Boxeo: Aviso 10 Seg",
                "End" => "Boxeo: Fin de Round",
                "SprintPrep" => "Sprints: Preparación",
                "SprintWork" => "Sprints: ¡A Correr!",
                "SprintRest" => "Sprints: Recuperación",
                "SprintEnd" => "Sprints: Fin de Rutina",
                _ => "Seleccionar Sonido"
            };

            _tempSoundSelection = btn.Text;
            CvSonidos.SelectedItem = _tempSoundSelection;
            MenuSonidosOverlay.IsVisible = true;
        }
    }

    private async void OnSonidoSeleccionado(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selectedSound)
        {
            _tempSoundSelection = selectedSound;
            string filename = _soundMap[selectedSound];

            try
            {
                if (_previewPlayer != null && _previewPlayer.IsPlaying)
                    _previewPlayer.Stop();

                var file = await FileSystem.OpenAppPackageFileAsync(filename);
                _previewPlayer = AudioManager.Current.CreatePlayer(file);
                _previewPlayer.Play();
            }
            catch { }
        }
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        _previewPlayer?.Stop();
        MenuSonidosOverlay.IsVisible = false;

        if (string.IsNullOrEmpty(_tempSoundSelection)) return;

        string filename = _soundMap[_tempSoundSelection];

        switch (_currentCategory)
        {
            case "Prep": Preferences.Set("SoundPrep", filename); BtnSoundPrep.Text = _tempSoundSelection; break;
            case "Start": Preferences.Set("SoundRoundStart", filename); BtnSoundRoundStart.Text = _tempSoundSelection; break;
            case "10Sec": Preferences.Set("Sound10Sec", filename); BtnSound10Sec.Text = _tempSoundSelection; break;
            case "End": Preferences.Set("SoundRoundEnd", filename); BtnSoundRoundEnd.Text = _tempSoundSelection; break;
            case "SprintPrep": Preferences.Set("SoundSprintPrep", filename); BtnSoundSprintPrep.Text = _tempSoundSelection; break;
            case "SprintWork": Preferences.Set("SoundSprintWork", filename); BtnSoundSprintWork.Text = _tempSoundSelection; break;
            case "SprintRest": Preferences.Set("SoundSprintRest", filename); BtnSoundSprintRest.Text = _tempSoundSelection; break;
            case "SprintEnd": Preferences.Set("SoundSprintEnd", filename); BtnSoundSprintEnd.Text = _tempSoundSelection; break;
        }
    }

    // =========================================================
    // OTROS EVENTOS
    // =========================================================

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (_isInitializing) return;
        Preferences.Set("BoxingRounds", PckRounds.SelectedIndex + 1);
        Preferences.Set("BoxingWork", (PckWorkMin.SelectedIndex * 60) + PckWorkSec.SelectedIndex);
        Preferences.Set("BoxingRest", (PckRestMin.SelectedIndex * 60) + PckRestSec.SelectedIndex);
        Preferences.Set("BoxingPrep", (PckPrepMin.SelectedIndex * 60) + PckPrepSec.SelectedIndex);
    }

    private void OnTogglesChanged(object? sender, ToggledEventArgs e)
    {
        if (_isInitializing) return;
        Preferences.Set("UseSound", SwSound.IsToggled);
        Preferences.Set("UseVibration", SwVib.IsToggled);
        Preferences.Set("KeepScreenOn", SwScreen.IsToggled);
    }

    private async void OnSaveReminder(object? sender, EventArgs e)
    {
        if (TpReminder != null)
        {
            TimeSpan safeTime = TpReminder.Time is TimeSpan ts ? ts : new TimeSpan(17, 0, 0);

            Preferences.Set("SavedReminderTime", safeTime.ToString());

            await NotificationService.RequestAndScheduleAsync(safeTime);
            await DisplayAlert("Recordatorio Activado", "Te avisaremos 30 minutos antes para que prepares tus vendas.", "OK");
        }
    }

    private void OnCancelReminder(object? sender, EventArgs e)
    {
#if ANDROID
        TuAPP.Platforms.Android.AlarmScheduler.CancelDaily(101);
        TuAPP.Platforms.Android.AlarmScheduler.CancelDaily(102);
#endif
        Preferences.Remove("SavedReminderTime");
        DisplayAlert("Recordatorio Desactivado", "Ya no recibirás avisos diarios de entrenamiento en segundo plano.", "OK");
    }
}