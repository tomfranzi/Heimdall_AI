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
        var isAlertModeEnabled = _listeningSettingsService.IsAlertModeEnabled;

        if (string.IsNullOrWhiteSpace(payloadText))
        {
            var detectedType = ExtractTypeFromTopic(topic);
            if (!isAlertModeEnabled && !_listeningSettingsService.IsCategoryEnabled(detectedType))
            {
                return null;
            }

            return new Alertes
            {
                Titre = "Alerte MQTT",
                TypeDetection = detectedType,
                Description = "Message vide reçu sur le broker.",
                Niveau = "Info",
                DateCreation = DateTime.Now
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadText);
            var root = doc.RootElement;

            var titre = ReadString(root, "titre")
                        ?? ReadString(root, "title")
                        ?? "Alerte MQTT";

            var detectedType = ReadString(root, "type")
                               ?? ReadString(root, "detectedType")
                               ?? ExtractTypeFromTopic(topic);

            var description = ReadString(root, "description")
                              ?? ReadString(root, "message")
                              ?? $"Bruit détecté : {detectedType}";

            var niveau = ReadString(root, "niveau")
                         ?? ReadString(root, "level")
                         ?? "Info";

            if (!isAlertModeEnabled && !_listeningSettingsService.IsCategoryEnabled(detectedType))
            {
                return null;
            }

            var confidence = ReadDouble(root, "confidence");
            confidence ??= ReadDouble(root, "score");
            confidence ??= ReadDouble(root, "probability");
            var normalizedConfidence = NormalizeConfidence(confidence);

            if (!isAlertModeEnabled && !_listeningSettingsService.IsConfidenceAccepted(normalizedConfidence))
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
                Niveau = niveau,
                Confiance = normalizedConfidence,
                DateCreation = dateCreation
            };
        }
        catch
        {
            var detectedType = ExtractTypeFromTopic(topic);
            if (!isAlertModeEnabled && !_listeningSettingsService.IsCategoryEnabled(detectedType))
            {
                return null;
            }

            return new Alertes
            {
                Titre = "Alerte MQTT",
                TypeDetection = detectedType,
                Description = $"Bruit détecté : {detectedType}",
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
}
