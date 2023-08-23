using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Sentry;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Blazor;

public partial class SentryBlazor : ComponentBase, IDisposable
{
    [Inject] private SentryBlazorOptions SentryBlazorOptions { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHub Sentry { get; set; } = null!;
    [Inject] private SentryOptions SentryOptions { get; set; } = null!;

    private string _previousUrl = "";

    /// <summary>
    /// ChildContent render fragment
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-7.0
    public void ProcessError(Exception ex)
    {
        // TODO: URL and other context already set?
        ex.SetSentryMechanism("SentryBlazorComponent");
        Sentry.CaptureException(ex);
    }

    public DebugImage[] DebugImages { get; set; } = Array.Empty<DebugImage>();

    // Early enough?
    protected override async Task OnInitializedAsync()
    {
        _previousUrl = NavigationManager.Uri;
        //
        try
        {
            var userAgent = await JSRuntime.InvokeAsync<string>("eval", "(() => navigator.userAgent)();")
                .ConfigureAwait(true);
            Sentry.ConfigureScope(s => s.Request.Headers["User-Agent"] = userAgent);
        }
        catch (Exception e)
        {
            SentryOptions.DiagnosticLogger?.LogError("Failed to read browser User-Agent.", e);
        }
        Console.WriteLine("SentryBlazor: OnInitializedAsync Starting");
        await Task.CompletedTask.ConfigureAwait(false);
        Console.WriteLine("SentryBlazor: OnInitializedAsync Completed");
    }

    protected override Task OnParametersSetAsync()
    {
        Console.WriteLine("SentryBlazor: OnParametersSetAsync");
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        Console.WriteLine("SentryBlazor: OnAfterRenderAsync -- Initializing the JS SDK via interop");
        if (firstRender)
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            Console.WriteLine("SentryBlazor: backend loaded");
        }
        // TODO: On each render? New modules can load but overhead?
        try
        {
            var asd  = await JSRuntime.InvokeAsync<dynamic[]>("getImages")
                .ConfigureAwait(true);

            DebugImages = await JSRuntime.InvokeAsync<DebugImage[]>("getImages")
                .ConfigureAwait(true);
        }
        catch (Exception e)
        {
            SentryOptions.DiagnosticLogger?.LogError("Failed to get DebugImages.", e);
        }
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (sender is null)
        {
            return;
        }

        SentrySdk.ConfigureScope(s =>
        {
            var url = e.Location;
            s.AddBreadcrumb("",
                type: "navigation",
				category: "navigation",
                data: new Dictionary<string, string>
                {
                    {"to", url},
                    {"from", _previousUrl},
                });
            s.Request.Url = url;
            // keep tabs for the next call
            _previousUrl = url;
        });
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }
}
