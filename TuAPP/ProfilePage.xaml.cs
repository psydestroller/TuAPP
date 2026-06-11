namespace TuAPP;

public partial class ProfilePage : ContentPage
{
    public ProfilePage() { InitializeComponent(); }

    protected override void OnAppearing() { base.OnAppearing(); LoadData(); }

    private void LoadData()
    {
        string name = Preferences.Get("AthleteName", "Juan Deportista");
        string age = Preferences.Get("AthleteAge", "25");
        string height = Preferences.Get("AthleteHeight", "175");
        string weight = Preferences.Get("AthleteWeight", "70");

        LblName.Text = name;
        if (name.Length > 1) LblInitials.Text = name.Substring(0, 2).ToUpper();
        LblAge.Text = age; LblHeight.Text = $"{height} cm"; LblWeight.Text = $"{weight} kg";

        if (double.TryParse(weight, out double w) && double.TryParse(height, out double h))
        {
            double imc = w / ((h / 100) * (h / 100));
            LblImc.Text = imc.ToString("F1");
            LblCatTop.Text = $"Categoría: {(w * 2.20462 <= 147 ? "Welter" : "Ligero")}"; // Lógica simplificada de ej.
        }
    }

    private void OnToggleWeight(object? sender, EventArgs e) { FormWeight.IsVisible = !FormWeight.IsVisible; }
    private void OnToggleEdit(object? sender, EventArgs e)
    {
        EntryName.Text = Preferences.Get("AthleteName", "Juan Deportista");
        EntryAge.Text = Preferences.Get("AthleteAge", "25");
        EntryHeight.Text = Preferences.Get("AthleteHeight", "175");
        FormEdit.IsVisible = !FormEdit.IsVisible;
    }

    private void OnSaveWeight(object? sender, EventArgs e)
    {
        Preferences.Set("AthleteWeight", EntryNewWeight.Text);
        FormWeight.IsVisible = false; LoadData();
    }

    private void OnSaveProfile(object? sender, EventArgs e)
    {
        Preferences.Set("AthleteName", EntryName.Text);
        Preferences.Set("AthleteAge", EntryAge.Text);
        Preferences.Set("AthleteHeight", EntryHeight.Text);
        FormEdit.IsVisible = false; LoadData();
    }
}