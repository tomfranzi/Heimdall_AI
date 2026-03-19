namespace Heimdall_AI.ViewModels;

public partial class HistoriqueViewModels : ObservableObject
{
    private readonly ObservableCollection<Alertes> _sourceAlertes;

    public ObservableCollection<Alertes> ListeAlertesHistoriques { get; } = new();
    public ObservableCollection<FiltreHistoriqueOption> Filtres { get; } = new();

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string filtreSelectionne = "Toutes";

    public HistoriqueViewModels(IAlertHistoryService alertHistoryService)
    {
        _sourceAlertes = alertHistoryService.Historique;

        ConstruireFiltres();
        AppliquerFiltre();

        _sourceAlertes.CollectionChanged += (_, _) =>
        {
            MettreAJourFiltresDynamiques();
            AppliquerFiltre();
        };
    }

    [RelayCommand]
    private async Task RafraichirAsync()
    {
        IsRefreshing = true;
        await Task.Delay(250);
        AppliquerFiltre();
        IsRefreshing = false;
    }

    [RelayCommand]
    private void SelectionnerFiltre(FiltreHistoriqueOption? filtre)
    {
        if (filtre is null)
        {
            return;
        }

        FiltreSelectionne = filtre.Libelle;

        foreach (var item in Filtres)
        {
            item.IsSelected = string.Equals(item.Libelle, FiltreSelectionne, StringComparison.OrdinalIgnoreCase);
        }

        AppliquerFiltre();
    }

    [RelayCommand]
    private async Task AlerteTappedAsync(Alertes alerteSelected)
    {
        if (alerteSelected is null)
        {
            return;
        }

        if (Shell.Current.CurrentPage is Page page)
        {
            await page.DisplayAlertAsync(alerteSelected.Titre, alerteSelected.Description, "Fermer");
        }
    }

    private void ConstruireFiltres()
    {
        var filtresFixes = new[]
        {
            "Toutes",
            "Alerte",
            "Parole",
            "Toc à la porte",
            "Alarme",
            "Porte",
            "Mécanique",
            "Ventilateur",
            "Frappe légère",
            "Coup",
            "Cri aigu",
            "Cri fort",
            "Hurlement",
            "Verre",
            "Bris",
            "Pleurs, sanglots",
            "Animaux domestiques",
            "Clic"
        };

        Filtres.Clear();
        foreach (var filtre in filtresFixes)
        {
            Filtres.Add(new FiltreHistoriqueOption
            {
                Libelle = filtre,
                IsSelected = string.Equals(filtre, FiltreSelectionne, StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    private void MettreAJourFiltresDynamiques()
    {
        var typesDynamiques = _sourceAlertes
            .Select(a => a.TypeDetection?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var type in typesDynamiques)
        {
            if (Filtres.Any(f => string.Equals(f.Libelle, type, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Filtres.Add(new FiltreHistoriqueOption
            {
                Libelle = type!,
                IsSelected = false
            });
        }
    }

    private void AppliquerFiltre()
    {
        IEnumerable<Alertes> resultat = _sourceAlertes.OrderByDescending(a => a.DateCreation);

        if (!string.Equals(FiltreSelectionne, "Toutes", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(FiltreSelectionne, "Alerte", StringComparison.OrdinalIgnoreCase))
            {
                resultat = resultat.Where(a => a.EstAlerte);
            }
            else if (string.Equals(FiltreSelectionne, "Info", StringComparison.OrdinalIgnoreCase))
            {
                resultat = resultat.Where(a => !a.EstAlerte);
            }
            else
            {
                resultat = resultat.Where(a => string.Equals(a.TypeDetection, FiltreSelectionne, StringComparison.OrdinalIgnoreCase));
            }
        }

        ListeAlertesHistoriques.Clear();
        foreach (var alerte in resultat)
        {
            ListeAlertesHistoriques.Add(alerte);
        }
    }

    public partial class FiltreHistoriqueOption : ObservableObject
    {
        public string Libelle { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isSelected;
    }
}
