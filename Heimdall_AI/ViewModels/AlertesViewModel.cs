namespace Heimdall_AI.ViewModels;

public partial class AlertesViewModels : ObservableObject
{
    // On utilise bien le modèle "Alertes"
    public ObservableCollection<Alertes> ListeAlertes { get; } = new();

    [ObservableProperty]
    private bool isRefreshing;

    public AlertesViewModels()
    {
        _ = LoadAlertesAsync();
    }

    [RelayCommand]
    private async Task LoadAlertesAsync()
    {
        IsRefreshing = true;
        await Task.Delay(1500);
        ListeAlertes.Clear();

        ListeAlertes.Add(new Alertes { Titre = "Intrusion détectée", Description = "Mouvement suspect au secteur Nord.", Niveau = "Critique", DateCreation = DateTime.Now.AddMinutes(-2) });
        ListeAlertes.Add(new Alertes { Titre = "Capteur hors ligne", Description = "Le capteur de température de la baie 3 ne répond plus.", Niveau = "Avertissement", DateCreation = DateTime.Now.AddHours(-1) });
        ListeAlertes.Add(new Alertes { Titre = "Mise à jour système", Description = "La mise à jour de l'agent Heimdall a réussi.", Niveau = "Info", DateCreation = DateTime.Now.AddDays(-1) });

        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task AlerteTappedAsync(Alertes alerteSelected)
    {
        if (alerteSelected == null) return;
        await Shell.Current.DisplayAlert(alerteSelected.Titre, alerteSelected.Description, "Fermer");
    }
}