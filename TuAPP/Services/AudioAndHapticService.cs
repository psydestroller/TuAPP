namespace TuAPP.Services;

public static class AudioAndHapticService
{
    // Vibración fuerte y única al empezar a golpear
    public static void VibrateRoundStart()
    {
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(800)); } catch { }
    }

    // Vibración en código (3 pulsos rápidos) simulando la campana final
    public static void VibrateRoundEnd()
    {
        Task.Run(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(150)); } catch { }
                await Task.Delay(250);
            }
        });
    }

    // Pequeños "toques" para la cuenta regresiva (los últimos 10 segundos)
    public static void VibrateCountdownTick()
    {
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50)); } catch { }
    }
}