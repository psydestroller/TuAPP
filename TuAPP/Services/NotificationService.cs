namespace TuAPP.Services;

public static class NotificationService
{
    public static async Task RequestPermissionAsync()
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
#endif
    }

    public static void ScheduleDailyReminder(TimeSpan time)
    {
#if ANDROID
        var ctx = Android.App.Application.Context;
        var intent = new Android.Content.Intent(ctx, typeof(TuAPP.Platforms.Android.TrainingReminderReceiver));
        var pending = Android.App.PendingIntent.GetBroadcast(ctx, 100, intent, Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);
        var am = (Android.App.AlarmManager?)ctx.GetSystemService(Android.Content.Context.AlarmService);

        var triggerTime = DateTime.Today.Add(time);
        if (triggerTime <= DateTime.Now) triggerTime = triggerTime.AddDays(1);

        am?.SetExactAndAllowWhileIdle(Android.App.AlarmType.RtcWakeup, new DateTimeOffset(triggerTime).ToUnixTimeMilliseconds(), pending);
#endif
    }
}