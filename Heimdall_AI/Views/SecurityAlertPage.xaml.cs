using Microsoft.Extensions.DependencyInjection;

namespace Heimdall_AI.Views;

public partial class SecurityAlertPage : ContentPage, IQueryAttributable
{
    private INativeAlertService NativeAlertService =>
        Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<INativeAlertService>()
        ?? throw new InvalidOperationException("Service d'alerte natif indisponible.");

    public SecurityAlertPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var type = LireValeur(query, "type");
        var location = LireValeur(query, "location");
        var timestamp = LireValeur(query, "timestamp");

        TypeLabel.Text = string.IsNullOrWhiteSpace(type) ? "SON INCONNU" : type.ToUpperInvariant();
        LocationLabel.Text = string.IsNullOrWhiteSpace(location) ? "Zone principale" : location;
        TimeLabel.Text = string.IsNullOrWhiteSpace(timestamp) ? "À l'instant" : timestamp;
    }

    private static string? LireValeur(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : Uri.UnescapeDataString(text);
    }

    private async void OnListenLiveClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Écoute en direct", "Ouverture de l'écoute en direct...", "OK");
    }

    private async void OnCallEmergencyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Urgence", "Appel d'urgence déclenché.", "OK");
    }

    private async void OnDismissClicked(object sender, EventArgs e)
    {
        await NativeAlertService.StopCriticalAlertAsync();
        await Shell.Current.GoToAsync("..");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = NativeAlertService.StopCriticalAlertAsync();
    }
}
