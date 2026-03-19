namespace Heimdall_AI.Services;

public interface IListeningSettingsService
{
    bool IsListeningEnabled { get; set; }
    bool IsAlertModeEnabled { get; set; }
    double MicroSensitivity { get; set; }
    double MinimumConfidence { get; set; }
    bool DetectSpeech { get; set; }
    bool DetectKnock { get; set; }
    bool DetectAlarm { get; set; }
    bool DetectDoor { get; set; }
    bool DetectMechanic { get; set; }
    bool DetectFan { get; set; }
    bool DetectTap { get; set; }
    bool DetectCoup { get; set; }
    bool DetectScream { get; set; }
    bool DetectShout { get; set; }
    bool DetectYell { get; set; }
    bool DetectGlass { get; set; }
    bool DetectShatter { get; set; }
    bool DetectCryingSobbing { get; set; }
    bool DetectDomesticAnimalsPets { get; set; }
    bool DetectClick { get; set; }

    bool IsCategoryEnabled(string? detectedType);
    bool IsConfidenceAccepted(double? confidence);
}

public sealed partial class ListeningSettingsService : ObservableObject, IListeningSettingsService
{
    private const string IsListeningEnabledKey = "settings.is_listening_enabled";
    private const string IsAlertModeEnabledKey = "settings.is_alert_mode_enabled";
    private const string MicroSensitivityKey = "settings.micro_sensitivity";
    private const string MinimumConfidenceKey = "settings.minimum_confidence";
    private const string DetectSpeechKey = "settings.detect_speech";
    private const string DetectKnockKey = "settings.detect_knock";
    private const string DetectAlarmKey = "settings.detect_alarm";
    private const string DetectDoorKey = "settings.detect_door";
    private const string DetectMechanicKey = "settings.detect_mechanic";
    private const string DetectFanKey = "settings.detect_fan";
    private const string DetectTapKey = "settings.detect_tap";
    private const string DetectCoupKey = "settings.detect_coup";
    private const string DetectScreamKey = "settings.detect_scream";
    private const string DetectShoutKey = "settings.detect_shout";
    private const string DetectYellKey = "settings.detect_yell";
    private const string DetectGlassKey = "settings.detect_glass";
    private const string DetectShatterKey = "settings.detect_shatter";
    private const string DetectCryingSobbingKey = "settings.detect_crying_sobbing";
    private const string DetectDomesticAnimalsPetsKey = "settings.detect_domestic_animals_pets";
    private const string DetectClickKey = "settings.detect_click";

    [ObservableProperty]
    private bool isListeningEnabled;

    [ObservableProperty]
    private bool isAlertModeEnabled;

    [ObservableProperty]
    private double microSensitivity;

    [ObservableProperty]
    private double minimumConfidence;

    [ObservableProperty]
    private bool detectSpeech;

    [ObservableProperty]
    private bool detectKnock;

    [ObservableProperty]
    private bool detectAlarm;

    [ObservableProperty]
    private bool detectDoor;

    [ObservableProperty]
    private bool detectMechanic;

    [ObservableProperty]
    private bool detectFan;

    [ObservableProperty]
    private bool detectTap;

    [ObservableProperty]
    private bool detectCoup;

    [ObservableProperty]
    private bool detectScream;

    [ObservableProperty]
    private bool detectShout;

    [ObservableProperty]
    private bool detectYell;

    [ObservableProperty]
    private bool detectGlass;

    [ObservableProperty]
    private bool detectShatter;

    [ObservableProperty]
    private bool detectCryingSobbing;

    [ObservableProperty]
    private bool detectDomesticAnimalsPets;

    [ObservableProperty]
    private bool detectClick;

    public ListeningSettingsService()
    {
        isListeningEnabled = Preferences.Default.Get(IsListeningEnabledKey, true);
        isAlertModeEnabled = Preferences.Default.Get(IsAlertModeEnabledKey, false);
        microSensitivity = Preferences.Default.Get(MicroSensitivityKey, 50d);
        minimumConfidence = Preferences.Default.Get(MinimumConfidenceKey, 30d);
        detectSpeech = Preferences.Default.Get(DetectSpeechKey, true);
        detectKnock = Preferences.Default.Get(DetectKnockKey, true);
        detectAlarm = Preferences.Default.Get(DetectAlarmKey, true);
        detectDoor = Preferences.Default.Get(DetectDoorKey, true);
        detectMechanic = Preferences.Default.Get(DetectMechanicKey, true);
        detectFan = Preferences.Default.Get(DetectFanKey, true);
        detectTap = Preferences.Default.Get(DetectTapKey, true);
        detectCoup = Preferences.Default.Get(DetectCoupKey, true);
        detectScream = Preferences.Default.Get(DetectScreamKey, true);
        detectShout = Preferences.Default.Get(DetectShoutKey, true);
        detectYell = Preferences.Default.Get(DetectYellKey, true);
        detectGlass = Preferences.Default.Get(DetectGlassKey, true);
        detectShatter = Preferences.Default.Get(DetectShatterKey, true);
        detectCryingSobbing = Preferences.Default.Get(DetectCryingSobbingKey, true);
        detectDomesticAnimalsPets = Preferences.Default.Get(DetectDomesticAnimalsPetsKey, true);
        detectClick = Preferences.Default.Get(DetectClickKey, true);
    }

    partial void OnIsListeningEnabledChanged(bool value) => Preferences.Default.Set(IsListeningEnabledKey, value);
    partial void OnIsAlertModeEnabledChanged(bool value) => Preferences.Default.Set(IsAlertModeEnabledKey, value);
    partial void OnMicroSensitivityChanged(double value) => Preferences.Default.Set(MicroSensitivityKey, Math.Clamp(value, 0, 100));
    partial void OnMinimumConfidenceChanged(double value) => Preferences.Default.Set(MinimumConfidenceKey, Math.Clamp(value, 0, 100));
    partial void OnDetectSpeechChanged(bool value) => Preferences.Default.Set(DetectSpeechKey, value);
    partial void OnDetectKnockChanged(bool value) => Preferences.Default.Set(DetectKnockKey, value);
    partial void OnDetectAlarmChanged(bool value) => Preferences.Default.Set(DetectAlarmKey, value);
    partial void OnDetectDoorChanged(bool value) => Preferences.Default.Set(DetectDoorKey, value);
    partial void OnDetectMechanicChanged(bool value) => Preferences.Default.Set(DetectMechanicKey, value);
    partial void OnDetectFanChanged(bool value) => Preferences.Default.Set(DetectFanKey, value);
    partial void OnDetectTapChanged(bool value) => Preferences.Default.Set(DetectTapKey, value);
    partial void OnDetectCoupChanged(bool value) => Preferences.Default.Set(DetectCoupKey, value);
    partial void OnDetectScreamChanged(bool value) => Preferences.Default.Set(DetectScreamKey, value);
    partial void OnDetectShoutChanged(bool value) => Preferences.Default.Set(DetectShoutKey, value);
    partial void OnDetectYellChanged(bool value) => Preferences.Default.Set(DetectYellKey, value);
    partial void OnDetectGlassChanged(bool value) => Preferences.Default.Set(DetectGlassKey, value);
    partial void OnDetectShatterChanged(bool value) => Preferences.Default.Set(DetectShatterKey, value);
    partial void OnDetectCryingSobbingChanged(bool value) => Preferences.Default.Set(DetectCryingSobbingKey, value);
    partial void OnDetectDomesticAnimalsPetsChanged(bool value) => Preferences.Default.Set(DetectDomesticAnimalsPetsKey, value);
    partial void OnDetectClickChanged(bool value) => Preferences.Default.Set(DetectClickKey, value);

    public bool IsCategoryEnabled(string? detectedType)
    {
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            return true;
        }

        var type = detectedType.Trim().ToLowerInvariant();

        if (type.Contains("speech") || type.Contains("talk") || type.Contains("voice") || type.Contains("parole"))
        {
            return DetectSpeech;
        }

        if (type.Contains("knock") || type.Contains("toc"))
        {
            return DetectKnock;
        }

        if (type.Contains("alarm") || type.Contains("siren") || type.Contains("alarme"))
        {
            return DetectAlarm;
        }

        if (type.Contains("door") || type.Contains("porte"))
        {
            return DetectDoor;
        }

        if (type.Contains("mechanic") || type.Contains("machine") || type.Contains("engine") || type.Contains("mécanique"))
        {
            return DetectMechanic;
        }

        if (type.Contains("fan") || type.Contains("ventilateur"))
        {
            return DetectFan;
        }

        if (type.Contains("tap") || type.Contains("robinet"))
        {
            return DetectTap;
        }

        if (type.Contains("wood") || type.Contains("coup"))
        {
            return DetectCoup;
        }

        if (type.Contains("scream") || type.Contains("cri aigu"))
        {
            return DetectScream;
        }

        if (type.Contains("shout") || type.Contains("cri fort"))
        {
            return DetectShout;
        }

        if (type.Contains("yell") || type.Contains("hurlement"))
        {
            return DetectYell;
        }

        if (type.Contains("glass") || type.Contains("verre"))
        {
            return DetectGlass;
        }

        if (type.Contains("shatter") || type.Contains("bris"))
        {
            return DetectShatter;
        }

        if (type.Contains("crying") || type.Contains("sobbing") || type.Contains("baby") || type.Contains("cry") || type.Contains("pleurs") || type.Contains("sanglots"))
        {
            return DetectCryingSobbing;
        }

        if (type.Contains("domestic animals") || type.Contains("pets") || type.Contains("pet") || type.Contains("animal") || type.Contains("animaux domestiques"))
        {
            return DetectDomesticAnimalsPets;
        }

        if (type.Contains("click") || type.Contains("clic"))
        {
            return DetectClick;
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
