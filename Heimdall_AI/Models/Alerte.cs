namespace Heimdall_AI.Models;

public class Alertes
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public string Niveau { get; set; } = "Info";

    public Color CouleurBadge => Niveau switch
    {
        "Critique" => Color.FromArgb("#EF4444"),
        "Avertissement" => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#3B82F6")
    };
}