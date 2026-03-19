using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Heimdall_AI.Services;

public interface ILocalAuthService
{
    Task<bool> CreerCompteAsync(string nomUtilisateur, string motDePasse);
    Task<bool> VerifierConnexionAsync(string identifiant, string motDePasse);
    Task<bool> UtilisateurExisteAsync(string identifiant);
    Task<bool> MettreAJourCompteAsync(string nomUtilisateurActuel, string nouveauNomUtilisateur, string? nouveauMotDePasse);
    Task<bool> SupprimerCompteAsync(string nomUtilisateur);
    Task<string?> GetDernierUtilisateurAsync();
    Task SetDernierUtilisateurAsync(string nomUtilisateur);
    bool IsSessionActive();
    void SetSessionActive(bool active);
}

public sealed class LocalAuthService : ILocalAuthService
{
    private const string AuthFileName = "auth_db.json";
    private const string SessionActiveKey = "heimdall_auth_session_active";

    private readonly IFileDatabaseService _fileDb;

    public LocalAuthService(IFileDatabaseService fileDb)
    {
        _fileDb = fileDb;
    }

    public async Task<bool> CreerCompteAsync(string nomUtilisateur, string motDePasse)
    {
        var username = (nomUtilisateur ?? string.Empty).Trim();
        var password = motDePasse ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var comptes = await ChargerComptesAsync();

        var existeDeja = comptes.Any(c =>
            string.Equals(c.NomUtilisateur, username, StringComparison.OrdinalIgnoreCase));

        if (existeDeja)
        {
            return false;
        }

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var hash = HasherMotDePasse(password, salt);

        comptes.Add(new CompteLocal
        {
            Id = Guid.NewGuid().ToString(),
            NomUtilisateur = username,
            PasswordSalt = salt,
            PasswordHash = hash,
            DateCreationUtc = DateTime.UtcNow
        });

        await SauvegarderComptesAsync(comptes);
        return true;
    }

    public async Task<bool> VerifierConnexionAsync(string identifiant, string motDePasse)
    {
        var id = (identifiant ?? string.Empty).Trim();
        var password = motDePasse ?? string.Empty;

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var comptes = await ChargerComptesAsync();

        var compte = comptes.FirstOrDefault(c =>
            string.Equals(c.NomUtilisateur, id, StringComparison.OrdinalIgnoreCase));

        if (compte is null)
        {
            return false;
        }

        var hashSaisi = HasherMotDePasse(password, compte.PasswordSalt);
        return string.Equals(hashSaisi, compte.PasswordHash, StringComparison.Ordinal);
    }

    public async Task<bool> UtilisateurExisteAsync(string identifiant)
    {
        var id = (identifiant ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var comptes = await ChargerComptesAsync();
        return comptes.Any(c => string.Equals(c.NomUtilisateur, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> MettreAJourCompteAsync(string nomUtilisateurActuel, string nouveauNomUtilisateur, string? nouveauMotDePasse)
    {
        var actuel = (nomUtilisateurActuel ?? string.Empty).Trim();
        var nouveau = (nouveauNomUtilisateur ?? string.Empty).Trim();
        var nouveauMdp = nouveauMotDePasse ?? string.Empty;

        if (string.IsNullOrWhiteSpace(actuel) || string.IsNullOrWhiteSpace(nouveau))
        {
            return false;
        }

        var comptes = await ChargerComptesAsync();
        var compte = comptes.FirstOrDefault(c => string.Equals(c.NomUtilisateur, actuel, StringComparison.OrdinalIgnoreCase));
        if (compte is null)
        {
            return false;
        }

        var dejaPris = comptes.Any(c =>
            !string.Equals(c.Id, compte.Id, StringComparison.Ordinal)
            && string.Equals(c.NomUtilisateur, nouveau, StringComparison.OrdinalIgnoreCase));

        if (dejaPris)
        {
            return false;
        }

        compte.NomUtilisateur = nouveau;
        if (!string.IsNullOrWhiteSpace(nouveauMdp))
        {
            var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            compte.PasswordSalt = salt;
            compte.PasswordHash = HasherMotDePasse(nouveauMdp, salt);
        }

        await SauvegarderComptesAsync(comptes);
        await SetDernierUtilisateurAsync(nouveau);
        return true;
    }

    public async Task<bool> SupprimerCompteAsync(string nomUtilisateur)
    {
        var nom = (nomUtilisateur ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nom))
        {
            return false;
        }

        var comptes = await ChargerComptesAsync();
        var cible = comptes.FirstOrDefault(c => string.Equals(c.NomUtilisateur, nom, StringComparison.OrdinalIgnoreCase));
        if (cible is null)
        {
            return false;
        }

        comptes.Remove(cible);
        await SauvegarderComptesAsync(comptes);

        var dernier = await GetDernierUtilisateurAsync();
        if (string.Equals(dernier, nom, StringComparison.OrdinalIgnoreCase))
        {
            await SetDernierUtilisateurAsync(string.Empty);
            SetSessionActive(false);
        }

        return true;
    }

    public async Task<string?> GetDernierUtilisateurAsync()
    {
        var db = await ChargerDbAsync();
        return db.DernierUtilisateur;
    }

    public async Task SetDernierUtilisateurAsync(string nomUtilisateur)
    {
        var db = await ChargerDbAsync();
        db.DernierUtilisateur = (nomUtilisateur ?? string.Empty).Trim();
        await SauvegarderDbAsync(db);
    }

    public bool IsSessionActive() => Preferences.Default.Get(SessionActiveKey, false);

    public void SetSessionActive(bool active) => Preferences.Default.Set(SessionActiveKey, active);

    private static string HasherMotDePasse(string motDePasse, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes($"{salt}|{motDePasse}");
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private async Task<List<CompteLocal>> ChargerComptesAsync()
    {
        var db = await ChargerDbAsync();
        return db.Comptes;
    }

    private async Task SauvegarderComptesAsync(List<CompteLocal> comptes)
    {
        var db = await ChargerDbAsync();
        db.Comptes = comptes;
        await SauvegarderDbAsync(db);
    }

    private Task<AuthStorageDto> ChargerDbAsync()
    {
        return _fileDb.LoadAsync(AuthFileName, new AuthStorageDto());
    }

    private Task SauvegarderDbAsync(AuthStorageDto db)
    {
        return _fileDb.SaveAsync(AuthFileName, db);
    }

    private sealed class CompteLocal
    {
        public string Id { get; set; } = string.Empty;
        public string NomUtilisateur { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime DateCreationUtc { get; set; }
    }

    private sealed class AuthStorageDto
    {
        public List<CompteLocal> Comptes { get; set; } = [];

        [JsonPropertyName("dernier_utilisateur")]
        public string DernierUtilisateur { get; set; } = string.Empty;
    }
}
