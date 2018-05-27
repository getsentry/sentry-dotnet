using System;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    // The SDK main API set
    public interface ISdk
    {
        bool IsEnabled { get; }

        // Scope stuff:
        void ConfigureScope(Action<Scope> configureScope);
        IDisposable PushScope();
        IDisposable PushScope<TState>(TState state);

        // Client or Client/Scope stuff:
        SentryResponse CaptureEvent(SentryEvent evt);
        SentryResponse CaptureEvent(Func<SentryEvent> eventFactory);
        Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory);
        SentryResponse CaptureException(Exception exception);
        Task<SentryResponse> CaptureExceptionAsync(Exception exception);
        SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler);
        Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler);
    }
}
