namespace Heimdall_AI.ViewModels;

public partial class SupervisionViewModels : ObservableObject
{
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly IListeningSettingsService _listeningSettingsService;

    [ObservableProperty]
    private string statutSysteme = "Hors ligne";

    [ObservableProperty]
    private Color couleurStatut = Color.FromArgb("#EF4444");

    [ObservableProperty]
    private string detailStatutSysteme = "Micro non connecté";

    [ObservableProperty]
    private string temperatureRaspberry = "--.-°C";

    [ObservableProperty]
    private string detailTemperature = "En attente de données";

    [ObservableProperty]
    private string derniereMiseAJour = "il y a 2 minutes";

    [ObservableProperty]
    private string nombreAlertes = "3";

    [ObservableProperty]
    private string tendanceAlertes = "+12%";

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool microActif;

    [ObservableProperty]
    private bool modeAlerteActif;

    public string LibelleBoutonMicro => MicroActif ? "Micro activé" : "Micro désactivé";
    public string LibelleBoutonModeAlerte => ModeAlerteActif ? "Mode alerte ON" : "Mode alerte OFF";
    public string IconeBoutonMicro => MicroActif ? "toggle_on.svg" : "close.svg";
    public string IconeBoutonModeAlerte => ModeAlerteActif ? "detector_alarm.svg" : "close.svg";
    public Color CouleurBoutonMicro => MicroActif ? Color.FromArgb("#0B5F48") : Color.FromArgb("#3F1D20");
    public Color CouleurBoutonModeAlerte => ModeAlerteActif ? Color.FromArgb("#7F1D1D") : Color.FromArgb("#1E293B");
    public string DescriptionModeAlerte => ModeAlerteActif
        ? "Chaque bruit détecté déclenche une alerte immédiate."
        : "Les filtres MQTT s'appliquent (catégories + confiance).";

    public SupervisionViewModels(
        IDeviceStatusService deviceStatusService,
        IListeningSettingsService listeningSettingsService,
        IMqttAlertService mqttAlertService)
    {
        _deviceStatusService = deviceStatusService;
        _listeningSettingsService = listeningSettingsService;

        MicroActif = _listeningSettingsService.IsListeningEnabled;
        ModeAlerteActif = _listeningSettingsService.IsAlertModeEnabled;

        _deviceStatusService.StatusUpdated += OnStatusUpdated;

        if (_deviceStatusService.CurrentStatus is not null)
        {
            AppliquerStatus(_deviceStatusService.CurrentStatus);
        }

        _ = mqttAlertService.StartAsync();
    }

    // --- Commandes pour les Actions Rapides ---
    [RelayCommand]
    private async Task ScanManuelAsync() => await Shell.Current.DisplayAlert("Scan", "Lancement du scan manuel en cours...", "Fermer");

    [RelayCommand]
    private async Task GenererRapportAsync() => await Shell.Current.DisplayAlert("Rapport", "Génération du rapport d'activité...", "Fermer");

    [RelayCommand]
    private async Task OuvrirParametresAsync() => await Shell.Current.DisplayAlert("Paramètres", "Ouverture des paramètres de supervision...", "Fermer");

    [RelayCommand]
    private async Task DeclencherUrgenceAsync() => await Shell.Current.DisplayAlert("URGENCE", "Procédure de verrouillage activée !", "Compris");

    [RelayCommand]
    private async Task RefreshDonneesAsync()
    {
        IsRefreshing = true;
        await Task.Delay(300);
        DerniereMiseAJour = "à l'instant";
        IsRefreshing = false;
    }

    private void OnStatusUpdated(DeviceStatusInfo status)
    {
        MainThread.BeginInvokeOnMainThread(() => AppliquerStatus(status));
    }

    private void AppliquerStatus(DeviceStatusInfo status)
    {
        var isOnline = string.Equals(status.Status, "online", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status.Status, "operational", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status.Status, "ok", StringComparison.OrdinalIgnoreCase);

        StatutSysteme = isOnline ? "Opérationnel" : "Hors ligne";
        CouleurStatut = isOnline ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
        DetailStatutSysteme = isOnline
            ? $"Micro actif • en ligne depuis {status.Uptime}"
            : "Micro non disponible";

        TemperatureRaspberry = string.IsNullOrWhiteSpace(status.CpuTemp) ? "--.-°C" : status.CpuTemp;
        DetailTemperature = isOnline ? "Température Raspberry Pi" : "Données indisponibles";
    }

    partial void OnMicroActifChanged(bool value)
    {
        _listeningSettingsService.IsListeningEnabled = value;
        OnPropertyChanged(nameof(LibelleBoutonMicro));
        OnPropertyChanged(nameof(IconeBoutonMicro));
        OnPropertyChanged(nameof(CouleurBoutonMicro));
    }

    partial void OnModeAlerteActifChanged(bool value)
    {
        _listeningSettingsService.IsAlertModeEnabled = value;
        OnPropertyChanged(nameof(LibelleBoutonModeAlerte));
        OnPropertyChanged(nameof(IconeBoutonModeAlerte));
        OnPropertyChanged(nameof(CouleurBoutonModeAlerte));
        OnPropertyChanged(nameof(DescriptionModeAlerte));
    }

    [RelayCommand]
    private void ToggleMicro()
    {
        MicroActif = !MicroActif;
    }

    [RelayCommand]
    private void ToggleModeAlerte()
    {
        ModeAlerteActif = !ModeAlerteActif;
    }
}