namespace Heimdall_AI.Services
{
    public interface INativeAlertService
    {
        Task PushNotificationAsync(Alertes alerte, bool critical, CancellationToken cancellationToken = default);
        Task StopCriticalAlertAsync(CancellationToken cancellationToken = default);
        Task<bool> IsCriticalAlertActiveAsync(CancellationToken cancellationToken = default);
    }

    public sealed class NativeAlertService : INativeAlertService
    {
        private const string CriticalAlarmStateKey = "heimdall.critical_alarm.active";

        public Task PushNotificationAsync(Alertes alerte, bool critical, CancellationToken cancellationToken = default)
        {
            if (alerte is null)
            {
                return Task.CompletedTask;
            }

#if ANDROID
            if (DescriptionSembleJson(alerte.Description))
            {
                alerte.Description = $"Bruit détecté : {alerte.TypeDetection}";
            }

            AndroidAlertHelper.ShowNotification(alerte, critical);
#endif

            return Task.CompletedTask;
        }

        public Task StopCriticalAlertAsync(CancellationToken cancellationToken = default)
        {
#if ANDROID
            AndroidAlertHelper.StopCriticalAlarm();
            global::Heimdall_AI.HeimdallForegroundService.RequestStopCriticalAlarm(Android.App.Application.Context);
#endif
            Preferences.Default.Set(CriticalAlarmStateKey, false);
            return Task.CompletedTask;
        }

        public Task<bool> IsCriticalAlertActiveAsync(CancellationToken cancellationToken = default)
        {
            var isActive = Preferences.Default.Get(CriticalAlarmStateKey, false);
            return Task.FromResult(isActive);
        }

        private static bool DescriptionSembleJson(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return false;
            }

            var text = description.TrimStart();
            return text.StartsWith("{") || text.StartsWith("[");
        }
    }

#if ANDROID
    internal static class AndroidAlertHelper
    {
        private const string NormalChannelId = "heimdall_notifications";
        private const string CriticalChannelId = "heimdall_critical_alerts";
        private const string CriticalAlarmStateKey = "heimdall.critical_alarm.active";

        private static Android.Media.ToneGenerator? _toneGenerator;
        private static CancellationTokenSource? _criticalAlarmCts;

        public static void ShowNotification(Alertes alerte, bool critical)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var context = Android.App.Application.Context;
                EnsureChannels(context);

                var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName);
                var contentIntent = Android.App.PendingIntent.GetActivity(
                    context,
                    0,
                    launchIntent,
                    Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.UpdateCurrent);

                var fullScreenIntent = Android.App.PendingIntent.GetActivity(
                    context,
                    1001,
                    launchIntent,
                    Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.UpdateCurrent);

                var channelId = critical ? CriticalChannelId : NormalChannelId;
                var builder = new AndroidX.Core.App.NotificationCompat.Builder(context, channelId)
                    .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
                    .SetContentTitle("Heimdall")
                    .SetContentText(string.IsNullOrWhiteSpace(alerte.Description) ? alerte.TypeDetection : alerte.Description)
                    .SetStyle(new AndroidX.Core.App.NotificationCompat.BigTextStyle().BigText(string.IsNullOrWhiteSpace(alerte.Description) ? alerte.TypeDetection : alerte.Description))
                    .SetContentIntent(contentIntent)
                    .SetAutoCancel(true)
                    .SetPriority(critical ? AndroidX.Core.App.NotificationCompat.PriorityMax : AndroidX.Core.App.NotificationCompat.PriorityHigh)
                    .SetCategory(critical ? AndroidX.Core.App.NotificationCompat.CategoryAlarm : AndroidX.Core.App.NotificationCompat.CategoryStatus)
                    .SetVisibility(AndroidX.Core.App.NotificationCompat.VisibilityPublic)
                    .SetDefaults((int)Android.App.NotificationDefaults.All);

                if (critical)
                {
                    builder.SetFullScreenIntent(fullScreenIntent, true)
                           .SetOngoing(true)
                           .SetVibrate([0, 700, 350, 700, 350, 700]);
                }

                AndroidX.Core.App.NotificationManagerCompat.From(context).Notify(Guid.NewGuid().GetHashCode(), builder.Build());

                if (critical)
                {
                    StartCriticalAlarm(context);
                }
            });
        }

        private static void EnsureChannels(Android.Content.Context context)
        {
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O)
            {
                return;
            }

            var manager = (Android.App.NotificationManager?)context.GetSystemService(Android.Content.Context.NotificationService);
            if (manager is null)
            {
                return;
            }

            if (manager.GetNotificationChannel(NormalChannelId) is null)
            {
                var normal = new Android.App.NotificationChannel(NormalChannelId, "Notifications Heimdall", Android.App.NotificationImportance.High)
                {
                    Description = "Notifications d'activité Heimdall"
                };
                manager.CreateNotificationChannel(normal);
            }

            if (manager.GetNotificationChannel(CriticalChannelId) is null)
            {
                var critical = new Android.App.NotificationChannel(CriticalChannelId, "Alertes critiques Heimdall", Android.App.NotificationImportance.High)
                {
                    Description = "Alertes sonores critiques"
                };
                critical.EnableVibration(true);
                critical.EnableLights(true);
                critical.LockscreenVisibility = Android.App.NotificationVisibility.Public;
                critical.SetSound(null, null);
                manager.CreateNotificationChannel(critical);
            }
        }

        private static void StartCriticalAlarm(Android.Content.Context context)
        {
            try
            {
                Preferences.Default.Set(CriticalAlarmStateKey, true);

                var audioManager = (Android.Media.AudioManager?)context.GetSystemService(Android.Content.Context.AudioService);
                if (audioManager is not null)
                {
                    var maxVolume = audioManager.GetStreamMaxVolume(Android.Media.Stream.Alarm);
                    audioManager.SetStreamVolume(Android.Media.Stream.Alarm, maxVolume, Android.Media.VolumeNotificationFlags.ShowUi);
                }

                _criticalAlarmCts?.Cancel();
                _criticalAlarmCts?.Dispose();
                _criticalAlarmCts = new CancellationTokenSource();
                var token = _criticalAlarmCts.Token;

                Task.Run(async () =>
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

        public static void StopCriticalAlarm()
        {
            try
            {
                Preferences.Default.Set(CriticalAlarmStateKey, false);

                _criticalAlarmCts?.Cancel();
                _criticalAlarmCts?.Dispose();
                _criticalAlarmCts = null;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _toneGenerator?.StopTone();
                });
            }
            catch
            {
            }
        }
    }
#endif
}
