using Microsoft.Maui.ApplicationModel;

namespace Heimdall_AI.Services;

public interface IAlertHistoryService
{
    Alertes? AlerteActive { get; }
    ObservableCollection<Alertes> Historique { get; }
    event Action<Alertes>? AlerteActiveRecue;
    void AddAlert(Alertes alerte);
}

public sealed class AlertHistoryService : IAlertHistoryService
{
    public Alertes? AlerteActive { get; private set; }
    public ObservableCollection<Alertes> Historique { get; } = new();

    public event Action<Alertes>? AlerteActiveRecue;

    public void AddAlert(Alertes alerte)
    {
        if (alerte is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (AlerteActive is not null)
            {
                Historique.Insert(0, AlerteActive);
            }

            AlerteActive = alerte;
            AlerteActiveRecue?.Invoke(alerte);
        });
    }
}
