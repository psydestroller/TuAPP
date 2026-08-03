using Microsoft.Maui.Dispatching;
using Plugin.Maui.Audio;

namespace TuAPP;

public partial class SprintsPage : ContentPage
{
    private enum SprintState { Idle, Prep, Sprint, Rest }
    private IDispatcherTimer _sprintTimer;
    private SprintState _currentState = SprintState.Idle;
    private int _timeLeft, _currentSet = 1, _cfgPrep, _cfgWork, _cfgRest, _cfgSets;

    private IAudioPlayer? _sprintPlayer;
    private bool _isInitializing = true; // NUEVO: Evita que guarde datos antes de cargar

    public SprintsPage()
    {
        InitializeComponent();
        LoadPickers();
        _sprintTimer = Dispatcher.CreateTimer();
        _sprintTimer.Interval = TimeSpan.FromSeconds(1);
        _sprintTimer.Tick += OnSprintTimerTicked;
    }

    private async void PlaySprintSound(string prefKey, string defaultFile)
    {
        if (!Preferences.Get("UseSound", true)) return;

        string filename = Preferences.Get(prefKey, defaultFile);

        if (filename != "bell.mp3" && !filename.StartsWith("alarma_"))
        {
            filename = defaultFile;
            Preferences.Set(prefKey, defaultFile);
        }

        try
        {
            if (_sprintPlayer != null && _sprintPlayer.IsPlaying) _sprintPlayer.Stop();
            var file = await FileSystem.OpenAppPackageFileAsync(filename);
            _sprintPlayer = AudioManager.Current.CreatePlayer(file);
            _sprintPlayer.Play();
        }
        catch { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    private void LoadPickers()
    {
        var timeList = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();
        var setsList = Enumerable.Range(1, 50).Select(i => i.ToString()).ToList();

        PckPrepMin.ItemsSource = timeList; PckPrepSec.ItemsSource = timeList;
        PckWorkMin.ItemsSource = timeList; PckWorkSec.ItemsSource = timeList;
        PckRestMin.ItemsSource = timeList; PckRestSec.ItemsSource = timeList;
        PckSets.ItemsSource = setsList;

        // NUEVO: Lee de Preferences, y si no hay nada, usa los valores por defecto
        PckPrepMin.SelectedIndex = Preferences.Get("SprintPrepMin", 0);
        PckPrepSec.SelectedIndex = Preferences.Get("SprintPrepSec", 5);
        PckWorkMin.SelectedIndex = Preferences.Get("SprintWorkMin", 0);
        PckWorkSec.SelectedIndex = Preferences.Get("SprintWorkSec", 30);
        PckRestMin.SelectedIndex = Preferences.Get("SprintRestMin", 1);
        PckRestSec.SelectedIndex = Preferences.Get("SprintRestSec", 0);
        PckSets.SelectedIndex = Preferences.Get("SprintSets", 9);

        _isInitializing = false; // Ya terminó de cargar, ahora sí puede guardar cambios
    }

    // NUEVO: Función que se dispara cada que mueves una ruleta
    private void OnSprintConfigChanged(object? sender, EventArgs e)
    {
        if (_isInitializing) return;

        Preferences.Set("SprintPrepMin", PckPrepMin.SelectedIndex);
        Preferences.Set("SprintPrepSec", PckPrepSec.SelectedIndex);
        Preferences.Set("SprintWorkMin", PckWorkMin.SelectedIndex);
        Preferences.Set("SprintWorkSec", PckWorkSec.SelectedIndex);
        Preferences.Set("SprintRestMin", PckRestMin.SelectedIndex);
        Preferences.Set("SprintRestSec", PckRestSec.SelectedIndex);
        Preferences.Set("SprintSets", PckSets.SelectedIndex);
    }

    private void OnStartSprintsClicked(object? sender, EventArgs e)
    {
        if (_sprintTimer.IsRunning)
        {
            _sprintTimer.Stop();
            DeviceDisplay.Current.KeepScreenOn = false;
            BtnStartSprints.Text = "REANUDAR";
            BtnStartSprints.BackgroundColor = Colors.Orange;
        }
        else
        {
            if (_currentState == SprintState.Idle)
            {
                _cfgPrep = (PckPrepMin.SelectedIndex * 60) + PckPrepSec.SelectedIndex;
                _cfgWork = (PckWorkMin.SelectedIndex * 60) + PckWorkSec.SelectedIndex;
                _cfgRest = (PckRestMin.SelectedIndex * 60) + PckRestSec.SelectedIndex;
                _cfgSets = PckSets.SelectedIndex + 1;

                if (_cfgWork == 0) return;
                _currentSet = 1;
                _currentState = _cfgPrep > 0 ? SprintState.Prep : SprintState.Sprint;
                _timeLeft = _currentState == SprintState.Prep ? _cfgPrep : _cfgWork;

                if (_currentState == SprintState.Prep)
                    PlaySprintSound("SoundSprintPrep", "alarma_3.mp3");
                else
                    PlaySprintSound("SoundSprintWork", "alarma_6.mp3");
            }
            _sprintTimer.Start();
            if (Preferences.Get("KeepScreenOn", true)) DeviceDisplay.Current.KeepScreenOn = true;
            BtnStartSprints.Text = "PAUSAR";
            BtnStartSprints.BackgroundColor = Colors.Red;
        }
        UpdateUI();
    }

    private void OnSkipClicked(object? sender, EventArgs e) { if (_currentState != SprintState.Idle) AdvanceState(); }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _sprintTimer.Stop();
        DeviceDisplay.Current.KeepScreenOn = false;
        _currentState = SprintState.Idle;
        _currentSet = 1;
        BtnStartSprints.Text = "INICIAR";
        BtnStartSprints.BackgroundColor = Color.FromArgb("#00E676");
        LblSprintTimer.Text = "00:00";
        LblSprintStatus.Text = "LISTO";
        LblSprintStatus.TextColor = Colors.White;
        LblSprintTimer.TextColor = Color.FromArgb("#00E676");
    }

    private void OnSprintTimerTicked(object? sender, EventArgs e)
    {
        if (_timeLeft > 0) { _timeLeft--; UpdateUI(); }
        else { AdvanceState(); }
    }

    private void AdvanceState()
    {
        if (_currentState == SprintState.Prep || _currentState == SprintState.Rest)
        {
            _currentState = SprintState.Sprint;
            _timeLeft = _cfgWork;

            PlaySprintSound("SoundSprintWork", "alarma_6.mp3");
        }
        else if (_currentState == SprintState.Sprint)
        {
            if (_currentSet >= _cfgSets)
            {
                PlaySprintSound("SoundSprintEnd", "bell.mp3");
                OnResetClicked(this, EventArgs.Empty);
                LblSprintStatus.Text = "¡TERMINADO!";
                return;
            }

            _currentState = SprintState.Rest;
            _timeLeft = _cfgRest;
            _currentSet++;

            PlaySprintSound("SoundSprintRest", "alarma_4.mp3");
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        LblSprintTimer.Text = TimeSpan.FromSeconds(_timeLeft).ToString(@"mm\:ss");
        if (_currentState == SprintState.Prep) { LblSprintStatus.Text = "PREPARACIÓN"; LblSprintTimer.TextColor = Color.FromArgb("#FFEA00"); LblSprintStatus.TextColor = Color.FromArgb("#FFEA00"); }
        else if (_currentState == SprintState.Sprint) { LblSprintStatus.Text = $"SPRINT {_currentSet}/{_cfgSets}"; LblSprintTimer.TextColor = Color.FromArgb("#00E676"); LblSprintStatus.TextColor = Color.FromArgb("#00E676"); }
        else if (_currentState == SprintState.Rest) { LblSprintStatus.Text = $"DESCANSO {_currentSet}/{_cfgSets}"; LblSprintTimer.TextColor = Color.FromArgb("#FF1744"); LblSprintStatus.TextColor = Color.FromArgb("#FF1744"); }
    }
}