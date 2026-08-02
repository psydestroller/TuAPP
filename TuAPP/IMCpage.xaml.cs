using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TuAPP;

public class WeightCategory : INotifyPropertyChanged
{
    private bool _isUser = false;
    public string Name { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;

    // El secreto: este valor numérico permite a la app evaluar matemáticamente
    public double MaxWeight { get; set; } = 0;
    public Color TextColor { get; set; } = Colors.White;

    public bool IsUser
    {
        get => _isUser;
        set
        {
            if (_isUser != value)
            {
                _isUser = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class IMCpage : ContentPage
{
    public ObservableCollection<WeightCategory> Categories { get; set; } = new();
    private bool _isInitializing = true;
    private bool _isAmateurMode = true; // Por defecto arrancamos en Amateur

    public IMCpage()
    {
        InitializeComponent();
        ListCategories.ItemsSource = Categories;
        LoadAmateurCategories(); // Carga IBA por defecto
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInitializing = true;

        var profile = StorageService.LoadProfile();

        TxtWeight.Text = profile.WeightKg > 0 ? profile.WeightKg.ToString("F1") : "65.0";
        TxtHeight.Text = profile.HeightCm > 0 ? profile.HeightCm.ToString("F0") : "175";

        PckGender.SelectedIndex = 0;
        TxtAge.Text = "17";

        _isInitializing = false;
        CalculateAndSaveIMC(false);
    }

    // ==========================================================
    // CAMBIO DE MODOS (AMATEUR VS PROFESIONAL)
    // ==========================================================
    private void OnModeAmateurClicked(object sender, EventArgs e)
    {
        if (_isAmateurMode) return;
        _isAmateurMode = true;

        // Estilos del botón
        BtnAmateur.BackgroundColor = Color.FromArgb("#00E676"); // WorkColor
        BtnAmateur.TextColor = Colors.Black;
        BtnPro.BackgroundColor = Colors.Transparent;
        BtnPro.TextColor = Colors.White;

        LoadAmateurCategories();
        CalculateAndSaveIMC(false); // Re-evaluar el peso en la nueva lista
    }

    private void OnModeProClicked(object sender, EventArgs e)
    {
        if (!_isAmateurMode) return;
        _isAmateurMode = false;

        // Estilos del botón
        BtnPro.BackgroundColor = Color.FromArgb("#00E676"); // WorkColor
        BtnPro.TextColor = Colors.Black;
        BtnAmateur.BackgroundColor = Colors.Transparent;
        BtnAmateur.TextColor = Colors.White;

        LoadProfessionalCategories();
        CalculateAndSaveIMC(false); // Re-evaluar el peso en la nueva lista
    }

    // ==========================================================
    // LOGICA DE DATOS
    // ==========================================================
    private void OnInputChanged(object sender, EventArgs e)
    {
        if (_isInitializing) return;
        CalculateAndSaveIMC(false);
    }

    private void OnCalculateClicked(object sender, EventArgs e)
    {
        CalculateAndSaveIMC(true);
        DisplayAlert("IMC Guardado", "Peso y altura sincronizados con tu Perfil.", "OK");
    }

    private void CalculateAndSaveIMC(bool saveToProfile)
    {
        if (PckGender == null || TxtAge == null || TxtWeight == null || TxtHeight == null) return;

        bool isWeightValid = double.TryParse(TxtWeight.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double weightKg);
        bool isHeightValid = double.TryParse(TxtHeight.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double heightCm);
        bool isAgeValid = int.TryParse(TxtAge.Text, out int age);

        if (!isWeightValid || !isHeightValid || !isAgeValid || heightCm <= 0 || weightKg <= 0 || age <= 0)
        {
            LblIMCResult.Text = "--.-";
            LblIMCStatus.Text = "Datos incompletos";
            LblBodyFat.Text = "Grasa Corporal: --%";
            LblBoxCategory.Text = "--";
            return;
        }

        double heightMeters = heightCm / 100.0;
        double imc = weightKg / (heightMeters * heightMeters);
        LblIMCResult.Text = imc.ToString("F1");

        if (imc < 18.5) { LblIMCStatus.Text = "Bajo peso"; LblIMCResult.TextColor = Color.FromArgb("#0EA5E9"); }
        else if (imc < 25) { LblIMCStatus.Text = "Peso normal"; LblIMCResult.TextColor = Color.FromArgb("#10B981"); }
        else if (imc < 30) { LblIMCStatus.Text = "Sobrepeso"; LblIMCResult.TextColor = Color.FromArgb("#EAB308"); }
        else { LblIMCStatus.Text = "Obesidad"; LblIMCResult.TextColor = Color.FromArgb("#EF4444"); }

        int genderValue = PckGender.SelectedIndex == 0 ? 1 : 0;
        double bodyFat = (1.20 * imc) + (0.23 * age) - (10.8 * genderValue) - 5.4;

        if (bodyFat < 1.0) bodyFat = 1.0;
        LblBodyFat.Text = $"Grasa Corporal est.: {bodyFat:F1}%";

        UpdateBoxingCategory(weightKg);

        if (saveToProfile)
        {
            var profile = StorageService.LoadProfile();
            profile.WeightKg = weightKg;
            profile.HeightCm = heightCm;
            StorageService.SaveProfile(profile);
        }
    }

    private void UpdateBoxingCategory(double weight)
    {
        foreach (var c in Categories) c.IsUser = false;

        WeightCategory current = Categories.Last(); // Por defecto asume el más pesado

        // Busca automáticamente en cuál categoría encaja según el MaxWeight configurado
        foreach (var c in Categories)
        {
            if (weight <= c.MaxWeight)
            {
                current = c;
                break; // Encontramos la categoría, detenemos la búsqueda
            }
        }

        current.IsUser = true;
        LblBoxCategory.Text = current.Name;
    }

    // ==========================================================
    // LISTAS OFICIALES REGLAMENTARIAS
    // ==========================================================

    // REGLAMENTO IBA (Asociación Internacional de Boxeo Amateur - Masculino Elite)
    private void LoadAmateurCategories()
    {
        Categories.Clear();
        Categories.Add(new WeightCategory { Name = "Mínimo", Range = "46 - 48 kg", MaxWeight = 48.0 });
        Categories.Add(new WeightCategory { Name = "Mosca", Range = "48 - 51 kg", MaxWeight = 51.0 });
        Categories.Add(new WeightCategory { Name = "Gallo", Range = "51 - 54 kg", MaxWeight = 54.0 });
        Categories.Add(new WeightCategory { Name = "Pluma", Range = "54 - 57 kg", MaxWeight = 57.0 });
        Categories.Add(new WeightCategory { Name = "Ligero", Range = "57 - 60 kg", MaxWeight = 60.0 });
        Categories.Add(new WeightCategory { Name = "Wélter Ligero", Range = "60 - 63.5 kg", MaxWeight = 63.5 });
        Categories.Add(new WeightCategory { Name = "Wélter", Range = "63.5 - 67 kg", MaxWeight = 67.0 });
        Categories.Add(new WeightCategory { Name = "Medio Ligero", Range = "67 - 71 kg", MaxWeight = 71.0 });
        Categories.Add(new WeightCategory { Name = "Medio", Range = "71 - 75 kg", MaxWeight = 75.0 });
        Categories.Add(new WeightCategory { Name = "Semipesado", Range = "75 - 80 kg", MaxWeight = 80.0 });
        Categories.Add(new WeightCategory { Name = "Crucero", Range = "80 - 86 kg", MaxWeight = 86.0 });
        Categories.Add(new WeightCategory { Name = "Pesado", Range = "86 - 92 kg", MaxWeight = 92.0 });
        Categories.Add(new WeightCategory { Name = "Súper Pesado", Range = "+92 kg", MaxWeight = double.MaxValue });
    }

    // REGLAMENTO PROFESIONAL (CMB, AMB, OMB, FIB)
    private void LoadProfessionalCategories()
    {
        Categories.Clear();
        Categories.Add(new WeightCategory { Name = "Paja / Mínimo", Range = "Max 47.6 kg", MaxWeight = 47.6 });
        Categories.Add(new WeightCategory { Name = "Minimosca", Range = "47.6 - 48.9 kg", MaxWeight = 48.9 });
        Categories.Add(new WeightCategory { Name = "Mosca", Range = "48.9 - 50.8 kg", MaxWeight = 50.8 });
        Categories.Add(new WeightCategory { Name = "Supermosca", Range = "50.8 - 52.1 kg", MaxWeight = 52.1 });
        Categories.Add(new WeightCategory { Name = "Gallo", Range = "52.1 - 53.5 kg", MaxWeight = 53.5 });
        Categories.Add(new WeightCategory { Name = "Supergallo", Range = "53.5 - 55.3 kg", MaxWeight = 55.3 });
        Categories.Add(new WeightCategory { Name = "Pluma", Range = "55.3 - 57.1 kg", MaxWeight = 57.1 });
        Categories.Add(new WeightCategory { Name = "Superpluma", Range = "57.1 - 58.9 kg", MaxWeight = 58.9 });
        Categories.Add(new WeightCategory { Name = "Ligero", Range = "58.9 - 61.2 kg", MaxWeight = 61.2 });
        Categories.Add(new WeightCategory { Name = "Superligero", Range = "61.2 - 63.5 kg", MaxWeight = 63.5 });
        Categories.Add(new WeightCategory { Name = "Wélter", Range = "63.5 - 66.7 kg", MaxWeight = 66.7 });
        Categories.Add(new WeightCategory { Name = "Superwélter", Range = "66.7 - 69.8 kg", MaxWeight = 69.8 });
        Categories.Add(new WeightCategory { Name = "Mediano", Range = "69.8 - 72.5 kg", MaxWeight = 72.5 });
        Categories.Add(new WeightCategory { Name = "Supermediano", Range = "72.5 - 76.2 kg", MaxWeight = 76.2 });
        Categories.Add(new WeightCategory { Name = "Semipesado", Range = "76.2 - 79.3 kg", MaxWeight = 79.3 });
        Categories.Add(new WeightCategory { Name = "Crucero", Range = "79.3 - 90.7 kg", MaxWeight = 90.7 });
        Categories.Add(new WeightCategory { Name = "Completo / Pesado", Range = "+90.7 kg", MaxWeight = double.MaxValue });
    }
}