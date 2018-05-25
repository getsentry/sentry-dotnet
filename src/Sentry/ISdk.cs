using System;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry
{
    internal interface ISdk : IDisposable
    {
        void ConfigureScope(Action<Scope> configureScope);

        IDisposable PushScope();

        SentryResponse CaptureEvent(SentryEvent evt);
        SentryResponse CaptureException(Exception exception);
        Task<SentryResponse> CaptureExceptionAsync(Exception exception);
        SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler);
        Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler);
        SentryResponse CaptureEvent(Func<SentryEvent> eventFactory);
        Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory);
    }
}
