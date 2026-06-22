using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace TuAPP;

public class CardioLog
{
    public string Date { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public string Dist { get; set; } = string.Empty;
    public double RawDist { get; set; }
    public string Pace { get; set; } = string.Empty;
}

public partial class CardioPage : ContentPage
{
    public ObservableCollection<CardioLog> Logs { get; set; } = new();

    public CardioPage()
    {
        InitializeComponent();
        ListCardio.ItemsSource = Logs;
    }

    private void OnToggleFormClicked(object? sender, EventArgs e)
    {
        bool isFormOpen = FormAdd.IsVisible;
        FormAdd.IsVisible = !isFormOpen;
        ViewHistory.IsVisible = isFormOpen;
        if (sender is Button btn) btn.Text = isFormOpen ? "+ Agregar" : "Cancelar";
    }

    private void OnSaveLog(object? sender, EventArgs e)
    {
        // Parseo seguro: Acepta tanto punto como coma en los decimales (ej. 1.5 o 1,5)
        bool isKmValid = double.TryParse(EntryKm.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double km);
        bool isMinValid = double.TryParse(EntryMin.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double min);

        if (isKmValid && isMinValid && km > 0)
        {
            double paceMinPerKm = min / km;
            int pMin = (int)paceMinPerKm;
            int pSec = (int)((paceMinPerKm - pMin) * 60);

            Logs.Insert(0, new CardioLog
            {
                Date = $"{DpDate.Date:yyyy-MM-dd}",
                // Usamos 0.## en lugar de F0 para que respete los decimales
                Info = string.IsNullOrWhiteSpace(EntryNotes.Text) ? $"{min:0.##} min · Sin notas" : $"{min:0.##} min · {EntryNotes.Text}",
                Dist = $"{km:0.##} km",
                RawDist = km,
                Pace = $"{pMin}:{pSec:D2}/km"
            });

            UpdateDashboard(km, pMin, pSec);
            DrawChart();

            EntryKm.Text = ""; EntryMin.Text = ""; EntryNotes.Text = "";
            FormAdd.IsVisible = false; ViewHistory.IsVisible = true;
            BtnAgregar.Text = "+ Agregar";
        }
    }

    private void UpdateDashboard(double newKm, int pMin, int pSec)
    {
        // Leemos el total actual respetando decimales para no perder distancia real
        double.TryParse(LblTotalKm.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double currentTotal);

        // Sumamos y mostramos con el formato 0.##
        LblTotalKm.Text = $"{currentTotal + newKm:0.##}";
        LblTotalSes.Text = $"{Logs.Count}";
        LblAvgPace.Text = $"{pMin}:{pSec:D2}";
    }

    private void DrawChart()
    {
        ChartGrid.Children.Clear();
        ChartGrid.ColumnDefinitions.Clear();

        var recentLogs = Logs.Take(7).Reverse().ToList(); // Últimos 7, de más antiguo a más reciente
        if (!recentLogs.Any()) return;

        double maxKm = recentLogs.Max(l => l.RawDist);
        if (maxKm == 0) maxKm = 1;

        for (int i = 0; i < recentLogs.Count; i++)
        {
            ChartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            double barHeight = (recentLogs[i].RawDist / maxKm) * 70; // 70 es la altura máxima de la barra

            var barLayout = new VerticalStackLayout { VerticalOptions = LayoutOptions.End, HorizontalOptions = LayoutOptions.Fill };

            // Modificado para que la etiqueta de la gráfica también muestre los decimales exactos
            var lblVal = new Label { Text = recentLogs[i].RawDist.ToString("0.##", CultureInfo.InvariantCulture), FontSize = 10, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center };
            var box = new Border
            {
                BackgroundColor = Color.FromArgb("#10B981"),
                HeightRequest = barHeight,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4, 4, 0, 0) }
            };

            barLayout.Children.Add(lblVal);
            barLayout.Children.Add(box);

            ChartGrid.Add(barLayout, i, 0);
        }
    }
}