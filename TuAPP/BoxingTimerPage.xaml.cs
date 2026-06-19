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
    private int _currentRound = 1, _timeLeft, _currentMaxTime;
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
        catch { }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _cfgRounds = Preferences.Get("BoxingRounds", 12);
        _cfgWork = Preferences.Get("BoxingWork", 180);
        _cfgRest = Preferences.Get("BoxingRest", 30);
        _cfgPrep = Preferences.Get("BoxingPrep", 10);

        LblWorkConfig.Text = TimeSpan.FromSeconds(_cfgWork).ToString(@"mm\:ss");
        LblRestConfig.Text = TimeSpan.FromSeconds(_cfgRest).ToString(@"mm\:ss");

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
                _currentMaxTime = _timeLeft;
                if (_cfgPrep == 0) _currentState = TimerState.Work;
            }
            _timer.Start();
            ForegroundTimerBridge.Start(_timeLeft, LblStatusTop.Text);

            if (Preferences.Get("KeepScreenOn", true))
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

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        if (_currentState != TimerState.Idle) AdvanceState();
    }

    private void AdvanceState()
    {
        if (_currentState == TimerState.Prep || _currentState == TimerState.Rest)
        {
            _currentState = TimerState.Work;
            _timeLeft = _cfgWork;
            _currentMaxTime = _cfgWork;

            if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundStart();
            if (Preferences.Get("UseBell", true)) _bellPlayer?.Play();
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                _timer.Stop();
                ForegroundTimerBridge.Stop();
                DeviceDisplay.Current.KeepScreenOn = false;

                if (Preferences.Get("UseBell", true)) _bellPlayer?.Play();

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
                    RoundsCompleted = _cfgRounds,
                    TotalSeconds = totalSecs,
                    CaloriesBurned = calsBurned,
                    Notes = "Guardado automático"
                };
                StorageService.AddWorkoutSession(session);

                Navigation.PushModalAsync(new WorkoutSummaryPage(session));
                ResetTimer();
                return;
            }
            _currentState = TimerState.Rest;
            _timeLeft = _cfgRest;
            _currentMaxTime = _cfgRest;
            _currentRound++;

            if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundEnd();
            if (Preferences.Get("UseBell", true)) _bellPlayer?.Play();
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
        _currentMaxTime = _cfgWork;
        DeviceDisplay.Current.KeepScreenOn = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        BtnPlayPause.Text = _timer.IsRunning ? "⏸" : "▶";
        LblRound.Text = $"Ronda {_currentRound}/{_cfgRounds}";
        LblTimer.Text = TimeSpan.FromSeconds(_timeLeft).ToString(@"mm\:ss");

        if (_currentState == TimerState.Prep) { LblStatusTop.Text = "PREPARACIÓN"; LblStatusTop.TextColor = Color.FromArgb("#EAB308"); LblTimer.TextColor = Color.FromArgb("#EAB308"); }
        else if (_currentState == TimerState.Work) { LblStatusTop.Text = "¡PELEA!"; LblStatusTop.TextColor = Color.FromArgb("#10B981"); LblTimer.TextColor = Color.FromArgb("#10B981"); }
        else if (_currentState == TimerState.Rest) { LblStatusTop.Text = "DESCANSO"; LblStatusTop.TextColor = Color.FromArgb("#EF4444"); LblTimer.TextColor = Color.FromArgb("#EF4444"); }
        else { LblStatusTop.Text = "LISTO"; LblStatusTop.TextColor = Colors.White; LblTimer.TextColor = Colors.White; }

        if (TimerGraphicsView.Drawable is CircularProgressDrawable drawable)
        {
            drawable.Progress = _currentMaxTime > 0 ? (double)_timeLeft / _currentMaxTime : 0;
            drawable.ProgressColor = LblStatusTop.TextColor;
            TimerGraphicsView.Invalidate();
        }
    }
}