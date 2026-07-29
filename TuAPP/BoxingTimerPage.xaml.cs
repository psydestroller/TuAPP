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
    private bool _isSaving = false;

    // GESTOR DE AUDIO DINÁMICO: Almacena los 7 sonidos listos en memoria
    private readonly Dictionary<string, IAudioPlayer> _audioPlayers = new();

    // Candado Anti-Glitch para evitar que se bugee si skipeas muy rápido
    private DateTime _lastTransitionTime = DateTime.MinValue;
    private DateTime _phaseEndTime;

    public BoxingTimerPage()
    {
        InitializeComponent();
        ForegroundTimerBridge.Initialize();
        LoadAllAudioFiles();
    }

    // 1. CARGA AUTOMÁTICA DE TODOS LOS SONIDOS (Campana + Alarmas 1 a 6)
    private async void LoadAllAudioFiles()
    {
        string[] soundFiles =
        {
            "bell.mp3",
            "alarma_1.mp3",
            "alarma_2.mp3",
            "alarma_3.mp3",
            "alarma_4.mp3",
            "alarma_5.mp3",
            "alarma_6.mp3"
        };

        foreach (var sound in soundFiles)
        {
            try
            {
                var file = await FileSystem.OpenAppPackageFileAsync(sound);
                var player = AudioManager.Current.CreatePlayer(file);
                _audioPlayers[sound] = player;
            }
            catch { }
        }
    }

    // 2. REPRODUCCIÓN POR PREFERENCIAS (Con sonido por defecto si no han configurado nada)
    private void PlayConfiguredSound(string preferenceKey, string defaultSoundName)
    {
        if (!Preferences.Get("UseSound", true)) return;

        string selectedSound = Preferences.Get(preferenceKey, defaultSoundName);

        if (_audioPlayers.TryGetValue(selectedSound, out var player))
        {
            player.Play();
        }
    }

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

    private void HandleServiceTick(int secondsLeft, string phase)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_isTimerRunning) return;

            int atomicSecondsLeft = (int)Math.Ceiling((_phaseEndTime - DateTime.Now).TotalSeconds);

            if (atomicSecondsLeft <= 0)
            {
                AdvanceState();
            }
            else
            {
                _timeLeft = atomicSecondsLeft;

                // =====================================================================
                // ALERTA 3: ÚLTIMOS 10 SEGUNDOS DEL ROUND (Default: alarma_2.mp3)
                // =====================================================================
                if (_timeLeft == 10 && _currentState == TimerState.Work)
                {
                    PlayConfiguredSound("Sound10Sec", "alarma_2.mp3");
                }
                // =====================================================================

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

                // =====================================================================
                // ALERTA 1: INICIO DE PREPARACIÓN (Default: alarma_1.mp3)
                // =====================================================================
                PlayConfiguredSound("SoundPrep", "alarma_1.mp3");
                // =====================================================================
            }
            else
            {
                _phaseEndTime = DateTime.Now.AddSeconds(_timeLeft);
                ForegroundTimerBridge.Resume();
            }

            if (Preferences.Get("KeepScreenOn", true))
                DeviceDisplay.Current.KeepScreenOn = true;
        }
        UpdateUI();
    }

    private void AdvanceState()
    {
        // CANDADO MILISEGUNDOS: Si presionaste Skip hace menos de 800ms, ignora el comando
        if ((DateTime.Now - _lastTransitionTime).TotalMilliseconds < 800) return;
        _lastTransitionTime = DateTime.Now;

        if (_currentState == TimerState.Prep || _currentState == TimerState.Rest)
        {
            _currentState = TimerState.Work;
            _timeLeft = _cfgWork;
            _currentMaxTime = _cfgWork;

            if (_isTimerRunning)
            {
                if (Preferences.Get("UseVibration", true)) AudioAndHapticService.VibrateRoundStart();

                // =====================================================================
                // ALERTA 2: INICIO DE ROUND / ¡PELEA! (Default: bell.mp3)
                // =====================================================================
                PlayConfiguredSound("SoundRoundStart", "bell.mp3");
                // =====================================================================
            }
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                _isTimerRunning = false;
                ForegroundTimerBridge.Stop();
                DeviceDisplay.Current.KeepScreenOn = false;

                PlayConfiguredSound("SoundRoundEnd", "bell.mp3");

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

                // =====================================================================
                // ALERTA 4: FIN DE ROUND / DESCANSAR (Default: bell.mp3)
                // =====================================================================
                PlayConfiguredSound("SoundRoundEnd", "bell.mp3");
                // =====================================================================
            }
        }

        _phaseEndTime = DateTime.Now.AddSeconds(_timeLeft);

        if (_isTimerRunning)
        {
            ForegroundTimerBridge.Start(_timeLeft, GetPhaseName(_currentState));
        }
        else
        {
            ForegroundTimerBridge.Stop();
        }

        UpdateUI();
    }

    private void SaveWorkout()
    {
        if (_isSaving) return;
        _isSaving = true;

        string selected = PckWorkoutType.SelectedItem?.ToString() ?? "Classic boxing";
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
            RoundsCompleted = _currentRound,
            TotalSeconds = totalSecs,
            CaloriesBurned = calsBurned,
            Notes = "Guardado automático"
        };
        StorageService.AddWorkoutSession(session);

        Navigation.PushModalAsync(new WorkoutSummaryPage(session));
        _isSaving = false;
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

        if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
            DeviceDisplay.Current.KeepScreenOn = false;

        UpdateUI();
    }

    private void UpdateUI()
    {
        BtnPlayPause.Source = _isTimerRunning ? "ic_pause.png" : "ic_play.png";
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