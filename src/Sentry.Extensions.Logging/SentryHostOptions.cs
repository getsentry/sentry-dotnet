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

    internal bool LogsEnabled { get; set; }

    /// <summary>
    /// Controls whether the casing of the standard (Production, Development and Staging) host environment name
    /// is adjusted when setting the Sentry environment. Defaults to true.
    /// </summary>
    /// <remarks>
    /// The default .NET environment names include Production, Development and Staging (note Pascal casing), whereas Sentry prefers
    /// to have its environment setting be all lower case. Has no effect in MAUI, which has no host environment.
    /// </remarks>
    public bool AdjustStandardEnvironmentNameCasing { get; set; } = true;

    private static readonly string[] StandardEnvironmentNames =
    {
        Internal.Constants.ProductionEnvironmentSetting,
        Internal.Constants.StagingEnvironmentSetting,
        Internal.Constants.DevelopmentEnvironmentSetting
    };

    internal void SetEnvironment(string hostEnvironmentName)
    {
        if (SettingLocator.GetEnvironment(useDefaultIfNotFound: false) is not null)
        {
            return;
        }

        Environment = AdjustStandardEnvironmentNameCasing
            ? StandardEnvironmentNames.FirstOrDefault(name => name.Equals(hostEnvironmentName, StringComparison.OrdinalIgnoreCase)) ?? hostEnvironmentName
            : hostEnvironmentName;
    }

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
