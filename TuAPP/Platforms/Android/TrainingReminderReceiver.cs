using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace TuAPP.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
public class TrainingReminderReceiver : BroadcastReceiver
{
    private const string ChannelId = "training_reminder";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;

        // EXTRAEMOS LO QUE EMPACÓ EL SCHEDULER (o ponemos un texto de auxilio si algo falla)
        string title = intent?.GetStringExtra("NOTIF_TITLE") ?? "Campamento 🥊";
        string msg = intent?.GetStringExtra("NOTIF_MESSAGE") ?? "¡Es hora de entrenar!";
        int notificationId = intent?.GetIntExtra("NOTIF_ID", 2001) ?? 2001;

        CreateChannel(context);

        var openIntent = new Intent(context, typeof(MainActivity));
        var pending = PendingIntent.GetActivity(
            context,
            notificationId, // El mismo ID para que al tocar la notificación abra la app correctamente
            openIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        var notif = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle(title)
            .SetContentText(msg)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(msg)) // Permite textos largos sin que se pongan "..."
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo) // Icono del sistema Android (Garantía de compilación 100%)
            .SetContentIntent(pending)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .Build();

        // Lanzamos la notificación usando su ID único (101 o 102)
        NotificationManagerCompat.From(context).Notify(notificationId, notif);
    }

    private void CreateChannel(Context context)
    {
        if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "Recordatorios de Campamento", NotificationImportance.High);
        var nm = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        nm?.CreateNotificationChannel(channel);
    }
}