using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Blazor;

public class JavaScriptScopeObserver : IScopeObserver
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SentryBlazorOptions _options;

    public JavaScriptScopeObserver(IJSRuntime jsRuntime, SentryBlazorOptions options)
    {
        _jsRuntime = jsRuntime;
        _options = options;
    }

    public async void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Sentry.addBreadcrumb", breadcrumb)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogError("Failed to sync scope with JavaScript", e);
        }
    }

    public void SetExtra(string key, object? value)
    {
        throw new NotImplementedException();
    }

    public async void SetTag(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Sentry.setTag", key, value)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogError("Failed to sync scope with JavaScript", e);
        }
    }

    public void UnsetTag(string key)
    {
        throw new NotImplementedException();
    }

    public async void SetUser(User? user)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Sentry.setUser", user)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogError("Failed to sync scope with JavaScript", e);
        }
    }
}
