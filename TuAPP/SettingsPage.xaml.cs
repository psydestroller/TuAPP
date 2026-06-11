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
}