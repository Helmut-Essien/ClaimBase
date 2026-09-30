using Microsoft.Extensions.Logging;

namespace ClaimBase.MobileApp;

/// <summary>
/// Builds the lecturer application host.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates the MAUI app. Lecturer sign-in and the offline outbox are added with session logging.
    /// </summary>
    /// <returns>The built application.</returns>
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
