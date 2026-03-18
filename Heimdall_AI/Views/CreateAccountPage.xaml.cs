using Microsoft.Extensions.DependencyInjection;

namespace Heimdall_AI.Views;

public partial class CreateAccountPage : ContentPage
{
    private ILocalAuthService AuthService =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ILocalAuthService>()
        ?? throw new InvalidOperationException("Service d'authentification indisponible.");

    public CreateAccountPage()
    {
        InitializeComponent();
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        var nomUtilisateur = UsernameEntry.Text?.Trim() ?? string.Empty;
        var motDePasse = PasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomUtilisateur)
            || string.IsNullOrWhiteSpace(motDePasse) || string.IsNullOrWhiteSpace(confirmation))
        {
            await DisplayAlert("Créer un compte", "Veuillez remplir tous les champs.", "OK");
            return;
        }

        if (motDePasse.Length < 6)
        {
            await DisplayAlert("Créer un compte", "Le mot de passe doit contenir au moins 6 caractères.", "OK");
            return;
        }

        if (!string.Equals(motDePasse, confirmation, StringComparison.Ordinal))
        {
            await DisplayAlert("Créer un compte", "Les mots de passe ne correspondent pas.", "OK");
            return;
        }

        bool cree;
        try
        {
            cree = await AuthService.CreerCompteAsync(nomUtilisateur, motDePasse);
        }
        catch
        {
            await DisplayAlert("Créer un compte", "Impossible d'enregistrer le compte en local.", "OK");
            return;
        }

        if (!cree)
        {
            await DisplayAlert("Créer un compte", "Un compte avec ce nom utilisateur existe déjà.", "OK");
            return;
        }

        await AuthService.SetDernierUtilisateurAsync(nomUtilisateur);
        await DisplayAlert("Créer un compte", "Compte créé avec succès.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
