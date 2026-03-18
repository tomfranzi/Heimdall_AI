using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Heimdall_AI.Services;

public interface ILocalAuthService
{
    Task<bool> CreerCompteAsync(string nomUtilisateur, string motDePasse);
    Task<bool> VerifierConnexionAsync(string identifiant, string motDePasse);
    Task<string?> GetDernierUtilisateurAsync();
    Task SetDernierUtilisateurAsync(string nomUtilisateur);
}

public sealed class LocalAuthService : ILocalAuthService
{
    private const string ComptesKey = "heimdall_auth_comptes";
    private const string DernierUtilisateurKey = "heimdall_auth_dernier_utilisateur";

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

    public Task<string?> GetDernierUtilisateurAsync() => SecureStorage.Default.GetAsync(DernierUtilisateurKey);

    public Task SetDernierUtilisateurAsync(string nomUtilisateur) =>
        SecureStorage.Default.SetAsync(DernierUtilisateurKey, (nomUtilisateur ?? string.Empty).Trim());

    private static string HasherMotDePasse(string motDePasse, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes($"{salt}|{motDePasse}");
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static async Task<List<CompteLocal>> ChargerComptesAsync()
    {
        var json = await SecureStorage.Default.GetAsync(ComptesKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CompteLocal>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task SauvegarderComptesAsync(List<CompteLocal> comptes)
    {
        var json = JsonSerializer.Serialize(comptes);
        await SecureStorage.Default.SetAsync(ComptesKey, json);
    }

    private sealed class CompteLocal
    {
        public string Id { get; set; } = string.Empty;
        public string NomUtilisateur { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime DateCreationUtc { get; set; }
    }
}
