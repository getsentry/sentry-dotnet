using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Internal;

internal sealed class BlazorWasmOptionsSetup : IConfigureOptions<SentryBlazorOptions>
{
    private readonly NavigationManager _navigationManager;
    private readonly IHub _hub;
    private bool _initialized;

    public BlazorWasmOptionsSetup(NavigationManager navigationManager)
        : this(navigationManager, HubAdapter.Instance)
    {
    }

    internal BlazorWasmOptionsSetup(NavigationManager navigationManager, IHub hub)
    {
        _navigationManager = navigationManager;
        _hub = hub;
    }

    public void Configure(SentryBlazorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_initialized)
        {
            return;
        }
        _initialized = true;

        var previousUrl = _navigationManager.Uri;

        // Set the initial scope request URL
        _hub.ConfigureScope(scope =>
        {
            scope.Request.Url = ToRelativePath(previousUrl);
        });

        _navigationManager.LocationChanged += (_, args) =>
        {
            var from = ToRelativePath(previousUrl);
            var to = ToRelativePath(args.Location);

            _hub.AddBreadcrumb(
                new Breadcrumb(
                    type: "navigation",
                    category: "navigation",
                    data: new Dictionary<string, string>
                    {
                        { "from", from },
                        { "to", to }
                    }));

            _hub.ConfigureScope(scope =>
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
