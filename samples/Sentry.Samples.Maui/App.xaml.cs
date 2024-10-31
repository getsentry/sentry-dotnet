namespace Sentry.Samples.Maui;

public partial class App
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        Windows[0].Page = new AppShell();
        return base.CreateWindow(activationState);
    }
}
