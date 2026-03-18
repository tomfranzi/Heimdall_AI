namespace Heimdall_AI.Services;

public interface IListeningSettingsService
{
    bool IsListeningEnabled { get; set; }
    bool IsAlertModeEnabled { get; set; }
    double MicroSensitivity { get; set; }
    double MinimumConfidence { get; set; }
    bool DetectGlassBreak { get; set; }
    bool DetectSmokeAlarm { get; set; }
    bool DetectShouting { get; set; }
    bool DetectBabyCrying { get; set; }
    bool DetectDogBarking { get; set; }

    bool IsCategoryEnabled(string? detectedType);
    bool IsConfidenceAccepted(double? confidence);
}

public sealed partial class ListeningSettingsService : ObservableObject, IListeningSettingsService
{
    private const string IsListeningEnabledKey = "settings.is_listening_enabled";
    private const string IsAlertModeEnabledKey = "settings.is_alert_mode_enabled";
    private const string MicroSensitivityKey = "settings.micro_sensitivity";
    private const string MinimumConfidenceKey = "settings.minimum_confidence";
    private const string DetectGlassBreakKey = "settings.detect_glass_break";
    private const string DetectSmokeAlarmKey = "settings.detect_smoke_alarm";
    private const string DetectShoutingKey = "settings.detect_shouting";
    private const string DetectBabyCryingKey = "settings.detect_baby_crying";
    private const string DetectDogBarkingKey = "settings.detect_dog_barking";

    [ObservableProperty]
    private bool isListeningEnabled;

    [ObservableProperty]
    private bool isAlertModeEnabled;

    [ObservableProperty]
    private double microSensitivity;

    [ObservableProperty]
    private double minimumConfidence;

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

    public ListeningSettingsService()
    {
        isListeningEnabled = Preferences.Default.Get(IsListeningEnabledKey, true);
        isAlertModeEnabled = Preferences.Default.Get(IsAlertModeEnabledKey, false);
        microSensitivity = Preferences.Default.Get(MicroSensitivityKey, 50d);
        minimumConfidence = Preferences.Default.Get(MinimumConfidenceKey, 30d);
        detectGlassBreak = Preferences.Default.Get(DetectGlassBreakKey, true);
        detectSmokeAlarm = Preferences.Default.Get(DetectSmokeAlarmKey, true);
        detectShouting = Preferences.Default.Get(DetectShoutingKey, false);
        detectBabyCrying = Preferences.Default.Get(DetectBabyCryingKey, false);
        detectDogBarking = Preferences.Default.Get(DetectDogBarkingKey, true);
    }

    partial void OnIsListeningEnabledChanged(bool value) => Preferences.Default.Set(IsListeningEnabledKey, value);
    partial void OnIsAlertModeEnabledChanged(bool value) => Preferences.Default.Set(IsAlertModeEnabledKey, value);
    partial void OnMicroSensitivityChanged(double value) => Preferences.Default.Set(MicroSensitivityKey, Math.Clamp(value, 0, 100));
    partial void OnMinimumConfidenceChanged(double value) => Preferences.Default.Set(MinimumConfidenceKey, Math.Clamp(value, 0, 100));
    partial void OnDetectGlassBreakChanged(bool value) => Preferences.Default.Set(DetectGlassBreakKey, value);
    partial void OnDetectSmokeAlarmChanged(bool value) => Preferences.Default.Set(DetectSmokeAlarmKey, value);
    partial void OnDetectShoutingChanged(bool value) => Preferences.Default.Set(DetectShoutingKey, value);
    partial void OnDetectBabyCryingChanged(bool value) => Preferences.Default.Set(DetectBabyCryingKey, value);
    partial void OnDetectDogBarkingChanged(bool value) => Preferences.Default.Set(DetectDogBarkingKey, value);

    public bool IsCategoryEnabled(string? detectedType)
    {
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            return true;
        }

        var type = detectedType.Trim().ToLowerInvariant();

        if (type.Contains("glass") || type.Contains("break"))
        {
            return DetectGlassBreak;
        }

        if (type.Contains("smoke") || type.Contains("alarm") || type.Contains("siren"))
        {
            return DetectSmokeAlarm;
        }

        if (type.Contains("shout") || type.Contains("scream") || type.Contains("aggressive"))
        {
            return DetectShouting;
        }

        if (type.Contains("baby") || type.Contains("cry"))
        {
            return DetectBabyCrying;
        }

        if (type.Contains("dog") || type.Contains("bark") || type.Contains("pet") || type.Contains("animal") || type.Contains("cat"))
        {
            return DetectDogBarking;
        }

        return true;
    }

    public bool IsConfidenceAccepted(double? confidence)
    {
        if (!confidence.HasValue)
        {
            return true;
        }

        return confidence.Value * 100 >= MinimumConfidence;
    }
}
