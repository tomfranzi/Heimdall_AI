namespace Heimdall_AI.Views;

public partial class HistoriquePage : ContentPage
{
    public HistoriquePage(HistoriqueViewModels viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ParametresPage");
    }
}
