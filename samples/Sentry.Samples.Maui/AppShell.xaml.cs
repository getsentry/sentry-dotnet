namespace Sentry.Samples.Maui;

public partial class AppShell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("ctmvvm", typeof(CtMvvmPage));
    }
}
