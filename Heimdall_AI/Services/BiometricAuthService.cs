namespace Heimdall_AI.Services;

public interface IBiometricAuthService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<bool> AuthenticateAsync(string title, string subtitle, CancellationToken cancellationToken = default);
}

public sealed class BiometricAuthService : IBiometricAuthService
{
#if ANDROID
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.Fragment.App.FragmentActivity;
        if (activity is null)
        {
            return Task.FromResult(false);
        }

        var manager = AndroidX.Biometric.BiometricManager.From(activity);
        var can = manager.CanAuthenticate(
            AndroidX.Biometric.BiometricManager.Authenticators.BiometricStrong
            | AndroidX.Biometric.BiometricManager.Authenticators.DeviceCredential);

        return Task.FromResult(can == AndroidX.Biometric.BiometricManager.BiometricSuccess);
    }

    public Task<bool> AuthenticateAsync(string title, string subtitle, CancellationToken cancellationToken = default)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.Fragment.App.FragmentActivity;
        if (activity is null)
        {
            return Task.FromResult(false);
        }

        var tcs = new TaskCompletionSource<bool>();
        var executor = AndroidX.Core.Content.ContextCompat.GetMainExecutor(activity);
        var callback = new BiometricPromptCallback(tcs);

        var prompt = new AndroidX.Biometric.BiometricPrompt(activity, executor, callback);

        var promptInfo = new AndroidX.Biometric.BiometricPrompt.PromptInfo.Builder()
            .SetTitle(title)
            .SetSubtitle(subtitle)
            .SetAllowedAuthenticators(
                AndroidX.Biometric.BiometricManager.Authenticators.BiometricStrong
                | AndroidX.Biometric.BiometricManager.Authenticators.DeviceCredential)
            .Build();

        prompt.Authenticate(promptInfo);
        return tcs.Task;
    }

    private sealed class BiometricPromptCallback(TaskCompletionSource<bool> tcs) : AndroidX.Biometric.BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(AndroidX.Biometric.BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            tcs.TrySetResult(true);
        }

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            tcs.TrySetResult(false);
        }

        public override void OnAuthenticationFailed()
        {
            base.OnAuthenticationFailed();
        }
    }
#else
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> AuthenticateAsync(string title, string subtitle, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
#endif
}
