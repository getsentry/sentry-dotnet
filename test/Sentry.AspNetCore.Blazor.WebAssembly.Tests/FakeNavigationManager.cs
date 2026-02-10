using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Tests;

internal sealed class FakeNavigationManager : NavigationManager
{
    public FakeNavigationManager(string baseUri = "https://localhost/", string initialUri = "https://localhost/")
    {
        Initialize(baseUri, initialUri);
    }

    public void NavigateTo(string uri)
    {
        var absoluteUri = ToAbsoluteUri(uri).ToString();
        Uri = absoluteUri;
        NotifyLocationChanged(isInterceptedLink: false);
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        var absoluteUri = ToAbsoluteUri(uri).ToString();
        Uri = absoluteUri;
        NotifyLocationChanged(isInterceptedLink: false);
    }
}
