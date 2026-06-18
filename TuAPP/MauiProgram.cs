using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace TuAPP;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registrar el servicio de audio para la campana
        builder.Services.AddSingleton(AudioManager.Current);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}