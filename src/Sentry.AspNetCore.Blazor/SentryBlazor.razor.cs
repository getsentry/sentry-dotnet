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
    [Inject] private SentryBlazorOptions SentryBlazorOptions { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Early enough?
    // protected override async Task OnInitializedAsync()
    // {
    //     Console.WriteLine("SentryBlazor: OnInitializedAsync Starting");
    //
    //     Console.WriteLine("SentryBlazor: OnInitializedAsync Completed");
    // }

    protected override Task OnParametersSetAsync()
    {
        Console.WriteLine("SentryBlazor: OnParametersSetAsync");
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;


            var options = new
            {
                // dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537",
                dsn = SentryBlazorOptions.Dsn,
                replaysSessionSampleRate = 1.0,
                replaysOnErrorSampleRate = 1.0,
                tracesSampleRate = 1.0,
                release = SentryBlazorOptions.Release,
                debug = true
            };

            try
            {
                await JSRuntime.InvokeVoidAsync("initSentry", options)
                    .ConfigureAwait(true);
            }
            catch (JSException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                SentrySdk.CaptureException(e);
                return;
            }
            Console.WriteLine("SentryBlazor: backend loaded");
            // await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (sender is null) return;
        SentrySdk.AddBreadcrumb("Location changed: " + ((NavigationManager)sender).Uri);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }
}
