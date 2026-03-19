using System.Text;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using MQTTnet;
using MQTTnet.Client;

namespace Heimdall_AI;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class HeimdallForegroundService : Service
{
    public const string ActionStopCriticalAlarm = "heimdall.action.STOP_CRITICAL_ALARM";
    private const string CriticalAlarmStateKey = "heimdall.critical_alarm.active";

    private const string ServiceChannelId = "heimdall_service";
    private const string AlertChannelId = "heimdall_critical_alerts";
    private const string NormalChannelId = "heimdall_notifications";

    private const string MqttBroker = "10.74.17.156";
    private const int MqttPort = 1883;

    private const string TopicAlerts = "heimdall/#";
    private const string TopicStatus = "heimdall/status";
    private const string TopicConfig = "heimdall/config";
    private const string TopicMobileNotifications = "heimdall/mobile/notifications";

    private IMqttClient? _client;
    private CancellationTokenSource? _alarmCts;
    private Android.Media.ToneGenerator? _toneGenerator;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (string.Equals(intent?.Action, ActionStopCriticalAlarm, StringComparison.Ordinal))
        {
            StopSiren();
            return StartCommandResult.Sticky;
        }

        EnsureChannels();
        StartForeground(7070, BuildServiceNotification());
        _ = StartMqttAsync();
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        StopSiren();
    }

    public static void RequestStopCriticalAlarm(Context context)
    {
        var intent = new Intent(context, typeof(HeimdallForegroundService));
        intent.SetAction(ActionStopCriticalAlarm);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    private Notification BuildServiceNotification()
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName);
        var pendingIntent = PendingIntent.GetActivity(this, 0, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        return new NotificationCompat.Builder(this, ServiceChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle("Heimdall")
            .SetContentText("Surveillance active")
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build();
    }

    private async Task StartMqttAsync()
    {
        if (_client is not null && _client.IsConnected)
        {
            return;
        }

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessageAsync;

        _client.DisconnectedAsync += args =>
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
        };

        await ConnectAndSubscribeAsync();
    }

    private async Task ConnectAndSubscribeAsync()
    {
        if (_client is null)
        {
            return;
        }

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttBroker, MqttPort)
            .Build();

        await _client.ConnectAsync(options);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(TopicAlerts)
            .WithTopicFilter(TopicStatus)
            .Build();

        await _client.SubscribeAsync(subscribeOptions);
    }

    private Task OnMessageAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        var topic = eventArgs.ApplicationMessage.Topic;
        if (string.Equals(topic, TopicStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(topic, TopicConfig, StringComparison.OrdinalIgnoreCase)
            || string.Equals(topic, TopicMobileNotifications, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (!GetBool("settings.is_listening_enabled", true))
        {
            return Task.CompletedTask;
        }

        var payload = eventArgs.ApplicationMessage.PayloadSegment;
        var payloadText = payload.Count == 0 ? string.Empty : Encoding.UTF8.GetString(payload);

        var type = ExtractType(payloadText, topic);
        if (!IsCategoryEnabled(type))
        {
            return Task.CompletedTask;
        }

        var message = BuildMessage(payloadText, TraduireType(type));
        var critical = GetBool("settings.is_alert_mode_enabled", false);
        ShowAlertNotification(message, critical);

        if (critical)
        {
            StartSiren();
        }

        return Task.CompletedTask;
    }

    private string ExtractType(string payloadText, string? topic)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(payloadText))
            {
                using var doc = JsonDocument.Parse(payloadText);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var t))
                {
                    return t.ToString();
                }
                if (root.TryGetProperty("detectedType", out var dt))
                {
                    return dt.ToString();
                }
            }
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            return "Inconnu";
        }

        var parts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "Inconnu" : parts[^1];
    }

    private static string BuildMessage(string payloadText, string detectedType)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(payloadText))
            {
                using var doc = JsonDocument.Parse(payloadText);
                var root = doc.RootElement;
                if (root.TryGetProperty("description", out var d))
                {
                    return d.ToString();
                }
                if (root.TryGetProperty("message", out var m))
                {
                    return m.ToString();
                }
            }
        }
        catch
        {
        }

        return $"Bruit détecté : {detectedType}";
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

    private void ShowAlertNotification(string message, bool critical)
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName);
        var contentIntent = PendingIntent.GetActivity(this, 1, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        var fullScreenIntent = PendingIntent.GetActivity(this, 2, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(this, critical ? AlertChannelId : NormalChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
            .SetContentTitle("Heimdall")
            .SetContentText(message)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
            .SetContentIntent(contentIntent)
            .SetAutoCancel(true)
            .SetPriority(critical ? NotificationCompat.PriorityMax : NotificationCompat.PriorityHigh)
            .SetCategory(critical ? NotificationCompat.CategoryAlarm : NotificationCompat.CategoryStatus)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetDefaults((int)NotificationDefaults.All);

        if (critical)
        {
            builder.SetFullScreenIntent(fullScreenIntent, true)
                   .SetVibrate(new long[] { 0, 700, 350, 700, 350, 700 });
        }

        NotificationManagerCompat.From(this).Notify(Guid.NewGuid().GetHashCode(), builder.Build());
    }

    private void StartSiren()
    {
        try
        {
            Preferences.Default.Set(CriticalAlarmStateKey, true);

            var audioManager = (Android.Media.AudioManager?)GetSystemService(AudioService);
            if (audioManager is not null)
            {
                var maxVolume = audioManager.GetStreamMaxVolume(Android.Media.Stream.Alarm);
                audioManager.SetStreamVolume(Android.Media.Stream.Alarm, maxVolume, Android.Media.VolumeNotificationFlags.ShowUi);
            }

            _alarmCts?.Cancel();
            _alarmCts?.Dispose();
            _alarmCts = new CancellationTokenSource();
            var token = _alarmCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _toneGenerator ??= new Android.Media.ToneGenerator(Android.Media.Stream.Alarm, 100);
                        _toneGenerator.StartTone(Android.Media.Tone.CdmaEmergencyRingback, 850);
                    });

                    try
                    {
                        await Task.Delay(900, token);
                    }
                    catch
                    {
                        break;
                    }
                }
            }, token);

            Vibration.Default.Vibrate(TimeSpan.FromSeconds(2));
        }
        catch
        {
        }
    }

    private void StopSiren()
    {
        try
        {
            Preferences.Default.Set(CriticalAlarmStateKey, false);

            _alarmCts?.Cancel();
            _alarmCts?.Dispose();
            _alarmCts = null;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _toneGenerator?.StopTone();
            });
        }
        catch
        {
        }
    }

    private void EnsureChannels()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager is null)
        {
            return;
        }

        if (manager.GetNotificationChannel(ServiceChannelId) is null)
        {
            var service = new NotificationChannel(ServiceChannelId, "Service Heimdall", NotificationImportance.Low)
            {
                Description = "Surveillance en arrière-plan"
            };
            manager.CreateNotificationChannel(service);
        }

        if (manager.GetNotificationChannel(NormalChannelId) is null)
        {
            var normal = new NotificationChannel(NormalChannelId, "Notifications Heimdall", NotificationImportance.High)
            {
                Description = "Notifications d'activité"
            };
            manager.CreateNotificationChannel(normal);
        }

        if (manager.GetNotificationChannel(AlertChannelId) is null)
        {
            var alert = new NotificationChannel(AlertChannelId, "Alertes critiques Heimdall", NotificationImportance.High)
            {
                Description = "Alertes urgentes"
            };
            alert.EnableVibration(true);
            alert.EnableLights(true);
            alert.LockscreenVisibility = NotificationVisibility.Public;
            alert.SetSound(null, null);
            manager.CreateNotificationChannel(alert);
        }
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        return Preferences.Default.Get(key, defaultValue);
    }

    private static bool IsCategoryEnabled(string? detectedType)
    {
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            return true;
        }

        var type = detectedType.Trim().ToLowerInvariant();

        if (type.Contains("speech") || type.Contains("talk") || type.Contains("voice") || type.Contains("parole")) return GetBool("settings.detect_speech", true);
        if (type.Contains("knock") || type.Contains("toc")) return GetBool("settings.detect_knock", true);
        if (type.Contains("alarm") || type.Contains("siren") || type.Contains("alarme")) return GetBool("settings.detect_alarm", true);
        if (type.Contains("door") || type.Contains("porte")) return GetBool("settings.detect_door", true);
        if (type.Contains("mechanic") || type.Contains("machine") || type.Contains("engine") || type.Contains("mécanique")) return GetBool("settings.detect_mechanic", true);
        if (type.Contains("fan") || type.Contains("ventilateur")) return GetBool("settings.detect_fan", true);
        if (type.Contains("tap") || type.Contains("robinet")) return GetBool("settings.detect_tap", true);
        if (type.Contains("wood") || type.Contains("coup")) return GetBool("settings.detect_coup", true);
        if (type.Contains("scream") || type.Contains("cri aigu")) return GetBool("settings.detect_scream", true);
        if (type.Contains("shout") || type.Contains("cri fort")) return GetBool("settings.detect_shout", true);
        if (type.Contains("yell") || type.Contains("hurlement")) return GetBool("settings.detect_yell", true);
        if (type.Contains("glass") || type.Contains("verre")) return GetBool("settings.detect_glass", true);
        if (type.Contains("shatter") || type.Contains("bris")) return GetBool("settings.detect_shatter", true);
        if (type.Contains("crying") || type.Contains("sobbing") || type.Contains("baby") || type.Contains("cry") || type.Contains("pleurs") || type.Contains("sanglots")) return GetBool("settings.detect_crying_sobbing", true);
        if (type.Contains("domestic animals") || type.Contains("pets") || type.Contains("pet") || type.Contains("animal") || type.Contains("animaux domestiques")) return GetBool("settings.detect_domestic_animals_pets", true);
        if (type.Contains("click") || type.Contains("clic")) return GetBool("settings.detect_click", true);

        return true;
    }
}
