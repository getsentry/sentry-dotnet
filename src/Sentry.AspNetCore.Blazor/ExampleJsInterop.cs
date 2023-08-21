using Microsoft.JSInterop;

namespace Sentry.AspNetCore.Blazor;

/// <summary>
/// ExampleJsInterop
/// </summary>
public class ExampleJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public ExampleJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new (() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/razor/exampleJsInterop.js").AsTask());
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await moduleTask.Value
            // TODO: Requires context?
            .ConfigureAwait(true);
        return await module.InvokeAsync<string>("showPrompt", message)
            // TODO: Requires context?
            .ConfigureAwait(true);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value
                // TODO: Requires context?
                .ConfigureAwait(true);
            await module.DisposeAsync()
                // TODO: Requires context?
                .ConfigureAwait(true);
        }
    }
}
