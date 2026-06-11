using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace TuAPP.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class TrainingReminderReceiver : BroadcastReceiver
{
    private const string ChannelId = "training_reminder";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;

        var name = "Boxeador";
        var msg = "¡Es hora de subir al ring! No pierdas el ritmo hoy. 🥊";

        CreateChannel(context);

        var openIntent = new Intent(context, typeof(MainActivity));
        var pending = PendingIntent.GetActivity(context, 0, openIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notif = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle("BoxingTimer 🥊")
            .SetContentText(msg)
            .SetSmallIcon(Resource.Drawable.dotnet_bot)
            .SetContentIntent(pending)
            .SetAutoCancel(true)
            .Build();

        NotificationManagerCompat.From(context).Notify(2001, notif);
    }

    private void CreateChannel(Context context)
    {
        if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "Recordatorios", NotificationImportance.Default);
        var nm = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        nm?.CreateNotificationChannel(channel);
    }
}