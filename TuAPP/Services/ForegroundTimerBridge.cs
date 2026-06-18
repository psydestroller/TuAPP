namespace TuAPP.Services;

public static class ForegroundTimerBridge
{
    public static void Start(int seconds, string label)
    {
#if ANDROID
        var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(Platforms.Android.TimerForegroundService));
        intent.PutExtra("seconds", seconds);

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            Android.App.Application.Context.StartForegroundService(intent);
        }
        else
        {
            Android.App.Application.Context.StartService(intent);
        }
#endif
    }
    public static void Stop()
    {
#if ANDROID
        var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(Platforms.Android.TimerForegroundService));
        Android.App.Application.Context.StopService(intent);
#endif
    }
}