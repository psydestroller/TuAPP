using Android.App;
using Android.Content;
using Android.OS;
using Application = Android.App.Application;

namespace TuAPP.Platforms.Android;

public static class AlarmScheduler
{
    public static void ScheduleDaily(TimeSpan triggerTime, int requestCode, string title, string message)
    {
        var context = Application.Context;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);

        if (alarmManager == null) return;

        var intent = new Intent(context, typeof(TrainingReminderReceiver));
        intent.PutExtra("NOTIF_TITLE", title);
        intent.PutExtra("NOTIF_MESSAGE", message);
        intent.PutExtra("NOTIF_ID", requestCode);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            requestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        var now = DateTime.Now;
        var nextTrigger = new DateTime(now.Year, now.Month, now.Day, triggerTime.Hours, triggerTime.Minutes, 0);
        if (now > nextTrigger) nextTrigger = nextTrigger.AddDays(1);
        long timeInMillis = new DateTimeOffset(nextTrigger).ToUnixTimeMilliseconds();

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S && !alarmManager.CanScheduleExactAlarms())
                alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, timeInMillis, pendingIntent);
            else
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, timeInMillis, pendingIntent);
        }
        catch
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, timeInMillis, pendingIntent);
        }
    }

    // ── MÉTODO NUEVO ──────────────────────────────────────────
    public static void CancelDaily(int requestCode)
    {
        var context = Application.Context;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);

        if (alarmManager == null) return;

        // Reconstruir el intent EXACTAMENTE igual que en ScheduleDaily
        // (mismo tipo de receiver, mismo requestCode)
        var intent = new Intent(context, typeof(TrainingReminderReceiver));

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            requestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        if (pendingIntent == null) return;

        alarmManager.Cancel(pendingIntent);
        pendingIntent.Cancel();
    }
}