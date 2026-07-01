using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

/// <summary>
/// Exposes settings that are read from multiple places, such as environment variables, options, attributes, or defaults.
/// </summary>
internal class SettingLocator
{
    private readonly SentryOptions _options;

    public SettingLocator(SentryOptions options)
    {
        _options = options;
    }

    public Assembly? AssemblyForAttributes { get; protected set; } = Assembly.GetEntryAssembly();

    // IMPORTANT: This method has the only usage of Environment.GetEnvironmentVariable in the entire solution.
    // All callers should go through this method.  Overriding this method thus allows one place for mocking environment variables.
    public virtual string? GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);

    /*
     * In all cases, the order of precedence is:
     *  1. A value already assigned to a SentryOptions property
     *  2. A value set in an environment variable
     *  3. A value from an assembly attribute, when applicable
     *  4. A default value, when applicable
     *
     *  Except when already assigned, any non-null value resolved should be assigned to the SentryOptions property.
     */

    public string GetDsn() => GetDsn(required: true);

    public string GetDsn(bool required)
    {
        // For DSN only

        if (!string.IsNullOrEmpty(_options.Dsn))
        {
            _options.LogDebug("DSN read from options: {0}", _options.Dsn);
            return _options.Dsn;
        }

        var dsn = GetEnvironmentVariable(Constants.DsnEnvironmentVariable);
        if (dsn is not null)
        {
            _options.LogDebug("DSN read from environment variable: {0}", dsn);
        }

        dsn ??= AssemblyForAttributes?.GetCustomAttribute<DsnAttribute>()?.Dsn;
        if (dsn is not null)
        {
            _options.LogDebug("DSN read from assembly attribute: {0}", dsn);
        }

        _options.LogDebug("AssemblyForAttributes is {0}", AssemblyForAttributes?.FullName);

        // If there has been no DSN provided (`null`) and none has been found in the environment we consider this a
        // failed configuration and throw
        // By conventions, skip this if the DSN is not `null` i.e. `string.Empty`
        if (_options.Dsn is null && dsn is null)
        {
            if (!required)
            {
                // When DSN is not required (e.g. Spotlight-only mode), treat as disabled
                _options.Dsn = string.Empty;
                return string.Empty;
            }

            throw new ArgumentNullException("You must supply a DSN to use Sentry." +
                                            "To disable Sentry, pass an empty string: \"\"." +
                                            "See https://docs.sentry.io/platforms/dotnet/configuration/options/#dsn");
        }

        // Overwriting the `string.Empty` with the DSN found in the environment
        if (dsn is not null)
        {
            _options.Dsn = dsn;
        }

        Debug.Assert(_options.Dsn != null, "Dsn can't be null at this point based on the rules above");
        return _options.Dsn!;
    }

    public string GetEnvironment() => GetEnvironment(true)!;

    public string? GetEnvironment(bool useDefaultIfNotFound)
    {
        var environment = _options.Environment;
        if (!string.IsNullOrWhiteSpace(environment))
        {
            return environment;
        }

        environment = GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariable).NullIfWhitespace();

        if (useDefaultIfNotFound)
        {
            environment ??= Debugger.IsAttached
                ? Constants.DebugEnvironmentSetting
                : Constants.ProductionEnvironmentSetting;
        }
        else if (environment == null)
        {
            return null;
        }

        _options.Environment = environment;
        return environment;
    }

    private static readonly HashSet<string> TruthyValues = new(StringComparer.OrdinalIgnoreCase)
        { "true", "t", "y", "yes", "on", "1" };

    private static readonly HashSet<string> FalsyValues = new(StringComparer.OrdinalIgnoreCase)
        { "false", "f", "n", "no", "off", "0" };

    /// <summary>
    /// Resolves Spotlight configuration from environment variables and applies precedence rules per spec.
    /// Must be called before <see cref="GetDsn()"/> so that <see cref="SentryOptions.EnableSpotlight"/> is set.
    /// </summary>
    public void ResolveSpotlight()
    {
        // Per spec: config options override environment variables.
        // If EnableSpotlight was explicitly set to false in config, nothing can override it.
        if (_options.EnableSpotlightExplicitlySet && !_options.EnableSpotlight)
        {
            return;
        }

        var envVar = GetEnvironmentVariable(Constants.SpotlightEnvironmentVariable)?.Trim();
        if (string.IsNullOrEmpty(envVar))
        {
            return;
        }

        if (FalsyValues.Contains(envVar))
        {
            // Env var disables — but only if config didn't explicitly enable
            if (!_options.EnableSpotlightExplicitlySet)
            {
                _options.LogDebug("Spotlight disabled via {0} environment variable.", Constants.SpotlightEnvironmentVariable);
            }
            else
            {
                _options.LogDebug("Spotlight {0} environment variable is '{1}' but EnableSpotlight was explicitly set in configuration. Config value takes precedence.",
                    Constants.SpotlightEnvironmentVariable, envVar);
            }
            return;
        }

        if (TruthyValues.Contains(envVar))
        {
            // Env var enables with default URL
            if (!_options.EnableSpotlight)
            {
                _options.EnableSpotlight = true;
                _options.LogDebug("Spotlight enabled via {0} environment variable.", Constants.SpotlightEnvironmentVariable);
            }
            // Per spec: config spotlight=true + env var URL → use env var URL.
            // But here the env var is just truthy (not a URL), so nothing more to do.
            return;
        }

        // Any other non-empty string is treated as a custom URL.
        // Per spec: if config specifies a string URL → override env var (with warning).
        if (_options.SpotlightUrlExplicitlySet)
        {
            _options.LogWarning(
                "Spotlight URL from {0} environment variable ('{1}') is being ignored " +
                "because a custom SpotlightUrl was set in configuration ('{2}').",
                Constants.SpotlightEnvironmentVariable, envVar, _options.SpotlightUrl);
            // Still enable if not already enabled
            if (!_options.EnableSpotlight)
            {
                _options.EnableSpotlight = true;
            }
            return;
        }

        // Env var provides a URL: enable Spotlight and use it.
        // Per spec: config spotlight=true + env var URL → use env var URL.
        _options.EnableSpotlight = true;
        _options.SpotlightUrl = envVar;
        _options.LogDebug("Spotlight enabled via {0} environment variable with URL: {1}",
            Constants.SpotlightEnvironmentVariable, envVar);
    }

    public string? GetRelease()
    {
        var release = _options.Release;
        if (!string.IsNullOrWhiteSpace(release))
        {
            return release;
        }

        release = GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable).NullIfWhitespace()
                  ?? ApplicationVersionLocator.GetCurrent(AssemblyForAttributes);

        _options.Release = release;
        return release;
    }
}
