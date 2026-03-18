namespace Heimdall_AI.ViewModels;

public partial class ParametresViewModels : ObservableObject
{
    private readonly IListeningSettingsService _settings;
    private readonly IMqttAlertService _mqttAlertService;
    private bool _initialisation;
    private CancellationTokenSource? _publishSensitivityCts;

    [ObservableProperty]
    private bool isListeningEnabled;

    [ObservableProperty]
    private double sensibiliteMicro;

    [ObservableProperty]
    private double seuilConfiance;

    [ObservableProperty]
    private bool detectGlassBreak;

    [ObservableProperty]
    private bool detectSmokeAlarm;

    [ObservableProperty]
    private bool detectShouting;

    [ObservableProperty]
    private bool detectBabyCrying;

    [ObservableProperty]
    private bool detectDogBarking;

    public ParametresViewModels(IListeningSettingsService settings, IMqttAlertService mqttAlertService)
    {
        _settings = settings;
        _mqttAlertService = mqttAlertService;

        _initialisation = true;

        IsListeningEnabled = settings.IsListeningEnabled;
        SensibiliteMicro = settings.MicroSensitivity;
        SeuilConfiance = settings.MinimumConfidence;
        DetectGlassBreak = settings.DetectGlassBreak;
        DetectSmokeAlarm = settings.DetectSmokeAlarm;
        DetectShouting = settings.DetectShouting;
        DetectBabyCrying = settings.DetectBabyCrying;
        DetectDogBarking = settings.DetectDogBarking;

        _initialisation = false;
    }

    partial void OnIsListeningEnabledChanged(bool value) => _settings.IsListeningEnabled = value;
    partial void OnSensibiliteMicroChanged(double value)
    {
        _settings.MicroSensitivity = value;

        if (_initialisation)
        {
            return;
        }

        _publishSensitivityCts?.Cancel();
        _publishSensitivityCts?.Dispose();
        _publishSensitivityCts = new CancellationTokenSource();
        var token = _publishSensitivityCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(220, token);
                await _mqttAlertService.PublishConfigAsync(value, token);
            }
            catch
            {
            }
        }, token);
    }

    [RelayCommand]
    private Task PublierSensibiliteAsync()
    {
        if (_initialisation)
        {
            return Task.CompletedTask;
        }

        _publishSensitivityCts?.Cancel();
        return _mqttAlertService.PublishConfigAsync(SensibiliteMicro);
    }

    partial void OnSeuilConfianceChanged(double value) => _settings.MinimumConfidence = value;
    partial void OnDetectGlassBreakChanged(bool value) => _settings.DetectGlassBreak = value;
    partial void OnDetectSmokeAlarmChanged(bool value) => _settings.DetectSmokeAlarm = value;
    partial void OnDetectShoutingChanged(bool value) => _settings.DetectShouting = value;
    partial void OnDetectBabyCryingChanged(bool value) => _settings.DetectBabyCrying = value;
    partial void OnDetectDogBarkingChanged(bool value) => _settings.DetectDogBarking = value;
}
