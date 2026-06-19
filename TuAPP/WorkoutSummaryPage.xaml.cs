using TuAPP.Models;

namespace TuAPP;

public partial class WorkoutSummaryPage : ContentPage
{
    private WorkoutSession _session;

    public WorkoutSummaryPage(WorkoutSession session)
    {
        InitializeComponent();
        _session = session;

        // Cargar los datos calculados en la interfaz visual
        LblType.Text = session.TypeLabel.ToUpper();
        LblRounds.Text = $"{session.RoundsCompleted} Rounds";
        LblCalories.Text = $"{session.CaloriesBurned:F0} kcal";

        // Formatear el tiempo total a minutos y segundos
        var timeSpan = TimeSpan.FromSeconds(session.TotalSeconds);
        LblTime.Text = $"Tiempo Total: {timeSpan.ToString(@"mm\:ss")}";
    }

    // LÓGICA DE FASE 3: MENÚ NATIVO DE COMPARTIR
    private async void OnShareClicked(object sender, EventArgs e)
    {
        // Construcción del mensaje con formato limpio y emojis para redes
        string txtCompartir = $"🥊 ¡Entrenamiento de Boxeo Completado! 🥊\n\n" +
                              $"⚡ Modalidad: {_session.TypeLabel}\n" +
                              $"💪 Intensidad: {_session.RoundsCompleted} Rounds terminados\n" +
                              $"🔥 Desgaste: {_session.CaloriesBurned:F0} kcal quemadas\n\n" +
                              $"¡La preparación no se detiene! 🥊🔥 #CampamentoActivo #BoxingTimer";

        // Invocar el selector de aplicaciones del celular (WhatsApp, Instagram, etc.)
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Presumir Entrenamiento",
            Text = txtCompartir
        });
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        // Cerrar la pantalla emergente de forma segura
        await Navigation.PopModalAsync();
    }
}