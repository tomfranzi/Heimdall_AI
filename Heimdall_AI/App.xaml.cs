namespace Heimdall_AI;

public partial class App : Application
{
    private readonly IMqttAlertService _mqttAlertService;
    private readonly IListeningSettingsService _listeningSettingsService;
    private readonly INativeAlertService _nativeAlertService;
    private bool _securityAlertOpening;
    private bool _alarmResumePromptOpening;

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
        var window = new Window(new AppShell());
        window.Resumed += OnWindowResumed;
        return window;
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_alarmResumePromptOpening)
            {
                return;
            }

            if (!await _nativeAlertService.IsCriticalAlertActiveAsync())
            {
                return;
            }

            if (Shell.Current is null)
            {
                return;
            }

            if (Shell.Current.CurrentState.Location.ToString().Contains(nameof(SecurityAlertPage), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _alarmResumePromptOpening = true;
            try
            {
                var type = Uri.EscapeDataString("ALARME ACTIVE");
                var location = Uri.EscapeDataString("Appareil");
                var time = Uri.EscapeDataString("Maintenant");
                await Shell.Current.GoToAsync($"{nameof(SecurityAlertPage)}?type={type}&location={location}&timestamp={time}");
            }
            finally
            {
                _alarmResumePromptOpening = false;
            }
        });
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
                var type = Uri.EscapeDataString(alerte.TypeDetection ?? "Son inconnu");
                var location = Uri.EscapeDataString("Zone principale");
                var time = Uri.EscapeDataString("À l'instant");

                await Shell.Current.GoToAsync($"{nameof(SecurityAlertPage)}?type={type}&location={location}&timestamp={time}");
            }
            finally
            {
                _securityAlertOpening = false;
            }
        });
    }
}