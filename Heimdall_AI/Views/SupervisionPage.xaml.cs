namespace Heimdall_AI.Views;

public partial class SupervisionPage : ContentPage
{
    public SupervisionPage(SupervisionViewModels viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}