namespace Heimdall_AI;

public partial class App : Application
{
    private readonly IMqttAlertService _mqttAlertService;
    private readonly IListeningSettingsService _listeningSettingsService;
    private readonly INativeAlertService _nativeAlertService;
    private bool _securityAlertOpening;

    public App(
        IMqttAlertService mqttAlertService,
        IAlertHistoryService alertHistoryService,
        IListeningSettingsService listeningSettingsService,
        INativeAlertService nativeAlertService)
    {
        _mqttAlertService = mqttAlertService;
        _listeningSettingsService = listeningSettingsService;
        _nativeAlertService = nativeAlertService;
        alertHistoryService.AlerteActiveRecue += OnAlerteActiveRecue;

        _ = mqttAlertService.StartAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private void OnAlerteActiveRecue(Alertes alerte)
    {
        var modeAlerte = _listeningSettingsService.IsAlertModeEnabled;
        _ = _nativeAlertService.PushNotificationAsync(alerte, modeAlerte);
        _ = _mqttAlertService.PublishNotificationEventAsync(alerte);

        if (!modeAlerte)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_securityAlertOpening)
            {
                return;
            }

            if (Shell.Current?.CurrentState.Location.ToString().Contains(nameof(SecurityAlertPage), StringComparison.OrdinalIgnoreCase) == true)
            {
                return;
            }

            if (Shell.Current is null)
            {
                return;
            }

            _securityAlertOpening = true;
            try
            {
                var type = Uri.EscapeDataString(alerte.TypeDetection ?? "Unknown sound");
                var location = Uri.EscapeDataString("Zone principale");
                var time = Uri.EscapeDataString("Just now");

                await Shell.Current.GoToAsync($"{nameof(SecurityAlertPage)}?type={type}&location={location}&timestamp={time}");
            }
            finally
            {
                _securityAlertOpening = false;
            }
        });
    }
}