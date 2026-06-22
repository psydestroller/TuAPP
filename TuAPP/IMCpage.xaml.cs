using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;

namespace TuAPP;

public class WeightCategory
{
    public string Name { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public Color TextColor { get; set; } = Colors.White;
    public bool IsUser { get; set; } = false;
}

public partial class IMCpage : ContentPage
{
    public ObservableCollection<WeightCategory> Categories { get; set; } = new();

    public IMCpage()
    {
        InitializeComponent();
        ListCategories.ItemsSource = Categories;
        LoadCategories();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var profile = StorageService.LoadProfile();

        SldPeso.ValueChanged -= OnSliderChanged;
        SldAltura.ValueChanged -= OnSliderChanged;

        SldPeso.Value = profile.WeightKg > 0 ? profile.WeightKg : 66.0;
        SldAltura.Value = profile.HeightCm > 0 ? profile.HeightCm : 170.0;

        SldPeso.ValueChanged += OnSliderChanged;
        SldAltura.ValueChanged += OnSliderChanged;

        UpdateLabels();
        CalculateAndSaveIMC(false);
    }

    private void OnSliderChanged(object sender, ValueChangedEventArgs e) => UpdateLabels();

    private void UpdateLabels()
    {
        LblPesoVal.Text = $"{SldPeso.Value:F1} kg";
        LblAlturaVal.Text = $"{SldAltura.Value:F0} cm";
    }

    private void OnCalculateClicked(object sender, EventArgs e)
    {
        CalculateAndSaveIMC(true);
        DisplayAlert("IMC Guardado", "Peso sincronizado con tu Perfil y Dashboard.", "OK");
    }

    private void CalculateAndSaveIMC(bool saveToProfile)
    {
        double weight = SldPeso.Value;
        double heightCm = SldAltura.Value;
        double heightM = heightCm / 100.0;

        if (heightM <= 0) return;

        double imc = weight / (heightM * heightM);
        LblIMCResult.Text = imc.ToString("F1");

        if (imc < 18.5) { LblIMCStatus.Text = "Bajo peso"; LblIMCResult.TextColor = Color.FromArgb("#0EA5E9"); }
        else if (imc < 25) { LblIMCStatus.Text = "Peso normal"; LblIMCResult.TextColor = Color.FromArgb("#10B981"); }
        else if (imc < 30) { LblIMCStatus.Text = "Sobrepeso"; LblIMCResult.TextColor = Color.FromArgb("#EAB308"); }
        else { LblIMCStatus.Text = "Obesidad"; LblIMCResult.TextColor = Color.FromArgb("#EF4444"); }

        UpdateBoxingCategory(weight);

        if (saveToProfile)
        {
            var profile = StorageService.LoadProfile();
            profile.WeightKg = weight;
            profile.HeightCm = heightCm;
            StorageService.SaveProfile(profile);
        }
    }

    private void UpdateBoxingCategory(double weight)
    {
        foreach (var c in Categories) c.IsUser = false;

        WeightCategory? current = null;
        if (weight <= 47.6) current = Categories[0];
        else if (weight <= 48.9) current = Categories[1];
        else if (weight <= 50.8) current = Categories[2];
        else if (weight <= 52.1) current = Categories[3];
        else if (weight <= 53.5) current = Categories[4];
        else if (weight <= 55.3) current = Categories[5];
        else if (weight <= 57.1) current = Categories[6];
        else if (weight <= 58.9) current = Categories[7];
        else if (weight <= 61.2) current = Categories[8];
        else if (weight <= 63.5) current = Categories[9];
        else if (weight <= 66.7) current = Categories[10]; // Wélter
        else if (weight <= 69.8) current = Categories[11];
        else if (weight <= 72.5) current = Categories[12];
        else if (weight <= 76.2) current = Categories[13];
        else if (weight <= 79.3) current = Categories[14];
        else if (weight <= 90.7) current = Categories[15];
        else current = Categories[16];

        if (current != null)
        {
            current.IsUser = true;
            LblBoxCategory.Text = current.Name;
        }
    }

    private void LoadCategories()
    {
        Categories.Clear();
        Categories.Add(new WeightCategory { Name = "Paja / Mínimo", Range = "Max 47.6 kg" });
        Categories.Add(new WeightCategory { Name = "Minimosca", Range = "47.6 - 48.9 kg" });
        Categories.Add(new WeightCategory { Name = "Mosca", Range = "48.9 - 50.8 kg" });
        Categories.Add(new WeightCategory { Name = "Supermosca", Range = "50.8 - 52.1 kg" });
        Categories.Add(new WeightCategory { Name = "Gallo", Range = "52.1 - 53.5 kg" });
        Categories.Add(new WeightCategory { Name = "Supergallo", Range = "53.5 - 55.3 kg" });
        Categories.Add(new WeightCategory { Name = "Pluma", Range = "55.3 - 57.1 kg" });
        Categories.Add(new WeightCategory { Name = "Superpluma", Range = "57.1 - 58.9 kg" });
        Categories.Add(new WeightCategory { Name = "Ligero", Range = "58.9 - 61.2 kg" });
        Categories.Add(new WeightCategory { Name = "Superligero", Range = "61.2 - 63.5 kg" });
        Categories.Add(new WeightCategory { Name = "Wélter", Range = "63.5 - 66.7 kg" });
        Categories.Add(new WeightCategory { Name = "Superwélter", Range = "66.7 - 69.8 kg" });
        Categories.Add(new WeightCategory { Name = "Mediano", Range = "69.8 - 72.5 kg" });
        Categories.Add(new WeightCategory { Name = "Supermediano", Range = "72.5 - 76.2 kg" });
        Categories.Add(new WeightCategory { Name = "Semipesado", Range = "76.2 - 79.3 kg" });
        Categories.Add(new WeightCategory { Name = "Crucero", Range = "79.3 - 90.7 kg" });
        Categories.Add(new WeightCategory { Name = "Completo / Pesado", Range = "+90.7 kg" });
    }
}