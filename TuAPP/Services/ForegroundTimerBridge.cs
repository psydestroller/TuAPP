namespace TuAPP.Services;

public static class ForegroundTimerBridge
{
    public static event Action<int, string>? OnTick;
    public static event Action? OnServiceStopped;

    public static void Start(int secondsLeft, string phaseLabel)
    {
#if ANDROID
        var ctx = Android.App.Application.Context;
        var intent = new Android.Content.Intent(ctx, typeof(TuAPP.Platforms.Android.TimerForegroundService));
        intent.PutExtra("seconds_left", secondsLeft);
        intent.PutExtra("phase_label", phaseLabel);

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            ctx.StartForegroundService(intent);
        else
            ctx.StartService(intent);

        TuAPP.Platforms.Android.TimerForegroundService.OnTick += RaiseOnTick;
        TuAPP.Platforms.Android.TimerForegroundService.OnServiceStop += RaiseOnStop;
#endif
    }

    public static void Stop()
    {
#if ANDROID
        TuAPP.Platforms.Android.TimerForegroundService.OnTick -= RaiseOnTick;
        TuAPP.Platforms.Android.TimerForegroundService.OnServiceStop -= RaiseOnStop;

        var ctx = Android.App.Application.Context;
        var intent = new Android.Content.Intent(ctx, typeof(TuAPP.Platforms.Android.TimerForegroundService));
        intent.SetAction("ACTION_STOP");
        ctx.StartService(intent);
#endif
    }

    private static void RaiseOnTick(int s, string p) => OnTick?.Invoke(s, p);
    private static void RaiseOnStop() => OnServiceStopped?.Invoke();
}