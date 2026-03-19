using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

namespace Heimdall_AI.Services;

public interface IMqttAlertService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task PublishNotificationEventAsync(Alertes alerte, CancellationToken cancellationToken = default);
    Task PublishConfigAsync(double sensitivityPercent, CancellationToken cancellationToken = default);
}

public sealed class MqttAlertService : IMqttAlertService
{
    private const string MqttBroker = "10.74.17.156";
    private const int MqttPort = 1883;
    private const string MqttTopicAlerts = "heimdall/#";
    private const string MqttTopicStatus = "heimdall/status";
    private const string MqttTopicMobileNotifications = "heimdall/mobile/notifications";
    private const string MqttTopicConfig = "heimdall/config";

    private readonly IAlertHistoryService _alertHistoryService;
    private readonly IListeningSettingsService _listeningSettingsService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly SemaphoreSlim _startLock = new(1, 1);

    private IMqttClient? _client;
    private bool _started;

    public MqttAlertService(
        IAlertHistoryService alertHistoryService,
        IListeningSettingsService listeningSettingsService,
        IDeviceStatusService deviceStatusService)
    {
        _alertHistoryService = alertHistoryService;
        _listeningSettingsService = listeningSettingsService;
        _deviceStatusService = deviceStatusService;
    }

    public async Task PublishNotificationEventAsync(Alertes alerte, CancellationToken cancellationToken = default)
    {
        if (_client is null || !_client.IsConnected)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event = "bruit_detecte",
            type = alerte.TypeDetection,
            titre = alerte.Titre,
            confiance = alerte.Confiance,
            timestamp = DateTime.UtcNow,
            source = "mobile"
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(MqttTopicMobileNotifications)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
    }

    public async Task PublishConfigAsync(double sensitivityPercent, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (_client is null || !_client.IsConnected)
        {
            return;
        }

        var clampedSensitivity = Math.Clamp(sensitivityPercent, 0, 100);
        var noiseGate = 0.5 - ((clampedSensitivity / 100d) * (0.5 - 0.001));
        noiseGate = Math.Clamp(noiseGate, 0.001, 0.5);

        var payload = JsonSerializer.Serialize(new
        {
            noise_gate = Math.Round(noiseGate, 4)
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(MqttTopicConfig)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            await StartAsync(cancellationToken);
            return;
        }

        if (_client.IsConnected)
        {
            return;
        }

        try
        {
            await ConnectAndSubscribeAsync(cancellationToken);
        }
        catch
        {
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            return;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            _client.DisconnectedAsync += OnDisconnectedAsync;

            await ConnectAndSubscribeAsync(cancellationToken);
            _started = true;
            await PublishConfigAsync(_listeningSettingsService.MicroSensitivity, cancellationToken);
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            return;
        }

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttBroker, MqttPort)
            .Build();

        await _client.ConnectAsync(options, cancellationToken);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(MqttTopicAlerts)
            .WithTopicFilter(MqttTopicStatus)
            .Build();

        await _client.SubscribeAsync(subscribeOptions, cancellationToken);
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000);
                await ConnectAndSubscribeAsync();
            }
            catch
            {
            }
        });

        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        var topic = eventArgs.ApplicationMessage.Topic;

        if (string.Equals(topic, MqttTopicMobileNotifications, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (string.Equals(topic, MqttTopicConfig, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (!_listeningSettingsService.IsListeningEnabled)
        {
            if (!string.Equals(topic, MqttTopicStatus, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }
        }

        var payload = eventArgs.ApplicationMessage.PayloadSegment;
        var payloadText = payload.Count == 0 ? string.Empty : Encoding.UTF8.GetString(payload);

        if (string.Equals(topic, MqttTopicStatus, StringComparison.OrdinalIgnoreCase))
        {
            _deviceStatusService.UpdateFromPayload(payloadText);
            return Task.CompletedTask;
        }

        var alerte = BuildAlertFromPayload(payloadText, topic);
        if (alerte is null)
        {
            return Task.CompletedTask;
        }

        _alertHistoryService.AddAlert(alerte);

        return Task.CompletedTask;
    }

    private Alertes? BuildAlertFromPayload(string payloadText, string? topic)
    {
        if (string.IsNullOrWhiteSpace(payloadText))
        {
            var detectedType = TraduireType(ExtractTypeFromTopic(topic));
            if (!_listeningSettingsService.IsCategoryEnabled(detectedType))
            {
                return null;
            }

            return new Alertes
            {
                Titre = ConstruireTitreDepuisType(detectedType),
                TypeDetection = detectedType,
                Description = $"Bruit détecté : {detectedType}",
                DureeTexte = "1.0s",
                Niveau = "Info",
                DateCreation = DateTime.Now
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadText);
            var root = doc.RootElement;

            var detectedTypeBrut = ReadString(root, "type")
                                   ?? ReadString(root, "detectedType")
                                   ?? ExtractTypeFromTopic(topic);

            var detectedType = TraduireType(detectedTypeBrut);

            var titreEntrant = ReadString(root, "titre")
                              ?? ReadString(root, "title");

            var titre = string.IsNullOrWhiteSpace(titreEntrant)
                        || string.Equals(titreEntrant, "Alerte MQTT", StringComparison.OrdinalIgnoreCase)
                        ? ConstruireTitreDepuisType(detectedType)
                        : titreEntrant!;

            var descriptionEntrante = ReadString(root, "description")
                                     ?? ReadString(root, "message");

            var description = string.IsNullOrWhiteSpace(descriptionEntrante)
                ? $"Bruit détecté : {detectedType}"
                : NettoyerDescription(descriptionEntrante!, detectedType);

            var niveau = ReadString(root, "niveau")
                         ?? ReadString(root, "level")
                         ?? "Info";

            if (!_listeningSettingsService.IsCategoryEnabled(detectedType))
            {
                return null;
            }

            var confidence = ReadDouble(root, "confidence");
            confidence ??= ReadDouble(root, "score");
            confidence ??= ReadDouble(root, "probability");
            var normalizedConfidence = NormalizeConfidence(confidence);

            var duration = ReadDouble(root, "duration")
                           ?? ReadDouble(root, "duration_seconds")
                           ?? ReadDouble(root, "durationSeconds");

            if (!_listeningSettingsService.IsAlertModeEnabled && !_listeningSettingsService.IsConfidenceAccepted(normalizedConfidence))
            {
                return null;
            }

            var dateCreationText = ReadString(root, "dateCreation")
                                   ?? ReadString(root, "timestamp");

            var dateCreation = DateTime.TryParse(dateCreationText, out var parsedDate)
                ? parsedDate
                : DateTime.Now;

            return new Alertes
            {
                Titre = titre,
                TypeDetection = detectedType,
                Description = description,
                DureeTexte = FormatDuree(duration),
                Niveau = niveau,
                Confiance = normalizedConfidence,
                DateCreation = dateCreation
            };
        }
        catch
        {
            var detectedType = TraduireType(ExtractTypeFromTopic(topic));
            if (!_listeningSettingsService.IsCategoryEnabled(detectedType))
            {
                return null;
            }

            return new Alertes
            {
                Titre = ConstruireTitreDepuisType(detectedType),
                TypeDetection = detectedType,
                Description = $"Bruit détecté : {detectedType}",
                DureeTexte = "1.0s",
                Niveau = "Info",
                Confiance = 0,
                DateCreation = DateTime.Now
            };
        }
    }

    private static double NormalizeConfidence(double? confidence)
    {
        if (!confidence.HasValue)
        {
            return 0;
        }

        var value = confidence.Value;
        if (value > 1)
        {
            value /= 100d;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static string ExtractTypeFromTopic(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return "Inconnu";
        }

        var parts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "Inconnu" : parts[^1];
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return value.ToString();
    }

    private static double? ReadDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string ConstruireTitreDepuisType(string type)
    {
        return string.IsNullOrWhiteSpace(type) ? "Heimdall" : $"{type} détecté";
    }

    private static string NettoyerDescription(string description, string typeTraduit)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return $"Bruit détecté : {typeTraduit}";
        }

        var texte = description.Trim();
        if (texte.StartsWith("{") || texte.StartsWith("["))
        {
            return $"Bruit détecté : {typeTraduit}";
        }

        return TraduireTypeDansTexte(texte);
    }

    private static string TraduireTypeDansTexte(string texte)
    {
        return texte
            .Replace("Bruit détecté : Speech", "Bruit détecté : Parole", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Knock", "Bruit détecté : Toc à la porte", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Alarm", "Bruit détecté : Alarme", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Door", "Bruit détecté : Porte", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Mechanic", "Bruit détecté : Mécanique", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Fan", "Bruit détecté : Ventilateur", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Tap", "Bruit détecté : Frappe légère", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Wood", "Bruit détecté : Coup", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Scream", "Bruit détecté : Cri aigu", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Shout", "Bruit détecté : Cri fort", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Yell", "Bruit détecté : Hurlement", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Glass", "Bruit détecté : Verre", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Shatter", "Bruit détecté : Bris", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Crying, sobbing", "Bruit détecté : Pleurs, sanglots", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Domestic animals, pets", "Bruit détecté : Animaux domestiques", StringComparison.OrdinalIgnoreCase)
            .Replace("Bruit détecté : Click", "Bruit détecté : Clic", StringComparison.OrdinalIgnoreCase);
    }

    private static string TraduireType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return "Inconnu";
        }

        return type.Trim() switch
        {
            var t when t.Contains("Speech", StringComparison.OrdinalIgnoreCase) => "Parole",
            var t when t.Contains("Knock", StringComparison.OrdinalIgnoreCase) => "Toc à la porte",
            var t when t.Contains("Alarm", StringComparison.OrdinalIgnoreCase) => "Alarme",
            var t when t.Contains("Door", StringComparison.OrdinalIgnoreCase) => "Porte",
            var t when t.Contains("Mechanic", StringComparison.OrdinalIgnoreCase) => "Mécanique",
            var t when t.Contains("Fan", StringComparison.OrdinalIgnoreCase) => "Ventilateur",
            var t when t.Contains("Tap", StringComparison.OrdinalIgnoreCase) => "Frappe légère",
            var t when t.Contains("Wood", StringComparison.OrdinalIgnoreCase) => "Coup",
            var t when t.Contains("Scream", StringComparison.OrdinalIgnoreCase) => "Cri aigu",
            var t when t.Contains("Shout", StringComparison.OrdinalIgnoreCase) => "Cri fort",
            var t when t.Contains("Yell", StringComparison.OrdinalIgnoreCase) => "Hurlement",
            var t when t.Contains("Glass", StringComparison.OrdinalIgnoreCase) => "Verre",
            var t when t.Contains("Shatter", StringComparison.OrdinalIgnoreCase) => "Bris",
            var t when t.Contains("Crying", StringComparison.OrdinalIgnoreCase) || t.Contains("sobbing", StringComparison.OrdinalIgnoreCase) => "Pleurs, sanglots",
            var t when t.Contains("Domestic animals", StringComparison.OrdinalIgnoreCase) || t.Contains("pets", StringComparison.OrdinalIgnoreCase) => "Animaux domestiques",
            var t when t.Contains("Click", StringComparison.OrdinalIgnoreCase) => "Clic",
            _ => type
        };
    }

    private static string FormatDuree(double? duration)
    {
        if (!duration.HasValue || duration.Value <= 0)
        {
            return "1.0s";
        }

        return $"{duration.Value:F1}s";
    }
}
