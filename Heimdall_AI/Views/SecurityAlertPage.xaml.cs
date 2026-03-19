using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;

namespace Heimdall_AI.Views;

public partial class SecurityAlertPage : ContentPage, IQueryAttributable
{
    private bool _appelUrgenceEnCours;

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

    private async void OnCallEmergencyClicked(object sender, EventArgs e)
    {
        if (_appelUrgenceEnCours)
        {
            return;
        }

        _appelUrgenceEnCours = true;
        CallEmergencyButton.IsEnabled = false;

        try
        {
            MettreVolumeVoixAuMaximum();

            try
            {
                await TextToSpeech.Default.SpeakAsync("Appel d'urgence enclenché");
            }
            catch
            {
            }

            await Launcher.Default.OpenAsync(new Uri("tel:17"));
        }
        finally
        {
            _appelUrgenceEnCours = false;
            CallEmergencyButton.IsEnabled = true;
        }
    }

    private static void MettreVolumeVoixAuMaximum()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var audioManager = (Android.Media.AudioManager?)context.GetSystemService(Android.Content.Context.AudioService);
            if (audioManager is null)
            {
                return;
            }

            var maxTts = audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
            audioManager.SetStreamVolume(Android.Media.Stream.Music, maxTts, Android.Media.VolumeNotificationFlags.ShowUi);
        }
        catch
        {
        }
#endif
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
