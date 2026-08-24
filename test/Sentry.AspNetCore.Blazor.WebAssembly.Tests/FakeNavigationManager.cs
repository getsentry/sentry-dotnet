using Microsoft.AspNetCore.Components;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Tests;

internal sealed class FakeNavigationManager : NavigationManager
{
    public FakeNavigationManager(string baseUri = "https://localhost/", string initialUri = "https://localhost/")
    {
        Initialize(baseUri, initialUri);
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        var absoluteUri = ToAbsoluteUri(uri).ToString();
        Uri = absoluteUri;
        NotifyLocationChanged(isInterceptedLink: false);
    }
}
