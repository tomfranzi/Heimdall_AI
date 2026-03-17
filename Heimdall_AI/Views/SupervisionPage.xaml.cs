using System.Text.RegularExpressions;

namespace Heimdall_AI.Views;

public partial class SupervisionPage : ContentPage
{
    public SupervisionPage(SupervisionViewModels viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 1. On fait tourner le faisceau à l'infini (3 secondes par tour)
        var radarAnimation = new Animation(v => RadarSweep.Rotation = v, 0, 360);
        radarAnimation.Commit(this, "RadarSweepAnim", length: 3000, repeat: () => true);

        // 2. On synchronise les points en fonction de leur position (en % d'un tour)
        StartPingAnimation(Capteur1, 0.12);
        StartPingAnimation(Capteur2, 0.40);
        StartPingAnimation(Capteur3, 0.62);
    }

    // VÉRIFIE QUE TU AS BIEN CETTE FONCTION AUSSI :
    private void StartPingAnimation(View dot, double startPercent)
    {
        var pingAnim = new Animation();

        double spikeEnd = Math.Min(1.0, startPercent + 0.05);
        double fadeEnd = Math.Min(1.0, spikeEnd + 0.30);

        pingAnim.Add(startPercent, spikeEnd, new Animation(v => dot.Opacity = v, 0, 1));
        pingAnim.Add(spikeEnd, fadeEnd, new Animation(v => dot.Opacity = v, 1, 0, Easing.CubicOut));

        pingAnim.Commit(dot, "PingAnim_" + dot.Id, length: 3000, repeat: () => true);
    }
}