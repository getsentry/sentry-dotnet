using Sentry.Internal.Extensions;

// ReSharper disable once CheckNamespace
namespace NLog;

/// <summary>
/// NLog configuration extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurationExtensions
{
    // Internal for testability
    internal const string DefaultTargetName = "sentry";

    internal const string ObsoleteDsnOverload =
        "The Sentry target no longer initializes the SDK, so a DSN can no longer be supplied to it. " +
        "Initialize Sentry with SentrySdk.Init (or UseSentry via one of the integrations), and remove 'dsn', " +
        "'initializeSdk' and any other core SDK settings from the target configuration.";

    /// <summary>
    /// Not supported. The Sentry target no longer initializes the SDK.
    /// </summary>
    /// <param name="configuration">The NLog configuration.</param>
    /// <param name="dsn">No longer supported.</param>
    /// <param name="optionsConfig">An optional action for configuring the Sentry target options.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    [Obsolete(ObsoleteDsnOverload, error: true)]
    public static LoggingConfiguration AddSentry(
        this LoggingConfiguration configuration,
        string? dsn,
        Action<SentryNLogOptions>? optionsConfig = null)
        => throw new NotSupportedException(ObsoleteDsnOverload);

    /// <summary>
    /// Not supported. The Sentry target no longer initializes the SDK.
    /// </summary>
    /// <param name="configuration">The NLog configuration.</param>
    /// <param name="dsn">No longer supported.</param>
    /// <param name="targetName">The name to give the new target.</param>
    /// <param name="optionsConfig">An optional action for configuring the Sentry target options.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    [Obsolete(ObsoleteDsnOverload, error: true)]
    public static LoggingConfiguration AddSentry(
        this LoggingConfiguration configuration,
        string? dsn,
        string targetName,
        Action<SentryNLogOptions>? optionsConfig = null)
        => throw new NotSupportedException(ObsoleteDsnOverload);

    /// <summary>
    /// Adds a target for Sentry to the NLog configuration.
    /// </summary>
    /// <remarks>
    /// This doesn't initialise Sentry. Initialise Sentry separately, using <c>SentrySdk.Init</c> or another Sentry
    /// integration (such as ASP.NET Core or MAUI).
    /// </remarks>
    /// <param name="configuration">The NLog configuration.</param>
    /// <param name="optionsConfig">An optional action for configuring the Sentry target options.</param>
    /// <param name="targetName">The name to give the new target.</param>
    /// <returns>The configuration.</returns>
    public static LoggingConfiguration AddSentry(
        this LoggingConfiguration configuration,
        Action<SentryNLogOptions>? optionsConfig = null,
        string targetName = DefaultTargetName)
    {
        // Not to throw on code that ignores nullability warnings.
        if (configuration.IsNull())
        {
            return configuration!;
        }

        var options = new SentryNLogOptions();

        optionsConfig?.Invoke(options);

        LogManager.Setup().SetupExtensions(e =>
        {
            e.RegisterTarget<SentryTarget>("Sentry");
            e.RegisterType<SentryNLogOptions>();
        });

        var target = new SentryTarget(options)
        {
            Name = targetName,
            Layout = "${message}",
        };

        configuration.AddTarget(targetName, target);

        configuration.AddRuleForAllLevels(targetName);

        return configuration;
    }

    /// <summary>
    /// Add additional tags that will be sent with every message.
    /// </summary>
    /// <param name="options">The options being configured.</param>
    /// <param name="name">The name of the tag.</param>
    /// <param name="layout">The layout to be rendered for the tag</param>
    public static void AddTag(this SentryNLogOptions options, string name, Layout layout)
    {
        options.Tags.Add(new TargetPropertyWithContext(name, layout));
    }
}
