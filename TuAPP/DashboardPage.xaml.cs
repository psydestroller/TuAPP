using TuAPP.Models;
using TuAPP.Services;
using System.Collections.ObjectModel;

namespace TuAPP;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void OnEditGoalsClicked(object sender, EventArgs e) => FormEditGoals.IsVisible = !FormEditGoals.IsVisible;

    private void OnSaveGoalsClicked(object sender, EventArgs e)
    {
        // Guardar metas
        FormEditGoals.IsVisible = false;
        DisplayAlert("Éxito", "Metas guardadas", "OK");
    }

    private void OnSaveGymSession(object sender, EventArgs e)
    {
        // Lógica de guardado
        DisplayAlert("Éxito", "Sesión guardada", "OK");
    }

    private void OnShareSession(object sender, EventArgs e)
    {
        // Lógica de compartir
        DisplayAlert("Compartir", "Compartiendo...", "OK");
    }
}