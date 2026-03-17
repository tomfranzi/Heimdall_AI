namespace Heimdall_AI.ViewModels;

public partial class SupervisionViewModels : ObservableObject
{
    [ObservableProperty]
    private string statutSysteme = "Opérationnel";

    [ObservableProperty]
    private Color couleurStatut = Color.FromArgb("#10B981"); // Vert émeraude

    [ObservableProperty]
    private string derniereMiseAJour = "il y a 2 minutes";

    [ObservableProperty]
    private int capteursActifs = 14;

    [ObservableProperty]
    private int capteursTotal = 15;

    // Pour la barre de progression (valeur entre 0 et 1)
    [ObservableProperty]
    private double progressionCapteurs = 14.0 / 15.0;

    [ObservableProperty]
    private string nombreAlertes = "3";

    [ObservableProperty]
    private string tendanceAlertes = "+12%";

    [ObservableProperty]
    private bool isRefreshing;

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
        await Task.Delay(1000); // Simulation réseau
        DerniereMiseAJour = "à l'instant";
        IsRefreshing = false;
    }
}