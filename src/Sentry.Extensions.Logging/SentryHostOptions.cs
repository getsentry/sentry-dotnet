using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Options for integrations that initialize Sentry and also send log entries to it, such as ASP.NET Core and MAUI.
/// </summary>
/// <inheritdoc />
public abstract class SentryHostOptions : SentryOptions
{
    internal SentryLoggingOptions Logging { get; } = new();

    /// <inheritdoc cref="SentryLoggingOptions.MinimumBreadcrumbLevel"/>
    public LogLevel MinimumBreadcrumbLevel
    {
        get => Logging.MinimumBreadcrumbLevel;
        set => Logging.MinimumBreadcrumbLevel = value;
    }

    /// <inheritdoc cref="SentryLoggingOptions.MinimumEventLevel"/>
    public LogLevel MinimumEventLevel
    {
        get => Logging.MinimumEventLevel;
        set => Logging.MinimumEventLevel = value;
    }

    /// <summary>
    /// Whether the <see cref="ILogger"/> integration that this host adds sends log entries to Sentry as structured logs.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>, including when the app also calls <c>AddSentry()</c> on its
    /// <see cref="ILoggingBuilder"/>. Logs sent with <see cref="SentrySdk.Logger"/>, or by the Serilog, NLog and
    /// log4net integrations, don't depend on this option.
    /// </remarks>
    public bool EnableLogs { get; set; }

    /// <summary>
    /// Add a callback to configure the scope upon SDK initialization
    /// </summary>
    /// <param name="action">The function to invoke when initializing the SDK</param>
    public void ConfigureScope(Action<Scope> action) => ConfigureScopeCallbacks = ConfigureScopeCallbacks.Concat(new[] { action }).ToArray();

    /// <summary>
    /// List of callbacks to be invoked when initializing the SDK
    /// </summary>
    internal Action<Scope>[] ConfigureScopeCallbacks { get; set; } = Array.Empty<Action<Scope>>();

    internal void ApplyConfigureScopeCallbacks(IHub hub)
    {
        foreach (var callback in ConfigureScopeCallbacks)
        {
            hub.ConfigureScope(callback);
        }
    }
}
