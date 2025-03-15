using Microsoft.Maui.Controls.Internals;

namespace Sentry.Maui.Tests.Mocks;

public class MockApplication : Application
{
    private static readonly object LockObj = new();
    private Page _mainPage;

    static MockApplication()
    {
#pragma warning disable CS0612
        // Invoking events may request system resources from a singleton ISystemResourcesProvider.
        DependencyService.RegisterSingleton(Substitute.For<ISystemResourcesProvider>());
#pragma warning restore CS0612
    }

    private MockApplication()
    {
    }

    public void AddWindow(Page mainPage)
    {
        _mainPage = mainPage;
        ((IApplication)this).CreateWindow(null);
        _mainPage = null;
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        if (_mainPage != null)
        {
            return new Window(_mainPage);
        }

        return base.CreateWindow(activationState);
    }

    public static MockApplication Create()
    {
        // The base constructor will try to set the mock as the current application, which we don't want in tests.

        lock (LockObj)
        {
            var previous = Current;
            var application = new MockApplication();
            Current = previous;
            return application;
        }
    }
}
