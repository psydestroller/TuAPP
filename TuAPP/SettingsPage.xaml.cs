using TuAPP.Services;

namespace TuAPP;

public partial class SettingsPage : ContentPage
{
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadPickers();
        LoadPreferences();
        _isInitializing = false;
    }

    private void LoadPickers()
    {
        var rounds = Enumerable.Range(1, 50).Select(i => i.ToString()).ToList();
        var times = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();

        PckRounds.ItemsSource = rounds;
        PckWorkMin.ItemsSource = times; PckWorkSec.ItemsSource = times;
        PckRestMin.ItemsSource = times; PckRestSec.ItemsSource = times;
        PckPrepMin.ItemsSource = times; PckPrepSec.ItemsSource = times;
    }

    private void LoadPreferences()
    {
        PckRounds.SelectedIndex = Preferences.Get("BoxingRounds", 12) - 1;
        int w = Preferences.Get("BoxingWork", 180); PckWorkMin.SelectedIndex = w / 60; PckWorkSec.SelectedIndex = w % 60;
        int r = Preferences.Get("BoxingRest", 30); PckRestMin.SelectedIndex = r / 60; PckRestSec.SelectedIndex = r % 60;
        int p = Preferences.Get("BoxingPrep", 10); PckPrepMin.SelectedIndex = p / 60; PckPrepSec.SelectedIndex = p % 60;

        SwSound.IsToggled = Preferences.Get("UseSound", true);
        SwVib.IsToggled = Preferences.Get("UseVibration", true);
        SwScreen.IsToggled = Preferences.Get("KeepScreenOn", true);

        string savedReminder = Preferences.Get("SavedReminderTime", "17:00:00");
        if (TimeSpan.TryParse(savedReminder, out TimeSpan ts)) TpReminder.Time = ts;
        else TpReminder.Time = new TimeSpan(17, 0, 0);
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (_isInitializing) return;
        Preferences.Set("BoxingRounds", PckRounds.SelectedIndex + 1);
        Preferences.Set("BoxingWork", (PckWorkMin.SelectedIndex * 60) + PckWorkSec.SelectedIndex);
        Preferences.Set("BoxingRest", (PckRestMin.SelectedIndex * 60) + PckRestSec.SelectedIndex);
        Preferences.Set("BoxingPrep", (PckPrepMin.SelectedIndex * 60) + PckPrepSec.SelectedIndex);
    }

    private void OnTogglesChanged(object? sender, ToggledEventArgs e)
    {
        if (_isInitializing) return;
        Preferences.Set("UseSound", SwSound.IsToggled);
        Preferences.Set("UseVibration", SwVib.IsToggled);
        Preferences.Set("KeepScreenOn", SwScreen.IsToggled);
    }

    private async void OnSaveReminder(object? sender, EventArgs e)
    {
        if (TpReminder != null)
        {
            TimeSpan safeTime = TpReminder.Time is TimeSpan ts ? ts : new TimeSpan(17, 0, 0);

            Preferences.Set("SavedReminderTime", safeTime.ToString());

            await NotificationService.RequestAndScheduleAsync(safeTime);
            await DisplayAlert("Recordatorio Activado", "Te avisaremos 30 minutos antes para que prepares tus vendas.", "OK");
        }
    }

    // FIX 3 COMPLETADO: Aniquila las alarmas en Android y limpia la memoria
    private void OnCancelReminder(object? sender, EventArgs e)
    {
#if ANDROID
        TuAPP.Platforms.Android.AlarmScheduler.CancelDaily(101);
        TuAPP.Platforms.Android.AlarmScheduler.CancelDaily(102);
#endif

        Preferences.Remove("SavedReminderTime");
        DisplayAlert("Recordatorio Desactivado", "Ya no recibirás avisos diarios de entrenamiento en segundo plano.", "OK");
    }
}