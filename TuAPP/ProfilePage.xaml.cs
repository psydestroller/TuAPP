using System.Globalization;
using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUI();
    }

    private void UpdateUI()
    {
        var profile = StorageService.LoadProfile();

        LblName.Text = profile.Name;
        LblAge.Text = profile.Age.ToString();
        LblWeight.Text = $"{profile.WeightKg.ToString("0.##", CultureInfo.InvariantCulture)} kg";
        LblHeight.Text = $"{profile.HeightCm.ToString("0.##", CultureInfo.InvariantCulture)} cm";

        LblInitials.Text = GetInitials(profile.Name);

        double heightM = profile.HeightCm / 100.0;
        if (heightM > 0)
        {
            double imc = profile.WeightKg / (heightM * heightM);
            LblImc.Text = imc.ToString("0.1", CultureInfo.InvariantCulture);
        }
        else LblImc.Text = "--";

        LblCatTop.Text = $"Categoría: {GetBoxingCategory(profile.WeightKg)}";
    }

    private string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "AT";
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1) return words[0].Substring(0, 1).ToUpper();
        return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpper();
    }

    private string GetBoxingCategory(double weight)
    {
        if (weight <= 60.0) return "Ligero";
        if (weight <= 63.5) return "Superligero";
        if (weight <= 66.6) return "Wélter";
        if (weight <= 69.8) return "Superwélter";
        return "Mediano";
    }

    private void OnToggleWeight(object sender, EventArgs e)
    {
        FormWeight.IsVisible = !FormWeight.IsVisible;
        if (FormWeight.IsVisible) EntryNewWeight.Text = "";
    }

    private void OnSaveWeight(object sender, EventArgs e)
    {
        if (double.TryParse(EntryNewWeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double newWeight) && newWeight > 0)
        {
            var profile = StorageService.LoadProfile();
            profile.WeightKg = newWeight;
            StorageService.SaveProfile(profile);

            UpdateUI();
            FormWeight.IsVisible = false;
        }
    }

    private void OnToggleEdit(object sender, EventArgs e)
    {
        FormEdit.IsVisible = !FormEdit.IsVisible;
        if (FormEdit.IsVisible)
        {
            var profile = StorageService.LoadProfile();
            EntryName.Text = profile.Name;
            EntryAge.Text = profile.Age.ToString();
            EntryHeight.Text = profile.HeightCm.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private void OnSaveProfile(object sender, EventArgs e)
    {
        var profile = StorageService.LoadProfile();
        profile.Name = string.IsNullOrWhiteSpace(EntryName.Text) ? "Atleta" : EntryName.Text;

        if (int.TryParse(EntryAge.Text, out int age) && age > 0) profile.Age = age;
        if (double.TryParse(EntryHeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double height) && height > 0) profile.HeightCm = height;

        StorageService.SaveProfile(profile);

        UpdateUI();
        FormEdit.IsVisible = false;
    }
}