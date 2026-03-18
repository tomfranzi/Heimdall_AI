namespace Heimdall_AI.Models;

public class Alertes
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titre { get; set; } = string.Empty;
    public string TypeDetection { get; set; } = "Inconnu";
    public string Description { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public string Niveau { get; set; } = "Info";
    public double Confiance { get; set; }

    public string HeureCourte => DateCreation.ToString("HH:mm:ss");
    public string ConfianceTexte => $"{Confiance:P0}";
    public string NiveauDbTexte => $"{-75 + (Confiance * 35):F1} dB";

    public bool EstAlerte
    {
        get
        {
            if (!string.Equals(Niveau, "Info", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var type = TypeDetection?.ToLowerInvariant() ?? string.Empty;
            return type.Contains("glass")
                   || type.Contains("break")
                   || type.Contains("smoke")
                   || type.Contains("alarm")
                   || type.Contains("shout")
                   || type.Contains("scream")
                   || type.Contains("intrusion")
                   || type.Contains("aggressive");
        }
    }

    public string TypeHistoriqueTexte => EstAlerte ? "Alert" : "Info";
    public Color CouleurHistoriqueBadge => EstAlerte ? Color.FromArgb("#EF4444") : Color.FromArgb("#3B82F6");
    public Color CouleurHistoriqueAccent => EstAlerte ? Color.FromArgb("#F43F5E") : Color.FromArgb("#22D3EE");
    public Color CouleurHistoriqueIconeFond => EstAlerte ? Color.FromArgb("#3B1B24") : Color.FromArgb("#14324A");
    public string ResumeHistorique => $"{DateCreation:hh:mm tt}  •  Confiance: {ConfianceTexte}";

    public string IconeHistorique => TypeDetection?.ToLowerInvariant() switch
    {
        var t when t.Contains("glass") || t.Contains("break") => "broken_image.svg",
        var t when t.Contains("baby") || t.Contains("cry") => "child_care.svg",
        var t when t.Contains("dog") || t.Contains("bark") || t.Contains("pet") || t.Contains("animal") => "pets.svg",
        var t when t.Contains("shout") || t.Contains("scream") || t.Contains("aggressive") => "brand_awareness.svg",
        var t when t.Contains("smoke") || t.Contains("alarm") || t.Contains("siren") => "detector_alarm.svg",
        _ => "shield_person.svg"
    };

    public Color CouleurBadge => Niveau switch
    {
        "Critique" => Color.FromArgb("#EF4444"),
        "Avertissement" => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#3B82F6")
    };
}