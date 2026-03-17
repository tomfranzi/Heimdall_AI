namespace Heimdall_AI.Views;

public partial class AlertesPage : ContentPage
{
    public AlertesPage(AlertesViewModels viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }   
}