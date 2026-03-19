using Microsoft.Extensions.DependencyInjection;

namespace Heimdall_AI.Views;

public partial class LoginPage : ContentPage
{
    private ILocalAuthService AuthService =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<ILocalAuthService>()
        ?? throw new InvalidOperationException("Service d'authentification indisponible.");

    private IBiometricAuthService BiometricAuthService =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IBiometricAuthService>()
        ?? throw new InvalidOperationException("Service biométrique indisponible.");

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

                if (AuthService.IsSessionActive())
                {
                    await Shell.Current.GoToAsync("//SupervisionPage");
                }
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
        AuthService.SetSessionActive(true);

        await Shell.Current.GoToAsync("//SupervisionPage");
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CreateAccountPage));
    }

    private async void OnFaceIdClicked(object sender, TappedEventArgs e)
    {
        await ConnexionBiometriqueAsync("Face ID");
    }

    private async void OnFingerprintClicked(object sender, TappedEventArgs e)
    {
        await ConnexionBiometriqueAsync("Empreinte");
    }

    private async Task ConnexionBiometriqueAsync(string mode)
    {
        var nomUtilisateur = UsernameEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nomUtilisateur))
        {
            await DisplayAlert("Connexion biométrique", "Saisissez votre nom utilisateur avant d'utiliser la biométrie.", "OK");
            return;
        }

        if (!await AuthService.UtilisateurExisteAsync(nomUtilisateur))
        {
            await DisplayAlert("Connexion biométrique", "Ce compte n'existe pas encore localement.", "OK");
            return;
        }

        if (!await BiometricAuthService.IsAvailableAsync())
        {
            await DisplayAlert("Connexion biométrique", "La biométrie n'est pas disponible sur cet appareil.", "OK");
            return;
        }

        var ok = await BiometricAuthService.AuthenticateAsync("Heimdall", $"Authentification {mode}");
        if (!ok)
        {
            await DisplayAlert("Connexion biométrique", "Authentification annulée ou échouée.", "OK");
            return;
        }

        await AuthService.SetDernierUtilisateurAsync(nomUtilisateur);
        AuthService.SetSessionActive(true);
        await Shell.Current.GoToAsync("//SupervisionPage");
    }
}