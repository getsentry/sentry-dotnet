using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Blazor;

internal class JavaScriptScopeObserver : IScopeObserver
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

    public async void SetExtra(string key, object? value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Sentry.setExtra", key, value)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogError("Failed to sync scope with JavaScript", e);
        }
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

    public async void UnsetTag(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Sentry.setTag", key, null)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogError("Failed to sync scope with JavaScript", e);
        }
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
