using System;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Sentry Client interface
    /// </summary>
    /// <remarks>
    /// The contract of which <see cref="T:Sentry.SentryCore" /> exposes statically.
    /// This interface exist to allow better testability of integrations which otherwise
    /// would require dependency to the static <see cref="T:Sentry.SentryCore" />
    /// </remarks>
    /// <inheritdoc />
    public interface ISentryClient : ISentryScopeManagement
    {
        bool IsEnabled { get; }

        SentryResponse CaptureEvent(SentryEvent evt);
        SentryResponse CaptureEvent(Func<SentryEvent> eventFactory);
    }
}
