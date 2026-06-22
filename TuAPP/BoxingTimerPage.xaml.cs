using Plugin.Maui.Audio;
using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class BoxingTimerPage : ContentPage
{
    private enum TimerState { Idle, Prep, Work, Rest }
    private TimerState _currentState = TimerState.Idle;
    private int _cfgRounds, _cfgWork, _cfgRest, _cfgPrep;
    private int _currentRound = 1, _timeLeft, _currentMaxTime;
    private bool _isTimerRunning = false;
    private IAudioPlayer? _bellPlayer;

    // EL SECRETO: El "Reloj Atómico" de C# que ignora el lag de Android
    private DateTime _phaseEndTime;

    public BoxingTimerPage()
    {
        InitializeComponent();
        ForegroundTimerBridge.Initialize();
        LoadAudio();
    }

    private async void LoadAudio()
    {
        try
        {
            var file = await FileSystem.OpenAppPackageFileAsync("bell.mp3");
            _bellPlayer = AudioManager.Current.CreatePlayer(file);
        }
        catch { }
    }

    // 1. GESTIÓN LIMPIA DE MEMORIA (Evita que se dupliquen los ticks al abrir/cerrar)
    protected override void OnAppearing()
    {
        base.OnAppearing();

        ForegroundTimerBridge.OnTick -= HandleServiceTick;
        ForegroundTimerBridge.OnServiceStop -= HandleServiceStop;

        ForegroundTimerBridge.OnTick += HandleServiceTick;
        ForegroundTimerBridge.OnServiceStop += HandleServiceStop;

        _cfgRounds = Preferences.Get("BoxingRounds", 12);
        _cfgWork = Preferences.Get("BoxingWork", 180);
        _cfgRest = Preferences.Get("BoxingRest", 30);
        _cfgPrep = Preferences.Get("BoxingPrep", 10);

        LblWorkConfig.Text = TimeSpan.FromSeconds(_cfgWork).ToString(@"mm\:ss");
        LblRestConfig.Text = TimeSpan.FromSeconds(_cfgRest).ToString(@"mm\:ss");

        if (_currentState == TimerState.Idle) ResetTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ForegroundTimerBridge.OnTick -= HandleServiceTick;
        ForegroundTimerBridge.OnServiceStop -= HandleServiceStop;
    }

    // 2. RECEPCIÓN BLINDADA CON RELOJ DE HARDWARE
    private void HandleServiceTick(int secondsLeft, string phase)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_isTimerRunning) return;

            // En lugar de creerle a Android (que se salta números), calculamos 
            // la distancia exacta contra el reloj de cuarzo del procesador:
            int atomicSecondsLeft = (int)Math.Ceiling((_phaseEndTime - DateTime.Now).TotalSeconds);

            if (atomicSecondsLeft <= 0)
            {
                AdvanceState();
            }
            else
            {
                _timeLeft = atomicSecondsLeft;
                UpdateUI();
            }
        });
    }

    private void HandleServiceStop()
    {
        MainThread.BeginInvokeOnMainThread(() => ResetTimer());
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_isTimerRunning)
        {
            _isTimerRunning = false;
            ForegroundTimerBridge.Pause();
            if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                DeviceDisplay.Current.KeepScreenOn = false;
        }
        else
        {
            _isTimerRunning = true;

            if (_currentState == TimerState.Idle)
            {
                _currentState = TimerState.Prep;
                _timeLeft = _cfgPrep > 0 ? _cfgPrep : _cfgWork;
                _currentMaxTime = _timeLeft;
                if (_cfgPrep == 0) _currentState = TimerState.Work;

                _phaseEndTime = DateTime.Now.AddSeconds(_timeLeft);
                ForegroundTimerBridge.Start(_timeLeft, GetPhaseName(_currentState));
            }
            else
            {
                // Al reanudar, recalculamos la hora de finalización en el futuro real
                _phaseEndTime = DateTime.Now.AddSeconds(_timeLeft);
                ForegroundTimerBridge.Resume();
            }

            if (Preferences.Get("KeepScreenOn", true))
                DeviceDisplay.Current.KeepScreenOn = true;
        }
        UpdateUI();
    }

    // 3. LÓGICA DE SKIP REPARADA (Cero desincronizaciones)
    private void AdvanceState()
    {
        if (_currentState == TimerState.Prep || _currentState == TimerState.Rest)
        {
            _currentState = TimerState.Work;
            _timeLeft = _cfgWork;
            _currentMaxTime = _cfgWork;

            if (_isTimerRunning)
            {
                if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundStart();
                if (Preferences.Get("UseSound", true)) _bellPlayer?.Play();
            }
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                _isTimerRunning = false;
                ForegroundTimerBridge.Stop();
                DeviceDisplay.Current.KeepScreenOn = false;

                if (Preferences.Get("UseSound", true)) _bellPlayer?.Play();

                SaveWorkout();
                ResetTimer();
                return;
            }

            _currentState = TimerState.Rest;
            _timeLeft = _cfgRest;
            _currentMaxTime = _cfgRest;
            _currentRound++;

            if (_isTimerRunning)
            {
                if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundEnd();
                if (Preferences.Get("UseSound", true)) _bellPlayer?.Play();
            }
        }

        _phaseEndTime = DateTime.Now.AddSeconds(_timeLeft);

        if (_isTimerRunning)
        {
            ForegroundTimerBridge.Start(_timeLeft, GetPhaseName(_currentState));
        }
        else
        {
            // Si el usuario dio Skip en PAUSA, obligamos a Android a quedarse callado
            ForegroundTimerBridge.Stop();
        }

        UpdateUI();
    }

    private void SaveWorkout()
    {
        string selected = PckWorkoutType.SelectedItem?.ToString() ?? "Classic boxing 🥊";
        WorkoutType currentWorkoutType = WorkoutType.ClassicBoxing;
        if (selected.Contains("Sombra")) currentWorkoutType = WorkoutType.Shadow;
        if (selected.Contains("Costal")) currentWorkoutType = WorkoutType.HeavyBag;
        if (selected.Contains("Sparring")) currentWorkoutType = WorkoutType.Sparring;
        if (selected.Contains("Cuerda")) currentWorkoutType = WorkoutType.JumpRope;

        var profile = StorageService.LoadProfile();
        int totalSecs = (_cfgRounds * _cfgWork) + ((_cfgRounds - 1) * _cfgRest);
        double calsBurned = CalorieCalculator.Calculate(currentWorkoutType, profile.WeightKg, totalSecs);

        var session = new WorkoutSession
        {
            Type = currentWorkoutType,
            TotalRounds = _cfgRounds,
            RoundsCompleted = _currentRound, // Arreglado: ahora guarda las rondas reales que hiciste
            TotalSeconds = totalSecs,
            CaloriesBurned = calsBurned,
            Notes = "Guardado automático"
        };
        StorageService.AddWorkoutSession(session);

        Navigation.PushModalAsync(new WorkoutSummaryPage(session));
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        ForegroundTimerBridge.Stop();
        ResetTimer();
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        if (_currentState != TimerState.Idle) AdvanceState();
    }

    private void ResetTimer()
    {
        _isTimerRunning = false;
        _currentState = TimerState.Idle;
        _currentRound = 1;
        _timeLeft = _cfgWork;
        _currentMaxTime = _cfgWork;
        DeviceDisplay.Current.KeepScreenOn = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        BtnPlayPause.Text = _isTimerRunning ? "⏸" : "▶";
        LblRound.Text = $"Ronda {_currentRound}/{_cfgRounds}";
        LblTimer.Text = TimeSpan.FromSeconds(_timeLeft).ToString(@"mm\:ss");

        LblStatusTop.Text = GetPhaseName(_currentState);

        if (_currentState == TimerState.Prep) { LblStatusTop.TextColor = Color.FromArgb("#EAB308"); LblTimer.TextColor = Color.FromArgb("#EAB308"); }
        else if (_currentState == TimerState.Work) { LblStatusTop.TextColor = Color.FromArgb("#10B981"); LblTimer.TextColor = Color.FromArgb("#10B981"); }
        else if (_currentState == TimerState.Rest) { LblStatusTop.TextColor = Color.FromArgb("#EF4444"); LblTimer.TextColor = Color.FromArgb("#EF4444"); }
        else { LblStatusTop.TextColor = Colors.White; LblTimer.TextColor = Colors.White; }

        if (TimerGraphicsView.Drawable is CircularProgressDrawable drawable)
        {
            drawable.Progress = _currentMaxTime > 0 ? (double)_timeLeft / _currentMaxTime : 0;
            drawable.ProgressColor = LblStatusTop.TextColor;
            TimerGraphicsView.Invalidate();
        }
    }

    private string GetPhaseName(TimerState state)
    {
        return state switch
        {
            TimerState.Prep => "PREPARACIÓN",
            TimerState.Work => "¡PELEA!",
            TimerState.Rest => "DESCANSO",
            _ => "LISTO"
        };
    }
}