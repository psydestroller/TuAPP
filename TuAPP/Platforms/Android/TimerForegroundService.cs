using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace TuAPP.Platforms.Android;

// SOLUCIÓN AL ERROR CS0234: Usar la ruta global absoluta de Android
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSpecialUse)]
public class TimerForegroundService : Service
{
    public const string ChannelId = "boxing_timer_channel";
    public const string ActionPause = "ACTION_PAUSE";
    public const string ActionResume = "ACTION_RESUME";
    public const string ActionStop = "ACTION_STOP";
    public const int NotifId = 1001;

    private Handler? _handler;
    private Action? _tickAction;
    private int _secondsLeft;
    private bool _isRunning;
    private string _phaseLabel = "PREPARACIÓN";

    public static event Action<int, string>? OnTick;
    public static event Action? OnServiceStop;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        switch (intent?.Action)
        {
            case ActionPause:
                _isRunning = false;
                _handler?.RemoveCallbacks(_tickAction!);
                UpdateNotification();
                return StartCommandResult.Sticky;
            case ActionResume:
                _isRunning = true;
                StartTicker();
                UpdateNotification();
                return StartCommandResult.Sticky;
            case ActionStop:
                StopForeground(StopForegroundFlags.Remove);
                StopSelf();
                OnServiceStop?.Invoke();
                return StartCommandResult.NotSticky;
        }

        _secondsLeft = intent?.GetIntExtra("seconds_left", 180) ?? 180;
        _phaseLabel = intent?.GetStringExtra("phase_label") ?? "PELEA";
        _isRunning = true;

        CreateNotificationChannel();
        StartForeground(NotifId, BuildNotification());
        StartTicker();
        return StartCommandResult.Sticky;
    }

    private void StartTicker()
    {
        if (_handler != null && _tickAction != null)
            _handler.RemoveCallbacks(_tickAction);

        _handler = new Handler(Looper.MainLooper!);
        _tickAction = () =>
        {
            if (!_isRunning) return;
            _secondsLeft--;
            OnTick?.Invoke(_secondsLeft, _phaseLabel);
            UpdateNotification();
            if (_secondsLeft > 0)
                _handler.PostDelayed(_tickAction!, 1000);
        };
        _handler.PostDelayed(_tickAction, 1000);
    }

    private Notification BuildNotification()
    {
        var min = _secondsLeft / 60;
        var sec = _secondsLeft % 60;
        var openIntent = new Intent(this, typeof(MainActivity));
        openIntent.SetFlags(ActivityFlags.SingleTop);
        var openPending = PendingIntent.GetActivity(this, 0, openIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var toggleAction = _isRunning ? ActionPause : ActionResume;
        var toggleLabel = _isRunning ? "⏸ Pausar" : "▶ Reanudar";
        var toggleIntent = new Intent(this, typeof(TimerForegroundService));
        toggleIntent.SetAction(toggleAction);
        var togglePending = PendingIntent.GetService(this, 1, toggleIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var stopIntent = new Intent(this, typeof(TimerForegroundService));
        stopIntent.SetAction(ActionStop);
        var stopPending = PendingIntent.GetService(this, 2, stopIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle($"🥊 {_phaseLabel}")
            .SetContentText($"{min:D2}:{sec:D2}")
            .SetSmallIcon(Resource.Drawable.dotnet_bot)
            .SetContentIntent(openPending)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .AddAction(0, toggleLabel, togglePending)
            .AddAction(0, "⏹ Detener", stopPending)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .Build();
    }

    private void UpdateNotification()
    {
        var nm = (NotificationManager?)GetSystemService(NotificationService);
        nm?.Notify(NotifId, BuildNotification());
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "Boxing Timer", NotificationImportance.Low);
        channel.SetShowBadge(false);
        var nm = (NotificationManager?)GetSystemService(NotificationService);
        nm?.CreateNotificationChannel(channel);
    }
}