using System.Collections.ObjectModel;
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
        if (double.TryParse(EntryKm.Text, out double km) && double.TryParse(EntryMin.Text, out double min) && km > 0)
        {
            double paceMinPerKm = min / km;
            int pMin = (int)paceMinPerKm;
            int pSec = (int)((paceMinPerKm - pMin) * 60);

            Logs.Insert(0, new CardioLog
            {
                Date = $"{DpDate.Date:yyyy-MM-dd}",
                Info = string.IsNullOrWhiteSpace(EntryNotes.Text) ? $"{min:F0} min · Sin notas" : $"{min:F0} min · {EntryNotes.Text}",
                Dist = $"{km:F0} km",
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
        double.TryParse(LblTotalKm.Text, out double currentTotal);
        LblTotalKm.Text = $"{currentTotal + newKm:F0}";
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

            var lblVal = new Label { Text = recentLogs[i].RawDist.ToString("F0"), FontSize = 10, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center };
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