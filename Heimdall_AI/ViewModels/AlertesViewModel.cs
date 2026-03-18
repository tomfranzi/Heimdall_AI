namespace Heimdall_AI.ViewModels;

public partial class AlertesViewModels : ObservableObject
{
    private const int NombreBarres = 8;

    private readonly IMqttAlertService _mqttAlertService;
    private readonly IAlertHistoryService _alertHistoryService;
    private readonly double[] _barresCibles = new double[NombreBarres];
    private readonly double[] _barresCourantes = new double[NombreBarres];
    private double _confianceCible;
    private double _phaseAnimation;
    private readonly IDispatcherTimer _animationTimer;

    public ObservableCollection<BarreConfianceViewModel> BarresConfiance { get; } = new();
    public ObservableCollection<Alertes> ActiviteRecente { get; } = new();

    [ObservableProperty]
    private Alertes? alerteActive;

    [ObservableProperty]
    private string statutSurveillance = "SURVEILLANCE ACTIVE";

    [ObservableProperty]
    private bool isRefreshing;

    public AlertesViewModels(IAlertHistoryService alertHistoryService, IMqttAlertService mqttAlertService)
    {
        _alertHistoryService = alertHistoryService;
        _mqttAlertService = mqttAlertService;

        for (var i = 0; i < NombreBarres; i++)
        {
            BarresConfiance.Add(new BarreConfianceViewModel());
            _barresCibles[i] = 0.12;
            _barresCourantes[i] = 0.12;
        }

        foreach (var alerte in _alertHistoryService.Historique.Take(4))
        {
            ActiviteRecente.Add(alerte);
        }

        AlerteActive = _alertHistoryService.AlerteActive;
        if (AlerteActive is not null)
        {
            var confianceInitiale = NormalizeConfiance(AlerteActive.Confiance);
            AlerteActive.Confiance = confianceInitiale;
            _confianceCible = confianceInitiale;

            for (var i = 0; i < NombreBarres; i++)
            {
                _barresCibles[i] = confianceInitiale;
                _barresCourantes[i] = confianceInitiale;
            }
        }

        _alertHistoryService.AlerteActiveRecue += OnAlerteActiveRecue;

        _animationTimer = Application.Current?.Dispatcher.CreateTimer() ?? throw new InvalidOperationException("Dispatcher indisponible.");
        _animationTimer.Interval = TimeSpan.FromMilliseconds(50);
        _animationTimer.Tick += (_, _) => AnimerBarres();
        _animationTimer.Start();

        _ = _mqttAlertService.StartAsync();
    }

    public string TitreAlerteCourante => AlerteActive?.Titre ?? "En attente d'une alerte";
    public string TypeAlerteCourante => AlerteActive is null ? "Aucune détection reçue" : $"Type détecté : {AlerteActive.TypeDetection}";
    public string NiveauConfianceCourant => AlerteActive?.ConfianceTexte ?? "0%";
    public string NiveauDbCourant => AlerteActive?.NiveauDbTexte ?? "-75.0 dB";

    [RelayCommand]
    private async Task LoadAlertesAsync()
    {
        IsRefreshing = true;
        await _mqttAlertService.StartAsync();
        await Task.Delay(250);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task AlerteTappedAsync(Alertes alerteSelected)
    {
        if (alerteSelected == null)
        {
            return;
        }

        if (Shell.Current.CurrentPage is Page page)
        {
            await page.DisplayAlertAsync(alerteSelected.Titre, alerteSelected.Description, "Fermer");
        }
    }

    partial void OnAlerteActiveChanged(Alertes? value)
    {
        OnPropertyChanged(nameof(TitreAlerteCourante));
        OnPropertyChanged(nameof(TypeAlerteCourante));
        OnPropertyChanged(nameof(NiveauConfianceCourant));
        OnPropertyChanged(nameof(NiveauDbCourant));
    }

    private void OnAlerteActiveRecue(Alertes alerte)
    {
        var confiance = NormalizeConfiance(alerte.Confiance);
        alerte.Confiance = confiance;

        AlerteActive = alerte;
        _confianceCible = confiance;
        InjecterConfianceDansBarres(confiance);

        ActiviteRecente.Insert(0, alerte);
        while (ActiviteRecente.Count > 4)
        {
            ActiviteRecente.RemoveAt(ActiviteRecente.Count - 1);
        }
    }

    private void AnimerBarres()
    {
        _phaseAnimation += 0.12;

        for (var i = 0; i < BarresConfiance.Count; i++)
        {
            _barresCourantes[i] += (_barresCibles[i] - _barresCourantes[i]) * 0.22;

            var microMouvement = Math.Sin(_phaseAnimation + i * 0.35) * 0.02 * (0.3 + _confianceCible);
            var valeur = Math.Clamp(_barresCourantes[i] + microMouvement, 0, 1);

            BarresConfiance[i].Hauteur = 20 + (valeur * 120);
            BarresConfiance[i].Opacite = 0.35 + (valeur * 0.65);
        }
    }

    private void InjecterConfianceDansBarres(double confiance)
    {
        for (var i = 0; i < NombreBarres - 1; i++)
        {
            _barresCibles[i] = _barresCibles[i + 1];
        }

        _barresCibles[NombreBarres - 1] = confiance;
    }

    private static double NormalizeConfiance(double confiance)
    {
        if (confiance > 1)
        {
            confiance /= 100d;
        }

        return Math.Clamp(confiance, 0, 1);
    }
}

public partial class BarreConfianceViewModel : ObservableObject
{
    [ObservableProperty]
    private double hauteur = 18;

    [ObservableProperty]
    private double opacite = 0.35;
}
