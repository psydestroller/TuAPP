namespace TuAPP.Services;

public static class ForegroundTimerBridge
{
    public static event Action<int, string>? OnTick;
    public static event Action? OnServiceStop;

    private static bool _isInitialized = false;

    // Inicializa la escucha de eventos desde el servicio nativo de Android
    public static void Initialize()
    {
        if (_isInitialized) return;
#if ANDROID
        TuAPP.Platforms.Android.TimerForegroundService.OnTick += (sec, phase) => OnTick?.Invoke(sec, phase);
        TuAPP.Platforms.Android.TimerForegroundService.OnServiceStop += () => OnServiceStop?.Invoke();
#endif
        _isInitialized = true;
    }

    public static void Start(int secondsLeft, string phaseLabel)
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var intent = new Android.Content.Intent(context, typeof(TuAPP.Platforms.Android.TimerForegroundService));
        intent.PutExtra("seconds_left", secondsLeft);
        intent.PutExtra("phase_label", phaseLabel);

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
#endif
    }

    public static void Pause()
    {
#if ANDROID
        SendAction(TuAPP.Platforms.Android.TimerForegroundService.ActionPause);
#endif
    }

    public static void Resume()
    {
#if ANDROID
        SendAction(TuAPP.Platforms.Android.TimerForegroundService.ActionResume);
#endif
    }

    public static void Stop()
    {
#if ANDROID
        SendAction(TuAPP.Platforms.Android.TimerForegroundService.ActionStop);
#endif
    }

#if ANDROID
    private static void SendAction(string action)
    {
        var context = Android.App.Application.Context;
        var intent = new Android.Content.Intent(context, typeof(TuAPP.Platforms.Android.TimerForegroundService));
        intent.SetAction(action);
        context.StartService(intent);
    }
#endif
}