namespace Heimdall_AI.ViewModels;

public partial class HistoriqueViewModels : ObservableObject
{
    public ObservableCollection<Alertes> ListeAlertesHistoriques { get; }

    [ObservableProperty]
    private bool isRefreshing;

    public HistoriqueViewModels(IAlertHistoryService alertHistoryService)
    {
        ListeAlertesHistoriques = alertHistoryService.Historique;
    }

    [RelayCommand]
    private async Task RafraichirAsync()
    {
        IsRefreshing = true;
        await Task.Delay(250);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task AlerteTappedAsync(Alertes alerteSelected)
    {
        if (alerteSelected is null)
        {
            return;
        }

        if (Shell.Current.CurrentPage is Page page)
        {
            await page.DisplayAlertAsync(alerteSelected.Titre, alerteSelected.Description, "Fermer");
        }
    }
}
