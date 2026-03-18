using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui; // 1. L'import du Toolkit
using Heimdall_AI.Services;
using Heimdall_AI.ViewModels;
using Heimdall_AI.Views;

namespace Heimdall_AI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // 2. L'activation du Toolkit (juste ici, SANS point-virgule)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IAlertHistoryService, AlertHistoryService>();
        builder.Services.AddSingleton<IListeningSettingsService, ListeningSettingsService>();
        builder.Services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
        builder.Services.AddSingleton<INativeAlertService, NativeAlertService>();
        builder.Services.AddSingleton<IMqttAlertService, MqttAlertService>();
        builder.Services.AddSingleton<ILocalAuthService, LocalAuthService>();

        // Déclaration de toutes tes pages et ViewModels
        builder.Services.AddTransient<AlertesViewModels>();
        builder.Services.AddTransient<AlertesPage>();

        builder.Services.AddTransient<HistoriqueViewModels>();
        builder.Services.AddTransient<HistoriquePage>();

        builder.Services.AddTransient<SupervisionViewModels>();
        builder.Services.AddTransient<SupervisionPage>();

        builder.Services.AddTransient<ParametresViewModels>();
        builder.Services.AddTransient<ParametresPage>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CreateAccountPage>();
        builder.Services.AddTransient<SecurityAlertPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}