using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

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
    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        throw new NotImplementedException();
    }

    public void SetExtra(string key, object? value)
    {
        throw new NotImplementedException();
    }

    public void SetTag(string key, string value)
    {
        throw new NotImplementedException();
    }

    public void UnsetTag(string key)
    {
        throw new NotImplementedException();
    }

    public void SetUser(User? user)
    {
        throw new NotImplementedException();
        }
    }
