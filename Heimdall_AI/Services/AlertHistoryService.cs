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
    private const string AlertesFileName = "alertes_db.json";

    private readonly IFileDatabaseService _fileDb;

    public Alertes? AlerteActive { get; private set; }
    public ObservableCollection<Alertes> Historique { get; } = new();

    public event Action<Alertes>? AlerteActiveRecue;

    public AlertHistoryService(IFileDatabaseService fileDb)
    {
        _fileDb = fileDb;
        _ = ChargerDepuisFichierAsync();
    }

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
            _ = SauvegarderAsync();
            AlerteActiveRecue?.Invoke(alerte);
        });
    }

    private async Task ChargerDepuisFichierAsync()
    {
        var data = await _fileDb.LoadAsync(AlertesFileName, new AlertesStorageDto());

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AlerteActive = data.AlerteActive;
            Historique.Clear();

            foreach (var item in data.Historique.OrderByDescending(a => a.DateCreation))
            {
                Historique.Add(item);
            }
        });
    }

    private Task SauvegarderAsync()
    {
        var dto = new AlertesStorageDto
        {
            AlerteActive = AlerteActive,
            Historique = [.. Historique]
        };

        return _fileDb.SaveAsync(AlertesFileName, dto);
    }

    private sealed class AlertesStorageDto
    {
        public Alertes? AlerteActive { get; set; }
        public List<Alertes> Historique { get; set; } = [];
    }
}
