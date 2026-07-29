using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace TuAPP.Platforms.Android;

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

    // El asesino de Zombies: Este ID asegura que solo un reloj corra a la vez
    private int _tickId = 0;

    public static event Action<int, string>? OnTick;
    public static event Action? OnServiceStop;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        switch (intent?.Action)
        {
            case ActionPause:
                _isRunning = false;
                _tickId++; // Destruimos cualquier cuenta regresiva pendiente al instante
                UpdateNotification();
                return StartCommandResult.Sticky;
            case ActionResume:
                _isRunning = true;
                StartTicker();
                UpdateNotification();
                return StartCommandResult.Sticky;
            case ActionStop:
                _tickId++;
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
        _tickId++;
        int currentTickId = _tickId;

        _handler ??= new Handler(Looper.MainLooper!);

        long targetEndTimeMs = SystemClock.ElapsedRealtime() + (_secondsLeft * 1000);

        _tickAction = () =>
        {
            // Seguro anti-zombies: Si se pausó, o si este es un loop viejo, muere aquí.
            if (!_isRunning || _tickId != currentTickId) return;

            long now = SystemClock.ElapsedRealtime();
            long millisRemaining = targetEndTimeMs - now;

            if (millisRemaining <= 0)
            {
                _secondsLeft = 0;
                _isRunning = false;
                OnTick?.Invoke(0, _phaseLabel);
                UpdateNotification();
                return;
            }

            _secondsLeft = (int)Math.Ceiling(millisRemaining / 1000.0);
            OnTick?.Invoke(_secondsLeft, _phaseLabel);
            UpdateNotification();

            if (_isRunning && _tickId == currentTickId)
            {
                long delay = millisRemaining % 1000;
                if (delay < 10) delay = 1000;
                _handler.PostDelayed(_tickAction!, delay);
            }
        };

        _handler.Post(_tickAction);
    }

    private Notification BuildNotification()
    {
        var min = _secondsLeft / 60;
        var sec = _secondsLeft % 60;
        var openIntent = new Intent(this, typeof(MainActivity));
        openIntent.SetFlags(ActivityFlags.SingleTop);
        var openPending = PendingIntent.GetActivity(this, 0, openIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var toggleAction = _isRunning ? ActionPause : ActionResume;
        var toggleLabel = _isRunning ? "Pausar" : "Reanudar";

        int toggleIcon = _isRunning
            ? global::Android.Resource.Drawable.IcMediaPause
            : global::Android.Resource.Drawable.IcMediaPlay;

        var toggleIntent = new Intent(this, typeof(TimerForegroundService));
        toggleIntent.SetAction(toggleAction);
        var togglePending = PendingIntent.GetService(this, 1, toggleIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var stopIntent = new Intent(this, typeof(TimerForegroundService));
        stopIntent.SetAction(ActionStop);
        var stopPending = PendingIntent.GetService(this, 2, stopIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(_phaseLabel)
            .SetContentText($"{min:D2}:{sec:D2}")
            .SetSmallIcon(global::TuAPP.Resource.Mipmap.appicon)
            .SetContentIntent(openPending)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .AddAction(toggleIcon, toggleLabel, togglePending)
            .AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "Detener", stopPending)
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

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        base.OnTaskRemoved(rootIntent);
        _isRunning = false;
        StopForeground(StopForegroundFlags.Remove);
        StopSelf();
    }
}