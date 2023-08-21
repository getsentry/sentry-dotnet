using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Sentry;

namespace Sentry.AspNetCore.Blazor;

public partial class SentryBlazor : ComponentBase, IDisposable
{
    [Inject] private SentryBlazorOptions? SentryBlazorOptions { get; set; }
    [Inject] private IJSRuntime? JSRuntime { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }

    private string _sentryBundleName = "";

    // Early enough?
    protected override Task OnInitializedAsync()
    {
        if (SentryBlazorOptions is null) return Task.CompletedTask;
        if (SentryBlazorOptions.ReplaysSessionSampleRate > 0 || SentryBlazorOptions.ReplaysOnErrorSampleRate > 0)
        {
            _sentryBundleName = "bundle.tracing.replay.min.js";
        }

        return Task.CompletedTask;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!string.IsNullOrEmpty(_sentryBundleName) && JSRuntime is not null && SentryBlazorOptions is not null)
            {
                var callback = DotNetObjectReference.Create((dynamic o) =>
                {
                    o.dsn = SentryBlazorOptions.Dsn!;
                    o.debug = true;
                });

                try
                {
                    // TODO: needs to be disposed?
                    var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./{_sentryBundleName}")
                        // TODO: Requires context?
                        .ConfigureAwait(true);
                    if (module is null) return;

                    await module.InvokeAsync<Action<dynamic>>("init", callback)
                        // TODO: Requires context?
                        .ConfigureAwait(true);
                }
                catch (JSException e)
                {
                    SentrySdk.CaptureException(e);
                    return;
                }
            }

            if (NavigationManager is null) return;
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
        }
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (sender is null) return;
        SentrySdk.AddBreadcrumb("Location changed: " + ((NavigationManager)sender).Uri);
    }

    public void Dispose()
    {
        if (NavigationManager is null) return;
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }
}
