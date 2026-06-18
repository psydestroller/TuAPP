namespace TuAPP.Services;

public static class NotificationService
{
    public static void ScheduleDailyReminder(TimeSpan time)
    {
#if ANDROID
        var ctx = Android.App.Application.Context;
        var intent = new Android.Content.Intent(ctx, typeof(TuAPP.Platforms.Android.TrainingReminderReceiver));

        var flags = OperatingSystem.IsAndroidVersionAtLeast(23)
            ? Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable
            : Android.App.PendingIntentFlags.UpdateCurrent;

        var pending = Android.App.PendingIntent.GetBroadcast(ctx, 100, intent, flags);
        if (pending == null) return;

        var am = (Android.App.AlarmManager?)ctx.GetSystemService(Android.Content.Context.AlarmService);
        var trigger = DateTime.Today.Add(time);

        if (trigger < DateTime.Now) trigger = trigger.AddDays(1);

        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            am?.SetExactAndAllowWhileIdle(Android.App.AlarmType.RtcWakeup, new DateTimeOffset(trigger).ToUnixTimeMilliseconds(), pending);
        }
        else
        {
            am?.SetExact(Android.App.AlarmType.RtcWakeup, new DateTimeOffset(trigger).ToUnixTimeMilliseconds(), pending);
        }
#endif
    }
}