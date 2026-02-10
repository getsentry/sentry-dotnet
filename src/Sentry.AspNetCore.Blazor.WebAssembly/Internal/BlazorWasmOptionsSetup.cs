using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Internal;

internal sealed class BlazorWasmOptionsSetup : IConfigureOptions<SentryBlazorOptions>
{
    private readonly NavigationManager _navigationManager;

    public BlazorWasmOptionsSetup(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public void Configure(SentryBlazorOptions options)
    {
        var previousUrl = _navigationManager.Uri;

        // Set the initial scope request URL
        SentrySdk.ConfigureScope(scope =>
        {
            scope.Request.Url = ToRelativePath(previousUrl);
        });

        _navigationManager.LocationChanged += (_, args) =>
        {
            var from = ToRelativePath(previousUrl);
            var to = ToRelativePath(args.Location);

            SentrySdk.AddBreadcrumb(
                message: "",
                category: "navigation",
                type: "navigation",
                data: new Dictionary<string, string>
                {
                    { "from", from },
                    { "to", to }
                });

            SentrySdk.ConfigureScope(scope =>
            {
                scope.Request.Url = to;
            });

            previousUrl = args.Location;
        };
    }

    private string ToRelativePath(string url)
    {
        var relative = _navigationManager.ToBaseRelativePath(url);
        return "/" + relative;
    }
}
