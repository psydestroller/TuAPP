using System.Collections.ObjectModel;

namespace TuAPP;

public class WeightCat
{
    public string Name { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public Color TextColor => IsUser ? Color.FromArgb("#10B981") : Color.FromArgb("#A1A1AA");
}

public partial class IMCpage : ContentPage
{
    public ObservableCollection<WeightCat> Categories { get; set; } = new();

    public IMCpage()
    {
        InitializeComponent();
        ListCategories.ItemsSource = Categories;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (double.TryParse(Preferences.Get("AthleteWeight", "70"), out double w)) SldPeso.Value = w;
        if (double.TryParse(Preferences.Get("AthleteHeight", "175"), out double h)) SldAltura.Value = h;
        CalculateIMC();
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e) => CalculateIMC();

    private void CalculateIMC()
    {
        double w = SldPeso.Value; double h = SldAltura.Value;
        LblPesoVal.Text = $"{w:F0} kg"; LblAlturaVal.Text = $"{h:F0} cm";

        double imc = w / ((h / 100) * (h / 100));
        LblIMCResult.Text = imc.ToString("F1");

        if (imc < 18.5) { LblIMCStatus.Text = "Bajo peso"; LblIMCResult.TextColor = Color.FromArgb("#0EA5E9"); }
        else if (imc < 25) { LblIMCStatus.Text = "Peso normal"; LblIMCResult.TextColor = Color.FromArgb("#10B981"); }
        else if (imc < 30) { LblIMCStatus.Text = "Sobrepeso"; LblIMCResult.TextColor = Color.FromArgb("#EAB308"); }
        else { LblIMCStatus.Text = "Obeso"; LblIMCResult.TextColor = Color.FromArgb("#EF4444"); }

        PopulateCategories(w);
        Preferences.Set("AthleteWeight", w.ToString("F0"));
        Preferences.Set("AthleteHeight", h.ToString("F0"));
    }

    private void PopulateCategories(double w)
    {
        Categories.Clear();
        var data = new List<(string, string, double)> {
            ("Minimosca", "hasta 48 kg", 48), ("Mosca", "48-51 kg", 51), ("Supermosca", "51-54 kg", 54),
            ("Gallo", "54-57 kg", 57), ("Supergallo", "57-60 kg", 60), ("Pluma", "60-64 kg", 64),
            ("Superpluma", "64-67 kg", 67), ("Ligero", "67-70 kg", 70), ("Superligero", "70-75 kg", 75),
            ("Welter", "75-80 kg", 80), ("Superwelter", "80-85 kg", 85), ("Mediano", "85-90 kg", 90),
            ("Supermediano", "90-95 kg", 95), ("Crucero", "95-90.7 kg", 90.7), ("Pesado", "más de 90 kg", 999)
        };

        string currentCat = "";
        foreach (var item in data)
        {
            bool isMatch = currentCat == "" && w <= item.Item3;
            if (isMatch) currentCat = item.Item1;
            Categories.Add(new WeightCat { Name = item.Item1, Range = item.Item2, IsUser = isMatch });
        }
        LblBoxCategory.Text = currentCat;
        LblBoxCategory.TextColor = LblIMCResult.TextColor;
    }
}