namespace TuAPP.Services;

public static class NotificationService
{
    public static async Task RequestAndScheduleAsync(TimeSpan trainingTime)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();

        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }

        if (status == PermissionStatus.Granted)
        {
            // 1. Calcular hora de Aviso de Vendas (-30 min)
            TimeSpan prepTime = trainingTime.Subtract(TimeSpan.FromMinutes(30));
            if (prepTime.TotalHours < 0) prepTime = prepTime.Add(TimeSpan.FromDays(1));

            // 2. Hora de Acción Exacta (0 min)
            TimeSpan actionTime = trainingTime;

            Preferences.Set("DailyReminderTime", prepTime.ToString());

#if ANDROID
            // DISPARO 1: Vendas (Cédula: 101)
            TuAPP.Platforms.Android.AlarmScheduler.ScheduleDaily(
                triggerTime: prepTime,
                requestCode: 101,
                title: "⏰ Preparación de Campamento",
                message: "🥊 ¡Prepárate! Tu entrenamiento comienza en 30 minutos. Ponte las vendas, vamos a dar esos 66 kg."
            );

            // DISPARO 2: Acción (Cédula: 102)
            TuAPP.Platforms.Android.AlarmScheduler.ScheduleDaily(
                triggerTime: actionTime,
                requestCode: 102,
                title: "🥊 ¡A ENTRENAR SE HA DICHO!",
                message: "Es la hora exacta. Guantes puestos, abre la app y pon a correr el cronómetro. ¡Cero excusas!"
            );
#endif
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current!.MainPage!.DisplayAlert("Permiso Denegado", "No podremos avisarte de tu entrenamiento. Ve a Ajustes para activarlo si cambias de opinión.", "Entendido");
            });
        }
    }
}