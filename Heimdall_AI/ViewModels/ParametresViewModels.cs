namespace Heimdall_AI.ViewModels;

public partial class ParametresViewModels : ObservableObject
{
    private readonly IListeningSettingsService _settings;
    private readonly IMqttAlertService _mqttAlertService;
    private readonly ILocalAuthService _localAuthService;
    private bool _initialisation;
    private CancellationTokenSource? _publishSensitivityCts;

    [ObservableProperty]
    private bool isListeningEnabled;

    [ObservableProperty]
    private double sensibiliteMicro;

    [ObservableProperty]
    private double seuilConfiance;

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

    [ObservableProperty]
    private string nomUtilisateurCompte = string.Empty;

    [ObservableProperty]
    private string nouveauNomUtilisateur = string.Empty;

    [ObservableProperty]
    private string nouveauMotDePasse = string.Empty;

    public ParametresViewModels(IListeningSettingsService settings, IMqttAlertService mqttAlertService, ILocalAuthService localAuthService)
    {
        _settings = settings;
        _mqttAlertService = mqttAlertService;
        _localAuthService = localAuthService;

        _initialisation = true;

        IsListeningEnabled = settings.IsListeningEnabled;
        SensibiliteMicro = settings.MicroSensitivity;
        SeuilConfiance = settings.MinimumConfidence;
        DetectSpeech = settings.DetectSpeech;
        DetectKnock = settings.DetectKnock;
        DetectAlarm = settings.DetectAlarm;
        DetectDoor = settings.DetectDoor;
        DetectMechanic = settings.DetectMechanic;
        DetectFan = settings.DetectFan;
        DetectTap = settings.DetectTap;
        DetectCoup = settings.DetectCoup;
        DetectScream = settings.DetectScream;
        DetectShout = settings.DetectShout;
        DetectYell = settings.DetectYell;
        DetectGlass = settings.DetectGlass;
        DetectShatter = settings.DetectShatter;
        DetectCryingSobbing = settings.DetectCryingSobbing;
        DetectDomesticAnimalsPets = settings.DetectDomesticAnimalsPets;
        DetectClick = settings.DetectClick;

        _initialisation = false;

        _ = ChargerCompteAsync();
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
    partial void OnDetectSpeechChanged(bool value) => _settings.DetectSpeech = value;
    partial void OnDetectKnockChanged(bool value) => _settings.DetectKnock = value;
    partial void OnDetectAlarmChanged(bool value) => _settings.DetectAlarm = value;
    partial void OnDetectDoorChanged(bool value) => _settings.DetectDoor = value;
    partial void OnDetectMechanicChanged(bool value) => _settings.DetectMechanic = value;
    partial void OnDetectFanChanged(bool value) => _settings.DetectFan = value;
    partial void OnDetectTapChanged(bool value) => _settings.DetectTap = value;
    partial void OnDetectCoupChanged(bool value) => _settings.DetectCoup = value;
    partial void OnDetectScreamChanged(bool value) => _settings.DetectScream = value;
    partial void OnDetectShoutChanged(bool value) => _settings.DetectShout = value;
    partial void OnDetectYellChanged(bool value) => _settings.DetectYell = value;
    partial void OnDetectGlassChanged(bool value) => _settings.DetectGlass = value;
    partial void OnDetectShatterChanged(bool value) => _settings.DetectShatter = value;
    partial void OnDetectCryingSobbingChanged(bool value) => _settings.DetectCryingSobbing = value;
    partial void OnDetectDomesticAnimalsPetsChanged(bool value) => _settings.DetectDomesticAnimalsPets = value;
    partial void OnDetectClickChanged(bool value) => _settings.DetectClick = value;

    [RelayCommand]
    private async Task MettreAJourCompteAsync()
    {
        var actuel = NomUtilisateurCompte.Trim();
        var nouveauNom = string.IsNullOrWhiteSpace(NouveauNomUtilisateur) ? actuel : NouveauNomUtilisateur.Trim();
        var nouveauMdp = NouveauMotDePasse;

        if (string.IsNullOrWhiteSpace(actuel) || string.IsNullOrWhiteSpace(nouveauNom))
        {
            await AfficherMessageAsync("Compte", "Nom utilisateur invalide.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(nouveauMdp) && nouveauMdp.Length < 6)
        {
            await AfficherMessageAsync("Compte", "Le nouveau mot de passe doit contenir au moins 6 caractères.");
            return;
        }

        var ok = await _localAuthService.MettreAJourCompteAsync(actuel, nouveauNom, nouveauMdp);
        if (!ok)
        {
            await AfficherMessageAsync("Compte", "Impossible de mettre à jour le compte (nom déjà utilisé ?).");
            return;
        }

        NomUtilisateurCompte = nouveauNom;
        NouveauNomUtilisateur = string.Empty;
        NouveauMotDePasse = string.Empty;
        await AfficherMessageAsync("Compte", "Compte mis à jour.");
    }

    [RelayCommand]
    private async Task SupprimerCompteAsync()
    {
        var actuel = NomUtilisateurCompte.Trim();
        if (string.IsNullOrWhiteSpace(actuel))
        {
            await AfficherMessageAsync("Compte", "Aucun compte à supprimer.");
            return;
        }

        if (Shell.Current?.CurrentPage is not Page confirmationPage)
        {
            return;
        }

        var confirmer = await confirmationPage.DisplayAlert("Supprimer le compte", "Voulez-vous vraiment supprimer ce compte ?", "Oui", "Non");
        if (!confirmer)
        {
            return;
        }

        var ok = await _localAuthService.SupprimerCompteAsync(actuel);
        if (!ok)
        {
            await AfficherMessageAsync("Compte", "Suppression impossible.");
            return;
        }

        NomUtilisateurCompte = string.Empty;
        NouveauNomUtilisateur = string.Empty;
        NouveauMotDePasse = string.Empty;
        await AfficherMessageAsync("Compte", "Compte supprimé.");
    }

    [RelayCommand]
    private async Task DeconnexionAsync()
    {
        _localAuthService.SetSessionActive(false);
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async Task ChargerCompteAsync()
    {
        NomUtilisateurCompte = await _localAuthService.GetDernierUtilisateurAsync() ?? string.Empty;
    }

    private static Task AfficherMessageAsync(string titre, string message)
    {
        if (Shell.Current?.CurrentPage is not Page page)
        {
            return Task.CompletedTask;
        }

        return page.DisplayAlertAsync(titre, message, "OK");
    }
}
