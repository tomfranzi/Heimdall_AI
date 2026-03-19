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
    public string DureeTexte { get; set; } = "1.0s";

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
                   || type.Contains("verre")
                   || type.Contains("bris")
                   || type.Contains("smoke")
                   || type.Contains("alarm")
                   || type.Contains("alarme")
                   || type.Contains("shout")
                   || type.Contains("scream")
                   || type.Contains("cri")
                   || type.Contains("hurlement")
                   || type.Contains("intrusion")
                   || type.Contains("aggressive");
        }
    }

    public string TypeHistoriqueTexte => EstAlerte ? "Alerte" : "Info";
    public Color CouleurHistoriqueAccent => TypeDetection?.ToLowerInvariant() switch
    {
        var t when t.Contains("pleurs") || t.Contains("sanglots") || t.Contains("cry") => Color.FromArgb("#F59E0B"),
        var t when t.Contains("porte") || t.Contains("knock") || t.Contains("toc") => Color.FromArgb("#14B8A6"),
        var t when t.Contains("alarme") || t.Contains("glass") || t.Contains("bris") || t.Contains("cri") || t.Contains("hurlement") => Color.FromArgb("#F43F5E"),
        _ => Color.FromArgb("#22D3EE")
    };

    public Color CouleurHistoriqueIconeFond => TypeDetection?.ToLowerInvariant() switch
    {
        var t when t.Contains("pleurs") || t.Contains("sanglots") || t.Contains("cry") => Color.FromArgb("#3A2A11"),
        var t when t.Contains("porte") || t.Contains("knock") || t.Contains("toc") => Color.FromArgb("#113A35"),
        var t when t.Contains("alarme") || t.Contains("glass") || t.Contains("bris") || t.Contains("cri") || t.Contains("hurlement") => Color.FromArgb("#3B1B24"),
        _ => Color.FromArgb("#14324A")
    };

    public string ResumeHistorique => $"{DateCreation:hh:mm tt}  •  Durée: {DureeTexte}";

    public string IconeHistorique => TypeDetection?.ToLowerInvariant() switch
    {
        var t when t.Contains("glass") || t.Contains("break") || t.Contains("verre") || t.Contains("bris") => "broken_image.svg",
        var t when t.Contains("parole") || t.Contains("speech") => "person.svg",
        var t when t.Contains("porte") || t.Contains("door") || t.Contains("knock") || t.Contains("toc") => "home.svg",
        var t when t.Contains("fan") || t.Contains("ventilateur") => "refresh.svg",
        var t when t.Contains("mechanic") || t.Contains("mécanique") => "analytics.svg",
        var t when t.Contains("clic") || t.Contains("click") => "check_box.svg",
        var t when t.Contains("baby") || t.Contains("cry") || t.Contains("pleurs") || t.Contains("sanglots") => "child_care.svg",
        var t when t.Contains("dog") || t.Contains("bark") || t.Contains("pet") || t.Contains("animal") => "pets.svg",
        var t when t.Contains("shout") || t.Contains("scream") || t.Contains("aggressive") || t.Contains("cri") || t.Contains("hurlement") => "brand_awareness.svg",
        var t when t.Contains("smoke") || t.Contains("alarm") || t.Contains("siren") || t.Contains("alarme") => "detector_alarm.svg",
        _ => "shield_person.svg"
    };

    public Color CouleurBadge => Niveau switch
    {
        "Critique" => Color.FromArgb("#EF4444"),
        "Avertissement" => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#3B82F6")
    };
}