using Microsoft.Extensions.DependencyInjection;

namespace Heimdall_AI.Views;

public partial class LoginPage : ContentPage
{
    private ILocalAuthService AuthService =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ILocalAuthService>()
        ?? throw new InvalidOperationException("Service d'authentification indisponible.");

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var dernierUtilisateur = await AuthService.GetDernierUtilisateurAsync();
            if (!string.IsNullOrWhiteSpace(dernierUtilisateur))
            {
                UsernameEntry.Text = dernierUtilisateur;
            }
        }
        catch
        {
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var nomUtilisateur = UsernameEntry.Text?.Trim() ?? string.Empty;
        var motDePasse = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomUtilisateur) || string.IsNullOrWhiteSpace(motDePasse))
        {
            await DisplayAlert("Connexion", "Veuillez saisir votre nom utilisateur et votre mot de passe.", "OK");
            return;
        }

        bool authentifie;
        try
        {
            authentifie = await AuthService.VerifierConnexionAsync(nomUtilisateur, motDePasse);
        }
        catch
        {
            await DisplayAlert("Connexion", "Impossible de vérifier les informations de connexion.", "OK");
            return;
        }

        if (!authentifie)
        {
            await DisplayAlert("Connexion", "Nom utilisateur ou mot de passe invalide.", "OK");
            return;
        }

        await AuthService.SetDernierUtilisateurAsync(nomUtilisateur);

        await Shell.Current.GoToAsync("//SupervisionPage");
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CreateAccountPage));
    }
}