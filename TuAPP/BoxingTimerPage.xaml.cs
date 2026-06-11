using Microsoft.Maui.Dispatching;
using Plugin.Maui.Audio; // Necesario para el reproductor

namespace TuAPP;

public partial class BoxingTimerPage : ContentPage
{
    private enum TimerState { Idle, Prep, Work, Rest }
    private IDispatcherTimer _timer;
    private TimerState _currentState = TimerState.Idle;
    private int _cfgRounds, _cfgWork, _cfgRest, _cfgPrep;
    private int _currentRound = 1, _timeLeft;
    private bool _useSound, _useVibration, _keepScreenOn;

    private IAudioPlayer? _bellPlayer; // Variable del reproductor

    public BoxingTimerPage()
    {
        InitializeComponent();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTicked;

        // Cargar el audio en la memoria al abrir la app
        LoadAudio();
    }

    private async void LoadAudio()
    {
        try
        {
            var audioFile = await FileSystem.OpenAppPackageFileAsync("bell.mp3");
            _bellPlayer = AudioManager.Current.CreatePlayer(audioFile);
        }
        catch
        {
            // Si el archivo bell.mp3 no existe o hay error, no crashea la app
        }
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
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    private void SafeVibrate(int ms) { if (_useVibration) { try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); } catch { } } }
    private void SafeSpeak(string text) { if (_useSound) { try { Task.Run(async () => await TextToSpeech.SpeakAsync(text)); } catch { } } }

    // Método para reproducir la campana real
    private void PlayBell() { if (_useSound && _bellPlayer != null) _bellPlayer.Play(); }

    private void ResetTimer()
    {
        _timer.Stop();
        _currentState = TimerState.Idle;
        _currentRound = 1;
        _timeLeft = _cfgWork;
        DeviceDisplay.Current.KeepScreenOn = false;
        UpdateUI();
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_timer.IsRunning)
        {
            _timer.Stop();
            DeviceDisplay.Current.KeepScreenOn = false;
        }
        else
        {
            if (_currentState == TimerState.Idle)
            {
                _currentState = TimerState.Prep;
                _timeLeft = _cfgPrep > 0 ? _cfgPrep : _cfgWork;
                if (_cfgPrep == 0) _currentState = TimerState.Work;

                if (_currentState == TimerState.Prep) SafeSpeak("Preparación"); // TTS solo para preparación
            }
            _timer.Start();
            if (_keepScreenOn) DeviceDisplay.Current.KeepScreenOn = true;
        }
        UpdateUI();
    }

    private void OnSkipClicked(object? sender, EventArgs e) { if (_currentState != TimerState.Idle) AdvanceState(); }
    private void OnResetClicked(object? sender, EventArgs e) => ResetTimer();

    private void OnTimerTicked(object? sender, EventArgs e)
    {
        if (_timeLeft > 0)
        {
            _timeLeft--;
            // Mantengo el aviso de los 10 segundos en voz y vibración corta porque es útil
            if (_timeLeft == 10 && _currentState == TimerState.Work) { SafeSpeak("Diez"); SafeVibrate(200); }
            UpdateUI();
        }
        else AdvanceState();
    }

    private void AdvanceState()
    {
        if (_currentState == TimerState.Prep || _currentState == TimerState.Rest)
        {
            _currentState = TimerState.Work;
            _timeLeft = _cfgWork > 0 ? _cfgWork : 1;
            SafeVibrate(800);
            PlayBell(); // Toca la campana al iniciar el round
        }
        else if (_currentState == TimerState.Work)
        {
            if (_currentRound >= _cfgRounds)
            {
                _timer.Stop(); _currentState = TimerState.Idle;
                PlayBell(); // Toca la campana al finalizar el entrenamiento
                SafeSpeak("Entrenamiento completado");
                ResetTimer(); return;
            }
            _currentState = TimerState.Rest;
            _timeLeft = _cfgRest > 0 ? _cfgRest : 1;
            _currentRound++;
            SafeVibrate(800);
            PlayBell(); // Toca la campana al terminar el round (inicio de descanso)
        }
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