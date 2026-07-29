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

        PckPrepMin.SelectedIndex = 0; PckPrepSec.SelectedIndex = 5;
        PckWorkMin.SelectedIndex = 0; PckWorkSec.SelectedIndex = 30;
        PckRestMin.SelectedIndex = 1; PckRestSec.SelectedIndex = 0;
        PckSets.SelectedIndex = 9;
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

                // INICIA EL PRIMER SONIDO DE LA RUTINA
                if (_currentState == SprintState.Prep)
                    PlaySprintSound("SoundSprintPrep", "alarma_3.mp3");
                else
                    PlaySprintSound("SoundSprintWork", "alarma_1.mp3"); // Cambiado a alarma 1
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

            // SUENA EL ARRANQUE DEL SPRINT
            PlaySprintSound("SoundSprintWork", "alarma_1.mp3"); // Cambiado a alarma 1
        }
        else if (_currentState == SprintState.Sprint)
        {
            if (_currentSet >= _cfgSets)
            {
                // LA RUTINA SE TERMINA POR COMPLETO
                PlaySprintSound("SoundSprintEnd", "bell.mp3");
                OnResetClicked(this, EventArgs.Empty);
                LblSprintStatus.Text = "¡TERMINADO!";
                return;
            }

            _currentState = SprintState.Rest;
            _timeLeft = _cfgRest;
            _currentSet++;

            // SUENA EL INICIO DE LA RECUPERACIÓN
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