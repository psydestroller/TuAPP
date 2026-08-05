using System.Globalization;
using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    private async void OnComenzarClicked(object sender, EventArgs e)
    {
        // 1. PRIMER FILTRO: Revisar que ningún campo esté vacío
        if (string.IsNullOrWhiteSpace(EntryName.Text) ||
            string.IsNullOrWhiteSpace(EntryWeight.Text) ||
            string.IsNullOrWhiteSpace(EntryHeight.Text) ||
            string.IsNullOrWhiteSpace(EntryAge.Text))
        {
            await DisplayAlert("Datos Incompletos", "Por favor, llena todos los campos para poder configurar tu perfil de peleador.", "Entendido");
            return; // Bloquea el avance y se queda en la pantalla
        }

        // 2. SEGUNDO FILTRO: Convertir y validar que sean números lógicos mayores a cero
        bool isWeightValid = double.TryParse(EntryWeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double w);
        bool isHeightValid = double.TryParse(EntryHeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double h);
        bool isAgeValid = int.TryParse(EntryAge.Text, out int age);

        if (!isWeightValid || !isHeightValid || !isAgeValid || w <= 0 || h <= 0 || age <= 0)
        {
            await DisplayAlert("Datos Inválidos", "Asegúrate de ingresar números reales y mayores a cero en tu peso, altura y edad.", "Corregir");
            return; // Bloquea el avance
        }

        // =================================================================
        // 3. ÉXITO: Si pasa los filtros, guardamos con la seguridad de que todo es correcto
        // =================================================================

        var profile = new AthleteProfile
        {
            Name = EntryName.Text.Trim(), // .Trim() borra si el usuario puso espacios al inicio o al final por error
            WeightKg = w,
            HeightCm = h,
            Age = age
        };
        StorageService.SaveProfile(profile);

        var fight = new FightEvent { Date = DateTime.Today.AddDays(30), TargetWeightKg = w };
        StorageService.SaveFightEvent(fight);

        if (TpTrainingTime != null)
        {
            TimeSpan safeTime = TpTrainingTime.Time is TimeSpan ts ? ts : new TimeSpan(17, 0, 0);
            await NotificationService.RequestAndScheduleAsync(safeTime);
        }

        Preferences.Set("IsFirstLaunch", false);
        Application.Current!.MainPage = new AppShell();
    }
}