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
    private bool _useSound, _useVibration, _keepScreenOn;

    private IAudioPlayer? _bellPlayer;

    public BoxingTimerPage()
    {
        InitializeComponent();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTicked;
        LoadAudio();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_currentState != TimerState.Idle)
        {
            return true; // Bloquea la salida si el timer está activo
        }
        return base.OnBackButtonPressed();
    }

    private void ToggleScreenLock(bool isLocked)
    {
        Shell.SetTabBarIsVisible(this, !isLocked);
        DeviceDisplay.Current.KeepScreenOn = isLocked && _keepScreenOn;
    }

    private async void LoadAudio()
    {
        try
        {
            var audioFile = await FileSystem.OpenAppPackageFileAsync("bell.mp3");
            _bellPlayer = AudioManager.Current.CreatePlayer(audioFile);
        }
        catch { /* Falla silenciosa y segura */ }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _cfgRounds = Preferences.Get("BoxingRounds", 12);
        _cfgWork = Preferences.Get("BoxingWork", 180);
        _cfgRest = Preferences.Get("BoxingRest", 30);
        _cfgPrep = Preferences.Get("BoxingPrep", 10);
        _useSound = Preferences.Get("UseSound", true);
        _useVibration = Preferences.Get("UseVibration", true);
        _keepScreenOn = Preferences.Get("KeepScreenOn", true);

        LblHeaderWork.Text = TimeSpan.FromSeconds(_cfgWork).ToString(@"mm\:ss");
        LblHeaderRest.Text = TimeSpan.FromSeconds(_cfgRest).ToString(@"mm\:ss");

        if (_currentState == TimerState.Idle) ResetTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ToggleScreenLock(false);
    }

    private void SafeSpeak(string text) { if (_useSound) { try { Task.Run(async () => await TextToSpeech.SpeakAsync(text)); } catch { } } }
    private void PlayBell() { if (_useSound && _bellPlayer != null) { try { _bellPlayer.Play(); } catch { } } }

    private void ResetTimer()
    {
        _timer.Stop();
        ForegroundTimerBridge.Stop();
        _currentState = TimerState.Idle;
        _currentRound = 1;
        _timeLeft = _cfgWork;
        ToggleScreenLock(false);
        UpdateUI();
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_timer.IsRunning)
        {
            _timer.Stop();
            ForegroundTimerBridge.Stop();
            ToggleScreenLock(false);
        }
        else
        {
            if (_currentState == TimerState.Idle)
            {
                _currentState = TimerState.Prep;
                _timeLeft = _cfgPrep > 0 ? _cfgPrep : _cfgWork;
                if (_cfgPrep == 0) _currentState = TimerState.Work;

                if (_currentState == TimerState.Prep) SafeSpeak("Preparación");
            }

            _timer.Start();
            ForegroundTimerBridge.Start(_timeLeft, LblStatusTop.Text);
            ToggleScreenLock(true);
        }
        UpdateUI();
    }

    private void OnSkipClicked(object? sender, EventArgs e) { if (_currentState != TimerState.Idle) AdvanceState(); }
    private void OnResetClicked(object? sender, EventArgs e) => ResetTimer();

    // AQUÍ ESTÁ EL PRIMER CÓDIGO (Haptics de 10 segundos)
    private void OnTimerTicked(object? sender, EventArgs e)
    {
        if (_timeLeft > 0)
        {
            _timeLeft--;
            if (_timeLeft <= 10 && _timeLeft > 0 && _currentState == TimerState.Work)
            {
                if (_timeLeft == 10) SafeSpeak("Diez");
                if (_useVibration) AudioAndHapticService.VibrateCountdownTick();
            }
            UpdateUI();
        }
        else AdvanceState();
    }

    // AQUÍ ESTÁ EL SEGUNDO CÓDIGO (Haptics de campana y Pantalla de Resumen)
    private void AdvanceState()
    {
        if (_currentState == TimerState.Prep || _currentState == TimerState.Rest)
        {
            _currentState = TimerState.Work;
            _timeLeft = _cfgWork > 0 ? _cfgWork : 1;
            if (_useVibration) AudioAndHapticService.VibrateRoundStart();
            PlayBell();
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                _timer.Stop();
                ForegroundTimerBridge.Stop();
                _currentState = TimerState.Idle;
                if (_useVibration) AudioAndHapticService.VibrateRoundEnd();
                PlayBell();
                SafeSpeak("Entrenamiento completado");

                int totalSecs = (_cfgRounds * _cfgWork) + ((_cfgRounds - 1) * _cfgRest);
                var profile = StorageService.LoadProfile();
                WorkoutType currentWorkoutType = WorkoutType.ClassicBoxing;
                double calsBurned = CalorieCalculator.Calculate(currentWorkoutType, profile.WeightKg, totalSecs);

                var session = new WorkoutSession
                {
                    Type = currentWorkoutType,
                    TotalRounds = _cfgRounds,
                    RoundsCompleted = _cfgRounds,
                    TotalSeconds = totalSecs,
                    CaloriesBurned = calsBurned
                };
                StorageService.AddWorkoutSession(session);

                // Llama a la tarjeta al terminar
                Navigation.PushModalAsync(new WorkoutSummaryPage(session));

                ResetTimer();
                return;
            }
            _currentState = TimerState.Rest;
            _timeLeft = _cfgRest > 0 ? _cfgRest : 1;
            _currentRound++;
            if (_useVibration) AudioAndHapticService.VibrateRoundEnd();
            PlayBell();
        }
        ForegroundTimerBridge.Start(_timeLeft, LblStatusTop.Text);
        UpdateUI();
    }

    private void UpdateUI()
    {
        BtnPlayPause.Text = _timer.IsRunning ? "⏸" : "▶";
        LblRound.Text = $"Ronda {_currentRound}/{_cfgRounds}";
        LblTimer.Text = TimeSpan.FromSeconds(_timeLeft).ToString(@"mm\:ss");

        Color currentColor = Colors.White;
        double currentMaxTime = 1;

        if (_currentState == TimerState.Prep) { LblStatusTop.Text = "PREPARACIÓN"; currentColor = Color.FromArgb("#EAB308"); currentMaxTime = _cfgPrep > 0 ? _cfgPrep : 1; }
        else if (_currentState == TimerState.Work) { LblStatusTop.Text = "¡PELEA!"; currentColor = Color.FromArgb("#10B981"); currentMaxTime = _cfgWork > 0 ? _cfgWork : 1; }
        else if (_currentState == TimerState.Rest) { LblStatusTop.Text = "DESCANSO"; currentColor = Color.FromArgb("#EF4444"); currentMaxTime = _cfgRest > 0 ? _cfgRest : 1; }
        else { LblStatusTop.Text = "LISTO"; currentMaxTime = _cfgWork > 0 ? _cfgWork : 1; }

        LblTimer.TextColor = currentColor; LblStatusTop.TextColor = currentColor;
        TimerDrawable.Progress = currentMaxTime > 0 ? (double)_timeLeft / currentMaxTime : 0;
        TimerDrawable.ProgressColor = currentColor;
        TimerGraphicsView.Invalidate();
    }
}