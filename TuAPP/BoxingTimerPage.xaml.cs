using Microsoft.Maui.Dispatching;
using Plugin.Maui.Audio;
using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class BoxingTimerPage : ContentPage
{
    private enum TimerState { Idle, Prep, Work, Rest }
    private IDispatcherTimer _timer;
    private TimerState _currentState = TimerState.Idle;
    private int _cfgRounds, _cfgWork, _cfgRest, _cfgPrep;
    private int _currentRound = 1, _timeLeft;
    private IAudioPlayer? _bellPlayer;

    public BoxingTimerPage()
    {
        InitializeComponent();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTicked;
        LoadAudio();
    }

    private async void LoadAudio()
    {
        try
        {
            var file = await FileSystem.OpenAppPackageFileAsync("bell.mp3");
            _bellPlayer = AudioManager.Current.CreatePlayer(file);
        }
        catch { /* Falla segura si no encuentra el mp3 */ }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Adelanto de la Fase 3: Asegurar permisos de notificaciones al entrar
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.PostNotifications>();
        }

        _cfgRounds = Preferences.Get("BoxingRounds", 12);
        _cfgWork = Preferences.Get("BoxingWork", 180);
        _cfgRest = Preferences.Get("BoxingRest", 30);
        _cfgPrep = Preferences.Get("BoxingPrep", 10);

        if (_currentState == TimerState.Idle) ResetTimer();
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_timer.IsRunning)
        {
            _timer.Stop();
            ForegroundTimerBridge.Stop();
            DeviceDisplay.Current.KeepScreenOn = false;
        }
        else
        {
            if (_currentState == TimerState.Idle)
            {
                _currentState = TimerState.Prep;
                _timeLeft = _cfgPrep > 0 ? _cfgPrep : _cfgWork;
                if (_cfgPrep == 0) _currentState = TimerState.Work;
            }
            _timer.Start();
            ForegroundTimerBridge.Start(_timeLeft, LblStatusTop.Text);
            DeviceDisplay.Current.KeepScreenOn = true;
        }
        UpdateUI();
    }

    private void OnTimerTicked(object? sender, EventArgs e)
    {
        if (_timeLeft > 0)
        {
            _timeLeft--;
            UpdateUI();
        }
        else AdvanceState();
    }

    private void AdvanceState()
    {
        if (_currentState == TimerState.Prep || _currentState == TimerState.Rest)
        {
            _currentState = TimerState.Work;
            _timeLeft = _cfgWork;
            if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundStart();
            _bellPlayer?.Play();
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                // ==========================================
                // LÓGICA DE FASE 2: CÁLCULO DINÁMICO REAL
                // ==========================================
                _timer.Stop();
                ForegroundTimerBridge.Stop();
                DeviceDisplay.Current.KeepScreenOn = false;
                _bellPlayer?.Play();

                // 1. Leer qué entrenaste
                string selected = PckWorkoutType.SelectedItem?.ToString() ?? "Boxeo Clásico";
                WorkoutType currentWorkoutType = selected switch
                {
                    "Sombra" => WorkoutType.Shadow,
                    "Costal" => WorkoutType.HeavyBag,
                    "Sparring" => WorkoutType.Sparring,
                    "Cuerda" => WorkoutType.JumpRope,
                    _ => WorkoutType.ClassicBoxing
                };

                // 2. Calcular calorías exactas
                var profile = StorageService.LoadProfile();
                int totalSecs = (_cfgRounds * _cfgWork) + ((_cfgRounds - 1) * _cfgRest);
                double calsBurned = CalorieCalculator.Calculate(currentWorkoutType, profile.WeightKg, totalSecs);

                // 3. Guardar en el historial
                var session = new WorkoutSession
                {
                    Type = currentWorkoutType,
                    TotalRounds = _cfgRounds,
                    RoundsCompleted = _cfgRounds,
                    TotalSeconds = totalSecs,
                    CaloriesBurned = calsBurned,
                    Notes = $"Guardado automático del temporizador"
                };
                StorageService.AddWorkoutSession(session);

                // Mandar a la tarjeta de resumen
                Navigation.PushModalAsync(new WorkoutSummaryPage(session));

                ResetTimer();
                return;
            }
            _currentState = TimerState.Rest;
            _timeLeft = _cfgRest;
            _currentRound++;
            if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundEnd();
            _bellPlayer?.Play();
        }

        ForegroundTimerBridge.Start(_timeLeft, LblStatusTop.Text);
        UpdateUI();
    }

    private void OnResetClicked(object? sender, EventArgs e) => ResetTimer();

    private void ResetTimer()
    {
        _timer.Stop();
        ForegroundTimerBridge.Stop();
        _currentState = TimerState.Idle;
        _currentRound = 1;
        _timeLeft = _cfgWork;
        DeviceDisplay.Current.KeepScreenOn = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        BtnPlayPause.Text = _timer.IsRunning ? "⏸ PAUSA" : "▶ INICIAR";
        LblRound.Text = $"Ronda {_currentRound}/{_cfgRounds}";
        LblTimer.Text = TimeSpan.FromSeconds(_timeLeft).ToString(@"mm\:ss");

        if (_currentState == TimerState.Prep) { LblStatusTop.Text = "PREPARACIÓN"; LblStatusTop.TextColor = Color.FromArgb("#EAB308"); LblTimer.TextColor = Color.FromArgb("#EAB308"); }
        else if (_currentState == TimerState.Work) { LblStatusTop.Text = "¡PELEA!"; LblStatusTop.TextColor = Color.FromArgb("#10B981"); LblTimer.TextColor = Color.FromArgb("#10B981"); }
        else if (_currentState == TimerState.Rest) { LblStatusTop.Text = "DESCANSO"; LblStatusTop.TextColor = Color.FromArgb("#EF4444"); LblTimer.TextColor = Color.FromArgb("#EF4444"); }
        else { LblStatusTop.Text = "LISTO"; LblStatusTop.TextColor = Colors.White; LblTimer.TextColor = Colors.White; }
    }
}