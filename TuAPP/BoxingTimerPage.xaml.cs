using Plugin.Maui.Audio;
using TuAPP.Services;

namespace TuAPP;

public partial class BoxingTimerPage : ContentPage
{
    private enum TimerState { Idle, Prep, Work, Rest }
    private TimerState _currentState = TimerState.Idle;
    private int _cfgRounds, _cfgWork, _cfgRest, _cfgPrep;
    private int _currentRound = 1, _timeLeft, _currentMaxTime;
    private bool _isTimerRunning = false;

    private readonly Dictionary<string, IAudioPlayer> _audioPlayers = new();
    private DateTime _lastTransitionTime = DateTime.MinValue;

    public BoxingTimerPage()
    {
        InitializeComponent();
        ForegroundTimerBridge.Initialize();
        LoadAllAudioFiles();
    }

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

    private void PlayConfiguredSound(string preferenceKey, string defaultSoundName)
    {
        if (!Preferences.Get("UseSound", true)) return;

        string selectedSound = Preferences.Get(preferenceKey, defaultSoundName);

        if (!_audioPlayers.ContainsKey(selectedSound))
        {
            selectedSound = defaultSoundName;
            Preferences.Set(preferenceKey, defaultSoundName);
        }

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
            if (!_isTimerRunning)
            {
                _isTimerRunning = true;
                UpdateUI();
            }

            if (secondsLeft <= 0)
            {
                AdvanceState();
            }
            else
            {
                _timeLeft = secondsLeft;

                if (_timeLeft == 10 && _currentState == TimerState.Work)
                {
                    PlayConfiguredSound("Sound10Sec", "alarma_6.mp3");
                }

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

                ForegroundTimerBridge.Start(_timeLeft, GetPhaseName(_currentState));

                PlayConfiguredSound("SoundPrep", "alarma_3.mp3");
            }
            else
            {
                ForegroundTimerBridge.Resume();
            }

            if (Preferences.Get("KeepScreenOn", true))
                DeviceDisplay.Current.KeepScreenOn = true;
        }
        UpdateUI();
    }

    private void AdvanceState()
    {
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
                PlayConfiguredSound("SoundRoundStart", "bell.mp3");
            }
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                // EL CRONÓMETRO TERMINÓ: Solo detenemos todo, sonamos la campana y reiniciamos
                _isTimerRunning = false;
                ForegroundTimerBridge.Stop();
                DeviceDisplay.Current.KeepScreenOn = false;

                PlayConfiguredSound("SoundRoundEnd", "bell.mp3");

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
                PlayConfiguredSound("SoundRoundEnd", "bell.mp3");
            }
        }

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