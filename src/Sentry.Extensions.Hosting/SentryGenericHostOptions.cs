using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Sentry.Reflection;

namespace Sentry.Extensions.Hosting;

/// <summary>
/// Sentry options for apps that use the .NET Generic Host.
/// </summary>
public class SentryGenericHostOptions : SentryHostOptions
{
    /// <summary>
    /// Creates a new instance of <see cref="SentryGenericHostOptions"/>.
    /// </summary>
    public SentryGenericHostOptions()
    {
        SetSdk(Constants.SdkName, typeof(SentryGenericHostOptions).Assembly.GetNameAndVersion());
    }

    /// <summary>
    /// Whether the <see cref="ILogger"/> integration that <c>UseSentry</c> adds sends log entries to Sentry as structured logs.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>, including when the app also calls <c>AddSentry()</c> on its
    /// <see cref="ILoggingBuilder"/>.
    /// </remarks>
    public bool EnableLogs
    {
        get => LogsEnabled;
        set => LogsEnabled = value;
    }
}
