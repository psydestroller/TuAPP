using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TuAPP.Models;
using TuAPP.Services;

namespace TuAPP;

// =======================================================
// CLASE VISUAL PARA ENLAZAR LA LISTA
// =======================================================
public class PastFightDisplay : PastFight
{
    public string FighterName { get; set; } = "Peleador";

    public Color ResultColor => Result.StartsWith("Victoria") ? Color.FromArgb("#10B981") :
                                Result.StartsWith("Derrota") ? Color.FromArgb("#EF4444") :
                                Color.FromArgb("#A1A1AA");

    public string TitleDisplay => $"{FighterName} vs {Opponent}";

    public string OptionalInfoDisplay
    {
        get
        {
            var parts = new List<string>();
            if (Date.HasValue) parts.Add(Date.Value.ToString("dd/MM/yy"));
            if (!string.IsNullOrWhiteSpace(FightType)) parts.Add(FightType);
            if (!string.IsNullOrWhiteSpace(WeightAtFight)) parts.Add($"{WeightAtFight} kg");

            return string.Join(" • ", parts);
        }
    }

    public bool HasOptionalInfo => !string.IsNullOrEmpty(OptionalInfoDisplay);
}

public partial class ProfilePage : ContentPage
{
    // Variables para "Deshacer"
    private PastFight _recentlyDeletedFight;
    private CancellationTokenSource _undoTokenSource;

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
        var profile = StorageService.LoadProfile() ?? new AthleteProfile();

        LblName.Text = profile.Name;
        LblAge.Text = profile.Age.ToString();
        LblWeight.Text = $"{profile.WeightKg.ToString("0.##", CultureInfo.InvariantCulture)} kg";
        LblHeight.Text = $"{profile.HeightCm.ToString("0.##", CultureInfo.InvariantCulture)} cm";

        // Récord V-E-D-KO
        LblWins.Text = profile.Wins.ToString();
        LblDraws.Text = profile.Draws.ToString();
        LblLosses.Text = profile.Losses.ToString();
        LblKOs.Text = profile.Knockouts.ToString();

        if (!string.IsNullOrEmpty(profile.ProfileImagePath) && File.Exists(profile.ProfileImagePath))
        {
            ImgProfile.Source = ImageSource.FromFile(profile.ProfileImagePath);
            ImgProfile.IsVisible = true;
            LblInitials.IsVisible = false;
        }
        else
        {
            LblInitials.Text = GetInitials(profile.Name);
            ImgProfile.IsVisible = false;
            LblInitials.IsVisible = true;
        }

        double heightM = profile.HeightCm / 100.0;
        if (heightM > 0)
        {
            double imc = profile.WeightKg / (heightM * heightM);
            LblImc.Text = imc.ToString("F1", CultureInfo.InvariantCulture);
        }
        else LblImc.Text = "--";

        LblCatTop.Text = $"Categoría: {profile.BoxingCategory}";

        LoadFightHistory(profile);
    }

    private void LoadFightHistory(AthleteProfile profile)
    {
        if (profile.FightHistory == null || profile.FightHistory.Count == 0)
        {
            ListPastFights.ItemsSource = null;
            LblEmptyFights.IsVisible = true;
            return;
        }

        LblEmptyFights.IsVisible = false;

        var displayList = profile.FightHistory.OrderByDescending(f => f.Date ?? DateTime.MinValue).Select(f => new PastFightDisplay
        {
            Id = f.Id,
            FighterName = profile.Name,
            Opponent = f.Opponent,
            Result = f.Result,
            Details = f.Details,
            Date = f.Date,
            FightType = f.FightType,
            WeightAtFight = f.WeightAtFight
        }).ToList();

        ListPastFights.ItemsSource = displayList;
    }

    private string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "PE";
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1) return words[0].Substring(0, 1).ToUpper();
        return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpper();
    }

    // =======================================================
    // MENÚ FLOTANTE (3 PUNTOS) Y BOTONES UNIFICADOS
    // =======================================================
    private void OnOpenOptionsMenu(object sender, EventArgs e)
    {
        MenuOptionsOverlay.IsVisible = true;
    }

    private void OnCloseMenuTapped(object sender, TappedEventArgs e)
    {
        MenuOptionsOverlay.IsVisible = false;
    }

    private void OnMenuEditClicked(object sender, EventArgs e)
    {
        MenuOptionsOverlay.IsVisible = false;
        FormEdit.IsVisible = !FormEdit.IsVisible;

        if (FormEdit.IsVisible)
        {
            var profile = StorageService.LoadProfile();
            EntryName.Text = profile.Name;
            EntryAge.Text = profile.Age.ToString();
            EntryHeight.Text = profile.HeightCm.ToString("0.##", CultureInfo.InvariantCulture);
            EntryWeight.Text = profile.WeightKg.ToString("0.##", CultureInfo.InvariantCulture);

            EntryWins.Text = profile.Wins.ToString();
            EntryDraws.Text = profile.Draws.ToString();
            EntryLosses.Text = profile.Losses.ToString();
            EntryKOs.Text = profile.Knockouts.ToString();
        }
    }

    private void OnCancelEdit(object sender, EventArgs e)
    {
        FormEdit.IsVisible = false;
    }

    private void OnSaveProfile(object sender, EventArgs e)
    {
        var profile = StorageService.LoadProfile();
        profile.Name = string.IsNullOrWhiteSpace(EntryName.Text) ? "Pedro" : EntryName.Text;

        if (int.TryParse(EntryAge.Text, out int age) && age > 0) profile.Age = age;
        if (double.TryParse(EntryHeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double height) && height > 0) profile.HeightCm = height;
        if (double.TryParse(EntryWeight.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double weight) && weight > 0) profile.WeightKg = weight;

        // Se actualiza el récord con los datos manuales (establece la nueva base real).
        if (int.TryParse(EntryWins.Text, out int w)) profile.Wins = w;
        if (int.TryParse(EntryDraws.Text, out int d)) profile.Draws = d;
        if (int.TryParse(EntryLosses.Text, out int l)) profile.Losses = l;
        if (int.TryParse(EntryKOs.Text, out int ko)) profile.Knockouts = ko;

        StorageService.SaveProfile(profile);
        UpdateUI();
        FormEdit.IsVisible = false;
    }

    private async void OnProfilePictureTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync();
            var photo = photos?.FirstOrDefault();

            if (photo != null)
            {
                string localFilePath = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);
                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(localFilePath);
                await sourceStream.CopyToAsync(localFileStream);

                var profile = StorageService.LoadProfile();
                profile.ProfileImagePath = localFilePath;
                StorageService.SaveProfile(profile);

                UpdateUI();
            }
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Error", "No se pudo cargar la imagen.", "OK");
        }
    }

    // =======================================================
    // LÓGICA DE SINCRONIZACIÓN CORREGIDA (EVITAR BUGS KO)
    // =======================================================
    private void AdjustRecord(AthleteProfile profile, string result, int multiplier)
    {
        if (string.IsNullOrEmpty(result)) return;

        // multiplier = 1 para sumar (Añadir pelea), -1 para restar (Eliminar pelea)
        if (result.StartsWith("Victoria"))
        {
            profile.Wins += multiplier;
            // Solo sumamos/restamos KOs si efectivamente fue una VICTORIA (KO o RSC)
            if (result.Contains("KO") || result.Contains("RSC"))
            {
                profile.Knockouts += multiplier;
            }
        }
        else if (result.StartsWith("Derrota"))
        {
            profile.Losses += multiplier;
        }
        else if (result.StartsWith("Empate"))
        {
            profile.Draws += multiplier;
        }

        // Prevenir números negativos si editan manualmente y luego borran una pelea
        if (profile.Wins < 0) profile.Wins = 0;
        if (profile.Losses < 0) profile.Losses = 0;
        if (profile.Draws < 0) profile.Draws = 0;
        if (profile.Knockouts < 0) profile.Knockouts = 0;
    }

    // =======================================================
    // HISTORIAL DE PELEAS (AÑADIR, ELIMINAR, DESHACER)
    // =======================================================
    private void OnToggleHistory(object sender, EventArgs e)
    {
        SectionHistory.IsVisible = !SectionHistory.IsVisible;
    }

    private void OnToggleAddFight(object sender, EventArgs e)
    {
        FormAddFight.IsVisible = !FormAddFight.IsVisible;
        if (FormAddFight.IsVisible)
        {
            EntryOpponent.Text = "";
            EntryDetails.Text = "";
            EntryFightType.Text = "";
            EntryFightWeight.Text = "";
            SwHasDate.IsToggled = false;
            PckResult.SelectedIndex = 0;
        }
    }

    private void OnSavePastFight(object sender, EventArgs e)
    {
        var profile = StorageService.LoadProfile();
        string resultStr = PckResult.SelectedItem?.ToString() ?? "Victoria";

        var fight = new PastFight
        {
            Opponent = string.IsNullOrWhiteSpace(EntryOpponent.Text) ? "Oponente Desconocido" : EntryOpponent.Text,
            Details = EntryDetails.Text ?? "",
            FightType = EntryFightType.Text ?? "",
            WeightAtFight = EntryFightWeight.Text ?? "",
            Result = resultStr,
            Date = SwHasDate.IsToggled ? DpFightDate.Date : null
        };

        profile.FightHistory.Add(fight);

        // ¡Sincronización Automática al Añadir!
        AdjustRecord(profile, resultStr, 1);

        StorageService.SaveProfile(profile);

        UpdateUI();
        FormAddFight.IsVisible = false;
    }

    private async void OnDeletePastFightClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Guid fightId)
        {
            var profile = StorageService.LoadProfile();
            var match = profile.FightHistory.FirstOrDefault(f => f.Id == fightId);

            if (match != null)
            {
                // 1. Guardar la pelea temporalmente para deshacer
                _recentlyDeletedFight = match;

                // 2. Remover del historial y restar del Récord General (-1)
                profile.FightHistory.Remove(match);
                AdjustRecord(profile, match.Result, -1);

                // 3. Guardar cambios y actualizar
                StorageService.SaveProfile(profile);
                UpdateUI();

                // 4. Mostrar Snackbar / Panel de Deshacer temporal
                ShowUndoPanel();
            }
        }
    }

    private async void ShowUndoPanel()
    {
        UndoPanel.IsVisible = true;

        // Cancelar el token anterior si el usuario presiona borrar muy rápido
        _undoTokenSource?.Cancel();
        _undoTokenSource = new CancellationTokenSource();

        try
        {
            // Esperar 4 segundos antes de ocultarlo automáticamente
            await Task.Delay(4000, _undoTokenSource.Token);
            UndoPanel.IsVisible = false;
            _recentlyDeletedFight = null; // Se pierde para siempre
        }
        catch (TaskCanceledException)
        {
            // Cancelado porque se inició otra acción o se des-hizo
        }
    }

    private void OnUndoDeleteClicked(object sender, EventArgs e)
    {
        if (_recentlyDeletedFight != null)
        {
            var profile = StorageService.LoadProfile();

            // Volver a añadir y sumar el récord que habíamos restado
            profile.FightHistory.Add(_recentlyDeletedFight);
            AdjustRecord(profile, _recentlyDeletedFight.Result, 1);

            StorageService.SaveProfile(profile);
            UpdateUI();

            // Ocultar el panel y limpiar
            UndoPanel.IsVisible = false;
            _recentlyDeletedFight = null;
            _undoTokenSource?.Cancel();
        }
    }
}