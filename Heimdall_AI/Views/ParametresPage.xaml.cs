namespace Heimdall_AI.Views;

public partial class ParametresPage : ContentPage
{
    public ParametresPage(ParametresViewModels viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
    }
}
